using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Vm;

/// <summary>
/// The window's brain: owns the source of readings, builds the console (one channel per part of the computer)
/// and keeps the status line. The right side shows the solo channel or the settings.
/// </summary>
public sealed class ProzorVm : ObservableObject, IDisposable
{
    private readonly Dispatcher _dispatcher;

    /// <summary>What this run works with. Same object as <see cref="Podesavanja"/>, or a copy in a probe run.</summary>
    private readonly Sistem.Podesavanja _rad;

    private Nadzor? _nadzor;
    private PomocnikKlijent? _pomocnik;
    private object? _prikaz;
    private KanalVm? _kanal;
    private string _stanje = "";
    private string _vrstaStanja = "ceka";
    private string? _upozorenje;
    private string _vreme = Format.Nema;
    private bool _disposed;
    private bool _trazimPomocnika;

    public ProzorVm(Dispatcher dispatcher, bool probno)
    {
        _dispatcher = dispatcher;

        Podesavanja = Sistem.Podesavanja.Ucitaj();
        _rad = Podesavanja;
        if (probno)
        {
            // Test and screenshot runs must not touch the real history, nor the saved settings.
            _rad = Podesavanja.Kopija();
            _rad.ProbniPodaci = true;
            _rad.Istorija = false;
        }

        PodesavanjaVm = new PodesavanjaVm(Podesavanja, ObrisiIstoriju, PromeniOsvezavanje);
        Stanje = "Učitavam senzore…";
    }

    /// <summary>The user's saved choices, as the settings screen shows and saves them.</summary>
    public Sistem.Podesavanja Podesavanja { get; }

    public PodesavanjaVm PodesavanjaVm { get; }

    /// <summary>The console, left to right: CPU, GPU, RAM, disks, fans.</summary>
    public ObservableCollection<KanalVm> Kanali { get; } = [];

    /// <summary>What the right side shows: a <see cref="KanalVm"/> (solo) or <see cref="PodesavanjaVm"/>.</summary>
    public object? Prikaz
    {
        get => _prikaz;
        private set
        {
            if (!SetProperty(ref _prikaz, value)) return;
            OnPropertyChanged(nameof(NemaPrikaza));
            OnPropertyChanged(nameof(PodesavanjaOtvorena));
        }
    }

    /// <summary>True while the sensors are still opening: the window shows "Učitavam senzore…" instead of nothing.</summary>
    public bool NemaPrikaza => _prikaz is null;

    public bool PodesavanjaOtvorena => _prikaz is PodesavanjaVm;

    /// <summary>The channel in solo. Null while the settings are open.</summary>
    public KanalVm? Kanal
    {
        get => _kanal;
        set
        {
            if (_kanal == value) return;
            if (_kanal is not null) _kanal.Solo = false;
            _kanal = value;
            if (value is not null)
            {
                value.Solo = true;
                Prikaz = value;
            }
            OnPropertyChanged();
        }
    }

    public string Stanje
    {
        get => _stanje;
        private set => SetProperty(ref _stanje, value);
    }

    /// <summary>"pravo", "pomocnik", "probno", "nema" or "ceka": picks the colour of the dot in the header.</summary>
    public string VrstaStanja
    {
        get => _vrstaStanja;
        private set => SetProperty(ref _vrstaStanja, value);
    }

    public string? Upozorenje
    {
        get => _upozorenje;
        private set
        {
            if (!SetProperty(ref _upozorenje, value)) return;
            OnPropertyChanged(nameof(UpozorenjeVidljivo));
        }
    }

    public bool UpozorenjeVidljivo => !string.IsNullOrWhiteSpace(_upozorenje);

    public string Vreme
    {
        get => _vreme;
        private set => SetProperty(ref _vreme, value);
    }

    /// <summary>Seconds between readings in this run; the chart turns it into "pre 2 min".</summary>
    public int Perioda => _rad.Osvezavanje;

    /// <summary>The beat in the header: "♩ 1 s".</summary>
    public string Ritam => $"♩ {_rad.Osvezavanje} s";

    public string Verzija => Izmene.Trenutna.Oznaka;

    public string Prava => Admin.Stanje;

    public int BrojSenzora => _nadzor?.Senzori.Count ?? 0;

    public string BrojSenzoraTekst => BrojSenzora switch
    {
        0 => "nema senzora",
        var n when n % 10 == 1 && n % 100 != 11 => $"{n} senzor",
        var n => $"{n} senzora",
    };

    private bool _pokrecem;
    private int _osvezavanjeCeka;

    /// <summary>
    /// Opens the sources off the UI thread: the driver, the helper's pipe and the database can each take a
    /// second, and the window must never show "Not responding" because of it. The window is already on screen
    /// when this runs and says "Učitavam senzore…". A helper the caller already reached is used as it is.
    /// </summary>
    public async Task PokreniAsync(PomocnikKlijent? gotovPomocnik = null)
    {
        if (_pokrecem) return;
        _pokrecem = true;

        try
        {
            Stanje = "Učitavam senzore…";
            VrstaStanja = "ceka";

            var (izvor, nadzor) = await Task.Run(() =>
            {
                IIzvorSenzora i = _rad.ProbniPodaci ? new LazniIzvor() : PraviIzvor(gotovPomocnik);
                return (i, new Nadzor(i, _rad));
            });

            if (_disposed)
            {
                nadzor.Dispose();
                return;
            }

            _nadzor = nadzor;
            _nadzor.NoviUzorak += _ => ZakaziOsvezavanje();
            _nadzor.Poruka += poruka => _dispatcher.BeginInvoke(new Action(() => Upozorenje = poruka));

            // Readings before the console is shown: the meters start with real numbers.
            if (!izvor.Pravi)
                for (int i = 0; i < 180; i++) _nadzor.UzmiJednom();
            else
                _nadzor.UzmiJednom();

            NapraviPult();
            Osvezi();
            _nadzor.Pokreni();
            PostaviStanje(izvor);
            OnPropertyChanged(nameof(PomocnikMoguc));
            OnPropertyChanged(nameof(BrojSenzora));
            OnPropertyChanged(nameof(BrojSenzoraTekst));
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Pokretanje nadzora", ex);
            Upozorenje = "Program ne može da čita senzore: " + ex.Message;
            Stanje = "Nema podataka";
            VrstaStanja = "nema";
        }
        finally
        {
            _pokrecem = false;
        }
    }

    /// <summary>At most one redraw waiting: a busy window must not queue a hundred updates.</summary>
    private void ZakaziOsvezavanje()
    {
        if (Interlocked.Exchange(ref _osvezavanjeCeka, 1) == 1) return;

        _dispatcher.BeginInvoke(new Action(() =>
        {
            Interlocked.Exchange(ref _osvezavanjeCeka, 0);
            Osvezi();
        }));
    }

    /// <summary>
    /// A helper already running as administrator gives everything; otherwise this account reads what it can.
    /// The helper is preferred even when the window itself is elevated, because then there is only one driver.
    /// </summary>
    private IIzvorSenzora PraviIzvor(PomocnikKlijent? gotov)
    {
        _pomocnik = gotov ?? PomocnikKlijent.Probaj();
        if (_pomocnik is not null) return _pomocnik;

        return new SlozeniIzvor(new LhmIzvor(), new RamIzvor());
    }

    /// <summary>True while a button offering the helper makes sense: real data, not elevated, no helper yet.</summary>
    public bool PomocnikMoguc => !_rad.ProbniPodaci && _pomocnik is null && !Admin.JeAdmin && !_trazimPomocnika;

    /// <summary>
    /// Asks for administrator rights. The Windows question is modal and can stay on screen for a while, so it
    /// is asked from a background thread and the window keeps drawing. Then the helper gets up to ten seconds
    /// to open the driver and answer on the pipe - loading the driver the first time can take that long.
    /// </summary>
    public async Task UkljuciPomocnikaAsync()
    {
        if (_trazimPomocnika) return;
        _trazimPomocnika = true;
        OnPropertyChanged(nameof(PomocnikMoguc));

        try
        {
            Upozorenje = "Tražim administratorska prava…";

            var (uspeh, poruka) = await Task.Run(() => Pomocnik.Pokreni());
            Upozorenje = poruka;
            if (!uspeh) return;

            PomocnikKlijent? klijent = null;
            for (int pokusaj = 0; pokusaj < 20 && klijent is null && !_disposed; pokusaj++)
                klijent = await Task.Run(() => PomocnikKlijent.Probaj(500));

            if (_disposed)
            {
                klijent?.Dispose();
                return;
            }

            if (klijent is null)
            {
                Upozorenje = "Pomoćnik se nije javio. Razlog je u greske.log; program nastavlja bez njega.";
                return;
            }

            await PonovoOtvoriAsync(klijent);
        }
        finally
        {
            _trazimPomocnika = false;
            OnPropertyChanged(nameof(PomocnikMoguc));
        }
    }

    private async Task PonovoOtvoriAsync(PomocnikKlijent klijent)
    {
        var stari = _nadzor;
        string? soloKljuc = _kanal?.Kljuc;
        _nadzor = null;
        _pomocnik = null;
        Prikaz = null;
        Kanal = null;
        Kanali.Clear();
        Upozorenje = null;

        if (stari is not null) await Task.Run(() => stari.Dispose());

        await PokreniAsync(klijent);

        // The same channel stays in solo after the helper brought more sensors.
        if (soloKljuc is not null && Kanali.FirstOrDefault(k => k.Kljuc == soloKljuc) is { } isti)
            Kanal = isti;
    }

    private void NapraviPult()
    {
        if (_nadzor is null) return;

        var senzori = new List<SenzorVm>(_nadzor.Senzori.Count);
        for (int i = 0; i < _nadzor.Senzori.Count; i++)
            senzori.Add(new SenzorVm(_nadzor.Senzori[i], i, _nadzor.Istorija[i]));

        Kanali.Clear();
        foreach (var kanal in KanalVm.Napravi(senzori))
        {
            kanal.Utisan = Podesavanja.Utisani.Contains(kanal.Kljuc);
            Kanali.Add(kanal);
        }

        Kanal = Kanali.FirstOrDefault(k => !k.Prazan && !k.Utisan) ?? Kanali.FirstOrDefault();
    }

    /// <summary>M on a strip: the channel folds into a narrow band, and the choice is remembered.</summary>
    public void Utisaj(KanalVm kanal)
    {
        kanal.Utisan = !kanal.Utisan;

        Podesavanja.Utisani.Remove(kanal.Kljuc);
        if (kanal.Utisan) Podesavanja.Utisani.Add(kanal.Kljuc);
        if (!ReferenceEquals(_rad, Podesavanja)) _rad.Utisani = [.. Podesavanja.Utisani];
        if (!_rad.ProbniPodaci || ReferenceEquals(_rad, Podesavanja)) Podesavanja.Sacuvaj();
    }

    private void PostaviStanje(IIzvorSenzora izvor)
    {
        (Stanje, VrstaStanja) = izvor switch
        {
            { Pravi: false } => ("Probni podaci", "probno"),
            PomocnikKlijent => ("Potpuni nadzor", "pomocnik"),
            { Senzori.Count: > 0 } when Admin.JeAdmin => ("Potpuni nadzor · administrator", "pomocnik"),
            { Senzori.Count: > 0 } => ("Ograničen nadzor", "pravo"),
            _ => ("Nema podataka", "nema"),
        };

        if (!izvor.Pravi)
        {
            Upozorenje = "Prikazani su probni podaci, ne stvarna merenja. U podešavanjima isključi „Probni podaci“.";
            return;
        }

        if (izvor.Senzori.Count == 0)
        {
            Upozorenje = (izvor.Poruka ?? "Nijedan senzor nije dostupan.") +
                         " Uključi potpuni nadzor ili probne podatke u podešavanjima.";
            return;
        }

        if (!Admin.JeAdmin && izvor is not PomocnikKlijent)
            Upozorenje = "Temperatura procesora, potrošnja, ventilatori i diskovi traže potpuni nadzor (administrator).";
    }

    private void Osvezi()
    {
        if (_disposed || _nadzor is null) return;

        var (snimak, najmanje, najvise) = _nadzor.Stanje();
        if (snimak is null) return;

        // The helper explains its own troubles (no driver, no permission) and the window repeats it.
        if (_nadzor.Izvor.Poruka is { } poruka) Upozorenje = poruka;

        Vreme = snimak.Vreme.ToString("HH:mm:ss");
        var sada = DateTime.Now;
        foreach (var kanal in Kanali)
        {
            kanal.Detalj.Osvezi(snimak, najmanje, najvise);
            kanal.Osvezi(sada);
        }
    }

    private KanalVm? _prePodesavanja;

    /// <summary>Opens the settings in place of the solo panel; the same button closes them and the channel returns.</summary>
    public void PrikaziPodesavanja()
    {
        if (Prikaz is PodesavanjaVm)
        {
            Kanal = _prePodesavanja ?? Kanali.FirstOrDefault(k => !k.Prazan) ?? Kanali.FirstOrDefault();
            return;
        }

        _prePodesavanja = _kanal;
        Kanal = null;
        Prikaz = PodesavanjaVm;
    }

    private void PromeniOsvezavanje(int sekundi)
    {
        _rad.Osvezavanje = sekundi;
        _nadzor?.PromeniPeriodu(sekundi);
        OnPropertyChanged(nameof(Perioda));
        OnPropertyChanged(nameof(Ritam));
    }

    private bool ObrisiIstoriju()
    {
        if (_nadzor is not null) return _nadzor.ObrisiIstoriju();

        // Sensors not open (still loading or failed): the file can still be emptied.
        try
        {
            if (File.Exists(Putanje.Baza))
            {
                using var baza = new Podaci.Baza(Putanje.Baza);
                baza.ObrisiSve();
            }
            return true;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Brisanje istorije", ex);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _nadzor?.Dispose();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Zatvaranje nadzora", ex);
        }
        _nadzor = null;
        _pomocnik = null;
    }
}

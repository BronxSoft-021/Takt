using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Vm;

/// <summary>The window's brain: owns the source of readings, builds one screen per group and keeps the status line.</summary>
public sealed class ProzorVm : ObservableObject, IDisposable
{
    private readonly Dispatcher _dispatcher;

    /// <summary>What this run works with. Same object as <see cref="Podesavanja"/>, or a copy in a probe run.</summary>
    private readonly Sistem.Podesavanja _rad;

    private Nadzor? _nadzor;
    private PomocnikKlijent? _pomocnik;
    private object? _prikaz;
    private EkranVm? _ekran;
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

    public ObservableCollection<EkranVm> Ekrani { get; } = [];

    /// <summary>The tiles at the top: one headline number per group, at most four.</summary>
    public ObservableCollection<PregledVm> Pregled { get; } = [];

    public bool ImaPregled => Pregled.Count > 0;

    /// <summary>What the right side shows: an <see cref="EkranVm"/> or <see cref="PodesavanjaVm"/>.</summary>
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

    /// <summary>Lights the settings button at the bottom of the left column while that screen is open.</summary>
    public bool PodesavanjaOtvorena => _prikaz is PodesavanjaVm;

    /// <summary>Selected in the left column. Null while the settings are open.</summary>
    public EkranVm? Ekran
    {
        get => _ekran;
        set
        {
            if (!SetProperty(ref _ekran, value)) return;
            if (value is not null) Prikaz = value;
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

            // Readings before the screens are shown: the list starts with real numbers, and a tile can pick
            // a sensor that actually has a value on this computer (load, when the temperature needs a driver).
            if (!izvor.Pravi)
                for (int i = 0; i < 180; i++) _nadzor.UzmiJednom();
            else
                _nadzor.UzmiJednom();

            NapraviEkrane();
            Osvezi();
            NapraviPregled();
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
        _nadzor = null;
        _pomocnik = null;
        Prikaz = null;
        Ekran = null;
        Ekrani.Clear();
        Pregled.Clear();
        OnPropertyChanged(nameof(ImaPregled));
        Upozorenje = null;

        if (stari is not null) await Task.Run(() => stari.Dispose());

        await PokreniAsync(klijent);
    }

    private void NapraviEkrane()
    {
        if (_nadzor is null) return;

        Ekrani.Clear();
        foreach (var grupa in GrupaOpis.Redosled)
        {
            var senzori = new List<SenzorVm>();
            for (int i = 0; i < _nadzor.Senzori.Count; i++)
            {
                if (_nadzor.Senzori[i].Grupa != grupa) continue;
                senzori.Add(new SenzorVm(_nadzor.Senzori[i], i, _nadzor.Istorija[i]));
            }
            Ekrani.Add(new EkranVm(grupa, senzori));
        }

        Ekran = Ekrani.FirstOrDefault(e => e.Senzori.Count > 0) ?? Ekrani.FirstOrDefault();
    }

    /// <summary>The tiles: the groups that have something to show, up to four, each with its headline sensor.</summary>
    private void NapraviPregled()
    {
        Pregled.Clear();

        foreach (var ekran in Ekrani)
        {
            if (ekran.Senzori.Count == 0) continue;
            Pregled.Add(new PregledVm(ekran, Glavni(ekran)));
            if (Pregled.Count == 4) break;
        }

        OnPropertyChanged(nameof(ImaPregled));
    }

    /// <summary>Names that deserve to be the big number, best first; the rest falls back to the first with a value.</summary>
    private static readonly string[] GlavnaImena =
    [
        "Temperatura procesora", "Temperatura", "Temperatura jezgra 1", "Temperatura paketa",
        "Opterećenje procesora", "Zauzeće", "Zauzeće VRAM", "Opterećenje",
        "Ventilator 1", "Ventilator",
    ];

    public static SenzorVm? Glavni(EkranVm ekran)
    {
        // Only a sensor that has a value: on a computer without the driver the processor temperature is "—",
        // and a tile reading "—" looks broken. Load is the honest fallback.
        foreach (var ime in GlavnaImena)
            if (ekran.Senzori.FirstOrDefault(s => s.Ime == ime && s.Broj is not null) is { } nadjen)
                return nadjen;

        return ekran.Senzori.FirstOrDefault(s => s.Broj is not null) ?? ekran.Senzori.FirstOrDefault();
    }

    public void PrikaziGrupu(Grupa grupa)
    {
        if (Ekrani.FirstOrDefault(e => e.Grupa == grupa) is { } ekran)
            Ekran = ekran;
    }

    private void PostaviStanje(IIzvorSenzora izvor)
    {
        (Stanje, VrstaStanja) = izvor switch
        {
            { Pravi: false } => ("Probni podaci", "probno"),
            PomocnikKlijent => ("Potpuni nadzor", "pomocnik"),
            { Senzori.Count: > 0 } when Admin.JeAdmin => ("Pravi senzori · administrator", "pomocnik"),
            { Senzori.Count: > 0 } => ("Pravi senzori", "pravo"),
            _ => ("Nema podataka", "nema"),
        };

        if (!izvor.Pravi)
        {
            Upozorenje = "Prikazani su probni podaci, ne stvarna merenja. U Podešavanjima isključi „Probni podaci“.";
            return;
        }

        if (izvor.Senzori.Count == 0)
        {
            Upozorenje = (izvor.Poruka ?? "Nijedan senzor nije dostupan.") +
                         " Uključi potpuni nadzor ili probne podatke u Podešavanjima.";
            return;
        }

        if (!Admin.JeAdmin && izvor is not PomocnikKlijent)
            Upozorenje = "Temperatura procesora, potrošnja, ventilatori i diskovi traže drajver i administratora. " +
                         "Ostalo se čita i sada.";
    }

    private void Osvezi()
    {
        if (_disposed || _nadzor is null) return;

        var (snimak, najmanje, najvise) = _nadzor.Stanje();
        if (snimak is null) return;

        // The helper explains its own troubles (no driver, no permission) and the window repeats it.
        if (_nadzor.Izvor.Poruka is { } poruka) Upozorenje = poruka;

        Vreme = snimak.Vreme.ToString("HH:mm:ss");
        foreach (var ekran in Ekrani)
            ekran.Osvezi(snimak, najmanje, najvise);
    }

    public void PrikaziPodesavanja()
    {
        Ekran = null;
        Prikaz = PodesavanjaVm;
    }

    private void PromeniOsvezavanje(int sekundi)
    {
        _rad.Osvezavanje = sekundi;
        _nadzor?.PromeniPeriodu(sekundi);
        OnPropertyChanged(nameof(Perioda));
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

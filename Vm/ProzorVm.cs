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

                // The new channel's table may be two seconds old: show it now, not on the next turn.
                _poslednjaTabela = DateTime.MinValue;
                Osvezi();
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

    /// <summary>
    /// True when the PawnIO driver is installed. Without it the processor's temperature, clocks, power and
    /// voltage, the board's fans and the drives' SMART cannot be read at all - asking for administrator again
    /// would change nothing, so the window says "bez drajvera" instead of pretending.
    /// </summary>
    public bool Drajver => PawnIo.JeTu;

    public string DrajverStanje => PawnIo.JeTu ? "instaliran" : "nije instaliran";

    public bool DrajverFali => !PawnIo.JeTu;

    /// <summary>Opens pawnio.eu; the button in the empty channel and in the settings both come here.</summary>
    public void OtvoriDrajver()
    {
        if (PawnIo.Otvori()) return;
        Upozorenje = $"Adresa nije otvorena. Ukucaj je u pregledač: {PawnIo.Adresa}";
    }

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

                // The service keeps the history itself (it reads even while the window is closed): the window
                // must not write the same rows a second time.
                var podesavanja = i is PomocnikKlijent { JeUsluga: true } ? _rad.Kopija() : _rad;
                if (!ReferenceEquals(podesavanja, _rad)) podesavanja.Istorija = false;

                return (i, new Nadzor(i, podesavanja));
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
        bool jePodesavanja = Prikaz is PodesavanjaVm;
        _nadzor = null;
        _pomocnik = null;
        Prikaz = null;
        Kanal = null;
        Kanali.Clear();
        Upozorenje = null;

        if (stari is not null) await Task.Run(() => stari.Dispose());

        await PokreniAsync(klijent);

        // The user stays where he was: in the settings, or in the same channel after the helper brought more sensors.
        if (jePodesavanja)
        {
            Kanal = null;
            PrikaziPodesavanja();
        }
        else if (soloKljuc is not null && Kanali.FirstOrDefault(k => k.Kljuc == soloKljuc) is { } isti)
        {
            Kanal = isti;
        }
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

        // Building the console must not pull the user out of the settings: the tray can open them before the
        // first reading, and then the pult would arrive a second later and replace them.
        if (Prikaz is not Takt.Vm.PodesavanjaVm)
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
        // Full monitoring has two keys, not one: administrator rights and the PawnIO driver. Only both together
        // give everything, so each state names the one that is missing instead of offering "potpuni nadzor".
        (Stanje, VrstaStanja) = izvor switch
        {
            { Pravi: false } => ("Probni podaci", "probno"),
            PomocnikKlijent { JeUsluga: true } when !Drajver => ("Potpuni nadzor · usluga, bez drajvera", "pravo"),
            PomocnikKlijent { JeUsluga: true } => ("Potpuni nadzor · usluga", "pomocnik"),
            PomocnikKlijent when !Drajver => ("Potpuni nadzor · pomoćnik, bez drajvera", "pravo"),
            PomocnikKlijent => ("Potpuni nadzor · pomoćnik", "pomocnik"),
            { Senzori.Count: > 0 } when Admin.JeAdmin && !Drajver => ("Ograničen nadzor · bez drajvera", "pravo"),
            { Senzori.Count: > 0 } when Admin.JeAdmin => ("Potpuni nadzor · administrator", "pomocnik"),
            { Senzori.Count: > 0 } when !Drajver => ("Ograničen nadzor · bez drajvera", "pravo"),
            { Senzori.Count: > 0 } => ("Ograničen nadzor", "pravo"),
            _ => ("Nema podataka", "nema"),
        };

        OnPropertyChanged(nameof(Drajver));
        OnPropertyChanged(nameof(DrajverStanje));
        OnPropertyChanged(nameof(DrajverFali));

        // One line per run, so a report ("nema temperature") can be checked without guessing.
        Dnevnik.Upisi($"Izvor: {izvor.Naziv}, {izvor.Senzori.Count} senzora, stanje: {Stanje}, drajver: {DrajverStanje}.");

        if (!izvor.Pravi)
        {
            Upozorenje = "Prikazani su probni podaci, ne stvarna merenja. U podešavanjima isključi „Probni podaci“.";
            return;
        }

        if (izvor.Senzori.Count == 0)
        {
            Upozorenje = (izvor.Poruka ?? "Nijedan senzor nije dostupan.") +
                         (Drajver
                             ? " Uključi potpuni nadzor ili probne podatke u podešavanjima."
                             : $" Nedostaje i PawnIO drajver ({PawnIo.Adresa}).");
            return;
        }

        if (!Drajver)
        {
            // The driver is the first thing missing; without it administrator rights add only the drives.
            bool vecPovisen = Admin.JeAdmin || izvor is PomocnikKlijent;
            Upozorenje = "Nedostaje PawnIO drajver, pa nema temperature procesora, potrošnje, napona ni ventilatora. " +
                         $"Preuzmi ga sa {PawnIo.Adresa}" +
                         (vecPovisen ? " i pokreni Takt ponovo." : " i uključi potpuni nadzor.");
            return;
        }

        if (!Admin.JeAdmin && izvor is not PomocnikKlijent)
            Upozorenje = "Temperatura procesora, potrošnja, ventilatori i diskovi traže potpuni nadzor (administrator).";
    }

    private DateTime _poslednjaTabela = DateTime.MinValue;

    /// <summary>A changed text costs a layout, so the long table hears about it every two seconds, not every second.</summary>
    private static readonly TimeSpan TabelaNa = TimeSpan.FromSeconds(2);

    private void Osvezi()
    {
        if (_disposed || _nadzor is null) return;

        var (snimak, najmanje, najvise) = _nadzor.Stanje();
        if (snimak is null) return;

        // The helper explains its own troubles (no driver, no permission) and the window repeats it.
        if (_nadzor.Izvor.Poruka is { } poruka) Upozorenje = poruka;

        Vreme = snimak.Vreme.ToString("HH:mm:ss");
        var sada = DateTime.Now;

        bool tabela = sada - _poslednjaTabela >= TabelaNa;
        if (tabela) _poslednjaTabela = sada;

        foreach (var kanal in Kanali)
            kanal.Osvezi(snimak, najmanje, najvise, sada, tabela && ReferenceEquals(kanal, Kanal));
    }

    private KanalVm? _prePodesavanja;

    /// <summary>
    /// Opens the settings in place of the solo panel. A second call changes nothing: the tray menu calls this, and
    /// clicking "Podešavanja" twice must not close them and show the console instead (it looked like the menu
    /// opening the program).
    /// </summary>
    public void PrikaziPodesavanja()
    {
        if (Prikaz is PodesavanjaVm) return;

        _prePodesavanja = _kanal;
        Kanal = null;
        Prikaz = PodesavanjaVm;
    }

    /// <summary>The gear in the header: opens the settings, or closes them and brings the channel back.</summary>
    public void PreklopiPodesavanja()
    {
        if (Prikaz is not Takt.Vm.PodesavanjaVm)
        {
            PrikaziPodesavanja();
            return;
        }

        var nazad = _prePodesavanja ?? Kanali.FirstOrDefault(k => !k.Prazan) ?? Kanali.FirstOrDefault();
        if (nazad is not null) Kanal = nazad;
        else Prikaz = null;
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

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
    private MeracKadrova? _kadrovi;
    private DispatcherTimer? _trazenje;
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

    /// <summary>The game on screen (frames per second from the service), for the Scene's top third.</summary>
    public IgraVm Igra { get; } = new();

    private readonly BiracIgre _birac = new();

    /// <summary>Records game sessions on its own and saves a report when a game ends.</summary>
    public SnimacSesije Sesija { get; } = new();

    private int _probniKadar;

    /// <summary>A made-up game for probe data: about 60 FPS with a slow wave and a stutter now and then.</summary>
    private Protokol.Igra ProbnaIgra(DateTime vreme)
    {
        int t = _probniKadar++;
        double fps = 58 + 8 * Math.Sin(t / 20.0);
        bool zastoj = t % 37 == 0;
        double najduzi = zastoj ? 74 : 1000 / fps + 3 * Math.Abs(Math.Sin(t / 3.0));
        return new Protokol.Igra(0, "Probna igra", null, Math.Round(fps), 1000 / fps, najduzi,
            Math.Round(fps * 0.72), zastoj ? 1 : 0);
    }

    /// <summary>The list of past sessions, opened from the header in place of the solo panel.</summary>
    public void PrikaziSesije()
    {
        if (Prikaz is SesijeVm)
        {
            // The same button closes the list: back to the channel that was in solo.
            var nazad = _prePodesavanja ?? Kanali.FirstOrDefault(k => !k.Prazan) ?? Kanali.FirstOrDefault();
            if (nazad is not null) Kanal = nazad;
            else Prikaz = null;
            return;
        }

        _prePodesavanja = _kanal ?? _prePodesavanja;
        Kanal = null;
        Prikaz = new SesijeVm(Sesija.Ucitaj());
    }

    /// <summary>
    /// The Scene: the console large over the whole second screen, read from a distance while playing. Remembered
    /// (only that one value is written to the settings file, nothing else unsaved goes with it).
    /// </summary>
    public bool Scena
    {
        get => Podesavanja.Scena;
        set
        {
            if (Podesavanja.Scena == value) return;
            Podesavanja.Scena = value;
            OnPropertyChanged();

            if (ReferenceEquals(_rad, Podesavanja))
            {
                try
                {
                    var naDisku = Sistem.Podesavanja.Ucitaj();
                    naDisku.Scena = value;
                    naDisku.Sacuvaj();
                }
                catch (Exception ex)
                {
                    Dnevnik.Greska("Čuvanje Scene", ex);
                }
            }
        }
    }

    /// <summary>The Scene's strips: the console without the muted channels.</summary>
    public IEnumerable<KanalVm> KanaliScene => Kanali.Where(k => !k.Utisan);

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

    private string _prava = Admin.Stanje;

    /// <summary>
    /// Who is actually reading the sensors: the service, the helper, or this window raised to administrator.
    /// The footer used to say „običan korisnik“ while the header said „Potpuni nadzor · usluga“ - two panels,
    /// two truths. Now both come from one switch (<see cref="OpisStanja"/>).
    /// </summary>
    public string Prava
    {
        get => _prava;
        private set => SetProperty(ref _prava, value);
    }

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

            // Everything there is to get: the driver, and administrator through the service, the helper or the
            // window itself. Empty strips then say "nema senzora" instead of asking for monitoring again.
            KanalVm.PotpunNadzor = PawnIo.JeTu && izvor.Pravi && (izvor is PomocnikKlijent || Admin.JeAdmin);
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

            // If the service was not up yet (it is installed but Windows starts it a moment after logon, and right
            // after an update), this window would read with its own rights until it was closed and opened again.
            // Keep asking every few seconds instead.
            PokreniTrazenje();
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

        // Frames belong to the service or the helper - but when the window itself was started as administrator
        // and nobody else is running, it is the only one who may count them. Without this, exactly the most
        // privileged window would be the one without FPS.
        if (SamMeriKadrove(imaPomocnika: false, Admin.JeAdmin)) PokreniKadrove();

        return new SlozeniIzvor(new LhmIzvor(), new RamIzvor());
    }

    /// <summary>
    /// Who counts frames: the service/helper whenever there is one (one ETW session, one driver), otherwise this
    /// window - if it is elevated. Public for a test.
    /// </summary>
    public static bool SamMeriKadrove(bool imaPomocnika, bool admin) => !imaPomocnika && admin;

    private void PokreniKadrove()
    {
        if (_kadrovi is not null) return;

        _kadrovi = new MeracKadrova();
        _kadrovi.Pokreni();
        if (_kadrovi.Greska is { } greska)
            _dispatcher.BeginInvoke(new Action(() => Upozorenje = greska));
    }

    /// <summary>True while a button offering the helper makes sense: real data, not elevated, no helper yet.</summary>
    public bool PomocnikMoguc => !_rad.ProbniPodaci && _pomocnik is null && !Admin.JeAdmin && !_trazimPomocnika;

    /// <summary>Whether asking again makes sense: real data, nobody connected, no request already open. Public for a test.</summary>
    public static bool VrediTrazi(bool probno, bool imaPomocnika, bool trazi, bool disposed) =>
        !probno && !imaPomocnika && !trazi && !disposed;

    private void PokreniTrazenje()
    {
        if (!VrediTrazi(_rad.ProbniPodaci, _pomocnik is not null, _trazimPomocnika, _disposed)) return;

        _trazenje ??= new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background,
            (_, _) => TraziVeze(), _dispatcher);
        _trazenje.Start();
    }

    /// <summary>
    /// Asks the pipe every five seconds while this window reads with its own rights. The connect is a blocking
    /// call, so it runs off the drawing thread; when the service answers, the window moves onto it (one driver,
    /// full monitoring) without being restarted.
    /// </summary>
    private async void TraziVeze()
    {
        if (!VrediTrazi(_rad.ProbniPodaci, _pomocnik is not null, _trazimPomocnika || _pokrecem, _disposed))
        {
            _trazenje?.Stop();
            return;
        }

        PomocnikKlijent? klijent = null;
        for (int pokusaj = 0; pokusaj < 3 && klijent is null && !_disposed; pokusaj++)
            klijent = await Task.Run(() => PomocnikKlijent.Probaj(500));

        if (klijent is null || _disposed || _pomocnik is not null)
        {
            klijent?.Dispose();
            return;
        }

        _trazenje?.Stop();
        Upozorenje = null;
        await PonovoOtvoriAsync(klijent);
    }

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

        // The helper brings its own frame counter (and the ETW session has one name): stop ours before it starts.
        _kadrovi?.Dispose();
        _kadrovi = null;

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
        foreach (var kanal in KanalVm.Napravi(senzori, _nadzor.Poslednji))
        {
            kanal.Utisan = Podesavanja.Utisani.Contains(kanal.Kljuc);
            Kanali.Add(kanal);
        }

        // Building the console must not pull the user out of the settings: the tray can open them before the
        // first reading, and then the pult would arrive a second later and replace them.
        if (Prikaz is not Takt.Vm.PodesavanjaVm)
            Kanal = Kanali.FirstOrDefault(k => !k.Prazan && !k.Utisan) ?? Kanali.FirstOrDefault();
        OnPropertyChanged(nameof(KanaliScene));
    }

    /// <summary>M on a strip: the channel folds into a narrow band, and the choice is remembered.</summary>
    public void Utisaj(KanalVm kanal)
    {
        kanal.Utisan = !kanal.Utisan;

        Podesavanja.Utisani.Remove(kanal.Kljuc);
        if (kanal.Utisan) Podesavanja.Utisani.Add(kanal.Kljuc);
        if (!ReferenceEquals(_rad, Podesavanja)) _rad.Utisani = [.. Podesavanja.Utisani];
        if (!_rad.ProbniPodaci || ReferenceEquals(_rad, Podesavanja)) Podesavanja.Sacuvaj();
        OnPropertyChanged(nameof(KanaliScene));
    }

    /// <summary>
    /// The header sentence, its colour kind, and the footer's word for who reads the sensors - one place, so the
    /// two panels cannot tell different stories. Full monitoring has two keys: administrator rights (by the
    /// window itself, or lent by the service/helper) and the PawnIO driver; each state names the one missing.
    /// </summary>
    /// <param name="pravi">False for the sample source: nothing here is a real reading.</param>
    /// <param name="usluga">The readings come from the installed service.</param>
    /// <param name="pomocnik">They come from the helper this window started (elevated for its lifetime only).</param>
    /// <param name="drajver">PawnIO is installed.</param>
    /// <param name="senzora">How many sensors this source gave.</param>
    /// <param name="admin">This window itself runs as administrator.</param>
    public static (string Stanje, string Vrsta, string Prava) OpisStanja(bool pravi, bool usluga, bool pomocnik,
        bool drajver, int senzora, bool admin)
    {
        if (!pravi) return (Tekst.T("Probni podaci"), "probno", Admin.Stanje);

        if (pomocnik)
        {
            string ko = usluga ? Tekst.T("usluga") : Tekst.T("pomoćnik");
            string nadzor = drajver ? $"{Tekst.T("Potpuni nadzor")} · {ko}" : $"{Tekst.T("Potpuni nadzor")} · {ko}, {Tekst.T("bez drajvera")}";
            return (nadzor, drajver ? "pomocnik" : "pravo", ko);
        }

        if (senzora == 0) return (Tekst.T("Nema podataka"), "nema", admin ? Tekst.T("administrator") : Admin.Stanje);

        if (!admin)
            return drajver
                ? (Tekst.T("Ograničen nadzor"), "pravo", Admin.Stanje)
                : ($"{Tekst.T("Ograničen nadzor")} · {Tekst.T("bez drajvera")}", "pravo", Admin.Stanje);

        return drajver
            ? ($"{Tekst.T("Potpuni nadzor")} · {Tekst.T("administrator")}", "pomocnik", Tekst.T("administrator"))
            : ($"{Tekst.T("Ograničen nadzor")} · {Tekst.T("bez drajvera")}", "pravo", Tekst.T("administrator"));
    }

    private void PostaviStanje(IIzvorSenzora izvor)
    {
        // Full monitoring has two keys, not one: administrator rights and the PawnIO driver. Only both together
        // give everything, so each state names the one that is missing instead of offering "potpuni nadzor".
        var (stanje, vrsta, prava) = OpisStanja(izvor.Pravi, izvor is PomocnikKlijent { JeUsluga: true },
            izvor is PomocnikKlijent, Drajver, izvor.Senzori.Count, Admin.JeAdmin);
        Stanje = stanje;
        VrstaStanja = vrsta;
        Prava = prava;

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

        // The game: the service and the helper count frames (Windows gives frame events to administrators); when
        // the window itself is the elevated one, it counts them (see PraviIzvor). Probe data play a made-up game
        // too, so the Scene can be seen (and pictured) without one.
        Igra.Postavi(_pomocnik is { } izvor ? _birac.Izaberi(izvor.Igre)
            : _kadrovi is { } kadrovi ? _birac.Izaberi(kadrovi.Stanje().ToArray())
            : _rad.ProbniPodaci ? ProbnaIgra(snimak.Vreme) : null);
        Sesija?.Uzmi(Igra, Kanali, sada);
        ProveriZdravlje();
        OsveziZdravlje();
    }

    private string? _prijavljenoZdravlje;

    private readonly Dictionary<string, Protokol.DiskZdravlje> _zdravljePoModelu = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A drive that says something is wrong gets the line at the bottom and one log entry - once per verdict, not
    /// every second. The window itself cannot read SMART (that needs administrator); the service or the helper
    /// reads it and sends the verdicts.
    /// </summary>
    private void ProveriZdravlje()
    {
        if (_pomocnik is not { } izvor) return;

        var diskovi = izvor.Diskovi;
        if (diskovi.Length == 0) return;

        _zdravljePoModelu.Clear();
        foreach (var disk in diskovi) _zdravljePoModelu[disk.Model] = disk;

        var najgore = diskovi
            .Where(d => d.Nivo >= (int)NivoZdravlja.Upozorenje)
            .OrderByDescending(d => d.Nivo)
            .FirstOrDefault();

        if (najgore is not null && najgore.Naslov != _prijavljenoZdravlje)
        {
            _prijavljenoZdravlje = najgore.Naslov;
            Upozorenje = $"{najgore.Model}: {najgore.Naslov} — {najgore.Detalj}";
            Dnevnik.Upisi($"Zdravlje diska: {najgore.Model}: {najgore.Naslov} - {najgore.Detalj}");
        }

        OsveziZdravlje();
    }

    /// <summary>
    /// Writes the verdict of the disk that is chosen in the solo panel - each disk has its own, and the group strip
    /// follows whichever one is picked. Cheap enough to run on every reading: the text only changes when it changes.
    /// </summary>
    private void OsveziZdravlje()
    {
        if (_zdravljePoModelu.Count == 0) return;

        foreach (var kanal in Kanali.Where(k => k.Kljuc.StartsWith(Grupa.Diskovi + "|", StringComparison.Ordinal)))
        {
            var nadjen = _zdravljePoModelu.FirstOrDefault(p => IstiDisk(kanal.Uredjaj, p.Key));
            if (nadjen.Value is not { } nalaz) continue;

            // A healthy drive shows why ("Zdrav · 99 % · 34 °C · SSD"); a problem shows just the verdict, because the
            // whole sentence is already in the line at the bottom and would not fit here.
            string tekst = nalaz.Nivo >= (int)NivoZdravlja.Upozorenje ? nalaz.Naslov : "Zdrav · " + nalaz.Detalj;
            kanal.PostaviZdravlje(tekst, nalaz.Nivo);
        }
    }

    /// <summary>The library trims model names differently from the toolkit; a trimmed, case-blind compare is enough.</summary>
    private static bool IstiDisk(string uredjaj, string model)
    {
        string a = uredjaj.Trim(), b = model.Trim();
        return a.Equals(b, StringComparison.OrdinalIgnoreCase) ||
               a.StartsWith(b, StringComparison.OrdinalIgnoreCase) ||
               b.StartsWith(a, StringComparison.OrdinalIgnoreCase);
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

        _prePodesavanja = _kanal ?? _prePodesavanja;
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

    /// <summary>
    /// Empties the window's own history and asks the service to empty its own. The service keeps its history in a
    /// folder the window may only read, so the request is a mark in the user's settings file, which the service
    /// reads every half minute. Returns what the settings screen says.
    /// </summary>
    private string ObrisiIstoriju()
    {
        bool lokalno = ObrisiLokalnuIstoriju();

        try
        {
            // Only the mark is written: whatever else is changed on the screen and not saved stays unsaved.
            long sada = DateTime.UtcNow.Ticks;
            var naDisku = Sistem.Podesavanja.Ucitaj();
            naDisku.ObrisiIstorijuZahtev = sada;
            naDisku.Sacuvaj();
            Podesavanja.ObrisiIstorijuZahtev = sada;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Zahtev usluzi za brisanje istorije", ex);
        }

        if (!lokalno) return "Brisanje nije uspelo, razlog je u greske.log.";
        return _pomocnik is { JeUsluga: true }
            ? "Istorija je obrisana. Usluga briše i svoju u roku od pola minuta."
            : "Istorija je obrisana.";
    }

    private bool ObrisiLokalnuIstoriju()
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

        _trazenje?.Stop();
        _trazenje = null;

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

        // The window counted frames itself (elevated, no service): its ETW session goes with it.
        try
        {
            _kadrovi?.Dispose();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Zatvaranje merenja kadrova", ex);
        }
        _kadrovi = null;
    }
}

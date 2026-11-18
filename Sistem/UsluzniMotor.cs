using System.IO.Pipes;
using System.Text;
using Takt.Merenje;
using Takt.Podaci;

namespace Takt.Sistem;

/// <summary>
/// The engine behind the service: one thread reads the hardware and keeps the history (it reads whether or not a
/// window is open, so the history fills while the program is closed), another serves the window over the pipe.
/// Two threads, not one: a window that never connects must not stop the readings, and a reading must not wait
/// for a window.
/// The user's choices (history on or off, how many days, how often to read, "delete the history") come from the
/// user's settings file, read again every half minute: the window cannot write to the service, so the service
/// follows the file.
/// </summary>
public sealed class UsluzniMotor : IDisposable
{
    /// <summary>How often the user's settings are read again.</summary>
    public static readonly TimeSpan ProveraPodesavanja = TimeSpan.FromSeconds(30);

    private readonly Func<Podesavanja> _ucitaj;
    private readonly bool _smeIstoriju;
    private readonly int _svakihUzoraka;
    private readonly string _bazaFajl;

    private readonly object _bravaSnimka = new();
    private readonly object _bravaBaze = new();
    private readonly object _bravaCevi = new();
    private readonly ManualResetEventSlim _kraj = new(false);

    private Podesavanja _podesavanja;
    private DateTime _procitano = DateTime.MinValue;
    private long _obrisanoDo;
    private volatile int _perioda;

    private IIzvorSenzora? _izvor;
    private MeracKadrova? _kadrovi;
    private IReadOnlyList<Senzor> _senzori = [];
    private Snimak? _poslednji;
    private Baza? _baza;
    private NamedPipeServerStream? _cev;
    private Thread? _uzimanje;
    private Thread? _sluzenje;
    private volatile bool _radi;
    private int _uzoraka;

    /// <summary>Fixed settings, for a test.</summary>
    public UsluzniMotor(Podesavanja podesavanja, bool pisiIstoriju = true, int svakihUzoraka = 60, string? baza = null)
        : this(() => podesavanja, pisiIstoriju, svakihUzoraka, baza)
    {
    }

    /// <param name="ucitaj">Reads the user's settings; called at start and every <see cref="ProveraPodesavanja"/>.</param>
    /// <param name="pisiIstoriju">False forbids history whatever the settings say (tests).</param>
    /// <param name="baza">The history file; by default <see cref="Putanje.Baza"/> (the service's own folder).</param>
    public UsluzniMotor(Func<Podesavanja> ucitaj, bool pisiIstoriju = true, int svakihUzoraka = 60, string? baza = null)
    {
        _ucitaj = ucitaj;
        _smeIstoriju = pisiIstoriju;
        _svakihUzoraka = Math.Max(1, svakihUzoraka);
        _bazaFajl = baza ?? Putanje.Baza;
        _podesavanja = UcitajBezbedno(new Podesavanja());

        // A request to delete that was already there when the service started is old: it does not delete again.
        _obrisanoDo = _podesavanja.ObrisiIstorijuZahtev;
        _perioda = Math.Max(1, _podesavanja.Osvezavanje) * 1000;
    }

    public bool Radi => _radi;

    /// <summary>The last trouble worth telling: the driver did not open, a database refused, a pipe failed.</summary>
    public string? Poruka { get; private set; }

    /// <summary>How many sensors the engine has; a test checks that the list reaches the window.</summary>
    public int BrojSenzora => _senzori.Count;

    /// <summary>True while the history file is open (the user's "Čuvaj istoriju" is on).</summary>
    public bool PiseIstoriju
    {
        get { lock (_bravaBaze) return _baza is not null; }
    }

    public void Pokreni()
    {
        if (_radi) return;

        _izvor = new SlozeniIzvor(new LhmIzvor(), new RamIzvor());
        _senzori = _izvor.Senzori;

        // What the hardware gave, per part, and the board's own sensors by name: a report like "no fans" can be
        // checked from the log, without running anything as administrator.
        Dnevnik.Upisi("Usluga: senzori - " + string.Join(", ", _senzori.GroupBy(s => s.Grupa)
            .Select(g => $"{GrupaOpis.Naslov(g.Key)} {g.Count()}")) + ".");
        var hladjenje = _senzori.Where(s => s.Grupa == Grupa.Hladjenje).Select(s => $"{s.Ime} ({s.Uredjaj})").ToList();
        Dnevnik.Upisi(hladjenje.Count == 0
            ? "Usluga: ploča ne daje nijedan senzor hlađenja."
            : "Usluga: hlađenje - " + string.Join(", ", hladjenje) + ".");
        Primeni(_podesavanja);
        _procitano = DateTime.UtcNow;

        // Frames per second of games: Windows' frame events, which only an administrator may read.
        _kadrovi = new MeracKadrova();
        _kadrovi.Pokreni();

        _radi = true;
        _uzimanje = new Thread(Uzimaj) { IsBackground = true, Name = "Takt uzimanje" };
        _sluzenje = new Thread(Sluzi) { IsBackground = true, Name = "Takt služenje" };
        _uzimanje.Start();
        _sluzenje.Start();
    }

    public void Zaustavi()
    {
        _radi = false;
        _kraj.Set();

        // A server blocked in WaitForConnection is released by closing its pipe.
        lock (_bravaCevi)
        {
            try { _cev?.Dispose(); } catch (Exception ex) { Dnevnik.Greska("Usluga: zatvaranje cevi", ex); }
            _cev = null;
        }

        bool uzimanjeStalo = _uzimanje?.Join(TimeSpan.FromSeconds(5)) ?? true;
        _sluzenje?.Join(TimeSpan.FromSeconds(5));
        _uzimanje = null;
        _sluzenje = null;

        // A reading still running (a disk waking up can take seconds) must not lose its source under it; the
        // process is ending anyway, and the library is closed with it.
        _kadrovi?.Dispose();
        _kadrovi = null;

        if (uzimanjeStalo)
        {
            _izvor?.Dispose();
            _izvor = null;
        }
        else
        {
            Dnevnik.Upisi("Usluga: čitanje se nije završilo za 5 s; izvor se ne zatvara ispod njega.");
        }
    }

    /// <summary>Reads the user's settings again and applies what changed. Public so a test can call it at once.</summary>
    public void OsveziPodesavanja()
    {
        _procitano = DateTime.UtcNow;
        Primeni(UcitajBezbedno(_podesavanja));
    }

    private Podesavanja UcitajBezbedno(Podesavanja dosadasnja)
    {
        try
        {
            return _ucitaj().Ispravno();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Usluga: čitanje podešavanja", ex);
            return dosadasnja;
        }
    }

    private void Primeni(Podesavanja novo)
    {
        _podesavanja = novo;
        _perioda = Math.Max(1, novo.Osvezavanje) * 1000;

        lock (_bravaBaze)
        {
            bool treba = _smeIstoriju && novo.Istorija;
            if (treba && _baza is null) OtvoriBazu();
            if (!treba && _baza is not null)
            {
                _baza.Dispose();
                _baza = null;
                Dnevnik.Upisi("Usluga: istorija je isključena u podešavanjima; upis staje.");
            }

            if (novo.ObrisiIstorijuZahtev > _obrisanoDo)
            {
                _obrisanoDo = novo.ObrisiIstorijuZahtev;
                ObrisiIstoriju();
            }
        }
    }

    /// <summary>The readings: one every period, into the history and into the shared "latest" for the window.</summary>
    private void Uzimaj()
    {
        while (_radi)
        {
            try
            {
                if (DateTime.UtcNow - _procitano >= ProveraPodesavanja) OsveziPodesavanja();

                var snimak = _izvor!.Uzmi();
                lock (_bravaSnimka) _poslednji = snimak;

                if (_izvor.Poruka is { } poruka) Poruka = poruka;
                Upisi(snimak);
            }
            catch (Exception ex)
            {
                Dnevnik.Greska("Usluga: uzimanje", ex);
            }

            if (_kraj.Wait(_perioda)) break;
        }
    }

    /// <summary>The window: list once, then whatever the reading thread last saw, every period.</summary>
    private void Sluzi()
    {
        while (_radi)
        {
            NamedPipeServerStream cev;
            try
            {
                cev = Cev.Napravi();
            }
            catch (Exception ex)
            {
                if (Poruka != ex.Message)
                {
                    Dnevnik.Greska("Usluga: cev", ex);
                    Poruka = "Cev za prozor se ne otvara: " + ex.Message;
                }

                if (_kraj.Wait(1000)) break;
                continue;
            }

            lock (_bravaCevi) _cev = cev;

            try
            {
                cev.WaitForConnection();      // blocks until a window comes, or until we close the pipe
                if (!_radi) break;
                Pisi(cev);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                // The window left, or we are shutting down: that is the normal way out of the inner loop.
            }
            finally
            {
                lock (_bravaCevi)
                {
                    if (ReferenceEquals(_cev, cev)) _cev = null;
                }
                cev.Dispose();
            }
        }
    }

    private void Pisi(NamedPipeServerStream cev)
    {
        using var pisac = new StreamWriter(cev, new UTF8Encoding(false)) { AutoFlush = true };
        pisac.WriteLine(Protokol.Spisak(_senzori, Protokol.Usluga));
        Dnevnik.Upisi($"Usluga: poslato {_senzori.Count} senzora, administrator: {Admin.JeAdmin}.");

        string? poslednjaGreska = null;

        while (_radi)
        {
            Snimak? snimak;
            lock (_bravaSnimka) snimak = _poslednji;
            if (snimak is not null) pisac.WriteLine(Protokol.Uzorak(snimak));
            if (_kadrovi is { Greska: null } kadrovi) pisac.WriteLine(Protokol.Igre(kadrovi.Stanje()));

            // Once, not every second: the window shows the last message it got anyway.
            if (Poruka is { } poruka && poruka != poslednjaGreska)
            {
                pisac.WriteLine(Protokol.Greska(poruka));
                poslednjaGreska = poruka;
            }

            if (_kraj.Wait(_perioda)) break;
        }
    }

    /// <summary>Under <see cref="_bravaBaze"/>.</summary>
    private void OtvoriBazu()
    {
        try
        {
            _baza = new Baza(_bazaFajl);
            _baza.UpisiSenzore(_senzori);
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Usluga: baza", ex);
            _baza?.Dispose();
            _baza = null;
            Poruka = "Istorija se ne čuva: " + ex.Message;
        }
    }

    private void Upisi(Snimak snimak)
    {
        lock (_bravaBaze)
        {
            if (_baza is null) return;

            try
            {
                if (++_uzoraka % _svakihUzoraka != 0) return;

                long vreme = new DateTimeOffset(snimak.Vreme.ToUniversalTime()).ToUnixTimeSeconds();
                var redovi = new List<(string Kljuc, double Vrednost)>(_senzori.Count);
                for (int i = 0; i < _senzori.Count && i < snimak.Vrednosti.Length; i++)
                    if (snimak.Vrednosti[i] is { } vrednost)
                        redovi.Add((_senzori[i].Kljuc.Tekst, vrednost));

                _baza.UpisiSve(vreme, redovi);
                _baza.OcistiPre(vreme - (long)_podesavanja.IstorijaDana * 86_400);
            }
            catch (Exception ex)
            {
                Dnevnik.Greska("Usluga: upis istorije", ex);
            }
        }
    }

    /// <summary>Under <see cref="_bravaBaze"/>. Empties the history whether or not it is being written now.</summary>
    private void ObrisiIstoriju()
    {
        try
        {
            if (_baza is not null)
            {
                _baza.ObrisiSve();
            }
            else if (File.Exists(_bazaFajl))
            {
                using var baza = new Baza(_bazaFajl);
                baza.ObrisiSve();
            }
            Dnevnik.Upisi("Usluga: istorija je obrisana na zahtev iz prozora.");
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Usluga: brisanje istorije", ex);
        }
    }

    public void Dispose()
    {
        Zaustavi();

        lock (_bravaBaze)
        {
            _baza?.Dispose();
            _baza = null;
        }

        _kraj.Dispose();
    }
}

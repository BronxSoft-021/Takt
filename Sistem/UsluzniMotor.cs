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
/// </summary>
public sealed class UsluzniMotor : IDisposable
{
    private readonly Podesavanja _podesavanja;
    private readonly bool _pisiIstoriju;
    private readonly int _svakihUzoraka;
    private readonly int _perioda;

    private readonly object _bravaSnimka = new();
    private readonly object _bravaBaze = new();
    private readonly object _bravaCevi = new();
    private readonly ManualResetEventSlim _kraj = new(false);

    private IIzvorSenzora? _izvor;
    private IReadOnlyList<Senzor> _senzori = [];
    private Snimak? _poslednji;
    private Baza? _baza;
    private NamedPipeServerStream? _cev;
    private Thread? _uzimanje;
    private Thread? _sluzenje;
    private volatile bool _radi;
    private int _uzoraka;

    public UsluzniMotor(Podesavanja podesavanja, bool pisiIstoriju = true, int svakihUzoraka = 60)
    {
        _podesavanja = podesavanja;
        _pisiIstoriju = pisiIstoriju;
        _svakihUzoraka = Math.Max(1, svakihUzoraka);
        _perioda = Math.Max(1, podesavanja.Osvezavanje) * 1000;
    }

    public bool Radi => _radi;

    /// <summary>The last trouble worth telling: the driver did not open, a database refused, a pipe failed.</summary>
    public string? Poruka { get; private set; }

    /// <summary>How many sensors the engine has; a test checks that the list reaches the window.</summary>
    public int BrojSenzora => _senzori.Count;

    public void Pokreni()
    {
        if (_radi) return;

        _izvor = new SlozeniIzvor(new LhmIzvor(), new RamIzvor());
        _senzori = _izvor.Senzori;
        OtvoriBazu();

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

        _uzimanje?.Join(TimeSpan.FromSeconds(5));
        _sluzenje?.Join(TimeSpan.FromSeconds(5));
        _uzimanje = null;
        _sluzenje = null;

        _izvor?.Dispose();
        _izvor = null;
    }

    /// <summary>The readings: one every period, into the history and into the shared "latest" for the window.</summary>
    private void Uzimaj()
    {
        while (_radi)
        {
            try
            {
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

            // Once, not every second: the window shows the last message it got anyway.
            if (Poruka is { } poruka && poruka != poslednjaGreska)
            {
                pisac.WriteLine(Protokol.Greska(poruka));
                poslednjaGreska = poruka;
            }

            if (_kraj.Wait(_perioda)) break;
        }
    }

    private void OtvoriBazu()
    {
        if (!_pisiIstoriju) return;

        lock (_bravaBaze)
        {
            try
            {
                _baza = new Baza(Putanje.Baza);
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
    }

    private void Upisi(Snimak snimak)
    {
        if (_baza is null) return;

        lock (_bravaBaze)
        {
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

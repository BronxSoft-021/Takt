using System.IO;
using Takt.Merenje;
using Takt.Podaci;

namespace Takt.Sistem;

/// <summary>
/// The clock of the whole program: reads the source once per tick, keeps the last hour in memory and writes
/// one row per minute to the database. Nothing here may throw into the window - a sensor that disappears
/// must not close the program.
/// </summary>
public sealed class Nadzor : IDisposable
{
    private readonly object _brava = new();
    private readonly object _bravaBaze = new();
    private readonly string[] _kljucevi;
    private Timer? _tajmer;
    private Baza? _baza;
    private int _brojUzoraka;
    private bool _radi;

    public Nadzor(IIzvorSenzora izvor, Podesavanja podesavanja, int kapacitet = 3600)
    {
        Izvor = izvor;
        Podesavanja = podesavanja;

        int broj = izvor.Senzori.Count;
        _kljucevi = new string[broj];
        Istorija = new Prsten[broj];
        Najmanje = new double?[broj];
        Najvise = new double?[broj];

        for (int i = 0; i < broj; i++)
        {
            _kljucevi[i] = izvor.Senzori[i].Kljuc.Tekst;
            Istorija[i] = new Prsten(kapacitet);
        }

        if (podesavanja.Istorija) OtvoriBazu();
    }

    public IIzvorSenzora Izvor { get; }
    public Podesavanja Podesavanja { get; }
    public IReadOnlyList<Senzor> Senzori => Izvor.Senzori;
    public Prsten[] Istorija { get; }
    public double?[] Najmanje { get; }
    public double?[] Najvise { get; }
    public Snimak? Poslednji { get; private set; }

    public event Action<Snimak>? NoviUzorak;
    public event Action<string>? Poruka;

    public void Pokreni()
    {
        if (_radi) return;
        _radi = true;
        int perioda = Math.Max(1, Podesavanja.Osvezavanje) * 1000;
        _tajmer = new Timer(_ => Tik(), null, 0, perioda);
    }

    public void Zaustavi()
    {
        _radi = false;
        _tajmer?.Dispose();
        _tajmer = null;
    }

    /// <summary>New time between readings, from the next tick on; no restart, the rings keep what they have.</summary>
    public void PromeniPeriodu(int sekundi)
    {
        if (!_radi) return;
        int perioda = Math.Max(1, sekundi) * 1000;
        _tajmer?.Change(perioda, perioda);
    }

    private int _uzimam;

    /// <summary>The timer's tick. A slow read (a disk waking up) must not start a second one on top of it.</summary>
    private void Tik()
    {
        if (Interlocked.Exchange(ref _uzimam, 1) == 1) return;
        try
        {
            UzmiJednom();
        }
        finally
        {
            Interlocked.Exchange(ref _uzimam, 0);
        }
    }

    /// <summary>One reading. Public so a test can take three in a row without waiting.</summary>
    public Snimak UzmiJednom()
    {
        try
        {
            var snimak = Izvor.Uzmi();

            // The rings are held under one short lock. The database write happens outside it, so the window
            // never waits for a disk, a virus scan or a file lock.
            Snimak? zaUpis = null;
            int dana = Podesavanja.IstorijaDana;

            lock (_brava)
            {
                Poslednji = snimak;

                for (int i = 0; i < Istorija.Length && i < snimak.Vrednosti.Length; i++)
                {
                    var vrednost = snimak.Vrednosti[i];
                    Istorija[i].Dodaj(vrednost);

                    if (vrednost is not { } broj) continue;
                    if (Najmanje[i] is not { } najmanje || broj < najmanje) Najmanje[i] = broj;
                    if (Najvise[i] is not { } najvise || broj > najvise) Najvise[i] = broj;
                }

                _brojUzoraka++;
                if (_baza is not null && _brojUzoraka % 60 == 0) zaUpis = snimak;
            }

            if (zaUpis is not null) Upisi(zaUpis, dana);

            NoviUzorak?.Invoke(snimak);
            return snimak;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Uzorkovanje", ex);
            Poruka?.Invoke("Greška pri čitanju senzora: " + ex.Message);
            return Poslednji ?? new Snimak(DateTime.Now, new double?[Istorija.Length]);
        }
    }

    /// <summary>A consistent copy for the window: the last reading and the smallest/largest since start.</summary>
    public (Snimak? Snimak, double?[] Najmanje, double?[] Najvise) Stanje()
    {
        lock (_brava)
        {
            return (Poslednji, [.. Najmanje], [.. Najvise]);
        }
    }

    /// <summary>
    /// Empties the history. With history switched off the database is not open, but the file from earlier
    /// runs is still on disk - it is opened just for this, so "Obriši istoriju" means what it says.
    /// </summary>
    public bool ObrisiIstoriju()
    {
        lock (_bravaBaze)
        {
            try
            {
                if (_baza is not null)
                {
                    _baza.ObrisiSve();
                }
                else if (File.Exists(Putanje.Baza))
                {
                    using var baza = new Baza(Putanje.Baza);
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
    }

    private void OtvoriBazu()
    {
        lock (_bravaBaze)
        {
            try
            {
                _baza = new Baza(Putanje.Baza);
                _baza.UpisiSenzore(Senzori);
                // A leftover of an older build (one lonely sample from a sensor we no longer read) goes once per run,
                // here rather than in Baza's constructor: a test that opens a database must not lose its own rows.
                _baza.ObrisiMrtve(Senzori.Select(s => s.Kljuc.Tekst).ToList());
            }
            catch (Exception ex)
            {
                Dnevnik.Greska("Otvaranje baze", ex);
                _baza?.Dispose();
                _baza = null;
                Poruka?.Invoke("Istorija se ne čuva: " + ex.Message);
            }
        }
    }

    private void Upisi(Snimak snimak, int dana)
    {
        lock (_bravaBaze)
        {
            if (_baza is null) return;

            try
            {
                long vreme = new DateTimeOffset(snimak.Vreme.ToUniversalTime()).ToUnixTimeSeconds();
                var redovi = new List<(string Kljuc, double Vrednost)>(_kljucevi.Length);

                for (int i = 0; i < _kljucevi.Length && i < snimak.Vrednosti.Length; i++)
                    if (snimak.Vrednosti[i] is { } vrednost)
                        redovi.Add((_kljucevi[i], vrednost));

                _baza.UpisiSve(vreme, redovi);
                _baza.OcistiPre(vreme - (long)dana * 86_400);
            }
            catch (Exception ex)
            {
                Dnevnik.Greska("Upis istorije", ex);
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

        Izvor.Dispose();
    }
}

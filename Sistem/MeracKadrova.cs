using System.Diagnostics;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace Takt.Sistem;

/// <summary>
/// Frames per second of every program that draws, from Windows' own events (ETW) - the same events PresentMon
/// reads, and like it nothing is put into the game: no injection, no overlay, so anti-cheat has nothing to see.
/// Each "Present" of DXGI (DirectX 10-12, also Vulkan/OpenGL games presented through DXGI) and of Direct3D 9 is
/// one frame. Needs administrator, so the service and the helper run it, never the window.
/// </summary>
public sealed class MeracKadrova : IDisposable
{
    /// <summary>Microsoft-Windows-DXGI; event 42 = Present start, 55 = PresentMultiplaneOverlay start.</summary>
    public static readonly Guid Dxgi = new("CA11C036-0102-4A2D-A6AD-F03CFED5D3C9");

    /// <summary>Microsoft-Windows-D3D9; event 1 = Present start (older games and emulators).</summary>
    public static readonly Guid D3d9 = new("783ACA0A-790E-4D7F-8451-AA850511C6B9");

    private const string ImeSesije = "BronxSoftTakt-kadrovi";

    private readonly object _brava = new();
    private readonly Dictionary<int, Proces> _procesi = [];
    private TraceEventSession? _sesija;
    private Thread? _nit;

    /// <summary>Why measuring does not run (no administrator, Windows refused), or null.</summary>
    public string? Greska { get; private set; }

    private sealed class Proces
    {
        public readonly Queue<double> Kadrovi = new();      // time of each frame, ms, last ~10 s
        public string Ime = "";
        public string? Putanja;
    }

    public void Pokreni()
    {
        try
        {
            // A session left over from a crashed run keeps the name: close it first.
            try { TraceEventSession.GetActiveSession(ImeSesije)?.Dispose(); } catch { }

            _sesija = new TraceEventSession(ImeSesije) { StopOnDispose = true };
            _sesija.EnableProvider(Dxgi, TraceEventLevel.Informational, ulong.MaxValue);
            _sesija.EnableProvider(D3d9, TraceEventLevel.Informational, ulong.MaxValue);
            _sesija.Source.AllEvents += NaDogadjaj;

            _nit = new Thread(() =>
            {
                try { _sesija.Source.Process(); }
                catch (Exception ex) { Dnevnik.Greska("Kadrovi: čitanje događaja", ex); }
            })
            { IsBackground = true, Name = "Takt kadrovi" };
            _nit.Start();
            Dnevnik.Upisi("Kadrovi: merenje FPS-a radi (ETW, DXGI i D3D9).");
        }
        catch (Exception ex)
        {
            Greska = "FPS se ne meri: " + ex.Message;
            Dnevnik.Greska("Kadrovi: pokretanje", ex);
            _sesija?.Dispose();
            _sesija = null;
        }
    }

    private long _svih;
    private readonly HashSet<string> _vrste = [];

    private void NaDogadjaj(TraceEvent dogadjaj)
    {
        int id = (int)dogadjaj.ID;

        // Which kinds of events arrive at all (first twenty, once): if frames are never counted, the log says
        // whether Windows sends nothing or sends them under other numbers.
        Interlocked.Increment(ref _svih);
        string vrsta = (dogadjaj.ProviderGuid == Dxgi ? "DXGI" : "D3D9") + ":" + id;
        lock (_vrste)
            if (_vrste.Count < 20 && _vrste.Add(vrsta))
                Dnevnik.Upisi($"Kadrovi: prvi događaj {vrsta} ({dogadjaj.EventName}) od procesa {dogadjaj.ProcessID}.");
        bool kadar = (dogadjaj.ProviderGuid == Dxgi && id is 42 or 55) || (dogadjaj.ProviderGuid == D3d9 && id == 1);
        if (!kadar) return;

        Zabelezi(dogadjaj.ProcessID, dogadjaj.TimeStampRelativeMSec);
    }

    // The event clock against the wall clock: the newest event time and when it arrived. Windows hands real-time
    // events over in buffers, about a second late, so "now" is worked out from the events themselves.
    private double _poslednjiDogadjaj;
    private readonly Stopwatch _odPoslednjeg = new();
    private long _dogadjaja;
    private DateTime _brojanoOd = DateTime.UtcNow;

    /// <summary>One frame of one program. Public for a test, which feeds made-up frames.</summary>
    public void Zabelezi(int pid, double ms)
    {
        lock (_brava)
        {
            if (!_procesi.TryGetValue(pid, out var proces))
                _procesi[pid] = proces = new Proces();
            proces.Kadrovi.Enqueue(ms);
            while (proces.Kadrovi.Count > 0 && ms - proces.Kadrovi.Peek() > 10_000) proces.Kadrovi.Dequeue();

            _dogadjaja++;
            if (ms >= _poslednjiDogadjaj)
            {
                _poslednjiDogadjaj = ms;
                _odPoslednjeg.Restart();
            }
        }
    }

    /// <summary>
    /// The last second of every program that drew, busiest first (at most eight). Each program is measured over
    /// the second before its own newest frame (events arrive in batches, so the newest second may be half
    /// delivered), and is dropped when it has drawn nothing for 2,5 s. <paramref name="sada"/> is the event clock
    /// (ms) to measure against; by default it is worked out from the newest event.
    /// </summary>
    public List<Protokol.Igra> Stanje(double? sada = null)
    {
        var rezultat = new List<Protokol.Igra>();
        lock (_brava)
        {
            double sat = sada ?? (_poslednjiDogadjaj + _odPoslednjeg.Elapsed.TotalMilliseconds);

            foreach (var (pid, proces) in _procesi.ToList())
            {
                while (proces.Kadrovi.Count > 0 && sat - proces.Kadrovi.Peek() > 11_000) proces.Kadrovi.Dequeue();
                if (proces.Kadrovi.Count == 0) { _procesi.Remove(pid); continue; }

                var kadrovi = proces.Kadrovi.ToArray();
                double kraj = kadrovi[^1];
                if (sat - kraj > 2500) continue;                      // stopped drawing
                var sekunda = kadrovi.Where(t => kraj - t < 1000).ToArray();
                if (sekunda.Length == 0) continue;

                if (proces.Ime.Length == 0) (proces.Ime, proces.Putanja) = Ime(pid);
                rezultat.Add(Izracunaj(pid, proces.Ime, proces.Putanja, kadrovi, sekunda));
            }
        }

        // Once per program and run, a line in the log: proof that frames arrive, without a game at hand.
        foreach (var igra in rezultat.Where(i => i.Fps >= 20))
            if (_zapisani.Add(igra.Ime))
                Dnevnik.Upisi($"Kadrovi: {igra.Ime} crta {igra.Fps:0} FPS.");

        // And for the first ten minutes, how many frame events arrived per minute (zero means Windows sends none).
        if (DateTime.UtcNow - _brojanoOd >= TimeSpan.FromMinutes(1) && _brojanja < 10)
        {
            long n;
            lock (_brava) { n = _dogadjaja; _dogadjaja = 0; }
            Dnevnik.Upisi($"Kadrovi: {n} kadrova u poslednjem minutu ({Interlocked.Exchange(ref _svih, 0)} događaja ukupno), " +
                          $"{rezultat.Count} programa sada crta.");
            _brojanoOd = DateTime.UtcNow;
            _brojanja++;
        }

        return rezultat.OrderByDescending(i => i.Fps).Take(8).ToList();
    }

    private readonly HashSet<string> _zapisani = new(StringComparer.OrdinalIgnoreCase);
    private int _brojanja;

    /// <summary>
    /// The numbers of one program: FPS of the last second, average and longest frame in it, the 1 % low over the
    /// last ten seconds (the frame time only 1 % of frames were slower than, as FPS), and stutters - frames more
    /// than 2,5 times the median and longer than 50 ms. Public for a test.
    /// </summary>
    public static Protokol.Igra Izracunaj(int pid, string ime, string? putanja, double[] kadrovi, double[] sekunda)
    {
        var razmaci = new List<double>(kadrovi.Length);
        for (int i = 1; i < kadrovi.Length; i++) razmaci.Add(kadrovi[i] - kadrovi[i - 1]);

        var razmaciSekunde = new List<double>(sekunda.Length);
        int prvi = Array.IndexOf(kadrovi, sekunda[0]);
        for (int i = Math.Max(1, prvi); i < kadrovi.Length; i++) razmaciSekunde.Add(kadrovi[i] - kadrovi[i - 1]);

        double prosek = razmaciSekunde.Count > 0 ? razmaciSekunde.Average() : 0;
        double najduzi = razmaciSekunde.Count > 0 ? razmaciSekunde.Max() : 0;

        double low1 = 0;
        int stucanja = 0;
        if (razmaci.Count >= 10)
        {
            var sortirano = razmaci.OrderBy(r => r).ToArray();
            double p99 = sortirano[(int)Math.Min(sortirano.Length - 1, Math.Ceiling(sortirano.Length * 0.99) - 1)];
            low1 = p99 > 0 ? 1000 / p99 : 0;

            double medijana = sortirano[sortirano.Length / 2];
            stucanja = razmaciSekunde.Count(r => r > 50 && r > medijana * 2.5);
        }

        return new Protokol.Igra(pid, ime, putanja, sekunda.Length, Math.Round(prosek, 2), Math.Round(najduzi, 2),
            Math.Round(low1, 1), stucanja);
    }

    private static (string Ime, string? Putanja) Ime(int pid)
    {
        try
        {
            using var proces = Process.GetProcessById(pid);
            string? putanja = null;
            try { putanja = proces.MainModule?.FileName; } catch { /* protected or 32-bit: the name is enough */ }
            return (proces.ProcessName, putanja);
        }
        catch
        {
            return ($"proces {pid}", null);
        }
    }

    public void Dispose()
    {
        try
        {
            _sesija?.Dispose();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Kadrovi: zatvaranje", ex);
        }
        _sesija = null;
        _nit?.Join(TimeSpan.FromSeconds(2));
        _nit = null;
    }
}

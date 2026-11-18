namespace Takt.Sistem;

/// <summary>
/// Only one Takt per user session. Since X hides the window to the tray, a double click on the desktop icon would
/// otherwise start a second copy: two icons by the clock and, without the service, two drivers reading the same
/// hardware and the same history written twice. The second copy asks the first to show its window and leaves.
/// </summary>
public sealed class JedanPrimerak : IDisposable
{
    private readonly Mutex _brava;
    private readonly EventWaitHandle _poziv;
    private readonly bool _prvi;
    private RegisteredWaitHandle? _cekanje;

    /// <param name="ime">Name of the lock; a test passes its own so it never meets the real program.</param>
    public JedanPrimerak(string ime = "BronxSoftTakt")
    {
        _brava = new Mutex(initiallyOwned: true, $@"Local\{ime}-prozor", out _prvi);
        _poziv = new EventWaitHandle(false, EventResetMode.AutoReset, $@"Local\{ime}-otvori");
    }

    /// <summary>True for the copy that runs; false for one that should ask it to open and quit.</summary>
    public bool Prvi => _prvi;

    /// <summary>The second copy: "show your window".</summary>
    public void JaviPrvom() => _poziv.Set();

    /// <summary>The first copy: what to do when another copy calls (on a pool thread; the caller marshals to the UI).</summary>
    public void Slusaj(Action poziv) =>
        _cekanje = ThreadPool.RegisterWaitForSingleObject(_poziv, (_, _) => poziv(), null, Timeout.Infinite,
            executeOnlyOnce: false);

    public void Dispose()
    {
        _cekanje?.Unregister(null);
        _poziv.Dispose();

        if (_prvi)
        {
            try { _brava.ReleaseMutex(); }
            catch (ApplicationException) { } // released from another thread at exit: Windows frees it with the process
        }
        _brava.Dispose();
    }
}

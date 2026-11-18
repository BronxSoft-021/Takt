using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// Where readings come from. Two implementations: the real hardware (LibreHardwareMonitor) and made-up
/// readings for screenshots and for a computer where the driver is not available.
/// </summary>
public interface IIzvorSenzora : IDisposable
{
    /// <summary>Shown in the header: "Pravi senzori" is not the same as "Probni podaci".</summary>
    string Naziv { get; }

    bool Pravi { get; }

    /// <summary>Fixed list, built once when the source is opened. The order never changes while the program runs.</summary>
    IReadOnlyList<Senzor> Senzori { get; }

    /// <summary>Why nothing was read, if something went wrong. Null when it works.</summary>
    string? Poruka { get; }

    Snimak Uzmi();
}

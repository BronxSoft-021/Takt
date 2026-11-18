using Microsoft.Win32;

namespace Takt.Sistem;

/// <summary>
/// Is the PawnIO driver installed? LibreHardwareMonitor 0.9.6 reads the processor's temperature, clocks, power
/// and voltage, the board's fans and the drives' SMART only through it; without it even an administrator sees
/// just load, the graphics card (through AMD's own interface) and what Windows itself reports. The driver is a
/// separate, free download from pawnio.eu and is not ours to ship, so the program says that instead of asking
/// for administrator rights again - which would not help.
/// </summary>
public static class PawnIo
{
    /// <summary>Where the driver comes from. Only ever written on screen; the program opens nothing by itself.</summary>
    public const string Adresa = "pawnio.eu";

    private static bool? _jeTu;

    /// <summary>Checked once per run: a driver cannot be installed in the middle of a reading.</summary>
    public static bool JeTu => _jeTu ??= Proveri(Environment.SystemDirectory, File.Exists, KljucPostoji);

    /// <summary>
    /// The part worth testing, without touching the real machine: the driver's file, the service the driver
    /// installs, or its entry in the list of installed programs (both registry views, because the installer may
    /// be 32-bit). The service key is the surest of the three and does not depend on how the installer names
    /// its entry.
    /// </summary>
    public static bool Proveri(string sistemski, Func<string, bool> postojiFajl, Func<string, bool> postojiKljuc) =>
        postojiFajl(Path.Combine(sistemski, "drivers", "PawnIO.sys")) ||
        postojiKljuc(@"SYSTEM\CurrentControlSet\Services\PawnIO") ||
        postojiKljuc(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO") ||
        postojiKljuc(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");

    /// <summary>Forgets the answer, for a test or after the driver is installed while the program runs.</summary>
    public static void Zaboravi() => _jeTu = null;

    /// <summary>
    /// Opens the driver's page in the user's browser and says whether it worked. Only ever called from a click;
    /// the program itself still sends nothing anywhere.
    /// </summary>
    public static bool Otvori()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://" + Adresa)
            {
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Otvaranje " + Adresa, ex);
            return false;
        }
    }

    private static bool KljucPostoji(string putanja)
    {
        try
        {
            using var kljuc = Registry.LocalMachine.OpenSubKey(putanja);
            return kljuc is not null;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Provera PawnIO u registru", ex);
            return false;
        }
    }
}

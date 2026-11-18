using System.Diagnostics;
using Microsoft.Win32;

namespace Takt.Sistem;

/// <summary>
/// Starting with Windows, for this user only (HKCU, no administrator): one value under Run, pointing at
/// <c>Takt.exe --u-traci</c>, so the program comes up in the tray without a window.
/// </summary>
public static class Pokretanje
{
    private const string Kljuc = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string Ime = "BronxSoft Takt";

    /// <summary>What the Run value must say: the quoted program and the switch that keeps it in the tray.</summary>
    public static string Linija(string putanja) => $"\"{putanja}\" --u-traci";

    public static bool Ukljuci()
    {
        try
        {
            var putanja = Environment.ProcessPath;
            if (string.IsNullOrEmpty(putanja)) return false;

            using var kljuc = Registry.CurrentUser.CreateSubKey(Kljuc);
            kljuc?.SetValue(Ime, Linija(putanja));
            return true;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Pokretanje sa Windowsom", ex);
            return false;
        }
    }

    public static bool Iskljuci()
    {
        try
        {
            using var kljuc = Registry.CurrentUser.OpenSubKey(Kljuc, writable: true);
            kljuc?.DeleteValue(Ime, throwOnMissingValue: false);
            return true;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Isključivanje pokretanja", ex);
            return false;
        }
    }

    /// <summary>True when the Run value exists and still points at this program.</summary>
    public static bool Radi()
    {
        try
        {
            using var kljuc = Registry.CurrentUser.OpenSubKey(Kljuc);
            var upisano = kljuc?.GetValue(Ime) as string;
            var putanja = Environment.ProcessPath;
            if (upisano is null || putanja is null) return false;

            return upisano.Contains(putanja, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Provera pokretanja sa Windowsom", ex);
            return false;
        }
    }
}

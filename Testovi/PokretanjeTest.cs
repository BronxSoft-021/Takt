using Microsoft.Win32;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>
/// The Windows Run value, for real: written, read back and removed - and then put back exactly as it was.
/// The user may have "start with Windows" on; a test run must never quietly turn it off.
/// </summary>
public static class PokretanjeTest
{
    private const string Kljuc = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static Task UpisiProcitajObrisi()
    {
        string? zateceno;
        using (var kljuc = Registry.CurrentUser.OpenSubKey(Kljuc))
            zateceno = kljuc?.GetValue(Pokretanje.Ime) as string;

        try
        {
            Proveri.Da(Pokretanje.Ukljuci(), "upis u HKCU Run uspeva bez administratora");
            Proveri.Da(Pokretanje.Radi(), "upisano pokretanje se prepoznaje kao ovo program");

            using (var kljuc = Registry.CurrentUser.OpenSubKey(Kljuc))
            {
                var upisano = kljuc?.GetValue(Pokretanje.Ime) as string;
                Proveri.Da(upisano is not null && upisano.EndsWith("--u-traci", StringComparison.Ordinal),
                    "u traci, bez prozora");
                Proveri.Da(upisano is not null && upisano.StartsWith("\"", StringComparison.Ordinal),
                    "putanja pod navodnicima (razmaci u putanji ne lome liniju)");
            }

            Proveri.Da(Pokretanje.Iskljuci(), "brisanje upisa uspeva");
            Proveri.Da(!Pokretanje.Radi(), "posle brisanja se ne pokreće sa Windowsom");
        }
        finally
        {
            // Back to what the user had: their own entry (pointing at their Takt), or nothing.
            using var kljuc = Registry.CurrentUser.CreateSubKey(Kljuc);
            if (zateceno is null) kljuc.DeleteValue(Pokretanje.Ime, throwOnMissingValue: false);
            else kljuc.SetValue(Pokretanje.Ime, zateceno);
        }

        using (var kljuc = Registry.CurrentUser.OpenSubKey(Kljuc))
            Proveri.Jednako(zateceno, kljuc?.GetValue(Pokretanje.Ime) as string, "upis je vraćen kakav je bio");

        return Task.CompletedTask;
    }
}

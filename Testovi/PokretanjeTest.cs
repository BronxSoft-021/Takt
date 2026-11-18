using Microsoft.Win32;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>
/// The Windows Run value, for real: written, read back and removed. The test leaves the user's Run key exactly
/// as it found it (no Takt entry), because it deletes what it wrote.
/// </summary>
public static class PokretanjeTest
{
    public static Task UpisiProcitajObrisi()
    {
        try
        {
            Proveri.Da(Pokretanje.Ukljuci(), "upis u HKCU Run uspeva bez administratora");
            Proveri.Da(Pokretanje.Radi(), "upisano pokretanje se prepoznaje kao ovo program");

            using (var kljuc = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                var upisano = kljuc?.GetValue(Pokretanje.Ime) as string;
                Proveri.Da(upisano is not null && upisano.EndsWith("--u-traci", StringComparison.Ordinal),
                    "u traci, bez prozora");
                Proveri.Da(upisano is not null && upisano.StartsWith("\"", StringComparison.Ordinal),
                    "putanja pod navodnicima (razmaci u putanji ne lome liniju)");
            }
        }
        finally
        {
            Proveri.Da(Pokretanje.Iskljuci(), "brisanje upisa uspeva");
        }

        Proveri.Da(!Pokretanje.Radi(), "posle brisanja se ne pokreće sa Windowsom");
        return Task.CompletedTask;
    }
}

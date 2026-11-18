using System.Diagnostics;
using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>
/// End to end through the pipe: start the helper the way the window starts it (without the UAC question here),
/// connect as the window does, and check that a reading arrives. The driver part is not tested - that needs
/// a real administrator start.
/// </summary>
public static class PomocnikTest
{
    public static async Task Cev()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "Takt.exe");
        Proveri.Da(File.Exists(exe), "Takt.exe nije u folderu testova");

        using var pomocnik = Process.Start(new ProcessStartInfo(exe)
        {
            Arguments = $"--pomocnik {Environment.ProcessId}",
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        Proveri.Da(pomocnik is not null, "pomoćnik nije pokrenut");

        try
        {
            PomocnikKlijent? klijent = null;
            for (int i = 0; i < 30 && klijent is null; i++)
            {
                klijent = PomocnikKlijent.Probaj(500);
                if (klijent is null) await Task.Delay(150);
            }

            Proveri.Da(klijent is not null, "klijent se nije povezao na cev pomoćnika");

            using (klijent)
            {
                Proveri.Da(klijent!.Senzori.Count > 0, "pomoćnik nije poslao spisak senzora");
                Proveri.Da(klijent.Naziv.Contains("pomoćnik", StringComparison.Ordinal), "izvor se predstavlja kao pomoćnik");

                Snimak? snimak = null;
                for (int i = 0; i < 60 && snimak is null; i++)
                {
                    var proba = klijent.Uzmi();
                    if (proba.Vrednosti.Any(v => v is not null)) snimak = proba;
                    else await Task.Delay(100);
                }

                Proveri.Da(snimak is not null, "nije stigao nijedan uzorak kroz cev");
                Proveri.Jednako(klijent.Senzori.Count, snimak!.Vrednosti.Length,
                    "uzorak ima vrednost za svaki senzor iz spiska");
            }

            // Closing the pipe must stop the helper; it exists for this window only.
            for (int i = 0; i < 40 && !pomocnik!.HasExited; i++) await Task.Delay(100);
            Proveri.Da(pomocnik!.HasExited, "pomoćnik je ostao da radi posle zatvaranja cevi");
        }
        finally
        {
            try
            {
                if (pomocnik is { HasExited: false }) pomocnik.Kill();
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                Dnevnik.Greska("Gašenje pomoćnika u testu", ex);
            }
        }
    }
}

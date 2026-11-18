using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// The helper: a second copy of this program, started as administrator, that reads the sensors the driver
/// needs and sends them over a named pipe. The window stays an ordinary user; the helper is the only thing
/// that is elevated, and only while the window is open (it stops when the pipe closes).
/// </summary>
public static class Pomocnik
{
    /// <summary>Asks Windows for administrator rights and starts <c>Takt.exe --pomocnik &lt;our pid&gt;</c>.</summary>
    public static (bool Uspeh, string Poruka) Pokreni()
    {
        try
        {
            var putanja = Environment.ProcessPath;
            if (string.IsNullOrEmpty(putanja))
                return (false, "Pomoćnik ne može da se pokrene: putanja programa nije poznata.");

            Process.Start(new ProcessStartInfo(putanja)
            {
                Arguments = $"--pomocnik {Environment.ProcessId}",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            });

            return (true, "Pomoćnik se pokreće. Ako Windows pita za dozvolu, pritisni „Da“.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED: the user said no to the UAC question. Not an error, just no full monitoring.
            return (false, "Pokretanje je otkazano — program ostaje na čitanju bez administratora.");
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Pokretanje pomoćnika", ex);
            return (false, "Pomoćnik nije pokrenut: " + ex.Message);
        }
    }

    /// <summary>
    /// The helper's loop: open the sensors, wait for the window, send the sensor list once and a reading every
    /// second. Returns when the window closes, when the parent program disappears, or when nobody connects.
    /// </summary>
    public static int Radi(int? roditelj)
    {
        try
        {
            using var cev = Cev.Napravi();

            // Nobody connected within half a minute: the program that asked for us is gone.
            if (!cev.WaitForConnectionAsync().Wait(TimeSpan.FromSeconds(30)))
            {
                Dnevnik.Upisi("Pomoćnik: niko se nije povezao, zatvaram se.");
                return 2;
            }

            using var pisac = new StreamWriter(cev, new UTF8Encoding(false)) { AutoFlush = true };
            using var izvor = new SlozeniIzvor(new LhmIzvor(), new RamIzvor());

            pisac.WriteLine(Protokol.Spisak(izvor.Senzori));
            Dnevnik.Upisi($"Pomoćnik: poslato {izvor.Senzori.Count} senzora, administrator: {Admin.JeAdmin}.");

            string? poslednjaGreska = null;

            while (true)
            {
                if (roditelj is { } pid && !Ziv(pid)) return 0;

                var snimak = izvor.Uzmi();
                pisac.WriteLine(Protokol.Uzorak(snimak));

                // Once, not every second: the window shows the last message it got anyway.
                if (izvor.Poruka is { } poruka && poruka != poslednjaGreska)
                {
                    pisac.WriteLine(Protokol.Greska(poruka));
                    poslednjaGreska = poruka;
                }

                Thread.Sleep(1000);
            }
        }
        catch (IOException)
        {
            // The window closed the pipe; that is the normal way to stop.
            return 0;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Pomoćnik", ex);
            return 1;
        }
    }

    private static bool Ziv(int pid)
    {
        try
        {
            using var proces = Process.GetProcessById(pid);
            return !proces.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

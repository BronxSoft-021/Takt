using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>Prints each drive's verdict as the program would show it: <c>Testovi --zdravlje</c>.</summary>
public static class ZdravljeIspis
{
    public static int Pokreni()
    {
        var redovi = new List<string>();
        var diskovi = Smart.Procitaj();

        if (diskovi.Count == 0)
        {
            redovi.Add("Nijedan disk nije odgovorio - pokreni kao administrator.");
        }
        else
        {
            foreach (var disk in diskovi)
            {
                var nalaz = Zdravlje.Proveri(disk);
                redovi.Add($"{nalaz.Nivo,-10} {disk.Model}");
                redovi.Add($"           {nalaz.Naslov} - {nalaz.Detalj}");
            }
        }

        // Printed and written down: an elevated run has no console to read from.
        foreach (var red in redovi) Console.WriteLine(red);

        try
        {
            File.WriteAllLines(Path.Combine(Path.GetTempPath(), "takt-zdravlje.txt"), redovi);
        }
        catch (IOException)
        {
            // The console output is enough.
        }

        return diskovi.Count == 0 ? 1 : 0;
    }
}

using System.IO;

namespace Takt;

/// <summary>Error log. Must never throw: a failed log write cannot bring the program down.</summary>
public static class Dnevnik
{
    private static readonly object Brava = new();
    private const long NajvecaVelicina = 2 * 1024 * 1024;

    public static void Greska(string gde, Exception? ex = null) =>
        Upisi(ex == null ? gde : $"{gde}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");

    public static void Upisi(string poruka)
    {
        try
        {
            lock (Brava)
            {
                var fajl = new FileInfo(Putanje.Greske);
                if (fajl.Exists && fajl.Length > NajvecaVelicina)
                    File.Move(fajl.FullName, fajl.FullName + ".staro", true);
                File.AppendAllText(Putanje.Greske, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {poruka}\n");
            }
        }
        catch { }
    }
}

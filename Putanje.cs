using System.IO;

namespace Takt;

/// <summary>Where the program keeps its data. TAKT_PODACI overrides it (tests must not touch the real history).</summary>
public static class Putanje
{
    public static string Podaci { get; } = Odredi();

    public static string Baza => Path.Combine(Podaci, "takt.db");
    public static string Podesavanja => Path.Combine(Podaci, "podesavanja.json");
    public static string Greske => Path.Combine(Podaci, "greske.log");

    private static string Odredi()
    {
        var zadato = Environment.GetEnvironmentVariable("TAKT_PODACI");
        if (!string.IsNullOrWhiteSpace(zadato))
        {
            zadato = Path.GetFullPath(zadato);
            Directory.CreateDirectory(zadato);
            return zadato;
        }

        var koren = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var putanja = Path.Combine(koren, "BronxSoft Takt");
        Directory.CreateDirectory(putanja);
        return putanja;
    }
}

using Takt.Podaci;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>
/// Moves a history folder onto the stable sensor keys without starting the service: it opens the database the
/// way the service does on start and records the sensors, which is what moves the old name-keyed rows over.
/// Meant for a copy of a real folder (or a folder whose service is stopped):
/// <c>Testovi --prebaci &lt;folder&gt;</c>. Run as administrator for the full sensor list; without it only the
/// sensors that need no administrator are moved.
/// </summary>
public static class Prebacivanje
{
    public static int Pokreni(string folder)
    {
        Environment.SetEnvironmentVariable("TAKT_PODACI", folder);

        using var izvor = new SlozeniIzvor(new LhmIzvor(), new RamIzvor());
        using var baza = new Baza(Path.Combine(folder, "takt.db"));

        int senzoraPre = baza.BrojSenzora();
        int redovaPre = baza.BrojRedova();

        baza.UpisiSenzore(izvor.Senzori);

        Console.WriteLine($"senzori: {senzoraPre} -> {baza.BrojSenzora()}, uzorci: {redovaPre} -> {baza.BrojRedova()}," +
                          $" sa stabilnim ključem: {izvor.Senzori.Count(s => s.Kljuc.Ident is not null)}" +
                          $" od {izvor.Senzori.Count} senzora");
        return 0;
    }
}

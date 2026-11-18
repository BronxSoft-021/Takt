using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>
/// Lists what the real sources say right now: <c>Testovi --popis [fajl]</c>. Run as administrator for the full
/// list - without it the driver cannot be loaded and most sensors are simply absent.
/// </summary>
public static class Popis
{
    public static int Senzora(string putanja)
    {
        Console.WriteLine($"Administrator: {(Admin.JeAdmin ? "da" : "ne")}");

        using var izvor = new SlozeniIzvor(new LhmIzvor(), new RamIzvor());
        Console.WriteLine($"Izvor: {izvor.Naziv}, senzora: {izvor.Senzori.Count}");
        if (izvor.Poruka is { } poruka) Console.WriteLine("Poruka: " + poruka);

        var snimak = izvor.Uzmi();
        var redovi = new List<string>();

        for (int i = 0; i < izvor.Senzori.Count; i++)
        {
            var senzor = izvor.Senzori[i];
            var vrednost = Format.Vrednost(snimak.Ovo(i), senzor.Jedinica);
            redovi.Add($"{senzor.Grupa} | {senzor.Uredjaj} | {senzor.Ime} | {vrednost}");
        }

        foreach (var red in redovi) Console.WriteLine(red);
        File.WriteAllLines(putanja, redovi);
        Console.WriteLine("\nPopis: " + putanja);

        return 0;
    }
}

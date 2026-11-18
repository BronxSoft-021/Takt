using System.Diagnostics;
using Takt.Merenje;
using Takt.Podaci;
using Takt.Sistem;
using Takt.Vm;

namespace Takt.Testovi;

/// <summary>
/// How long one reading and one minute write take: <c>Testovi --meri [n]</c>. Run it before and after a change -
/// "malo troši" is a rule of this program, not a wish.
/// </summary>
public static class MerenjeSenzora
{
    public static int Pokreni(int merenja)
    {
        using var izvor = new SlozeniIzvor(new LhmIzvor(), new RamIzvor());
        Console.WriteLine($"senzora: {izvor.Senzori.Count}");

        izvor.Uzmi(); // the first reading opens the driver and is always the slowest; not counted

        var vremena = new List<double>(merenja);
        for (int i = 0; i < merenja; i++)
        {
            var sat = Stopwatch.StartNew();
            izvor.Uzmi();
            sat.Stop();
            vremena.Add(sat.Elapsed.TotalMilliseconds);
        }

        vremena.Sort();
        double prosek = vremena.Average();
        Console.WriteLine($"čitanje: prosek {prosek:F1} ms, medijana {vremena[vremena.Count / 2]:F1} ms, " +
                          $"najbrže {vremena[0]:F1} ms, najsporije {vremena[^1]:F1} ms");
        Console.WriteLine($"na 1 s: {prosek / 10:F1} % jednog jezgra samo za senzore");

        // The console's own work per tick, with no window attached: what the view models cost.
        var senzori = izvor.Senzori.Select((s, i) => new SenzorVm(s, i, new Prsten(3600))).ToList();
        var kanali = KanalVm.Napravi(senzori);
        var vrednosti = new double?[senzori.Count];
        var najmanje = new double?[senzori.Count];
        var najvise = new double?[senzori.Count];

        var pult = Stopwatch.StartNew();
        for (int i = 0; i < 200; i++)
            foreach (var kanal in kanali)
                kanal.Osvezi(new Snimak(DateTime.Now, vrednosti), najmanje, najvise, DateTime.Now, i % 2 == 0);
        pult.Stop();
        Console.WriteLine($"pult: {pult.Elapsed.TotalMilliseconds / 200:F2} ms po otkucaju " +
                          $"({kanali.Count} kanala, {senzori.Count} senzora, bez prozora)");

        // The minute write: sixty readings' worth of rows, in a throwaway file.
        var putanja = Path.Combine(Spisak.Radni, $"merenje-{Guid.NewGuid():N}.db");
        using (var baza = new Baza(putanja))
        {
            baza.UpisiSenzore(izvor.Senzori);
            var redovi = izvor.Senzori
                .Select((s, i) => (s.Kljuc.Tekst, (double)i))
                .ToList();

            var sat = Stopwatch.StartNew();
            baza.UpisiSve(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), redovi);
            sat.Stop();
            Console.WriteLine($"upis {redovi.Count} redova: {sat.Elapsed.TotalMilliseconds:F1} ms (jednom u minuti)");
        }

        // The file stays in the test's own temporary folder, which is emptied at the start of every run: SQLite
        // keeps a pooled connection, so deleting it here would fail while the pool still holds the handle.

        return 0;
    }
}


// using Takt.Vm;

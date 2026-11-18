using System.Text.RegularExpressions;

namespace Takt.Testovi;

public static class IzmeneTestovi
{
    public static Task Verzija()
    {
        var koren = new DirectoryInfo(AppContext.BaseDirectory);
        while (koren is not null && !File.Exists(Path.Combine(koren.FullName, "Takt.csproj")))
            koren = koren.Parent;

        Proveri.Da(koren is not null, "Takt.csproj nije nađen iznad testova");

        var tekst = File.ReadAllText(Path.Combine(koren!.FullName, "Takt.csproj"));
        var nadjeno = Regex.Match(tekst, @"<Version>([^<]+)</Version>");
        Proveri.Da(nadjeno.Success, "<Version> nije nađen u Takt.csproj");

        Proveri.Jednako(nadjeno.Groups[1].Value.Trim(), Izmene.Trenutna.Broj,
            "verzija u Takt.csproj i prva u Istoriji");

        Proveri.Jednako("alfa", Izmene.Trenutna.Faza, "prva verzija je alfa");

        // The number is the sum of the history: it is never written by hand.
        int novih = Izmene.Spisak.Sum(u => u.Nove.Length);
        int dorada = Izmene.Spisak.Sum(u => u.Dorade.Length);
        Proveri.Jednako($"0.{novih}.{dorada}", Izmene.Trenutna.Broj, "broj je zbir cele istorije");
        Proveri.Da(novih > 0, "istorija ima bar jednu novu stvar");

        Proveri.Da(Izmene.Spisak.Length >= 1, "istorija nije prazna");
        Proveri.Da(Izmene.Tekst.Contains(Izmene.Trenutna.Broj), "tekst istorije sadrži broj verzije");
        return Task.CompletedTask;
    }
}

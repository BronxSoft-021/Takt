using Takt.Merenje;
using Takt.Podaci;

namespace Takt.Testovi;

public static class BazaTestovi
{
    public static Task UpisICitanje()
    {
        var putanja = Spisak.NoviFajl("baza-1.db");

        using (var baza = new Baza(putanja))
        {
            var senzori = new[]
            {
                new Senzor(new SenzorKljuc(Grupa.Procesor, "CPU", "Temperatura"), Jedinica.Celzijus),
                new Senzor(new SenzorKljuc(Grupa.Grafika, "GPU", "Temperatura"), Jedinica.Celzijus),
            };
            baza.UpisiSenzore(senzori);
            Proveri.Jednako(2, baza.BrojSenzora(), "senzora u bazi");

            // The same sensor twice must stay one row, not two.
            baza.UpisiSenzore(senzori);
            Proveri.Jednako(2, baza.BrojSenzora(), "isti senzori se ne dupliraju");

            string kljuc = senzori[0].Kljuc.Tekst;
            baza.Upisi(1000, kljuc, 45.5);
            baza.Upisi(1060, kljuc, 46.5);
            baza.Upisi(1120, kljuc, 47.5);
            Proveri.Jednako(3, baza.BrojRedova(), "redova posle tri upisa");

            var raspon = baza.Citaj(kljuc, 1000, 1060);
            Proveri.Jednako(2, raspon.Count, "dve vrednosti u rasponu");
            Proveri.Jednako(1000L, raspon[0].Vreme, "prvo vreme");
            Proveri.Blizu(46.5, raspon[1].Vrednost, 0.0001, "druga vrednost");

            Proveri.Jednako(0, baza.Citaj("nema ovog senzora", 0, 2000).Count, "nepoznat ključ ne puca");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// A sensor that spoke once and is no longer read is a leftover of an older build (the board's +3.3 V rail) and
    /// goes; a sensor that is read now keeps every row, even its first one.
    /// </summary>
    public static Task Mrtvi()
    {
        var baza = new Baza(Spisak.NoviFajl("baza-mrtvi.db"));

        var mrtav = new Senzor(new SenzorKljuc(Grupa.Hladjenje, "Ploča", "+3,3 V"), Jedinica.Volt);
        var ziv = new Senzor(new SenzorKljuc(Grupa.Procesor, "CPU", "Temperatura procesora"), Jedinica.Celzijus);
        var nov = new Senzor(new SenzorKljuc(Grupa.Diskovi, "Disk", "Temperatura"), Jedinica.Celzijus);
        baza.UpisiSenzore([mrtav, ziv, nov]);

        long sada = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        baza.Upisi(sada, mrtav.Kljuc.Tekst, 3.3);                       // one sample, and nobody reads it any more
        for (int i = 0; i < 5; i++) baza.Upisi(sada - i * 60, ziv.Kljuc.Tekst, 50 + i);
        baza.Upisi(sada, nov.Kljuc.Tekst, 36);                          // it just appeared: one sample, still read

        var zivi = new[] { ziv.Kljuc.Tekst, nov.Kljuc.Tekst };
        Proveri.Jednako(1, baza.ObrisiMrtve(zivi), "usamljen uzorak senzora kojeg više nema je uklonjen");
        Proveri.Jednako(0, baza.ObrisiMrtve(zivi), "drugi put nema šta da se ukloni");
        Proveri.Jednako(0, baza.Citaj(mrtav.Kljuc.Tekst, 0, long.MaxValue).Count, "mrtav senzor nema više ni jedan uzorak");
        Proveri.Jednako(5, baza.Citaj(ziv.Kljuc.Tekst, 0, long.MaxValue).Count, "živ senzor zadržava sve");
        Proveri.Jednako(1, baza.Citaj(nov.Kljuc.Tekst, 0, long.MaxValue).Count, "nov senzor sa jednim uzorkom ostaje");

        baza.Dispose();
        return Task.CompletedTask;
    }

    public static Task Brisanje()
    {
        var putanja = Spisak.NoviFajl("baza-2.db");

        using (var baza = new Baza(putanja))
        {
            baza.UpisiSve(500, [("a", 1.0), ("b", 2.0)]);
            baza.UpisiSve(1500, [("a", 3.0)]);
            Proveri.Jednako(3, baza.BrojRedova(), "tri reda pre brisanja");

            Proveri.Jednako(2, baza.OcistiPre(1000), "obrisana su dva stara reda");
            Proveri.Jednako(1, baza.BrojRedova(), "jedan red ostaje");

            baza.ObrisiSve();
            Proveri.Jednako(0, baza.BrojRedova(), "posle brisanja svega");
        }

        using (var baza = new Baza(putanja))
        {
            Proveri.Jednako(0, baza.BrojRedova(), "baza ostaje prazna posle ponovnog otvaranja");
            baza.Upisi(2000, "a", 9);
            Proveri.Jednako(1, baza.BrojRedova(), "i prima novi upis");
        }

        return Task.CompletedTask;
    }
}

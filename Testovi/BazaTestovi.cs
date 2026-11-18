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

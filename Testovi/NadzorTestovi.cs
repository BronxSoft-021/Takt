using Takt.Merenje;
using Takt.Podaci;
using Takt.Sistem;

namespace Takt.Testovi;

public static class NadzorTestovi
{
    public static Task Uzorkovanje()
    {
        using var izvor = new LazniIzvor();
        using var nadzor = new Nadzor(izvor, new Podesavanja { Istorija = false });

        var prvi = nadzor.UzmiJednom();
        nadzor.UzmiJednom();
        var treci = nadzor.UzmiJednom();

        Proveri.Jednako(izvor.Senzori.Count, prvi.Vrednosti.Length, "snimak ima vrednost za svaki senzor");
        Proveri.Da(prvi.Vrednosti.All(v => v is not null), "probni izvor daje sve vrednosti");
        Proveri.Da(treci.Vreme >= prvi.Vreme, "vreme ide napred");
        Proveri.Jednako(3, nadzor.Istorija[0].Broj, "tri uzorka u prstenu");
        Proveri.Da(nadzor.Najmanje[0] is not null, "najmanje je zapamćeno");
        Proveri.Da(nadzor.Najvise[0] >= nadzor.Najmanje[0], "najviše nije manje od najmanjeg");

        var (snimak, najmanje, najvise) = nadzor.Stanje();
        Proveri.Da(snimak is not null, "stanje daje poslednji snimak");
        Proveri.Jednako(izvor.Senzori.Count, najmanje.Length, "kopija najmanjih");
        Proveri.Jednako(izvor.Senzori.Count, najvise.Length, "kopija najviših");

        // Every temperature the fake source makes up must look like a temperature.
        for (int i = 0; i < izvor.Senzori.Count; i++)
        {
            if (izvor.Senzori[i].Jedinica != Jedinica.Celzijus) continue;
            double vrednost = snimak!.Vrednosti[i]!.Value;
            Proveri.Da(vrednost is > 0 and < 120, $"{izvor.Senzori[i].Ime} van pameti: {vrednost}");
        }

        return Task.CompletedTask;
    }

    public static Task Grupe()
    {
        using var izvor = new LazniIzvor();

        var grupe = izvor.Senzori.Select(s => s.Grupa).Distinct().ToArray();
        Proveri.Jednako(GrupaOpis.Redosled.Length, grupe.Length, "probni podaci pokrivaju svih pet grupa");

        foreach (var grupa in GrupaOpis.Redosled)
            Proveri.Da(grupe.Contains(grupa), "nedostaje grupa " + grupa);

        foreach (var senzor in izvor.Senzori)
        {
            Proveri.Da(!string.IsNullOrWhiteSpace(senzor.Ime), "senzor bez imena");
            Proveri.Da(!string.IsNullOrWhiteSpace(senzor.Uredjaj), "senzor bez uređaja: " + senzor.Ime);
            Proveri.Da(senzor.Jedinica != Jedinica.Nepoznato, "senzor bez jedinice: " + senzor.Ime);
        }

        var kljucevi = izvor.Senzori.Select(s => s.Kljuc.Tekst).ToArray();
        Proveri.Jednako(kljucevi.Length, kljucevi.Distinct().Count(), "ključevi senzora su jedinstveni");

        return Task.CompletedTask;
    }

    /// <summary>One reading per second, one row per minute: sixty readings must leave history behind.</summary>
    public static Task UpisUMinuti()
    {
        using var izvor = new LazniIzvor();
        using var nadzor = new Nadzor(izvor, new Podesavanja { Istorija = true, IstorijaDana = 7 }, kapacitet: 120);

        for (int i = 0; i < 60; i++) nadzor.UzmiJednom();

        Proveri.Da(File.Exists(Putanje.Baza), "baza je napravljena u folderu podataka");

        using var baza = new Baza(Putanje.Baza);
        Proveri.Da(baza.BrojRedova() >= izvor.Senzori.Count,
            $"posle 60 uzoraka upisan je bar jedan red po senzoru (redova: {baza.BrojRedova()})");
        Proveri.Jednako(izvor.Senzori.Count, baza.BrojSenzora(), "svi senzori su upamćeni u bazi");
        return Task.CompletedTask;
    }

    /// <summary>History switched off, but the file from earlier runs is there: "Obriši istoriju" still empties it.</summary>
    public static Task BrisanjeBezBaze()
    {
        using (var izvor = new LazniIzvor())
        using (var nadzor = new Nadzor(izvor, new Podesavanja { Istorija = true }, kapacitet: 120))
        {
            for (int i = 0; i < 60; i++) nadzor.UzmiJednom();
        }

        using (var baza = new Baza(Putanje.Baza))
            Proveri.Da(baza.BrojRedova() > 0, "pre brisanja ima istorije");

        using (var izvor = new LazniIzvor())
        using (var nadzor = new Nadzor(izvor, new Podesavanja { Istorija = false }))
            Proveri.Da(nadzor.ObrisiIstoriju(), "brisanje javlja uspeh");

        using (var baza = new Baza(Putanje.Baza))
            Proveri.Jednako(0, baza.BrojRedova(), "posle brisanja baza je prazna");

        return Task.CompletedTask;
    }
}

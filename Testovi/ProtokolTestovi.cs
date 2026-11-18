using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>The line between the helper and the window: it must survive a round trip unchanged.</summary>
public static class ProtokolTestovi
{
    public static Task SpisakIUzorak()
    {
        var senzori = new[]
        {
            new Senzor(new SenzorKljuc(Grupa.Procesor, "AMD Ryzen 5 5600", "Temperatura jezgro 1"), Jedinica.Celzijus),
            new Senzor(new SenzorKljuc(Grupa.Grafika, "AMD Radeon RX 6600", "Ventilator"), Jedinica.Obrtaj),
        };

        var spisak = Protokol.Citaj(Protokol.Spisak(senzori));
        Proveri.Da(spisak is not null, "spisak se čita");
        Proveri.Jednako(Protokol.SpisakVrsta, spisak!.Vrsta, "vrsta spiska");
        Proveri.Jednako(2, spisak.Senzori!.Length, "dva senzora u spisku");
        Proveri.Jednako("Procesor", spisak.Senzori[0].Grupa, "grupa po imenu");
        Proveri.Jednako("Celzijus", spisak.Senzori[0].Jedinica, "jedinica po imenu");
        Proveri.Jednako("Ventilator", spisak.Senzori[1].Ime, "ime senzora");
        Proveri.Jednako("AMD Radeon RX 6600", spisak.Senzori[1].Uredjaj, "uređaj senzora");

        var vreme = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Local);
        var snimak = new Snimak(vreme, [45.5, null]);
        var uzorak = Protokol.Citaj(Protokol.Uzorak(snimak));

        Proveri.Da(uzorak is not null, "uzorak se čita");
        Proveri.Jednako(Protokol.UzorakVrsta, uzorak!.Vrsta, "vrsta uzorka");
        Proveri.Jednako(2, uzorak.Vrednosti!.Length, "dve vrednosti");
        Proveri.Blizu(45.5, uzorak.Vrednosti[0]!.Value, 0.0001, "prva vrednost");
        Proveri.Jednako<double?>(null, uzorak.Vrednosti[1], "rupa ostaje rupa");

        var procitano = DateTimeOffset.FromUnixTimeMilliseconds(uzorak.Vreme).LocalDateTime;
        Proveri.Jednako(vreme, procitano, "vreme u milisekundama");

        Proveri.Da(Protokol.Citaj("ovo nije json") is null, "đubre se preskače, ne puca");
        Proveri.Da(Protokol.Citaj("{\"vrsta\":\"greska\",\"greska\":\"nema drajvera\"}") is not null,
            "poruka o grešci se čita");
        return Task.CompletedTask;
    }
}

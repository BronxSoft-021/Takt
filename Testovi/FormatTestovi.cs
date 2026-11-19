using Takt.Merenje;

namespace Takt.Testovi;

public static class FormatTestovi
{
    /// <summary>
    /// One culture for the whole program: the rows (Format), the chart labels and plain interpolations must all
    /// write "52,8" - a report that reads "52.8" beside a screen that reads "52,8" is the same number told twice.
    /// Windows on this machine is en-GB, which is what made the difference visible (19. 11. 2026).
    /// </summary>
    public static Task Kultura()
    {
        Format.PostaviKulturu();

        Proveri.Jednako("sr-Latn-RS", Format.Kultura.Name, "kultura programa");
        Proveri.Jednako("52,8 °C", $"{52.8:0.0} °C", "interpolacija piše zapetu, kao Format");
        Proveri.Jednako(Format.Vrednost(52.8, Jedinica.Celzijus), $"{52.8:0.0} °C", "Format i interpolacija se poklapaju");
        Proveri.Jednako("55", $"{55.4:0}", "ceo broj bez separatora");
        return Task.CompletedTask;
    }

    public static Task Zapeta()
    {
        Proveri.Jednako("45,3 °C", Format.Vrednost(45.28, Jedinica.Celzijus), "temperatura sa zapetom");
        Proveri.Jednako("62 %", Format.Vrednost(62, Jedinica.Procenat), "procenat bez decimala");
        Proveri.Jednako("1350 o/min", Format.Vrednost(1350, Jedinica.Obrtaj), "obrtaji bez grupa");
        Proveri.Jednako("4200 MHz", Format.Vrednost(4200, Jedinica.Megaherc), "takt");
        Proveri.Jednako("1,284 V", Format.Vrednost(1.2843, Jedinica.Volt), "napon na tri decimale");
        Proveri.Jednako("180,0 W", Format.Vrednost(180, Jedinica.Vat), "potrošnja");
        Proveri.Jednako("900 MB", Format.Vrednost(900, Jedinica.Megabajt), "malo memorije ostaje u MB");
        Proveri.Jednako("8,0 GB", Format.Vrednost(8200, Jedinica.Megabajt), "gigabajt i više se piše u GB");
        Proveri.Jednako("15,3 GB", Format.Vrednost(15698, Jedinica.Megabajt), "zauzeto");
        Proveri.Jednako(("31,9", "GB"), Format.Delovi(32681, Jedinica.Megabajt), "broj i oznaka posebno");
        Proveri.Jednako("37,42 TB", Format.Vrednost(37.42 * 1_048_576, Jedinica.Megabajt), "ukupno upisano na disk");

        Proveri.Jednako("—", Format.Vrednost(null, Jedinica.Celzijus), "nema vrednosti");
        Proveri.Jednako("", Format.Oznaka(Jedinica.Nepoznato), "nepoznata jedinica nema oznaku");
        Proveri.Jednako("12,0", Format.Vrednost(12, Jedinica.Nepoznato), "broj bez oznake");
        Proveri.Jednako("49", Format.Broj(49.04, Jedinica.Procenat, 0), "procenat bez decimala");
        Proveri.Jednako("49,0", Format.Broj(49.04, Jedinica.Procenat, 1), "osa uskog raspona dobija decimalu");
        Proveri.Jednako("48,64", Format.Broj(48.641, Jedinica.Procenat, 2), "još uži raspon dobija dve");
        Proveri.Jednako("1,550", Format.Broj(1.55, Jedinica.Volt, 1), "volt ostaje na tri decimale");
        return Task.CompletedTask;
    }
}

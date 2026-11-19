using Takt.Merenje;

namespace Takt.Testovi;

/// <summary>
/// The second language: Serbian is the source text, English is a table. A text missing from the table stays
/// Serbian (a visible gap, never a blank label), and the numbers follow the language - 52,8 or 52.8.
/// </summary>
public static class TekstTestovi
{
    public static Task DvaJezika()
    {
        var kultura = Format.Kultura;

        try
        {
            Tekst.Postavi(Tekst.Srpski);
            Proveri.Jednako("Podešavanja", Tekst.T("Podešavanja"), "srpski vraća isti tekst");
            Proveri.Jednako("sr-Latn-RS", Format.Kultura.Name, "srpska kultura");
            Proveri.Jednako("52,8 °C", Format.Vrednost(52.8, Jedinica.Celzijus), "srpski piše zapetu");

            Tekst.Postavi(Tekst.Engleski);
            Proveri.Jednako("Settings", Tekst.T("Podešavanja"), "engleski prevodi");
            Proveri.Jednako("en-GB", Format.Kultura.Name, "engleska kultura");
            Proveri.Jednako("52.8 °C", Format.Vrednost(52.8, Jedinica.Celzijus), "engleski piše tačku");
            Proveri.Jednako("Rečenica koje nema u tabeli", Tekst.T("Rečenica koje nema u tabeli"),
                "tekst kojeg nema u tabeli ostaje srpski");
        }
        finally
        {
            Tekst.Postavi(Tekst.Srpski);
            Format.PostaviKulturu(kultura);
        }

        return Task.CompletedTask;
    }
}

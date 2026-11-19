using System.Globalization;
using Takt.Merenje;

namespace Takt;

/// <summary>
/// The program's two languages. Serbian is the source: every text in the code is written in Serbian and
/// <see cref="T"/> gives the English one from a table - a text missing from the table stays Serbian, which is a
/// visible gap rather than a blank screen. The numbers follow the language too (52,8 and not 52.8).
/// </summary>
public static class Tekst
{
    public const string Srpski = "sr";
    public const string Engleski = "en";

    /// <summary>"sr" or "en"; Serbian unless the user chose English.</summary>
    public static string Jezik { get; private set; } = Srpski;

    public static bool JeEngleski => Jezik == Engleski;

    /// <summary>Called once at start, before anything is shown or written.</summary>
    public static void Postavi(string? jezik)
    {
        Jezik = jezik == Engleski ? Engleski : Srpski;

        // A Serbian program writes 52,8 and 19. 11. 2026; an English one writes 52.8 and 19/11/2026.
        var kultura = CultureInfo.GetCultureInfo(JeEngleski ? "en-GB" : "sr-Latn-RS");
        Format.PostaviKulturu(kultura);
        CultureInfo.CurrentCulture = kultura;
        CultureInfo.CurrentUICulture = kultura;
    }

    /// <summary>
    /// The text in the chosen language. <paramref name="srpski"/> is the key: the Serbian text as written in the
    /// code. Unknown text comes back as it was, so a forgotten line still reads, in Serbian.
    /// </summary>
    public static string T(string srpski) => JeEngleski ? NaEngleskom(srpski) : srpski;

    private static string NaEngleskom(string srpski) => TekstEngleski.Nadji(srpski) ?? srpski;
}

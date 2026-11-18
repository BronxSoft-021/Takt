using System.Globalization;

namespace Takt.Merenje;

/// <summary>Numbers and units as the user reads them: decimal comma, never a dot.</summary>
public static class Format
{
    private static readonly CultureInfo Kultura = CultureInfo.GetCultureInfo("sr-Latn-RS");

    /// <summary>Shown instead of a number the hardware did not give.</summary>
    public const string Nema = "—";

    public static string Oznaka(Jedinica jedinica) => jedinica switch
    {
        Jedinica.Celzijus => "°C",
        Jedinica.Procenat => "%",
        Jedinica.Megaherc => "MHz",
        Jedinica.Obrtaj => "o/min",
        Jedinica.Volt => "V",
        Jedinica.Vat => "W",
        Jedinica.Amper => "A",
        Jedinica.Megabajt => "MB",
        Jedinica.MegabajtUSekundi => "MB/s",
        _ => "",
    };

    public static string Vrednost(double? vrednost, Jedinica jedinica)
    {
        if (vrednost is null) return Nema;
        var (broj, oznaka) = Delovi(vrednost.Value, jedinica);
        return oznaka.Length == 0 ? broj : $"{broj} {oznaka}";
    }

    /// <summary>
    /// Number and unit apart, the way a row shows them in two sizes. Memory of a gigabyte or more reads as
    /// "15,3 GB" and not "15698 MB" - the same thing, but a person reads it at a glance.
    /// </summary>
    public static (string Broj, string Oznaka) Delovi(double vrednost, Jedinica jedinica)
    {
        // A disk's lifetime totals run into terabytes: "37,42 TB", not "38318,1 GB".
        if (jedinica == Jedinica.Megabajt && Math.Abs(vrednost) >= 1_048_576)
            return ((vrednost / 1_048_576).ToString("F2", Kultura), "TB");

        if (UGigabajte(vrednost, jedinica))
            return ((vrednost / 1024).ToString("F1", Kultura), "GB");

        return (Broj(vrednost, jedinica), Oznaka(jedinica));
    }

    /// <summary>True when a memory amount is large enough to be shown in GB.</summary>
    public static bool UGigabajte(double vrednost, Jedinica jedinica) =>
        jedinica == Jedinica.Megabajt && Math.Abs(vrednost) >= 1024;

    public static string Broj(double vrednost, Jedinica jedinica)
        => Broj(vrednost, jedinica, 0);

    /// <summary>
    /// Same number, but never with fewer decimals than asked: a chart axis over a narrow range needs
    /// "48,6 / 48,4 / 48,2" even when the unit itself is shown as whole numbers.
    /// </summary>
    public static string Broj(double vrednost, Jedinica jedinica, int najmanjeDecimala)
    {
        int decimale = Math.Max(Decimale(jedinica), najmanjeDecimala);
        return vrednost.ToString("F" + decimale, Kultura);
    }

    private static int Decimale(Jedinica jedinica) => jedinica switch
    {
        Jedinica.Procenat => 0,
        Jedinica.Obrtaj => 0,
        Jedinica.Megaherc => 0,
        Jedinica.Megabajt => 0,
        Jedinica.MegabajtUSekundi => 1,
        Jedinica.Volt => 3,
        Jedinica.Amper => 2,
        _ => 1,
    };
}

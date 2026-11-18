using System.Text.Json;
using System.Text.Json.Serialization;
using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// What travels over the pipe between the helper (administrator, reads the hardware) and the window
/// (ordinary user, draws it). One JSON object per line, so a half-written line is simply skipped.
/// </summary>
public static class Protokol
{
    private static readonly JsonSerializerOptions Opcije = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>A sensor without its value: the helper sends this once, when the window connects.</summary>
    public sealed record Opis(string Grupa, string Uredjaj, string Ime, string Jedinica);

    /// <summary>
    /// One program that drew frames in the last second, as the service saw it through Windows' own events:
    /// frames per second, the average and the longest frame, the 1 % low (fps of the slowest 1 % of the last ten
    /// seconds) and how many frames were stutters (far longer than the frames around them).
    /// </summary>
    public sealed record Igra(int Pid, string Ime, string? Putanja, double Fps, double ProsekMs, double NajviseMs,
        double Low1, int Stucanja);

    /// <summary>vrsta: "spisak", "uzorak", "greska" or "igre". <paramref name="Izvor"/> says who serves: "usluga" or "pomocnik".</summary>
    public sealed record Poruka(string Vrsta, Opis[]? Senzori, double?[]? Vrednosti, long Vreme, string? Greska,
        string? Izvor = null, Igra[]? Igre = null);

    public const string SpisakVrsta = "spisak";
    public const string UzorakVrsta = "uzorak";
    public const string GreskaVrsta = "greska";
    public const string IgreVrsta = "igre";

    public static string Igre(IEnumerable<Igra> igre) =>
        JsonSerializer.Serialize(new Poruka(IgreVrsta, null, null, 0, null, null, igre.ToArray()), Opcije);

    /// <summary>Who serves: the service writes history itself, the helper leaves it to the window.</summary>
    public const string Usluga = "usluga";
    public const string Pomocnik = "pomocnik";

    public static string Spisak(IReadOnlyList<Senzor> senzori, string izvor = Pomocnik) =>
        JsonSerializer.Serialize(
            new Poruka(SpisakVrsta,
                senzori.Select(s => new Opis(s.Grupa.ToString(), s.Uredjaj, s.Ime, s.Jedinica.ToString())).ToArray(),
                null, 0, null, izvor),
            Opcije);

    public static string Uzorak(Snimak snimak) =>
        JsonSerializer.Serialize(
            new Poruka(UzorakVrsta, null, snimak.Vrednosti,
                new DateTimeOffset(snimak.Vreme.ToUniversalTime()).ToUnixTimeMilliseconds(), null),
            Opcije);

    public static string Greska(string poruka) =>
        JsonSerializer.Serialize(new Poruka(GreskaVrsta, null, null, 0, poruka), Opcije);

    /// <summary>Null for a line that is not ours (a stale file, a half-written message).</summary>
    public static Poruka? Citaj(string linija)
    {
        try
        {
            var poruka = JsonSerializer.Deserialize<Poruka>(linija, Opcije);
            return string.IsNullOrEmpty(poruka?.Vrsta) ? null : poruka;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

namespace Takt.Merenje;

/// <summary>How worried a person should be about a drive.</summary>
public enum NivoZdravlja
{
    Nepoznato,
    Dobro,
    Upozorenje,
    Kvar,
}

/// <summary>
/// What one drive says about itself, in numbers. Filled by <see cref="Sistem.Smart"/> (the toolkit's SMART read),
/// judged here - so the rules can be tested without a drive, and the reader can change without touching them.
/// </summary>
public sealed record StanjeDiska(
    string Model,
    bool Ssd,
    bool Nvme,
    double? ZivotniVek,
    double? Istrosenost,
    double? RezervniBlokovi,
    double? GranicaRezervnih,
    double? Temperatura,
    double? GranicaTemperature,
    double? KriticnaTemperatura,
    long? Preraspodeljeni,
    long? NaCekanju,
    long? Neispravni,
    long? CrcGreske,
    long? MediaGreske,
    long? KriticnoUpozorenje,
    double? SatiRada,
    bool ImaSmart = false);

/// <summary>A verdict in a few words: the level for the colour, the title for the line, the detail for the tool tip.</summary>
public sealed record NalazZdravlja(NivoZdravlja Nivo, string Naslov, string Detalj)
{
    public static readonly NalazZdravlja Nema = new(NivoZdravlja.Nepoznato, Takt.Tekst.T("Nema podataka o zdravlju"),
        Takt.Tekst.T("SMART se čita kao administrator (usluga ili pomoćnik)."));
}

/// <summary>
/// The health rules, in one place: a handful of SMART numbers turned into "fine", "watch this" or "save your
/// data". Only what a person can act on is reported - the full attribute table is never shown.
/// </summary>
public static class Zdravlje
{
    public static NalazZdravlja Proveri(StanjeDiska s)
    {
        // No attributes at all: the drive did not answer (not administrator, USB bridge, a driver in between).
        // "Healthy" would be a lie, so the verdict says it does not know.
        if (!s.ImaSmart) return NalazZdravlja.Nema;

        var kvar = Kvar(s);
        if (kvar is not null) return kvar;

        var upozorenje = Upozorenje(s);
        if (upozorenje is not null) return upozorenje;

        return new NalazZdravlja(NivoZdravlja.Dobro, Takt.Tekst.T("Zdrav"), Dobro(s));
    }

    private static NalazZdravlja? Kvar(StanjeDiska s)
    {
        // Sectors that already failed and are waiting to be replaced: the drive is losing data now.
        if (s.NaCekanju is > 0)
            return new(NivoZdravlja.Kvar, Takt.Tekst.T("Kvar: sektori čekaju popravku"),
                Takt.Tekst.P("{0} sektora se više ne čita ispravno i čeka zamenu - sačuvaj važne fajlove odmah.",
                    s.NaCekanju));

        if (s.Neispravni is > 0)
            return new(NivoZdravlja.Kvar, Takt.Tekst.T("Kvar: neispravni sektori"),
                Takt.Tekst.P("{0} neispravnih sektora - podaci su već izgubljeni na tim mestima; menjaj disk.",
                    s.Neispravni));

        if (s.MediaGreske is > 0)
            return new(NivoZdravlja.Kvar, Takt.Tekst.T("Kvar: greške u podacima"),
                Takt.Tekst.P("{0} grešaka u integritetu podataka (NVMe). Sačuvaj važno i planiraj zamenu.", s.MediaGreske));

        // NVMe critical warning is a bit mask; any bit means the drive itself is asking for attention.
        if (s.KriticnoUpozorenje is > 0)
            return new(NivoZdravlja.Kvar, Takt.Tekst.T("Kvar: drajv prijavljuje kritično stanje"),
                Takt.Tekst.P("NVMe kritično upozorenje (maska {0}) - proveri Samsung Magician / proizvođački alat.",
                    s.KriticnoUpozorenje));

        if (s.ZivotniVek is { } zivot && zivot <= 5)
            return new(NivoZdravlja.Kvar, Takt.Tekst.T("Kvar: životni vek potrošen"),
                Takt.Tekst.P("Ostalo je {0} % upisanih podataka predviđenih za ovaj disk - zameni ga.", $"{zivot:0}"));

        if (s.Temperatura is { } t && s.KriticnaTemperatura is { } tk && t >= tk)
            return new(NivoZdravlja.Kvar, Takt.Tekst.T("Kvar: pregrijan"), Takt.Tekst.P("{0} °C je na kritičnoj granici ({1} °C).", $"{t:0}", $"{tk:0}"));

        return null;
    }

    private static NalazZdravlja? Upozorenje(StanjeDiska s)
    {
        // Replaced sectors are not lost data yet, but the count only ever grows: worth watching.
        if (s.Preraspodeljeni is > 0)
            return new(NivoZdravlja.Upozorenje, Takt.Tekst.T("Upozorenje: preraspoređeni sektori"),
                Takt.Tekst.P("{0} sektora je disk zamenio rezervnim. Nije kvar, ali prati da broj ne raste.",
                    s.Preraspodeljeni));

        // CRC errors on the cable, not on the platters: usually a bad SATA cable or a loose connector.
        if (s.CrcGreske is > 0)
            return new(NivoZdravlja.Upozorenje, Takt.Tekst.T("Upozorenje: greške u prenosu"),
                Takt.Tekst.P("{0} CRC grešaka - obično loš SATA kabl ili konektor, ne sam disk.", s.CrcGreske));

        if (s.ZivotniVek is { } zivot && zivot <= 20)
            return new(NivoZdravlja.Upozorenje, Takt.Tekst.T("Upozorenje: životni vek se troši"),
                Takt.Tekst.P("Ostalo je {0} % - vreme je da se pravi rezervna kopija i gleda novi disk.", $"{zivot:0}"));

        if (s.Istrosenost is { } istrosenost && istrosenost >= 80)
            return new(NivoZdravlja.Upozorenje, Takt.Tekst.T("Upozorenje: disk je istrošen"),
                Takt.Tekst.P("Istrošenost {0} % - blizu je kraju predviđenog života.", $"{istrosenost:0}"));

        if (s.RezervniBlokovi is { } rezerva && s.GranicaRezervnih is { } granica && rezerva <= granica * 2)
            return new(NivoZdravlja.Upozorenje, Takt.Tekst.T("Upozorenje: malo rezervnih blokova"),
                Takt.Tekst.P("Rezervnih blokova {0} (granica {1}) - disk počinje da troši zamenu.",
                    $"{rezerva:0}", $"{granica:0}"));

        if (s.Temperatura is { } t && s.GranicaTemperature is { } tg && t >= tg)
            return new(NivoZdravlja.Upozorenje, Takt.Tekst.T("Upozorenje: topao disk"),
                Takt.Tekst.P("{0} °C je na granici upozorenja ({1} °C).", $"{t:0}", $"{tg:0}"));

        return null;
    }

    private static string Dobro(StanjeDiska s)
    {
        var delovi = new List<string>();

        if (s.ZivotniVek is { } zivot) delovi.Add(Takt.Tekst.P("životni vek {0} %", $"{zivot:0}"));
        else if (s.Istrosenost is { } istrosenost) delovi.Add(Takt.Tekst.P("istrošenost {0} %", $"{istrosenost:0}"));

        if (s.SatiRada is { } sati and > 0) delovi.Add(Takt.Tekst.P("{0} h rada", sati.ToString("N0", Format.Kultura)));
        if (s.Temperatura is { } t) delovi.Add($"{t:0} °C");

        delovi.Add(s.Nvme ? "NVMe" : s.Ssd ? "SSD" : "HDD");
        return string.Join(" · ", delovi);
    }
}

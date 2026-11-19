namespace Takt.Merenje;

/// <summary>The five groups the program shows, in the order they appear on the left.</summary>
public enum Grupa
{
    Procesor,
    Grafika,
    Memorija,
    Diskovi,
    Hladjenje,
}

/// <summary>
/// What makes a sensor *that* sensor. The text is what the database stores, so it must stay stable:
/// a renamed sensor would otherwise continue as a new row and the old history would look lost.
/// <paramref name="Ident"/> is that stable name: the library's own path (for example
/// <c>/amdcpu/0/temperature/2</c>), or a made-up one for a source that has no library. It does not depend on
/// the language on screen, so the name may be translated without splitting the history; rows written before
/// it existed are keyed by the text, and <see cref="Podaci.Baza"/> moves them over once.
/// </summary>
public sealed record SenzorKljuc(Grupa Grupa, string Uredjaj, string Ime, string? Ident = null)
{
    /// <summary>The key in the database: the stable name when there is one.</summary>
    public string Tekst => Ident ?? TekstImena;

    /// <summary>The old key (group, device, the name as shown); used only to move old history over.</summary>
    public string TekstImena => $"{Grupa} | {Uredjaj} | {Ime}";

    public override string ToString() => Tekst;
}

/// <summary>One reading point: which device, which name, which unit. The value lives in <see cref="Snimak"/>.</summary>
public sealed record Senzor(SenzorKljuc Kljuc, Jedinica Jedinica)
{
    public string Ime => Kljuc.Ime;
    public string Uredjaj => Kljuc.Uredjaj;
    public Grupa Grupa => Kljuc.Grupa;
}

/// <summary>All values read at one moment, in the same order as the source's sensor list.</summary>
public sealed record Snimak(DateTime Vreme, double?[] Vrednosti)
{
    public double? Ovo(int indeks) => indeks >= 0 && indeks < Vrednosti.Length ? Vrednosti[indeks] : null;
}

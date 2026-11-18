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
/// </summary>
public sealed record SenzorKljuc(Grupa Grupa, string Uredjaj, string Ime)
{
    public string Tekst => $"{Grupa} | {Uredjaj} | {Ime}";

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

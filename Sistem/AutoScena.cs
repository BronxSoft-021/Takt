namespace Takt.Sistem;

/// <summary>
/// When the Scene comes on by itself. With a game on screen it comes on; when the game stops drawing it goes; and a
/// person who pressed Esc is not overruled until the next game starts. Public for a test: the decision is a table,
/// not an if inside a window event.
/// </summary>
public static class AutoScena
{
    public enum Odluka
    {
        Nista,
        Uci,
        Izadji,
    }

    public static Odluka Odluci(bool ukljuceno, bool igraAktivna, bool bilaAktivna, bool ugasenaRukom, bool uSceni)
    {
        if (!ukljuceno) return Odluka.Nista;

        // A new game: the person's "no" from the last one no longer counts (the caller resets it when the game goes).
        if (igraAktivna && !bilaAktivna && !ugasenaRukom && !uSceni) return Odluka.Uci;

        if (!igraAktivna && bilaAktivna && uSceni) return Odluka.Izadji;

        return Odluka.Nista;
    }
}

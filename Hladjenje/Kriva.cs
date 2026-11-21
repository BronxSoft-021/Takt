namespace Takt.Hladjenje;

/// <summary>One point of a fan curve: at this temperature the fan turns at this percentage.</summary>
public sealed record Tacka(double Temperatura, int Procenat);

/// <summary>
/// A fan curve: the higher the temperature, the faster the fan. For now it only calculates - nothing is sent
/// to the hardware yet. The rule from the radio program applies here too: the program must never leave a fan
/// slower than the board itself would, so a curve is refused if it is not safe.
/// </summary>
public sealed class Kriva
{
    private readonly Tacka[] _tacke;

    public Kriva(IEnumerable<Tacka> tacke)
    {
        _tacke = tacke.ToArray();
        Greska = Proveri(_tacke);
    }

    public static Kriva Podrazumevana { get; } = new(
    [
        new Tacka(30, 25),
        new Tacka(50, 40),
        new Tacka(65, 55),
        new Tacka(75, 75),
        new Tacka(85, 90),
        new Tacka(90, 100),
    ]);

    public IReadOnlyList<Tacka> Tacke => _tacke;

    /// <summary>Why the curve cannot be used, or null when it can.</summary>
    public string? Greska { get; }

    public bool Ispravna => Greska is null;

    /// <summary>Slowest the fan may ever turn with this curve.</summary>
    public int Najmanje => _tacke.Length == 0 ? 0 : _tacke.Min(t => t.Procenat);

    public int Najvise => _tacke.Length == 0 ? 0 : _tacke.Max(t => t.Procenat);

    /// <summary>How fast the fan should turn at this temperature: straight line between two points, clamped at the ends.</summary>
    public int Procenat(double temperatura)
    {
        if (_tacke.Length == 0) return 0;
        if (temperatura <= _tacke[0].Temperatura) return _tacke[0].Procenat;
        if (temperatura >= _tacke[^1].Temperatura) return _tacke[^1].Procenat;

        for (int i = 1; i < _tacke.Length; i++)
        {
            var donja = _tacke[i - 1];
            var gornja = _tacke[i];
            if (temperatura > gornja.Temperatura) continue;

            double deo = (temperatura - donja.Temperatura) / (gornja.Temperatura - donja.Temperatura);
            double vrednost = donja.Procenat + deo * (gornja.Procenat - donja.Procenat);
            return (int)Math.Round(Math.Clamp(vrednost, 0, 100));
        }

        return _tacke[^1].Procenat;
    }

    /// <summary>Checks the curve as written, without sorting it: a curve must already be in order.</summary>
    public static string? Proveri(IReadOnlyList<Tacka> tacke)
    {
        if (tacke.Count < 2) return Takt.Tekst.T("Kriva mora imati bar dve tačke.");

        foreach (var tacka in tacke)
        {
            if (tacka.Temperatura < 0 || tacka.Temperatura > 120)
                return Takt.Tekst.T("Temperatura u krivoj mora biti između 0 i 120 °C.");
            if (tacka.Procenat < 0 || tacka.Procenat > 100)
                return Takt.Tekst.T("Brzina ventilatora mora biti između 0 i 100 %.");
        }

        for (int i = 1; i < tacke.Count; i++)
        {
            if (tacke[i].Temperatura <= tacke[i - 1].Temperatura)
                return Takt.Tekst.T("Temperature u krivoj moraju rasti.");
            if (tacke[i].Procenat < tacke[i - 1].Procenat)
                return Takt.Tekst.T("Brzina ventilatora ne sme da padne kad temperatura raste.");
        }

        return null;
    }
}

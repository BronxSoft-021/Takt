namespace Takt.Merenje;

/// <summary>Titles, short explanations and icons of the five screens, in one place.</summary>
public static class GrupaOpis
{
    public static readonly Grupa[] Redosled =
    [
        Grupa.Procesor,
        Grupa.Grafika,
        Grupa.Memorija,
        Grupa.Diskovi,
        Grupa.Hladjenje,
    ];

    public static string Naslov(Grupa grupa) => grupa switch
    {
        Grupa.Procesor => "Procesor",
        Grupa.Grafika => "Grafička karta",
        Grupa.Memorija => "Memorija",
        Grupa.Diskovi => "Diskovi",
        Grupa.Hladjenje => "Hlađenje",
        _ => grupa.ToString(),
    };

    public static string Opis(Grupa grupa) => grupa switch
    {
        Grupa.Procesor => "Temperatura po jezgrima, opterećenje, takt, potrošnja, napon i thermal throttling.",
        Grupa.Grafika => "Temperatura i hotspot, VRAM, opterećenje, takt jezgra i memorije, potrošnja i ventilatori.",
        Grupa.Memorija => "Zauzeće, slobodan RAM, brzina memorije, commit i pagefile.",
        Grupa.Diskovi => "Temperature SSD-a, zdravlje, SMART parametri i životni vek.",
        Grupa.Hladjenje => "Brzina ventilatora, temperature koje ih pokreću i praćenje anomalija.",
        _ => "",
    };

    /// <summary>
    /// What the empty screen says: why this part has no sensors and what would bring them. With
    /// <paramref name="drajver"/> false the answer is the PawnIO driver, not administrator rights - asking for
    /// administrator again would change nothing, and saying "turn on full monitoring" would be a lie.
    /// </summary>
    public static string Prazno(Grupa grupa, bool drajver = true) => (grupa, drajver) switch
    {
        (Grupa.Procesor, false) => "Temperatura, takt, potrošnja i napon procesora idu preko PawnIO drajvera, a on se " +
                                   "instalira posebno (pawnio.eu). Bez njega se vidi samo opterećenje - i kao administrator.",
        (Grupa.Diskovi, false) => "Temperatura, zdravlje i SMART diskova traže PawnIO drajver (pawnio.eu); " +
                                   "administratorska prava sama nisu dovoljna.",
        (Grupa.Hladjenje, false) => "Ventilatori kućišta i procesora i temperature matične ploče traže PawnIO drajver " +
                                     "(pawnio.eu). Bez njega ni administrator ne dobija te senzore.",
        (_, false) => "Ovaj deo traži PawnIO drajver (pawnio.eu); bez njega ni administrator ne dobija te senzore.",

        (Grupa.Diskovi, true) => "Temperatura, zdravlje i SMART diskova se čitaju preko drajvera, a on traži administratora. " +
                                 "Uključi potpuni nadzor i diskovi će se pojaviti ovde.",
        (Grupa.Hladjenje, true) => "Ventilatore kućišta i procesora i temperature matične ploče čita Super I/O čip, a do njega " +
                                   "se stiže samo preko drajvera. Uključi potpuni nadzor.",
        _ => "Ovaj deo računara ne daje nijedan senzor bez drajvera. Uključi potpuni nadzor.",
    };

    /// <summary>
    /// A small line drawing for the left column, on a 16 × 16 grid, drawn as a stroke. Hand-made on purpose:
    /// an icon font may be missing on an older Windows and then the column shows empty squares.
    /// </summary>
    public static string Ikona(Grupa grupa) => grupa switch
    {
        // chip with pins
        Grupa.Procesor => "M4,4 H12 V12 H4 Z M6.5,6.5 H9.5 V9.5 H6.5 Z M6,1.5 V4 M10,1.5 V4 M6,12 V14.5 M10,12 V14.5 " +
                          "M1.5,6 H4 M1.5,10 H4 M12,6 H14.5 M12,10 H14.5",
        // card with a fan and a bracket
        Grupa.Grafika => "M1.5,4 H14.5 V12 H1.5 Z M6,8 A2.5,2.5 0 1 0 11,8 A2.5,2.5 0 1 0 6,8 M3.5,12 V14",
        // memory stick
        Grupa.Memorija => "M1.5,4.5 H14.5 V10.5 H1.5 Z M4,6.5 V8.5 M7,6.5 V8.5 M10,6.5 V8.5 M13,6.5 V8.5 " +
                          "M3,10.5 V12.5 M6,10.5 V12.5 M10,10.5 V12.5 M13,10.5 V12.5",
        // drive with a light
        Grupa.Diskovi => "M2,3.5 H14 V12.5 H2 Z M2,9.5 H14 M11.5,11 H11.6 M4.5,11 H8",
        // fan: rim and three blades
        Grupa.Hladjenje => "M1.5,8 A6.5,6.5 0 1 0 14.5,8 A6.5,6.5 0 1 0 1.5,8 M8,6.5 C8,3 10.5,3 11,4.5 " +
                           "M9.3,8.8 C12.3,10.5 11,12.7 9.5,13 M6.7,8.8 C3.7,10.5 3.5,7.8 4.5,6.5",
        _ => "",
    };
}

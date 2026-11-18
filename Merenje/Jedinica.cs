namespace Takt.Merenje;

/// <summary>What a number means. Decides the unit shown next to it and how many decimals it gets.</summary>
public enum Jedinica
{
    Nepoznato,
    Celzijus,
    Procenat,
    Megaherc,
    Obrtaj,
    Volt,
    Vat,
    Amper,
    Megabajt,
    MegabajtUSekundi,

    /// <summary>Frames per second of a game.</summary>
    Kadrovi,

    /// <summary>Time of one frame, in milliseconds (frametime).</summary>
    Milisekunda,
}

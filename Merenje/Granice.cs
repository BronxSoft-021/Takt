namespace Takt.Merenje;

/// <summary>
/// Limits a part reports about itself: "warn above 70 °C", "critical at 84 °C". They come through the same
/// sensor list as readings, but they are not readings - nothing about them is hot or full. The screen keeps
/// them under their own heading and uses them as the marks for that part's real temperature.
/// The name arrives translated when the program speaks English, so the check knows both languages; without that,
/// a limit would be drawn as a measurement in an English window.
/// </summary>
public static class Granice
{
    public const string Upozorenje = "Granica upozorenja";
    public const string Kriticna = "Kritična granica";
    public const string Rezerva = "Granica rezervnih blokova";

    public static bool JeGranica(string ime) =>
        ime is Upozorenje or Kriticna or Rezerva
        || ime is "Warning limit" or "Critical limit" or "Spare blocks limit";
}

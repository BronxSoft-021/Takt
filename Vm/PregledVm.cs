using System.Windows.Media;
using Takt.Merenje;

namespace Takt.Vm;

/// <summary>
/// One tile in the strip at the top: the headline number of a group and the shape of the last minute.
/// Clicking the tile opens that screen.
/// </summary>
public sealed class PregledVm
{
    public PregledVm(EkranVm ekran, SenzorVm? senzor)
    {
        Grupa = ekran.Grupa;
        Ikona = ekran.Ikona;
        Senzor = senzor;
    }

    public Grupa Grupa { get; }
    public Geometry Ikona { get; }
    public SenzorVm? Senzor { get; }
    public string Naslov => GrupaOpis.Naslov(Grupa).ToUpperInvariant();
    public string Opis => GrupaOpis.Opis(Grupa);
}

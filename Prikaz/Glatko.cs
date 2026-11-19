using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Takt.Prikaz;

/// <summary>
/// A wheel step that glides. Windows sends the wheel in whole notches, and jumping the offset by a notch at once
/// reads as a snap; here the offset walks to the new place in about a fifth of a second. A second notch while it
/// is still moving continues from where it is going, so a fast scroll does not stutter.
/// </summary>
public static class Glatko
{
    /// <summary>How much of what is left is covered per frame: 0,3 at 60 frames a second lands in ~0,2 s.</summary>
    private const double Korak = 0.3;

    private static readonly TimeSpan Kadenca = TimeSpan.FromMilliseconds(15);

    private static readonly ConditionalWeakTable<ScrollViewer, Stanje> Stanja = new();

    private sealed class Stanje
    {
        public DispatcherTimer? Tajmer;
        public double Cilj;
    }

    /// <summary>Moves a console sideways; <paramref name="pomeraj"/> is in pixels, positive to the right.</summary>
    public static void Vodoravno(ScrollViewer pogled, double pomeraj) =>
        Pomeri(pogled, pomeraj, vodoravno: true);

    /// <summary>Moves a page up or down; <paramref name="pomeraj"/> is in pixels, positive downwards.</summary>
    public static void Uspravno(ScrollViewer pogled, double pomeraj) =>
        Pomeri(pogled, pomeraj, vodoravno: false);

    private static void Pomeri(ScrollViewer pogled, double pomeraj, bool vodoravno)
    {
        double najvece = vodoravno ? pogled.ScrollableWidth : pogled.ScrollableHeight;
        if (najvece <= 0) return;

        var stanje = Stanja.GetOrCreateValue(pogled);

        // While a move is running the new notch is added to where it was going, not to where it is.
        double sada = stanje.Tajmer?.IsEnabled == true ? stanje.Cilj : vodoravno ? pogled.HorizontalOffset : pogled.VerticalOffset;
        stanje.Cilj = Math.Clamp(sada + pomeraj, 0, najvece);

        if (stanje.Tajmer is null)
        {
            stanje.Tajmer = new DispatcherTimer { Interval = Kadenca };
            stanje.Tajmer.Tick += (_, _) =>
            {
                double trenutno = vodoravno ? pogled.HorizontalOffset : pogled.VerticalOffset;
                double ostalo = stanje.Cilj - trenutno;

                if (Math.Abs(ostalo) < 0.8)
                {
                    Postavi(pogled, stanje.Cilj, vodoravno);
                    stanje.Tajmer!.Stop();
                    return;
                }

                Postavi(pogled, trenutno + ostalo * Korak, vodoravno);
            };
        }

        if (!stanje.Tajmer.IsEnabled) stanje.Tajmer.Start();
    }

    private static void Postavi(ScrollViewer pogled, double mesto, bool vodoravno)
    {
        if (vodoravno) pogled.ScrollToHorizontalOffset(mesto);
        else pogled.ScrollToVerticalOffset(mesto);
    }
}

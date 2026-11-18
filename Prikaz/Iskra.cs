using System.Windows;
using System.Windows.Media;
using Takt.Merenje;

namespace Takt.Prikaz;

/// <summary>
/// A small line without axes or labels: the shape of the last minute inside a tile at the top.
/// The same accent and the same soft fill as the big chart, so the two look like one program.
/// </summary>
public sealed class Iskra : FrameworkElement
{
    private const int BrojUzoraka = 60;

    public static readonly DependencyProperty PrstenProperty = DependencyProperty.Register(
        nameof(Prsten), typeof(Prsten), typeof(Iskra),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MeraProperty = DependencyProperty.Register(
        nameof(Mera), typeof(Jedinica), typeof(Iskra),
        new FrameworkPropertyMetadata(Jedinica.Nepoznato, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Raised by the view model on every reading; it exists only to ask for a redraw.</summary>
    public static readonly DependencyProperty TikProperty = DependencyProperty.Register(
        nameof(Tik), typeof(int), typeof(Iskra),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public Iskra()
    {
        ClipToBounds = true;
    }

    public Prsten? Prsten
    {
        get => (Prsten?)GetValue(PrstenProperty);
        set => SetValue(PrstenProperty, value);
    }

    public Jedinica Mera
    {
        get => (Jedinica)GetValue(MeraProperty);
        set => SetValue(MeraProperty, value);
    }

    public int Tik
    {
        get => (int)GetValue(TikProperty);
        set => SetValue(TikProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double sirina = ActualWidth, visina = ActualHeight;
        if (sirina < 12 || visina < 8) return;

        var uzorci = Prsten?.Poslednjih(BrojUzoraka) ?? [];
        var brojevi = uzorci.Where(v => v is not null).Select(v => v!.Value).ToArray();
        if (brojevi.Length < 2) return;

        double najmanje = brojevi.Min(), najvise = brojevi.Max();
        if (najvise - najmanje < 0.001)
        {
            najmanje -= 1;
            najvise += 1;
        }

        double rezerva = (najvise - najmanje) * 0.2;
        najmanje -= rezerva;
        najvise += rezerva;
        if (brojevi.Min() >= 0 && Mera is Jedinica.Procenat or Jedinica.Celzijus or Jedinica.Obrtaj
                or Jedinica.Vat or Jedinica.Megaherc or Jedinica.Megabajt or Jedinica.MegabajtUSekundi)
            najmanje = Math.Max(0, najmanje);
        if (najvise - najmanje < 0.001) najvise = najmanje + 1;

        double Y(double vrednost) => (1 - (vrednost - najmanje) / (najvise - najmanje)) * (visina - 2) + 1;
        double X(int indeks) => indeks * (sirina / Math.Max(1, uzorci.Length - 1));

        var akcenat = TryFindResource("Akcenat") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0xF0, 0xA2, 0x3B));
        var punjenje = new LinearGradientBrush(
            Color.FromArgb(60, akcenat.Color.R, akcenat.Color.G, akcenat.Color.B),
            Color.FromArgb(0, akcenat.Color.R, akcenat.Color.G, akcenat.Color.B),
            new Point(0, 0), new Point(0, 1));

        int red = 0;
        while (red < uzorci.Length)
        {
            while (red < uzorci.Length && uzorci[red] is null) red++;
            int pocetakNiza = red;
            while (red < uzorci.Length && uzorci[red] is not null) red++;
            int krajNiza = red - 1;
            if (krajNiza <= pocetakNiza) continue;

            var ispuna = new StreamGeometry();
            using (var ctx = ispuna.Open())
            {
                ctx.BeginFigure(new Point(X(pocetakNiza), visina), true, true);
                for (int k = pocetakNiza; k <= krajNiza; k++)
                    ctx.LineTo(new Point(X(k), Y(uzorci[k]!.Value)), true, false);
                ctx.LineTo(new Point(X(krajNiza), visina), true, false);
            }
            ispuna.Freeze();
            dc.DrawGeometry(punjenje, null, ispuna);

            var linija = new StreamGeometry();
            using (var ctx = linija.Open())
            {
                ctx.BeginFigure(new Point(X(pocetakNiza), Y(uzorci[pocetakNiza]!.Value)), false, false);
                for (int k = pocetakNiza + 1; k <= krajNiza; k++)
                    ctx.LineTo(new Point(X(k), Y(uzorci[k]!.Value)), true, false);
            }
            linija.Freeze();
            dc.DrawGeometry(null, new Pen(akcenat, 1.4) { LineJoin = PenLineJoin.Round }, linija);
        }
    }
}

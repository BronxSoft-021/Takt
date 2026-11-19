using System.Windows;
using System.Windows.Media;

namespace Takt.Prikaz;

/// <summary>
/// A column of segments, like the meter on a mixing desk. Lit from the bottom up to the value, in a ramp that
/// goes green - amber - red as it climbs; the peak is the same colour, dimmed, one or two segments above.
/// When the part reports its own warm and hot limits the ramp follows them (amber from the warm limit, red at
/// the hot one), so a cool part never looks hot just because it sits high on a fixed scale.
/// Drawn by hand: twenty rectangles, redrawn only when a value changes.
/// </summary>
public sealed class VuMerac : FrameworkElement
{
    private static readonly Brush Ugaseno = Zamrzni(Color.FromRgb(0x17, 0x1C, 0x24));
    private static readonly Brush GranicaUgaseno = Zamrzni(Color.FromRgb(0x3F, 0x1F, 0x1C));

    private static readonly Color Zeleno = Color.FromRgb(0x45, 0xC0, 0x8A);
    private static readonly Color Zuto = Color.FromRgb(0xE7, 0xB8, 0x4A);
    private static readonly Color Crveno = Color.FromRgb(0xF2, 0x63, 0x4F);

    /// <summary>Green to red in 101 frozen steps: a segment asks for the colour by its place on the scale.</summary>
    private static readonly Brush[] Rampa = NapraviRampu(1.0);

    /// <summary>The same ramp, darker: the peak marker, which must not outshine the segments.</summary>
    private static readonly Brush[] RampaTamna = NapraviRampu(0.45);

    public static readonly DependencyProperty UdeoProperty = Svojstvo(nameof(Udeo), 0.0);
    public static readonly DependencyProperty VrhProperty = Svojstvo(nameof(Vrh), 0.0);
    public static readonly DependencyProperty ToploGranicaProperty = Svojstvo(nameof(ToploGranica), (double?)null);
    public static readonly DependencyProperty VruceGranicaProperty = Svojstvo(nameof(VruceGranica), (double?)null);
    public static readonly DependencyProperty SegmenataProperty = Svojstvo(nameof(Segmenata), 20);

    private static DependencyProperty Svojstvo<T>(string ime, T podrazumevano) =>
        DependencyProperty.Register(ime, typeof(T), typeof(VuMerac),
            new FrameworkPropertyMetadata(podrazumevano, FrameworkPropertyMetadataOptions.AffectsRender));

    private static Brush Zamrzni(Color boja)
    {
        var cetka = new SolidColorBrush(boja);
        cetka.Freeze();
        return cetka;
    }

    private static Brush[] NapraviRampu(double jacina)
    {
        var rampa = new Brush[101];
        for (int i = 0; i <= 100; i++)
        {
            double t = i / 100.0;
            var boja = t < 0.5 ? Pomesaj(Zeleno, Zuto, t * 2) : Pomesaj(Zuto, Crveno, (t - 0.5) * 2);
            rampa[i] = Zamrzni(jacina >= 1 ? boja : Pomesaj(Color.FromRgb(0x0A, 0x0C, 0x10), boja, jacina));
        }
        return rampa;
    }

    private static Color Pomesaj(Color od, Color ka, double t) => Color.FromRgb(
        (byte)Math.Round(od.R + (ka.R - od.R) * t),
        (byte)Math.Round(od.G + (ka.G - od.G) * t),
        (byte)Math.Round(od.B + (ka.B - od.B) * t));

    /// <summary>
    /// Where a segment sits on the green - amber - red ramp (0 green, 50 amber, 100 red). With the part's own
    /// limits the amber starts at the warm limit and the red at the hot one; without them the scale is the ramp.
    /// Public for a test.
    /// </summary>
    public static double Polozaj(double udeo, double? toplo, double? vruce)
    {
        if (vruce is { } v && v > 0.02)
        {
            double topla = toplo is { } t && t > 0.02 && t < v ? t : v * 0.75;
            if (udeo >= v) return 100;
            if (udeo >= topla) return 50 + 50 * (udeo - topla) / (v - topla);
            return Math.Clamp(50 * udeo / topla, 0, 50);
        }

        return Math.Clamp(udeo * 100, 0, 100);
    }

    public double Udeo { get => (double)GetValue(UdeoProperty); set => SetValue(UdeoProperty, value); }
    public double Vrh { get => (double)GetValue(VrhProperty); set => SetValue(VrhProperty, value); }
    public double? ToploGranica { get => (double?)GetValue(ToploGranicaProperty); set => SetValue(ToploGranicaProperty, value); }
    public double? VruceGranica { get => (double?)GetValue(VruceGranicaProperty); set => SetValue(VruceGranicaProperty, value); }
    public int Segmenata { get => (int)GetValue(SegmenataProperty); set => SetValue(SegmenataProperty, value); }

    /// <summary>How many segments are lit for a share of the scale: any value above zero lights at least one.</summary>
    public static int Upaljeno(double udeo, int segmenata) =>
        udeo <= 0 ? 0 : Math.Clamp((int)Math.Ceiling(udeo * segmenata - 1e-9), 1, segmenata);

    private DrawingGroup? _pozadina;
    private double _pozSirina = -1, _pozVisina = -1;
    private int _pozN;
    private double? _pozGranica;

    protected override void OnRender(DrawingContext dc)
    {
        int n = Math.Max(4, Segmenata);
        double sirina = ActualWidth, visina = ActualHeight;
        if (sirina < 2 || visina < n * 2) return;

        // The unlit track never changes while the size and the limit do not: draw it once, keep it frozen, and
        // per reading paint only the lit segments and the peak.
        if (_pozadina is null || _pozN != n || _pozSirina != sirina || _pozVisina != visina || _pozGranica != VruceGranica)
            NapraviPozadinu(n, sirina, visina);

        dc.DrawDrawing(_pozadina!);

        const double razmak = 2;
        double visinaSegmenta = (visina - razmak * (n - 1)) / n;

        int upaljeno = Upaljeno(Udeo, n);
        int vrh = Upaljeno(Vrh, n);

        for (int i = 0; i < n; i++)
        {
            int redni = i + 1;                      // 1 = bottom segment
            double donjiUdeo = (double)i / n;       // where this segment starts on the scale

            // The colour follows the segment's own place: green low, amber halfway, red at the top.
            int mesto = (int)Math.Round(Polozaj(donjiUdeo, ToploGranica, VruceGranica));

            Brush? boja = redni <= upaljeno ? Rampa[mesto]
                : redni == vrh ? RampaTamna[mesto]
                : null;
            if (boja is null) continue;

            double y = visina - redni * visinaSegmenta - i * razmak;
            dc.DrawRoundedRectangle(boja, null, new Rect(0, Math.Round(y), sirina, Math.Max(1, Math.Round(visinaSegmenta))), 1, 1);
        }
    }

    private void NapraviPozadinu(int n, double sirina, double visina)
    {
        const double razmak = 2;
        double visinaSegmenta = (visina - razmak * (n - 1)) / n;
        int granica = VruceGranica is { } g ? Math.Clamp((int)Math.Ceiling(g * n - 1e-9), 1, n) : 0;

        var grupa = new DrawingGroup();
        using (var dc = grupa.Open())
        {
            for (int i = 0; i < n; i++)
            {
                int redni = i + 1;
                var boja = redni == granica ? GranicaUgaseno : Ugaseno;
                double y = visina - redni * visinaSegmenta - i * razmak;
                dc.DrawRoundedRectangle(boja, null, new Rect(0, Math.Round(y), sirina, Math.Max(1, Math.Round(visinaSegmenta))), 1, 1);
            }
        }

        grupa.Freeze();
        _pozadina = grupa;
        _pozN = n;
        _pozSirina = sirina;
        _pozVisina = visina;
        _pozGranica = VruceGranica;
    }
}

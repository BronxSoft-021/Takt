using System.Windows;
using System.Windows.Media;

namespace Takt.Prikaz;

/// <summary>
/// A column of segments, like the meter on a mixing desk. Lit from the bottom up to the value; the peak is one
/// dimmer segment above it; segments above the part's warm and hot limits light amber and red, and the
/// segment at the hot limit glows faintly even when unlit, so the limit is always on the scale.
/// Drawn by hand: twenty rectangles, redrawn only when a value changes.
/// </summary>
public sealed class VuMerac : FrameworkElement
{
    private static readonly Brush Ugaseno = Zamrzni(Color.FromRgb(0x22, 0x27, 0x2B));
    private static readonly Brush GranicaUgaseno = Zamrzni(Color.FromRgb(0x4A, 0x24, 0x20));
    private static readonly Brush Akcenat = Zamrzni(Color.FromRgb(0xF0, 0xA2, 0x3B));
    private static readonly Brush Toplo = Zamrzni(Color.FromRgb(0xE8, 0xB3, 0x3A));
    private static readonly Brush Vruce = Zamrzni(Color.FromRgb(0xE5, 0x65, 0x4C));
    private static readonly Brush VrhObican = Zamrzni(Color.FromRgb(0x8A, 0x66, 0x30));
    private static readonly Brush VrhVruc = Zamrzni(Color.FromRgb(0x8A, 0x3A, 0x2C));

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

            bool vruc = VruceGranica is { } v && donjiUdeo >= v - 1e-9;
            bool topao = !vruc && ToploGranica is { } t && donjiUdeo >= t - 1e-9;

            Brush? boja = redni <= upaljeno
                ? vruc ? Vruce : topao ? Toplo : Akcenat
                : redni == vrh ? vruc ? VrhVruc : VrhObican : null;
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

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Takt.Merenje;

namespace Takt.Prikaz;

/// <summary>
/// The line of the last samples. Drawn by hand on purpose: a chart library would add megabytes to a program
/// whose whole point is to sit quietly in the background, and this needs no more than a polyline, a grid and
/// a break where a value is missing.
/// </summary>
public sealed class Grafikon : FrameworkElement
{
    /// <summary>How many readings fit in the window.</summary>
    private const int BrojUzoraka = 120;

    private static readonly CultureInfo Kultura = CultureInfo.GetCultureInfo("sr-Latn-RS");
    private static readonly Typeface Pismo = new("Segoe UI");

    // Brushes and pens are frozen and shared: OnRender runs every second, and a new gradient each time is
    // garbage for the collector and work for nothing.
    private static readonly Color BojaAkcenta = Color.FromRgb(0xF0, 0xA2, 0x3B);
    private static readonly Brush AkcenatCetka = Zamrzni(new SolidColorBrush(BojaAkcenta));
    private static readonly Brush MrezaCetka = Zamrzni(new SolidColorBrush(Color.FromRgb(0x2E, 0x34, 0x39)));

    private static readonly Pen Olovka = Zamrzni(new Pen(AkcenatCetka, 2.0)
    {
        LineJoin = PenLineJoin.Round,
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round,
    });

    private static readonly Pen OlovkaMreze = Zamrzni(new Pen(MrezaCetka, 1)
    {
        DashStyle = new DashStyle([3, 4], 0),
    });

    private static readonly LinearGradientBrush Ispuna = Zamrzni(new LinearGradientBrush(
        Color.FromArgb(70, BojaAkcenta.R, BojaAkcenta.G, BojaAkcenta.B),
        Color.FromArgb(0, BojaAkcenta.R, BojaAkcenta.G, BojaAkcenta.B),
        new Point(0, 0), new Point(0, 1)));

    private static readonly Pen OlovkaMisa = Zamrzni(new Pen(
        new SolidColorBrush(Color.FromArgb(110, 0xED, 0xE9, 0xE1)), 1));

    private static readonly Pen OlovkaTacke = Zamrzni(new Pen(AkcenatCetka, 2));

    private static T Zamrzni<T>(T cetka) where T : Freezable
    {
        cetka.Freeze();
        return cetka;
    }

    public static readonly DependencyProperty PrstenProperty = DependencyProperty.Register(
        nameof(Prsten), typeof(Prsten), typeof(Grafikon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MeraProperty = DependencyProperty.Register(
        nameof(Mera), typeof(Jedinica), typeof(Grafikon),
        new FrameworkPropertyMetadata(Jedinica.Nepoznato, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Raised by the view model on every reading; it exists only to ask for a redraw.</summary>
    public static readonly DependencyProperty TikProperty = DependencyProperty.Register(
        nameof(Tik), typeof(int), typeof(Grafikon),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Seconds between two readings: turns "120 samples" into "pre 2 min" under the chart.</summary>
    public static readonly DependencyProperty PeriodaProperty = DependencyProperty.Register(
        nameof(Perioda), typeof(int), typeof(Grafikon),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Where the mouse is over the drawing, or null: a thin line and the value at that moment.</summary>
    private Point? _mis;

    public Grafikon()
    {
        ClipToBounds = true;
        SnapsToDevicePixels = true;
        Cursor = System.Windows.Input.Cursors.Cross;
    }

    public int Perioda
    {
        get => (int)GetValue(PeriodaProperty);
        set => SetValue(PeriodaProperty, value);
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _mis = e.GetPosition(this);
        InvalidateVisual();
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _mis = null;
        InvalidateVisual();
    }

    /// <summary>"pre 45 s", "pre 2 min", "pre 1 h 10 min" - how long ago the oldest point on the chart was read.</summary>
    public static string PreKoliko(int sekundi)
    {
        if (sekundi < 90) return $"pre {Math.Max(1, sekundi)} s";
        int minuta = (int)Math.Round(sekundi / 60.0);
        if (minuta < 60) return $"pre {minuta} min";
        return minuta % 60 == 0 ? $"pre {minuta / 60} h" : $"pre {minuta / 60} h {minuta % 60} min";
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
        if (sirina < 80 || visina < 70) return;

        var pozadina = Cetka("GrafikonPozadina", Color.FromRgb(0x14, 0x18, 0x1B));
        var linija = Cetka("Linija", Color.FromRgb(0x2E, 0x34, 0x39));
        var akcenat = AkcenatCetka;
        var mutan = Cetka("Mutan", Color.FromRgb(0xA8, 0xA4, 0x9C));

        dc.DrawRoundedRectangle(pozadina, new Pen(linija, 1), new Rect(0.5, 0.5, sirina - 1, visina - 1), 12, 12);

        double levo = 14, desno = 58, gore = 16, dole = 26;
        double sirinaCrteza = sirina - levo - desno;
        double visinaCrteza = visina - gore - dole;
        if (sirinaCrteza < 20 || visinaCrteza < 20) return;

        var uzorci = Prsten?.Poslednjih(BrojUzoraka) ?? [];
        var brojevi = uzorci.Where(v => v is not null).Select(v => v!.Value).ToArray();

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        if (brojevi.Length == 0)
        {
            var nema = Tekst("Nema podataka", 12, mutan, dpi);
            dc.DrawText(nema, new Point((sirina - nema.Width) / 2, (visina - nema.Height) / 2));
            return;
        }

        double najmanje = brojevi.Min();
        double najvise = brojevi.Max();
        if (najvise - najmanje < 0.001)
        {
            double pomeraj = Math.Max(Math.Abs(najvise) * 0.1, 1);
            najmanje -= pomeraj;
            najvise += pomeraj;
        }

        double rezerva = (najvise - najmanje) * 0.12;
        najmanje -= rezerva;
        najvise += rezerva;

        // Percentages, temperatures, clocks, fans, watts and bytes never go below zero, so a scale that dips
        // under it (and shows 0 % as a line in the middle) would be misleading.
        if (brojevi.Min() >= 0 && Mera is Jedinica.Procenat or Jedinica.Celzijus or Jedinica.Obrtaj
                or Jedinica.Vat or Jedinica.Megaherc or Jedinica.Megabajt or Jedinica.MegabajtUSekundi)
            najmanje = Math.Max(0, najmanje);

        if (najvise - najmanje < 0.001) najvise = najmanje + 1;

        double Y(double vrednost) => gore + (1 - (vrednost - najmanje) / (najvise - najmanje)) * visinaCrteza;

        // With only a few readings the line spans the whole width instead of clinging to the right edge;
        // once there are 120 it scrolls.
        double X(int indeks) => levo + indeks * (sirinaCrteza / Math.Max(1, uzorci.Length - 1));

        // Grid with values on the right. The scale is rounded to steps a person reads easily (1, 2 or 5 times
        // a power of ten), and a narrow range gets decimals, or every label would read the same.
        double opseg = Math.Max(najvise - najmanje, 0.001);
        double korak = LepKorak(opseg / 4);
        najmanje = Math.Floor(najmanje / korak) * korak;
        najvise = najmanje + korak * 4;

        int decimala = 0;
        if (korak < 1) decimala = 1;
        if (korak < 0.1) decimala = 2;
        if (korak < 0.01) decimala = 3;

        // Memory of a gigabyte and more is labelled in GB, like the number above the chart.
        double delilac = Mera != Jedinica.Megabajt ? 1
            : Math.Abs(najvise) >= 1_048_576 ? 1_048_576
            : Math.Abs(najvise) >= 1024 ? 1024 : 1;
        bool uGb = delilac > 1;

        var olovkaMreze = OlovkaMreze;
        for (int i = 0; i <= 4; i++)
        {
            double deo = i / 4.0;
            double y = Math.Round(gore + deo * visinaCrteza) + 0.5;
            dc.DrawLine(olovkaMreze, new Point(levo, y), new Point(levo + sirinaCrteza, y));

            double vrednost = najvise - deo * (najvise - najmanje);
            var tekst = uGb ? (vrednost / delilac).ToString(decimala > 0 || delilac > 1024 ? "F2" : "F1", Kultura)
                            : Format.Broj(vrednost, Mera, decimala);
            var oznaka = Tekst(tekst, 10, mutan, dpi);
            dc.DrawText(oznaka, new Point(levo + sirinaCrteza + 9, y - oznaka.Height / 2));
        }

        // The line, broken where readings are missing, with a soft fill under it.
        var punjenje = Ispuna;

        int red = 0;
        while (red < uzorci.Length)
        {
            while (red < uzorci.Length && uzorci[red] is null) red++;
            int pocetakNiza = red;
            while (red < uzorci.Length && uzorci[red] is not null) red++;
            int krajNiza = red - 1;
            if (krajNiza < pocetakNiza) break;

            var ispuna = new StreamGeometry();
            using (var ctx = ispuna.Open())
            {
                ctx.BeginFigure(new Point(X(pocetakNiza), gore + visinaCrteza), true, true);
                for (int k = pocetakNiza; k <= krajNiza; k++)
                    ctx.LineTo(new Point(X(k), Y(uzorci[k]!.Value)), true, false);
                ctx.LineTo(new Point(X(krajNiza), gore + visinaCrteza), true, false);
            }
            ispuna.Freeze();
            dc.DrawGeometry(punjenje, null, ispuna);
        }

        var geometrija = new StreamGeometry();
        using (var ctx = geometrija.Open())
        {
            bool otvoreno = false;
            for (int j = 0; j < uzorci.Length; j++)
            {
                if (uzorci[j] is not { } vrednost)
                {
                    otvoreno = false;
                    continue;
                }

                var tacka = new Point(X(j), Y(vrednost));
                if (!otvoreno)
                {
                    ctx.BeginFigure(tacka, false, false);
                    otvoreno = true;
                }
                else
                {
                    ctx.LineTo(tacka, true, false);
                }
            }
        }
        geometrija.Freeze();

        var olovka = Olovka;
        dc.DrawGeometry(null, olovka, geometrija);

        // The newest reading as a dot.
        for (int zadnji = uzorci.Length - 1; zadnji >= 0; zadnji--)
        {
            if (uzorci[zadnji] is not { } zadnja) continue;
            dc.DrawEllipse(akcenat, null, new Point(X(zadnji), Y(zadnja)), 2.6, 2.6);
            break;
        }

        var pocetak = Tekst(PreKoliko((uzorci.Length - 1) * Math.Max(1, Perioda)), 9.5, mutan, dpi);
        var sada = Tekst("sada", 9.5, mutan, dpi);
        dc.DrawText(pocetak, new Point(levo, visina - dole + 7));
        dc.DrawText(sada, new Point(levo + sirinaCrteza - sada.Width, visina - dole + 7));

        // Under the mouse: a thin vertical line, a dot on the curve and the value with how long ago it was read.
        if (_mis is { } mis && mis.X >= levo - 4 && mis.X <= levo + sirinaCrteza + 4 && uzorci.Length > 1)
        {
            double korakX = sirinaCrteza / (uzorci.Length - 1);
            int indeks = (int)Math.Round(Math.Clamp((mis.X - levo) / korakX, 0, uzorci.Length - 1));
            double x = X(indeks);

            var olovkaMisa = OlovkaMisa;
            dc.DrawLine(olovkaMisa, new Point(Math.Round(x) + 0.5, gore), new Point(Math.Round(x) + 0.5, gore + visinaCrteza));

            string zaTo = uzorci[indeks] is { } v ? Format.Vrednost(v, Mera) : Format.Nema;
            int pre = (uzorci.Length - 1 - indeks) * Math.Max(1, Perioda);
            string kada = pre == 0 ? "sada" : PreKoliko(pre);

            if (uzorci[indeks] is { } tackaVrednost)
            {
                var tacka = new Point(x, Y(tackaVrednost));
                dc.DrawEllipse(pozadina, OlovkaTacke, tacka, 4, 4);
            }

            var tekstVrednosti = Tekst(zaTo, 12, Cetka("Tekst", Color.FromRgb(0xED, 0xE9, 0xE1)), dpi);
            tekstVrednosti.SetFontWeight(FontWeights.SemiBold);
            var tekstVremena = Tekst(kada, 9.5, mutan, dpi);

            double sirinaBalona = Math.Max(tekstVrednosti.Width, tekstVremena.Width) + 18;
            double visinaBalona = tekstVrednosti.Height + tekstVremena.Height + 10;
            double bx = x + 10;
            if (bx + sirinaBalona > levo + sirinaCrteza) bx = x - 10 - sirinaBalona;
            double by = gore + 4;

            var balon = new Rect(bx, by, sirinaBalona, visinaBalona);
            dc.DrawRoundedRectangle(Cetka("PovrsinaHover", Color.FromRgb(0x24, 0x2A, 0x2F)),
                new Pen(Cetka("LinijaJaka", Color.FromRgb(0x3A, 0x42, 0x49)), 1), balon, 7, 7);
            dc.DrawText(tekstVrednosti, new Point(bx + 9, by + 5));
            dc.DrawText(tekstVremena, new Point(bx + 9, by + 5 + tekstVrednosti.Height));
        }
    }

    private readonly Dictionary<(string Tekst, double Velicina, double Dpi), FormattedText> _kesTeksta = [];

    private FormattedText Tekst(string tekst, double velicina, Brush boja, double dpi)
    {
        var kljuc = (tekst, velicina, dpi);
        if (_kesTeksta.TryGetValue(kljuc, out var gotov)) return gotov;

        // The scale rarely moves, so the same seven labels come back every second: shaping them once is enough.
        if (_kesTeksta.Count > 64) _kesTeksta.Clear();
        var novi = new FormattedText(tekst, Kultura, FlowDirection.LeftToRight, Pismo, velicina, boja, dpi);
        _kesTeksta[kljuc] = novi;
        return novi;
    }

    private SolidColorBrush Cetka(string kljuc, Color rezerva) =>
        TryFindResource(kljuc) as SolidColorBrush ?? new SolidColorBrush(rezerva);

    /// <summary>Rounds a step to 1, 2 or 5 times a power of ten, so the scale reads 0/2/4/6 and not 0/1,35/2,7.</summary>
    private static double LepKorak(double korak)
    {
        if (korak <= 0 || double.IsNaN(korak)) return 1;

        double red = Math.Pow(10, Math.Floor(Math.Log10(korak)));
        double odnos = korak / red;
        double lep = odnos <= 1 ? 1 : odnos <= 2 ? 2 : odnos <= 5 ? 5 : 10;
        return lep * red;
    }
}

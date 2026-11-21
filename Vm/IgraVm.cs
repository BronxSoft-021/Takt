using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Vm;

/// <summary>
/// The game on screen: which one, how many frames per second, the 1 % low and the frame time, and the line of the
/// longest frame of every second - a stutter is a spike there. Fed every reading from what the service sees.
/// </summary>
public sealed class IgraVm : ObservableObject
{
    private bool _aktivna;
    private string _ime = "";
    private string? _putanja;
    private int _pid = -1;
    private string _proces = "";
    private ImageSource? _ikona;
    private double? _fps, _low1, _frametime;
    private int _tik;
    private readonly Queue<int> _zastoji = new();

    /// <summary>The longest frame of each second (ms), for the chart. 600 = ten minutes at one reading per second.</summary>
    public Prsten Frametime { get; } = new(600);

    public bool Aktivna
    {
        get => _aktivna;
        private set => SetProperty(ref _aktivna, value);
    }

    public string Ime
    {
        get => _ime;
        private set => SetProperty(ref _ime, value);
    }

    public ImageSource? Ikona
    {
        get => _ikona;
        private set => SetProperty(ref _ikona, value);
    }

    public string FpsTekst => _fps is { } f ? Format.Broj(f, Jedinica.Kadrovi) : Format.Nema;
    public string Low1Tekst => _low1 is { } l ? Format.Broj(l, Jedinica.Kadrovi) : Format.Nema;

    /// <summary>
    /// The average frame time of the last second. It says "prosek" because the chart beside it draws the longest
    /// frame of each second - two different numbers that used to stand one under the other unlabelled.
    /// </summary>
    public string FrametimeTekst => _frametime is null ? Format.Nema : "prosek " + Format.Vrednost(_frametime, Jedinica.Milisekunda);

    public double? Fps => _fps;

    /// <summary>The game's process: the window that gets the focus back when the Scene opens.</summary>
    public int? Pid => _pid;

    /// <summary>The 1 % low of the last reading, as a number (the session keeps these).</summary>
    public double? Low1 => _low1;

    /// <summary>Frames that stalled in the last second, as the service counted them.</summary>
    public int PoslednjiZastoji { get; private set; }

    /// <summary>"2 zastoja u poslednjih 60 s", or "bez zastoja" when the game runs smooth.</summary>
    public string ZastojiTekst
    {
        get
        {
            int n = _zastoji.Sum();
            return n switch
            {
                0 => Tekst.T("bez zastoja u poslednjih 60 s"),
                _ when n % 10 == 1 && n % 100 != 11 => Tekst.P("{0} zastoj u poslednjih 60 s", n),
                _ when n % 10 is >= 2 and <= 4 && n % 100 is < 12 or > 14 => Tekst.P("{0} zastoja u poslednjih 60 s", n),
                _ => Tekst.P("{0} zastoja u poslednjih 60 s", n),
            };
        }
    }

    /// <summary>
    /// The session's own line ("snima sesiju 1:42:10") lives in <see cref="SnimacSesije"/> and nowhere else: beside
    /// it the Scene draws a dot, and the two used to come from different owners - after a game stopped the dot was
    /// out while this text still said "snima sesiju" (fixed 19. 11. 2026, one master).
    /// </summary>
    public int Tik
    {
        get => _tik;
        private set => SetProperty(ref _tik, value);
    }

    /// <summary>One reading: the game now on screen, or null when none is.</summary>
    public void Postavi(Protokol.Igra? igra)
    {
        if (igra is null)
        {
            if (Aktivna)
            {
                Aktivna = false;
                Frametime.Ocisti();
                _zastoji.Clear();
            }
            return;
        }

        // The same game is the same process: compared by pid and the raw process name. Comparing the raw name
        // with the shortened one ("SWGR-Win64-Shipping" against "SWGR") made every reading look like a new game,
        // and the chart was wiped every second (first real game, 18. 11. 2026).
        if (!Aktivna || igra.Pid != _pid || igra.Ime != _proces)
        {
            _pid = igra.Pid;
            _proces = igra.Ime;
            _putanja = igra.Putanja;
            Ime = LepoIme(igra.Ime);
            Ikona = Slicica(igra.Putanja);
            Frametime.Ocisti();
            _zastoji.Clear();
        }

        Aktivna = true;
        _fps = igra.Fps;
        _low1 = igra.Low1 > 0 ? igra.Low1 : null;
        _frametime = igra.ProsekMs > 0 ? igra.ProsekMs : null;
        Frametime.Dodaj(igra.NajviseMs > 0 ? igra.NajviseMs : null);

        PoslednjiZastoji = igra.Zastoji;
        _zastoji.Enqueue(igra.Zastoji);
        while (_zastoji.Count > 60) _zastoji.Dequeue();

        OnPropertyChanged(nameof(FpsTekst));
        OnPropertyChanged(nameof(Low1Tekst));
        OnPropertyChanged(nameof(FrametimeTekst));
        OnPropertyChanged(nameof(Fps));
        OnPropertyChanged(nameof(ZastojiTekst));
        Tik++;
    }

    /// <summary>"Hellblade2-Win64-Shipping" → "Hellblade2": the process name, without the engine's tail.</summary>
    public static string LepoIme(string proces)
    {
        var ime = proces;
        foreach (var rep in new[] { "-Win64-Shipping", "-WinGDK-Shipping", "_x64", "-x64", "_dx12", "_dx11", "DX12", "DX11" })
            ime = ime.Replace(rep, "", StringComparison.OrdinalIgnoreCase);
        return ime.Trim(' ', '-', '_');
    }

    /// <summary>The game's own icon from its .exe, frozen; null when the file cannot be read.</summary>
    private static ImageSource? Slicica(string? putanja)
    {
        if (string.IsNullOrEmpty(putanja) || !File.Exists(putanja)) return null;
        try
        {
            using var ikona = System.Drawing.Icon.ExtractAssociatedIcon(putanja);
            if (ikona is null) return null;
            var slika = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(ikona.Handle,
                System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            slika.Freeze();
            return slika;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Ikonica igre", ex);
            return null;
        }
    }
}

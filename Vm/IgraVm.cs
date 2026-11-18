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
    private ImageSource? _ikona;
    private double? _fps, _low1, _frametime;
    private int _tik;
    private string _sesija = "";
    private readonly Queue<int> _stucanja = new();

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
    public string FrametimeTekst => Format.Vrednost(_frametime, Jedinica.Milisekunda);

    public double? Fps => _fps;

    /// <summary>The 1 % low of the last reading, as a number (the session keeps these).</summary>
    public double? Low1 => _low1;

    /// <summary>Stutters in the last second, as the service counted them.</summary>
    public int PoslednjaStucanja { get; private set; }

    /// <summary>"2 štucanja u poslednjih 60 s", or nothing when the game runs smooth.</summary>
    public string StucanjaTekst
    {
        get
        {
            int n = _stucanja.Sum();
            return n switch
            {
                0 => "bez štucanja u poslednjih 60 s",
                _ when n % 10 == 1 && n % 100 != 11 => $"{n} štucanje u poslednjih 60 s",
                _ when n % 10 is >= 2 and <= 4 && n % 100 is < 12 or > 14 => $"{n} štucanja u poslednjih 60 s",
                _ => $"{n} štucanja u poslednjih 60 s",
            };
        }
    }

    /// <summary>"snima sesiju 1:42:10" while a game session is being recorded.</summary>
    public string Sesija
    {
        get => _sesija;
        set => SetProperty(ref _sesija, value);
    }

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
                _stucanja.Clear();
            }
            return;
        }

        if (!Aktivna || igra.Putanja != _putanja || igra.Ime != Ime)
        {
            _putanja = igra.Putanja;
            Ime = LepoIme(igra.Ime);
            Ikona = Slicica(igra.Putanja);
            Frametime.Ocisti();
            _stucanja.Clear();
        }

        Aktivna = true;
        _fps = igra.Fps;
        _low1 = igra.Low1 > 0 ? igra.Low1 : null;
        _frametime = igra.ProsekMs > 0 ? igra.ProsekMs : null;
        Frametime.Dodaj(igra.NajviseMs > 0 ? igra.NajviseMs : null);

        PoslednjaStucanja = igra.Stucanja;
        _stucanja.Enqueue(igra.Stucanja);
        while (_stucanja.Count > 60) _stucanja.Dequeue();

        OnPropertyChanged(nameof(FpsTekst));
        OnPropertyChanged(nameof(Low1Tekst));
        OnPropertyChanged(nameof(FrametimeTekst));
        OnPropertyChanged(nameof(Fps));
        OnPropertyChanged(nameof(StucanjaTekst));
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

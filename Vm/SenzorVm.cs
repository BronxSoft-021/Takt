using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Merenje;

namespace Takt.Vm;

/// <summary>One row in the list: the sensor's name, its last value and the smallest/largest seen since start.</summary>
public sealed class SenzorVm : ObservableObject
{
    private string _vrednost = Format.Nema;
    private string _najmanje = Format.Nema;
    private string _najvise = Format.Nema;
    private string _prosek = Format.Nema;
    private double? _broj;
    private bool _imaloVrednost;
    private int _tik;
    private double _zbir;
    private long _brojVrednosti;

    public SenzorVm(Senzor senzor, int indeks, Prsten prsten)
    {
        Senzor = senzor;
        Indeks = indeks;
        Prsten = prsten;
    }

    public Senzor Senzor { get; }
    public int Indeks { get; }
    public Prsten Prsten { get; }

    public string Ime => Senzor.Ime;
    public string Uredjaj => Senzor.Uredjaj;

    /// <summary>A limit the part reports about itself ("Kritična granica 84 °C"), not a reading.</summary>
    public bool JeGranica => Granice.JeGranica(Ime);

    /// <summary>
    /// The drive's own warning and critical limits, when it reports them: its temperature turns amber and red
    /// at those marks instead of the general ones. Set once by <see cref="EkranVm"/>.
    /// </summary>
    public SenzorVm? GranicaToplo { get; set; }
    public SenzorVm? GranicaVruce { get; set; }

    /// <summary>The heading the row sits under in the list: "Temperature", "Takt", "Ventilatori"…</summary>
    public string Vrsta => JeGranica ? "Granice koje prijavljuje uređaj" : Senzor.Jedinica switch
    {
        Jedinica.Celzijus => "Temperature",
        Jedinica.Procenat => "Opterećenje i zauzeće",
        Jedinica.Megaherc => "Takt",
        Jedinica.Obrtaj => "Ventilatori",
        Jedinica.Vat => "Potrošnja",
        Jedinica.Volt => "Napon",
        Jedinica.Amper => "Struja",
        Jedinica.Megabajt => "Količina",
        Jedinica.MegabajtUSekundi => "Protok",
        _ => "Ostalo",
    };

    public double? Broj
    {
        get => _broj;
        private set => SetProperty(ref _broj, value);
    }

    /// <summary>
    /// True once the sensor has given at least one number. Without the driver a third of the processor's
    /// sensors never do; the list keeps those apart instead of showing a wall of "—".
    /// </summary>
    public bool ImaloVrednost
    {
        get => _imaloVrednost;
        private set => SetProperty(ref _imaloVrednost, value);
    }

    public string Vrednost
    {
        get => _vrednost;
        private set => SetProperty(ref _vrednost, value);
    }

    public string Najmanje
    {
        get => _najmanje;
        private set => SetProperty(ref _najmanje, value);
    }

    public string Najvise
    {
        get => _najvise;
        private set => SetProperty(ref _najvise, value);
    }

    /// <summary>Average of every reading since the program started, the same span as the smallest and largest.</summary>
    public string Prosek
    {
        get => _prosek;
        private set => SetProperty(ref _prosek, value);
    }

    /// <summary>Changes on every reading; the chart listens to it so it redraws without a timer of its own.</summary>
    public int Tik
    {
        get => _tik;
        private set => SetProperty(ref _tik, value);
    }

    /// <summary>Number without the unit, and the unit on its own: the row shows them in two sizes.</summary>
    public string BrojPrikaz => Broj is { } broj ? Format.Delovi(broj, Senzor.Jedinica).Broj : Format.Nema;

    public string JedinicaPrikaz => Broj is { } broj ? Format.Delovi(broj, Senzor.Jedinica).Oznaka : "";

    /// <summary>
    /// How full the thin bar under the row is, 0 to 1. Only for values with a natural ceiling of a hundred
    /// (percent and degrees); a clock or a fan has no "full", so it gets no bar rather than an invented one.
    /// </summary>
    public double? Udeo => !JeGranica && Broj is { } broj && Senzor.Jedinica is Jedinica.Procenat or Jedinica.Celzijus
        ? Math.Clamp(broj / 100, 0, 1)
        : null;

    public bool ImaUdeo => Udeo is not null;

    /// <summary>"toplo" or "vruce" when a temperature is above what the part likes; empty otherwise.</summary>
    public string Nivo
    {
        get
        {
            if (Broj is not { } broj || Pragovi() is not { } prag) return "";
            if (broj >= prag.Vruce) return "vruce";
            if (broj >= prag.Toplo) return "toplo";
            return "";
        }
    }

    /// <summary>
    /// Where a temperature stops being comfortable. A graphics hotspot and VRAM run hotter by design, a disk
    /// cooler; everything else (processor, card, board) shares one pair.
    /// </summary>
    public (double Toplo, double Vruce)? Pragovi()
    {
        if (Senzor.Jedinica != Jedinica.Celzijus || JeGranica) return null;

        // The board's numbered temperatures ("Temperatura 3"): nobody knows what they measure on this board, and an
        // unconnected input often reads a steady 80-90 °C. No warm or hot colour, no clip light, for what may be
        // nothing at all; the number itself is still shown.
        if (Senzor.Grupa == Grupa.Hladjenje && Ime.StartsWith("Temperatura ", StringComparison.Ordinal)
            && char.IsAsciiDigit(Ime[^1])) return null;

        // The drive knows best: its own limits, when it gives them (one is enough, the other follows from it).
        double? toplo = GranicaToplo?.Broj, vruce = GranicaVruce?.Broj;
        if (toplo is { } t && vruce is { } v && v > t) return (t, v);
        if (vruce is { } samoVruce) return (samoVruce - 10, samoVruce);
        if (toplo is { } samoToplo) return (samoToplo, samoToplo + 10);

        if (Senzor.Grupa == Grupa.Diskovi) return (55, 65);
        if (Senzor.Grupa == Grupa.Grafika && (Ime.Contains("Hotspot") || Ime.Contains("VRAM"))) return (95, 105);
        return (80, 90);
    }

    /// <summary>
    /// Takes the reading and works out the texts, but tells the window nothing. Called on every tick: the meters
    /// and the chart read the numbers directly, so only the table needs a notification, and it can wait.
    /// Returns true when the sensor spoke for the first time.
    /// </summary>
    public bool Postavi(double? vrednost, double? najmanje, double? najvise)
    {
        bool novo = vrednost is not null && !_imaloVrednost;

        _broj = vrednost;
        if (vrednost is { } v)
        {
            _imaloVrednost = true;
            _zbir += v;
            _brojVrednosti++;
        }

        _vrednost = Format.Vrednost(vrednost, Senzor.Jedinica);
        _najmanje = Format.Vrednost(najmanje, Senzor.Jedinica);
        _najvise = Format.Vrednost(najvise, Senzor.Jedinica);
        _prosek = Format.Vrednost(_brojVrednosti > 0 ? _zbir / _brojVrednosti : null, Senzor.Jedinica);

        return novo;
    }

    /// <summary>
    /// Tells the window that this row changed. A changed text costs a text layout, so this is done when the
    /// table is on screen (every two seconds), not on every reading. The strip's own numbers call it every tick.
    /// </summary>
    public void Obavesti()
    {
        OnPropertyChanged(nameof(Broj));
        OnPropertyChanged(nameof(ImaloVrednost));
        OnPropertyChanged(nameof(Vrednost));
        OnPropertyChanged(nameof(Najmanje));
        OnPropertyChanged(nameof(Najvise));
        OnPropertyChanged(nameof(Prosek));
        OnPropertyChanged(nameof(BrojPrikaz));
        OnPropertyChanged(nameof(JedinicaPrikaz));
        OnPropertyChanged(nameof(Udeo));
        OnPropertyChanged(nameof(ImaUdeo));
        OnPropertyChanged(nameof(Nivo));
        Tik++;
    }

    /// <summary>Tells the chart to redraw without touching the table: one int, not eleven strings.</summary>
    public void Tikni() => Tik++;

    public void Osvezi(double? vrednost, double? najmanje, double? najvise)
    {
        Postavi(vrednost, najmanje, najvise);
        Obavesti();
    }
}

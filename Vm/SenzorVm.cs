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

    /// <summary>The heading the row sits under in the list: "Temperature", "Takt", "Ventilatori"…</summary>
    public string Vrsta => Senzor.Jedinica switch
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
    public double? Udeo => Broj is { } broj && Senzor.Jedinica is Jedinica.Procenat or Jedinica.Celzijus
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
    private (double Toplo, double Vruce)? Pragovi()
    {
        if (Senzor.Jedinica != Jedinica.Celzijus) return null;
        if (Senzor.Grupa == Grupa.Diskovi) return (55, 65);
        if (Senzor.Grupa == Grupa.Grafika && (Ime.Contains("Hotspot") || Ime.Contains("VRAM"))) return (95, 105);
        return (80, 90);
    }

    public void Osvezi(double? vrednost, double? najmanje, double? najvise)
    {
        Broj = vrednost;
        if (vrednost is { } v)
        {
            ImaloVrednost = true;
            _zbir += v;
            _brojVrednosti++;
        }

        Vrednost = Format.Vrednost(vrednost, Senzor.Jedinica);
        Najmanje = Format.Vrednost(najmanje, Senzor.Jedinica);
        Najvise = Format.Vrednost(najvise, Senzor.Jedinica);
        Prosek = Format.Vrednost(_brojVrednosti > 0 ? _zbir / _brojVrednosti : null, Senzor.Jedinica);

        OnPropertyChanged(nameof(BrojPrikaz));
        OnPropertyChanged(nameof(JedinicaPrikaz));
        OnPropertyChanged(nameof(Udeo));
        OnPropertyChanged(nameof(ImaUdeo));
        OnPropertyChanged(nameof(Nivo));
        Tik++;
    }
}

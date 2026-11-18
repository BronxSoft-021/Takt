using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Merenje;

namespace Takt.Vm;

/// <summary>
/// One meter of a channel strip: one sensor drawn as a column of segments, with a VU-style peak that holds for
/// two seconds and then falls. The scale is a display choice (0-100 %, 20-100 °C…), never a value.
/// </summary>
public sealed class MeracVm : ObservableObject
{
    /// <summary>How long the peak stays put before it starts to fall, and how fast it falls per second.</summary>
    public static readonly TimeSpan Zadrzavanje = TimeSpan.FromSeconds(2);
    public const double PadPoSekundi = 1.0 / 20;

    private double _udeo;
    private double _vrh;
    private DateTime _vrhOd = DateTime.MinValue;
    private DateTime _poslednje = DateTime.MinValue;
    private double _skalaDo;

    public MeracVm(SenzorVm senzor, string oznaka)
    {
        Senzor = senzor;
        Oznaka = oznaka;
        (SkalaOd, _skalaDo) = senzor.Senzor.Jedinica switch
        {
            Jedinica.Celzijus => (20.0, 100.0),
            Jedinica.Obrtaj => (0.0, 3000.0),
            _ => (0.0, 100.0),
        };
    }

    public SenzorVm Senzor { get; }

    /// <summary>The small print under the meter: "%", "°C", "akt", "com", "1"…</summary>
    public string Oznaka { get; }

    public double SkalaOd { get; }
    public double SkalaDo => _skalaDo;

    /// <summary>How full, 0 to 1. Zero when the sensor gives nothing - the readout under it says "—".</summary>
    public double Udeo
    {
        get => _udeo;
        private set => SetProperty(ref _udeo, value);
    }

    public double Vrh
    {
        get => _vrh;
        private set => SetProperty(ref _vrh, value);
    }

    /// <summary>Where the meter turns amber and red, as a share of the scale; null when the sensor has no limits.</summary>
    public double? Toplo { get; private set; }
    public double? Vruce { get; private set; }

    public bool ImaVrednost => Senzor.Broj is not null;

    private double UUdeo(double vrednost) => Math.Clamp((vrednost - SkalaOd) / (SkalaDo - SkalaOd), 0, 1);

    /// <summary>After the sensor took its reading. <paramref name="sada"/> is passed in so a test can move time.</summary>
    public void Osvezi(DateTime sada)
    {
        // Fans have no natural top: the scale grows to fit the fastest seen, in steps of 500.
        if (Senzor.Senzor.Jedinica == Jedinica.Obrtaj && Senzor.Broj is { } obrtaji && obrtaji > _skalaDo)
        {
            _skalaDo = Math.Ceiling(obrtaji * 1.1 / 500) * 500;
            OnPropertyChanged(nameof(SkalaDo));
        }

        if (Senzor.Pragovi() is { } prag)
        {
            // A limit above the scale stretches the scale, so the hot mark is always on the meter.
            if (prag.Vruce + 5 > _skalaDo) { _skalaDo = prag.Vruce + 5; OnPropertyChanged(nameof(SkalaDo)); }
            Toplo = UUdeo(prag.Toplo);
            Vruce = UUdeo(prag.Vruce);
        }

        double udeo = Senzor.Broj is { } broj ? UUdeo(broj) : 0;
        double proteklo = _poslednje == DateTime.MinValue ? 0 : Math.Max(0, (sada - _poslednje).TotalSeconds);
        _poslednje = sada;

        if (udeo >= _vrh)
        {
            Vrh = udeo;
            _vrhOd = sada;
        }
        else if (sada - _vrhOd >= Zadrzavanje)
        {
            Vrh = Math.Max(udeo, _vrh - PadPoSekundi * proteklo);
        }

        Udeo = udeo;
        OnPropertyChanged(nameof(ImaVrednost));
    }
}

/// <summary>
/// One strip on the console: a part of the computer (processor, a graphics card, memory, a disk, the fans) with
/// one or two meters, a readout, S (solo) and M (mute). Its sensors are an <see cref="EkranVm"/> that the solo
/// panel shows as a chart and a table.
/// </summary>
public sealed class KanalVm : ObservableObject
{
    private bool _solo;
    private bool _utisan;

    public KanalVm(string kljuc, string naslov, string uredjaj, EkranVm detalj, MeracVm? levi, MeracVm? desni)
    {
        Kljuc = kljuc;
        Naslov = naslov;
        Uredjaj = uredjaj;
        Detalj = detalj;
        Levi = levi ?? desni;
        Desni = levi is null ? null : desni;

        Jezgra = detalj.Senzori
            .Where(s => s.Senzor.Jedinica == Jedinica.Procenat && s.Ime.StartsWith("Opterećenje jezgra ", StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>Stable name for remembering a muted channel: group and device.</summary>
    public string Kljuc { get; }

    /// <summary>"CPU", "GPU", "RAM", "Disk 1", "Ventilatori".</summary>
    public string Naslov { get; }

    /// <summary>The full device name, "AMD Ryzen 5 5600"; the strip shows <see cref="Kratko"/>.</summary>
    public string Uredjaj { get; }

    public string Kratko => Skrati(Uredjaj);

    public EkranVm Detalj { get; }

    public MeracVm? Levi { get; }
    public MeracVm? Desni { get; }

    public bool DvaMeraca => Desni is not null;

    /// <summary>A place holder: the part exists but gives no sensor without the driver.</summary>
    public bool Prazan => Levi is null;

    /// <summary>Load of every thread, for the row of small bars in the processor's solo panel.</summary>
    public IReadOnlyList<SenzorVm> Jezgra { get; }

    public bool ImaJezgra => Jezgra.Count > 0;

    public string JezgraNaslov => Jezgra.Count == 1 ? "Jezgro" : $"Jezgra ({Jezgra.Count} niti)";

    public bool Solo
    {
        get => _solo;
        set => SetProperty(ref _solo, value);
    }

    public bool Utisan
    {
        get => _utisan;
        set => SetProperty(ref _utisan, value);
    }

    public void Osvezi(DateTime sada)
    {
        Levi?.Osvezi(sada);
        Desni?.Osvezi(sada);
    }

    /// <summary>"AMD Radeon RX 6600" → "RX 6600": the brand is in the tool tip, the strip is narrow.</summary>
    public static string Skrati(string uredjaj)
    {
        string ime = uredjaj;
        foreach (var deo in new[] { "AMD ", "Intel(R) ", "Intel ", "NVIDIA ", "GeForce ", "Radeon ", "Core(TM) ", "(TM)", "(R)" })
            ime = ime.Replace(deo, "", StringComparison.OrdinalIgnoreCase);
        return ime.Trim();
    }

    /// <summary>
    /// Builds the console from the sensor screens: one channel per processor, graphics card and disk, one for
    /// memory and one for the fans. A part with no sensors still gets its strip (pale), so the console always
    /// shows where disks and fans would be once full monitoring is on.
    /// </summary>
    public static List<KanalVm> Napravi(IReadOnlyList<SenzorVm> sviSenzori)
    {
        var kanali = new List<KanalVm>();

        void PoUredjaju(Grupa grupa, Func<int, int, string> naslov, string[] levo, string[] desno, string oznakaL, string oznakaD)
        {
            var uredjaji = sviSenzori.Where(s => s.Senzor.Grupa == grupa).Select(s => s.Uredjaj).Distinct().ToList();
            for (int i = 0; i < uredjaji.Count; i++)
            {
                var senzori = sviSenzori.Where(s => s.Senzor.Grupa == grupa && s.Uredjaj == uredjaji[i]).ToList();
                kanali.Add(new KanalVm($"{grupa}|{uredjaji[i]}", naslov(i, uredjaji.Count), uredjaji[i],
                    new EkranVm(grupa, senzori), Merac(senzori, levo, oznakaL), Merac(senzori, desno, oznakaD)));
            }

            if (uredjaji.Count == 0)
                kanali.Add(new KanalVm($"{grupa}|", naslov(0, 1), GrupaOpis.Naslov(grupa), new EkranVm(grupa, []), null, null));
        }

        PoUredjaju(Grupa.Procesor, (i, n) => n == 1 ? "CPU" : $"CPU {i + 1}",
            ["Opterećenje procesora"], ["Temperatura procesora", "Temperatura paketa", "Temperatura jezgra 1"], "%", "°C");
        PoUredjaju(Grupa.Grafika, (i, n) => n == 1 ? "GPU" : $"GPU {i + 1}",
            ["Opterećenje"], ["Temperatura"], "%", "°C");
        PoUredjaju(Grupa.Memorija, (_, _) => "RAM",
            ["Zauzeće"], ["Zauzeće commit-a"], "RAM", "com");
        PoUredjaju(Grupa.Diskovi, (i, _) => $"Disk {i + 1}",
            ["Aktivnost", "Zauzeće"], ["Temperatura"], "akt", "°C");

        // Fans live on the board and on the cards; the strip shows the first two the board gives.
        var ventilatori = sviSenzori.Where(s => s.Senzor.Grupa == Grupa.Hladjenje).ToList();
        var obrtaji = ventilatori.Where(s => s.Senzor.Jedinica == Jedinica.Obrtaj).ToList();
        kanali.Add(new KanalVm($"{Grupa.Hladjenje}|", "Vent.",
            ventilatori.Select(s => s.Uredjaj).FirstOrDefault() ?? "Hlađenje",
            new EkranVm(Grupa.Hladjenje, ventilatori),
            obrtaji.Count > 0 ? new MeracVm(obrtaji[0], "1") : null,
            obrtaji.Count > 1 ? new MeracVm(obrtaji[1], "2") : null));

        return kanali;
    }

    /// <summary>The first of the wanted names that this device has, or any sensor with a matching name start.</summary>
    private static MeracVm? Merac(List<SenzorVm> senzori, string[] imena, string oznaka)
    {
        foreach (var ime in imena)
            if (senzori.FirstOrDefault(s => s.Ime == ime) is { } nadjen)
                return new MeracVm(nadjen, oznaka);
        return null;
    }
}

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

    /// <summary>Tool tip of the meter: which device and which sensor it shows.</summary>
    public string Opis => $"{Senzor.Uredjaj} · {Senzor.Ime}";

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
/// One strip on the console: a part of the computer with its meters, a readout, S (solo) and M (mute). Its
/// sensors are an <see cref="EkranVm"/> that the solo panel shows as a chart and a table.
/// The disks are one strip too - a group, like a bus on a mixing desk: one narrow meter per disk, and the solo
/// panel picks the disk (<see cref="Podkanali"/>). Six disks as six strips pushed everything else off the console.
/// </summary>
public sealed class KanalVm : ObservableObject
{
    private readonly string _uredjaj;
    private bool _solo;
    private bool _utisan;
    private KanalVm? _izabraniPodkanal;
    private EkranVm _detalj;

    public KanalVm(string kljuc, string naslov, string uredjaj, EkranVm detalj, IEnumerable<MeracVm?> meraci,
        IReadOnlyList<KanalVm>? podkanali = null)
    {
        Kljuc = kljuc;
        Naslov = naslov;
        _uredjaj = uredjaj;
        _detalj = detalj;
        Meraci = meraci.OfType<MeracVm>().ToList();
        Podkanali = podkanali ?? [];

        _izabraniPodkanal = Podkanali.FirstOrDefault(p => !p.Prazan) ?? Podkanali.FirstOrDefault();
        if (_izabraniPodkanal is not null) _detalj = _izabraniPodkanal.Detalj;

        Jezgra = detalj.Senzori
            .Where(s => s.Senzor.Jedinica == Jedinica.Procenat && s.Ime.StartsWith("Opterećenje jezgra ", StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>Stable name for remembering a muted channel: group and device.</summary>
    public string Kljuc { get; }

    /// <summary>"CPU", "GPU", "RAM", "Diskovi", "Vent."; a disk inside the group is "Disk 1", "Disk 2"…</summary>
    public string Naslov { get; }

    /// <summary>The full device name; for the disk group, the disk now in the solo panel.</summary>
    public string Uredjaj => _izabraniPodkanal?.Uredjaj ?? _uredjaj;

    /// <summary>The small line under the name: "Ryzen 5 5600", or "6 diskova" for the group.</summary>
    public string Kratko => JeGrupa ? BrojDiskova(Podkanali.Count) : Skrati(_uredjaj);

    /// <summary>The sensors in the solo panel; for the disk group, those of the chosen disk.</summary>
    public EkranVm Detalj
    {
        get => _detalj;
        private set => SetProperty(ref _detalj, value);
    }

    public IReadOnlyList<MeracVm> Meraci { get; }

    public MeracVm? Levi => Meraci.Count > 0 ? Meraci[0] : null;
    public MeracVm? Desni => Meraci.Count > 1 ? Meraci[1] : null;

    public bool DvaMeraca => Meraci.Count == 2;

    /// <summary>A place holder: the part exists but gives no sensor without the driver.</summary>
    public bool Prazan => Meraci.Count == 0;

    /// <summary>Width of one meter's column and of the meter in it: wide alone, narrow in a group of many.</summary>
    public double SirinaKolone => Meraci.Count switch { <= 1 => 30, 2 => 24, _ => 16 };
    public double SirinaMeraca => Meraci.Count switch { <= 1 => 22, 2 => 14, _ => 10 };

    public IReadOnlyList<KanalVm> Podkanali { get; }

    public bool JeGrupa => Podkanali.Count > 0;

    /// <summary>The selector above the chart only makes sense with more than one disk.</summary>
    public bool ImaIzbor => Podkanali.Count > 1;

    public KanalVm? IzabraniPodkanal
    {
        get => _izabraniPodkanal;
        set
        {
            if (value is null || !SetProperty(ref _izabraniPodkanal, value)) return;
            Detalj = value.Detalj;
            OnPropertyChanged(nameof(Uredjaj));
        }
    }

    /// <summary>
    /// The big number under the meters: the left meter's sensor, or in the disk group the hottest disk (the one
    /// worth watching). Null on an empty strip.
    /// </summary>
    public SenzorVm? Glavni => JeGrupa
        ? Meraci.Select(m => m.Senzor).Where(s => s.Broj is not null).MaxBy(s => s.Broj) ?? Levi?.Senzor
        : Levi?.Senzor;

    /// <summary>The small line under it: the right meter's value, "najtopliji" for the group, and what an empty place waits for.</summary>
    public string DrugiTekst => Prazan ? "traži nadzor"
        : JeGrupa ? (Meraci.Count > 1 ? "najtopliji" : "")
        : Desni?.Senzor.Vrednost ?? "";

    /// <summary>Tool tip of the strip: the device, or what is missing on an empty place.</summary>
    public string OpisKanala => Prazan
        ? $"{Naslov}: ovaj deo traži potpuni nadzor (administrator i drajver). Klikni za objašnjenje."
        : Uredjaj;

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

    /// <summary>
    /// One reading: the sensors, the meters, then the readout. <paramref name="obavestiTabelu"/> false skips the
    /// notifications for the long table (a text layout per cell), but the strip's own numbers are always told:
    /// they are on screen all the time and they are what the eye follows.
    /// </summary>
    public void Osvezi(Snimak snimak, double?[] najmanje, double?[] najvise, DateTime sada, bool obavestiTabelu = true)
    {
        if (JeGrupa)
            foreach (var pod in Podkanali)
                pod.Osvezi(snimak, najmanje, najvise, sada,
                    obavestiTabelu && ReferenceEquals(pod, _izabraniPodkanal));
        else
            Detalj.Osvezi(snimak, najmanje, najvise, obavestiTabelu);

        foreach (var merac in Meraci)
        {
            merac.Osvezi(sada);
            merac.Senzor.Obavesti();
        }

        // The big number's DataContext is a sensor, not a text: telling the window it changed every second makes
        // WPF tear down and rebuild those bindings each time. Say it only when another sensor takes the number
        // (the hottest disk can change); the texts themselves are refreshed through Obavesti above.
        var glavni = Glavni;
        if (!ReferenceEquals(glavni, _glavniPre))
        {
            _glavniPre = glavni;
            OnPropertyChanged(nameof(Glavni));
        }

        OnPropertyChanged(nameof(DrugiTekst));
    }

    private SenzorVm? _glavniPre;

    /// <summary>"AMD Radeon RX 6600" → "RX 6600": the brand is in the tool tip, the strip is narrow.</summary>
    public static string Skrati(string uredjaj)
    {
        string ime = uredjaj;
        foreach (var deo in new[] { "AMD ", "Intel(R) ", "Intel ", "NVIDIA ", "GeForce ", "Radeon ", "Core(TM) ", "(TM)", "(R)" })
            ime = ime.Replace(deo, "", StringComparison.OrdinalIgnoreCase);
        return ime.Trim();
    }

    public static string BrojDiskova(int n) => n switch
    {
        0 => "nema diskova",
        _ when n % 10 == 1 && n % 100 != 11 => $"{n} disk",
        _ when n % 10 is >= 2 and <= 4 && n % 100 is < 12 or > 14 => $"{n} diska",
        _ => $"{n} diskova",
    };

    /// <summary>
    /// Builds the console from the sensors: one channel per processor and graphics card, one for memory, one group
    /// for all the disks and one for the fans. A part with no sensors still gets its strip (pale), so the console
    /// always shows where disks and fans would be once full monitoring is on.
    /// </summary>
    public static List<KanalVm> Napravi(IReadOnlyList<SenzorVm> sviSenzori)
    {
        var kanali = new List<KanalVm>();

        List<KanalVm> PoUredjaju(Grupa grupa, Func<int, int, string> naslov, string[] levo, string[] desno, string oznakaL, string oznakaD)
        {
            var rezultat = new List<KanalVm>();
            var uredjaji = sviSenzori.Where(s => s.Senzor.Grupa == grupa).Select(s => s.Uredjaj).Distinct().ToList();
            for (int i = 0; i < uredjaji.Count; i++)
            {
                var senzori = sviSenzori.Where(s => s.Senzor.Grupa == grupa && s.Uredjaj == uredjaji[i]).ToList();
                rezultat.Add(new KanalVm($"{grupa}|{uredjaji[i]}", naslov(i, uredjaji.Count), uredjaji[i],
                    new EkranVm(grupa, senzori), [Merac(senzori, levo, oznakaL), Merac(senzori, desno, oznakaD)]));
            }
            return rezultat;
        }

        void Dodaj(Grupa grupa, List<KanalVm> nadjeni, string naslov)
        {
            if (nadjeni.Count > 0) kanali.AddRange(nadjeni);
            else kanali.Add(new KanalVm($"{grupa}|", naslov, GrupaOpis.Naslov(grupa), new EkranVm(grupa, []), []));
        }

        Dodaj(Grupa.Procesor, PoUredjaju(Grupa.Procesor, (i, n) => n == 1 ? "CPU" : $"CPU {i + 1}",
            ["Opterećenje procesora"], ["Temperatura procesora", "Temperatura paketa", "Temperatura jezgra 1"], "%", "°C"), "CPU");
        Dodaj(Grupa.Grafika, PoUredjaju(Grupa.Grafika, (i, n) => n == 1 ? "GPU" : $"GPU {i + 1}",
            ["Opterećenje"], ["Temperatura"], "%", "°C"), "GPU");
        Dodaj(Grupa.Memorija, PoUredjaju(Grupa.Memorija, (_, _) => "RAM",
            ["Zauzeće"], ["Zauzeće commit-a"], "RAM", "com"), "RAM");

        // All disks in one group: each disk keeps its own channel inside (for the solo panel), and the group shows
        // one meter per disk - its temperature, or its activity when it has no thermometer.
        var diskovi = PoUredjaju(Grupa.Diskovi, (i, _) => $"Disk {i + 1}", ["Aktivnost", "Zauzeće"], ["Temperatura"], "akt", "°C");
        var meraciDiskova = diskovi.Select((d, i) =>
        {
            var senzor = d.Detalj.Senzori.FirstOrDefault(s => s.Ime == "Temperatura") ?? d.Levi?.Senzor;
            return senzor is null ? null : new MeracVm(senzor, (i + 1).ToString());
        }).ToList();
        kanali.Add(new KanalVm($"{Grupa.Diskovi}|", "Diskovi", GrupaOpis.Naslov(Grupa.Diskovi),
            diskovi.FirstOrDefault()?.Detalj ?? new EkranVm(Grupa.Diskovi, []), meraciDiskova, diskovi));

        // Fans live on the board and on the cards; the strip shows the first two the board gives.
        var ventilatori = sviSenzori.Where(s => s.Senzor.Grupa == Grupa.Hladjenje).ToList();
        var obrtaji = ventilatori.Where(s => s.Senzor.Jedinica == Jedinica.Obrtaj).ToList();
        kanali.Add(new KanalVm($"{Grupa.Hladjenje}|", "Vent.",
            ventilatori.Select(s => s.Uredjaj).FirstOrDefault() ?? "Hlađenje",
            new EkranVm(Grupa.Hladjenje, ventilatori),
            [obrtaji.Count > 0 ? new MeracVm(obrtaji[0], "1") : null, obrtaji.Count > 1 ? new MeracVm(obrtaji[1], "2") : null]));

        return kanali;
    }

    /// <summary>The first of the wanted names that this device has.</summary>
    private static MeracVm? Merac(List<SenzorVm> senzori, string[] imena, string oznaka)
    {
        foreach (var ime in imena)
            if (senzori.FirstOrDefault(s => s.Ime == ime) is { } nadjen)
                return new MeracVm(nadjen, oznaka);
        return null;
    }
}

using LibreHardwareMonitor.Hardware;
using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// Real readings through LibreHardwareMonitor (MPL-2.0). The library brings its own signed PawnIO driver;
/// without administrator rights and the driver it simply returns fewer sensors - the program must not fail.
/// The library's names are technical English ("GPU Core #2"); everything here is renamed to what a person
/// reads on screen, and a few values are worked out from others (VRAM usage from used and total).
/// </summary>
public sealed class LhmIzvor : IIzvorSenzora
{
    private readonly Computer? _racunar;
    private readonly List<Senzor> _senzori = [];
    private readonly List<ISensor?> _izvori = [];
    private readonly List<double> _faktori = [];
    private readonly List<(int Mesto, int Gore, int Dole)> _izvedeni = [];
    private string? _poruka;

    /// <summary>One sensor on its way in: the library's own, or one worked out from two others.</summary>
    /// <c>Faktor</c> turns the library's unit into ours (GB → MB, B/s → MB/s).
    private sealed record Stavka(Senzor Senzor, ISensor? Izvor, SenzorKljuc? Gore = null, SenzorKljuc? Dole = null,
        double Faktor = 1);

    public LhmIzvor()
    {
        try
        {
            var racunar = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                // Fan control is not implemented yet, and enabling the controllers starts LibreHardwareMonitor's
                // HID/serial watcher threads, which poll in the background: measured ~4 % of one core while idle.
                IsControllerEnabled = false,
            };
            racunar.Open();
            _racunar = racunar;

            // The board's fans and temperatures (Super I/O) appear in the library's list only after they have been
            // read once: a fan is "switched on" by its first reading. Listing right after Open() left the fan
            // channel empty on a board the library supports (ASUS TUF B550, 18. 11. 2026). Two readings, a moment
            // apart, before the list is made; a fan that is not connected reads 0 and rightly stays out.
            foreach (var hardver in racunar.Hardware) Osvezi(hardver);
            Thread.Sleep(300);
            foreach (var hardver in racunar.Hardware) Osvezi(hardver);

            var stavke = new List<Stavka>();
            foreach (var hardver in racunar.Hardware)
                Popisi(hardver, null, stavke);

            DodajVramProcenat(stavke);
            stavke.Sort(Poredak);

            var mesta = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var stavka in stavke)
            {
                mesta[stavka.Senzor.Kljuc.Tekst] = _senzori.Count;
                _senzori.Add(stavka.Senzor);
                _izvori.Add(stavka.Izvor);
                _faktori.Add(stavka.Faktor);
            }

            foreach (var stavka in stavke)
            {
                if (stavka is not { Gore: { } gore, Dole: { } dole }) continue;
                if (!mesta.TryGetValue(gore.Tekst, out int goreMesto)) continue;
                if (!mesta.TryGetValue(dole.Tekst, out int doleMesto)) continue;
                _izvedeni.Add((mesta[stavka.Senzor.Kljuc.Tekst], goreMesto, doleMesto));
            }

            if (_senzori.Count == 0)
                _poruka = PawnIo.JeTu
                    ? "Nijedan senzor nije dostupan. Probaj da pokreneš program kao administrator."
                    : $"Nijedan senzor nije dostupan. Nedostaje PawnIO drajver ({PawnIo.Adresa}).";
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("LibreHardwareMonitor", ex);
            _poruka = "Senzori nisu dostupni: " + ex.Message;
        }
    }

    public string Naziv => "Pravi senzori";

    public bool Pravi => true;

    public IReadOnlyList<Senzor> Senzori => _senzori;

    public string? Poruka => _poruka;

    public Snimak Uzmi()
    {
        var vrednosti = new double?[_senzori.Count];
        if (_racunar is null) return new Snimak(DateTime.Now, vrednosti);

        try
        {
            foreach (var hardver in _racunar.Hardware)
                Osvezi(hardver);

            for (int i = 0; i < _izvori.Count; i++)
            {
                if (_izvori[i] is not { } izvor) continue; // worked out below

                try
                {
                    vrednosti[i] = Ocitaj(izvor, _senzori[i].Jedinica) * _faktori[i];
                }
                catch (Exception ex)
                {
                    Dnevnik.Greska("Senzor " + izvor.Name, ex);
                }
            }

            foreach (var (mesto, gore, dole) in _izvedeni)
                vrednosti[mesto] = Procenat(vrednosti[gore], vrednosti[dole]);

            BezDrajveraNemaNapona(vrednosti);
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Čitanje senzora", ex);
            _poruka = "Čitanje senzora nije uspelo: " + ex.Message;
        }

        return new Snimak(DateTime.Now, vrednosti);
    }

    /// <summary>
    /// A value with one correction: a working processor is never at 0 °C, 0 MHz or 0 W. Without the driver
    /// the library fills in a zero, and a zero shown as a measurement would be a lie.
    /// </summary>
    private static double? Ocitaj(ISensor izvor, Jedinica jedinica)
    {
        double? ocitano = izvor.Value;
        bool nulaJeNema = ocitano == 0 && jedinica is Jedinica.Celzijus or Jedinica.Megaherc or Jedinica.Vat;
        return nulaJeNema ? null : ocitano;
    }

    /// <summary>
    /// Without the driver the library still reports the processor's voltages, but they are a constant it
    /// never read (1,550 V on every core). The driver is working when the processor gives at least one
    /// temperature or clock; until then its volts and watts are "—", not a made-up number.
    /// </summary>
    private void BezDrajveraNemaNapona(double?[] vrednosti)
    {
        bool drajver = false;
        for (int i = 0; i < _senzori.Count && !drajver; i++)
            drajver = _senzori[i] is { Grupa: Grupa.Procesor, Jedinica: Jedinica.Celzijus or Jedinica.Megaherc }
                      && vrednosti[i] is not null;

        if (drajver) return;

        for (int i = 0; i < _senzori.Count; i++)
            if (_senzori[i] is { Grupa: Grupa.Procesor, Jedinica: Jedinica.Volt or Jedinica.Vat or Jedinica.Amper })
                vrednosti[i] = null;
    }

    /// <summary>Share of the whole, in percent; null when a part is missing or the whole is zero.</summary>
    public static double? Procenat(double? deo, double? celina) =>
        deo is { } d && celina is { } c && c > 0 ? Math.Round(d / c * 100, 0) : null;

    private static void Osvezi(IHardware hardver)
    {
        hardver.Update();
        foreach (var pod in hardver.SubHardware)
            Osvezi(pod);
    }

    private static void Popisi(IHardware hardver, string? roditelj, List<Stavka> stavke)
    {
        var grupa = GrupaZa(hardver.HardwareType);
        // Some drives report their model with a trailing space ("Samsung SSD 870 EVO 1TB ").
        var imeHardvera = hardver.Name.Trim();
        var uredjaj = roditelj is null ? imeHardvera : $"{roditelj} › {imeHardvera}";

        if (grupa is { } g)
        {
            foreach (var senzor in hardver.Sensors)
            {
                var (jedinica, faktor) = JedinicaZa(senzor.SensorType);
                if (jedinica == Jedinica.Nepoznato) continue;
                if (Zanemari(senzor.Name, jedinica)) continue;

                var osnovno = Prevedi(g, senzor.Name, jedinica);
                var ime = Jedinstveno(osnovno, uredjaj, g, jedinica, stavke);
                // The library's own path is the stable key: the name beside it may be translated or renamed.
                stavke.Add(new Stavka(new Senzor(new SenzorKljuc(g, uredjaj, ime, senzor.Identifier.ToString()), jedinica),
                    senzor, Faktor: faktor));
            }
        }

        foreach (var pod in hardver.SubHardware)
            Popisi(pod, uredjaj, stavke);
    }

    /// <summary>VRAM usage in percent, worked out from the library's used and total, so the screen can show it.</summary>
    private static void DodajVramProcenat(List<Stavka> stavke)
    {
        var uredjaji = stavke
            .Where(s => s.Senzor.Grupa == Grupa.Grafika && s.Senzor.Ime == "VRAM zauzeto")
            .Select(s => s.Senzor.Uredjaj)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var uredjaj in uredjaji)
        {
            var gore = stavke.FirstOrDefault(s =>
                s.Senzor.Grupa == Grupa.Grafika && s.Senzor.Uredjaj == uredjaj && s.Senzor.Ime == "VRAM zauzeto");
            var dole = stavke.FirstOrDefault(s =>
                s.Senzor.Grupa == Grupa.Grafika && s.Senzor.Uredjaj == uredjaj && s.Senzor.Ime == "VRAM ukupno");
            if (gore is null || dole is null) continue;

            var kljuc = new SenzorKljuc(Grupa.Grafika, uredjaj, "Zauzeće VRAM",
                IzvedenIdent(gore.Senzor.Kljuc.Ident, "zauzece-vram"));
            if (stavke.Any(s => s.Senzor.Kljuc == kljuc)) continue;

            stavke.Add(new Stavka(new Senzor(kljuc, Jedinica.Procenat), null, gore.Senzor.Kljuc, dole.Senzor.Kljuc));
        }
    }

    /// <summary>
    /// A stable key for a sensor the library does not have: its neighbour's path with another last part
    /// (<c>/gpu-amd/0/smalldata/1</c> → <c>/gpu-amd/0/zauzece-vram</c>), so it cannot collide with a reading.
    /// </summary>
    private static string? IzvedenIdent(string? ident, string ime)
    {
        if (ident is null) return null;
        int kosa = ident.LastIndexOf('/');
        return kosa <= 0 ? null : ident[..kosa] + "/" + ime;
    }

    /// <summary>Temperature, then load, clock, fan, power, voltage, memory: the order a person looks for.</summary>
    private static int Poredak(Stavka a, Stavka b)
    {
        int grupa = a.Senzor.Grupa.CompareTo(b.Senzor.Grupa);
        if (grupa != 0) return grupa;

        int jedinica = Rang(a.Senzor.Jedinica).CompareTo(Rang(b.Senzor.Jedinica));
        if (jedinica != 0) return jedinica;

        int uredjaj = string.Compare(a.Senzor.Uredjaj, b.Senzor.Uredjaj, StringComparison.Ordinal);
        if (uredjaj != 0) return uredjaj;

        // Within one unit, the headline value first: "Temperatura", then "Hotspot"; "Opterećenje", then VRAM.
        int ime = RangImena(a.Senzor.Ime).CompareTo(RangImena(b.Senzor.Ime));
        return ime != 0 ? ime : Prirodno.Poredak.Compare(a.Senzor.Ime, b.Senzor.Ime);
    }

    private static int RangImena(string ime)
    {
        if (ime is "Temperatura" or "Temperatura procesora" or "Temperatura paketa") return 0;
        if (ime.StartsWith("Temperatura jezgra", StringComparison.Ordinal)) return 1;
        if (ime.StartsWith("Temperatura CCD", StringComparison.Ordinal)) return 1;
        if (ime.StartsWith("Temperatura senzora", StringComparison.Ordinal)) return 1;
        if (ime == "Hotspot") return 2;
        if (Merenje.Granice.JeGranica(ime)) return 5; // limits after every reading of the same unit

        if (ime is "Opterećenje" or "Opterećenje procesora") return 0;
        if (ime.StartsWith("Opterećenje jezgra", StringComparison.Ordinal)) return 1;
        if (ime == "Zauzeće VRAM") return 2;

        if (ime == "Takt jezgra") return 0;
        if (ime == "Takt memorije" || ime.StartsWith("Takt jezgra", StringComparison.Ordinal)) return 1;
        if (ime.StartsWith("Prosečan", StringComparison.Ordinal)) return 2;

        if (ime == "Ventilator" || ime.StartsWith("Ventilator", StringComparison.Ordinal)) return 0;
        if (ime == "Potrošnja" || ime.StartsWith("Potrošnja jezgra", StringComparison.Ordinal)) return 0;
        if (ime == "Napon jezgra" || ime.StartsWith("Napon", StringComparison.Ordinal)) return 0;

        if (ime == "Zauzeće VRAM" || ime == "VRAM zauzeto") return 0;
        if (ime == "VRAM ukupno") return 1;

        return 3;
    }

    /// <summary>Two sensors with one name on one device are told apart by the unit, not by "#2".</summary>
    private static string Jedinstveno(string osnovno, string uredjaj, Grupa grupa, Jedinica jedinica,
        List<Stavka> stavke)
    {
        bool Postoji(string kandidat) =>
            stavke.Any(s => s.Senzor.Grupa == grupa && s.Senzor.Uredjaj == uredjaj && s.Senzor.Ime == kandidat);

        if (!Postoji(osnovno)) return osnovno;

        var oznaka = Format.Oznaka(jedinica);
        var ime = oznaka.Length == 0 ? osnovno : $"{osnovno} ({oznaka})";

        int broj = 2;
        while (Postoji(ime))
            ime = $"{osnovno} ({oznaka} {broj++})";

        return ime;
    }

    /// <summary>
    /// The library speaks in datasheet English. This is the same thing in the words the program uses, so the
    /// screen reads "Takt jezgra" and not "GPU Core #2".
    /// </summary>
    public static string Prevedi(Grupa grupa, string ime, Jedinica jedinica)
    {
        string? broj = null;
        int taraba = ime.IndexOf('#');
        if (taraba >= 0)
        {
            int kraj = ime.IndexOfAny([' ', '(', ')'], taraba);
            broj = (kraj < 0 ? ime[(taraba + 1)..] : ime[(taraba + 1)..kraj]).Trim();
            if (broj.Length == 0) broj = null;
        }

        return grupa switch
        {
            Grupa.Grafika => Grafika(ime, jedinica),
            Grupa.Procesor => Procesor(ime, jedinica, broj),
            Grupa.Diskovi => Disk(ime, broj),
            Grupa.Hladjenje => Hladjenje(ime, broj),
            _ => ime,
        };
    }

    /// <summary>
    /// Disks. "Warning/Critical Temperature" are not readings but the limits the drive itself reports (the
    /// temperature where it starts to slow down or shut off); they are named as limits so nobody reads them
    /// as heat, and <see cref="Vm.SenzorVm"/> uses them as that drive's warm and hot marks.
    /// </summary>
    private static string Disk(string ime, string? broj) => ime switch
    {
        "Temperature" or "Composite Temperature" or "Drive Temperature" => "Temperatura",
        _ when ime.StartsWith("Temperature #", StringComparison.Ordinal) => $"Temperatura senzora {broj}",
        "Warning Temperature" => Merenje.Granice.Upozorenje,
        "Critical Temperature" => Merenje.Granice.Kriticna,
        "Available Spare" => "Rezervni blokovi",
        "Available Spare Threshold" => Merenje.Granice.Rezerva,
        "Used Space" => "Zauzeće",
        "Free Space" or "Available Free Space" => "Slobodan prostor",
        "Total Space" => "Ukupan prostor",
        "Remaining Life" or "Life" => "Životni vek",
        "Percentage Used" => "Istrošenost",
        "Data Read" => "Ukupno pročitano",
        "Data Written" or "Total Bytes Written" => "Ukupno upisano",
        "Read Activity" => "Aktivnost čitanja",
        "Write Activity" => "Aktivnost upisa",
        "Total Activity" => "Aktivnost",
        "Read Rate" => "Brzina čitanja",
        "Write Rate" => "Brzina upisa",
        "Power On Hours" => "Radni sati",
        "Power On Count" => "Broj uključivanja",
        _ => ime,
    };

    private static string Grafika(string ime, Jedinica jedinica) => ime switch
    {
        "GPU Core" when jedinica == Jedinica.Celzijus => "Temperatura",
        "GPU Core" when jedinica == Jedinica.Procenat => "Opterećenje",
        "GPU Core" when jedinica == Jedinica.Megaherc => "Takt jezgra",
        "GPU Core" when jedinica == Jedinica.Volt => "Napon jezgra",
        "GPU Hot Spot" => "Hotspot",
        "GPU Memory Junction" => "Temperatura VRAM",
        "GPU Memory" when jedinica == Jedinica.Megaherc => "Takt memorije",
        "GPU Memory Used" => "VRAM zauzeto",
        "GPU Memory Total" => "VRAM ukupno",
        "GPU Fan" => "Ventilator",
        "GPU Package" when jedinica == Jedinica.Vat => "Potrošnja",
        "GPU Power" when jedinica == Jedinica.Vat => "Potrošnja",
        _ => ime,
    };

    private static string Procesor(string ime, Jedinica jedinica, string? broj)
    {
        if (ime.StartsWith("CPU Core #", StringComparison.Ordinal)) return $"Opterećenje jezgra {broj}";
        if (ime.EndsWith(" (Effective)", StringComparison.Ordinal)) return $"Takt jezgra {broj} (efektivni)";
        if (ime.EndsWith(" VID", StringComparison.Ordinal)) return $"Napon jezgra {broj} (VID)";
        if (ime.EndsWith(" (SMU)", StringComparison.Ordinal)) return $"Potrošnja jezgra {broj}";

        if (ime.StartsWith("Core #", StringComparison.Ordinal))
            return jedinica switch
            {
                Jedinica.Celzijus => $"Temperatura jezgra {broj}",
                Jedinica.Megaherc => $"Takt jezgra {broj}",
                _ => ime,
            };

        return ime switch
        {
            "CPU Total" => "Opterećenje procesora",
            "CPU Core Max" => "Najopterećenije jezgro",
            "Core (Tctl/Tdie)" or "Core (Tdie)" or "Core (Tctl)" => "Temperatura procesora",
            "Core (SVI2 TFN)" => "Napon procesora",
            "SoC (SVI2 TFN)" => "Napon SoC-a",
            "Cores (Average)" => "Prosečan takt",
            "Cores (Average Effective)" => "Prosečan efektivni takt",
            "Bus Speed" => "Takt magistrale",
            // Ryzen: one die temperature per chiplet, "CCD1 (Tdie)".
            _ when ime.StartsWith("CCD", StringComparison.Ordinal) && jedinica == Jedinica.Celzijus
                => "Temperatura " + ime.Split(' ')[0],
            "Package" when jedinica == Jedinica.Vat => "Potrošnja paketa",
            "Package" when jedinica == Jedinica.Celzijus => "Temperatura paketa",
            _ => ime,
        };
    }

    private static string Hladjenje(string ime, string? broj) => ime switch
    {
        _ when ime.StartsWith("Fan #", StringComparison.Ordinal) => $"Ventilator {broj}",
        _ when ime.StartsWith("Temperature #", StringComparison.Ordinal) => $"Temperatura {broj}",
        _ when ime.StartsWith("Voltage #", StringComparison.Ordinal) => $"Napon {broj}",
        _ when ime.StartsWith("Pump #", StringComparison.Ordinal) => $"Pumpa {broj}",
        "Fan" => "Ventilator",
        // The Super I/O's named voltages (Nuvoton NCT6798D on ASUS TUF B550, 18. 11. 2026).
        "Vcore" => "Napon procesora (ploča)",
        "CMOS Battery" => "Baterija CMOS-a",
        "+3V Standby" => "+3 V u mirovanju",
        "+3.3V" => "+3,3 V",
        "CPU Termination" => "Terminacija procesora",
        _ => ime,
    };

    /// <summary>Engine counters, "free" leftovers and the memory-controller load are noise on one card's screen.</summary>
    private static bool Zanemari(string ime, Jedinica jedinica) =>
        ime.StartsWith("D3D ", StringComparison.Ordinal) ||
        ime.EndsWith(" Free", StringComparison.Ordinal) ||
        ime.EndsWith(" Available", StringComparison.Ordinal) ||
        (ime == "GPU Memory" && jedinica == Jedinica.Procenat);

    private static int Rang(Jedinica jedinica) => jedinica switch
    {
        Jedinica.Celzijus => 0,
        Jedinica.Procenat => 1,
        Jedinica.Megaherc => 2,
        Jedinica.Obrtaj => 3,
        Jedinica.Vat => 4,
        Jedinica.Volt => 5,
        Jedinica.Amper => 6,
        Jedinica.Megabajt => 7,
        Jedinica.MegabajtUSekundi => 8,
        _ => 9,
    };

    /// <summary>Compared by name so a library update that adds a value cannot break the build.</summary>
    private static Grupa? GrupaZa(HardwareType tip) => tip.ToString() switch
    {
        "Cpu" => Grupa.Procesor,
        "GpuNvidia" or "GpuAmd" or "GpuIntel" => Grupa.Grafika,
        // Memory is read by RamIzvor: the library only offers throughput there, not what a person expects.
        "Storage" => Grupa.Diskovi,
        "Motherboard" or "SuperIO" or "EmbeddedController" or "Cooler" => Grupa.Hladjenje,
        _ => null,
    };

    /// <summary>
    /// Our unit for the library's sensor type, and the factor to get there. The library gives "Data" in GB
    /// (disk totals), "SmallData" in MB and "Throughput" in bytes per second.
    /// </summary>
    public static (Jedinica Jedinica, double Faktor) JedinicaZa(SensorType tip) => tip.ToString() switch
    {
        "Temperature" => (Jedinica.Celzijus, 1),
        "Load" or "Level" => (Jedinica.Procenat, 1),
        "Clock" => (Jedinica.Megaherc, 1),
        "Fan" => (Jedinica.Obrtaj, 1),
        "Voltage" => (Jedinica.Volt, 1),
        "Power" => (Jedinica.Vat, 1),
        "Current" => (Jedinica.Amper, 1),
        "Data" => (Jedinica.Megabajt, 1024),
        "SmallData" => (Jedinica.Megabajt, 1),
        "Throughput" => (Jedinica.MegabajtUSekundi, 1.0 / 1_048_576),
        _ => (Jedinica.Nepoznato, 1),
    };

    public void Dispose()
    {
        try
        {
            _racunar?.Close();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Zatvaranje senzora", ex);
        }
    }
}



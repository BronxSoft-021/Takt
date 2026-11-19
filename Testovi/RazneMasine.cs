using Takt.Merenje;
using Takt.Sistem;
using Takt.Vm;

namespace Takt.Testovi;

/// <summary>
/// Machines this one is not: no sensor at all, only percentages, 32 threads, twelve drives, drives without a
/// thermometer, a card without VRAM sensors, an old board without fans, 150 sensors. None of that can be tried on
/// this hardware, but every shape goes through the same view models - and that is what these tests put them
/// through, looking for a crash or a sentence that would be a lie.
/// </summary>
public static class RazneMasine
{
    private static SenzorVm S(Grupa grupa, string uredjaj, string ime, Jedinica jedinica, int indeks) =>
        new(new Senzor(new SenzorKljuc(grupa, uredjaj, ime), jedinica), indeks, new Prsten(8));

    /// <summary>One reading for every sensor: 20, 21, 22… - enough to have a number everywhere.</summary>
    private static void Napuni(KanalVm kanal, IReadOnlyList<SenzorVm> senzori)
    {
        var vrednosti = senzori.Select((_, i) => (double?)(20 + i)).ToArray();
        kanal.Osvezi(new Snimak(DateTime.Now, vrednosti), vrednosti, vrednosti, DateTime.Now);
    }

    /// <summary>A machine where the driver is missing and nothing at all comes through.</summary>
    public static Task NemaSenzora()
    {
        var kanali = KanalVm.Napravi([]);

        Proveri.Jednako(5, kanali.Count, "pet kanala i bez ijednog senzora");
        Proveri.Da(kanali.All(k => k.Prazan), "svi su prazni");
        Proveri.Da(kanali.All(k => !string.IsNullOrWhiteSpace(k.DrugiTekst)), "svaki u malom redu kaže šta čeka");
        Proveri.Da(kanali.All(k => !string.IsNullOrWhiteSpace(k.OpisKanala)), "i u tooltipu");

        foreach (var grupa in GrupaOpis.Redosled)
        {
            Proveri.Da(GrupaOpis.Prazno(grupa, drajver: false).Length > 40, $"{grupa}: prazan kanal objašnjava");
            Proveri.Da(GrupaOpis.Prazno(grupa, drajver: true, potpunNadzor: true).Length > 20,
                $"{grupa}: i uz potpuni nadzor kaže šta je");
        }
        return Task.CompletedTask;
    }

    /// <summary>Only percentages come through (no driver, laptop): no temperature, no limit, nothing to colour.</summary>
    public static Task SamoProcenti()
    {
        var senzori = new List<SenzorVm>
        {
            S(Grupa.Procesor, "CPU", "Opterećenje procesora", Jedinica.Procenat, 0),
            S(Grupa.Procesor, "CPU", "Opterećenje jezgra 1", Jedinica.Procenat, 1),
            S(Grupa.Grafika, "GPU", "Opterećenje", Jedinica.Procenat, 2),
            S(Grupa.Memorija, "RAM", "Zauzeće", Jedinica.Procenat, 3),
        };

        var kanali = KanalVm.Napravi(senzori);
        Proveri.Da(!kanali.Single(k => k.Naslov == "CPU").Prazan, "CPU ima merač");
        Proveri.Da(!kanali.Single(k => k.Naslov == "GPU").Prazan, "GPU ima merač");
        Proveri.Da(kanali.Single(k => k.Naslov == "Diskovi").Prazan, "diskovi nemaju ništa i kažu to");
        Proveri.Da(senzori[0].Pragovi() is null, "procenat nema granicu, pa nema ni boje ni lampice");

        Napuni(kanali.Single(k => k.Naslov == "CPU"), senzori.Take(2).ToList());
        Proveri.Jednako(20.0, senzori[0].Broj, "vrednost stiže u merač");
        return Task.CompletedTask;
    }

    /// <summary>A 32-thread processor: the core bars and the busiest core must count to 31, not stop at 9.</summary>
    public static Task TridesetDvaJezgra()
    {
        var senzori = Enumerable.Range(1, 32)
            .Select(i => S(Grupa.Procesor, "CPU", $"Opterećenje jezgra {i}", Jedinica.Procenat, i - 1))
            .ToList();

        var cpu = KanalVm.Napravi(senzori).Single(k => k.Naslov == "CPU");
        Proveri.Jednako("Jezgra (32 niti)", cpu.JezgraNaslov, "naslov broji sve niti");
        Proveri.Jednako(32, cpu.Jezgra.Count, "i sve crtice");

        var vrednosti = Enumerable.Range(0, 32).Select(i => (double?)(i == 30 ? 99 : 5)).ToArray();
        cpu.Osvezi(new Snimak(DateTime.Now, vrednosti), vrednosti, vrednosti, DateTime.Now);
        Proveri.Jednako("jezgro 31: 99 %", cpu.Dodatak, "najopterećenije je jezgro 31, ne jezgro 9");
        return Task.CompletedTask;
    }

    /// <summary>Twelve drives in one strip: twelve meters, twelve keys, and the count said in Serbian.</summary>
    public static Task DvanaestDiskova()
    {
        var senzori = Enumerable.Range(0, 12)
            .Select(i => S(Grupa.Diskovi, $"Disk {i}", "Temperatura", Jedinica.Celzijus, i))
            .ToList();

        var diskovi = KanalVm.Napravi(senzori).Single(k => k.Naslov == "Diskovi");
        Proveri.Jednako("12 diskova", diskovi.Kratko, "broj diskova u malom redu");
        Proveri.Jednako(12, diskovi.Meraci.Count, "dvanaest merača");
        Proveri.Jednako(12, diskovi.Podkanali.Count, "dvanaest diskova u pilulama");
        Proveri.Jednako(12, diskovi.Podkanali.Select(p => p.Kljuc).Distinct().Count(), "svaki disk ima svoj ključ");
        return Task.CompletedTask;
    }

    /// <summary>A drive without a thermometer (an older USB box): the meter falls back to used space, and the
    /// readout must not call anything "the hottest".</summary>
    public static Task DiskoviBezTermometra()
    {
        var senzori = new List<SenzorVm>
        {
            S(Grupa.Diskovi, "USB 1", "Zauzeće", Jedinica.Procenat, 0),
            S(Grupa.Diskovi, "USB 2", "Zauzeće", Jedinica.Procenat, 1),
        };

        var diskovi = KanalVm.Napravi(senzori).Single(k => k.Naslov == "Diskovi");
        Proveri.Jednako("2 diska", diskovi.Kratko, "dva diska");
        Proveri.Jednako("", diskovi.DrugiTekst, "nema „najtopliji“ kad nijedan nema termometar");
        Proveri.Jednako(2, diskovi.Meraci.Count, "merači postoje i bez temperature");
        return Task.CompletedTask;
    }

    /// <summary>A card whose driver gives no VRAM: the line under the meters shows the temperature only.</summary>
    public static Task GrafikaBezVram()
    {
        var senzori = new List<SenzorVm>
        {
            S(Grupa.Grafika, "GPU", "Opterećenje", Jedinica.Procenat, 0),
            S(Grupa.Grafika, "GPU", "Temperatura", Jedinica.Celzijus, 1),
        };

        var gpu = KanalVm.Napravi(senzori).Single(k => k.Naslov == "GPU");
        Napuni(gpu, senzori);

        Proveri.Da(!gpu.Dodatak.Contains("VRAM"), "ne pominje VRAM koji ne postoji");
        Proveri.Da(gpu.Dodatak.Contains("°C"), $"dodatak je temperatura (dobijeno „{gpu.Dodatak}“)");
        return Task.CompletedTask;
    }

    /// <summary>An old board with no fan headers: the channel is empty and says why.</summary>
    public static Task PlocaBezVentilatora()
    {
        var prazno = GrupaOpis.Prazno(Grupa.Hladjenje, drajver: true, potpunNadzor: true);
        Proveri.Da(prazno.Contains("ventilatore"), $"kaže da ploča ne daje ventilatore („{prazno}“)");
        Proveri.Da(prazno.Contains("GPU"), "i gde je ventilator kartice");

        var kanali = KanalVm.Napravi([]);
        Proveri.Da(kanali.Single(k => k.Naslov == "Vent.").Prazan, "kanal ventilatora je prazan");
        return Task.CompletedTask;
    }

    /// <summary>A workstation with many sensors: building the console must not become quadratic.</summary>
    public static Task StoPedesetSenzora()
    {
        var senzori = Enumerable.Range(0, 150)
            .Select(i => S(Grupa.Procesor, "CPU", $"Opterećenje jezgra {i + 1}", Jedinica.Procenat, i))
            .ToList();

        var sat = System.Diagnostics.Stopwatch.StartNew();
        var kanali = KanalVm.Napravi(senzori);
        sat.Stop();

        Proveri.Jednako(5, kanali.Count, "pet kanala");
        Proveri.Da(sat.ElapsedMilliseconds < 1000, $"pult se pravi brzo ({sat.ElapsedMilliseconds} ms za 150 senzora)");
        return Task.CompletedTask;
    }

    /// <summary>Sensor names the program has never seen stay as they are: nothing is invented, nothing crashes.</summary>
    public static Task NepoznataImena()
    {
        var senzori = new List<SenzorVm>
        {
            S(Grupa.Procesor, "CPU", "CPU Total", Jedinica.Procenat, 0),
            S(Grupa.Hladjenje, "Ploča", "Fan #2", Jedinica.Obrtaj, 1),
            S(Grupa.Diskovi, "Disk X", "Temperature", Jedinica.Celzijus, 2),
        };

        var kanali = KanalVm.Napravi(senzori);
        Proveri.Da(kanali.Single(k => k.Naslov == "CPU").Meraci.Count > 0, "CPU dobija merač i sa stranim imenom");
        Proveri.Jednako("CPU Total", senzori[0].Ime, "ime ostaje kako je došlo");
        Proveri.Jednako("Fan #2", senzori[1].Ime, "i kod ventilatora");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Two drives both have a sensor called "Temperatura" and both must stay: merging the sources by name alone once
    /// threw five of six drives away (125 sensors became 93 - seen live 19. 11. 2026). The device is part of a sensor.
    /// </summary>
    public static Task IstaImenaRazniUredjaji()
    {
        var diskovi = new Jedan(
            (new Senzor(new SenzorKljuc(Grupa.Diskovi, "Samsung 870", "Temperatura"), Jedinica.Celzijus), 34),
            (new Senzor(new SenzorKljuc(Grupa.Diskovi, "ST12000VE", "Temperatura"), Jedinica.Celzijus), 37));
        using var spoj = new SlozeniIzvor(diskovi);

        Proveri.Jednako(2, spoj.Senzori.Count, "oba diska zadržavaju svoju temperaturu");
        Proveri.Jednako("Samsung 870", spoj.Senzori[0].Uredjaj, "prvi disk");
        Proveri.Jednako("ST12000VE", spoj.Senzori[1].Uredjaj, "drugi disk");
        var vrednosti = spoj.Uzmi().Vrednosti;
        Proveri.Jednako(34.0, vrednosti[0], "prva vrednost je prvog diska");
        Proveri.Jednako(37.0, vrednosti[1], "druga vrednost je drugog diska");
        return Task.CompletedTask;
    }

    /// <summary>A few sensors, one value each: the smallest source there is, for the merge tests.</summary>
    private sealed class Jedan : IIzvorSenzora
    {
        private readonly double[] _vrednosti;

        public Jedan(params (Senzor Senzor, double Vrednost)[] senzori)
        {
            Senzori = senzori.Select(s => s.Senzor).ToArray();
            _vrednosti = senzori.Select(s => s.Vrednost).ToArray();
        }

        public string Naziv => "probni";
        public bool Pravi => true;
        public IReadOnlyList<Senzor> Senzori { get; }
        public string? Poruka => null;
        public Snimak Uzmi() => new(DateTime.Now, _vrednosti.Select(v => (double?)v).ToArray());
        public void Dispose() { }
    }
}

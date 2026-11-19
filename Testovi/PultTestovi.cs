using Takt.Merenje;
using Takt.Prikaz;
using Takt.Sistem;
using Takt.Vm;

namespace Takt.Testovi;

/// <summary>The console: which channels exist, which meters they carry, how the VU peak moves.</summary>
public static class PultTestovi
{
    private static List<SenzorVm> Senzori(IIzvorSenzora izvor) =>
        izvor.Senzori.Select((s, i) => new SenzorVm(s, i, new Prsten(10))).ToList();

    public static Task Kanali()
    {
        using var izvor = new LazniIzvor();
        var kanali = KanalVm.Napravi(Senzori(izvor));

        Proveri.Jednako("CPU, GPU, RAM, Diskovi, Vent.", string.Join(", ", kanali.Select(k => k.Naslov)), "redosled kanala");
        Proveri.Da(kanali.All(k => !k.Prazan), "probni podaci daju svakom kanalu bar jedan merač");

        var cpu = kanali[0];
        Proveri.Jednako("Opterećenje procesora", cpu.Levi?.Senzor.Ime, "CPU levo: opterećenje");
        Proveri.Jednako("Temperatura jezgra 1", cpu.Desni?.Senzor.Ime, "CPU desno: temperatura");
        Proveri.Jednako("Zauzeće commit-a", kanali[2].Desni?.Senzor.Ime, "RAM desno: commit");
        Proveri.Jednako("1", kanali[4].Levi?.Oznaka, "ventilatori: prvi i drugi");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Six disks used to be six strips and pushed the rest off the console. Now they are one group: one meter
    /// per disk, the hottest one in the readout, and the solo panel switches between them.
    /// </summary>
    public static Task GrupaDiskova()
    {
        var senzori = new List<SenzorVm>();
        double[] temperature = [41, 35, 52, 38, 44, 36];
        for (int i = 0; i < temperature.Length; i++)
        {
            senzori.Add(new(new Senzor(new SenzorKljuc(Grupa.Diskovi, $"Disk model {i}", "Temperatura"), Jedinica.Celzijus), senzori.Count, new Prsten(4)));
            senzori.Add(new(new Senzor(new SenzorKljuc(Grupa.Diskovi, $"Disk model {i}", "Aktivnost"), Jedinica.Procenat), senzori.Count, new Prsten(4)));
        }

        var kanali = KanalVm.Napravi(senzori);
        Proveri.Jednako(1, kanali.Count(k => k.Naslov.StartsWith("Disk")), "šest diskova je jedan kanal");

        var diskovi = kanali.Single(k => k.Naslov == "Diskovi");
        Proveri.Jednako(6, diskovi.Meraci.Count, "po jedan merač za svaki disk");
        Proveri.Jednako("1 2 3 4 5 6", string.Join(" ", diskovi.Meraci.Select(m => m.Oznaka)), "merači su numerisani");
        Proveri.Da(diskovi.Meraci.All(m => m.Senzor.Ime == "Temperatura"), "merač diska je njegova temperatura");
        Proveri.Jednako("6 diskova", diskovi.Kratko, "ispod imena koliko ih je");
        Proveri.Da(diskovi.ImaIzbor, "solo panel nudi izbor diska");

        var vrednosti = new double?[senzori.Count];
        for (int i = 0; i < temperature.Length; i++) { vrednosti[2 * i] = temperature[i]; vrednosti[2 * i + 1] = 5; }
        diskovi.Osvezi(new Snimak(DateTime.Now, vrednosti), vrednosti, vrednosti, DateTime.Now);

        Proveri.Jednako(52.0, diskovi.Glavni?.Broj, "veliki broj je najtopliji disk");
        Proveri.Jednako("Disk model 0", diskovi.Uredjaj, "solo kreće od prvog diska");

        diskovi.IzabraniPodkanal = diskovi.Podkanali[2];
        Proveri.Jednako("Disk model 2", diskovi.Uredjaj, "izbor menja uređaj u zaglavlju");
        Proveri.Da(diskovi.Detalj.Senzori.All(s => s.Uredjaj == "Disk model 2"), "i tabelu");

        Proveri.Jednako("2 diska", KanalVm.BrojDiskova(2), "dva diska");
        Proveri.Jednako("1 disk", KanalVm.BrojDiskova(1), "jedan disk");
        Proveri.Jednako("12 diskova", KanalVm.BrojDiskova(12), "dvanaest");
        return Task.CompletedTask;
    }

    /// <summary>Without the driver there are no disks and no fans: their strips still stand, pale, as places.</summary>
    public static Task PraznaMesta()
    {
        var senzori = new List<SenzorVm>
        {
            new(new Senzor(new SenzorKljuc(Grupa.Procesor, "AMD Ryzen 5 5600", "Opterećenje procesora"), Jedinica.Procenat), 0, new Prsten(4)),
        };
        var kanali = KanalVm.Napravi(senzori);

        Proveri.Jednako("CPU, GPU, RAM, Diskovi, Vent.", string.Join(", ", kanali.Select(k => k.Naslov)), "mesta za sve delove");
        Proveri.Da(!kanali[0].Prazan && !kanali[0].DvaMeraca, "CPU sa jednim meračem");
        Proveri.Da(kanali.Skip(1).All(k => k.Prazan), "ostali su prazna mesta");
        Proveri.Jednako("Ryzen 5 5600", kanali[0].Kratko, "kratko ime uređaja");
        Proveri.Jednako("RX 6600", KanalVm.Skrati("AMD Radeon RX 6600"), "kratko ime grafičke");

        // An empty strip names the key that is actually missing, not always administrator rights.
        Proveri.Jednako(PawnIo.JeTu ? "traži nadzor" : "traži drajver", kanali[1].DrugiTekst,
            "prazno mesto kaže šta mu treba");
        Proveri.Da(kanali[1].OpisKanala.Contains("Klikni za objašnjenje"), "tooltip upućuje na objašnjenje");
        return Task.CompletedTask;
    }

    /// <summary>The ramp: green at the bottom, amber halfway, red at the top; the part's own limits move the amber.</summary>
    public static Task Rampa()
    {
        Proveri.Blizu(0, VuMerac.Polozaj(0, null, null), 0.01, "dno je zeleno");
        Proveri.Blizu(50, VuMerac.Polozaj(0.5, null, null), 0.01, "pola skale je žuto");
        Proveri.Blizu(100, VuMerac.Polozaj(1, null, null), 0.01, "vrh je crven");

        // With the part's limits: amber starts at the warm limit, red at the hot one.
        Proveri.Blizu(50, VuMerac.Polozaj(0.8, 0.8, 0.95), 0.01, "toplo je žuto");
        Proveri.Blizu(100, VuMerac.Polozaj(0.95, 0.8, 0.95), 0.01, "vruće je crveno");
        Proveri.Da(VuMerac.Polozaj(0.7, 0.8, 0.95) < 50, "ispod toplog još nije žuto");

        Proveri.Jednako(0, VuMerac.Upaljeno(0, 20), "ništa upaljeno na nuli");
        Proveri.Jednako(20, VuMerac.Upaljeno(1, 20), "sve upaljeno na vrhu");
        return Task.CompletedTask;
    }

    /// <summary>The peak holds two seconds, then falls one segment (of twenty) per second; a higher value lifts it at once.</summary>
    public static Task Vrh()
    {
        var senzor = new SenzorVm(new Senzor(new SenzorKljuc(Grupa.Procesor, "CPU", "Opterećenje procesora"), Jedinica.Procenat), 0, new Prsten(4));
        var merac = new MeracVm(senzor, "%");
        var t = new DateTime(2026, 11, 18, 12, 0, 0);

        senzor.Osvezi(80, 80, 80); merac.Osvezi(t);
        Proveri.Blizu(0.8, merac.Vrh, 1e-9, "vrh skače odmah");

        senzor.Osvezi(20, 20, 80); merac.Osvezi(t.AddSeconds(1));
        Proveri.Blizu(0.8, merac.Vrh, 1e-9, "vrh stoji prve dve sekunde");
        Proveri.Blizu(0.2, merac.Udeo, 1e-9, "merač pada odmah");

        senzor.Osvezi(20, 20, 80); merac.Osvezi(t.AddSeconds(3));
        Proveri.Blizu(0.7, merac.Vrh, 1e-9, "posle zadržavanja pada 1/20 u sekundi");

        senzor.Osvezi(90, 20, 90); merac.Osvezi(t.AddSeconds(4));
        Proveri.Blizu(0.9, merac.Vrh, 1e-9, "nova najviša vrednost diže vrh");

        senzor.Osvezi(null, 20, 90); merac.Osvezi(t.AddSeconds(5));
        Proveri.Jednako(0.0, merac.Udeo, "bez vrednosti merač je prazan, ne izmišlja");
        Proveri.Da(!merac.ImaVrednost, "i to zna");
        return Task.CompletedTask;
    }

    public static Task Segmenti()
    {
        Proveri.Jednako(0, VuMerac.Upaljeno(0, 20), "nula ne pali ništa");
        Proveri.Jednako(1, VuMerac.Upaljeno(0.01, 20), "bilo šta iznad nule pali prvi segment");
        Proveri.Jednako(10, VuMerac.Upaljeno(0.5, 20), "pola");
        Proveri.Jednako(20, VuMerac.Upaljeno(1, 20), "puno");

        // Temperature: 20-100 °C on the scale; a 50 °C reading is 3/8 of the way up.
        var senzor = new SenzorVm(new Senzor(new SenzorKljuc(Grupa.Grafika, "GPU", "Temperatura"), Jedinica.Celzijus), 0, new Prsten(4));
        var merac = new MeracVm(senzor, "°C");
        senzor.Osvezi(50, 50, 50);
        merac.Osvezi(DateTime.Now);
        Proveri.Blizu(0.375, merac.Udeo, 1e-9, "50 °C na skali 20-100");
        Proveri.Blizu(0.875, merac.Vruce ?? 0, 1e-9, "granica 90 °C na skali");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The split that keeps the window cheap: a reading is taken silently, and the window is told separately.
    /// Without it every sensor raised a dozen notifications per second, whether or not anyone was looking.
    /// </summary>
    public static Task TihoIPaObavesti()
    {
        var senzor = new SenzorVm(
            new Senzor(new SenzorKljuc(Grupa.Procesor, "CPU", "Test"), Jedinica.Procenat), 0, new Prsten(4));

        int promena = 0;
        senzor.PropertyChanged += (_, _) => promena++;

        senzor.Postavi(42, 0, 100);
        Proveri.Jednako(0, promena, "Postavi ne diže obaveštenja");
        Proveri.Jednako<double?>(42, senzor.Broj, "vrednost je ipak primljena");

        senzor.Obavesti();
        Proveri.Da(promena >= 10, $"Obavesti diže sve kolone (bilo {promena})");
        Proveri.Jednako("42 %", senzor.Vrednost, "tekst je izračunat");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The strip's number and the small line under it are on screen all the time; they must be fresh even when
    /// the long table waits for its two seconds. Otherwise a channel would freeze the moment it is not selected.
    /// </summary>
    public static Task BezTabele()
    {
        using var izvor = new LazniIzvor();
        var kanali = KanalVm.Napravi(Senzori(izvor));
        var cpu = kanali[0];

        int n = izvor.Senzori.Count;
        var snimak = new Snimak(DateTime.Now, Enumerable.Repeat<double?>(50, n).ToArray());
        var najmanje = Enumerable.Repeat<double?>(0, n).ToArray();
        var najvise = Enumerable.Repeat<double?>(100, n).ToArray();

        cpu.Osvezi(snimak, najmanje, najvise, DateTime.Now, obavestiTabelu: false);

        Proveri.Jednako<double?>(50, cpu.Levi?.Senzor.Broj, "merač dobija vrednost i bez tabele");
        Proveri.Jednako("50 %", cpu.Levi?.Senzor.Vrednost, "levi broj na traci je osvežen");
        Proveri.Jednako("50,0 °C", cpu.DrugiTekst, "desni broj na traci je osvežen");

        // The fan channel has two meters and one big number: the second fan must still be readable under it.
        var vent = kanali[4];
        vent.Osvezi(snimak, najmanje, najvise, DateTime.Now, obavestiTabelu: false);
        Proveri.Da(!string.IsNullOrWhiteSpace(vent.DrugiTekst),
            $"ventilatori: drugi broj ispod velikog (bilo: „{vent.DrugiTekst}“)");
        return Task.CompletedTask;
    }
}

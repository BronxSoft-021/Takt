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

        Proveri.Jednako("CPU, GPU, RAM, Disk 1, Vent.", string.Join(", ", kanali.Select(k => k.Naslov)), "redosled kanala");
        Proveri.Da(kanali.All(k => !k.Prazan), "probni podaci daju svakom kanalu bar jedan merač");

        var cpu = kanali[0];
        Proveri.Jednako("Opterećenje procesora", cpu.Levi?.Senzor.Ime, "CPU levo: opterećenje");
        Proveri.Jednako("Temperatura jezgra 1", cpu.Desni?.Senzor.Ime, "CPU desno: temperatura");
        Proveri.Jednako("Zauzeće commit-a", kanali[2].Desni?.Senzor.Ime, "RAM desno: commit");
        Proveri.Jednako("Aktivnost", kanali[3].Levi?.Senzor.Ime, "disk levo: aktivnost");
        Proveri.Jednako("1", kanali[4].Levi?.Oznaka, "ventilatori: prvi i drugi");
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

        Proveri.Jednako("CPU, GPU, RAM, Disk 1, Vent.", string.Join(", ", kanali.Select(k => k.Naslov)), "mesta za sve delove");
        Proveri.Da(!kanali[0].Prazan && !kanali[0].DvaMeraca, "CPU sa jednim meračem");
        Proveri.Da(kanali.Skip(1).All(k => k.Prazan), "ostali su prazna mesta");
        Proveri.Jednako("Ryzen 5 5600", kanali[0].Kratko, "kratko ime uređaja");
        Proveri.Jednako("RX 6600", KanalVm.Skrati("AMD Radeon RX 6600"), "kratko ime grafičke");
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
}

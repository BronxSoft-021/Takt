using Takt.Merenje;
using Takt.Prikaz;
using Takt.Sistem;
using Takt.Vm;

namespace Takt.Testovi;

/// <summary>What the screen does with the numbers: order, labels, hidden rows, a probe run's settings.</summary>
public static class PrikazTestovi
{
    public static Task KopijaPodesavanja()
    {
        var sacuvano = new Podesavanja { ProbniPodaci = false, Istorija = true, Osvezavanje = 2 };
        var rad = sacuvano.Kopija();
        rad.ProbniPodaci = true;
        rad.Istorija = false;

        Proveri.Da(!sacuvano.ProbniPodaci, "probni rad ne uključuje probne podatke u sačuvanim podešavanjima");
        Proveri.Da(sacuvano.Istorija, "probni rad ne gasi istoriju u sačuvanim podešavanjima");
        Proveri.Jednako(2, rad.Osvezavanje, "kopija nosi ostale izbore");
        return Task.CompletedTask;
    }

    public static Task Prirodno()
    {
        var imena = new List<string>
        {
            "Opterećenje jezgra 10", "Opterećenje jezgra 2", "Opterećenje jezgra 1",
            "Opterećenje jezgra 12", "Opterećenje jezgra 11", "Opterećenje jezgra 3",
        };
        imena.Sort(Merenje.Prirodno.Poredak);

        Proveri.Jednako("Opterećenje jezgra 1, Opterećenje jezgra 2, Opterećenje jezgra 3, Opterećenje jezgra 10, " +
                        "Opterećenje jezgra 11, Opterećenje jezgra 12",
            string.Join(", ", imena), "brojevi u imenu se porede kao brojevi");

        Proveri.Da(Merenje.Prirodno.Poredak.Compare("Takt", "Takt jezgra") < 0, "kraće ime ide prvo");
        Proveri.Jednako(0, Merenje.Prirodno.Poredak.Compare("Ventilator 02", "Ventilator 2"), "vodeće nule ne menjaju broj");
        return Task.CompletedTask;
    }

    public static Task PreKoliko()
    {
        Proveri.Jednako("pre 1 s", Grafikon.PreKoliko(0), "bar jedna sekunda");
        Proveri.Jednako("pre 45 s", Grafikon.PreKoliko(45), "sekunde");
        Proveri.Jednako("pre 2 min", Grafikon.PreKoliko(119), "120 uzoraka po sekundu");
        Proveri.Jednako("pre 10 min", Grafikon.PreKoliko(595), "120 uzoraka na 5 s");
        Proveri.Jednako("pre 1 h 10 min", Grafikon.PreKoliko(4200), "sati i minuti");
        return Task.CompletedTask;
    }

    /// <summary>A screen with one silent sensor shows the others and offers the silent one behind a click.</summary>
    public static Task Skriveni()
    {
        var senzori = new[]
        {
            new Senzor(new SenzorKljuc(Grupa.Procesor, "CPU", "Temperatura procesora"), Jedinica.Celzijus),
            new Senzor(new SenzorKljuc(Grupa.Procesor, "CPU", "Opterećenje procesora"), Jedinica.Procenat),
        };
        var vm = senzori.Select((s, i) => new SenzorVm(s, i, new Prsten(10))).ToList();
        var ekran = new EkranVm(Grupa.Procesor, vm);

        ekran.Osvezi(new Snimak(DateTime.Now, [null, 30]), [null, 30], [null, 30]);

        Proveri.Jednako(1, ekran.BezVrednosti, "jedan senzor nije dao broj");
        Proveri.Da(ekran.ImaSkrivenih, "ponuđen je klik za skrivene");
        Proveri.Jednako(1, ekran.Pogled.Cast<object>().Count(), "spisak pokazuje samo senzor sa vrednošću");
        Proveri.Jednako("Opterećenje procesora", ekran.Izabran?.Ime, "otvoren je senzor koji ima vrednost");
        Proveri.Jednako("Još 1 senzor bez vrednosti", ekran.SkriveniTekst, "tekst ispod spiska");

        ekran.SviSenzori = true;
        Proveri.Jednako(2, ekran.Pogled.Cast<object>().Count(), "posle klika vide se svi");

        ekran.SviSenzori = false;
        ekran.Osvezi(new Snimak(DateTime.Now, [55, 30]), [55, 30], [55, 30]);
        Proveri.Jednako(0, ekran.BezVrednosti, "temperatura se javila");
        Proveri.Jednako(2, ekran.Pogled.Cast<object>().Count(), "senzor koji se javio ulazi u spisak sam");
        Proveri.Da(!ekran.ImaSkrivenih, "nema više skrivenih");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The drive strip measures one quantity for all drives, and the user picks which: temperature (default), used
    /// space or lifetime. A drive without the chosen quantity gets no bar - before, it fell back to its used space,
    /// so a "99 %" bar stood beside a "45 °C" bar (a report called it a mishmash, 19. 11. 2026).
    /// </summary>
    public static Task MetarDiskova()
    {
        var senzori = new List<SenzorVm>
        {
            new(new Senzor(new SenzorKljuc(Grupa.Diskovi, "Topli", "Temperatura"), Jedinica.Celzijus), 0, new Prsten(8)),
            new(new Senzor(new SenzorKljuc(Grupa.Diskovi, "Topli", "Zauzeće"), Jedinica.Procenat), 1, new Prsten(8)),
            new(new Senzor(new SenzorKljuc(Grupa.Diskovi, "Bez termometra", "Zauzeće"), Jedinica.Procenat), 2, new Prsten(8)),
        };

        var poTemperaturi = KanalVm.Napravi(senzori).Single(k => k.Naslov == "Diskovi");
        Proveri.Jednako(1, poTemperaturi.Meraci.Count, "samo disk sa termometrom dobija stub");
        Proveri.Jednako(Jedinica.Celzijus, poTemperaturi.Meraci[0].Senzor.Senzor.Jedinica, "stub je temperatura");

        var poZauzecu = KanalVm.Napravi(senzori, metar: "Zauzeće").Single(k => k.Naslov == "Diskovi");
        Proveri.Jednako(2, poZauzecu.Meraci.Count, "po zauzeću oba diska imaju stub");
        Proveri.Da(poZauzecu.Meraci.All(m => m.Senzor.Senzor.Jedinica == Jedinica.Procenat), "svi stubovi su procenti");
        Proveri.Jednako("Zauzeće", poZauzecu.Meraci[0].Senzor.Ime, "i to je zauzeće");
        return Task.CompletedTask;
    }

    /// <summary>
    /// A drive's rows are three different things, and the list says which is which: the temperature now, the health
    /// from SMART (read every ten minutes), the space of the volumes on the drive, and - apart - the limits the
    /// drive reports about itself. Mixed together, "Životni vek 93 %" sat right above "Zauzeće 54 %" and read as
    /// the same kind of number.
    /// </summary>
    public static Task VrsteDiskova()
    {
        const string disk = "ST12000VE";
        var senzori = new[]
        {
            new Senzor(new SenzorKljuc(Grupa.Diskovi, disk, "Temperatura"), Jedinica.Celzijus),
            new Senzor(new SenzorKljuc(Grupa.Diskovi, disk, "Životni vek"), Jedinica.Procenat),
            new Senzor(new SenzorKljuc(Grupa.Diskovi, disk, "Zauzeće"), Jedinica.Procenat),
            new Senzor(new SenzorKljuc(Grupa.Diskovi, disk, "Slobodan prostor"), Jedinica.Megabajt),
            new Senzor(new SenzorKljuc(Grupa.Diskovi, disk, Granice.Upozorenje), Jedinica.Celzijus),
        };
        var vm = senzori.Select((s, i) => new SenzorVm(s, i, new Prsten(8))).ToList();

        Proveri.Jednako("Temperature", vm[0].Vrsta, "temperatura je svoja sekcija");
        Proveri.Jednako("Zdravlje i SMART", vm[1].Vrsta, "životni vek je zdravlje, ne zauzeće");
        Proveri.Jednako("Prostor (particija)", vm[2].Vrsta, "zauzeće pripada particiji");
        Proveri.Jednako("Prostor (particija)", vm[3].Vrsta, "i slobodan prostor");
        Proveri.Jednako("Granice koje prijavljuje uređaj", vm[4].Vrsta, "granica nije merenje");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The board's numbered temperature channels are inputs of the Super I/O chip, and nobody knows what they
    /// measure. One that stands still at a high value ("Temperatura 6" at 80,0 °C, min = average = max) has nothing
    /// attached and leaves the list; the one that moves stays, but without colour or clip light (SenzorVm.Pragovi).
    /// </summary>
    public static Task MrtvePloce()
    {
        var senzori = new[]
        {
            new Senzor(new SenzorKljuc(Grupa.Hladjenje, "Ploča", "Temperatura 1"), Jedinica.Celzijus),
            new Senzor(new SenzorKljuc(Grupa.Hladjenje, "Ploča", "Temperatura 6"), Jedinica.Celzijus),
            new Senzor(new SenzorKljuc(Grupa.Hladjenje, "Ploča", "Ventilator 1"), Jedinica.Obrtaj),
        };
        var vm = senzori.Select((s, i) => new SenzorVm(s, i, new Prsten(80))).ToList();
        var ekran = new EkranVm(Grupa.Hladjenje, vm);

        for (int i = 0; i < 40; i++)
        {
            double zivi = 33 + i % 3;
            vm[0].Prsten.Dodaj(zivi);
            vm[1].Prsten.Dodaj(80);
            vm[2].Prsten.Dodaj(1200);
            ekran.Osvezi(new Snimak(DateTime.Now, [zivi, 80, 1200]), [zivi, 80, 1200], [zivi, 80, 1200]);
        }

        Proveri.Da(vm[1].Mrtav, "kanal koji stoji na 80,0 °C je mrtav");
        Proveri.Da(!vm[0].Mrtav, "kanal koji se menja nije mrtav");
        Proveri.Jednako(2, ekran.Pogled.Cast<object>().Count(), "mrtav kanal se ne prikazuje");
        Proveri.Da(!ekran.Pogled.Cast<SenzorVm>().Any(s => s.Ime == "Temperatura 6"), "nema ga u spisku");

        // A channel that never moves but reads low is a real one (a cold board), so it stays.
        var hladni = new[] { new Senzor(new SenzorKljuc(Grupa.Hladjenje, "Ploča", "Temperatura 2"), Jedinica.Celzijus) };
        var hladan = new SenzorVm(hladni[0], 0, new Prsten(80));
        var ekranHladni = new EkranVm(Grupa.Hladjenje, [hladan]);
        for (int i = 0; i < 40; i++)
        {
            hladan.Prsten.Dodaj(34);
            ekranHladni.Osvezi(new Snimak(DateTime.Now, [34]), [34], [34]);
        }
        Proveri.Da(!hladan.Mrtav, "34,0 °C koje stoji nije mrtav kanal");
        Proveri.Jednako(1, ekranHladni.Pogled.Cast<object>().Count(), "i prikazuje se");
        return Task.CompletedTask;
    }

    /// <summary>"Kritična granica 84 °C" is a limit: no colour, no bar; the drive's temperature is marked against it.</summary>
    public static Task GraniceDiska()
    {
        const string disk = "Samsung SSD 970 EVO Plus 500GB";
        var senzori = new[]
        {
            new Senzor(new SenzorKljuc(Grupa.Diskovi, disk, "Temperatura"), Jedinica.Celzijus),
            new Senzor(new SenzorKljuc(Grupa.Diskovi, disk, Granice.Upozorenje), Jedinica.Celzijus),
            new Senzor(new SenzorKljuc(Grupa.Diskovi, disk, Granice.Kriticna), Jedinica.Celzijus),
        };
        var vm = senzori.Select((s, i) => new SenzorVm(s, i, new Prsten(10))).ToList();
        var ekran = new EkranVm(Grupa.Diskovi, vm);

        ekran.Osvezi(new Snimak(DateTime.Now, [62, 70, 84]), [62, 70, 84], [62, 70, 84]);

        var kriticna = vm[2];
        Proveri.Da(kriticna.JeGranica, "kritična granica je granica");
        Proveri.Jednako("", kriticna.Nivo, "granica nije ni topla ni vruća");
        Proveri.Da(!kriticna.ImaUdeo, "granica nema traku popunjenosti");
        Proveri.Jednako("Granice koje prijavljuje uređaj", kriticna.Vrsta, "granice imaju svoj naslov");

        // 62 °C is above the general disk mark (55) but below this drive's own warning limit (70).
        Proveri.Jednako("", vm[0].Nivo, "62 °C je ispod granice upozorenja ovog diska");
        ekran.Osvezi(new Snimak(DateTime.Now, [75, 70, 84]), [62, 70, 84], [75, 70, 84]);
        Proveri.Jednako("toplo", vm[0].Nivo, "preko granice upozorenja");
        ekran.Osvezi(new Snimak(DateTime.Now, [85, 70, 84]), [62, 70, 84], [85, 70, 84]);
        Proveri.Jednako("vruce", vm[0].Nivo, "preko kritične granice");
        return Task.CompletedTask;
    }
}

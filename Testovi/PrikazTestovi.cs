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
}

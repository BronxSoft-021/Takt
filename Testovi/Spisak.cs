namespace Takt.Testovi;

/// <summary>Every test in one list, run in order by <c>Program.cs</c>.</summary>
public static class Spisak
{
    /// <summary>Folder for temporary files; <c>Program.cs</c> creates a fresh one on every run.</summary>
    public static string Radni { get; set; } = "";

    public static string NoviFajl(string ime) => Path.Combine(Radni, ime);

    public static readonly (string Ime, Func<Task> Test)[] Svi =
    [
        ("prsten: red, punjenje i prepisivanje", PrstenTestovi.Red),
        ("prsten: rupe u podacima i raspon", PrstenTestovi.Rupe),
        ("prsten: prazan i kapacitet", PrstenTestovi.Prazan),
        ("format: zapeta, jedinice i crtica", FormatTestovi.Zapeta),
        ("kriva: podrazumevana i između tačaka", KrivaTestovi.Podrazumevana),
        ("kriva: provera pogrešnih kriva", KrivaTestovi.Provera),
        ("baza: upis senzora i vrednosti", BazaTestovi.UpisICitanje),
        ("baza: brisanje starog i ponovno otvaranje", BazaTestovi.Brisanje),
        ("nadzor: probni izvor daje vrednosti i istoriju", NadzorTestovi.Uzorkovanje),
        ("nadzor: posle 60 uzoraka istorija je u bazi", NadzorTestovi.UpisUMinuti),
        ("izvor: probni podaci pokrivaju svih pet grupa", NadzorTestovi.Grupe),
        ("nadzor: brisanje istorije i kad je baza zatvorena", NadzorTestovi.BrisanjeBezBaze),
        ("podešavanja: probni rad ne menja sačuvano", PrikazTestovi.KopijaPodesavanja),
        ("redosled: jezgro 2 pre jezgra 10", PrikazTestovi.Prirodno),
        ("grafikon: koliko je star najstariji uzorak", PrikazTestovi.PreKoliko),
        ("ekran: senzori bez vrednosti se sklanjaju", PrikazTestovi.Skriveni),
        ("ekran: granice diska nisu merenja", PrikazTestovi.GraniceDiska),
        ("pult: kanali i njihovi merači", PultTestovi.Kanali),
        ("pult: prazna mesta za diskove i ventilatore", PultTestovi.PraznaMesta),
        ("pult: VU vrh drži pa pada", PultTestovi.Vrh),
        ("pult: segmenti i skala temperature", PultTestovi.Segmenti),
        ("protokol: spisak i uzorak kroz tekst", ProtokolTestovi.SpisakIUzorak),
        ("prevod: imena senzora na srpski", PrevodTestovi.Imena),
        ("prevod: zauzeće VRAM u procentima", PrevodTestovi.Vram),
        ("pomoćnik: spisak i uzorak stižu kroz cev", PomocnikTest.Cev),
        ("izmene: broj verzije se poklapa sa Takt.csproj", IzmeneTestovi.Verzija),
    ];
}

/// <summary>Assertions. A failed test throws; the runner prints the message.</summary>
public static class Proveri
{
    public static void Da(bool uslov, string poruka)
    {
        if (!uslov) throw new InvalidOperationException(poruka);
    }

    public static void Jednako<T>(T ocekivano, T stvarno, string poruka)
    {
        if (!EqualityComparer<T>.Default.Equals(ocekivano, stvarno))
            throw new InvalidOperationException($"{poruka} (očekivano {ocekivano}, dobijeno {stvarno})");
    }

    public static void Blizu(double ocekivano, double stvarno, double tolerancija, string poruka)
    {
        if (Math.Abs(ocekivano - stvarno) > tolerancija)
            throw new InvalidOperationException($"{poruka} (očekivano {ocekivano}, dobijeno {stvarno})");
    }
}

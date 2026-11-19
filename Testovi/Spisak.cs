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
        ("format: jedna kultura za ceo program", FormatTestovi.Kultura),
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
        ("ekran: mrtav kanal ploče se ne prikazuje", PrikazTestovi.MrtvePloce),
        ("pult: kanali i njihovi merači", PultTestovi.Kanali),
        ("pult: prazna mesta za diskove i ventilatore", PultTestovi.PraznaMesta),
        ("pult: šest diskova je jedan kanal", PultTestovi.GrupaDiskova),
        ("pult: najtopliji je samo među temperaturama", PultTestovi.Najtopliji),
        ("pult: merač bez skale ne crta stub", PultTestovi.BezSkale),
        ("pult: VU vrh drži pa pada", PultTestovi.Vrh),
        ("pult: tiho čitanje pa obaveštenje", PultTestovi.TihoIPaObavesti),
        ("pult: traka se osvežava i kad tabela čeka", PultTestovi.BezTabele),
        ("pult: segmenti i skala temperature", PultTestovi.Segmenti),
        ("pult: rampa zeleno-žuto-crveno", PultTestovi.Rampa),
        ("cev: pravi se i pušta klijenta", CevTestovi.PraviIPusta),
        ("prozor: sačuvano mesto se koristi samo ako je na ekranu", ProzorTestovi.Sredi),
        ("prozor: mali ekran skraćuje prozor da stane", ProzorTestovi.MaliEkran),
        ("prozor: podešavanja iz trake se ne zatvaraju drugim klikom", ProzorTestovi.Podesavanja),
        ("prozor: podnožje kaže ko čita senzore", ProzorTestovi.Prava),
        ("prozor: FPS i kad je prozor sam povišen", ProzorTestovi.Kadrovi),
        ("prozor: veza sa uslugom se traži i posle starta", ProzorTestovi.TrazenjeVeze),
        ("grafikon: uska skala puni visinu (korak 2,5)", ProzorTestovi.Skala),
        ("pokretanje: linija za Windows Run", ProzorTestovi.PokretanjeSaWindowsom),
        ("pokretanje: upis u Run, čitanje i brisanje", PokretanjeTest.UpisiProcitajObrisi),
        ("drajver: fajl ili upis u registru znače instaliran", PawnIoTest.Provera),
        ("drajver: prazan kanal kaže šta mu treba", PawnIoTest.Prazno),
        ("ključ: prevedeno ime ne menja ključ senzora", KljucTestovi.Kljuc),
        ("ključ: stara istorija se prebacuje bez gubljenja", KljucTestovi.Migracija),
        ("ključ: jezgra se prepoznaju po putanji", KljucTestovi.Jezgra),
        ("zdravlje: pravila presude za diskove", ZdravljeTestovi.Provere),
        ("protokol: spisak i uzorak kroz tekst", ProtokolTestovi.SpisakIUzorak),
        ("prevod: imena senzora na srpski", PrevodTestovi.Imena),
        ("prevod: zauzeće VRAM u procentima", PrevodTestovi.Vram),
        ("pomoćnik: spisak i uzorak stižu kroz cev", PomocnikTest.Cev),
        ("usluga: služi prozor, puni istoriju i prima sledeći", UslugaTest.Posluzuje),
        ("usluga: radi iz svoje kopije, ne iz dist", BezbednostTestovi.KopijaUsluge),
        ("usluga: podmetnuta veza umesto foldera se uklanja", BezbednostTestovi.FolderBezVeze),
        ("usluga: sluša čuvanje i brisanje istorije", BezbednostTestovi.UslugaSlusaPodesavanja),
        ("prozor: samo jedan Takt, drugi otvara prvog", BezbednostTestovi.JedanTakt),
        ("igre: FPS, najduži kadar, 1% low i zastoji", IgreTestovi.Kadrovi),
        ("igre: 1% low sesije iz najgorih 1 % sekundi", IgreTestovi.Low1Sesije),
        ("igre: koja je igra (prvi plan, Takt, pregledač)", IgreTestovi.Birac),
        ("igre: sesija se snima sama i pravi izveštaj", IgreTestovi.Sesija),
        ("igre: igra ne nestaje posle tri sekunde", IgreTestovi.NeNestaje),
        ("pult: clip lampica se pali i ostaje do klika", IgreTestovi.Clip),
        ("izmene: broj verzije se poklapa sa Takt.csproj", IzmeneTestovi.Verzija),
        ("mašina: nema nijednog senzora", RazneMasine.NemaSenzora),
        ("mašina: samo procenti (bez drajvera)", RazneMasine.SamoProcenti),
        ("mašina: 32 niti u pultu", RazneMasine.TridesetDvaJezgra),
        ("mašina: dvanaest diskova", RazneMasine.DvanaestDiskova),
        ("mašina: diskovi bez termometra", RazneMasine.DiskoviBezTermometra),
        ("mašina: grafička bez VRAM senzora", RazneMasine.GrafikaBezVram),
        ("mašina: ploča bez ventilatora", RazneMasine.PlocaBezVentilatora),
        ("mašina: 150 senzora se pravi brzo", RazneMasine.StoPedesetSenzora),
        ("mašina: strana imena senzora ostaju kakva su", RazneMasine.NepoznataImena),
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

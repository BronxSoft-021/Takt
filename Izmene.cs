namespace Takt;

/// <summary>One entry of "what is new": the date, what was added and what was polished.</summary>
public sealed record Unos(string Faza, string Datum, string[] Nove, string[] Dorade)
{
    /// <summary>Counted from the history, never written by hand. See <see cref="Izmene"/>.</summary>
    public string Broj { get; internal set; } = "";

    public IEnumerable<string> Izmene => Nove.Concat(Dorade);

    public string Oznaka => Faza.Length == 0 ? Broj : $"{Broj} {Faza}";
}

/// <summary>
/// What the program shows under "Šta je novo", newest first. The number is 0.Y.Z: Y counts every new thing
/// since the start, Z every polish or fix, and neither ever goes back to zero. A test checks that the first
/// entry matches &lt;Version&gt; in Takt.csproj.
/// </summary>
public static class Izmene
{
    public static readonly Unos[] Spisak = Numerisi(
    [
        new("alfa", "19. 11. 2026.",
        [
            "Zdravlje diskova: čitaju se samo brojke koje odlučuju (preraspoređeni i sektori na čekanju, neispravni, CRC greške, rezervni blokovi, životni vek, sati rada, temperature) i daje se presuda — Zdrav / Upozorenje / Kvar, sa jednom rečenicom šta se dešava. Puna SMART tabela se nikad ne prikazuje.",
            "`Testovi --zdravlje`: ispiše presudu za svaki disk, isto što program pokaže (kao administrator).",
        ],
        [
            "SMART čita usluga ili pomoćnik (prozor kao običan korisnik ne može) i šalje presudu kroz cev; kanal „Diskovi“ dobija red ispod imena, a statusna traka kaže celu rečenicu kad nešto nije u redu.",
            "Disk koji ne odgovori (USB most, nema administratora) prijavljuje „nema podataka o zdravlju“, nikad „zdrav“.",
            "Red zdravlja prati **izabrani** disk u grupi (ranije je grupa uzimala presudu prvog/najgoreg diska, pa je uz ST12000VE pisalo da je NVMe).",
            "SMART se čita na **10 minuta**, ne svakog minuta: upit ume da probudi hard disk koji spava, a četiri diska buđena svakog minuta samo vrte ploče i troše sat rada bez potrebe. Prvi upit posle povezivanja je odmah.",
            "Ispravka koja se meri: LHM je pitao diskove **svake sekunde**, a jedno pitanje traje 1,2–1,6 s (šest diskova) — čitanje je bilo **750 ms** (75 % jezgra). Sada se diskovi pitaju na 10 minuta: čitanje je **15 ms** (1,5 %).",
            "Trake „Aktivnost“, „Aktivnost upisa“ i „Aktivnost čitanja“ su **sklonjene** sa diskova: brojale su se iz istih SMART brojača, pa bi uz pitanje na 10 minuta bile zamrznuta laž. Ostaju temperatura, zauzeće i slobodan prostor.",
            "Podnožje više ne laže o pravima: pisalo je „običan korisnik“ i kad je gore stajalo „Potpuni nadzor · usluga“. Sada zaglavlje i podnožje dolaze iz **istog prekidača** (`ProzorVm.OpisStanja`): „usluga“, „pomoćnik“, „administrator“ ili — samo kad niko ne pomaže — „običan korisnik“.",
            "FPS više ne nestaje kad je **prozor sam povišen**: merenje kadrova je bilo samo u usluzi i pomoćniku, pa je baš najviše prava imao prozor koji igru ne vidi. Sada povišen prozor meri kadrove sam — i samo tada, jer uz uslugu ili pomoćnika postoji jedna ETW sesija, ne dve.",
            "Usluga se Windowsu javi **odmah**, a motor (drajver, diskovi) otvara u pozadini: `OnStart` je blokirao dok se sve ne otvori — mereno **9 s** do „Running“, a sa uspavanim diskovima to bi prešlo 30 s koliko SCM daje i Windows bi rekao da usluga nije pokrenuta.",
            "Reč **„zastoj“** umesto „štucanje“: štucanje je hiccup, a u programskom svetu se meša i sa prazninom u podacima. Zastoj je kadar znatno duži od susednih (preko 50 ms i 2,5 × medijana). Sačuvani izveštaji se i dalje čitaju (stari ključ u JSON-u).",
            "**Jedan „1 % low“**, ne dva: izveštaj sesije je uzimao 5. percentil sekundi („najgorih 5 % sekundi“) i zvao ga 1 % low, pa su ekran i izveštaj davali dva broja za istu igru. Sada je i u izveštaju prosek najgorih **1 %** sekundi, gde je svaka sekunda računata iz kadrova.",
            "Prozor više ne ostaje bez usluge: ako se podigne dok se usluga još diže (posle instalacije, na prijavi), pita cev **svakih 5 s** i sam pređe na potpuni nadzor — ranije je tako ostajao na „Ograničen nadzor“ do zatvaranja i ponovnog otvaranja.",
            "Grafikon više ne stoji u praznom: korak skale je skakao sa 2 na 5, pa se linija od 51,6–58,4 °C crtala u prozoru 50–70 (donja trećina). Sada korak ide 1, 2, **2,5**, 5, 10 — ista linija dobija 50–60. Visina grafikona je ograničena na **300 px**, pa u velikom prozoru tabela senzora dobija prostor umesto praznine.",
            "**Jedna kultura za ceo program**: redovi i osa grafikona forsiraju srpski (52,8), a sve ostalo je pratilo Windows — na ovoj mašini en-GB, pa je izveštaj pisao „52.8 °C“ pored ekrana koji piše „52,8 °C“. Sada se kultura postavlja na startu (prozor, usluga, testovi) i sve interpolacije pišu zapetu.",
        ]),
        new("alfa", "19. 11. 2026.",
        [
        ],
        [
            "Rampa merača: zeleno do 60 % skale, žuto oko 80 %, crveno tek pri vrhu — pola opterećenja više ne izgleda kao alarm. Granice dela i dalje pomeraju žuto na granicu upozorenja.",
            "Red jezgara je opet tačan: LHM-ov „Najopterećenije jezgro“ (i prosek) više ne ulaze u red niti — jezgro se prepoznaje po tome što ime završava brojem, pa pravilo radi i posle prevoda.",
            "Male crtice jezgara idu istom rampom kao merači (zeleno → crveno) umesto ćilibara — jedna slika, jedan jezik boja.",
        ]),
        new("alfa", "19. 11. 2026.",
        [
            "Točkić miša više ne skače: pult i stranica podešavanja klize do novog mesta u oko 0,2 s, a drugi pokret točkića nastavlja odakle je krenuo (`Prikaz/Glatko.cs`).",
            "Merači idu zeleno → žuto → crveno po visini; kada deo prijavi svoje granice, žuto počinje na granici upozorenja, a crveno na kritičnoj, pa hladan deo nikad ne izgleda vruć samo zato što je visoko na skali.",
        ],
        [
            "Nova paleta: hladniji i dublji tonovi (plavo-sivi umesto braon-sivih), svetliji tekst, tiša linija; ćilibar ostaje akcenat, a boju sada nose merači.",
            "Naponi napajanja sa matične ploče (+3,3 V, +5 V, +12 V, −12 V, 3VSB…) više se ne prikazuju: to su grube procene Super I/O čipa, retko u ±10 %, i samo su gušili kanal „Vent.“. Naponi procesora i grafičke karte ostaju — njih daju sami delovi.",
        ]),
        new("alfa", "19. 11. 2026.",
        [
            "Senzor dobija stabilno ime (`Ident`) — putanju iz biblioteke, npr. `/amdcpu/0/temperature/2` — pa istorija preživljava preimenovanje i budući prevod na engleski.",
        ],
        [
            "Stara istorija se pri prvom pokretanju prebacuje sa imena na novi ključ, bez gubljenja i bez dupliranja redova (provereno na kopiji prave baze: 176 senzora i 109.631 uzorak ostali su isti).",
            "Ključ putuje i kroz cev, pa usluga i pomoćnik pišu istoriju pod istim ključem; stariji pomoćnik bez ključa se i dalje prihvata.",
            "Jezgra se prepoznaju po putanji (`/load/N`), ne po imenu — prevod više ne može da ih izgubi iz reda pod procesorom.",
            "`Testovi --prebaci <folder>`: prebacivanje baze na nove ključeve bez pokretanja usluge (za kopiju ili dok usluga ne radi).",
        ]),
        new("alfa", "18. 11. 2026.",
        [
        ],
        [
            "Grafikon najdužeg kadra u Sceni sada raste dok igra traje (prvi put sa pravom igrom brisao se svake sekunde, pa je imao jednu tačku), a ikonica igre se ne čita iznova svake sekunde.",
            "Ispod CPU kanala piše „jezgro 6: 74 %“ umesto „jezgra 6“, i isto u izveštaju sesije.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
        ],
        [
            "Ventilatori i temperature matične ploče se sada vide (npr. 7 ventilatora na ASUS TUF B550): Takt je tražio spisak senzora pre prvog čitanja, a ploča ih prijavi tek posle njega.",
            "Kanal „Vent.“ uzima ventilatore koji se okreću (ne prazne priključke od 0 o/min) i pod imenom piše „Matična ploča“ umesto dugačkog imena čipa.",
            "Neimenovane temperature ploče („Temperatura 3“) se ne boje žuto ni crveno i ne pale lampicu: neprikačen ulaz često pokazuje 80-90 °C.",
            "Naponi ploče na srpskom (napon procesora, baterija CMOS-a, +3 V u mirovanju…).",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Scena (dugme ili F11, Esc izlazi): veliki pult preko celog drugog ekrana, za gledanje krajičkom oka dok igraš; gore igra, dole pet velikih kanala sa po jednim dodatnim redom (najopterećenije jezgro, VRAM, najtopliji disk).",
            "FPS igre: usluga meri kadrove preko Windows-ovih događaja (bez ubacivanja u igru), a Scena pokazuje FPS, 1% low, vreme kadra i grafikon najdužeg kadra svake sekunde, gde se zastoj vidi kao šiljak.",
            "Sesije igara: svaka igra duža od minuta se snima sama i posle nje Takt pravi izveštaj (trajanje, FPS, 1% low, zastoji, najviše temperature, koliko je VRAM bio pun, najopterećenije jezgro) - dugme „Sesije“.",
            "Clip lampice: crvena lampica iznad kanala se pali kad temperatura pređe granicu, naglo skoči, VRAM se napuni, jezgro dugo stoji na 100 % ili stane ventilator, i ostaje upaljena dok je ne klikneš.",
        ],
        [
            "Procesor: „CCD1 (Tdie)“ je sada „Temperatura CCD1“, a „Bus Speed“ „Takt magistrale“.",
            "Prazan kanal uz potpuni nadzor kaže „nema senzora“ (npr. ploča bez ventilatora), umesto da opet traži nadzor.",
            "Ime diska bez razmaka na kraju.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Takt se instalira kao pravi program u C:\\Program Files\\BronxSoft Takt (prozor i usluga zajedno, uvek ista verzija), a prečice na desktopu i u Start meniju otvaraju instalirani Takt, ne razvojnu kopiju; „Instaliraj Takt.bat“ sve napravi i instalira jednim klikom.",
        ],
        [
            "Usluga (TaktSluzba.exe) ima istu ikonicu kao Takt.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
        ],
        [
            "Usluga se instalira u Program Files i radi odande, a ne iz foldera u koji svaki program može da piše - tako niko ne može da joj podmetne svoj kod.",
            "Usluga čuva istoriju i dnevnik u zaključanom folderu C:\\ProgramData\\BronxSoft Takt, a ne više u korisnikovom folderu.",
            "Usluga sluša „Čuvaj istoriju“, broj dana, osvežavanje i „Obriši istoriju“ iz podešavanja, bez ponovnog pokretanja (u roku od pola minuta).",
            "Takt može da radi samo jednom: drugo pokretanje (npr. ikonica na desktopu dok je program u traci) samo otvori prozor prvog.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
        ],
        [
            "„Podešavanja“ iz menija ikonice više ne mogu da otvore program umesto podešavanja: prvi klik ih otvara, a svaki sledeći ih ostavlja otvorena (ranije su se zatvarala i vraćala pult, jer je meni zvao isto dugme koje radi kao preklopnik).",
            "Pult koji se napravi posle otvaranja podešavanja ih više ne pretekne, a posle pokretanja pomoćnika ostaješ gde si bio — u podešavanjima ili u istom kanalu.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
        ],
        [
            "Svako pokretanje u `greske.log` ostavlja jednu liniju istine: „Izvor: Pravi senzori (pomoćnik), 129 senzora, stanje: Potpuni nadzor · pomoćnik, drajver: instaliran.“ — dovoljno da se prijava „nema temperature“ proveri bez nagađanja.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Program zna da li je PawnIO drajver instaliran i to kaže: zaglavlje piše „…bez drajvera“ (žuto), a statusna traka objašnjava da temperatura procesora, potrošnja, napon i ventilatori traže drajver sa pawnio.eu — administrator sam tu ne pomaže.",
            "Podešavanja dobijaju odeljak „DRAJVER (PAWNIO)“ sa stanjem (instaliran / nije instaliran), objašnjenjem i dugmetom koje otvara pawnio.eu (samo na tvoj klik — program ništa ne otvara i ne šalje sam).",
        ],
        [
            "Prazan kanal sada kaže „traži drajver“ kad drajver fali, a „traži nadzor“ kad je stvar u pravima; objašnjenje u kanalu imenuje ono što stvarno nedostaje.",
            "Pomoćnik u `greske.log` upisuje i stanje drajvera („poslato 129 senzora, administrator: True, PawnIO drajver: nije instaliran“) — da se posle ne pita zašto nema temperature.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Ikonica pored sata sa menijem (Otvori, Podešavanja, Izlaz); X skriva program u traku umesto da ga ugasi, a prvi put to i kaže balonom.",
            "Pokretanje sa Windowsom (`Takt.exe --u-traci`, upis u HKCU Run bez administratora) i pamćenje položaja, veličine i maksimizovanog stanja prozora.",
        ],
        [
            "Probni rad i slike ekrana (`--probno`, `--snimak`) ne diraju sačuvani položaj prozora ni korisnikova podešavanja.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Usluga `TaktSluzba`: Windows servis koji čita senzore kao administrator, puni istoriju i kad je prozor zatvoren, i služi prozor preko iste cevi — jedna instalacija, jedan UAC, bez pitanja pred svaku igru.",
            "Instalacija i uklanjanje jednom komandom (`TaktSluzba.exe --instaliraj` / `--ukloni`), bez dodatnog instalatera; `--konzola` radi isto što i usluga, u konzoli.",
        ],
        [
            "Prozor zna ko ga služi: „Potpuni nadzor · usluga“ u zaglavlju, i tada ne piše istoriju drugi put (to radi usluga).",
            "Cev je zajednička za uslugu i pomoćnika, sa pravima koja puštaju običnog korisnika da čita (samo čitanje — Windows odbija upis na jednosmernu cev).",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Dijagnostika `Testovi --meri`: koliko traje jedno čitanje senzora, jedan otkucaj pulta i jedan upis u bazu — broj za poređenje posle svake izmene.",
        ],
        [
            "Tabela u solo panelu dobija obaveštenja na dve sekunde, a merači i grafikon na svaki otkucaj: promena teksta je najskuplja stvar u prozoru.",
            "Grafikon i merači crtaju iz keša (zamrznute četke i olovke, pozadina segmenata jednom) umesto da prave nove svake sekunde.",
            "Prazan kanal sada kaže „traži nadzor“ i u tooltipu objašnjava šta mu treba (ranije je stajalo samo „nadzor“).",
            "LibreHardwareMonitorovi kontroleri su isključeni dok kontrola ventilatora ne bude urađena — njihove HID/serial niti vrte u prazno.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Svi diskovi su jedan kanal na pultu, „Diskovi“, sa po jednim uskim meračem temperature za svaki disk i najtoplijim diskom ispod; u solo panelu se disk bira dugmićima Disk 1, Disk 2…",
        ],
        [
            "Pult se pomera točkićem miša kad ne stane u prozor.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Pult: računar je miksета, a procesor, grafička karta, memorija, svaki disk i ventilatori su kanali, svaki sa dva VU merača (npr. opterećenje i temperatura) i vrhom koji stoji pa polako pada.",
            "Solo panel: izabrani kanal desno, sa grafikonom, stubićima svih niti procesora i tabelom senzora (Sada, Najmanje, Prosek, Najviše).",
            "Tasteri S i M na svakom kanalu: S ga stavlja desno, M ga skuplja u usku traku, a program to pamti.",
            "Novo merenje: zauzeće commit-a (koliko je memorije obećano od RAM-a i pagefile-a zajedno), kao drugi merač RAM kanala.",
            "Zaglavlje sa ritmom čitanja (♩ 1 s), stanjem nadzora i dugmetom za podešavanja, koja se otvaraju na mestu solo panela.",
        ],
        [
            "Ravna dugmad sa malim zaobljenjem umesto ispupčenih pilula.",
            "Poruke i upozorenja tiho u donjoj traci umesto žute trake preko celog prozora.",
            "Manje veličina slova i zaobljenja, bez kartica u karticama: sve deluje mirnije.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Temperatura diska postaje žuta i crvena po granicama koje prijavljuje sam disk, a te granice stoje pod svojim naslovom „Granice koje prijavljuje uređaj“.",
        ],
        [
            "„Kritična granica“ diska (npr. 84 °C) više nije crvena kao da je disk vruć - to je granica, ne merenje.",
            "Senzori diska su na srpskom: Temperatura, Temperatura senzora 1, Rezervni blokovi, Istrošenost, Ukupno pročitano i upisano, Brzina čitanja i upisa, Aktivnost.",
            "Ukupno pročitano i upisano na disk piše se u GB i TB (bilo je pogrešno u MB/s), a brzina čitanja i upisa diska se sada vidi.",
        ]),
        new("alfa", "18. 11. 2026.",
        [
            "Novi izgled: ikonica programa u zaglavlju, ikonice u levoj koloni, oznaka stanja u boji (pravi senzori, potpuni nadzor, probni podaci), izdignuta dugmad i pregledno „Šta je novo“.",
            "Pločice na vrhu pokazuju i ime senzora, a spisak je podeljen po vrstama (Temperature, Takt, Potrošnja…) sa trakom popunjenosti; visoka temperatura postaje žuta, a previsoka crvena.",
            "Grafikon: pod mišem pokazuje vrednost i koliko je pre očitana, ispod piše koliko unazad seže („pre 2 min“), a uz najmanje i najviše stoje prosek i trenutna vrednost.",
            "Senzori koji nikad nisu dali broj (traže drajver) skupljeni su ispod spiska: „Još 30 senzora bez vrednosti“, jednim klikom se vide svi.",
            "Promena osvežavanja (1, 2 ili 5 s) važi odmah, bez ponovnog pokretanja.",
        ],
        [
            "Pločica „Procesor“ bez drajvera pokazuje opterećenje umesto „—“.",
            "Jezgra idu redom 1, 2, 3 … 10, 11, 12, a ne 1, 10, 11, 12, 2.",
            "Memorija od gigabajta naviše piše se u GB (15,3 GB umesto 15698 MB).",
            "Napon i potrošnja procesora bez drajvera su „—“: biblioteka je tada davala izmišljenih 1,550 V.",
            "Probno pokretanje i slike ekrana više ne menjaju sačuvana podešavanja, ni kad se pritisne „Sačuvaj“.",
            "„Obriši istoriju“ prvo pita, briše i kad je čuvanje istorije isključeno i kaže ako nije uspelo.",
            "Pomoćnik dobija do 10 sekundi da se javi, a njegova veza sa prozorom ima izričito upisana prava.",
            "Sporo čitanje senzora više ne pokreće drugo čitanje preko prvog.",
            "Slike ekrana (--snimak) nemaju belu traku na dnu.",
        ]),
        new("alfa", "17. 11. 2026.",
        [
            "Prozor se otvara odmah, a senzori se čitaju u pozadini — program više ne staje na „Not responding“ dok traži drajver, pomoćnika ili bazu.",
            "Grafička karta na srpskom: Temperatura, Hotspot, Zauzeće VRAM, Opterećenje, Takt jezgra i memorije, Potrošnja, Ventilator, Napon — i Zauzeće VRAM u procentima, izračunato iz zauzetog i ukupnog.",
            "Grafikon ima blagu ispunu ispod linije i okrugle ose (30, 40, 50, 60, 70), pa se vrednosti čitaju na prvi pogled.",
        ],
        [
            "Istorija se upisuje van brave koju prozor čeka: upis u bazu više ne može da zamrzne sliku.",
            "Pokretanje pomoćnika ide sa pozadinske niti, pa ni Windowsovo pitanje za dozvolu ne blokira prozor.",
        ]),
        new("alfa", "17. 11. 2026.",
        [
            "Potpuni nadzor: pomoćnik se pokreće kao administrator i čita temperaturu procesora, potrošnju, napon, ventilatore i SMART, a prozor ostaje običan korisnik.",
            "Dugme „Uključi potpuni nadzor“ u traci upozorenja i oznaka „Pravi senzori · pomoćnik“ u zaglavlju.",
        ],
        [
            "Grafikon sam otvara prvi senzor koji ima vrednost, ose uskog raspona dobijaju decimale, a izabrana pilula i kućica za potvrdu su čitljivi na tamnoj pozadini.",
        ]),
        new("alfa", "17. 11. 2026.",
        [
            "Prvi oblik programa: ekrani za procesor, grafiku, memoriju, diskove i hlađenje, sa vrednostima, najmanjim i najvećim i grafikonom poslednjih uzoraka.",
            "Čitanje pravih senzora preko LibreHardwareMonitor-a: temperatura, takt, opterećenje, potrošnja, napon, ventilatori, SMART.",
            "Istorija u SQLite bazi: upis jednom u minuti i brisanje starijeg od izabranog broja dana.",
            "Probni podaci za rad bez administratora i za slike ekrana, i kriva ventilatora sa proverom (još se ne šalje hardveru).",
        ],
        [
        ]),
    ]);

    public static Unos Trenutna => Spisak[0];

    /// <summary>The whole history as plain text, for the settings screen.</summary>
    public static string Tekst => string.Join("\n\n", Spisak.SelectMany(u =>
        new[] { $"{u.Oznaka} · {u.Datum}" }.Concat(u.Izmene.Select(i => "• " + i))));

    private static Unos[] Numerisi(Unos[] unosi)
    {
        int novih = 0, dorada = 0;
        for (int i = unosi.Length - 1; i >= 0; i--)
        {
            novih += unosi[i].Nove.Length;
            dorada += unosi[i].Dorade.Length;
            unosi[i].Broj = $"0.{novih}.{dorada}";
        }
        return unosi;
    }
}

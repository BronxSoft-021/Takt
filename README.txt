BronxSoft Takt 0.38.76 alfa
19. 11. 2026.

NOVO
- Zdravlje diskova: čitaju se samo brojke koje odlučuju (preraspoređeni i sektori na čekanju, neispravni,
  CRC greške, rezervni blokovi, životni vek, sati rada, temperature) i daje se presuda - Zdrav /
  Upozorenje / Kvar, sa jednom rečenicom šta se dešava. Puna SMART tabela se nikad ne prikazuje.
- "Testovi --zdravlje": ispiše presudu za svaki disk, isto što program pokaže (kao administrator).

DORADE
- SMART čita usluga ili pomoćnik (prozor kao običan korisnik ne može) i šalje presudu kroz cev; kanal
  "Diskovi" dobija red ispod imena, a statusna traka kaže celu rečenicu kad nešto nije u redu.
- Disk koji ne odgovori (USB most, nema administratora) prijavljuje "nema podataka o zdravlju", nikad
  "zdrav".

BronxSoft Takt 0.36.54 alfa
19. 11. 2026.

DORADE
- Rampa merača: zeleno do 60 % skale, žuto oko 80 %, crveno tek pri vrhu - pola opterećenja više ne
  izgleda kao alarm. Granice dela i dalje pomeraju žuto na granicu upozorenja.

BronxSoft Takt 0.36.51 alfa
19. 11. 2026.

NOVO
- Točkić miša više ne skače: pult i stranica podešavanja klize do novog mesta u oko 0,2 s, a drugi
  pokret točkića nastavlja odakle je krenuo.
- Merači idu zeleno → žuto → crveno po visini; kada deo prijavi svoje granice, žuto počinje na granici
  upozorenja, a crveno na kritičnoj - hladan deo nikad ne izgleda vruć samo zato što je visoko na skali.

DORADE
- Nova paleta: hladniji i dublji tonovi (plavo-sivi umesto braon-sivih), svetliji tekst, tiša linija;
  ćilibar ostaje akcenat, a boju sada nose merači.
- Naponi napajanja sa matične ploče (+3,3 V, +5 V, +12 V, -12 V, 3VSB...) više se ne prikazuju: to su
  grube procene Super I/O čipa, retko u ±10 %, i samo su gušili kanal "Vent.". Naponi procesora i
  grafičke karte ostaju - njih daju sami delovi.

BronxSoft Takt 0.34.49 alfa
19. 11. 2026.

NOVO
- Senzor ima stabilno ime (`Ident`) — putanju iz biblioteke, npr. "/amdcpu/0/temperature/2" — pa
  istorija preživljava preimenovanje i budući prevod na engleski.

DORADE
- Stara istorija se pri prvom pokretanju prebacuje sa imena na novi ključ, bez gubljenja i bez
  dupliranja redova. Provereno na kopiji prave baze: 176 senzora i 109.631 uzorak ostali su isti.
- Ključ putuje i kroz cev, pa usluga i pomoćnik pišu istoriju pod istim ključem; stariji pomoćnik
  bez ključa se i dalje prihvata.
- Jezgra se prepoznaju po putanji ("/load/N"), ne po imenu - prevod više ne može da ih izgubi.
- "Testovi --prebaci <folder>": prebacivanje baze na nove ključeve bez pokretanja usluge.

BronxSoft Takt 0.33.45 alfa
18. 11. 2026.

Posle prve prave igre.

DORADE
- Grafikon najduzeg kadra u Sceni raste dok igra traje (brisao se svake sekunde), a ikonica igre se
  ne cita iznova svake sekunde.
- Ispod CPU kanala pise "jezgro 6: 74 %" umesto "jezgra 6", isto u izvestaju sesije.


BronxSoft Takt 0.33.43 alfa
18. 11. 2026.

Ventilatori matične ploče.

DORADE
- Ventilatori i temperature maticne ploce se sada vide (npr. 7 ventilatora na ASUS TUF B550): Takt je
  trazio spisak senzora pre prvog citanja, a ploca ih prijavi tek posle njega.
- Kanal "Vent." uzima ventilatore koji se okrecu i pod imenom pise "Maticna ploca".
- Neimenovane temperature ploce se ne boje i ne pale lampicu (neprikacen ulaz cesto pokazuje 80-90 °C).
- Naponi ploce na srpskom.


BronxSoft Takt 0.33.39 alfa
18. 11. 2026.

Za igre: Scena, FPS, sesije i clip lampice.

NOVO
- Scena (dugme ili F11, Esc izlazi): veliki pult preko celog drugog ekrana, za gledanje krajickom oka
  dok igras; gore igra, dole pet velikih kanala sa po jednim dodatnim redom.
- FPS igre: usluga meri kadrove preko Windows-ovih dogadjaja (bez ubacivanja u igru); Scena pokazuje
  FPS, 1% low, vreme kadra i grafikon najduzeg kadra, gde se zastoj vidi kao siljak.
- Sesije igara: svaka igra duza od minuta se snima sama; posle nje izvestaj (trajanje, FPS, 1% low,
  zastoji, temperature, VRAM, najopterecenije jezgro) - dugme "Sesije".
- Clip lampice: crvena lampica iznad kanala se pali kad nesto predje granicu i ostaje do klika.

DORADE
- Prevedeno "CCD1 (Tdie)" i "Bus Speed"; prazan kanal uz potpuni nadzor kaze "nema senzora";
  ime diska bez razmaka na kraju.


BronxSoft Takt 0.29.36 alfa
18. 11. 2026.

Takt kao instaliran program.

NOVO
- Takt se instalira u C:\Program Files\BronxSoft Takt (prozor i usluga zajedno, uvek ista verzija);
  precice na desktopu i u Start meniju otvaraju instalirani Takt. "Instaliraj Takt.bat" sve napravi i
  instalira jednim klikom (Windows pita za dozvolu jednom).

DORADE
- Usluga (TaktSluzba.exe) ima istu ikonicu kao Takt.


BronxSoft Takt 0.28.35 alfa
18. 11. 2026.

Bezbednija usluga i samo jedan Takt (posle pregleda koda).

DORADE
- Usluga se instalira u Program Files i radi odande, a ne iz foldera u koji svaki program može da
  piše - tako niko ne može da joj podmetne svoj kod.
- Usluga čuva istoriju i dnevnik u zaključanom folderu C:\ProgramData\BronxSoft Takt, a ne više u
  korisnikovom folderu.
- Usluga sluša "Čuvaj istoriju", broj dana, osvežavanje i "Obriši istoriju" iz podešavanja, bez
  ponovnog pokretanja (u roku od pola minuta).
- Takt može da radi samo jednom: drugo pokretanje samo otvori prozor prvog.

ZNAJ
- Ko je već instalirao uslugu: pokreni ponovo "TaktSluzba.exe --instaliraj" kao administrator, da se
  usluga preseli u Program Files. Stara istorija ostaje u %APPDATA%\BronxSoft Takt.


BronxSoft Takt 0.28.31 alfa
18. 11. 2026.

DORADE
- "Podešavanja" iz menija ikonice više ne otvaraju program umesto podešavanja: prvi klik ih otvara, a svaki
  sledeći ih ostavlja otvorena. (Ranije su se zatvarala i vraćala pult, jer je meni zvao isto dugme koje
  radi kao preklopnik - dugme u zaglavlju i dalje otvara/zatvara.)
- Pult koji se napravi posle otvaranja podešavanja ih više ne pretekne; posle pokretanja pomoćnika ostaješ
  gde si bio - u podešavanjima ili u istom kanalu.

BronxSoft Takt 0.28.29 alfa
18. 11. 2026.

DORADE
- Svako pokretanje u "greske.log" ostavlja jednu liniju istine: "Izvor: Pravi senzori (pomoćnik), 129
  senzora, stanje: Potpuni nadzor · pomoćnik, drajver: instaliran." Dovoljno da se prijava "nema
  temperature" proveri bez nagađanja.

BronxSoft Takt 0.28.28 alfa
18. 11. 2026.

NOVO
- Program zna da li je PawnIO drajver instaliran i to kaže, umesto da traži administratora koji tu
  ne pomaže: zaglavlje piše "Ograničen nadzor · bez drajvera" (žuto), a statusna traka objašnjava
  da temperatura procesora, potrošnja, napon i ventilatori traže drajver sa pawnio.eu.
- Podešavanja imaju odeljak "DRAJVER (PAWNIO)": stanje (instaliran / nije instaliran), objašnjenje i
  dugme "Otvori pawnio.eu". Program ništa ne otvara i ne šalje sam - samo na tvoj klik.

DORADE
- Prazan kanal kaže "traži drajver" kad drajver fali, a "traži nadzor" kad je stvar u pravima;
  objašnjenje u kanalu imenuje ono što stvarno nedostaje, a dugme nudi ono što pomaže
  (bez drajvera: "Otvori pawnio.eu"; sa drajverom: "Uključi potpuni nadzor").
- Pomoćnik u greske.log upisuje i stanje drajvera, npr.
  "poslato 129 senzora, administrator: True, PawnIO drajver: nije instaliran".

ZAŠTO DRAJVER
- LibreHardwareMonitor od 0.9.5 čita temperaturu procesora, potrošnju, napon, ventilatore i SMART
  diskova samo preko PawnIO drajvera (zamenio je WinRing0, koji blokiraju Defender i anti-cheat).
  Drajver se preuzima sa pawnio.eu i nije naš da se deli uz program, pa ga Takt ne donosi sa sobom.
- Treba PawnIO 2.1.0 ili noviji: stariji drajveri ne rade sa ovom verzijom biblioteke (0.9.6).
  Instalacija traži administratora, jednom; posle nje samo pokreni Takt ponovo.
- Bez njega se, i kao administrator, vide samo opterećenje, grafička karta i ono što Windows sam daje.

BronxSoft Takt 0.26.26 alfa
18. 11. 2026.

NOVO
- Ikonica pored sata: dvoklik otvara prozor, desni klik daje meni (Otvori Takt,
  Podešavanja, Izlaz). X više ne gasi program nego ga sklanja u traku; prvi put
  to i kaže balonom.
- Pokretanje sa Windowsom (Podešavanja -> "Pokreni sa Windowsom, u traci"): upis
  ide u tvoj deo registra, bez administratora. Tada se Takt diže samo kao ikonica
  pored sata - prozor, senzori i pomoćnik se bude na prvi klik, pa pri podizanju
  Windowsa nema pitanja za dozvolu.
- Prozor pamti gde je bio: položaj, veličinu, maksimizovano stanje i ekran. Ako je
  taj ekran u međuvremenu otkačen, prozor se otvara na glavnom, nikad van slike.

DORADE
- Mirno u traci: 16 MB i 0,00 % procesora (mereno 18. 11.); sa otvorenim prozorom
  oko 4,5 % jednog jezgra.
- Probni rad i slike ekrana ne diraju sačuvani položaj prozora ni podešavanja.

KAKO SE PONAŠA
  X            sklanja u traku (program i dalje meri)
  meni -> Izlaz  stvarno gasi program
  --u-traci    pokretanje bez prozora (tako ga diže Windows)

Mereno na razvojnom računaru (Ryzen 5 5600, RX 6600, 63 senzora):
- čitanje senzora: 14,3 ms (najviše AMD grafika), tj. 1,4 % jednog jezgra na 1 s;
- pult (view modeli): 0,05 ms po otkucaju - nije problem;
- upis 63 reda u bazu: 1,9 ms, jednom u minuti;
- program sa otvorenim prozorom: 4,5 % jednog jezgra (oko 0,4 % mašine sa 12 niti).

ZNAJ
- Bez usluge ili pomoćnika se ne vide temperatura procesora, potrošnja, napon i ventilatori
  — to traži drajver. Program tada prikazuje "—", nikad izmišljen broj, i piše šta nedostaje.
- Pomoćnik živi dok je prozor otvoren (i kad je sklonjen u traku); usluga ostaje i puni
  istoriju bez prozora.

BronxSoft Takt 0.24.25 alfa
18. 11. 2026.

NOVO
- Usluga "TaktSluzba": Windows servis koji čita senzore kao administrator, puni istoriju i
  kada je prozor zatvoren, i služi prozor preko iste cevi. Jedna instalacija, jedan UAC,
  bez pitanja pred svaku igru.
- Instalacija i uklanjanje jednom komandom: "TaktSluzba.exe --instaliraj" i "--ukloni";
  "--konzola" radi isto što i usluga, u konzoli (za proveru).

DORADE
- Prozor zna ko ga služi: "Potpuni nadzor · usluga" u zaglavlju, i tada ne piše istoriju
  drugi put - to radi usluga.
- Cev je zajednička za uslugu i pomoćnika, sa pravima koja puštaju običnog korisnika da
  čita (samo čitanje - Windows odbija upis na jednosmernu cev).

KAKO SE USLUGA INSTALIRA (jednom, kao administrator)
  TaktSluzba.exe --instaliraj
  TaktSluzba.exe --ukloni

BronxSoft Takt 0.22.23 alfa
18. 11. 2026.

NOVO
- Dijagnostika "Testovi --meri": koliko traje jedno čitanje senzora, jedan otkucaj pulta i
  jedan upis u bazu. Broj za poređenje posle svake izmene.

DORADE
- Tabela u solo panelu dobija obaveštenja na dve sekunde, a merači i grafikon na svaki
  otkucaj: promena teksta je najskuplja stvar u prozoru.
- Grafikon i merači crtaju iz keša (zamrznute četke i olovke, pozadina segmenata jednom).
- Prazan kanal kaže "traži nadzor" i u tooltipu objašnjava šta mu treba.
- Kontroleri LibreHardwareMonitora su isključeni dok kontrola ventilatora ne bude urađena.

Mereno na razvojnom računaru (Ryzen 5 5600, RX 6600, 63 senzora):
- čitanje senzora: 14,3 ms (najviše AMD grafika), tj. 1,4 % jednog jezgra na 1 s;
- pult (view modeli): 0,05 ms po otkucaju - nije problem;
- upis 63 reda u bazu: 1,9 ms, jednom u minuti;
- program u mirnom radu: 4,5 % jednog jezgra (oko 0,4 % mašine sa 12 niti).
  Ostatak je WPF, ne naš kod; isti broj i kada je prozor minimizovan.

ZNAJ
- Bez pomoćnika se ne vide temperatura procesora, potrošnja, napon i ventilatori — to traži
  drajver. Program tada prikazuje "—", nikad izmišljen broj, i piše šta nedostaje.
- Pomoćnik živi samo dok je prozor otvoren; kada se prozor zatvori, on se sam ugasi.

BronxSoft Takt 0.21.19 alfa
18. 11. 2026.

Diskovi kao jedna grupa na pultu.

NOVO
- Svi diskovi su jedan kanal, "Diskovi", sa po jednim uskim meračem temperature za svaki disk i
  najtoplijim diskom ispod; u solo panelu se disk bira dugmićima Disk 1, Disk 2...

DORADE
- Pult se pomera točkićem miša kad ne stane u prozor.


BronxSoft Takt 0.20.18 alfa
18. 11. 2026.

Pult: novi izgled, računar kao miksета.

NOVO
- Pult: procesor, grafička karta, memorija, svaki disk i ventilatori su kanali, svaki sa dva VU
  merača (npr. opterećenje i temperatura) i vrhom koji stoji pa polako pada.
- Solo panel: izabrani kanal desno, sa grafikonom, stubićima svih niti procesora i tabelom
  senzora (Sada, Najmanje, Prosek, Najviše).
- Tasteri S i M na svakom kanalu: S ga stavlja desno, M ga skuplja u usku traku (pamti se).
- Novo merenje: zauzeće commit-a, kao drugi merač RAM kanala.
- Zaglavlje sa ritmom čitanja (♩ 1 s), stanjem nadzora i dugmetom za podešavanja.

DORADE
- Ravna dugmad sa malim zaobljenjem umesto ispupčenih pilula.
- Poruke i upozorenja tiho u donjoj traci umesto žute trake.
- Manje veličina slova i zaobljenja, bez kartica u karticama.


BronxSoft Takt 0.15.15 alfa
18. 11. 2026.

Diskovi sa pomoćnikom: granice, imena i jedinice.

NOVO
- Temperatura diska postaje žuta i crvena po granicama koje prijavljuje sam disk, a te granice
  stoje pod svojim naslovom "Granice koje prijavljuje uređaj".

DORADE
- "Kritična granica" diska (npr. 84 °C) više nije crvena kao da je disk vruć - to je granica,
  ne merenje.
- Senzori diska su na srpskom: Temperatura, Temperatura senzora 1, Rezervni blokovi, Istrošenost,
  Ukupno pročitano i upisano, Brzina čitanja i upisa, Aktivnost.
- Ukupno pročitano i upisano na disk piše se u GB i TB (bilo je pogrešno u MB/s), a brzina
  čitanja i upisa diska se sada vidi.


BronxSoft Takt 0.14.12 alfa
18. 11. 2026.

Novi izgled i pregled svega što program radi.

NOVO
- Novi izgled: ikonica programa u zaglavlju, ikonice u levoj koloni, oznaka stanja u boji
  (pravi senzori, potpuni nadzor, probni podaci), izdignuta dugmad i pregledno "Šta je novo".
- Pločice na vrhu pokazuju i ime senzora, a spisak je podeljen po vrstama (Temperature, Takt,
  Potrošnja...) sa trakom popunjenosti; visoka temperatura postaje žuta, a previsoka crvena.
- Grafikon: pod mišem pokazuje vrednost i koliko je pre očitana, ispod piše koliko unazad seže
  ("pre 2 min"), a uz najmanje i najviše stoje prosek i trenutna vrednost.
- Senzori koji nikad nisu dali broj (traže drajver) skupljeni su ispod spiska: "Još 30 senzora
  bez vrednosti", jednim klikom se vide svi.
- Promena osvežavanja (1, 2 ili 5 s) važi odmah, bez ponovnog pokretanja.

DORADE
- Pločica "Procesor" bez drajvera pokazuje opterećenje umesto "—".
- Jezgra idu redom 1, 2, 3 ... 10, 11, 12, a ne 1, 10, 11, 12, 2.
- Memorija od gigabajta naviše piše se u GB (15,3 GB umesto 15698 MB).
- Napon i potrošnja procesora bez drajvera su "—": biblioteka je tada davala izmišljenih 1,550 V.
- Probno pokretanje i slike ekrana više ne menjaju sačuvana podešavanja.
- "Obriši istoriju" prvo pita, briše i kad je čuvanje istorije isključeno i kaže ako nije uspelo.
- Pomoćnik dobija do 10 sekundi da se javi, a njegova veza sa prozorom ima izričito upisana prava.
- Sporo čitanje senzora više ne pokreće drugo čitanje preko prvog.
- Slike ekrana (--snimak) nemaju belu traku na dnu.

ZNAJ
- Bez pomoćnika se ne vide temperatura procesora, potrošnja, napon, ventilatori i diskovi — to
  traži drajver. Program tada prikazuje "—", nikad izmišljen broj, i piše šta nedostaje.
- Pomoćnik živi samo dok je prozor otvoren; kada se prozor zatvori, on se sam ugasi.


BronxSoft Takt 0.9.3 alfa
17. 11. 2026.

Prvi oblik programa, sa pomoćnikom za potpuni nadzor.

NOVO
- Ekrani za procesor, grafiku, memoriju, diskove i hlađenje: vrednost, najmanje i najviše od
  pokretanja i grafikon poslednjih uzoraka.
- Čitanje pravih senzora preko LibreHardwareMonitor-a: temperatura, takt, opterećenje, potrošnja,
  napon, ventilatori, SMART.
- Istorija u SQLite bazi: upis jednom u minuti, brisanje starijeg od izabranog broja dana.
- Probni podaci za rad bez administratora i za slike ekrana; kriva ventilatora sa proverom
  (još se ne šalje hardveru).
- Potpuni nadzor: pomoćnik se pokreće kao administrator i čita temperaturu procesora, potrošnju,
  napon, ventilatore i SMART, a prozor ostaje običan korisnik.
- Dugme "Uključi potpuni nadzor" u traci upozorenja i oznaka "Pravi senzori · pomoćnik" u zaglavlju.
- Prozor se otvara odmah (oko pola sekunde), a senzori se čitaju u pozadini - nema više
  "Not responding" dok se traži drajver, pomoćnik ili baza.
- Grafička karta na srpskom: Temperatura, Hotspot, Zauzeće VRAM, Opterećenje, Takt jezgra i
  memorije, Potrošnja, Ventilator, Napon - i Zauzeće VRAM u procentima.
- Grafikon ima blagu ispunu ispod linije i okrugle ose (30, 40, 50, 60, 70).

DORADE
- Upis istorije je van brave koju prozor čeka, pa upis u bazu ne može da zamrzne sliku.
- Pokretanje pomoćnika ide sa pozadinske niti: ni Windowsovo pitanje za dozvolu ne blokira prozor.
- Grafikon sam otvara prvi senzor koji ima vrednost, ose uskog raspona dobijaju decimale,
  izabrana pilula i kućica za potvrdu su čitljivi na tamnoj pozadini.

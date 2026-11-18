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

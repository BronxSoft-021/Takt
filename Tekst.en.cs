namespace Takt;

/// <summary>
/// The English side of <see cref="Tekst.T"/>: the Serbian text as written in the code, and what it says in English.
/// A text that is missing here stays Serbian on screen - a visible gap, never a blank label.
/// </summary>
public static class TekstEngleski
{
    public static string? Nadji(string srpski) => Recnik.GetValueOrDefault(srpski);

    private static readonly Dictionary<string, string> Recnik = new(StringComparer.Ordinal)
    {
        // ---- Groups: titles and descriptions (GrupaOpis) ----
        ["Procesor"] = "Processor",
        ["Grafička karta"] = "Graphics card",
        ["Memorija"] = "Memory",
        ["Diskovi"] = "Drives",
        ["Hlađenje"] = "Cooling",
        ["Temperatura po jezgrima, opterećenje, takt, potrošnja i napon."] =
            "Temperature per core, load, clock, power and voltage.",
        ["Temperatura i hotspot, VRAM, opterećenje, takt jezgra i memorije, potrošnja i ventilatori."] =
            "Temperature and hotspot, VRAM, load, core and memory clock, power and fans.",
        ["Zauzeće, slobodan RAM, brzina memorije, commit i pagefile."] =
            "Usage, free RAM, memory speed, commit and pagefile.",
        ["Temperature SSD-a, zdravlje, SMART parametri i životni vek."] =
            "Drive temperatures, health, SMART attributes and lifetime.",
        ["Brzina ventilatora i temperature matične ploče. Brojane temperature („Temperatura 3“) su ulazi čipa za koje se ne zna šta mere — ne pale lampicu, a onaj koji stoji na istoj visokoj vrednosti se i ne prikazuje."] =
            "Fan speeds and motherboard temperatures. The numbered ones (\"Temperature 3\") are chip inputs nobody " +
            "can name - they never light the clip light, and one that stands still at a high value is not shown at all.",

        // ---- A group with no sensors: why, and what would bring them ----
        ["Temperatura, takt, potrošnja i napon procesora idu preko PawnIO drajvera, a on se instalira posebno (pawnio.eu). Bez njega se vidi samo opterećenje - i kao administrator."] =
            "Processor temperature, clock, power and voltage need the PawnIO driver, which is installed separately " +
            "(pawnio.eu). Without it only the load is visible - and only as administrator.",
        ["Temperatura, zdravlje i SMART diskova traže PawnIO drajver (pawnio.eu); administratorska prava sama nisu dovoljna."] =
            "Drive temperature, health and SMART need the PawnIO driver (pawnio.eu); administrator rights alone are " +
            "not enough.",
        ["Ventilatori kućišta i procesora i temperature matične ploče traže PawnIO drajver (pawnio.eu). Bez njega ni administrator ne dobija te senzore."] =
            "Case and processor fans and motherboard temperatures need the PawnIO driver (pawnio.eu). Without it even " +
            "an administrator gets none of those sensors.",
        ["Ovaj deo traži PawnIO drajver (pawnio.eu); bez njega ni administrator ne dobija te senzore."] =
            "This part needs the PawnIO driver (pawnio.eu); without it even an administrator gets none of those sensors.",
        ["Temperatura, zdravlje i SMART diskova se čitaju preko drajvera, a on traži administratora. Uključi potpuni nadzor i diskovi će se pojaviti ovde."] =
            "Drive temperature, health and SMART are read through the driver, and it needs administrator. Turn on full " +
            "monitoring and the drives will appear here.",
        ["Ventilatore kućišta i procesora i temperature matične ploče čita Super I/O čip, a do njega se stiže samo preko drajvera. Uključi potpuni nadzor."] =
            "Case and processor fans and motherboard temperatures are read by the Super I/O chip, and only the driver " +
            "reaches it. Turn on full monitoring.",
        ["Ovaj deo računara ne daje nijedan senzor bez drajvera. Uključi potpuni nadzor."] =
            "This part of the computer gives no sensors without the driver. Turn on full monitoring.",
        ["Matična ploča ovog računara ne daje ventilatore ni svoje temperature ni uz potpuni nadzor (čip za ventilatore nije prepoznat ili nijedan ventilator nije priključen na ploču). Ventilator grafičke karte je na kanalu GPU."] =
            "This board gives no fans and no own temperatures even with full monitoring (the fan chip is not " +
            "recognised, or no fan is connected to the board). The graphics card's fan is on the GPU channel.",
        ["Nijedan disk ne daje temperaturu ni SMART (npr. diskovi preko nekih USB kućišta ili RAID-a)."] =
            "No drive gives a temperature or SMART (for example drives behind some USB boxes or RAID).",
        ["Ovaj deo računara ne daje senzore ni uz potpuni nadzor."] =
            "This part of the computer gives no sensors even with full monitoring.",

        // ---- The console (KanalVm) ----
        ["najtopliji"] = "hottest",
        ["traži drajver"] = "needs driver",
        ["traži nadzor"] = "needs monitoring",
        ["nema termometra"] = "no thermometer",
        ["nema senzora"] = "no sensors",
        ["nema diskova"] = "no drives",
        ["Matična ploča"] = "Motherboard",
        ["Vent."] = "Fans",

        // ---- Rights and state (Admin, ProzorVm.OpisStanja) ----
        ["Potpuni nadzor"] = "Full monitoring",
        ["Ograničen nadzor"] = "Limited monitoring",
        ["administrator"] = "administrator",
        ["običan korisnik"] = "standard user",
        ["Probni podaci"] = "Sample data",
        ["Nema podataka"] = "No data",
        ["Učitavam senzore…"] = "Loading sensors…",
        ["usluga"] = "service",
        ["pomoćnik"] = "helper",
        ["bez drajvera"] = "no driver",

        // ---- A game session (IgraVm) ----
        ["bez zastoja u poslednjih 60 s"] = "no stalls in the last 60 s",

        // ---- Settings screen ----
        ["Podešavanja"] = "Settings",
        ["Program ništa ne šalje na internet. Sve što izmeri ostaje u folderu podataka na ovom računaru."] =
            "The program sends nothing to the internet. Everything it measures stays in the data folder on this computer.",
        ["OSVEŽAVANJE"] = "REFRESH",
        ["sekundi između dva čitanja"] = "seconds between two readings",
        ["Važi odmah. Ređe čitanje znači i manje posla za procesor."] =
            "Takes effect at once. A slower reading is also less work for the processor.",
        ["ISTORIJA"] = "HISTORY",
        ["Čuvaj istoriju u bazi (jedan red u minuti)"] = "Keep history in a database (one row per minute)",
        ["dana se čuva, starije se briše"] = "days kept, older is deleted",
        ["PODACI"] = "DATA",
        ["Probni podaci (izmišljena merenja, za pregled i slike ekrana)"] =
            "Sample data (made-up readings, for a look and for screenshots)",
        ["Istorija i probni podaci važe posle ponovnog pokretanja programa."] =
            "History and sample data take effect after the program is restarted.",
        ["JEZIK"] = "LANGUAGE",
        ["Srpski"] = "Serbian",
        ["Engleski"] = "English",
        ["Jezik važi posle ponovnog pokretanja programa."] = "The language takes effect after the program is restarted.",
        ["PROZOR I TRAKA"] = "WINDOW AND TRAY",
        ["Zatvori u traku umesto gašenja (X)"] = "Close to the tray instead of quitting (X)",
        ["Pokreni sa Windowsom, u traci (bez prozora)"] = "Start with Windows, in the tray (no window)",
        ["Ikonica pored sata vraća prozor; bez nje se sakriveni prozor ne može vratiti. Ako traka ne uspe, X gasi program."] =
            "The icon by the clock brings the window back; without it a hidden window cannot be restored. If the tray " +
            "fails, X quits the program.",
        ["DRAJVER (PAWNIO)"] = "DRIVER (PAWNIO)",
        ["O PROGRAMU"] = "ABOUT",
        ["Verzija"] = "Version",
        ["Prava"] = "Rights",
        ["Podaci"] = "Data",
        ["ŠTA JE NOVO"] = "WHAT IS NEW",
        ["Obriši istoriju"] = "Delete history",
        ["Da, obriši sve"] = "Yes, delete all",
        ["Sakrij senzore bez vrednosti"] = "Hide sensors without a value",
        ["Senzori koje biblioteka navodi, ali na ovom računaru još nisu dali broj - obično traže drajver ili potpuni nadzor."] =
            "Sensors the library lists that have not given a number on this computer yet - they usually need the " +
            "driver or full monitoring.",

        // ---- The tray ----
        ["Prikaži Takt"] = "Show Takt",
        ["Podešavanja…"] = "Settings…",
        ["Izlaz"] = "Quit",
        // ---- Sensor names: processor ----
        ["Opterećenje procesora"] = "CPU load",
        ["Najopterećenije jezgro"] = "Busiest core",
        ["Temperatura procesora"] = "CPU temperature",
        ["Napon procesora"] = "CPU voltage",
        ["Napon SoC-a"] = "SoC voltage",
        ["Prosečan takt"] = "Average clock",
        ["Prosečan efektivni takt"] = "Average effective clock",
        ["Takt magistrale"] = "Bus clock",
        ["Potrošnja paketa"] = "Package power",
        ["Temperatura paketa"] = "Package temperature",
        ["Opterećenje jezgra {0}"] = "Core {0} load",
        ["Takt jezgra {0}"] = "Core {0} clock",
        ["Takt jezgra {0} (efektivni)"] = "Core {0} clock (effective)",
        ["Napon jezgra {0} (VID)"] = "Core {0} voltage (VID)",
        ["Potrošnja jezgra {0}"] = "Core {0} power",
        ["Temperatura jezgra {0}"] = "Core {0} temperature",

        // ---- Sensor names: graphics card ----
        ["Temperatura"] = "Temperature",
        ["Opterećenje"] = "Load",
        ["Takt jezgra"] = "Core clock",
        ["Napon jezgra"] = "Core voltage",
        ["Temperatura VRAM"] = "VRAM temperature",
        ["Takt memorije"] = "Memory clock",
        ["VRAM zauzeto"] = "VRAM used",
        ["VRAM ukupno"] = "VRAM total",
        ["Ventilator"] = "Fan",
        ["Potrošnja"] = "Power",
        ["Zauzeće VRAM"] = "VRAM usage",

        // ---- Sensor names: drives ----
        ["Rezervni blokovi"] = "Spare blocks",
        ["Zauzeće"] = "Usage",
        ["Slobodan prostor"] = "Free space",
        ["Ukupan prostor"] = "Total space",
        ["Životni vek"] = "Lifetime",
        ["Istrošenost"] = "Wear",
        ["Ukupno pročitano"] = "Total read",
        ["Ukupno upisano"] = "Total written",
        ["Aktivnost čitanja"] = "Read activity",
        ["Aktivnost upisa"] = "Write activity",
        ["Aktivnost"] = "Activity",
        ["Brzina čitanja"] = "Read rate",
        ["Brzina upisa"] = "Write rate",
        ["Radni sati"] = "Power-on hours",
        ["Broj uključivanja"] = "Power-on count",
        ["Temperatura senzora {0}"] = "Sensor {0} temperature",

        // ---- Sensor names: board ----
        ["Ventilator {0}"] = "Fan {0}",
        ["Temperatura {0}"] = "Temperature {0}",
        ["Napon {0}"] = "Voltage {0}",
        ["Pumpa {0}"] = "Pump {0}",
        ["Napon procesora (ploča)"] = "CPU voltage (board)",
        ["Baterija CMOS-a"] = "CMOS battery",
        ["+3 V u mirovanju"] = "+3 V standby",
        ["+3,3 V"] = "+3.3 V",
        ["Terminacija procesora"] = "CPU termination",

        // ---- Sensor names: memory (Windows) ----
        ["Zauzeto"] = "Used",
        ["Slobodno"] = "Free",
        ["Ukupno"] = "Total",
        ["Pagefile (procena)"] = "Pagefile (estimate)",
        ["Brzina memorije"] = "Memory speed",
        ["Zauzeće commit-a"] = "Commit usage",

        ["DISKOVI"] = "DRIVES",
        ["Merač na traci diskova pokazuje:"] = "The drive strip measures:",
        ["Merač na traci diskova važi posle ponovnog pokretanja programa."] =
            "The drive strip measure takes effect after the program is restarted.",

        // ---- Table captions (SenzorVm.Vrsta) ----
        ["Zdravlje i SMART"] = "Health and SMART",
        ["Prostor (particija)"] = "Space (volume)",
        ["Opterećenje i zauzeće"] = "Load and usage",
        ["Takt"] = "Clock",
        ["Ventilatori"] = "Fans",
        ["Napon"] = "Voltage",
        ["Struja"] = "Current",
        ["Količina"] = "Amount",
        ["Protok"] = "Throughput",
        ["Ostalo"] = "Other",
        ["Granice koje prijavljuje uređaj"] = "Limits reported by the device",

        // ---- Counts and labels (KanalVm, ProzorVm, IgraVm) ----
        ["{0} disk"] = "{0} drive",
        ["{0} diska"] = "{0} drives",
        ["{0} diskova"] = "{0} drives",

        ["jezgro {0}"] = "core {0}",
        ["Jezgro"] = "Core",
        ["Jezgra ({0} niti)"] = "Cores ({0} threads)",
        ["{0} senzor"] = "{0} sensor",
        ["{0} senzora"] = "{0} sensors",
        ["bez zastoja u poslednjih 60 s"] = "no stalls in the last 60 s",
        ["{0} zastoj u poslednjih 60 s"] = "{0} stall in the last 60 s",
        ["{0} zastoja u poslednjih 60 s"] = "{0} stalls in the last 60 s",

        // ---- A game session (SnimacSesije) ----
        ["snima sesiju {0}"] = "recording session {0}",
        ["Trajanje: {0}, od {1}"] = "Duration: {0}, from {1}",
        ["Prosečno {0} FPS, 1% low {1} FPS"] = "Average {0} FPS, 1% low {1} FPS",
        ["Bez zastoja"] = "No stalls",
        ["Zastoji: {0}"] = "Stalls: {0}",
        ["Procesor najtopliji {0} °C u {1}"] = "CPU hottest {0} °C at {1}",
        ["Grafička najtoplija {0} °C u {1}"] = "GPU hottest {0} °C at {1}",
        ["VRAM pun (95 % i više): {0} - tada igra najčešće secka"] = "VRAM full (95 % or more): {0} - that is when games stutter most",
        ["VRAM nijednom pun"] = "VRAM never full",
        ["Najopterećenije: {0} do {1} %"] = "Busiest: {0} up to {1} %",
        [", na 100 % ukupno {0} (usko grlo)"] = ", at 100 % for {0} (bottleneck)",
        // ---- Table and scene labels ----
        ["Senzor"] = "Sensor",
        ["Sada"] = "Now",
        ["Najmanje"] = "Min",
        ["Prosek"] = "Avg",
        ["Najviše"] = "Max",
        ["Sesije"] = "Sessions",
        ["Izađi iz Scene  (Esc)"] = "Leave the Scene  (Esc)",
        ["Najduži kadar svake sekunde (ms) — zastoj je šiljak"] = "Longest frame of each second (ms) — a stall is the spike",
        ["Računar"] = "This computer",
        ["Scene  F1"] = "Scene  F1",

        // ---- Chart labels ----
        ["pre {0} s"] = "{0} s ago",
        ["pre {0} min"] = "{0} min ago",
        ["pre {0} h"] = "{0} h ago",
        ["pre {0} h {1} min"] = "{0} h {1} min ago",
        ["sada"] = "now",

        // ---- Warnings and messages (ProzorVm) ----
        ["Adresa nije otvorena. Ukucaj je u pregledač: {0}"] = "The address was not opened. Type it in a browser: {0}",
        ["Program ne može da čita senzore: "] = "The program cannot read the sensors: ",
        ["Tražim administratorska prava…"] = "Asking for administrator rights…",
        ["Pomoćnik se nije javio. Razlog je u greske.log; program nastavlja bez njega."] =
            "The helper did not answer. The reason is in greske.log; the program continues without it.",
        ["Prikazani su probni podaci, ne stvarna merenja. U podešavanjima isključi „Probni podaci“."] =
            "Sample data are shown, not real readings. Turn off \"Sample data\" in the settings.",
        [" Uključi potpuni nadzor ili probne podatke u podešavanjima."] =
            " Turn on full monitoring or sample data in the settings.",
        ["Nedostaje PawnIO drajver, pa nema temperature procesora, potrošnje, napona ni ventilatora. "] =
            "The PawnIO driver is missing, so there is no CPU temperature, power, voltage or fans. ",
        [" i pokreni Takt ponovo."] = " and start Takt again.",
        [" i uključi potpuni nadzor."] = " and turn on full monitoring.",
        ["Temperatura procesora, potrošnja, ventilatori i diskovi traže potpuni nadzor (administrator)."] =
            "CPU temperature, power, fans and drives need full monitoring (administrator).",
        ["Istorija je obrisana. Usluga briše i svoju u roku od pola minuta."] =
            "The history is deleted. The service deletes its own within half a minute.",

        // ---- Settings messages ----
        ["Windows nije prihvatio upis; pokušaj ponovo ili proveri zaštitu."] =
            "Windows did not accept the change; try again or check the protection settings.",
        ["Takt će se pokretati sa Windowsom, u traci."] = "Takt will start with Windows, in the tray.",
        ["Takt se više ne pokreće sa Windowsom."] = "Takt no longer starts with Windows.",
        ["Preko njega se čitaju temperatura procesora, potrošnja, napon, ventilatori i SMART diskova."] =
            "It is what reads the CPU temperature, power, voltage, fans and drive SMART.",
        ["Bez njega se, i kao administrator, vide samo opterećenje, grafička karta i ono što Windows sam daje. "] =
            "Without it, even as administrator, only the load, the graphics card and what Windows gives by itself are visible. ",
        ["Preuzmi ga sa pawnio.eu i pokreni Takt ponovo; program ga ne donosi sa sobom jer nije naš da se deli."] =
            "Download it from pawnio.eu and start Takt again; the program does not carry it, as it is not ours to share.",
        ["Brišu se sva sačuvana merenja iz baze, i to ne može da se vrati. Klikni još jednom za potvrdu."] =
            "Every saved reading is deleted from the database, and that cannot be undone. Click once more to confirm.",
        ["Sačuvano. Sve važi odmah osim probnih podataka — oni traže ponovno pokretanje programa. Usluga, ako je instalirana, svoje izmene prihvati u roku od pola minuta."] =
            "Saved. Everything applies at once except sample data - they need the program restarted. The service, if installed, takes its own changes within half a minute.",
        ["Instaliran"] = "Installed",
        ["Nije instaliran"] = "Not installed",

        // ---- Hidden sensors (EkranVm) ----
        ["Još {0} senzor bez vrednosti"] = "{0} sensor without a value",
        ["Još {0} senzora bez vrednosti"] = "{0} sensors without a value",

        // ---- The helper's answers (Pomocnik) ----
        ["Pomoćnik ne može da se pokrene: putanja programa nije poznata."] =
            "The helper cannot start: the program path is unknown.",
        ["Pomoćnik se pokreće. Ako Windows pita za dozvolu, pritisni „Da“."] =
            "The helper is starting. If Windows asks for permission, press \"Yes\".",
        ["Pokretanje je otkazano — program ostaje na čitanju bez administratora."] =
            "The start was cancelled - the program stays on reading without administrator.",
        ["Pomoćnik nije pokrenut: "] = "The helper was not started: ",

        // ---- Disk verdicts (Zdravlje) ----
        ["Zdrav"] = "Healthy",
        ["Kvar"] = "Failing",
        ["SMART se čita kao administrator (usluga ili pomoćnik)."] = "SMART is read as administrator (service or helper).",
        ["Kvar: sektori čekaju popravku"] = "Failing: sectors waiting for repair",
        ["{0} sektora se više ne čita ispravno i čeka zamenu - sačuvaj važne fajlove odmah."] =
            "{0} sectors can no longer be read and are waiting to be replaced - save important files now.",
        ["{0} neispravnih sektora - podaci su već izgubljeni na tim mestima; menjaj disk."] =
            "{0} bad sectors - data is already lost there; replace the drive.",
        ["Kvar: greške u podacima"] = "Failing: data errors",
        ["{0} grešaka u integritetu podataka (NVMe). Sačuvaj važno i planiraj zamenu."] =
            "{0} data integrity errors (NVMe). Save what matters and plan a replacement.",
        ["Kvar: drajv prijavljuje kritično stanje"] = "Failing: the drive reports a critical state",
        ["NVMe kritično upozorenje (maska {0}) - proveri Samsung Magician / proizvođački alat."] =
            "NVMe critical warning (mask {0}) - check Samsung Magician or the vendor's tool.",
        ["Kvar: životni vek potrošen"] = "Failing: lifetime used up",
        ["Ostalo je {0} % upisanih podataka predviđenih za ovaj disk - zameni ga."] =
            "{0} % of the writes this drive was made for are left - replace it.",
        ["Kvar: pregrijan"] = "Failing: overheated",
        ["{0} °C je na kritičnoj granici ({1} °C)."] = "{0} °C is at the critical limit ({1} °C).",
        ["Upozorenje: preraspoređeni sektori"] = "Warning: reallocated sectors",
        ["Upozorenje: greške u prenosu"] = "Warning: transfer errors",
        ["{0} CRC grešaka - obično loš SATA kabl ili konektor, ne sam disk."] =
            "{0} CRC errors - usually a bad SATA cable or connector, not the drive itself.",

        // ---- Sessions screen ----
        ["Sesije igara"] = "Game sessions",
        ["Takt sam snima svaku igru duže od minuta (kad usluga meri kadrove) i posle nje pravi izveštaj."] =
            "Takt records every game longer than a minute by itself (when the service counts frames) and writes a report afterwards.",
        ["Još nema nijedne sesije. Pokreni igru: posle deset sekundi Takt počinje da snima, a kad je ugasiš, izveštaj se pojavi ovde."] =
            "No sessions yet. Start a game: after ten seconds Takt begins recording, and when you close it the report appears here.",

        // ---- Tool tips (the console and the header) ----
        ["Solo: ovaj kanal desno"] = "Solo: this channel on the right",
        ["Utišaj: skupi kanal (pamti se)"] = "Mute: fold this channel (remembered)",
        ["Vrati kanal"] = "Restore the channel",
        ["Ritam čitanja senzora; menja se u podešavanjima"] = "Sensor reading rate; changed in the settings",
        ["Pokreće pomoćnika kao administratora (Windows pita za dozvolu). Prozor ostaje običan korisnik."] =
            "Starts the helper as administrator (Windows asks for permission). The window stays a standard user.",
        ["Izveštaji o odigranim sesijama: trajanje, FPS, zastoji, temperature"] =
            "Reports of played sessions: duration, FPS, stalls, temperatures",
        ["Veliki pult preko celog ekrana, za drugi monitor dok igraš (Esc izlazi)"] =
            "A large console across the whole screen, for a second monitor while you play (Esc leaves)",        ["Kvar: neispravni sektori"] = "Failing: bad sectors",
        ["Upozorenje: životni vek se troši"] = "Warning: lifetime running out",
        ["Upozorenje: disk je istrošen"] = "Warning: the drive is worn",
        ["Upozorenje: malo rezervnih blokova"] = "Warning: few spare blocks",
        ["Upozorenje: topao disk"] = "Warning: the drive runs hot",
        ["Nema podataka o zdravlju"] = "No health data",
        ["{0} sektora je disk zamenio rezervnim. Nije kvar, ali prati da broj ne raste."] =
            "{0} sectors were replaced by spares. Not a failure, but watch that the number does not grow.",
        ["Ostalo je {0} % - vreme je da se pravi rezervna kopija i gleda novi disk."] =
            "{0} % is left - time for a backup and a look at a new drive.",
        ["Istrošenost {0} % - blizu je kraju predviđenog života."] =
            "{0} % worn - close to the end of its design life.",
        ["Rezervnih blokova {0} (granica {1}) - disk počinje da troši zamenu."] =
            "{0} spare blocks (limit {1}) - the drive is starting to use them up.",
        ["{0} °C je na granici upozorenja ({1} °C)."] = "{0} °C is at the warning limit ({1} °C).",
        ["životni vek {0} %"] = "lifetime {0} %",
        ["istrošenost {0} %"] = "wear {0} %",
        ["{0} h rada"] = "{0} h on",        ["Otvori Takt"] = "Open Takt",
        ["{0}: nedostaje PawnIO drajver ({1}). Bez njega ni administrator ne vidi temperaturu, potrošnju ni napon. Klikni za objašnjenje."] =
            "{0}: the PawnIO driver is missing ({1}). Without it even an administrator sees no temperature, power or voltage. Click for the explanation.",
        ["{0}: ovaj deo ne daje senzore ni uz potpuni nadzor. Klikni za objašnjenje."] =
            "{0}: this part gives no sensors even with full monitoring. Click for the explanation.",
        ["{0}: ovaj deo traži potpuni nadzor (administrator i drajver). Klikni za objašnjenje."] =
            "{0}: this part needs full monitoring (administrator and driver). Click for the explanation.",    };
}

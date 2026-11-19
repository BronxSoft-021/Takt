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
            "A large console across the whole screen, for a second monitor while you play (Esc leaves)",    };
}

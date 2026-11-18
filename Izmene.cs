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

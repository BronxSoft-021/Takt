using Takt.Merenje;
using Takt.Sistem;
using Takt.Vm;

namespace Takt.Testovi;

/// <summary>Frames, the game on screen, the session report and the clip light.</summary>
public static class IgreTestovi
{
    /// <summary>
    /// Ten seconds at 60 FPS with nine 80 ms frames, one of them in the last second: 60 FPS, one stall now, and
    /// a 1 % low far below the average (nine of ~590 frames is more than 1 %). One slow frame alone would not move
    /// the 1 % low - that is the definition, not a fault.
    /// </summary>
    public static Task Kadrovi()
    {
        var merac = new MeracKadrova();
        int[] spori = [100, 150, 200, 250, 300, 350, 400, 450, 590];
        double t = 0;
        for (int i = 0; i < 600; i++)
        {
            t += spori.Contains(i) ? 80 : 1000.0 / 60;
            merac.Zabelezi(1234, t);
        }

        var igra = merac.Stanje(sada: t).Single();
        Proveri.Da(igra.Fps is >= 56 and <= 61, $"oko 60 kadrova u poslednjoj sekundi (dobijeno {igra.Fps})");
        Proveri.Blizu(80, igra.NajviseMs, 0.5, "najduži kadar je onaj od 80 ms");
        Proveri.Jednako(1, igra.Zastoji, "jedan kadar od 80 ms u poslednjoj sekundi je zastoj");
        Proveri.Blizu(12.5, igra.Low1, 0.2, "1% low je 1000 / 80 ms kad je više od 1 % kadrova sporo");

        // Events arrive about a second late, so a program counts as drawing for 2,5 s after its last frame.
        Proveri.Jednako(1, merac.Stanje(sada: t + 1500).Count, "kadrovi kasne, pa program ostaje još malo");
        Proveri.Jednako(0, merac.Stanje(sada: t + 3000).Count, "program koji ne crta 2,5 s nije više na spisku");
        return Task.CompletedTask;
    }

    /// <summary>The game is the program in front that draws; Takt itself in front does not end it; a browser is not a game.</summary>
    public static Task Birac()
    {
        var birac = new BiracIgre();
        var igra = new Protokol.Igra(10, "Hellblade2-Win64-Shipping", null, 58, 17, 25, 41, 0);
        var takt = new Protokol.Igra(20, "Takt", null, 2, 500, 500, 0, 0);
        var pregledac = new Protokol.Igra(30, "chrome", null, 60, 16, 17, 55, 0);

        Proveri.Jednako(10, birac.Izaberi([igra, takt], 10, _ => false)?.Pid, "igra u prvom planu");
        Proveri.Jednako(10, birac.Izaberi([igra, takt], 20, _ => false)?.Pid, "klik na Takt (drugi ekran) ne prekida igru");
        Proveri.Jednako(null, new BiracIgre().Izaberi([pregledac], 30, _ => false)?.Pid, "pregledač u prozoru nije igra");
        Proveri.Jednako(30, new BiracIgre().Izaberi([pregledac], 30, _ => true)?.Pid, "pregledač preko celog ekrana jeste");
        Proveri.Jednako(null, birac.Izaberi([takt], 20, _ => false)?.Pid, "kad igra prestane da crta, nema igre");

        Proveri.Jednako("Hellblade2", IgraVm.LepoIme("Hellblade2-Win64-Shipping"), "ime bez repa endžina");

        // First real game (18. 11. 2026): the shortened name was compared with the raw one, so every reading looked
        // like a new game and the frame chart held one point. Five readings of one game must leave five points.
        var vm = new IgraVm();
        for (int i = 0; i < 5; i++) vm.Postavi(igra with { NajviseMs = 20 + i });
        Proveri.Jednako(5, vm.Frametime.Broj, "grafikon kadrova raste dok igra traje");
        Proveri.Da(vm.FrametimeTekst.StartsWith("prosek"), "vreme kadra piše da je prosek, jer grafikon crta najduži");

        Proveri.Jednako("jezgro 6", KanalVm.ImeJezgra("Opterećenje jezgra 6"), "natpis jezgra u nominativu");
        return Task.CompletedTask;
    }

    /// <summary>Ten seconds to start, the game plays, stops: after fifteen seconds the report is saved.</summary>
    public static Task Sesija()
    {
        var folder = Spisak.NoviFajl("sesije");
        var snimac = new SnimacSesije(folder);
        var igra = new IgraVm();
        IzvestajSesije? zavrsena = null;
        snimac.Zavrsena += i => zavrsena = i;

        var t = new DateTime(2026, 11, 18, 21, 0, 0);
        for (int s = 0; s < 120; s++)
        {
            igra.Postavi(new Protokol.Igra(5, "Dead Space", null, 60, 16.7, s == 50 ? 90 : 18, 48, s == 50 ? 1 : 0));
            snimac.Uzmi(igra, [], t.AddSeconds(s));
        }

        Proveri.Da(snimac.Snima, "sesija se snima dok igra radi");
        Proveri.Da(snimac.Snima, "sesija se snima");
        Proveri.Da(snimac.Tekst.StartsWith("snima sesiju"), "Scena kaže da se snima");

        for (int s = 120; s < 140; s++)
        {
            igra.Postavi(null);
            snimac.Uzmi(igra, [], t.AddSeconds(s));
        }

        Proveri.Da(!snimac.Snima, "sesija se završava 15 s posle igre");
        Proveri.Jednako("", snimac.Tekst, "kad se ne snima, ni tekst ne kaže da snima (jedan gospodar)");
        Proveri.Da(zavrsena is not null, "izveštaj je napravljen");
        Proveri.Jednako("Dead Space", zavrsena!.Igra, "ime igre");
        Proveri.Blizu(60, zavrsena.ProsekFps, 0.01, "prosečan FPS");
        Proveri.Blizu(48, zavrsena.Low1, 0.01, "1% low sesije je prosek najgorih 1 % sekundi");
        Proveri.Jednako(1, zavrsena.Zastoji, "jedan zastoj");
        Proveri.Da(zavrsena.TrajanjeSekundi is >= 100 and <= 120, $"trajanje bez prvih 10 s čekanja ({zavrsena.TrajanjeSekundi})");

        var sacuvane = snimac.Ucitaj();
        Proveri.Jednako(1, sacuvane.Count, "izveštaj je sačuvan i čita se");
        Proveri.Jednako("Dead Space", sacuvane[0].Igra, "isti izveštaj");

        // A game shorter than a minute is not a session.
        var kratak = new SnimacSesije(Spisak.NoviFajl("sesije-kratke"));
        for (int s = 0; s < 40; s++) { igra.Postavi(new Protokol.Igra(6, "Kratko", null, 60, 16, 18, 50, 0)); kratak.Uzmi(igra, [], t.AddSeconds(s)); }
        for (int s = 40; s < 60; s++) { igra.Postavi(null); kratak.Uzmi(igra, [], t.AddSeconds(s)); }
        Proveri.Jednako(0, kratak.Ucitaj().Count, "kraće od minuta se ne čuva");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The session's 1 % low is the average of the worst one percent of its seconds. The old code took the 5th
    /// percentile - a "worst 5 % of seconds" - so the report and the screen showed two numbers for one game.
    /// </summary>
    public static Task Low1Sesije()
    {
        Proveri.Jednako(0.0, IzvestajSesije.Najgorih1Procenat([]), "bez uzoraka nema 1% low");
        Proveri.Blizu(20, IzvestajSesije.Najgorih1Procenat([20, 60, 60, 60, 60]), 0.01,
            "1 % od pet uzoraka je jedan uzorak, i to najgori");

        var sto = Enumerable.Repeat(60.0, 95).Concat([10, 11, 12, 13, 14]).ToList();
        Proveri.Blizu(10, IzvestajSesije.Najgorih1Procenat(sto), 0.01, "1 % od sto je jedan, i to najgori (ne peti)");

        var cetiristo = Enumerable.Repeat(60.0, 396).Concat(Enumerable.Repeat(10.0, 4)).ToList();
        Proveri.Blizu(10, IzvestajSesije.Najgorih1Procenat(cetiristo), 0.01, "1 % od četiristo su četiri najgora");
        return Task.CompletedTask;
    }

    /// <summary>
    /// A loading screen or a menu stops frames for a few seconds, and the game card used to vanish from the Scene
    /// each time (three seconds of patience). Fifteen seconds is the same patience the session recorder has.
    /// </summary>
    public static Task NeNestaje()
    {
        Proveri.Da(PomocnikKlijent.IgreSveze(TimeSpan.FromSeconds(5)), "pet sekundi bez kadrova je pauza");
        Proveri.Da(PomocnikKlijent.IgreSveze(TimeSpan.FromSeconds(14)), "i četrnaest je još pauza");
        Proveri.Da(!PomocnikKlijent.IgreSveze(TimeSpan.FromSeconds(20)), "posle dvadeset igra je otišla");
        return Task.CompletedTask;
    }

    /// <summary>A temperature over the hot limit lights the clip, which stays on until clicked.</summary>
    public static Task Clip()
    {
        var senzori = new List<SenzorVm>
        {
            new(new Senzor(new SenzorKljuc(Grupa.Grafika, "GPU", "Opterećenje"), Jedinica.Procenat), 0, new Prsten(30)),
            new(new Senzor(new SenzorKljuc(Grupa.Grafika, "GPU", "Temperatura"), Jedinica.Celzijus), 1, new Prsten(30)),
            new(new Senzor(new SenzorKljuc(Grupa.Grafika, "GPU", "Zauzeće VRAM"), Jedinica.Procenat), 2, new Prsten(30)),
        };
        var gpu = KanalVm.Napravi(senzori).Single(k => k.Naslov == "GPU");

        var t = DateTime.Now;
        void Citaj(double opt, double temp, double vram)
        {
            double?[] v = [opt, temp, vram];
            for (int i = 0; i < senzori.Count; i++) senzori[i].Prsten.Dodaj(v[i]);
            gpu.Osvezi(new Snimak(t, v), v, v, t);
            t = t.AddSeconds(1);
        }

        for (int i = 0; i < 5; i++) Citaj(50, 70, 60);
        Proveri.Da(!gpu.Clip, "70 °C nije razlog za lampicu");

        Citaj(99, 93, 60);
        Proveri.Da(gpu.Clip, "93 °C (preko 90) pali lampicu");
        Proveri.Da(gpu.ClipOpis.Contains("Temperatura"), "lampica kaže šta i kada");

        Citaj(50, 70, 60);
        Proveri.Da(gpu.Clip, "lampica ostaje upaljena i kad se ohladi");

        gpu.UgasiClip();
        Proveri.Da(!gpu.Clip, "klik je gasi");

        Citaj(50, 70, 97);
        Proveri.Da(gpu.Clip && gpu.ClipOpis.Contains("VRAM"), "pun VRAM pali lampicu");
        return Task.CompletedTask;
    }
}

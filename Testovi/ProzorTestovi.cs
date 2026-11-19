using System.Windows;
using Takt.Sistem;
using Takt.Vm;

namespace Takt.Testovi;

/// <summary>Remembering the window: a saved place is used only if a screen still exists there.</summary>
public static class ProzorTestovi
{
    public static Task Sredi()
    {
        var ekrani = new Rect(0, 0, 1920, 1080);

        Proveri.Jednako<Rect?>(new Rect(100, 100, 1200, 760),
            ProzorPamet.Sredi(new Rect(100, 100, 1200, 760), ekrani), "unutar ekrana ostaje");
        Proveri.Da(ProzorPamet.Sredi(new Rect(-3000, 100, 1200, 760), ekrani) is null, "van ekrana se odbacuje");
        Proveri.Da(ProzorPamet.Sredi(new Rect(100, 100, 500, 300), ekrani) is null, "premali prozor se odbacuje");
        Proveri.Da(ProzorPamet.Sredi(null, ekrani) is null, "ništa sačuvano - ništa se ne primenjuje");

        // A second monitor: the window saved on the right one is still good.
        var dva = new Rect(0, 0, 3840, 1080);
        Proveri.Da(ProzorPamet.Sredi(new Rect(1920, 50, 1200, 760), dva) is not null, "drugi monitor se prihvata");

        // That monitor unplugged since: whatever is still on the first screen is enough to keep the window.
        Proveri.Da(ProzorPamet.Sredi(new Rect(1700, 50, 1200, 760), ekrani) is not null, "deo na ekranu je dovoljan");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The same settings on a smaller machine: a 1240-wide window saved on a big screen would lose its right edge
    /// and its status bar on a 1366×768 laptop. It is shrunk to the screen and kept inside; on two monitors the
    /// virtual desktop is wide enough, so nothing moves.
    /// </summary>
    public static Task MaliEkran()
    {
        var laptop = new Rect(0, 0, 1366, 728);   // the screen without the taskbar
        var sređen = ProzorPamet.Sredi(new Rect(100, 50, 1240, 900), laptop);
        Proveri.Da(sređen is not null, "prozor se prihvata");
        Proveri.Blizu(1240, sređen!.Value.Width, 0.01, "širina staje");
        Proveri.Blizu(728, sređen.Value.Height, 0.01, "visina je skraćena na ekran");
        Proveri.Da(sređen.Value.Bottom <= laptop.Bottom, "donja ivica (statusna traka) je na ekranu");

        var usko = ProzorPamet.Sredi(new Rect(1000, 40, 1240, 760), laptop);
        Proveri.Blizu(1366 - 1240, usko!.Value.X, 0.01, "prozor je pomeren levo da stane");

        // Two monitors: the desktop is wide, so a window on the second one keeps its place.
        var dva = new Rect(0, 0, 3840, 1080);
        var naDrugom = ProzorPamet.Sredi(new Rect(1920, 50, 1200, 760), dva);
        Proveri.Blizu(1920, naDrugom!.Value.X, 0.01, "na dva monitora se ne pomera");
        Proveri.Blizu(1200, naDrugom.Value.Width, 0.01, "i ne skraćuje");
        return Task.CompletedTask;
    }

    /// <summary>
    /// FPS in the one case that used to have no game at all: the window itself elevated, nobody helping. ETW
    /// gives frame events only to administrators, so that window becomes its own counter - and only then: with a
    /// service or helper there must be one ETW session, not two.
    /// </summary>
    public static Task Kadrovi()
    {
        Proveri.Da(ProzorVm.SamMeriKadrove(imaPomocnika: false, admin: true), "povišen prozor meri kadrove sam");
        Proveri.Da(!ProzorVm.SamMeriKadrove(imaPomocnika: true, admin: true),
            "uz uslugu ili pomoćnika prozor ne pravi drugu sesiju");
        Proveri.Da(!ProzorVm.SamMeriKadrove(imaPomocnika: false, admin: false), "bez prava se ne meri");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The chart scale: a narrow range must fill the chart instead of being drawn in a window twice as tall. The
    /// step used to jump straight from 2 to 5, so 51,6-58,4 °C got a 50-70 window (seen on screen, 19. 11. 2026).
    /// </summary>
    public static Task Skala()
    {
        var (donja, gornja, korak) = Takt.Prikaz.Grafikon.Skala(51.6, 58.4, nenegativno: true);
        Proveri.Blizu(50, donja, 0.001, "donja granica");
        Proveri.Blizu(60, gornja, 0.001, "gornja je 60, ne 70");
        Proveri.Blizu(2.5, korak, 0.001, "korak 2,5");

        var nula = Takt.Prikaz.Grafikon.Skala(0, 2, nenegativno: true);
        Proveri.Blizu(0, nula.Donja, 0.001, "nula ostaje donja granica za procente");

        var ravno = Takt.Prikaz.Grafikon.Skala(55, 55, nenegativno: true);
        Proveri.Da(ravno.Gornja > ravno.Donja, "i ravna linija dobija prozor");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The window keeps asking the service (or the helper) while it reads with its own rights: right after an
    /// update, or at logon, the service is not up yet and the window used to stay "Ograničen nadzor" until it was
    /// closed and opened again (seen 19. 11. 2026).
    /// </summary>
    public static Task TrazenjeVeze()
    {
        Proveri.Da(ProzorVm.VrediTrazi(probno: false, imaPomocnika: false, trazi: false, disposed: false),
            "bez veze se pita ponovo");
        Proveri.Da(!ProzorVm.VrediTrazi(false, imaPomocnika: true, trazi: false, disposed: false),
            "kad veza postoji, ne pita se");
        Proveri.Da(!ProzorVm.VrediTrazi(true, false, trazi: false, disposed: false), "probni podaci ne traže vezu");
        Proveri.Da(!ProzorVm.VrediTrazi(false, false, trazi: true, disposed: false),
            "dok se pomoćnik diže, ne pita se ponovo");
        Proveri.Da(!ProzorVm.VrediTrazi(false, false, trazi: false, disposed: true), "zatvoren prozor ne pita");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Header and footer must tell the same story. „Potpuni nadzor · usluga“ above and „običan korisnik“ below
    /// was the lie a report pointed at (19. 11. 2026): the footer asked only this window, never the helper.
    /// </summary>
    public static Task Prava()
    {
        var usluga = ProzorVm.OpisStanja(pravi: true, usluga: true, pomocnik: true, drajver: true, senzora: 125,
            admin: false);
        Proveri.Jednako("Potpuni nadzor · usluga", usluga.Stanje, "zaglavlje: usluga");
        Proveri.Jednako("usluga", usluga.Prava, "podnožje: usluga, ne „običan korisnik“");

        var bezDrajvera = ProzorVm.OpisStanja(true, usluga: true, pomocnik: true, drajver: false, 60, admin: false);
        Proveri.Jednako("Potpuni nadzor · usluga, bez drajvera", bezDrajvera.Stanje, "zaglavlje: usluga bez drajvera");
        Proveri.Jednako("usluga", bezDrajvera.Prava, "podnožje: usluga i kad drajver fali");

        var pomocnik = ProzorVm.OpisStanja(true, usluga: false, pomocnik: true, drajver: true, 125, admin: false);
        Proveri.Jednako("pomoćnik", pomocnik.Prava, "podnožje: pomoćnik");

        var sam = ProzorVm.OpisStanja(true, false, false, drajver: true, 40, admin: true);
        Proveri.Jednako("Potpuni nadzor · administrator", sam.Stanje, "zaglavlje: administrator");
        Proveri.Jednako("administrator", sam.Prava, "podnožje: administrator");

        var obican = ProzorVm.OpisStanja(true, false, false, drajver: true, 40, admin: false);
        Proveri.Jednako("običan korisnik", obican.Prava, "„običan korisnik“ samo kad niko ne pomaže");

        var probni = ProzorVm.OpisStanja(false, false, false, drajver: true, 0, admin: false);
        Proveri.Jednako("Probni podaci", probni.Stanje, "zaglavlje: probni podaci");
        return Task.CompletedTask;
    }

    public static Task PokretanjeSaWindowsom()
    {
        Proveri.Jednako("\"C:\\Program Files\\BronxSoft Takt\\Takt.exe\" --u-traci",
            Pokretanje.Linija("C:\\Program Files\\BronxSoft Takt\\Takt.exe"),
            "putanja pod navodnicima i --u-traci");
        return Task.CompletedTask;
    }

    /// <summary>
    /// The tray menu opens the settings; opening them twice (or opening them and then clicking the menu again)
    /// must not close them - that is what made the menu look like it opens the console.
    /// </summary>
    public static Task Podesavanja()
    {
        using var vm = new ProzorVm(System.Windows.Threading.Dispatcher.CurrentDispatcher, probno: true);

        Proveri.Da(!vm.PodesavanjaOtvorena, "na startu su zatvorena");
        vm.PrikaziPodesavanja();
        Proveri.Da(vm.PodesavanjaOtvorena, "meni trake ih otvara");
        vm.PrikaziPodesavanja();
        Proveri.Da(vm.PodesavanjaOtvorena, "ponovni poziv iz menija ih ne zatvara");

        vm.PreklopiPodesavanja();
        Proveri.Da(!vm.PodesavanjaOtvorena, "dugme u zaglavlju ih zatvara");
        vm.PreklopiPodesavanja();
        Proveri.Da(vm.PodesavanjaOtvorena, "i ponovo otvara");
        return Task.CompletedTask;
    }
}

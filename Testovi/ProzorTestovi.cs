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

using Takt.Merenje;
using Takt.Podaci;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>
/// The service's engine without the SCM: it serves a window, fills the history itself, and takes the next window
/// after the first one closes. What the service does when installed is this, only started by Windows.
/// </summary>
public static class UslugaTest
{
    public static async Task Posluzuje()
    {
        var podesavanja = new Podesavanja { Osvezavanje = 1, Istorija = true, IstorijaDana = 1 };
        using var motor = new UsluzniMotor(podesavanja, pisiIstoriju: true, svakihUzoraka: 2);
        motor.Pokreni();

        try
        {
            Proveri.Da(motor.BrojSenzora > 0, "usluga nije otvorila nijedan senzor");

            var prvi = CekajKlijenta();
            Proveri.Da(prvi is not null, "prozor se nije povezao na uslugu");

            using (prvi)
            {
                Proveri.Da(prvi!.JeUsluga, "prozor zna da ga služi usluga");
                Proveri.Jednako("Pravi senzori (usluga)", prvi.Naziv, "naziv izvora");

                var snimak = CekajUzorak(prvi);
                Proveri.Da(snimak is not null, "nije stigao nijedan uzorak kroz cev usluge");
                Proveri.Jednako(prvi.Senzori.Count, snimak!.Vrednosti.Length,
                    "uzorak ima vrednost za svaki senzor iz spiska");
            }

            // The history is the service's job; in this test it writes every other reading.
            await Task.Delay(2600);
            using (var baza = new Baza(Putanje.Baza))
                Proveri.Da(baza.BrojRedova() > 0, "usluga nije upisala istoriju");

            // A new window after the first one closed: the service waits and takes it.
            var drugi = CekajKlijenta();
            Proveri.Da(drugi is not null, "drugi prozor se ne povezuje posle prvog");
            drugi!.Dispose();
        }
        finally
        {
            motor.Zaustavi();
        }
    }

    private static PomocnikKlijent? CekajKlijenta()
    {
        for (int i = 0; i < 40; i++)
        {
            if (PomocnikKlijent.Probaj(300) is { } klijent) return klijent;
            Thread.Sleep(150);
        }
        return null;
    }

    private static Snimak? CekajUzorak(PomocnikKlijent klijent)
    {
        for (int i = 0; i < 50; i++)
        {
            var snimak = klijent.Uzmi();
            if (snimak.Vrednosti.Any(v => v is not null)) return snimak;
            Thread.Sleep(100);
        }
        return null;
    }
}

using Takt.Merenje;

namespace Takt.Testovi;

public static class PrstenTestovi
{
    public static Task Red()
    {
        var prsten = new Prsten(3);
        prsten.Dodaj(10);
        prsten.Dodaj(20);

        Proveri.Jednako(2, prsten.Broj, "broj posle dva dodavanja");
        Proveri.Jednako(2, prsten.Poslednjih(10).Length, "ne vraća više nego što ima");
        Isto(new double?[] { 10, 20 }, prsten.Poslednjih(5), "dva uzorka po redu");
        Proveri.Jednako<double?>(20, prsten.Poslednja, "poslednja vrednost");

        prsten.Dodaj(30);
        prsten.Dodaj(40);

        Proveri.Jednako(3, prsten.Broj, "prsten ne raste preko kapaciteta");
        Isto(new double?[] { 20, 30, 40 }, prsten.Poslednjih(3), "najstariji je prepisan");
        Isto(new double?[] { 30, 40 }, prsten.Poslednjih(2), "poslednja dva");
        return Task.CompletedTask;
    }

    public static Task Rupe()
    {
        var prsten = new Prsten(5);
        prsten.Dodaj(1);
        prsten.Dodaj(null);
        prsten.Dodaj(3);

        Isto(new double?[] { 1, null, 3 }, prsten.Poslednjih(3), "rupa ostaje rupa");

        var (najmanje, najvise) = prsten.Raspon();
        Proveri.Jednako<double?>(1, najmanje, "najmanje preskače rupe");
        Proveri.Jednako<double?>(3, najvise, "najviše preskače rupe");

        prsten.Dodaj(null);
        Proveri.Jednako<double?>(null, prsten.Poslednja, "poslednja može biti rupa");
        return Task.CompletedTask;
    }

    public static Task Prazan()
    {
        var prsten = new Prsten(4);

        Proveri.Jednako(4, prsten.Kapacitet, "kapacitet");
        Proveri.Jednako(0, prsten.Broj, "prazan broj");
        Proveri.Jednako(0, prsten.Poslednjih(2).Length, "prazan nema šta da vrati");
        Proveri.Jednako<double?>(null, prsten.Poslednja, "prazan nema poslednju");

        var (najmanje, najvise) = prsten.Raspon();
        Proveri.Jednako<double?>(null, najmanje, "prazan raspon");
        Proveri.Jednako<double?>(null, najvise, "prazan raspon");

        prsten.Dodaj(5);
        prsten.Ocisti();
        Proveri.Jednako(0, prsten.Broj, "posle čišćenja");

        bool palo = false;
        try
        {
            _ = new Prsten(0);
        }
        catch (ArgumentOutOfRangeException)
        {
            palo = true;
        }
        Proveri.Da(palo, "kapacitet nula se odbija");
        return Task.CompletedTask;
    }

    private static void Isto(double?[] ocekivano, double?[] stvarno, string poruka)
    {
        Proveri.Jednako(ocekivano.Length, stvarno.Length, poruka + " (dužina)");
        for (int i = 0; i < ocekivano.Length; i++)
            Proveri.Jednako(ocekivano[i], stvarno[i], $"{poruka} (mesto {i})");
    }
}

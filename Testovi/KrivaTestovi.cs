using Takt.Hladjenje;

namespace Takt.Testovi;

public static class KrivaTestovi
{
    public static Task Podrazumevana()
    {
        var kriva = Kriva.Podrazumevana;

        Proveri.Da(kriva.Ispravna, "podrazumevana kriva mora biti ispravna: " + kriva.Greska);
        Proveri.Jednako(25, kriva.Procenat(10), "ispod prve tačke");
        Proveri.Jednako(40, kriva.Procenat(50), "na tački");
        Proveri.Jednako(40, kriva.Procenat(30 + 20), "druga tačka");
        Proveri.Jednako(100, kriva.Procenat(120), "iznad zadnje tačke");
        Proveri.Jednako(25, kriva.Najmanje, "najmanje u krivoj");
        Proveri.Jednako(100, kriva.Najvise, "najviše u krivoj");

        int izmedju = kriva.Procenat(57.5);
        Proveri.Da(izmedju is 47 or 48, "između 50 °C (40 %) i 65 °C (55 %)");
        return Task.CompletedTask;
    }

    public static Task Provera()
    {
        Proveri.Da(new Kriva([new Tacka(50, 40)]).Greska is not null, "jedna tačka nije kriva");
        Proveri.Da(new Kriva([]).Greska is not null, "prazna kriva");
        Proveri.Da(new Kriva([new Tacka(50, 40), new Tacka(50, 60)]).Greska is not null, "iste temperature");
        Proveri.Da(new Kriva([new Tacka(60, 30), new Tacka(50, 60)]).Greska is not null, "temperature ne rastu");
        Proveri.Da(new Kriva([new Tacka(50, 60), new Tacka(60, 50)]).Greska is not null, "brzina pada");
        Proveri.Da(new Kriva([new Tacka(50, 40), new Tacka(60, 120)]).Greska is not null, "preko 100 %");
        Proveri.Da(new Kriva([new Tacka(-5, 40), new Tacka(60, 60)]).Greska is not null, "negativna temperatura");
        Proveri.Da(new Kriva([new Tacka(40, 30), new Tacka(70, 70)]).Greska is null, "ispravna kriva prolazi");
        return Task.CompletedTask;
    }
}

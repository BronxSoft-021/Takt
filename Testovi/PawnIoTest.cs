using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>
/// The driver check and the words it produces. Full monitoring has two keys - administrator rights and the
/// PawnIO driver - and the program must not offer one while the other is missing.
/// </summary>
public static class PawnIoTest
{
    public static Task Provera()
    {
        static bool Nema(string _) => false;
        static bool Ima(string _) => true;

        Proveri.Da(PawnIo.Proveri(@"C:\Windows", Ima, Nema), "drajver .sys je dovoljan dokaz");
        Proveri.Da(PawnIo.Proveri(@"C:\Windows", Nema, Ima), "upis u listi instaliranih programa je dovoljan");
        Proveri.Da(!PawnIo.Proveri(@"C:\Windows", Nema, Nema), "bez fajla i bez upisa - nije instaliran");

        // The service key is checked too: the installer's own naming cannot hide the driver from us.
        var kljucevi = new List<string>();
        PawnIo.Proveri(@"C:\Windows", Nema, k => { kljucevi.Add(k); return false; });
        Proveri.Da(kljucevi.Contains(@"SYSTEM\CurrentControlSet\Services\PawnIO"), "servisni ključ drajvera se proverava");
        Proveri.Da(kljucevi.Any(k => k.Contains("Uninstall")), "proverava se i lista instaliranih programa");

        string? trazen = null;
        PawnIo.Proveri(@"D:\Win", p => { trazen = p; return false; }, _ => false);
        Proveri.Jednako(@"D:\Win\drivers\PawnIO.sys", trazen, "drajver se traži u System32\\drivers");

        // On this machine the real check must answer without throwing, whichever way it goes.
        bool stvarno = PawnIo.JeTu;
        Proveri.Da(stvarno || !stvarno, "provera na ovom računaru ne baca izuzetak");
        return Task.CompletedTask;
    }

    public static Task Prazno()
    {
        var bezDrajvera = GrupaOpis.Prazno(Grupa.Procesor, false);
        Proveri.Da(bezDrajvera.Contains("PawnIO") && bezDrajvera.Contains("pawnio.eu"),
            "bez drajvera se kaže šta tačno donosi senzore");
        Proveri.Da(!bezDrajvera.Contains("Uključi potpuni nadzor"),
            "ne nudi administratora kad on sam ne pomaže");

        Proveri.Da(GrupaOpis.Prazno(Grupa.Hladjenje).Contains("Uključi potpuni nadzor"),
            "sa drajverom prazan kanal nudi potpuni nadzor");
        Proveri.Da(GrupaOpis.Prazno(Grupa.Diskovi, false).Contains("nisu dovoljna"),
            "za diskove se kaže da administrator sam nije dovoljan");
        return Task.CompletedTask;
    }
}

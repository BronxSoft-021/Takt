using System.Diagnostics;
using Takt.Podaci;
using Takt.Sistem;
using TaktSluzba;

namespace Takt.Testovi;

/// <summary>
/// What came out of the code review of 18. 11. 2026: the service runs from its own copy, writes only into a folder
/// that cannot be a planted link, follows the user's history settings, and there is only one Takt at a time.
/// </summary>
public static class BezbednostTestovi
{
    /// <summary>The installer copies the program (without .pdb) into a fresh folder and returns the service's exe there.</summary>
    public static Task KopijaUsluge()
    {
        var izvor = Spisak.NoviFajl("kopija-izvor");
        var cilj = Spisak.NoviFajl("kopija-cilj");
        Directory.CreateDirectory(Path.Combine(izvor, "runtimes"));
        File.WriteAllText(Path.Combine(izvor, "TaktSluzba.exe"), "exe");
        File.WriteAllText(Path.Combine(izvor, "Takt.dll"), "dll");
        File.WriteAllText(Path.Combine(izvor, "Takt.pdb"), "pdb");
        File.WriteAllText(Path.Combine(izvor, "runtimes", "e_sqlite3.dll"), "native");

        // Whatever an older copy left behind is gone after a new install.
        Directory.CreateDirectory(cilj);
        File.WriteAllText(Path.Combine(cilj, "stari.dll"), "staro");

        var exe = Instalacija.Kopiraj(izvor, cilj);

        Proveri.Jednako(Path.Combine(Path.GetFullPath(cilj), "TaktSluzba.exe"), exe, "usluga se pokreće iz kopije");
        Proveri.Da(File.Exists(Path.Combine(cilj, "Takt.dll")), "biblioteke su kopirane");
        Proveri.Da(File.Exists(Path.Combine(cilj, "runtimes", "e_sqlite3.dll")), "i podfolderi");
        Proveri.Da(!File.Exists(Path.Combine(cilj, "Takt.pdb")), ".pdb se ne kopira");
        Proveri.Da(!File.Exists(Path.Combine(cilj, "stari.dll")), "stara kopija je ispražnjena");
        Proveri.Da(Instalacija.Folder.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            StringComparison.OrdinalIgnoreCase), "prava kopija ide u Program Files");
        return Task.CompletedTask;
    }

    /// <summary>A junction planted where the service writes is removed as a link; its target is not touched.</summary>
    public static Task FolderBezVeze()
    {
        var meta = Spisak.NoviFajl("meta");
        var podaci = Spisak.NoviFajl("podaci-usluge");
        Directory.CreateDirectory(meta);
        File.WriteAllText(Path.Combine(meta, "vazno.txt"), "ne dirati");

        var mklink = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{podaci}\" \"{meta}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        })!;
        mklink.WaitForExit();
        Proveri.Da(new DirectoryInfo(podaci).Attributes.HasFlag(FileAttributes.ReparsePoint), "veza je napravljena");

        Proveri.Da(PodaciUsluge.Ocisti(podaci), "veza je prepoznata i uklonjena");
        Proveri.Da(!new DirectoryInfo(podaci).Attributes.HasFlag(FileAttributes.ReparsePoint), "sada je pravi folder");
        Proveri.Da(File.Exists(Path.Combine(meta, "vazno.txt")), "meta veze je netaknuta");
        Proveri.Da(!File.Exists(Path.Combine(podaci, "vazno.txt")), "u novom folderu nema ničeg tuđeg");

        // A folder somebody else made first (here: this user, not the system or the administrators) goes too.
        var tudji = Spisak.NoviFajl("tudji-folder");
        Directory.CreateDirectory(tudji);
        File.WriteAllText(Path.Combine(tudji, "podmetnuto.db"), "x");
        Proveri.Da(PodaciUsluge.Ocisti(tudji), "tuđi folder se pravi iznova");
        Proveri.Da(!File.Exists(Path.Combine(tudji, "podmetnuto.db")), "sa njim i ono što je podmetnuto");

        Proveri.Da(PodaciUsluge.Folder.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), StringComparison.OrdinalIgnoreCase),
            "podaci usluge su u ProgramData, ne u korisnikovom folderu");
        return Task.CompletedTask;
    }

    /// <summary>The service follows "Čuvaj istoriju" and "Obriši istoriju" from the user's settings.</summary>
    public static Task UslugaSlusaPodesavanja()
    {
        var fajl = Spisak.NoviFajl("usluga-istorija.db");
        var podesavanja = new Podesavanja { Istorija = false, ObrisiIstorijuZahtev = 100 };

        using (var baza = new Baza(fajl))
            baza.Upisi(1_000, "Procesor | CPU | Opterećenje", 42);

        using var motor = new UsluzniMotor(() => podesavanja, baza: fajl);

        motor.OsveziPodesavanja();
        Proveri.Da(!motor.PiseIstoriju, "isključena istorija se ne piše");
        using (var baza = new Baza(fajl))
            Proveri.Jednako(1, baza.BrojRedova(), "zahtev za brisanje koji je postojao pre pokretanja se ne izvršava ponovo");

        podesavanja.Istorija = true;
        motor.OsveziPodesavanja();
        Proveri.Da(motor.PiseIstoriju, "uključena istorija se piše bez ponovnog pokretanja usluge");

        podesavanja.ObrisiIstorijuZahtev = 200;
        motor.OsveziPodesavanja();
        using (var baza = new Baza(fajl))
            Proveri.Jednako(0, baza.BrojRedova(), "nov zahtev iz prozora briše istoriju usluge");

        podesavanja.Istorija = false;
        motor.OsveziPodesavanja();
        Proveri.Da(!motor.PiseIstoriju, "isključivanje zatvara bazu");
        return Task.CompletedTask;
    }

    /// <summary>A second Takt finds the first, asks it to open its window, and the first hears it.</summary>
    public static Task JedanTakt()
    {
        string ime = "TaktTest-" + Guid.NewGuid().ToString("N");
        using var prvi = new JedanPrimerak(ime);
        Proveri.Da(prvi.Prvi, "prvi primerak radi");

        using var pozvan = new ManualResetEventSlim(false);
        prvi.Slusaj(() => pozvan.Set());

        using (var drugi = new JedanPrimerak(ime))
        {
            Proveri.Da(!drugi.Prvi, "drugi primerak zna da prvi već radi");
            drugi.JaviPrvom();
        }

        Proveri.Da(pozvan.Wait(TimeSpan.FromSeconds(3)), "prvi primerak je čuo poziv i otvara prozor");
        return Task.CompletedTask;
    }
}

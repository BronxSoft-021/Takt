using System.Diagnostics;
using Microsoft.Win32;
using Takt;
using Takt.Sistem;

namespace TaktSluzba;

/// <summary>
/// Installing and removing Takt: the program and the service together, through <c>sc.exe</c>, so no extra
/// installer is needed - one elevated command, one UAC question.
/// Everything goes to <c>C:\Program Files\BronxSoft Takt</c>: the service runs as LocalSystem and must never run
/// from a folder an ordinary user can write to (anything running as that user could swap a DLL and be started as
/// SYSTEM at the next boot), and the window the shortcuts open is the installed one, not the developer's
/// <c>dist</c> - so the window and the service are always the same version.
/// </summary>
public static class Instalacija
{
    public const string Ime = "BronxSoftTakt";
    public const string Naziv = "BronxSoft Takt (nadzor)";
    public const string Precica = "BronxSoft Takt.lnk";

    /// <summary>Where the installed program lives (Takt.exe and TaktSluzba.exe side by side).</summary>
    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BronxSoft Takt");

    public static string Prozor => Path.Combine(Folder, "Takt.exe");

    public static string? Argument(string[] argumenti, string ime)
    {
        int mesto = Array.IndexOf(argumenti, ime);
        return mesto >= 0 && mesto + 1 < argumenti.Length ? argumenti[mesto + 1] : null;
    }

    public static int Instaliraj(string[] argumenti)
    {
        if (!JeAdmin())
        {
            Console.WriteLine("Instalacija traži administratora: pokreni TaktSluzba.exe --instaliraj kao administrator.");
            return 1;
        }

        // The user's settings folder: the service only reads podesavanja.json there (history on/off, days,
        // refresh, "delete history"); everything it writes goes to its own locked folder in ProgramData.
        var korisnik = Argument(argumenti, "--korisnik") ?? Putanje.Podaci;

        // A service already installed, and a Takt window running from the installed copy, hold their files open.
        if (Sc(["query", Ime], tiho: true) == 0)
        {
            Sc(["stop", Ime], tiho: true);
            Sc(["delete", Ime], tiho: true);
        }
        ZatvoriInstaliraniTakt();
        SacekajStaruUslugu();

        string exe;
        try
        {
            exe = Kopiraj(AppContext.BaseDirectory, Folder);
            PodaciUsluge.Pripremi();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Instalacija", ex);
            Console.WriteLine("Kopiranje u " + Folder + " nije uspelo: " + ex.Message);
            return 1;
        }

        var pokretanje = $"\"{exe}\" --korisnik \"{korisnik}\"";

        int kod = Sc(["create", Ime, "binPath=", pokretanje, "start=", "auto", "obj=", "LocalSystem",
            "DisplayName=", Naziv]);
        if (kod != 0) return kod;

        Sc(["description", Ime, "Čita senzore kao administrator i služi prozor preko cevi. BronxSoft Takt."]);
        Sc(["start", Ime]);

        // The shortcuts and "start with Windows" open the installed Takt from now on, not the developer's dist.
        foreach (var lnk in Precice())
            NapraviPrecicu(lnk, Prozor);
        PreusmeriPokretanje(Prozor);

        Console.WriteLine($"Takt je instaliran u {Folder}; prečice na desktopu i u Start meniju otvaraju njega.");
        Console.WriteLine($"Istorija i dnevnik usluge: {PodaciUsluge.Folder}. Podešavanja čita iz: {korisnik}");
        return 0;
    }

    public static int Ukloni()
    {
        if (!JeAdmin())
        {
            Console.WriteLine("Uklanjanje traži administratora: pokreni TaktSluzba.exe --ukloni kao administrator.");
            return 1;
        }

        Sc(["stop", Ime]);
        int kod = Sc(["delete", Ime]);
        ZatvoriInstaliraniTakt();
        SacekajStaruUslugu();

        // The installed copy goes, with the shortcuts and the Run value that point at it; the history in
        // ProgramData and the user's settings stay (they are the user's data).
        for (int pokusaj = 0; pokusaj < 10 && Directory.Exists(Folder); pokusaj++)
        {
            try
            {
                Directory.Delete(Folder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(500); // the stopped service may still hold its files for a moment
            }
        }

        foreach (var lnk in Precice())
            if (File.Exists(lnk) && CiljPrecice(lnk)?.StartsWith(Folder, StringComparison.OrdinalIgnoreCase) == true)
                File.Delete(lnk);
        UkloniPokretanje();

        if (kod == 0) Console.WriteLine($"Takt je uklonjen. Istorija ostaje u {PodaciUsluge.Folder}.");
        return kod;
    }

    /// <summary>
    /// Copies every file of the running program (with subfolders) into <paramref name="cilj"/>, emptied first,
    /// and returns the path of the service's .exe there. Public so a test can check it on a temporary folder.
    /// </summary>
    public static string Kopiraj(string izvor, string cilj)
    {
        izvor = Path.GetFullPath(izvor);
        cilj = Path.GetFullPath(cilj);
        if (string.Equals(izvor.TrimEnd('\\'), cilj.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            return Path.Combine(cilj, "TaktSluzba.exe");

        // A file of the old copy can stay locked for a moment after its process ends (antivirus, the loader):
        // a few tries before giving up.
        for (int pokusaj = 1; Directory.Exists(cilj); pokusaj++)
        {
            try
            {
                Directory.Delete(cilj, recursive: true);
            }
            catch (Exception ex) when (pokusaj < 10 && ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(500);
            }
        }
        Directory.CreateDirectory(cilj);

        foreach (var fajl in Directory.EnumerateFiles(izvor, "*", SearchOption.AllDirectories))
        {
            var relativno = Path.GetRelativePath(izvor, fajl);
            if (relativno.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) continue;

            var odrediste = Path.Combine(cilj, relativno);
            Directory.CreateDirectory(Path.GetDirectoryName(odrediste)!);
            File.Copy(fajl, odrediste, overwrite: true);
        }

        var exe = Path.Combine(cilj, "TaktSluzba.exe");
        if (!File.Exists(exe)) throw new FileNotFoundException("TaktSluzba.exe nije među kopiranim fajlovima.", exe);
        return exe;
    }

    /// <summary>The desktop and the Start menu of the user who installs (the elevated process is still that user).</summary>
    private static IEnumerable<string> Precice() =>
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Precica),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Precica),
    ];

    /// <summary>
    /// A Takt window running from the installed copy is closed (it would hold Takt.dll open). The settings are
    /// saved whenever the window hides, so nothing is lost; the developer's own Takt from dist is left alone.
    /// </summary>
    private static void ZatvoriInstaliraniTakt()
    {
        foreach (var proces in Process.GetProcessesByName("Takt"))
        {
            using (proces)
            {
                try
                {
                    var putanja = proces.MainModule?.FileName;
                    if (putanja is null || !putanja.StartsWith(Folder, StringComparison.OrdinalIgnoreCase)) continue;

                    Console.WriteLine("Zatvaram instalirani Takt da bi se zamenio novim.");
                    proces.Kill();
                    proces.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    Dnevnik.Greska("Instalacija: zatvaranje Takta", ex);
                }
            }
        }
    }

    /// <summary>
    /// "sc stop" only asks; the old service process may need a few seconds to close its sensors and exit, and
    /// until then its files cannot be replaced. Waits up to 20 s, then ends it. Never touches the running installer.
    /// </summary>
    private static void SacekajStaruUslugu()
    {
        foreach (var proces in Process.GetProcessesByName("TaktSluzba"))
        {
            using (proces)
            {
                try
                {
                    if (proces.Id == Environment.ProcessId) continue;
                    var putanja = proces.MainModule?.FileName;
                    if (putanja is null || !putanja.StartsWith(Folder, StringComparison.OrdinalIgnoreCase)) continue;

                    if (!proces.WaitForExit(20_000))
                    {
                        Console.WriteLine("Stara usluga se ne gasi sama; zatvaram je.");
                        proces.Kill();
                        proces.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    Dnevnik.Greska("Instalacija: čekanje stare usluge", ex);
                }
            }
        }
    }

    private static void NapraviPrecicu(string lnk, string exe)
    {
        static string Navodi(string tekst) => "'" + tekst.Replace("'", "''") + "'";

        var skript =
            "$s = (New-Object -ComObject WScript.Shell).CreateShortcut(" + Navodi(lnk) + "); " +
            "$s.TargetPath = " + Navodi(exe) + "; " +
            "$s.WorkingDirectory = " + Navodi(Path.GetDirectoryName(exe)!) + "; " +
            "$s.IconLocation = " + Navodi(exe + ",0") + "; " +
            "$s.Description = 'BronxSoft Takt'; $s.Save()";

        if (Pokreni("powershell.exe", ["-NoProfile", "-NonInteractive", "-Command", skript], tiho: true) != 0)
            Console.WriteLine("Prečica nije napravljena: " + lnk);
    }

    private static string? CiljPrecice(string lnk)
    {
        try
        {
            var shell = Type.GetTypeFromProgID("WScript.Shell");
            if (shell is null) return null;
            dynamic wsh = Activator.CreateInstance(shell)!;
            return (string)wsh.CreateShortcut(lnk).TargetPath;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Instalacija: čitanje prečice", ex);
            return null;
        }
    }

    /// <summary>"Start with Windows" that was on keeps working: the Run value now points at the installed Takt.</summary>
    private static void PreusmeriPokretanje(string exe)
    {
        try
        {
            using var kljuc = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (kljuc?.GetValue(Pokretanje.Ime) is string) kljuc.SetValue(Pokretanje.Ime, Pokretanje.Linija(exe));
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Instalacija: pokretanje sa Windowsom", ex);
        }
    }

    private static void UkloniPokretanje()
    {
        try
        {
            using var kljuc = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (kljuc?.GetValue(Pokretanje.Ime) is string linija && linija.Contains(Folder, StringComparison.OrdinalIgnoreCase))
                kljuc.DeleteValue(Pokretanje.Ime, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Instalacija: uklanjanje pokretanja", ex);
        }
    }

    private static bool JeAdmin()
    {
        try
        {
            using var identitet = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(identitet)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static int Sc(string[] argumenti, bool tiho = false) => Pokreni("sc.exe", argumenti, tiho);

    /// <summary>Runs a program with one argument per list entry, so paths with spaces stay one argument.</summary>
    private static int Pokreni(string program, string[] argumenti, bool tiho)
    {
        try
        {
            var pokretanje = new ProcessStartInfo(program)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var argument in argumenti) pokretanje.ArgumentList.Add(argument);

            using var proces = Process.Start(pokretanje);
            if (proces is null) return 1;

            string izlaz = proces.StandardOutput.ReadToEnd() + proces.StandardError.ReadToEnd();
            proces.WaitForExit();

            if (!tiho && !string.IsNullOrWhiteSpace(izlaz)) Console.WriteLine(izlaz.Trim());
            return proces.ExitCode;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska(program, ex);
            Console.WriteLine(program + " nije uspeo: " + ex.Message);
            return 1;
        }
    }
}

using System.Diagnostics;
using Takt;
using Takt.Sistem;

namespace TaktSluzba;

/// <summary>
/// Installing and removing the service through <c>sc.exe</c>, so no extra installer is needed: one elevated
/// command, one UAC question, and the service starts with Windows from then on.
/// The service runs as LocalSystem, so it must never run from a folder an ordinary user can write to - anything
/// running as that user could swap a DLL and be started as SYSTEM at the next boot. The installer therefore
/// copies itself to <c>C:\Program Files\BronxSoft Takt\Usluga</c> (only administrators write there) and points
/// the service at that copy, never at <c>dist</c>.
/// </summary>
public static class Instalacija
{
    public const string Ime = "BronxSoftTakt";
    public const string Naziv = "BronxSoft Takt (nadzor)";

    /// <summary>Where the service's own copy lives.</summary>
    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BronxSoft Takt", "Usluga");

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

        // A service already installed holds its files open: stop and remove it before copying over it.
        if (Sc(["query", Ime], tiho: true) == 0)
        {
            Sc(["stop", Ime], tiho: true);
            Sc(["delete", Ime], tiho: true);
            Thread.Sleep(1500);
        }

        string exe;
        try
        {
            exe = Kopiraj(AppContext.BaseDirectory, Folder);
            PodaciUsluge.Pripremi();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Instalacija usluge", ex);
            Console.WriteLine("Kopiranje u " + Folder + " nije uspelo: " + ex.Message);
            return 1;
        }

        var pokretanje = $"\"{exe}\" --korisnik \"{korisnik}\"";

        int kod = Sc(["create", Ime, "binPath=", pokretanje, "start=", "auto", "obj=", "LocalSystem",
            "DisplayName=", Naziv]);
        if (kod != 0) return kod;

        Sc(["description", Ime, "Čita senzore kao administrator i služi prozor preko cevi. BronxSoft Takt."]);
        Sc(["start", Ime]);

        Console.WriteLine($"Usluga „{Naziv}“ je instalirana u {Folder}.");
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

        // The copy in Program Files goes too; the history in ProgramData stays (it is the user's data).
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

        if (kod == 0) Console.WriteLine($"Usluga „{Naziv}“ je uklonjena. Istorija ostaje u {PodaciUsluge.Folder}.");
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

        if (Directory.Exists(cilj)) Directory.Delete(cilj, recursive: true);
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

    /// <summary>Runs sc.exe with one argument per list entry, so paths with spaces stay one argument.</summary>
    private static int Sc(string[] argumenti, bool tiho = false)
    {
        try
        {
            var pokretanje = new ProcessStartInfo("sc.exe")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
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
            Dnevnik.Greska("sc.exe", ex);
            Console.WriteLine("sc.exe nije uspeo: " + ex.Message);
            return 1;
        }
    }
}

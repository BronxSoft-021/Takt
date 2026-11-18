using System.Diagnostics;
using Takt;
using Takt.Sistem;

namespace TaktSluzba;

/// <summary>
/// Installing and removing the service through <c>sc.exe</c>, so no extra installer is needed: one elevated
/// command, one UAC question, and the service starts with Windows from then on.
/// </summary>
public static class Instalacija
{
    public const string Ime = "BronxSoftTakt";
    public const string Naziv = "BronxSoft Takt (nadzor)";

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

        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            Console.WriteLine("Ne znam putanju programa.");
            return 1;
        }

        // The service runs as LocalSystem; its %APPDATA% is not the user's, so the data folder is fixed here.
        var podaci = Argument(argumenti, "--podaci") ?? Putanje.Podaci;
        var pokretanje = $"\"{exe}\" --podaci \"{podaci}\"";

        int kod = Sc(["create", Ime, "binPath=", pokretanje, "start=", "auto", "obj=", "LocalSystem",
            "DisplayName=", Naziv]);
        if (kod != 0) return kod;

        Sc(["description", Ime, "Čita senzore kao administrator i služi prozor preko cevi. BronxSoft Takt."]);
        Sc(["start", Ime]);

        Console.WriteLine($"Usluga „{Naziv}“ je instalirana. Podaci: {podaci}");
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
        if (kod == 0) Console.WriteLine($"Usluga „{Naziv}“ je uklonjena.");
        return kod;
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
    private static int Sc(string[] argumenti)
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

            if (!string.IsNullOrWhiteSpace(izlaz)) Console.WriteLine(izlaz.Trim());
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

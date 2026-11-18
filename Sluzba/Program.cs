using System.ServiceProcess;
using Takt.Sistem;
using TaktSluzba;

// --instaliraj / --ukloni traže administratora; --konzola radi isto što i usluga, ali u konzoli (za proveru).
if (args.Contains("--instaliraj")) return Instalacija.Instaliraj(args);
if (args.Contains("--ukloni")) return Instalacija.Ukloni();

// The service runs as LocalSystem, whose %APPDATA% is not the user's: instalacija zato upiše putanju u binPath.
var podaci = Instalacija.Argument(args, "--podaci");
if (!string.IsNullOrWhiteSpace(podaci))
    Environment.SetEnvironmentVariable("TAKT_PODACI", Path.GetFullPath(podaci));

if (args.Contains("--konzola"))
{
    using var motor = new UsluzniMotor(Podesavanja.Ucitaj());
    using var kraj = new ManualResetEventSlim(false);
    Console.CancelKeyPress += (_, dogadjaj) =>
    {
        dogadjaj.Cancel = true;
        kraj.Set();
    };

    motor.Pokreni();
    Console.WriteLine("Usluga radi u konzoli. Ctrl+C za kraj.");
    kraj.Wait();
    return 0;
}

ServiceBase.Run(new TaktServis());
return 0;

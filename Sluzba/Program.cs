using System.ServiceProcess;
using Takt.Sistem;
using TaktSluzba;

// --instaliraj / --ukloni traže administratora; --konzola radi isto što i usluga, ali u konzoli (za proveru).
if (args.Contains("--instaliraj")) return Instalacija.Instaliraj(args);
if (args.Contains("--ukloni")) return Instalacija.Ukloni();

// Serbian text means Serbian numbers, and the log is read beside the window: one culture for both.
Takt.Merenje.Format.PostaviKulturu();

// The service writes only into its own locked folder in ProgramData (history and log): never into the user's
// %APPDATA%, which anything running as that user could turn into a link. Set before anything logs.
Environment.SetEnvironmentVariable("TAKT_PODACI", PodaciUsluge.Folder);
try
{
    PodaciUsluge.Pripremi();
}
catch (Exception ex)
{
    Takt.Dnevnik.Greska("Usluga: priprema foldera podataka", ex);
}

// The user's settings (history on/off, days, refresh, "delete history") are only read, from the folder the
// installer wrote into the service's command line.
var korisnik = Instalacija.Argument(args, "--korisnik");
var podesavanja = korisnik is null ? null : Path.Combine(Path.GetFullPath(korisnik), "podesavanja.json");
Func<Podesavanja> ucitaj = () => podesavanja is null ? new Podesavanja() : Podesavanja.Ucitaj(podesavanja);

// The service names the sensors (it is the one that reads the hardware and sends the list), so it must speak the
// language the user chose: with "sr" here and "en" in the window, the table showed Serbian names in an English UI.
Takt.Tekst.Postavi(ucitaj().Jezik);

if (args.Contains("--konzola"))
{
    using var motor = new UsluzniMotor(ucitaj);
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

ServiceBase.Run(new TaktServis(ucitaj));
return 0;

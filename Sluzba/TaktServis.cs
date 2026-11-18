using System.ServiceProcess;
using Takt;
using Takt.Sistem;

namespace TaktSluzba;

/// <summary>
/// The service itself is a shell: it starts the engine and stops it. Everything that reads sensors, keeps the
/// history and talks to the window lives in <see cref="UsluzniMotor"/>, so it can be tested without the SCM.
/// </summary>
public sealed class TaktServis : ServiceBase
{
    private readonly Func<Podesavanja> _ucitaj;
    private UsluzniMotor? _motor;

    public TaktServis(Func<Podesavanja> ucitaj)
    {
        _ucitaj = ucitaj;
        ServiceName = Instalacija.Ime;
        CanStop = true;
        CanShutdown = true;
        AutoLog = true;
    }

    protected override void OnStart(string[] args)
    {
        _motor = new UsluzniMotor(_ucitaj);
        _motor.Pokreni();
        Dnevnik.Upisi("Usluga: pokrenuta.");
    }

    protected override void OnStop()
    {
        _motor?.Dispose();
        _motor = null;
        Dnevnik.Upisi("Usluga: zaustavljena.");
    }

    protected override void OnShutdown() => OnStop();
}

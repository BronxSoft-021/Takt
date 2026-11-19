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
    private Task? _pokretanje;

    public TaktServis(Func<Podesavanja> ucitaj)
    {
        _ucitaj = ucitaj;
        ServiceName = Instalacija.Ime;
        CanStop = true;
        CanShutdown = true;
        AutoLog = true;
    }

    /// <summary>
    /// The SCM counts the seconds this method runs and gives thirty. Opening the driver and asking six drives
    /// takes several (measured 9 s until Running on 19. 11. 2026 - a machine with sleeping drives could pass
    /// thirty and be told the service did not start). So the engine starts on its own thread and this returns
    /// at once; the service is "running" and reading a moment later.
    /// </summary>
    protected override void OnStart(string[] args)
    {
        RequestAdditionalTime(30_000);

        var motor = new UsluzniMotor(_ucitaj);
        _motor = motor;
        _pokretanje = Task.Run(() =>
        {
            try
            {
                motor.Pokreni();
                Dnevnik.Upisi("Usluga: pokrenuta.");
            }
            catch (Exception ex)
            {
                Dnevnik.Greska("Usluga: pokretanje motora", ex);
            }
        });
    }

    protected override void OnStop()
    {
        // A stop during the first second must not pull the engine from under its own start.
        try
        {
            _pokretanje?.Wait(TimeSpan.FromSeconds(20));
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Usluga: čekanje na pokretanje", ex);
        }
        _pokretanje = null;

        _motor?.Dispose();
        _motor = null;
        Dnevnik.Upisi("Usluga: zaustavljena.");
    }

    protected override void OnShutdown() => OnStop();
}

using System.Windows;
using System.Windows.Threading;
using Takt.Prikaz;
using Takt.Sistem;
using Takt.Vm;

namespace Takt;

public partial class App : Application
{
    private ProzorVm? _vm;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, a) =>
        {
            Dnevnik.Greska("Greška u prozoru", a.Exception);
            a.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, a) => Dnevnik.Greska("Neobrađena greška", a.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, a) =>
        {
            Dnevnik.Greska("Zadatak", a.Exception);
            a.SetObserved();
        };

        // Started by the window (after the Windows question about administrator rights). It only reads the
        // sensors and sends them over a pipe; it has no window and stops when the window closes.
        if (e.Args.Contains("--pomocnik"))
        {
            PokreniPomocnika(e.Args);
            return;
        }

        // --probno: made-up readings. --snimak <folder>: save every screen as PNG and close.
        // --snimak --pravo takes the pictures from the real hardware instead.
        var snimak = Argument(e.Args, "--snimak");
        bool probno = e.Args.Contains("--probno") || (snimak is not null && !e.Args.Contains("--pravo"));

        _vm = new ProzorVm(Dispatcher, probno);
        var prozor = new GlavniProzor { DataContext = _vm };
        MainWindow = prozor;
        prozor.Show();

        if (snimak is not null)
        {
            var vm = _vm;
            _ = Dispatcher.BeginInvoke(new Action(async () =>
            {
                await vm.PokreniAsync();
                await Slike.SacuvajSve(prozor, vm, snimak);
            }));
        }
        else
        {
            // The window is already on screen; the sensors open behind it so it never says "Not responding".
            _ = _vm.PokreniAsync();
        }
    }

    private static string? Argument(string[] argumenti, string ime)
    {
        int mesto = Array.IndexOf(argumenti, ime);
        return mesto >= 0 && mesto + 1 < argumenti.Length ? argumenti[mesto + 1] : null;
    }

    /// <summary>Runs the helper on a background thread and closes this copy of the program when it ends.</summary>
    private void PokreniPomocnika(string[] argumenti)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        int mesto = Array.IndexOf(argumenti, "--pomocnik");
        int? roditelj = mesto >= 0 && mesto + 1 < argumenti.Length && int.TryParse(argumenti[mesto + 1], out int pid)
            ? pid
            : null;

        _ = Task.Run(() =>
        {
            int kod = Pomocnik.Radi(roditelj);
            Dispatcher.BeginInvoke(new Action(() => Shutdown(kod)));
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _vm?.Dispose();
        base.OnExit(e);
    }
}

using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Takt.Prikaz;
using Takt.Sistem;
using Takt.Vm;

namespace Takt;

public partial class App : Application
{
    private ProzorVm? _vm;
    private GlavniProzor? _prozor;
    private Traka? _traka;
    private JedanPrimerak? _primerak;
    private bool _izlazim;
    private bool _testniRad;
    private bool _probno;

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
        _probno = probno;

        // Test runs must not move or resize the user's window, and must not write their settings.
        _testniRad = probno || snimak is not null;

        // Started by Windows (Run key): the program lives in the tray only. The window, the sensors and the
        // helper are made on the first "Otvori Takt", so a boot with Takt costs no UAC question and almost
        // nothing else; the service, if installed, keeps reading and filling the history meanwhile.
        bool uTraci = e.Args.Contains("--u-traci") && !_testniRad;

        // One Takt per session: a second start (desktop icon while the first sits in the tray) shows the first
        // one's window and leaves. Started by Windows with a copy already running, it just leaves.
        if (!_testniRad)
        {
            _primerak = new JedanPrimerak();
            if (!_primerak.Prvi)
            {
                if (!uTraci) _primerak.JaviPrvom();
                _primerak.Dispose();
                _primerak = null;
                Shutdown();
                return;
            }
            _primerak.Slusaj(() => Dispatcher.BeginInvoke(new Action(OtvoriProzor)));
        }

        NapraviTraku();

        if (!uTraci || _traka is null) NapraviProzor();

        if (snimak is not null)
        {
            var vm = _vm!;
            var prozor = _prozor!;
            _ = Dispatcher.BeginInvoke(new Action(async () =>
            {
                await vm.PokreniAsync();
                await Slike.SacuvajSve(prozor, vm, snimak);
            }));
        }
        else if (!uTraci)
        {
            // The window is already on screen; the sensors open behind it so it never says "Not responding".
            _ = _vm!.PokreniAsync();
        }
    }

    /// <summary>Makes the window (and the view models behind it); the sensors are started by whoever shows it first.</summary>
    private void NapraviProzor()
    {
        if (_prozor is not null || _vm is not null) return;

        _vm = new ProzorVm(Dispatcher, _probno);

        // A finished game session says so from the tray: one line, the full report is under "Sesije".
        if (!_testniRad)
            _vm.Sesija.Zavrsena += izvestaj =>
                _traka?.Reci($"Sesija: {izvestaj.Igra}", izvestaj.Kratko + ". Ceo izveštaj je pod „Sesije“.");
        _prozor = new GlavniProzor { DataContext = _vm };

        if (!_testniRad) ProzorPamet.Primeni(_prozor, _vm.Podesavanja);

        MainWindow = _prozor;
        _prozor.Closing += NaZatvaranje;
        _prozor.Closed += (_, _) => Shutdown();
        _prozor.Show();
    }

    private void NapraviTraku()
    {
        if (_testniRad) return;

        try
        {
            _traka = new Traka(Izmene.Trenutna.Oznaka);
            _traka.Otvori += () => Dispatcher.Invoke(OtvoriProzor);
            _traka.Podesavanja += () => Dispatcher.Invoke(() =>
            {
                OtvoriProzor();
                _vm?.PrikaziPodesavanja();
            });
            _traka.Izlaz += () => Dispatcher.Invoke(() =>
            {
                _izlazim = true;
                Shutdown();
            });
        }
        catch (Exception ex)
        {
            // Without the icon there is no way back from a hidden window: the old behaviour (X closes) stays.
            Dnevnik.Greska("Ikonica u traci", ex);
            _traka = null;
        }
    }

    private void OtvoriProzor()
    {
        bool novi = _prozor is null;
        if (novi) NapraviProzor();
        if (_prozor is null) return;

        if (!_prozor.IsVisible) _prozor.Show();
        if (_prozor.WindowState == WindowState.Minimized) _prozor.WindowState = WindowState.Normal;
        _prozor.Activate();

        // The sensors and the helper wake up with the first window: no UAC question at boot.
        if (novi) _ = _vm!.PokreniAsync();
    }

    /// <summary>X hides to the tray (unless the user turned that off, or the tray could not be made).</summary>
    private void NaZatvaranje(object? sender, CancelEventArgs a)
    {
        if (_izlazim || _prozor is null) return;

        if (_testniRad)
        {
            _izlazim = true;
            return;
        }

        ProzorPamet.Sacuvaj(_prozor, _vm!.Podesavanja);
        _vm.Podesavanja.Sacuvaj();

        if (_traka is not null && _vm.Podesavanja.ZatvoriUTraku)
        {
            a.Cancel = true;
            _prozor.Hide();
            PrviPutUTraci();
            return;
        }

        _izlazim = true;
    }

    private void PrviPutUTraci()
    {
        if (_vm is null || _vm.Podesavanja.ObjasnjenoUTraci) return;

        _vm.Podesavanja.ObjasnjenoUTraci = true;
        _vm.Podesavanja.Sacuvaj();
        _traka?.Reci("BronxSoft Takt",
            "Program i dalje radi. Ikonica je pored sata: dvoklik otvara prozor, desni klik daje meni.");
    }

    private static string? Argument(string[] argumenti, string ime)
    {
        int mesto = Array.IndexOf(argumenti, ime);
        return mesto >= 0 && mesto + 1 < argumenti.Length ? argumenti[mesto + 1] : null;
    }

    /// <summary>Runs the helper on a background thread and closes this copy of the program when it ends.</summary>
    private void PokreniPomocnika(string[] argumenti)
    {
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
        if (!_testniRad && _vm is not null && _prozor is not null)
        {
            ProzorPamet.Sacuvaj(_prozor, _vm.Podesavanja);
            _vm.Podesavanja.Sacuvaj();
        }

        _traka?.Dispose();
        _traka = null;
        _vm?.Dispose();
        _primerak?.Dispose();
        _primerak = null;
        base.OnExit(e);
    }
}

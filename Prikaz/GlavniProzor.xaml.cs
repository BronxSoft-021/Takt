using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Takt.Vm;

namespace Takt.Prikaz;

/// <summary>
/// The window. Clicks are passed to <see cref="ProzorVm"/>. The only thing it does itself is the Scene's frame:
/// no title bar and maximized on the screen it is on, and back to the old frame when the Scene ends.
/// </summary>
public partial class GlavniProzor : Window
{
    private (WindowState Stanje, WindowStyle Stil, ResizeMode Velicina)? _preScene;

    public GlavniProzor()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is ProzorVm stari) stari.PropertyChanged -= NaPromenu;
            if (e.NewValue is ProzorVm novi) novi.PropertyChanged += NaPromenu;
        };
        Loaded += (_, _) => { if (Vm?.Scena == true) PrimeniScenu(true); };
        PreviewKeyDown += NaTaster;
    }

    private ProzorVm? Vm => DataContext as ProzorVm;

    private void NaPromenu(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProzorVm.Scena) && Vm is { } vm) PrimeniScenu(vm.Scena);
    }

    /// <summary>F11 turns the Scene on and off, Esc only leaves it.</summary>
    private void NaTaster(object sender, KeyEventArgs e)
    {
        if (Vm is not { } vm) return;
        if (e.Key == Key.F11) { vm.Scena = !vm.Scena; e.Handled = true; }
        else if (e.Key == Key.Escape && vm.Scena) { vm.Scena = false; e.Handled = true; }
    }

    private void PrimeniScenu(bool scena)
    {
        if (scena)
        {
            if (_preScene is not null) return;
            _preScene = (WindowState, WindowStyle, ResizeMode);
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            // Normal first: a window already maximized keeps its frame-size otherwise.
            if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;
            return;
        }

        if (_preScene is not { } pre) return;
        _preScene = null;
        WindowStyle = pre.Stil;
        ResizeMode = pre.Velicina;
        WindowState = pre.Stanje == WindowState.Minimized ? WindowState.Normal : pre.Stanje;
    }

    private void NaScenu(object sender, RoutedEventArgs e)
    {
        if (Vm is { } vm) vm.Scena = true;
    }

    private void NaSesije(object sender, RoutedEventArgs e) => Vm?.PrikaziSesije();

    private void NaPodesavanja(object sender, RoutedEventArgs e) => Vm?.PreklopiPodesavanja();

    private void NaPomocnika(object sender, RoutedEventArgs e) => _ = Vm?.UkljuciPomocnikaAsync();

    private void NaKanal(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal } && Vm is { } vm) vm.Kanal = kanal;
    }

    private void NaSolo(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal } && Vm is { } vm) vm.Kanal = kanal;
        e.Handled = true;
    }

    /// <summary>The console scrolls sideways; the mouse wheel moves it, as nobody looks for a thin bar at the bottom.</summary>
    private void NaTockicPulta(object sender, MouseWheelEventArgs e)
    {
        if (sender is not System.Windows.Controls.ScrollViewer pult || pult.ScrollableWidth <= 0) return;
        pult.ScrollToHorizontalOffset(pult.HorizontalOffset - e.Delta / 2.0);
        e.Handled = true;
    }

    private void NaClip(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal }) kanal.UgasiClip();
        e.Handled = true;
    }

    private void NaUtisaj(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal } && Vm is { } vm) vm.Utisaj(kanal);
        e.Handled = true;
    }
}

using System.Windows;
using System.Windows.Input;
using Takt.Vm;

namespace Takt.Prikaz;

/// <summary>The window. Clicks are passed to <see cref="ProzorVm"/>; there is no logic here.</summary>
public partial class GlavniProzor : Window
{
    public GlavniProzor() => InitializeComponent();

    private ProzorVm? Vm => DataContext as ProzorVm;

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

    private void NaUtisaj(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal } && Vm is { } vm) vm.Utisaj(kanal);
        e.Handled = true;
    }
}

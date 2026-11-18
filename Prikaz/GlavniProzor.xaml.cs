using System.Windows;
using System.Windows.Input;
using Takt.Vm;

namespace Takt.Prikaz;

/// <summary>The window. Clicks are passed to <see cref="ProzorVm"/>; there is no logic here.</summary>
public partial class GlavniProzor : Window
{
    public GlavniProzor() => InitializeComponent();

    private ProzorVm? Vm => DataContext as ProzorVm;

    private void NaPodesavanja(object sender, RoutedEventArgs e) => Vm?.PrikaziPodesavanja();

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

    private void NaUtisaj(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal } && Vm is { } vm) vm.Utisaj(kanal);
        e.Handled = true;
    }
}

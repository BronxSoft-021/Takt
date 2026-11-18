using System.Windows;
using System.Windows.Controls;
using Takt.Vm;

namespace Takt.Prikaz;

public partial class GlavniProzor : Window
{
    public GlavniProzor() => InitializeComponent();

    private void NaPodesavanja(object sender, RoutedEventArgs e) =>
        (DataContext as ProzorVm)?.PrikaziPodesavanja();

    private void NaPomocnika(object sender, RoutedEventArgs e) =>
        _ = (DataContext as ProzorVm)?.UkljuciPomocnikaAsync();

    private void NaPlocicu(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Merenje.Grupa grupa } && DataContext is ProzorVm vm)
            vm.PrikaziGrupu(grupa);
    }
}

using System.Windows;
using System.Windows.Controls;
using Takt.Vm;

namespace Takt.Prikaz;

/// <summary>Settings screen. The buttons only pass the click to the view model.</summary>
public partial class EkranPodesavanja : UserControl
{
    public EkranPodesavanja() => InitializeComponent();

    private PodesavanjaVm? Vm => DataContext as PodesavanjaVm;

    private void NaSacuvaj(object sender, RoutedEventArgs e) => Vm?.Sacuvaj();

    private void NaFolder(object sender, RoutedEventArgs e) => Vm?.OtvoriFolder();

    private void NaObrisi(object sender, RoutedEventArgs e) => Vm?.ObrisiIstoriju();

    private void NaDrajver(object sender, RoutedEventArgs e) => Vm?.OtvoriDrajver();

    /// <summary>The page is longer than the window; the wheel moves it smoothly, like the console.</summary>
    private void NaTockic(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer strana || strana.ScrollableHeight <= 0) return;
        Glatko.Uspravno(strana, -e.Delta * 0.6);
        e.Handled = true;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Takt.Vm;

namespace Takt.Prikaz;

/// <summary>The Scene: clicks go to <see cref="ProzorVm"/>. A click on a strip leaves the Scene with that channel in solo.</summary>
public partial class Scena : UserControl
{
    public Scena() => InitializeComponent();

    private ProzorVm? Vm => DataContext as ProzorVm;

    private void NaIzlaz(object sender, RoutedEventArgs e)
    {
        // The button in the Scene's own header: closed by hand, so it must not come back for this game.
        if (Vm is { } vm) vm.UgasiScenuRukom();
    }

    private void NaKanal(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal } && Vm is { } vm)
        {
            // Picking a channel leaves the Scene on purpose (the person wants to look at that channel).
            vm.UgasiScenuRukom();
            vm.Kanal = kanal;
        }
    }

    private void NaClip(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal }) kanal.UgasiClip();
        e.Handled = true;
    }
}

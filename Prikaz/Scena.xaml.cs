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
        if (Vm is { } vm) vm.Scena = false;
    }

    private void NaKanal(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal } && Vm is { } vm)
        {
            vm.Scena = false;
            vm.Kanal = kanal;
        }
    }

    private void NaClip(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: KanalVm kanal }) kanal.UgasiClip();
        e.Handled = true;
    }
}

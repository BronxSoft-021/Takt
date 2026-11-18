using System.Windows;
using System.Windows.Controls;
using Takt.Vm;

namespace Takt.Prikaz;

/// <summary>One screen for one group. The content comes from <see cref="EkranVm"/>, this file has no logic.</summary>
public partial class EkranSenzora : UserControl
{
    public EkranSenzora() => InitializeComponent();

    private void NaPomocnika(object sender, RoutedEventArgs e) =>
        _ = (Window.GetWindow(this)?.DataContext as ProzorVm)?.UkljuciPomocnikaAsync();

    private void NaSkrivene(object sender, RoutedEventArgs e)
    {
        if (DataContext is EkranVm ekran) ekran.SviSenzori = !ekran.SviSenzori;
    }
}

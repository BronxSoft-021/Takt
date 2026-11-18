using System.Windows;
using System.Windows.Controls;
using Takt.Vm;

namespace Takt.Prikaz;

/// <summary>The channel in solo: chart, threads and the table of the device's sensors. No logic here.</summary>
public partial class SoloPanel : UserControl
{
    public SoloPanel() => InitializeComponent();

    private void NaPomocnika(object sender, RoutedEventArgs e) =>
        _ = (Window.GetWindow(this)?.DataContext as ProzorVm)?.UkljuciPomocnikaAsync();

    private void NaSkrivene(object sender, RoutedEventArgs e)
    {
        if (DataContext is KanalVm kanal) kanal.Detalj.SviSenzori = !kanal.Detalj.SviSenzori;
    }
}

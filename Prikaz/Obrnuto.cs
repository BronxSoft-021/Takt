using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Takt.Prikaz;

/// <summary>True shows nothing, false shows everything - used for "the chart, unless the screen is empty".</summary>
public sealed class ObrnutaVidljivost : IValueConverter
{
    public object Convert(object? vrednost, Type tip, object? parametar, CultureInfo kultura) =>
        vrednost is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? vrednost, Type tip, object? parametar, CultureInfo kultura) =>
        vrednost is Visibility.Collapsed;
}

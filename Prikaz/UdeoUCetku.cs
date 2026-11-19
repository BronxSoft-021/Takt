using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Takt.Prikaz;

namespace Takt.Prikaz;

/// <summary>A share (0-1) as the meter's colour: used by the small bar of every thread.</summary>
public sealed class UdeoUCetku : IValueConverter
{
    public object Convert(object value, Type cilj, object parametar, CultureInfo kultura) =>
        VuMerac.CetkaZa(value is double udeo ? udeo : 0);

    public object ConvertBack(object value, Type cilj, object parametar, CultureInfo kultura) =>
        throw new NotSupportedException();
}

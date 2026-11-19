using System.Windows.Markup;

namespace Takt.Prikaz;

/// <summary>
/// A label in XAML in the chosen language: <c>Text="{prikaz:T 'Podešavanja'}"</c>. The Serbian text is the key and
/// also the fallback, so a forgotten line still reads. Evaluated once, when the window is built - the language is
/// chosen in the settings and takes effect at the next start.
/// </summary>
public sealed class T : MarkupExtension
{
    public T()
    {
    }

    public T(string tekst) => Tekst = tekst;

    public string Tekst { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider) => Takt.Tekst.T(Tekst);
}

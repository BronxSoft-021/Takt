using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Takt.Sistem;

/// <summary>
/// Remembers where the window was and puts it back there - but only if a screen still exists at that place.
/// A monitor unplugged since the last run would otherwise leave the window off-screen, looking like the program
/// did not start.
/// </summary>
public static class ProzorPamet
{
    /// <summary>The whole desktop as WPF measures it (DIPs).</summary>
    public static Rect Ekrani => new(
        SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

    /// <summary>
    /// The rectangle to restore, or null when it cannot be trusted (first run, too small, or off the screens).
    /// A window is kept only if a good part of it lands on the desktop.
    /// </summary>
    public static Rect? Sredi(Rect? sacuvan, Rect ekrani)
    {
        if (sacuvan is not { } okvir) return null;
        if (okvir.Width < 600 || okvir.Height < 400) return null;
        if (double.IsNaN(okvir.Left) || double.IsNaN(okvir.Top)) return null;

        var vidljiv = Rect.Intersect(okvir, ekrani);
        if (vidljiv.IsEmpty || vidljiv.Width < 200 || vidljiv.Height < 100) return null;

        return okvir;
    }

    public static void Primeni(Window prozor, Podesavanja podesavanja)
    {
        var sacuvan = new Rect(podesavanja.ProzorLevo, podesavanja.ProzorGore,
            podesavanja.ProzorSirina, podesavanja.ProzorVisina);
        if (sacuvan.Width <= 0 || sacuvan.Height <= 0) return;

        if (Sredi(sacuvan, Ekrani) is not { } okvir) return;

        prozor.WindowStartupLocation = WindowStartupLocation.Manual;
        prozor.Left = okvir.Left;
        prozor.Top = okvir.Top;
        prozor.Width = okvir.Width;
        prozor.Height = okvir.Height;

        if (podesavanja.ProzorStanje == "Maximized") prozor.WindowState = WindowState.Maximized;
    }

    /// <summary>Writes the current place into the settings (maximized remembers its restored size, not the screen).</summary>
    public static void Sacuvaj(Window prozor, Podesavanja podesavanja)
    {
        bool maksimizovan = Maksimizovan(prozor);

        var okvir = maksimizovan ? prozor.RestoreBounds : new Rect(prozor.Left, prozor.Top, prozor.Width, prozor.Height);

        // A window zoomed from outside has no restore bounds in WPF; its Left/Top/Width/Height are then still
        // the size before zooming, which is exactly what should be remembered.
        if (okvir.IsEmpty || okvir.Width < 200 || okvir.Height < 200)
            okvir = new Rect(prozor.Left, prozor.Top, prozor.Width, prozor.Height);
        if (double.IsNaN(okvir.Left) || double.IsNaN(okvir.Top)) return;

        podesavanja.ProzorLevo = Math.Round(okvir.Left);
        podesavanja.ProzorGore = Math.Round(okvir.Top);
        podesavanja.ProzorSirina = Math.Round(okvir.Width);
        podesavanja.ProzorVisina = Math.Round(okvir.Height);
        podesavanja.ProzorStanje = maksimizovan ? "Maximized" : "Normal";
    }

    /// <summary>
    /// WPF's WindowState is not always told: Aero Snap and another program's ShowWindow can zoom the window
    /// without the property changing, so the window itself is asked as well.
    /// </summary>
    private static bool Maksimizovan(Window prozor)
    {
        if (prozor.WindowState == WindowState.Maximized) return true;
        if (prozor.WindowState != WindowState.Normal) return false;

        try
        {
            var handle = new WindowInteropHelper(prozor).Handle;
            return handle != IntPtr.Zero && IsZoomed(handle);
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Provera maksimizovanog prozora", ex);
            return false;
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(IntPtr prozor);
}

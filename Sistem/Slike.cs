using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Takt.Vm;

namespace Takt.Sistem;

/// <summary>
/// Screenshot run: <c>Takt.exe --snimak &lt;folder&gt;</c>. Saves every screen as PNG and closes the program,
/// so the look can be checked without clicking through it (and without depending on this computer's hardware).
/// </summary>
public static class Slike
{
    public static async Task SacuvajSve(Window prozor, ProzorVm vm, string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            // Long enough for the real source to take a few readings, so the chart is not a single dot.
            await Task.Delay(2400);

            foreach (var ekran in vm.Ekrani.ToList())
            {
                vm.Ekran = ekran;
                await Task.Delay(400);
                Sacuvaj(prozor, Path.Combine(folder, $"{ekran.Grupa}.png"));
            }

            vm.PrikaziPodesavanja();
            await Task.Delay(400);
            Sacuvaj(prozor, Path.Combine(folder, "Podesavanja.png"));
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Snimak ekrana", ex);
        }
        finally
        {
            Application.Current.Shutdown();
        }
    }

    private static void Sacuvaj(Window prozor, string putanja)
    {
        prozor.UpdateLayout();

        // The window's content, not the window: the frame and title bar are drawn by Windows, and rendering
        // the Window itself leaves a white strip where they would be.
        if (prozor.Content is not FrameworkElement sadrzaj) return;

        int sirina = (int)Math.Ceiling(sadrzaj.ActualWidth);
        int visina = (int)Math.Ceiling(sadrzaj.ActualHeight);
        if (sirina < 10 || visina < 10) return;

        var slika = new RenderTargetBitmap(sirina, visina, 96, 96, PixelFormats.Pbgra32);
        var crtez = new DrawingVisual();
        using (var dc = crtez.RenderOpen())
        {
            dc.DrawRectangle(prozor.Background, null, new Rect(0, 0, sirina, visina));
            dc.DrawRectangle(new VisualBrush(sadrzaj), null, new Rect(0, 0, sirina, visina));
        }
        slika.Render(crtez);

        var koder = new PngBitmapEncoder();
        koder.Frames.Add(BitmapFrame.Create(slika));

        using var tok = File.Create(putanja);
        koder.Save(tok);
    }
}

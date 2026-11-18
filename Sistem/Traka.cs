using System.Drawing;
using System.Windows.Forms;

namespace Takt.Sistem;

/// <summary>
/// The icon by the clock: open the window, its settings, or quit. While the window is hidden this is the only
/// way back to the program, so it is made once at start and never recreated; if it cannot be made at all,
/// <see cref="App"/> keeps the old behaviour and X really closes the program.
/// </summary>
public sealed class Traka : IDisposable
{
    private readonly NotifyIcon _ikona;

    public Traka(string verzija)
    {
        var meni = new ContextMenuStrip();

        var otvori = new ToolStripMenuItem("Otvori Takt") { Font = new Font(meni.Font, System.Drawing.FontStyle.Bold) };
        otvori.Click += (_, _) => Otvori?.Invoke();

        var podesavanja = new ToolStripMenuItem("Podešavanja");
        podesavanja.Click += (_, _) => Podesavanja?.Invoke();

        var izlaz = new ToolStripMenuItem("Izlaz");
        izlaz.Click += (_, _) => Izlaz?.Invoke();

        meni.Items.AddRange([otvori, podesavanja, new ToolStripSeparator(), izlaz]);

        _ikona = new NotifyIcon
        {
            Icon = Ikona(),
            Text = $"BronxSoft Takt {verzija}",
            Visible = true,
            ContextMenuStrip = meni,
        };
        _ikona.DoubleClick += (_, _) => Otvori?.Invoke();
    }

    public event Action? Otvori;
    public event Action? Podesavanja;
    public event Action? Izlaz;

    /// <summary>A short message from the tray ("program is still running"), once.</summary>
    public void Reci(string naslov, string tekst)
    {
        try
        {
            _ikona.ShowBalloonTip(4000, naslov, tekst, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Balon u traci", ex);
        }
    }

    private static Icon Ikona()
    {
        try
        {
            // The icon already built into the .exe (assets/app.ico), so there is one icon, not two.
            if (Environment.ProcessPath is { } putanja && Icon.ExtractAssociatedIcon(putanja) is { } ikona) return ikona;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Ikonica u traci", ex);
        }

        return SystemIcons.Application;
    }

    public void Dispose()
    {
        try
        {
            _ikona.Visible = false;
            _ikona.Dispose();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Zatvaranje ikonice u traci", ex);
        }
    }
}

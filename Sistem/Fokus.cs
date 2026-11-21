using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Takt.Sistem;

/// <summary>
/// Gives the focus back to the game after the Scene takes the screen. Without it the game would stay behind and
/// minimised, which is worse than a Scene that does not open: a game that loses focus stops drawing, and then the
/// frames, the session and the whole point of the Scene go with it.
/// </summary>
public static class Fokus
{
    public static bool Na(int pid)
    {
        try
        {
            using var proces = Process.GetProcessById(pid);
            var prozor = proces.MainWindowHandle;
            if (prozor == IntPtr.Zero) return false;
            return SetForegroundWindow(prozor);
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Vraćanje fokusa igri", ex);
            return false;
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr prozor);
}

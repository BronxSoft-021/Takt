using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Takt.Sistem;

/// <summary>
/// Which of the programs drawing frames is "the game". The service sees every program that presents frames;
/// the window knows which one the user is looking at. The game is the program in the foreground that draws at
/// least a few frames a second and is not one of the everyday ones (browsers, Takt itself, Windows' own) -
/// unless it covers its whole screen, which a browser playing a game full-screen also may.
/// With two screens the user looks at Takt on the other one: clicking Takt does not end the game, the last
/// game stays until it stops drawing.
/// </summary>
public sealed class BiracIgre
{
    private static readonly HashSet<string> Svakodnevni = new(StringComparer.OrdinalIgnoreCase)
    {
        "Takt", "TaktSluzba", "dwm", "explorer", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost",
        "TextInputHost", "ApplicationFrameHost", "msedge", "msedgewebview2", "chrome", "firefox", "opera", "brave",
        "vivaldi", "Claude", "Discord", "Spotify", "Teams", "steamwebhelper", "obs64",
    };

    private int? _pid;

    /// <summary>The game among <paramref name="igre"/>, or null. Called once per reading.</summary>
    public Protokol.Igra? Izaberi(Protokol.Igra[] igre) =>
        Izaberi(igre, Prednji(), CeoEkran);

    /// <summary>The decision without Windows, for a test: who is in front, and whether a program fills its screen.</summary>
    public Protokol.Igra? Izaberi(Protokol.Igra[] igre, int? prednji, Func<int, bool> ceoEkran)
    {
        // The game we already have keeps its place while it still draws, whoever is in front.
        if (_pid is { } staro && igre.FirstOrDefault(i => i.Pid == staro) is { } ista && ista.Fps >= 1)
        {
            // ... unless another program came to the front and plays: then that one is the game now.
            if (prednji is { } p && p != staro && igre.FirstOrDefault(i => i.Pid == p) is { } nova && JeIgra(nova, ceoEkran))
                return Uzmi(nova);
            return ista;
        }

        _pid = null;
        if (prednji is { } pid && igre.FirstOrDefault(i => i.Pid == pid) is { } prednja && JeIgra(prednja, ceoEkran))
            return Uzmi(prednja);

        return null;
    }

    private Protokol.Igra Uzmi(Protokol.Igra igra)
    {
        _pid = igra.Pid;
        return igra;
    }

    private static bool JeIgra(Protokol.Igra igra, Func<int, bool> ceoEkran) =>
        igra.Fps >= 5 && (!Svakodnevni.Contains(igra.Ime) || ceoEkran(igra.Pid));

    private static int? Prednji()
    {
        try
        {
            var prozor = GetForegroundWindow();
            if (prozor == IntPtr.Zero) return null;
            GetWindowThreadProcessId(prozor, out uint pid);
            return pid == 0 ? null : (int)pid;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>True when the program's front window covers the whole monitor it is on (borderless or exclusive full screen).</summary>
    private static bool CeoEkran(int pid)
    {
        try
        {
            var prozor = GetForegroundWindow();
            GetWindowThreadProcessId(prozor, out uint vlasnik);
            if (vlasnik != pid || !GetWindowRect(prozor, out var okvir)) return false;

            var monitor = MonitorFromWindow(prozor, 2 /* MONITOR_DEFAULTTONEAREST */);
            var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info)) return false;

            return okvir.Left <= info.rcMonitor.Left && okvir.Top <= info.rcMonitor.Top &&
                   okvir.Right >= info.rcMonitor.Right && okvir.Bottom >= info.rcMonitor.Bottom;
        }
        catch
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Pravougaonik { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Pravougaonik rcMonitor;
        public Pravougaonik rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr prozor, out uint pid);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr prozor, out Pravougaonik okvir);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr prozor, uint zastavice);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}

using System.Management;
using System.Runtime.InteropServices;
using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// RAM without a driver: Windows itself knows how much is used, free, committed and paged. The memory speed
/// comes from SMBIOS through WMI and never changes, so it is read once. Nothing here needs administrator.
/// </summary>
public sealed class RamIzvor : IIzvorSenzora
{
    private readonly List<Senzor> _senzori = [];
    private bool _brzinaProcitana;
    private double? _brzina;

    public RamIzvor()
    {
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Zauzeće"), Jedinica.Procenat));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Zauzeto"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Slobodno"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Ukupno"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Commit"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Pagefile (procena)"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Brzina memorije"), Jedinica.Megaherc));
    }

    public string Naziv => "Memorija";
    public bool Pravi => true;
    public IReadOnlyList<Senzor> Senzori => _senzori;
    public string? Poruka => null;

    public Snimak Uzmi()
    {
        var vrednosti = new double?[_senzori.Count];

        if (!_brzinaProcitana)
        {
            _brzinaProcitana = true;
            _brzina = Brzina();
        }

        long ukupno = 0, slobodno = 0, commit = 0, stranica = 0;

        var status = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (GlobalMemoryStatusEx(ref status))
        {
            ukupno = (long)(status.ullTotalPhys / 1_048_576);
            slobodno = (long)(status.ullAvailPhys / 1_048_576);
            vrednosti[0] = status.dwMemoryLoad;
        }

        var info = new PerformanceInformation { cb = Marshal.SizeOf<PerformanceInformation>() };
        if (GetPerformanceInfo(ref info, info.cb))
        {
            stranica = info.PageSize.ToInt64();
            if (stranica > 0) commit = info.CommitTotal.ToInt64() * stranica / 1_048_576;
        }

        vrednosti[1] = ukupno > 0 ? ukupno - slobodno : null;
        vrednosti[2] = ukupno > 0 ? slobodno : null;
        vrednosti[3] = ukupno > 0 ? ukupno : null;
        vrednosti[4] = commit > 0 ? commit : null;

        // Pagefile is what is committed but no longer fits in the memory in use. An estimate, and it is labelled so.
        vrednosti[5] = commit > 0 && ukupno > 0 ? Math.Max(0, commit - (ukupno - slobodno)) : null;
        vrednosti[6] = _brzina;

        return new Snimak(DateTime.Now, vrednosti);
    }

    private static double? Brzina()
    {
        try
        {
            using var trazio = new ManagementObjectSearcher("select Speed from Win32_PhysicalMemory");
            double? najveca = null;

            foreach (var objekat in trazio.Get())
            {
                using (objekat)
                {
                    if (objekat["Speed"] is not { } vrednost) continue;
                    if (!double.TryParse(vrednost.ToString(), out double brzina) || brzina <= 0) continue;
                    if (najveca is null || brzina > najveca) najveca = brzina;
                }
            }

            return najveca;
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Brzina memorije", ex);
            return null;
        }
    }

    public void Dispose() { }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PerformanceInformation
    {
        public int cb;
        public IntPtr CommitTotal;
        public IntPtr CommitLimit;
        public IntPtr CommitPeak;
        public IntPtr PhysicalTotal;
        public IntPtr PhysicalAvailable;
        public IntPtr SystemCache;
        public IntPtr KernelTotal;
        public IntPtr KernelPaged;
        public IntPtr KernelNonpaged;
        public IntPtr PageSize;
        public int HandleCount;
        public int ProcessCount;
        public int ThreadCount;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool GetPerformanceInfo(ref PerformanceInformation informacije, int velicina);
}

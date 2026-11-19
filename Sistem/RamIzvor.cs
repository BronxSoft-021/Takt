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
        // Made-up but stable keys ("ram/0/…"): the name beside them may be translated without touching history.
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Zauzeće", "ram/0/zauzece"), Jedinica.Procenat));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Zauzeto", "ram/0/zauzeto"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Slobodno", "ram/0/slobodno"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Ukupno", "ram/0/ukupno"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Commit", "ram/0/commit"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Pagefile (procena)", "ram/0/pagefile"), Jedinica.Megabajt));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Brzina memorije", "ram/0/brzina"), Jedinica.Megaherc));
        _senzori.Add(new Senzor(new SenzorKljuc(Grupa.Memorija, "Računar", "Zauzeće commit-a", "ram/0/commit-procenat"), Jedinica.Procenat));
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

        long ukupno = 0, slobodno = 0, commit = 0, granica = 0, stranica = 0;

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
            if (stranica > 0)
            {
                commit = info.CommitTotal.ToInt64() * stranica / 1_048_576;
                granica = info.CommitLimit.ToInt64() * stranica / 1_048_576;
            }
        }

        vrednosti[1] = ukupno > 0 ? ukupno - slobodno : null;
        vrednosti[2] = ukupno > 0 ? slobodno : null;
        vrednosti[3] = ukupno > 0 ? ukupno : null;
        vrednosti[4] = commit > 0 ? commit : null;

        // Pagefile is what is committed but no longer fits in the memory in use. An estimate, and it is labelled so.
        vrednosti[5] = commit > 0 && ukupno > 0 ? Math.Max(0, commit - (ukupno - slobodno)) : null;
        vrednosti[6] = _brzina;

        // Commit against its limit (RAM + pagefile): the second meter of the RAM channel. Windows' own numbers.
        vrednosti[7] = commit > 0 && granica > 0 ? Math.Round(commit * 100.0 / granica, 0) : null;

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

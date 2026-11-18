using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Merenje;

namespace Takt.Vm;

/// <summary>What one game session was like, as a person reads it afterwards.</summary>
public sealed record IzvestajSesije(
    string Igra,
    DateTime Pocetak,
    double TrajanjeSekundi,
    double ProsekFps,
    double Low1,
    int Stucanja,
    double? NajCpu, DateTime? NajCpuKada,
    double? NajGpu, DateTime? NajGpuKada,
    double? NajHotspot,
    int VramPunSekundi,
    string? NajJezgro, double NajJezgroProcenat,
    int JezgroNa100Sekundi)
{
    public string TrajanjeTekst => Trajanje(TrajanjeSekundi);
    public string Naslov => $"{Igra} · {Pocetak:d. M. yyyy. HH:mm}";
    public string Kratko => $"{TrajanjeTekst} · prosek {ProsekFps:0} FPS · 1% low {Low1:0}";

    /// <summary>The report line by line, in plain words.</summary>
    public IReadOnlyList<string> Redovi
    {
        get
        {
            var redovi = new List<string>
            {
                $"Trajanje: {TrajanjeTekst}, od {Pocetak:HH:mm}",
                $"Prosečno {ProsekFps:0} FPS, 1% low {Low1:0} FPS",
                Stucanja == 0 ? "Bez štucanja" : $"Štucanja: {Stucanja}",
            };
            if (NajCpu is { } c) redovi.Add($"Procesor najtopliji {c:0.0} °C u {NajCpuKada:HH:mm}");
            if (NajGpu is { } g) redovi.Add($"Grafička najtoplija {g:0.0} °C u {NajGpuKada:HH:mm}" +
                                            (NajHotspot is { } h ? $" (hotspot {h:0.0} °C)" : ""));
            redovi.Add(VramPunSekundi > 0
                ? $"VRAM pun (95 % i više): {Trajanje(VramPunSekundi)} - tada igra najlakše štuca"
                : "VRAM nijednom pun");
            if (NajJezgro is not null)
                redovi.Add($"Najopterećenije: {NajJezgro} do {NajJezgroProcenat:0} %" +
                           (JezgroNa100Sekundi > 0 ? $", na 100 % ukupno {Trajanje(JezgroNa100Sekundi)} (usko grlo)" : ""));
            return redovi;
        }
    }

    public static string Trajanje(double sekundi)
    {
        var t = TimeSpan.FromSeconds(Math.Round(sekundi));
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours} h {t.Minutes} min";
        if (t.TotalMinutes >= 1) return $"{t.Minutes} min {t.Seconds} s";
        return $"{t.Seconds} s";
    }
}

/// <summary>
/// Records a game session on its own: it starts when a game has been on screen for ten seconds and ends fifteen
/// seconds after it stops drawing (a loading screen or an Alt+Tab does not split it). Sessions shorter than a
/// minute are not kept. The report is saved as JSON in the data folder, under "sesije".
/// </summary>
public sealed class SnimacSesije : ObservableObject
{
    public static readonly TimeSpan Pocni = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan Zavrsi = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan Najkraca = TimeSpan.FromSeconds(60);

    private DateTime? _kandidatOd;
    private string? _igra;
    private DateTime _pocetak, _poslednje;
    private double _zbirFps;
    private int _uzoraka, _stucanja, _vramPun, _jezgroNa100;
    private readonly List<double> _low = [];
    private double? _najCpu, _najGpu, _najHotspot;
    private DateTime? _najCpuKada, _najGpuKada;
    private string? _najJezgro;
    private double _najJezgroProcenat;

    private readonly string _folder;

    public SnimacSesije(string? folder = null) => _folder = folder ?? Path.Combine(Putanje.Podaci, "sesije");

    public bool Snima => _igra is not null;

    public string Folder => _folder;

    /// <summary>Raised when a session ends and is saved; the window says so from the tray.</summary>
    public event Action<IzvestajSesije>? Zavrsena;

    /// <summary>One reading.</summary>
    public void Uzmi(IgraVm igra, IEnumerable<KanalVm> kanali, DateTime sada)
    {
        if (igra.Aktivna && igra.Fps is { } fps)
        {
            if (_igra is not null && _igra != igra.Ime) Zavrsi_(sada);

            if (_igra is null)
            {
                _kandidatOd ??= sada;
                if (sada - _kandidatOd < Pocni) return;

                _igra = igra.Ime;
                _pocetak = _kandidatOd.Value;
                OnPropertyChanged(nameof(Snima));
            }

            _poslednje = sada;
            Dodaj(fps, igra, kanali, sada);
            igra.Sesija = $"snima sesiju {Formatiraj(sada - _pocetak)}";
            return;
        }

        _kandidatOd = null;
        if (_igra is not null && sada - _poslednje >= Zavrsi) Zavrsi_(sada);
    }

    private void Dodaj(double fps, IgraVm igra, IEnumerable<KanalVm> kanali, DateTime sada)
    {
        _uzoraka++;
        _zbirFps += fps;
        if (igra.Low1 is { } low) _low.Add(low);
        _stucanja += igra.PoslednjaStucanja;

        foreach (var kanal in kanali)
        {
            var senzori = kanal.Detalj.Senzori;
            switch (kanal.Detalj.Grupa)
            {
                case Grupa.Procesor:
                    if (kanal.Desni?.Senzor.Broj is { } c && (_najCpu is null || c > _najCpu)) { _najCpu = c; _najCpuKada = sada; }
                    if (kanal.Jezgra.Where(j => j.Broj is not null).MaxBy(j => j.Broj) is { Broj: { } p } jezgro)
                    {
                        if (p > _najJezgroProcenat) { _najJezgroProcenat = p; _najJezgro = KanalVm.ImeJezgra(jezgro.Ime); }
                        if (p >= 99) _jezgroNa100++;
                    }
                    break;

                case Grupa.Grafika:
                    if (kanal.Desni?.Senzor.Broj is { } g && (_najGpu is null || g > _najGpu)) { _najGpu = g; _najGpuKada = sada; }
                    if (senzori.FirstOrDefault(s => s.Ime == "Hotspot")?.Broj is { } h && (_najHotspot is null || h > _najHotspot)) _najHotspot = h;
                    if (senzori.FirstOrDefault(s => s.Ime == "Zauzeće VRAM")?.Broj >= 95) _vramPun++;
                    break;
            }
        }
    }

    private void Zavrsi_(DateTime sada)
    {
        if (_igra is null) return;

        var trajanje = _poslednje - _pocetak;
        if (trajanje >= Najkraca && _uzoraka > 0)
        {
            _low.Sort();
            var izvestaj = new IzvestajSesije(
                _igra, _pocetak, trajanje.TotalSeconds,
                _zbirFps / _uzoraka,
                _low.Count > 0 ? _low[(int)(_low.Count * 0.05)] : 0,   // the 5th percentile of the per-second 1 % lows
                _stucanja,
                _najCpu, _najCpuKada, _najGpu, _najGpuKada, _najHotspot,
                _vramPun, _najJezgro, _najJezgroProcenat, _jezgroNa100);

            Sacuvaj(izvestaj);
            Zavrsena?.Invoke(izvestaj);
        }

        _igra = null;
        _kandidatOd = null;
        _zbirFps = 0;
        _uzoraka = _stucanja = _vramPun = _jezgroNa100 = 0;
        _low.Clear();
        _najCpu = _najGpu = _najHotspot = null;
        _najCpuKada = _najGpuKada = null;
        _najJezgro = null;
        _najJezgroProcenat = 0;
        OnPropertyChanged(nameof(Snima));
    }

    private void Sacuvaj(IzvestajSesije izvestaj)
    {
        try
        {
            Directory.CreateDirectory(_folder);
            var ime = string.Concat(izvestaj.Igra.Select(z => Path.GetInvalidFileNameChars().Contains(z) ? '_' : z));
            var fajl = Path.Combine(_folder, $"{izvestaj.Pocetak:yyyy-MM-dd_HH-mm}_{ime}.json");
            File.WriteAllText(fajl, JsonSerializer.Serialize(izvestaj, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Čuvanje sesije", ex);
        }
    }

    /// <summary>Every saved session, newest first.</summary>
    public IReadOnlyList<IzvestajSesije> Ucitaj()
    {
        var sesije = new List<IzvestajSesije>();
        try
        {
            if (!Directory.Exists(_folder)) return sesije;
            foreach (var fajl in Directory.EnumerateFiles(_folder, "*.json"))
            {
                try
                {
                    if (JsonSerializer.Deserialize<IzvestajSesije>(File.ReadAllText(fajl)) is { } s) sesije.Add(s);
                }
                catch (Exception ex) when (ex is JsonException or IOException)
                {
                    Dnevnik.Greska("Čitanje sesije " + Path.GetFileName(fajl), ex);
                }
            }
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Čitanje sesija", ex);
        }
        return sesije.OrderByDescending(s => s.Pocetak).ToList();
    }

    private static string Formatiraj(TimeSpan t) => $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
}

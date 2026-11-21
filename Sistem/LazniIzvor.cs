using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// Made-up but plausible readings: slow waves plus a little noise. It exists so the program can be seen and
/// tested without the driver, and so a screenshot test never depends on this computer's hardware.
/// It is always labelled "Probni podaci" - it must never look like a real measurement.
/// </summary>
public sealed class LazniIzvor : IIzvorSenzora
{
    private readonly List<Senzor> _senzori = [];
    private readonly double[] _osnova;
    private readonly double[] _amplituda;
    private readonly double[] _perioda;
    private readonly double[] _faza;
    private readonly Random _slucajno = new(20261117);
    private int _tik;

    public LazniIzvor()
    {
        // group, device, name, unit, base, amplitude, period in samples
        var tabela = new (Grupa Grupa, string Uredjaj, string Ime, Jedinica Jedinica, double Osnova, double Amplituda, double Perioda)[]
        {
            (Grupa.Procesor, Takt.Tekst.T("Procesor"), "Temperatura jezgra 1", Jedinica.Celzijus, 48, 12, 90),
            (Grupa.Procesor, Takt.Tekst.T("Procesor"), "Temperatura jezgra 2", Jedinica.Celzijus, 47, 11, 90),
            (Grupa.Procesor, Takt.Tekst.T("Procesor"), "Temperatura jezgra 3", Jedinica.Celzijus, 50, 13, 90),
            (Grupa.Procesor, Takt.Tekst.T("Procesor"), "Temperatura jezgra 4", Jedinica.Celzijus, 49, 12, 90),
            (Grupa.Procesor, "Procesor", "Opterećenje procesora", Jedinica.Procenat, 35, 30, 45),
            (Grupa.Procesor, "Procesor", "Prosečan takt", Jedinica.Megaherc, 4200, 600, 30),
            (Grupa.Procesor, "Procesor", "Potrošnja paketa", Jedinica.Vat, 85, 40, 60),
            (Grupa.Procesor, "Procesor", "Napon procesora", Jedinica.Volt, 1.28, 0.09, 50),
            (Grupa.Procesor, "Procesor", "Thermal throttling", Jedinica.Procenat, 0, 0, 1),

            (Grupa.Grafika, "Grafička karta", "Temperatura", Jedinica.Celzijus, 62, 8, 120),
            (Grupa.Grafika, "Grafička karta", "Hotspot", Jedinica.Celzijus, 74, 9, 120),
            (Grupa.Grafika, "Grafička karta", "Zauzeće VRAM", Jedinica.Procenat, 55, 20, 100),
            (Grupa.Grafika, "Grafička karta", "Opterećenje", Jedinica.Procenat, 60, 35, 70),
            (Grupa.Grafika, "Grafička karta", "Takt jezgra", Jedinica.Megaherc, 1850, 150, 40),
            (Grupa.Grafika, "Grafička karta", "Takt memorije", Jedinica.Megaherc, 7000, 50, 80),
            (Grupa.Grafika, "Grafička karta", "Potrošnja", Jedinica.Vat, 180, 90, 65),
            (Grupa.Grafika, "Grafička karta", "Ventilator", Jedinica.Obrtaj, 1350, 450, 75),

            (Grupa.Memorija, "Memorija", "Zauzeće", Jedinica.Procenat, 48, 6, 200),
            (Grupa.Memorija, "Memorija", "Slobodno", Jedinica.Megabajt, 8200, 900, 200),
            (Grupa.Memorija, "Memorija", "Brzina memorije", Jedinica.Megaherc, 3200, 0, 1),
            (Grupa.Memorija, "Memorija", "Zauzeće commit-a", Jedinica.Procenat, 57, 7, 200),

            (Grupa.Diskovi, "NVMe SSD", "Temperatura", Jedinica.Celzijus, 41, 4, 150),
            (Grupa.Diskovi, "NVMe SSD", "Aktivnost", Jedinica.Procenat, 12, 11, 25),
            (Grupa.Diskovi, "SATA SSD", "Temperatura", Jedinica.Celzijus, 34, 2, 170),
            (Grupa.Diskovi, "SATA SSD", "Aktivnost", Jedinica.Procenat, 4, 4, 40),
            (Grupa.Diskovi, "HDD 2 TB", "Temperatura", Jedinica.Celzijus, 37, 1, 200),
            (Grupa.Diskovi, "NVMe SSD", "Zdravlje", Jedinica.Procenat, 98, 0, 1),
            (Grupa.Diskovi, "NVMe SSD", "Životni vek (preostalo)", Jedinica.Procenat, 96, 0, 1),

            (Grupa.Hladjenje, "Matična ploča", "Ventilator kućišta", Jedinica.Obrtaj, 850, 120, 65),
            (Grupa.Hladjenje, "Matična ploča", "Ventilator procesora", Jedinica.Obrtaj, 1100, 300, 70),
        };

        _osnova = new double[tabela.Length];
        _amplituda = new double[tabela.Length];
        _perioda = new double[tabela.Length];
        _faza = new double[tabela.Length];

        for (int i = 0; i < tabela.Length; i++)
        {
            var t = tabela[i];
            _senzori.Add(new Senzor(new SenzorKljuc(t.Grupa, t.Uredjaj, t.Ime), t.Jedinica));
            _osnova[i] = t.Osnova;
            _amplituda[i] = t.Amplituda;
            _perioda[i] = Math.Max(1, t.Perioda);
            _faza[i] = i * 0.7;
        }
    }

    public string Naziv => "Probni podaci";

    public bool Pravi => false;

    public IReadOnlyList<Senzor> Senzori => _senzori;

    public string? Poruka => null;

    public Snimak Uzmi()
    {
        int tik = _tik++;
        var vrednosti = new double?[_senzori.Count];

        for (int i = 0; i < vrednosti.Length; i++)
        {
            double vrednost = _osnova[i] + _amplituda[i] * Math.Sin(2 * Math.PI * tik / _perioda[i] + _faza[i]);
            if (_amplituda[i] > 0)
                vrednost += (_slucajno.NextDouble() - 0.5) * _amplituda[i] * 0.06;
            vrednosti[i] = Math.Round(Math.Max(0, vrednost), 3);
        }

        return new Snimak(DateTime.Now, vrednosti);
    }

    public void Dispose() { }
}

using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// Two sources shown as one: the hardware readings and the things Windows knows by itself. The merged list
/// keeps a fixed order, so an index into a reading always means the same sensor.
/// </summary>
public sealed class SlozeniIzvor : IIzvorSenzora
{
    private readonly IIzvorSenzora[] _delovi;
    private readonly List<Senzor> _senzori = [];
    private readonly List<(int Deo, int Lokalni)> _mapa = [];

    public SlozeniIzvor(params IIzvorSenzora[] delovi)
    {
        _delovi = delovi;

        // One sensor shown once: the same group, name and unit from two sources would be the same number twice on
        // screen (memory from the library and from Windows is the case that happens). The first source wins, and
        // the order of the merged list stays fixed, so an index into a reading always means the same sensor.
        var vidjeni = new HashSet<(Grupa, string, Jedinica)>();

        for (int deo = 0; deo < delovi.Length; deo++)
        {
            var izvor = delovi[deo];
            for (int i = 0; i < izvor.Senzori.Count; i++)
            {
                var senzor = izvor.Senzori[i];
                if (!vidjeni.Add((senzor.Grupa, senzor.Ime, senzor.Jedinica))) continue;

                _senzori.Add(senzor);
                _mapa.Add((deo, i));
            }
        }
    }

    public string Naziv => string.Join(" + ", _delovi.Select(d => d.Naziv));

    public bool Pravi => _delovi.All(d => d.Pravi);

    public IReadOnlyList<Senzor> Senzori => _senzori;

    public string? Poruka => _delovi.Select(d => d.Poruka).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

    public Snimak Uzmi()
    {
        var snimci = new Snimak[_delovi.Length];
        for (int deo = 0; deo < _delovi.Length; deo++)
            snimci[deo] = _delovi[deo].Uzmi();

        var vrednosti = new double?[_senzori.Count];
        for (int i = 0; i < vrednosti.Length; i++)
        {
            var (deo, lokalni) = _mapa[i];
            vrednosti[i] = snimci[deo].Ovo(lokalni);
        }

        return new Snimak(DateTime.Now, vrednosti);
    }

    public void Dispose()
    {
        foreach (var deo in _delovi)
        {
            try
            {
                deo.Dispose();
            }
            catch (Exception ex)
            {
                Dnevnik.Greska("Zatvaranje izvora " + deo.Naziv, ex);
            }
        }
    }
}

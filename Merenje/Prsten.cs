namespace Takt.Merenje;

/// <summary>
/// Last N values of one sensor, oldest first. A ring: when it is full the oldest value is overwritten,
/// and a missing value (null) stays a hole so the chart can break the line instead of inventing a slope.
/// </summary>
public sealed class Prsten
{
    private readonly double?[] _vrednosti;
    private readonly object _brava = new();
    private int _pocetak;
    private int _broj;

    public Prsten(int kapacitet)
    {
        if (kapacitet < 1) throw new ArgumentOutOfRangeException(nameof(kapacitet), "Kapacitet mora biti bar 1.");
        _vrednosti = new double?[kapacitet];
    }

    public int Kapacitet => _vrednosti.Length;

    public int Broj
    {
        get { lock (_brava) return _broj; }
    }

    public void Dodaj(double? vrednost)
    {
        lock (_brava)
        {
            if (_broj < _vrednosti.Length)
            {
                _vrednosti[(_pocetak + _broj) % _vrednosti.Length] = vrednost;
                _broj++;
            }
            else
            {
                _vrednosti[_pocetak] = vrednost;
                _pocetak = (_pocetak + 1) % _vrednosti.Length;
            }
        }
    }

    /// <summary>Last n values, oldest first. Fewer if the ring does not hold that many yet.</summary>
    public double?[] Poslednjih(int n)
    {
        lock (_brava)
        {
            if (n <= 0 || _broj == 0) return [];
            if (n > _broj) n = _broj;

            var rezultat = new double?[n];
            int pomeraj = _broj - n;
            for (int i = 0; i < n; i++)
                rezultat[i] = _vrednosti[(_pocetak + pomeraj + i) % _vrednosti.Length];
            return rezultat;
        }
    }

    public double? Poslednja
    {
        get
        {
            lock (_brava)
            {
                if (_broj == 0) return null;
                return _vrednosti[(_pocetak + _broj - 1) % _vrednosti.Length];
            }
        }
    }

    /// <summary>Smallest and largest real value in the ring, ignoring holes.</summary>
    public (double? Najmanje, double? Najvise) Raspon()
    {
        double? najmanje = null, najvise = null;
        foreach (var v in Poslednjih(int.MaxValue))
        {
            if (v is not { } x) continue;
            if (najmanje is null || x < najmanje.Value) najmanje = x;
            if (najvise is null || x > najvise.Value) najvise = x;
        }
        return (najmanje, najvise);
    }

    public void Ocisti()
    {
        lock (_brava)
        {
            Array.Clear(_vrednosti);
            _pocetak = 0;
            _broj = 0;
        }
    }
}

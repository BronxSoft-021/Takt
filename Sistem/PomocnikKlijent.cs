using System.IO.Pipes;
using System.Text;
using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// The window's side of the helper: a read-only view of the sensors another process reads as administrator.
/// Connecting is quick and may fail (no helper running), in which case the caller falls back to reading
/// whatever this account can read by itself.
/// </summary>
public sealed class PomocnikKlijent : IIzvorSenzora
{
    private readonly NamedPipeClientStream _cev;
    private readonly StreamReader _citalac;
    private readonly List<Senzor> _senzori = [];
    private readonly object _brava = new();
    private Snimak? _poslednji;
    private string? _poruka;
    private volatile bool _radi = true;

    private PomocnikKlijent(NamedPipeClientStream cev, StreamReader citalac, List<Senzor> senzori, string? izvor)
    {
        _cev = cev;
        _citalac = citalac;
        _senzori = senzori;
        Izvor = izvor;
    }

    /// <summary>"usluga" when a service serves, "pomocnik" for the temporary helper, null for an old build.</summary>
    public string? Izvor { get; }

    /// <summary>The service writes the history itself; when it serves, the window must not write it a second time.</summary>
    public bool JeUsluga => Izvor == Protokol.Usluga;

    public string Naziv => JeUsluga ? "Pravi senzori (usluga)" : "Pravi senzori (pomoćnik)";
    public bool Pravi => true;
    public IReadOnlyList<Senzor> Senzori => _senzori;

    public string? Poruka
    {
        get { lock (_brava) return _poruka; }
    }

    /// <summary>Tries to reach a running helper. Null when there is none - that is not an error.</summary>
    public static PomocnikKlijent? Probaj(int cekanjeMs = 300)
    {
        NamedPipeClientStream? cev = null;

        try
        {
            cev = new NamedPipeClientStream(".", Cev.Ime, PipeDirection.In);
            cev.Connect(cekanjeMs);

            var citalac = new StreamReader(cev, new UTF8Encoding(false));
            var prva = citalac.ReadLine();
            var poruka = prva is null ? null : Protokol.Citaj(prva);

            if (poruka is not { Vrsta: Protokol.SpisakVrsta, Senzori: { } opisi })
            {
                cev.Dispose();
                return null;
            }

            var senzori = new List<Senzor>(opisi.Length);
            foreach (var opis in opisi)
            {
                if (!Enum.TryParse<Grupa>(opis.Grupa, out var grupa))
                {
                    Dnevnik.Upisi("Pomoćnik: nepoznata grupa " + opis.Grupa);
                    continue;
                }
                if (!Enum.TryParse<Jedinica>(opis.Jedinica, out var jedinica)) jedinica = Jedinica.Nepoznato;

                senzori.Add(new Senzor(new SenzorKljuc(grupa, opis.Uredjaj, opis.Ime), jedinica));
            }

            var klijent = new PomocnikKlijent(cev, citalac, senzori, poruka.Izvor);
            klijent.PokreniCitanje();
            return klijent;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException
                                       or InvalidOperationException)
        {
            cev?.Dispose();
            return null;
        }
    }

    public Snimak Uzmi()
    {
        lock (_brava)
            return _poslednji ?? new Snimak(DateTime.Now, new double?[_senzori.Count]);
    }

    private void PokreniCitanje()
    {
        var nit = new Thread(Citaj)
        {
            IsBackground = true,
            Name = "Takt pomoćnik",
        };
        nit.Start();
    }

    private void Citaj()
    {
        try
        {
            while (_radi)
            {
                var linija = _citalac.ReadLine();
                if (linija is null) break;

                var poruka = Protokol.Citaj(linija);
                if (poruka is null) continue;

                switch (poruka.Vrsta)
                {
                    case Protokol.UzorakVrsta when poruka.Vrednosti is { } vrednosti:
                        var vreme = DateTimeOffset.FromUnixTimeMilliseconds(poruka.Vreme).LocalDateTime;
                        lock (_brava) _poslednji = new Snimak(vreme, vrednosti);
                        break;

                    case Protokol.GreskaVrsta:
                        lock (_brava) _poruka = poruka.Greska;
                        break;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // The helper stopped or we closed the pipe first.
        }
        finally
        {
            lock (_brava)
                _poruka ??= "Pomoćnik je prestao da radi. Zatvori i otvori program da ga pokreneš ponovo.";
        }
    }

    public void Dispose()
    {
        _radi = false;
        try
        {
            _cev.Dispose();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Zatvaranje cevi pomoćnika", ex);
        }
    }
}

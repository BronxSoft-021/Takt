using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Sistem;

namespace Takt.Vm;

/// <summary>
/// The settings screen. The refresh rate and the number of days apply at once; the rest (history on or off,
/// probe data) waits for the next start - opening the sensors twice in one run would mean two drivers.
/// "Sačuvaj" writes everything to podesavanja.json.
/// </summary>
public sealed record OpcijaMetra(string Vrednost, string Naziv);

public sealed class PodesavanjaVm : ObservableObject
{
    private readonly Sistem.Podesavanja _podesavanja;
    private readonly Func<string> _obrisiIstoriju;
    private readonly Action<int> _promeniOsvezavanje;
    private string _poruka = "";
    private bool _potvrdaBrisanja;

    /// <param name="obrisiIstoriju">Deletes the history and says what happened (now, or "the service will").</param>
    public PodesavanjaVm(Sistem.Podesavanja podesavanja, Func<string> obrisiIstoriju, Action<int> promeniOsvezavanje)
    {
        _podesavanja = podesavanja;
        _obrisiIstoriju = obrisiIstoriju;
        _promeniOsvezavanje = promeniOsvezavanje;
    }

    public int[] OpcijeOsvezavanja { get; } = [1, 2, 5];
    public int[] OpcijeDana { get; } = [1, 7, 30, 90];

    /// <summary>The three quantities the drive strip can show, each with its name in the chosen language.</summary>
    public OpcijaMetra[] OpcijeMetra { get; } =
    [
        new("Temperatura", Tekst.T("Temperatura")),
        new("Zauzeće", Tekst.T("Zauzeće")),
        new("Životni vek", Tekst.T("Životni vek")),
    ];

    public OpcijaMetra? IzabraniMetar
    {
        get => OpcijeMetra.FirstOrDefault(o => o.Vrednost == _podesavanja.DiskMetar) ?? OpcijeMetra[0];
        set
        {
            if (value is not null) DiskMetar = value.Vrednost;
        }
    }

    /// <summary>
    /// What the drive strip measures. One quantity for all drives: a strip mixing "99 %" and "45 °C" bars cannot be
    /// read (a report called it a mishmash, 19. 11. 2026). Takes effect at the next start, like the language.
    /// </summary>
    public string DiskMetar
    {
        get => _podesavanja.DiskMetar;
        set
        {
            if (value is null || _podesavanja.DiskMetar == value) return;

            _podesavanja.DiskMetar = value;
            _podesavanja.Sacuvaj();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IzabraniMetar));
            Poruka = Tekst.T("Merač na traci diskova važi posle ponovnog pokretanja programa.");
        }
    }

    public int Osvezavanje
    {
        get => _podesavanja.Osvezavanje;
        set
        {
            if (_podesavanja.Osvezavanje == value) return;
            _podesavanja.Osvezavanje = value;
            _promeniOsvezavanje(value);
            OnPropertyChanged();
        }
    }

    public bool Istorija
    {
        get => _podesavanja.Istorija;
        set
        {
            if (_podesavanja.Istorija == value) return;
            _podesavanja.Istorija = value;
            OnPropertyChanged();
        }
    }

    public int IstorijaDana
    {
        get => _podesavanja.IstorijaDana;
        set
        {
            if (_podesavanja.IstorijaDana == value) return;
            _podesavanja.IstorijaDana = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Made-up readings - for a look at the program, never a real measurement.</summary>
    public bool ProbniPodaci
    {
        get => _podesavanja.ProbniPodaci;
        set
        {
            if (_podesavanja.ProbniPodaci == value) return;
            _podesavanja.ProbniPodaci = value;
            OnPropertyChanged();
        }
    }

    /// <summary>X hides the program to the tray instead of closing it. Takes effect at once.</summary>
    public bool ZatvoriUTraku
    {
        get => _podesavanja.ZatvoriUTraku;
        set
        {
            if (_podesavanja.ZatvoriUTraku == value) return;
            _podesavanja.ZatvoriUTraku = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Start with Windows, in the tray. Written into the registry at once (HKCU, no administrator).</summary>
    public bool PokreniSaWindowsom
    {
        get => _podesavanja.PokreniSaWindowsom;
        set
        {
            if (_podesavanja.PokreniSaWindowsom == value) return;

            bool uspelo = value ? Pokretanje.Ukljuci() : Pokretanje.Iskljuci();
            if (!uspelo)
            {
                Poruka = "Windows nije prihvatio upis; pokušaj ponovo ili proveri zaštitu.";
                OnPropertyChanged();
                return;
            }

            _podesavanja.PokreniSaWindowsom = value;
            _podesavanja.Sacuvaj();
            Poruka = value ? "Takt će se pokretati sa Windowsom, u traci." : "Takt se više ne pokreće sa Windowsom.";
            OnPropertyChanged();
        }
    }

    public string Poruka
    {
        get => _poruka;
        private set => SetProperty(ref _poruka, value);
    }

    /// <summary>The first click on "Obriši istoriju" only asks; the second one deletes.</summary>
    public bool PotvrdaBrisanja
    {
        get => _potvrdaBrisanja;
        private set
        {
            if (!SetProperty(ref _potvrdaBrisanja, value)) return;
            OnPropertyChanged(nameof(TekstBrisanja));
        }
    }

    public string TekstBrisanja => PotvrdaBrisanja ? "Da, obriši sve" : "Obriši istoriju";

    public string Folder => Putanje.Podaci;

    /// <summary>Serbian is the program's own language; English is the second one. Both take effect at the next start.</summary>
    public bool Srpski
    {
        get => _podesavanja.Jezik != Tekst.Engleski;
        set
        {
            if (value) PostaviJezik(Tekst.Srpski);
        }
    }

    public bool Engleski
    {
        get => _podesavanja.Jezik == Tekst.Engleski;
        set
        {
            if (value) PostaviJezik(Tekst.Engleski);
        }
    }

    private void PostaviJezik(string jezik)
    {
        if (_podesavanja.Jezik == jezik) return;

        _podesavanja.Jezik = jezik;
        _podesavanja.Sacuvaj();
        OnPropertyChanged(nameof(Srpski));
        OnPropertyChanged(nameof(Engleski));
        Poruka = Tekst.T("Jezik važi posle ponovnog pokretanja programa.");
    }

    /// <summary>
    /// The second key to full monitoring, and the one the program cannot turn by itself: pawnio.eu is a
    /// separate, free download, and nothing is opened unless the user clicks the button.
    /// </summary>
    public string DrajverStanje => PawnIo.JeTu ? "Instaliran" : "Nije instaliran";

    public bool DrajverFali => !PawnIo.JeTu;

    public string DrajverOpis => PawnIo.JeTu
        ? "Preko njega se čitaju temperatura procesora, potrošnja, napon, ventilatori i SMART diskova."
        : "Bez njega se, i kao administrator, vide samo opterećenje, grafička karta i ono što Windows sam daje. " +
          "Preuzmi ga sa pawnio.eu i pokreni Takt ponovo; program ga ne donosi sa sobom jer nije naš da se deli.";

    public void OtvoriDrajver()
    {
        PotvrdaBrisanja = false;
        if (!PawnIo.Otvori()) Poruka = "Adresa nije otvorena. Ukucaj je u pregledač: " + PawnIo.Adresa;
    }

    public string Verzija => Izmene.Trenutna.Oznaka;

    public string Prava => Admin.Stanje;

    public IReadOnlyList<Unos> Istorijat => Izmene.Spisak;

    public void Sacuvaj()
    {
        _podesavanja.Sacuvaj();
        PotvrdaBrisanja = false;
        // Only the probe data need a restart: the source is chosen when the program starts. Everything else the
        // window takes at once, and the service reads the file again within half a minute.
        Poruka = "Sačuvano. Sve važi odmah osim probnih podataka — oni traže ponovno pokretanje programa. " +
                 "Usluga, ako je instalirana, svoje izmene prihvati u roku od pola minuta.";
    }

    public void OtvoriFolder()
    {
        PotvrdaBrisanja = false;
        try
        {
            Directory.CreateDirectory(Putanje.Podaci);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{Putanje.Podaci}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Otvaranje foldera podataka", ex);
            Poruka = "Folder nije otvoren.";
        }
    }

    public void ObrisiIstoriju()
    {
        if (!PotvrdaBrisanja)
        {
            PotvrdaBrisanja = true;
            Poruka = "Brišu se sva sačuvana merenja iz baze, i to ne može da se vrati. Klikni još jednom za potvrdu.";
            return;
        }

        PotvrdaBrisanja = false;
        Poruka = _obrisiIstoriju();
    }
}

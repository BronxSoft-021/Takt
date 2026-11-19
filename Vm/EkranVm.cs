using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Merenje;

namespace Takt.Vm;

/// <summary>One screen: the sensors of one group and the one whose chart is open.</summary>
public sealed class EkranVm : ObservableObject
{
    private SenzorVm? _izabran;
    private bool _sviSenzori;
    private int _mrtvih;
    private bool _prviUzorak;

    public EkranVm(Grupa grupa, IEnumerable<SenzorVm> senzori)
    {
        Grupa = grupa;
        Senzori = new ObservableCollection<SenzorVm>(senzori);
        _izabran = Senzori.FirstOrDefault();

        // A drive's temperatures are marked against that drive's own limits.
        foreach (var senzor in Senzori)
        {
            if (senzor.Senzor.Jedinica != Jedinica.Celzijus || senzor.JeGranica) continue;
            senzor.GranicaToplo = Senzori.FirstOrDefault(s => s.Uredjaj == senzor.Uredjaj && s.Ime == Granice.Upozorenje);
            senzor.GranicaVruce = Senzori.FirstOrDefault(s => s.Uredjaj == senzor.Uredjaj && s.Ime == Granice.Kriticna);
        }

        // The list is grouped under small headings (Temperature, Takt…) and, once some sensors have a value,
        // leaves out the ones that never had one. They are one click away, never deleted.
        var pogled = new ListCollectionView(Senzori) { Filter = Prikazati };
        pogled.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SenzorVm.Vrsta)));
        Pogled = pogled;

        Ikona = Geometry.Parse(GrupaOpis.Ikona(grupa));
        Ikona.Freeze();
    }

    public Grupa Grupa { get; }
    public string Naslov => GrupaOpis.Naslov(Grupa);
    public string Opis => GrupaOpis.Opis(Grupa);
    public Geometry Ikona { get; }
    public ObservableCollection<SenzorVm> Senzori { get; }

    /// <summary>What the list shows: <see cref="Senzori"/> grouped by kind, without the silent ones.</summary>
    public ICollectionView Pogled { get; }

    public SenzorVm? Izabran
    {
        get => _izabran;
        set => SetProperty(ref _izabran, value);
    }

    public bool Prazan => Senzori.Count == 0;

    public string PorukaPrazno => GrupaOpis.Prazno(Grupa, Sistem.PawnIo.JeTu, KanalVm.PotpunNadzor);

    public string BrojSenzora => Senzori.Count.ToString();

    /// <summary>Sensors that have not given a single number yet (no driver, or the part has no such sensor).</summary>
    public int BezVrednosti => Senzori.Count(s => !s.ImaloVrednost);

    /// <summary>True when the switch under the list makes sense: some sensors speak and some do not.</summary>
    public bool ImaSkrivenih => BezVrednosti > 0 && BezVrednosti < Senzori.Count;

    public bool SviSenzori
    {
        get => _sviSenzori;
        set
        {
            if (!SetProperty(ref _sviSenzori, value)) return;
            Pogled.Refresh();
            OnPropertyChanged(nameof(SkriveniTekst));
        }
    }

    public string SkriveniTekst => SviSenzori
        ? "Sakrij senzore bez vrednosti"
        : Tekst.P(BezVrednosti % 10 == 1 && BezVrednosti % 100 != 11 ? "Još {0} senzor bez vrednosti" : "Još {0} senzora bez vrednosti", BezVrednosti);

    private bool Prikazati(object stavka) =>
        _sviSenzori || stavka is not SenzorVm senzor || (senzor.ImaloVrednost && !senzor.Mrtav)
        || Senzori.All(s => !s.ImaloVrednost);

    public void Osvezi(Snimak snimak, double?[] najmanje, double?[] najvise, bool obavesti = true)
    {
        int pre = BezVrednosti;
        bool novo = false;

        foreach (var senzor in Senzori)
        {
            novo |= senzor.Postavi(
                snimak.Ovo(senzor.Indeks),
                senzor.Indeks < najmanje.Length ? najmanje[senzor.Indeks] : null,
                senzor.Indeks < najvise.Length ? najvise[senzor.Indeks] : null);

            if (obavesti) senzor.Obavesti();
        }

        // A sensor spoke for the first time (the helper started, a disk woke up): it joins the list.
        if (novo && BezVrednosti != pre)
        {
            var izabran = Izabran;
            Pogled.Refresh();
            Izabran = izabran;
            OnPropertyChanged(nameof(BezVrednosti));
            OnPropertyChanged(nameof(ImaSkrivenih));
            OnPropertyChanged(nameof(SkriveniTekst));
        }

        // A board channel that stops moving at a high value ("Temperatura 6" at 80,0 °C for ever) is an input with
        // nothing on it: it leaves the list once it has been still long enough to tell (SenzorVm.Mrtav).
        int mrtvih = Senzori.Count(s => s.Mrtav);
        if (mrtvih != _mrtvih)
        {
            _mrtvih = mrtvih;
            Pogled.Refresh();
        }

        // The chart is on screen all the time: one int notification keeps it live while the table waits.
        if (!obavesti && Izabran is { } tekuci) tekuci.Tikni();

        // On the first reading, open the first sensor that actually has a value: "—" is a poor first impression
        // when the screen has something to show. After that the choice is the user's.
        if (_prviUzorak) return;
        _prviUzorak = true;
        if (Izabran?.Broj is null && Senzori.FirstOrDefault(s => s.Broj is not null) is { } prvi)
            Izabran = prvi;
    }
}

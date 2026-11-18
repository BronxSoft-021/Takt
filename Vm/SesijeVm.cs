using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Takt.Vm;

/// <summary>The list of game sessions (newest first) and the report of the one chosen.</summary>
public sealed class SesijeVm : ObservableObject
{
    private IzvestajSesije? _izabrana;

    public SesijeVm(IEnumerable<IzvestajSesije> sesije)
    {
        Sesije = new ObservableCollection<IzvestajSesije>(sesije);
        _izabrana = Sesije.FirstOrDefault();
    }

    public ObservableCollection<IzvestajSesije> Sesije { get; }

    public IzvestajSesije? Izabrana
    {
        get => _izabrana;
        set => SetProperty(ref _izabrana, value);
    }

    public bool Prazno => Sesije.Count == 0;
}

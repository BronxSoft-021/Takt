using System.IO;
using System.Text.Json;

namespace Takt.Sistem;

/// <summary>What the user chose. Kept in %APPDATA%\BronxSoft Takt\podesavanja.json.</summary>
public sealed class Podesavanja
{
    /// <summary>Seconds between two readings. 1, 2 or 5.</summary>
    public int Osvezavanje { get; set; } = 1;

    /// <summary>Write one row per minute to the database.</summary>
    public bool Istorija { get; set; } = true;

    /// <summary>How many days of history to keep. 1, 7, 30 or 90.</summary>
    public int IstorijaDana { get; set; } = 7;

    /// <summary>Show made-up readings instead of the real hardware. Used when there is no driver and for screenshots.</summary>
    public bool ProbniPodaci { get; set; }

    public string Faza { get; set; } = "alfa";

    /// <summary>Channels the user muted (M) on the console, by <c>KanalVm.Kljuc</c>.</summary>
    public List<string> Utisani { get; set; } = [];

    public static Podesavanja Ucitaj()
    {
        try
        {
            if (File.Exists(Putanje.Podesavanja))
            {
                var procitano = JsonSerializer.Deserialize<Podesavanja>(File.ReadAllText(Putanje.Podesavanja));
                if (procitano is not null) return procitano.Ispravno();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Dnevnik.Greska("Čitanje podešavanja", ex);
        }
        return new Podesavanja();
    }

    public void Sacuvaj()
    {
        try
        {
            Directory.CreateDirectory(Putanje.Podaci);
            File.WriteAllText(Putanje.Podesavanja,
                JsonSerializer.Serialize(Ispravno(), new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Upis podešavanja", ex);
        }
    }

    /// <summary>
    /// A separate copy for one run. A screenshot or <c>--probno</c> run changes its copy (fake data, no history)
    /// and the user's saved choices stay as they were, even if "Sačuvaj" is pressed during that run.
    /// </summary>
    public Podesavanja Kopija()
    {
        var kopija = (Podesavanja)MemberwiseClone();
        kopija.Utisani = [.. Utisani];
        return kopija;
    }

    /// <summary>Refuses values the program cannot work with (a hand-edited file must not break it).</summary>
    public Podesavanja Ispravno()
    {
        if (Osvezavanje is not (1 or 2 or 5)) Osvezavanje = 1;
        if (IstorijaDana is not (1 or 7 or 30 or 90)) IstorijaDana = 7;
        Utisani ??= [];
        return this;
    }
}

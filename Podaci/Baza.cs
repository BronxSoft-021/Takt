using System.IO;
using Microsoft.Data.Sqlite;

namespace Takt.Podaci;

/// <summary>
/// History of readings in SQLite: one row per sensor per minute. Kept simple on purpose - the program must run
/// for weeks without growing without bound, so <see cref="OcistiPre"/> is called after every write.
/// </summary>
public sealed class Baza : IDisposable
{
    private readonly SqliteConnection _veza;

    public Baza(string putanja)
    {
        _veza = new SqliteConnection($"Data Source={putanja}");
        _veza.Open();

        // WAL keeps the window from waiting while a minute is written.
        Naredba("PRAGMA journal_mode=WAL;").ExecuteNonQuery();
        Naredba("PRAGMA synchronous=NORMAL;").ExecuteNonQuery();
        NapraviTabele();
    }

    public static string ZaPutanju(string putanja) => $"Data Source={putanja}";

    private void NapraviTabele()
    {
        Naredba("""
                create table if not exists senzori(
                    kljuc text primary key,
                    grupa text not null,
                    uredjaj text not null,
                    ime text not null,
                    jedinica integer not null);
                """).ExecuteNonQuery();

        Naredba("""
                create table if not exists uzorci(
                    id integer primary key autoincrement,
                    vreme integer not null,
                    senzor text not null,
                    vrednost real not null);
                """).ExecuteNonQuery();

        Naredba("create index if not exists ix_uzorci_senzor on uzorci(senzor, vreme);").ExecuteNonQuery();
    }

    /// <summary>Remembers the sensors so old rows can still be explained after a restart.</summary>
    public void UpisiSenzore(IEnumerable<Merenje.Senzor> senzori)
    {
        using var transakcija = _veza.BeginTransaction();
        foreach (var senzor in senzori)
        {
            using var naredba = _veza.CreateCommand();
            naredba.Transaction = transakcija;
            naredba.CommandText = """
                                   insert into senzori(kljuc, grupa, uredjaj, ime, jedinica)
                                   values($kljuc, $grupa, $uredjaj, $ime, $jedinica)
                                   on conflict(kljuc) do update set
                                       grupa = excluded.grupa,
                                       uredjaj = excluded.uredjaj,
                                       ime = excluded.ime,
                                       jedinica = excluded.jedinica;
                                   """;
            naredba.Parameters.AddWithValue("$kljuc", senzor.Kljuc.Tekst);
            naredba.Parameters.AddWithValue("$grupa", senzor.Grupa.ToString());
            naredba.Parameters.AddWithValue("$uredjaj", senzor.Uredjaj);
            naredba.Parameters.AddWithValue("$ime", senzor.Ime);
            naredba.Parameters.AddWithValue("$jedinica", (int)senzor.Jedinica);
            naredba.ExecuteNonQuery();
        }
        transakcija.Commit();
    }

    public void UpisiSve(long vreme, IEnumerable<(string Kljuc, double Vrednost)> redovi)
    {
        using var transakcija = _veza.BeginTransaction();
        using var naredba = _veza.CreateCommand();
        naredba.Transaction = transakcija;
        naredba.CommandText = "insert into uzorci(vreme, senzor, vrednost) values($vreme, $senzor, $vrednost);";
        var pVreme = naredba.Parameters.Add("$vreme", SqliteType.Integer);
        var pSenzor = naredba.Parameters.Add("$senzor", SqliteType.Text);
        var pVrednost = naredba.Parameters.Add("$vrednost", SqliteType.Real);
        pVreme.Value = vreme;

        foreach (var (kljuc, vrednost) in redovi)
        {
            pSenzor.Value = kljuc;
            pVrednost.Value = vrednost;
            naredba.ExecuteNonQuery();
        }
        transakcija.Commit();
    }

    public void Upisi(long vreme, string kljuc, double vrednost) =>
        UpisiSve(vreme, [(kljuc, vrednost)]);

    public List<(long Vreme, double Vrednost)> Citaj(string kljuc, long od, long doVreme)
    {
        var rezultat = new List<(long Vreme, double Vrednost)>();
        using var naredba = Naredba(
            "select vreme, vrednost from uzorci where senzor = $senzor and vreme >= $od and vreme <= $do order by vreme;",
            ("$senzor", kljuc), ("$od", od), ("$do", doVreme));

        using var citac = naredba.ExecuteReader();
        while (citac.Read())
            rezultat.Add((citac.GetInt64(0), citac.GetDouble(1)));
        return rezultat;
    }

    /// <summary>Drops everything older than the given moment. Returns how many rows went.</summary>
    public int OcistiPre(long vreme)
    {
        using var naredba = Naredba("delete from uzorci where vreme < $vreme;", ("$vreme", vreme));
        return naredba.ExecuteNonQuery();
    }

    public void ObrisiSve()
    {
        Naredba("delete from uzorci;").ExecuteNonQuery();
    }

    public int BrojRedova()
    {
        using var naredba = Naredba("select count(*) from uzorci;");
        return Convert.ToInt32(naredba.ExecuteScalar());
    }

    public int BrojSenzora()
    {
        using var naredba = Naredba("select count(*) from senzori;");
        return Convert.ToInt32(naredba.ExecuteScalar());
    }

    private SqliteCommand Naredba(string sql, params (string Ime, object? Vrednost)[] parametri)
    {
        var naredba = _veza.CreateCommand();
        naredba.CommandText = sql;
        foreach (var (ime, vrednost) in parametri)
            naredba.Parameters.AddWithValue(ime, vrednost ?? DBNull.Value);
        return naredba;
    }

    public void Dispose()
    {
        try
        {
            _veza.Dispose();
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("Zatvaranje baze", ex);
        }
    }
}

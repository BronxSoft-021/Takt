using Takt.Merenje;
using Takt.Podaci;
using Takt.Sistem;
using Takt.Vm;

namespace Takt.Testovi;

/// <summary>
/// The stable sensor key: a renamed or translated sensor keeps its row in the database, and the history written
/// before the key existed is moved over once - without losing a minute and without doubling one.
/// </summary>
public static class KljucTestovi
{
    public static Task Kljuc()
    {
        var stari = new SenzorKljuc(Grupa.Procesor, "AMD Ryzen 5 5600", "Temperatura procesora", "/amdcpu/0/temperature/2");
        var preveden = new SenzorKljuc(Grupa.Procesor, "AMD Ryzen 5 5600", "CPU Temperature", "/amdcpu/0/temperature/2");
        var bez = new SenzorKljuc(Grupa.Procesor, "AMD Ryzen 5 5600", "Temperatura procesora");

        Proveri.Jednako(stari.Tekst, preveden.Tekst, "prevedeno ime ne menja ključ");
        Proveri.Da(stari.TekstImena != preveden.TekstImena, "ključ po imenu se razlikuje");
        Proveri.Jednako(bez.TekstImena, bez.Tekst, "bez putanje ključ je kao ranije");
        Proveri.Jednako("ram/0/zauzece", new RamIzvor().Senzori[0].Kljuc.Tekst, "memorija ima svoj stabilan ključ");
        return Task.CompletedTask;
    }

    public static Task Migracija()
    {
        var putanja = Path.Combine(Path.GetTempPath(), $"takt-kljuc-{Guid.NewGuid():N}.db");
        try
        {
            // The real upgrade: the same sensor, the same name, now with a stable key. (A name already changed
            // before this release could not be recognised - which is why the keys come first, then the translation.)
            var staro = new Senzor(new SenzorKljuc(Grupa.Procesor, "CPU", "Temperatura procesora"), Jedinica.Celzijus);
            var novo = new Senzor(new SenzorKljuc(Grupa.Procesor, "CPU", "Temperatura procesora", "/amdcpu/0/temperature/2"),
                Jedinica.Celzijus);

            using (var baza = new Baza(putanja))
            {
                baza.UpisiSenzore([staro]);
                baza.Upisi(1000, staro.Kljuc.Tekst, 50);
                baza.Upisi(2000, staro.Kljuc.Tekst, 51);
            }

            using (var baza = new Baza(putanja))
            {
                baza.UpisiSenzore([novo]);
                Proveri.Jednako(1, baza.BrojSenzora(), "jedan senzor posle prebacivanja");
                Proveri.Jednako(2, baza.BrojRedova(), "nijedan red nije izgubljen ni udvojen");
                Proveri.Jednako(2, baza.Citaj(novo.Kljuc.Tekst, 0, 3000).Count, "istorija je pod novim ključem");
                Proveri.Jednako(0, baza.Citaj(staro.Kljuc.Tekst, 0, 3000).Count, "stari ključ je prazan");
            }

            // Every later start must find nothing left to move.
            using (var baza = new Baza(putanja))
            {
                baza.UpisiSenzore([novo]);
                Proveri.Jednako(2, baza.BrojRedova(), "ponovni start ne duplira redove");
                Proveri.Jednako(1, baza.BrojSenzora(), "ni senzore");
            }
        }
        finally
        {
            foreach (var fajl in new[] { putanja, putanja + "-wal", putanja + "-shm" })
                try
                {
                    if (File.Exists(fajl)) File.Delete(fajl);
                }
                catch (IOException)
                {
                    // A leftover temp file is harmless.
                }
        }

        return Task.CompletedTask;
    }

    public static Task Jezgra()
    {
        Proveri.Da(KanalVm.JeJezgro(new SenzorKljuc(Grupa.Procesor, "CPU", "Bilo koje ime", "/amdcpu/0/load/3")),
            "jezgro se prepoznaje po putanji, ne po imenu");
        Proveri.Da(!KanalVm.JeJezgro(new SenzorKljuc(Grupa.Procesor, "CPU", "Opterećenje procesora", "/amdcpu/0/load/0")),
            "ukupno opterećenje nije jezgro");
        Proveri.Da(KanalVm.JeJezgro(new SenzorKljuc(Grupa.Procesor, "CPU", "Opterećenje jezgra 4")),
            "bez putanje (probni podaci) - po imenu");
        return Task.CompletedTask;
    }
}

using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>The library's datasheet English turned into the words on screen.</summary>
public static class PrevodTestovi
{
    public static Task Imena()
    {
        Proveri.Jednako("Temperatura", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Core", Jedinica.Celzijus), "GPU temperatura");
        Proveri.Jednako("Opterećenje", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Core", Jedinica.Procenat), "GPU opterećenje");
        Proveri.Jednako("Takt jezgra", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Core", Jedinica.Megaherc), "GPU takt");
        Proveri.Jednako("Napon jezgra", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Core", Jedinica.Volt), "GPU napon");
        Proveri.Jednako("Hotspot", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Hot Spot", Jedinica.Celzijus), "hotspot");
        Proveri.Jednako("Takt memorije", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Memory", Jedinica.Megaherc), "takt VRAM");
        Proveri.Jednako("VRAM zauzeto", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Memory Used", Jedinica.Megabajt), "VRAM zauzeto");
        Proveri.Jednako("VRAM ukupno", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Memory Total", Jedinica.Megabajt), "VRAM ukupno");
        Proveri.Jednako("Potrošnja", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Package", Jedinica.Vat), "potrošnja");
        Proveri.Jednako("Ventilator", LhmIzvor.Prevedi(Grupa.Grafika, "GPU Fan", Jedinica.Obrtaj), "ventilator");

        Proveri.Jednako("Temperatura procesora", LhmIzvor.Prevedi(Grupa.Procesor, "Core (Tctl/Tdie)", Jedinica.Celzijus), "Tctl");
        Proveri.Jednako("Opterećenje procesora", LhmIzvor.Prevedi(Grupa.Procesor, "CPU Total", Jedinica.Procenat), "ukupno");
        Proveri.Jednako("Opterećenje jezgra 7", LhmIzvor.Prevedi(Grupa.Procesor, "CPU Core #7", Jedinica.Procenat), "jezgro");
        Proveri.Jednako("Takt jezgra 7 (efektivni)", LhmIzvor.Prevedi(Grupa.Procesor, "Core #7 (Effective)", Jedinica.Megaherc), "efektivni");
        Proveri.Jednako("Temperatura jezgra 3", LhmIzvor.Prevedi(Grupa.Procesor, "Core #3", Jedinica.Celzijus), "temp jezgra");
        Proveri.Jednako("Napon jezgra 3 (VID)", LhmIzvor.Prevedi(Grupa.Procesor, "Core #3 VID", Jedinica.Volt), "VID");
        Proveri.Jednako("Napon procesora", LhmIzvor.Prevedi(Grupa.Procesor, "Core (SVI2 TFN)", Jedinica.Volt), "SVI2");
        Proveri.Jednako("Potrošnja paketa", LhmIzvor.Prevedi(Grupa.Procesor, "Package", Jedinica.Vat), "paket");

        Proveri.Jednako("Ventilator 2", LhmIzvor.Prevedi(Grupa.Hladjenje, "Fan #2", Jedinica.Obrtaj), "ventilator 2");
        Proveri.Jednako("Temperatura 1", LhmIzvor.Prevedi(Grupa.Hladjenje, "Temperature #1", Jedinica.Celzijus), "temperatura 1");
        Proveri.Jednako("Napon procesora (ploča)", LhmIzvor.Prevedi(Grupa.Hladjenje, "Vcore", Jedinica.Volt), "Vcore sa ploče");
        Proveri.Jednako("Baterija CMOS-a", LhmIzvor.Prevedi(Grupa.Hladjenje, "CMOS Battery", Jedinica.Volt), "baterija");
        Proveri.Jednako("Temperatura CCD1", LhmIzvor.Prevedi(Grupa.Procesor, "CCD1 (Tdie)", Jedinica.Celzijus), "Ryzen CCD");
        Proveri.Jednako("Takt magistrale", LhmIzvor.Prevedi(Grupa.Procesor, "Bus Speed", Jedinica.Megaherc), "magistrala");

        // A numbered board temperature has no warm/hot marks: it may be an empty input reading 87 °C.
        var plocaTemp = new Takt.Vm.SenzorVm(new Senzor(new SenzorKljuc(Grupa.Hladjenje, "Ploča", "Temperatura 3"), Jedinica.Celzijus), 0, new Prsten(2));
        plocaTemp.Osvezi(87, 87, 87);
        Proveri.Jednako("", plocaTemp.Nivo, "neimenovana temperatura ploče se ne boji");
        Proveri.Jednako("Životni vek", LhmIzvor.Prevedi(Grupa.Diskovi, "Remaining Life", Jedinica.Procenat), "životni vek");
        Proveri.Jednako("Temperatura", LhmIzvor.Prevedi(Grupa.Diskovi, "Composite Temperature", Jedinica.Celzijus), "NVMe temperatura");
        Proveri.Jednako("Temperatura senzora 2", LhmIzvor.Prevedi(Grupa.Diskovi, "Temperature #2", Jedinica.Celzijus), "NVMe senzor");
        Proveri.Jednako("Kritična granica", LhmIzvor.Prevedi(Grupa.Diskovi, "Critical Temperature", Jedinica.Celzijus), "granica, ne merenje");
        Proveri.Jednako("Granica upozorenja", LhmIzvor.Prevedi(Grupa.Diskovi, "Warning Temperature", Jedinica.Celzijus), "granica upozorenja");
        Proveri.Jednako("Ukupno upisano", LhmIzvor.Prevedi(Grupa.Diskovi, "Data Written", Jedinica.Megabajt), "upisano");
        Proveri.Jednako("Brzina čitanja", LhmIzvor.Prevedi(Grupa.Diskovi, "Read Rate", Jedinica.MegabajtUSekundi), "brzina");
        Proveri.Jednako(Jedinica.Megabajt, LhmIzvor.JedinicaZa(LibreHardwareMonitor.Hardware.SensorType.Data).Jedinica, "Data je količina");
        Proveri.Jednako(1024.0, LhmIzvor.JedinicaZa(LibreHardwareMonitor.Hardware.SensorType.Data).Faktor, "Data stiže u GB");
        Proveri.Jednako(Jedinica.MegabajtUSekundi, LhmIzvor.JedinicaZa(LibreHardwareMonitor.Hardware.SensorType.Throughput).Jedinica, "protok");

        // Unknown names stay as they are: a technical name is better than a wrong translation.
        Proveri.Jednako("Neka nova stvar", LhmIzvor.Prevedi(Grupa.Grafika, "Neka nova stvar", Jedinica.Nepoznato), "nepoznato");
        return Task.CompletedTask;
    }

    public static Task Vram()
    {
        Proveri.Jednako<double?>(50, LhmIzvor.Procenat(4096, 8192), "pola VRAM-a");
        Proveri.Jednako<double?>(19, LhmIzvor.Procenat(1553, 8176), "zaokruženo na ceo procenat");
        Proveri.Jednako<double?>(null, LhmIzvor.Procenat(null, 8192), "nema dela");
        Proveri.Jednako<double?>(null, LhmIzvor.Procenat(100, null), "nema celine");
        Proveri.Jednako<double?>(null, LhmIzvor.Procenat(100, 0), "celina je nula");
        return Task.CompletedTask;
    }
}

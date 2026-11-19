using Takt.Merenje;
using Takt.Sistem;

namespace Takt.Testovi;

/// <summary>
/// The drive verdict: a handful of SMART numbers turned into "fine", "watch this" or "save your data". The rules
/// are the part worth testing - the reading itself needs a real drive and administrator.
/// </summary>
public static class ZdravljeTestovi
{
    private static StanjeDiska Disk(string model = "Test", bool ssd = true, bool nvme = false, double? zivot = 93,
        double? istrosenost = null, double? rezerva = null, double? granica = null, double? temp = 40,
        double? granicaTemp = null, double? kriticna = null, long? preraspodeljeni = null, long? naCekanju = null,
        long? neispravni = null, long? crc = null, long? media = null, long? kriticno = null, bool ima = true) =>
        new(model, ssd, nvme, zivot, istrosenost, rezerva, granica, temp, granicaTemp, kriticna, preraspodeljeni,
            naCekanju, neispravni, crc, media, kriticno, 1000, ima);

    public static Task Provere()
    {
        // A healthy drive says what it is: life, hours, temperature.
        var zdravo = Zdravlje.Proveri(Disk());
        Proveri.Jednako(NivoZdravlja.Dobro, zdravo.Nivo, "zdrav disk");
        Proveri.Da(zdravo.Detalj.Contains("93"), "u detalju je životni vek");
        Proveri.Da(zdravo.Detalj.Contains("°C"), "i temperatura");

        // Sectors that no longer read correctly: data is being lost now.
        Proveri.Jednako(NivoZdravlja.Kvar, Zdravlje.Proveri(Disk(naCekanju: 8)).Nivo, "sektori na čekanju su kvar");
        Proveri.Jednako(NivoZdravlja.Kvar, Zdravlje.Proveri(Disk(neispravni: 2)).Nivo, "neispravni sektori su kvar");
        Proveri.Jednako(NivoZdravlja.Kvar, Zdravlje.Proveri(Disk(media: 1)).Nivo, "greške u podacima su kvar");

        // Replaced sectors and cable errors: watch, do not panic.
        Proveri.Jednako(NivoZdravlja.Upozorenje, Zdravlje.Proveri(Disk(preraspodeljeni: 3)).Nivo, "preraspoređeni su upozorenje");
        Proveri.Da(Zdravlje.Proveri(Disk(crc: 57)).Detalj.Contains("kabl"), "CRC greške upućuju na kabl");

        // Life and wear.
        Proveri.Jednako(NivoZdravlja.Upozorenje, Zdravlje.Proveri(Disk(zivot: 12)).Nivo, "malo života je upozorenje");
        Proveri.Jednako(NivoZdravlja.Kvar, Zdravlje.Proveri(Disk(zivot: 3)).Nivo, "kraj života je kvar");
        Proveri.Jednako(NivoZdravlja.Upozorenje, Zdravlje.Proveri(Disk(zivot: null, istrosenost: 85)).Nivo, "istrošenost 85 %");

        // Temperatures against the drive's own limits.
        Proveri.Jednako(NivoZdravlja.Upozorenje, Zdravlje.Proveri(Disk(temp: 62, granicaTemp: 60)).Nivo, "topao disk");
        Proveri.Jednako(NivoZdravlja.Kvar, Zdravlje.Proveri(Disk(temp: 86, kriticna: 85)).Nivo, "pregrijan disk");
        Proveri.Jednako(NivoZdravlja.Dobro, Zdravlje.Proveri(Disk(temp: 45, granicaTemp: 84)).Nivo, "45 °C je u redu");

        // Spare blocks running towards their threshold.
        Proveri.Jednako(NivoZdravlja.Upozorenje, Zdravlje.Proveri(Disk(rezerva: 12, granica: 10)).Nivo, "malo rezerve");
        Proveri.Jednako(NivoZdravlja.Dobro, Zdravlje.Proveri(Disk(rezerva: 100, granica: 10)).Nivo, "rezerve ima");

        // A drive that did not answer is not "healthy".
        Proveri.Jednako(NivoZdravlja.Nepoznato, Zdravlje.Proveri(Disk(ima: false)).Nivo, "bez SMART-a se ne tvrdi ništa");
        return Task.CompletedTask;
    }
}

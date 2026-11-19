using DiskInfoToolkit;
using Takt.Merenje;

namespace Takt.Sistem;

/// <summary>
/// Reads each drive's SMART through <c>DiskInfoToolkit</c> - the same library LibreHardwareMonitor uses - and
/// keeps only the few numbers a verdict needs. Needs administrator, so the service or the helper runs it; the
/// window draws what they send. Everything is in a try: a drive that refuses to answer must not cost a reading.
/// </summary>
public static class Smart
{
    public static IReadOnlyList<StanjeDiska> Procitaj()
    {
        var rezultat = new List<StanjeDiska>();

        try
        {
            StorageManager.ReloadStorages();

            foreach (var disk in StorageManager.Storages)
            {
                var smart = disk.Smart;
                var atributi = smart?.SmartAttributes;

                rezultat.Add(new StanjeDiska(
                    Model: (disk.Model ?? "").Trim(),
                    Ssd: disk.IsSSD,
                    Nvme: disk.IsNVMe,
                    ZivotniVek: smart?.Life,
                    Istrosenost: Sirovo(atributi, "PercentageUsed"),
                    RezervniBlokovi: Sirovo(atributi, "AvailableSpare"),
                    GranicaRezervnih: Sirovo(atributi, "AvailableSpareThreshold"),
                    Temperatura: smart?.Temperature,
                    GranicaTemperature: smart?.TemperatureWarning,
                    KriticnaTemperatura: smart?.TemperatureCritical,
                    Preraspodeljeni: Sirovo(atributi, "ReallocatedSectorsCount"),
                    NaCekanju: Sirovo(atributi, "PendingSectorCount"),
                    Neispravni: Sirovo(atributi, "UncorrectableSectorCount"),
                    CrcGreske: Sirovo(atributi, "CRCErrorCount"),
                    MediaGreske: Sirovo(atributi, "MediaAndDataIntegrityErrors"),
                    KriticnoUpozorenje: Sirovo(atributi, "CriticalWarning"),
                    SatiRada: smart?.MeasuredPowerOnHours is { } sati and > 0 ? sati : null,
                    ImaSmart: atributi is { Count: > 0 }));
            }
        }
        catch (Exception ex)
        {
            Dnevnik.Greska("SMART", ex);
        }

        return rezultat;
    }

    /// <summary>
    /// The verdicts ready for the pipe, with one log line per new problem (a repeated "reallocated sectors" every
    /// minute would bury everything else in the log).
    /// </summary>
    public static IReadOnlyList<Protokol.DiskZdravlje> Nalazi()
    {
        var lista = new List<Protokol.DiskZdravlje>();

        foreach (var disk in Procitaj())
        {
            var nalaz = Zdravlje.Proveri(disk);
            lista.Add(new Protokol.DiskZdravlje(disk.Model, (int)nalaz.Nivo, nalaz.Naslov, nalaz.Detalj));

            if (nalaz.Nivo is NivoZdravlja.Upozorenje or NivoZdravlja.Kvar && Prijavljeno.Add(disk.Model + "|" + nalaz.Naslov))
                Dnevnik.Upisi($"SMART: {disk.Model}: {nalaz.Naslov} - {nalaz.Detalj}");
        }

        return lista;
    }

    private static readonly HashSet<string> Prijavljeno = new(StringComparer.Ordinal);

    /// <summary>The raw number behind an attribute, by the toolkit's own name (a missing one is simply null).</summary>
    private static long? Sirovo(List<SmartAttribute>? atributi, string ime)
    {
        if (atributi is null) return null;

        foreach (var atribut in atributi)
        {
            if (!string.Equals(atribut.Info.Type.ToString(), ime, StringComparison.Ordinal)) continue;
            return (long)atribut.Attribute.RawValueULong;
        }

        return null;
    }
}

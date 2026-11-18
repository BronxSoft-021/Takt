using Takt.Testovi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Everything temporary stays inside the test's own output folder.
var radni = Path.Combine(AppContext.BaseDirectory, "privremeno");
if (Directory.Exists(radni)) Directory.Delete(radni, true);
Directory.CreateDirectory(radni);
Environment.SetEnvironmentVariable("TAKT_PODACI", Path.Combine(radni, "podaci"));
Spisak.Radni = radni;

// What the real hardware says right now: Testovi --popis [fajl]. Run as administrator for the full list.
if (args.Length >= 1 && args[0] == "--popis")
    return Popis.Senzora(args.Length >= 2 ? args[1] : Path.Combine(radni, "popis.txt"));

var filter = args.FirstOrDefault();
int pali = 0, ukupno = 0;

foreach (var (ime, test) in Spisak.Svi)
{
    if (filter != null && !ime.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
    ukupno++;
    try
    {
        await test();
        Console.WriteLine($"OK    {ime}");
    }
    catch (Exception ex)
    {
        pali++;
        Console.WriteLine($"PAO   {ime}: {ex.Message}");
    }
}

Console.WriteLine($"\n{ukupno - pali}/{ukupno} prošlo");
return pali == 0 ? 0 : 1;

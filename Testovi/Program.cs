using Takt.Testovi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// The program's own culture: a test must format numbers the way the program does (52,8 and not 52.8).
Takt.Merenje.Format.PostaviKulturu();

// Everything temporary stays inside the test's own output folder.
var radni = Path.Combine(AppContext.BaseDirectory, "privremeno");
if (Directory.Exists(radni)) Directory.Delete(radni, true);
Directory.CreateDirectory(radni);
Environment.SetEnvironmentVariable("TAKT_PODACI", Path.Combine(radni, "podaci"));
Spisak.Radni = radni;

// Our own pipe: the installed service holds the real one ("BronxSoftTakt-nadzor"), and a test must neither talk
// to it nor fight it for the name. Child processes (the helper test) inherit this.
Environment.SetEnvironmentVariable("TAKT_CEV", "BronxSoftTakt-test-" + Environment.ProcessId);

// What the real hardware says right now: Testovi --popis [fajl]. Run as administrator for the full list.
if (args.Length >= 1 && args[0] == "--popis")
    return Popis.Senzora(args.Length >= 2 ? args[1] : Path.Combine(radni, "popis.txt"));

// How long one reading takes: Testovi --meri [n]. The number to compare after a change.
if (args.Length >= 1 && args[0] == "--meri")
    return MerenjeSenzora.Pokreni(args.Length >= 2 && int.TryParse(args[1], out int merenja) ? merenja : 20);

// Moves an existing history folder onto the stable sensor keys: Testovi --prebaci <folder>.
if (args.Length >= 2 && args[0] == "--prebaci")
    return Prebacivanje.Pokreni(args[1]);

// What each drive says about its own health: Testovi --zdravlje (as administrator for every drive).
if (args.Length >= 1 && args[0] == "--zdravlje")
    return ZdravljeIspis.Pokreni();

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


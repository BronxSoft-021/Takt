# BronxSoft Takt

Merač računara u BronxSoft porodici: temperatura, takt, opterećenje, potrošnja, napon, memorija,
diskovi i ventilatori — na jednom mirnom mestu, bez interneta i bez gomile opcija.

Verzija 0.14.12 alfa ima pet ekrana (procesor, grafička karta, memorija, diskovi, hlađenje) sa vrednostima
po vrstama, najmanjim, prosečnim i najvećim od pokretanja i grafikonom poslednjih uzoraka koji pod mišem
pokazuje vrednost, istoriju u SQLite bazi, pomoćnika za senzore koji traže administratora i probne podatke
za prikaz bez pravog hardvera.

## Šta se vidi

| Ekran | Podaci |
|---|---|
| Procesor | temperatura po jezgrima, opterećenje, takt, potrošnja, napon, thermal throttling |
| Grafička karta | temperatura i hotspot, VRAM, opterećenje, takt jezgra i memorije, potrošnja, ventilator |
| Memorija | zauzeće, zauzeto, slobodno, ukupno, commit, pagefile (procena), brzina memorije |
| Diskovi | temperature SSD-a, zdravlje, SMART parametri, životni vek |
| Hlađenje | brzina ventilatora, temperature koje ih pokreću, praćenje anomalija |

## Šta traži administratora

Windows ne daje temperaturu procesora, potrošnju, napon i ventilatore bez kernel drajvera. Program zato
radi u dva sloja: bez administratora čita sve što može (grafička karta preko drajverskog API-ja, memorija,
opterećenje procesora, diskovi koliko Windows dozvoljava), a za ostalo pokrene **pomoćnika** — drugu kopiju
sebe kojoj Windows da administratorska prava. Prozor ostaje običan korisnik i samo čita ono što pomoćnik
pošalje preko cevi; pomoćnik se sam ugasi kad se prozor zatvori.

Dugme **„Uključi potpuni nadzor“** stoji u traci upozorenja. Ono što hardver ne daje prikazuje se kao `—`,
nikad kao izmišljen broj.

Broj senzora na jednom računaru (Ryzen 5 5600, Radeon RX 6600): **62 bez administratora**.

## Pokretanje

- Windows 10 ili 11, 64 bita.
- .NET 10 (Desktop Runtime), program ga ne nosi sa sobom.

```
dotnet build Takt.csproj
bin\Debug\net10.0-windows10.0.19041.0\win-x64\Takt.exe
```

Za prikaz bez pravih senzora: `Takt.exe --probno`.
Za slike svih ekrana: `Takt.exe --snimak <folder>` (probni podaci) ili `Takt.exe --snimak <folder> --pravo`
(pravi senzori).

## Provere

```
dotnet build Testovi\Testovi.csproj
Testovi\bin\Debug\net10.0-windows10.0.19041.0\win-x64\Testovi.exe
```

`Testovi --popis [fajl]` ispiše šta stvarni hardver daje na ovom računaru — pokreni kao administrator
za pun spisak.

## Podaci

`%APPDATA%\BronxSoft Takt\` — `takt.db` (istorija), `podesavanja.json`, `greske.log`.
Program ništa ne šalje na internet.

## Licence

Kod programa: MIT (vidi `LICENSE`). Korišćeni delovi: LibreHardwareMonitor (MPL 2.0), .NET/WPF,
CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, System.Management (MIT) — spisak i tekstovi u `Licence.txt`.

---

**English (short).** BronxSoft Takt is a Windows hardware monitor in the BronxSoft family: per-core
temperatures, clocks, load, power, voltage, memory, disks and fans, with a 120-sample chart, SQLite history
and no network access. C# / .NET 10 / WPF; sensor access through LibreHardwareMonitor (MPL 2.0). Some
readings need administrator rights; the rest work without them and unavailable values are shown as `—`.
Code under MIT.

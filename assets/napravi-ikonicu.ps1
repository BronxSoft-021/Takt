# Ikonica za BronxSoft Takt: tamna pločica sa narandžastim rubom i kazaljkom obrtomera.
# Ista porodica kao ostale BronxSoft ikonice. Pokreni: powershell -ExecutionPolicy Bypass -File napravi-ikonicu.ps1
Add-Type -AssemblyName System.Drawing

$izlaz = Join-Path $PSScriptRoot 'app.ico'
$velicine = @(16, 24, 32, 48, 64, 128, 256)

function Napravi-Sliku([int]$velicina) {
    $b = New-Object System.Drawing.Bitmap($velicina, $velicina, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($b)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = $velicina / 256.0
    $ploca = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 16, 18, 20))
    $rub = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 240, 162, 59), [float](7 * $s))
    $krem = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 241, 230, 208), [float](9 * $s))
    $kazaljka = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 240, 162, 59), [float](11 * $s))
    $kazaljka.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $kazaljka.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    # zaobljena pločica
    $x = 6 * $s; $y = 6 * $s; $w = 244 * $s; $h = 244 * $s; $r = 50 * $s
    $put = New-Object System.Drawing.Drawing2D.GraphicsPath
    $put.AddArc([float]$x, [float]$y, [float]($r * 2), [float]($r * 2), 180, 90)
    $put.AddArc([float]($x + $w - $r * 2), [float]$y, [float]($r * 2), [float]($r * 2), 270, 90)
    $put.AddArc([float]($x + $w - $r * 2), [float]($y + $h - $r * 2), [float]($r * 2), [float]($r * 2), 0, 90)
    $put.AddArc([float]$x, [float]($y + $h - $r * 2), [float]($r * 2), [float]($r * 2), 90, 90)
    $put.CloseFigure()

    $g.FillPath($ploca, $put)
    $g.DrawPath($rub, $put)

    # skala obrtomera (donji deo ostaje otvoren)
    $skalaX = 58 * $s; $skalaY = 62 * $s; $skalaW = 140 * $s; $skalaH = 140 * $s
    $g.DrawArc($krem, [float]$skalaX, [float]$skalaY, [float]$skalaW, [float]$skalaH, 150, 240)

    # kazaljka
    $centarX = 128 * $s; $centarY = 132 * $s
    $g.DrawLine($kazaljka, [float]$centarX, [float]$centarY, [float](176 * $s), [float](84 * $s))
    $g.FillEllipse($ploca, [float]($centarX - 13 * $s), [float]($centarY - 13 * $s), [float](26 * $s), [float](26 * $s))
    $g.DrawEllipse($rub, [float]($centarX - 13 * $s), [float]($centarY - 13 * $s), [float](26 * $s), [float](26 * $s))

    $g.Dispose()
    return $b
}

$slike = New-Object System.Collections.ArrayList
foreach ($v in $velicine) {
    $b = Napravi-Sliku $v
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    [void]$slike.Add($ms.ToArray())
    $b.Dispose()
    $ms.Dispose()
}

$izlazni = New-Object System.IO.MemoryStream
$pisac = New-Object System.IO.BinaryWriter($izlazni)
$pisac.Write([UInt16]0)
$pisac.Write([UInt16]1)
$pisac.Write([UInt16]$velicine.Count)

$pomak = 6 + 16 * $velicine.Count
for ($i = 0; $i -lt $velicine.Count; $i++) {
    $v = $velicine[$i]
    $mera = if ($v -ge 256) { 0 } else { $v }
    $pisac.Write([byte]$mera)
    $pisac.Write([byte]$mera)
    $pisac.Write([byte]0)
    $pisac.Write([byte]0)
    $pisac.Write([UInt16]1)
    $pisac.Write([UInt16]32)
    $pisac.Write([UInt32]$slike[$i].Length)
    $pisac.Write([UInt32]$pomak)
    $pomak += $slike[$i].Length
}
foreach ($slika in $slike) { $pisac.Write($slika) }
$pisac.Flush()

[System.IO.File]::WriteAllBytes($izlaz, $izlazni.ToArray())
$pisac.Dispose()
$izlazni.Dispose()

"Gotovo: $izlaz ($([Math]::Round((Get-Item $izlaz).Length / 1024, 1)) KB)"

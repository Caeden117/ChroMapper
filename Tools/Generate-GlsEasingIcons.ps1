#Requires -Version 7.0
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../Assets/_Graphics/Textures/GLS Event Icons/Easings')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# These standard equations correspond to the curve identities published by easings.net at the recorded source revision.
$c1 = 1.70158
$c2 = $c1 * 1.525
$c3 = $c1 + 1
$c4 = (2 * [Math]::PI) / 3
$c5 = (2 * [Math]::PI) / 4.5

# Bounce composition reuses the exact piecewise curve for its In and InOut mirrors.
function Get-BounceOut([double]$x) {
    $n1 = 7.5625
    $d1 = 2.75
    if ($x -lt (1 / $d1)) {
        return $n1 * $x * $x
    }
    if ($x -lt (2 / $d1)) {
        $x -= 1.5 / $d1
        return ($n1 * $x * $x) + 0.75
    }
    if ($x -lt (2.5 / $d1)) {
        $x -= 2.25 / $d1
        return ($n1 * $x * $x) + 0.9375
    }

    $x -= 2.625 / $d1
    return ($n1 * $x * $x) + 0.984375
}

# Output order mirrors Beatmap.Enums.EaseType, followed by the three Beat Saber-specific variants absent from easings.net.
$easings = [ordered]@{
    EaseLinear = { param($x) $x }
    EaseInQuadratic = { param($x) $x * $x }
    EaseOutQuadratic = { param($x) 1 - ((1 - $x) * (1 - $x)) }
    EaseInOutQuadratic = { param($x) if ($x -lt 0.5) { 2 * $x * $x } else { 1 - ([Math]::Pow((-2 * $x) + 2, 2) / 2) } }
    EaseInSinusoidal = { param($x) 1 - [Math]::Cos(($x * [Math]::PI) / 2) }
    EaseOutSinusoidal = { param($x) [Math]::Sin(($x * [Math]::PI) / 2) }
    EaseInOutSinusoidal = { param($x) -([Math]::Cos([Math]::PI * $x) - 1) / 2 }
    EaseInCubic = { param($x) $x * $x * $x }
    EaseOutCubic = { param($x) 1 - [Math]::Pow(1 - $x, 3) }
    EaseInOutCubic = { param($x) if ($x -lt 0.5) { 4 * $x * $x * $x } else { 1 - ([Math]::Pow((-2 * $x) + 2, 3) / 2) } }
    EaseInQuartic = { param($x) [Math]::Pow($x, 4) }
    EaseOutQuartic = { param($x) 1 - [Math]::Pow(1 - $x, 4) }
    EaseInOutQuartic = { param($x) if ($x -lt 0.5) { 8 * [Math]::Pow($x, 4) } else { 1 - ([Math]::Pow((-2 * $x) + 2, 4) / 2) } }
    EaseInQuintic = { param($x) [Math]::Pow($x, 5) }
    EaseOutQuintic = { param($x) 1 - [Math]::Pow(1 - $x, 5) }
    EaseInOutQuintic = { param($x) if ($x -lt 0.5) { 16 * [Math]::Pow($x, 5) } else { 1 - ([Math]::Pow((-2 * $x) + 2, 5) / 2) } }
    EaseInExponential = { param($x) if ($x -eq 0) { 0 } else { [Math]::Pow(2, (10 * $x) - 10) } }
    EaseOutExponential = { param($x) if ($x -eq 1) { 1 } else { 1 - [Math]::Pow(2, -10 * $x) } }
    EaseInOutExponential = { param($x) if ($x -eq 0) { 0 } elseif ($x -eq 1) { 1 } elseif ($x -lt 0.5) { [Math]::Pow(2, (20 * $x) - 10) / 2 } else { (2 - [Math]::Pow(2, (-20 * $x) + 10)) / 2 } }
    EaseInCircular = { param($x) 1 - [Math]::Sqrt(1 - [Math]::Pow($x, 2)) }
    EaseOutCircular = { param($x) [Math]::Sqrt(1 - [Math]::Pow($x - 1, 2)) }
    EaseInOutCircular = { param($x) if ($x -lt 0.5) { (1 - [Math]::Sqrt(1 - [Math]::Pow(2 * $x, 2))) / 2 } else { ([Math]::Sqrt(1 - [Math]::Pow((-2 * $x) + 2, 2)) + 1) / 2 } }
    EaseInBack = { param($x) ($c3 * $x * $x * $x) - ($c1 * $x * $x) }
    EaseOutBack = { param($x) 1 + ($c3 * [Math]::Pow($x - 1, 3)) + ($c1 * [Math]::Pow($x - 1, 2)) }
    EaseInOutBack = { param($x) if ($x -lt 0.5) { ([Math]::Pow(2 * $x, 2) * ((($c2 + 1) * 2 * $x) - $c2)) / 2 } else { ([Math]::Pow((2 * $x) - 2, 2) * ((($c2 + 1) * (($x * 2) - 2)) + $c2) + 2) / 2 } }
    EaseInElastic = { param($x) if ($x -eq 0) { 0 } elseif ($x -eq 1) { 1 } else { -[Math]::Pow(2, (10 * $x) - 10) * [Math]::Sin((($x * 10) - 10.75) * $c4) } }
    EaseOutElastic = { param($x) if ($x -eq 0) { 0 } elseif ($x -eq 1) { 1 } else { ([Math]::Pow(2, -10 * $x) * [Math]::Sin((($x * 10) - 0.75) * $c4)) + 1 } }
    EaseInOutElastic = { param($x) if ($x -eq 0) { 0 } elseif ($x -eq 1) { 1 } elseif ($x -lt 0.5) { -([Math]::Pow(2, (20 * $x) - 10) * [Math]::Sin(((20 * $x) - 11.125) * $c5)) / 2 } else { ([Math]::Pow(2, (-20 * $x) + 10) * [Math]::Sin(((20 * $x) - 11.125) * $c5) / 2) + 1 } }
    EaseInBounce = { param($x) 1 - (Get-BounceOut (1 - $x)) }
    EaseOutBounce = { param($x) Get-BounceOut $x }
    EaseInOutBounce = { param($x) if ($x -lt 0.5) { (1 - (Get-BounceOut (1 - (2 * $x)))) / 2 } else { (1 + (Get-BounceOut ((2 * $x) - 1))) / 2 } }
    EaseBeatSaberInOutBack = { param($x) if ($x -lt 0.517) { 5.014 * $x * $x * $x } else { 1 + (2.70158 * [Math]::Pow((1.665 * ($x - 0.4)) - 1, 3)) + (1.70158 * [Math]::Pow((1.665 * ($x - 0.4)) - 1, 2)) } }
    EaseBeatSaberInOutElastic = { param($x) if ($x -lt 0.3) { 37.037 * $x * $x * $x } else { ([Math]::Pow(2, -10 * ($x - 0.2)) * [Math]::Sin($x * 10 * ([Math]::PI * 2 / 3))) + 1 } }
    EaseBeatSaberInOutBounce = { param($x) if ($x -lt 0.36363637) { 20.796 * $x * $x * $x } elseif ($x -lt 0.72727275) { $offset = $x - 0.54545456; (7.5625 * $offset * $offset) + 0.75 } elseif ($x -lt 0.90909094) { $offset = $x - 0.8181818; (7.5625 * $offset * $offset) + 0.9375 } else { $offset = $x - (21 / 22); (7.5625 * $offset * $offset) + (63 / 64) } }
}

$markers = [ordered]@{
    NoEasingStep = { param($x) if ($x -lt 1) { 0 } else { 1 } }
}

Add-Type -AssemblyName System.Drawing.Common
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$iconHeight = 128
$easingDisplayAspect = 0.3168 / 0.198      # GLSEventIconView width/height footprint before the shared 1.15 fit
$circularDisplayAspect = 0.22 / 0.198      # CircularEasingsKeepOriginalWidth's narrower footprint, same height
$circularEasingNames = @('EaseInCircular', 'EaseOutCircular', 'EaseInOutCircular')
$sampleCount = 512

# Keep white stroke and black border widths independent so changing the border does not resize the icon.
$foregroundWidth = 11.8125
$borderWidth = 5.34375
$outlineWidth = $foregroundWidth + (2 * $borderWidth)

# Margin must clear half the total stroke or the outline clips at the canvas edge where curves touch 0/1.
$margin = [int][Math]::Ceiling(($outlineWidth / 2) + 1)

# Missing sprite metas get fresh GUIDs cloned from a committed sibling template so prefab sprite references
# survive regeneration on any machine; existing metas are never touched.
$spriteMetaTemplatePath = Join-Path $OutputDirectory 'EaseLinear.png.meta'
function Ensure-SpriteMeta([string]$pngPath) {
    $metaPath = "$pngPath.meta"
    if ((Test-Path -LiteralPath $metaPath) -or -not (Test-Path -LiteralPath $spriteMetaTemplatePath)) {
        return
    }
    $guid = [guid]::NewGuid().ToString('N')
    $spriteId = [guid]::NewGuid().ToString('N').Substring(0, 16) + '0800000000000000'
    (Get-Content -LiteralPath $spriteMetaTemplatePath -Raw) `
        -replace 'guid: [0-9a-f]{32}', "guid: $guid" `
        -replace 'spriteID: [0-9a-f]{32}', "spriteID: $spriteId" |
        Set-Content -LiteralPath $metaPath -Encoding utf8 -NoNewline
}

foreach ($entry in @($easings.GetEnumerator()) + @($markers.GetEnumerator())) {
    $values = [double[]]::new($sampleCount)
    $minimum = 0.0
    $maximum = 1.0
    for ($i = 0; $i -lt $sampleCount; $i++) {
        $x = $i / ($sampleCount - 1)
        $value = [double](& $entry.Value $x)
        $values[$i] = $value
        $minimum = [Math]::Min($minimum, $value)
        $maximum = [Math]::Max($maximum, $value)
    }

    # Overshooting curves receive proportional vertical padding while ordinary curves retain the full icon height.
    if ($minimum -lt 0 -or $maximum -gt 1) {
        $padding = ($maximum - $minimum) * 0.08
        $minimum -= $padding
        $maximum += $padding
    }

    $aspect = ($circularEasingNames -contains $entry.Key) ? $circularDisplayAspect : $easingDisplayAspect
    $iconWidth = [int][Math]::Round($iconHeight * $aspect)

    $points = [System.Drawing.PointF[]]::new($sampleCount)
    for ($i = 0; $i -lt $sampleCount; $i++) {
        $normalizedX = $i / ($sampleCount - 1)
        $normalizedY = ($values[$i] - $minimum) / ($maximum - $minimum)
        $points[$i] = [System.Drawing.PointF]::new(
            $margin + ($normalizedX * ($iconWidth - (2 * $margin))),
            ($iconHeight - $margin) - ($normalizedY * ($iconHeight - (2 * $margin))))
    }

    $bitmap = [System.Drawing.Bitmap]::new(
        $iconWidth,
        $iconHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            # GeneratedIconsUseOutlinedStrokes paints both strokes into one atlas sprite, preserving the existing renderer and draw-call count.
            $pens = @(
                [System.Drawing.Pen]::new([System.Drawing.Color]::Black, $outlineWidth),
                [System.Drawing.Pen]::new([System.Drawing.Color]::White, $foregroundWidth)
            )
            try {
                foreach ($pen in $pens) {
                    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
                    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
                    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
                    $graphics.DrawLines($pen, $points)
                }
            }
            finally {
                foreach ($pen in $pens) {
                    $pen.Dispose()
                }
            }
        }
        finally {
            $graphics.Dispose()
        }

        $fillRect = [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height)
        $pixelData = $bitmap.LockBits(
            $fillRect,
            [System.Drawing.Imaging.ImageLockMode]::ReadWrite,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $pixelBytes = [byte[]]::new($pixelData.Stride * $pixelData.Height)
            [System.Runtime.InteropServices.Marshal]::Copy(
                $pixelData.Scan0, $pixelBytes, 0, $pixelBytes.Length)
            for ($p = 0; $p -lt $pixelBytes.Length; $p += 4) {
                if ($pixelBytes[$p + 3] -eq 0) {
                    $pixelBytes[$p] = 255
                    $pixelBytes[$p + 1] = 255
                    $pixelBytes[$p + 2] = 255
                }
            }
            [System.Runtime.InteropServices.Marshal]::Copy(
                $pixelBytes, 0, $pixelData.Scan0, $pixelBytes.Length)
        }
        finally {
            $bitmap.UnlockBits($pixelData)
        }

        $destination = Join-Path $OutputDirectory "$($entry.Key).png"
        $bitmap.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }

    Ensure-SpriteMeta $destination
}

# Keep the visual reference and the Beat Saber-only extension explicit without importing GPL-3 site artwork.
$manifest = [ordered]@{
    source = 'https://easings.net/'
    sourceRepository = 'https://github.com/ai/easings.net'
    sourceRevision = 'd2563f0d32a511b5556774b838ec35c3a841b15d'
    sourceFunctions = 'src/easings/easingsFunctions.ts'
    beatSaberSource = 'Beat Saber 1.44.1 Tweening.dll, Tweening.Easing'
    beatSaberSourceMethods = @('BeatSaberInOutBack', 'BeatSaberInOutElastic', 'BeatSaberInOutBounce')
    rendering = 'Original transparent curve glyphs generated from the standard published equations and the three Beat Saber-specific methods; no site bitmap assets copied.'
    iconHeight = $iconHeight
    easingIconWidth = [int][Math]::Round($iconHeight * $easingDisplayAspect)
    circularIconWidth = [int][Math]::Round($iconHeight * $circularDisplayAspect)
    margin = $margin
    borderWidth = $borderWidth
    outlineWidth = $outlineWidth
    foregroundWidth = $foregroundWidth
    samplesPerCurve = $sampleCount
    standardIcons = @($easings.Keys | Where-Object { $_ -notlike 'EaseBeatSaber*' })
    beatSaberSpecificIcons = @($easings.Keys | Where-Object { $_ -like 'EaseBeatSaber*' })
    generatedMarkerIcons = @($markers.Keys)
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'easings-source-manifest.json') -Encoding utf8

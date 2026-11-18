#Requires -Version 7.0
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\Assets\_Graphics\Textures\GLS Event Icons')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing.Common
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$iconWidth = 128
$iconHeight = 160
$verticalPadding = 16.0
$circleCenter = [System.Drawing.PointF]::new(64.0, 64.0 + $verticalPadding)
$circleRadius = 43.0
$sampleCount = 192
$outlineWidth = 18.0
$foregroundWidth = 9.5

$arrowScale = 3.0
$arrowBlackScale = 1.05
$arrowWhiteScale = 0.78
$autoPinkScale = $arrowWhiteScale * 0.5
$arrowLength = 18.0
$arrowHalfWidth = 9.0
$directionArrowHeadingDegrees = -20.0
$autoArrowHeadingDegrees = 10
$autoTriangleCenterOffset = 7.5
$directionArrowCenterX = 51.0
$directionArrowCenterY = 25.0 + $verticalPadding
$autoArrowVerticalOffset = -7.0
$autoInnerColor = [System.Drawing.Color]::FromArgb(255, 255, 182, 193)

function New-CircularArcPoints(
    [double]$startDegrees,
    [double]$endDegrees) {
    $points = [System.Drawing.PointF[]]::new($sampleCount)
    for ($i = 0; $i -lt $sampleCount; $i++) {
        $progress = $i / ($sampleCount - 1)
        $degrees = $startDegrees + (($endDegrees - $startDegrees) * $progress)
        $radians = $degrees * ([Math]::PI / 180.0)
        $points[$i] = [System.Drawing.PointF]::new(
            $circleCenter.X + ($circleRadius * [Math]::Cos($radians)),
            $circleCenter.Y + ($circleRadius * [Math]::Sin($radians)))
    }

    return $points
}

function New-OpenAutoRingPoints {
    return New-CircularArcPoints 245.0 -65.0
}

function Get-MirroredPoints([System.Drawing.PointF[]]$points) {
    $mirrored = [System.Drawing.PointF[]]::new($points.Length)
    for ($i = 0; $i -lt $points.Length; $i++) {
        $mirrored[$i] = [System.Drawing.PointF]::new(
            (2 * $circleCenter.X) - $points[$i].X,
            $points[$i].Y)
    }

    return $mirrored
}

function New-ArrowTriangle(
    [System.Drawing.PointF]$center,
    [double]$headingDegrees,
    [double]$scale) {
    $radians = $headingDegrees * ([Math]::PI / 180.0)
    $directionX = [Math]::Cos($radians)
    $directionY = [Math]::Sin($radians)
    $perpendicularX = -$directionY
    $perpendicularY = $directionX
    $length = $arrowLength * $arrowScale * $scale
    $halfWidth = $arrowHalfWidth * $arrowScale * $scale
    $tipDistance = (2.0 * $length) / 3.0
    $baseDistance = $length / 3.0
    return [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(
            $center.X + ($directionX * $tipDistance),
            $center.Y + ($directionY * $tipDistance)),
        [System.Drawing.PointF]::new(
            $center.X - ($directionX * $baseDistance) + ($perpendicularX * $halfWidth),
            $center.Y - ($directionY * $baseDistance) + ($perpendicularY * $halfWidth)),
        [System.Drawing.PointF]::new(
            $center.X - ($directionX * $baseDistance) - ($perpendicularX * $halfWidth),
            $center.Y - ($directionY * $baseDistance) - ($perpendicularY * $halfWidth)))
}

function Draw-PathLayer(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.PointF[]]$points,
    [System.Drawing.Color]$color,
    [double]$width) {
    $pen = [System.Drawing.Pen]::new($color, $width)
    try {
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $graphics.DrawLines($pen, $points)
    }
    finally {
        $pen.Dispose()
    }
}

function Draw-TriangleLayer(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.PointF[]]$points,
    [System.Drawing.Color]$color) {
    $brush = [System.Drawing.SolidBrush]::new($color)
    try {
        $graphics.FillPolygon($brush, $points)
    }
    finally {
        $brush.Dispose()
    }
}

function Draw-BlackGlyphLayer(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.PointF[]]$arc,
    [System.Drawing.PointF[]]$firstTriangle,
    [System.Drawing.PointF[]]$secondTriangle = $null) {
    Draw-PathLayer $graphics $arc ([System.Drawing.Color]::Black) $outlineWidth
    Draw-TriangleLayer $graphics $firstTriangle ([System.Drawing.Color]::Black)
    if ($null -ne $secondTriangle) {
        Draw-TriangleLayer $graphics $secondTriangle ([System.Drawing.Color]::Black)
    }
}

function Draw-WhiteGlyphLayer(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.PointF[]]$arc,
    [System.Drawing.PointF[]]$firstTriangle,
    [System.Drawing.PointF[]]$secondTriangle = $null) {
    Draw-TriangleLayer $graphics $firstTriangle ([System.Drawing.Color]::White)
    if ($null -ne $secondTriangle) {
        Draw-TriangleLayer $graphics $secondTriangle ([System.Drawing.Color]::White)
    }
    Draw-PathLayer $graphics $arc ([System.Drawing.Color]::White) $foregroundWidth
}

function Draw-PinkAutoLayer(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.PointF[]]$leftTriangle,
    [System.Drawing.PointF[]]$rightTriangle) {
    Draw-TriangleLayer $graphics $leftTriangle $autoInnerColor
    Draw-TriangleLayer $graphics $rightTriangle $autoInnerColor
}

function New-IconCanvas {
    $bitmap = [System.Drawing.Bitmap]::new(
        $iconWidth,
        $iconHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    return [pscustomobject]@{ Bitmap = $bitmap; Graphics = $graphics }
}

$clockwiseArc = New-CircularArcPoints 90.0 245.0
$counterClockwiseArc = Get-MirroredPoints $clockwiseArc
$autoArc = New-OpenAutoRingPoints

$clockwiseCenter = [System.Drawing.PointF]::new($directionArrowCenterX, $directionArrowCenterY)
$clockwiseOuter = New-ArrowTriangle $clockwiseCenter $directionArrowHeadingDegrees $arrowBlackScale
$clockwiseInner = New-ArrowTriangle $clockwiseCenter $directionArrowHeadingDegrees $arrowWhiteScale
$counterClockwiseOuter = Get-MirroredPoints $clockwiseOuter
$counterClockwiseInner = Get-MirroredPoints $clockwiseInner

foreach ($direction in @(
    [pscustomobject]@{
        Name = 'RotationClockwise'
        Arc = $clockwiseArc
        Outer = $clockwiseOuter
        Inner = $clockwiseInner
    },
    [pscustomobject]@{
        Name = 'RotationCounterClockwise'
        Arc = $counterClockwiseArc
        Outer = $counterClockwiseOuter
        Inner = $counterClockwiseInner
    })) {
    $canvas = New-IconCanvas
    try {
    
        Draw-BlackGlyphLayer $canvas.Graphics $direction.Arc $direction.Outer
        Draw-WhiteGlyphLayer $canvas.Graphics $direction.Arc $direction.Inner
        $canvas.Bitmap.Save(
            (Join-Path $OutputDirectory "$($direction.Name).png"),
            [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $canvas.Graphics.Dispose()
        $canvas.Bitmap.Dispose()
    }
}

$autoLeftCenter = [System.Drawing.PointF]::new(
    $directionArrowCenterX-$autoTriangleCenterOffset,
    $directionArrowCenterY + $autoArrowVerticalOffset)
$autoLeftOuter = New-ArrowTriangle $autoLeftCenter $autoArrowHeadingDegrees $arrowBlackScale
$autoLeftInner = New-ArrowTriangle $autoLeftCenter $autoArrowHeadingDegrees $arrowWhiteScale
$autoLeftPink = New-ArrowTriangle $autoLeftCenter $autoArrowHeadingDegrees $autoPinkScale
$autoRightOuter = Get-MirroredPoints $autoLeftOuter
$autoRightInner = Get-MirroredPoints $autoLeftInner
$autoRightPink = Get-MirroredPoints $autoLeftPink

$autoCanvas = New-IconCanvas
try {

    Draw-BlackGlyphLayer $autoCanvas.Graphics $autoArc $autoLeftOuter $autoRightOuter
    Draw-WhiteGlyphLayer $autoCanvas.Graphics $autoArc $autoLeftInner $autoRightInner
    Draw-PinkAutoLayer $autoCanvas.Graphics $autoLeftPink $autoRightPink
    $autoCanvas.Bitmap.Save(
        (Join-Path $OutputDirectory 'RotationAutomatic.png'),
        [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $autoCanvas.Graphics.Dispose()
    $autoCanvas.Bitmap.Dispose()
}

$manifest = [ordered]@{
    construction = 'Original mathematical glyphs; no OE direction pixels copied.'
    iconWidth = $iconWidth
    iconHeight = $iconHeight
    verticalPadding = $verticalPadding
    circleCenter = @($circleCenter.X, $circleCenter.Y)
    circleRadius = $circleRadius
    samplesPerHalf = $sampleCount
    outlineWidth = $outlineWidth
    foregroundWidth = $foregroundWidth
    arrowScale = $arrowScale
    arrowBlackScale = $arrowBlackScale
    arrowWhiteScale = $arrowWhiteScale
    autoPinkScale = $autoPinkScale
    directionArrowHeadingDegrees = $directionArrowHeadingDegrees
    autoArrowHeadingDegrees = $autoArrowHeadingDegrees
    autoArrowVerticalOffset = $autoArrowVerticalOffset
    autoInnerColor = '#FFB6C1'
}
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (
    Join-Path $OutputDirectory 'rotation-direction-source-manifest.json') -Encoding utf8

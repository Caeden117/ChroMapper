using System.Collections.Generic;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSStrobeSdfContinuityTest
    {
        private sealed class ReferenceCurve
        {
            private const int SegmentsPerBlock = 32;
            private readonly Vector2[] points;
            private readonly Vector2[] blockYRanges;

            public ReferenceCurve(EaseType easing, float pulseWidth)
            {
                var curve = Easing.FromID((int)easing);
                GLSStrobePulseWidthIconTest.GetCurveRange(curve, out var minimum, out var maximum);
                var path = new List<Vector2>();
                Vector2 Point(float phase, float value)
                {
                    return new Vector2(13f + (258f * phase), 13f + (102f * (value - minimum) / (maximum - minimum)));
                }

                if (pulseWidth == 0f)
                {
                    path.Add(Point(0, 0));
                    path.Add(Point(0, 1));
                }
                else
                {
                    for (var i = 0; i <= 2048; i++)
                    {
                        var progress = i / 2048f;
                        path.Add(Point(pulseWidth * progress, curve(progress)));
                    }
                }

                if (pulseWidth == 1f)
                {
                    path.Add(Point(1, 0));
                }
                else
                {
                    for (var i = 0; i <= 2048; i++)
                    {
                        var progress = i / 2048f;
                        path.Add(Point(pulseWidth + ((1f - pulseWidth) * progress), 1f - curve(progress)));
                    }
                }

                points = path.ToArray();
                blockYRanges = new Vector2[(points.Length - 1 + SegmentsPerBlock - 1) / SegmentsPerBlock];
                for (var block = 0; block < blockYRanges.Length; block++)
                {
                    var first = block * SegmentsPerBlock;
                    var last = Mathf.Min(first + SegmentsPerBlock, points.Length - 1);
                    var minimumY = points[first].y;
                    var maximumY = minimumY;
                    for (var point = first + 1; point <= last; point++)
                    {
                        minimumY = Mathf.Min(minimumY, points[point].y);
                        maximumY = Mathf.Max(maximumY, points[point].y);
                    }

                    blockYRanges[block] = new Vector2(minimumY, maximumY);
                }
            }

            public float Distance(Vector2 coordinate)
            {
                // Only segments within the stroke's horizontal reach can affect these coverage assertions.
                var maximumX = coordinate.x + 14f;
                var firstSegment = Mathf.Max(0, FindFirstPoint(coordinate.x - 14f) - 1);
                var closest = float.PositiveInfinity;
                // A nearby segment gives the block bounds a useful distance before scanning from the left edge.
                var nearestSegment = Mathf.Clamp(FindFirstPoint(coordinate.x) - 1, 0, points.Length - 2);
                if (nearestSegment >= firstSegment && points[nearestSegment].x <= maximumX)
                {
                    closest = SegmentDistanceSquared(coordinate, nearestSegment);
                }

                for (var block = firstSegment / SegmentsPerBlock;
                     block < blockYRanges.Length && points[block * SegmentsPerBlock].x <= maximumX;
                     block++)
                {
                    var first = Mathf.Max(firstSegment, block * SegmentsPerBlock);
                    var last = Mathf.Min((block + 1) * SegmentsPerBlock, points.Length - 1);
                    // The block's bounds give a lower distance bound without projecting onto every segment.
                    var range = blockYRanges[block];
                    var horizontalGap = Mathf.Max(0f, Mathf.Max(points[first].x - coordinate.x,
                        coordinate.x - points[last].x));
                    var verticalGap = Mathf.Max(0f, Mathf.Max(range.x - coordinate.y, coordinate.y - range.y));
                    if (horizontalGap * horizontalGap + verticalGap * verticalGap > closest)
                    {
                        continue;
                    }

                    for (var i = first; i < last && points[i].x <= maximumX; i++)
                    {
                        closest = Mathf.Min(closest, SegmentDistanceSquared(coordinate, i));
                    }
                }

                return Mathf.Sqrt(closest);
            }

            private int FindFirstPoint(float x)
            {
                var lower = 0;
                var upper = points.Length;
                while (lower < upper)
                {
                    var middle = lower + ((upper - lower) / 2);
                    if (points[middle].x < x)
                    {
                        lower = middle + 1;
                    }
                    else
                    {
                        upper = middle;
                    }
                }

                return lower;
            }

            private float SegmentDistanceSquared(Vector2 coordinate, int index)
            {
                var start = points[index];
                var delta = points[index + 1] - start;
                var position = Mathf.Clamp01(Vector2.Dot(coordinate - start, delta) / Mathf.Max(delta.sqrMagnitude, 1e-12f));
                var offset = coordinate - start - (position * delta);
                return offset.sqrMagnitude;
            }
        }

        [TestCase(EaseType.InOutCubic, 0.95f)]
        [TestCase(EaseType.OutCircular, 0.95f)]
        [TestCase(EaseType.OutElastic, 0.95f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.95f)]
        public void PixelsBeyondTheStrokeDoNotGainWhiteEdgeStripes(EaseType easing, float pulseWidth)
        {
            var pixelOffset = new Vector2(-1.25f, 0.75f);
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(easing, pulseWidth, Color.green, pixelOffset);
            var reference = new ReferenceCurve(easing, pulseWidth);
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 568; x++)
                {
                    var pixel = pixels[(y * 568) + x];
                    if (pixel.r <= 15)
                    {
                        continue;
                    }

                    var distance = reference.Distance(new Vector2(
                        (x + 0.5f + pixelOffset.x) * 0.5f,
                        (y + 0.5f + pixelOffset.y) * 0.5f));
                    Assert.That(distance, Is.LessThanOrEqualTo(12.75f),
                        $"{easing}, pulse width {pulseWidth}: stray white coverage at ({x}, {y}), distance={distance}.");
                }
            }
        }

        [TestCase(EaseType.OutCircular, 0.97f)]
        public void PulseJoinHasNoDarkSeamInsideItsWhiteCore(EaseType easing, float pulseWidth)
        {
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(easing, pulseWidth, Color.gray);
            var reference = new ReferenceCurve(easing, pulseWidth);
            var split = 13f + (258f * pulseWidth);
            var minimumX = Mathf.Max(0, Mathf.FloorToInt((split - 20f) * 2f));
            var maximumX = Mathf.Min(568, Mathf.CeilToInt((split + 20f) * 2f));
            for (var y = 0; y < 256; y++)
            {
                for (var x = minimumX; x < maximumX; x++)
                {
                    var distance = reference.Distance(new Vector2((x + 0.5f) * 0.5f, (y + 0.5f) * 0.5f));
                    if (distance < 4.9f)
                    {
                        Assert.That(pixels[(y * 568) + x].r, Is.GreaterThanOrEqualTo(240),
                            $"{easing}, pulse width {pulseWidth}: dark seam near phase join at ({x}, {y}), distance={distance}.");
                    }
                }
            }
        }

        [TestCase(0.35f)]
        [TestCase(0.65f)]
        public void ConcaveInBounceArcsDoNotGrowOutlineSpikes(float pulseWidth)
        {
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(EaseType.InBounce, pulseWidth, Color.green);
            var reference = new ReferenceCurve(EaseType.InBounce, pulseWidth);
            var darkest = 255;
            var darkestPosition = Vector2Int.zero;
            // The falling phase's largest concave arc turns at progress 5/11 and native height 89.5.
            var arcCenter = 13f + (258f * (pulseWidth + ((1f - pulseWidth) * 5f / 11f)));
            var farthestOutline = 0f;
            var farthestPosition = Vector2Int.zero;
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 568; x++)
                {
                    var coordinate = new Vector2((x + 0.5f) * 0.5f, (y + 0.5f) * 0.5f);
                    var distance = reference.Distance(coordinate);
                    var red = pixels[(y * 568) + x].r;
                    if (distance < 4.9f && red < darkest)
                    {
                        darkest = red;
                        darkestPosition = new Vector2Int(x, y);
                    }

                    if (Mathf.Abs(coordinate.x - arcCenter) < 8f && coordinate.y > 89.5f
                        && pixels[(y * 568) + x].g < 240 && distance > farthestOutline)
                    {
                        farthestOutline = distance;
                        farthestPosition = new Vector2Int(x, y);
                    }
                }
            }

            Assert.That(darkest, Is.GreaterThanOrEqualTo(240),
                $"InBounce, pulse width {pulseWidth}: dark spike inside white core at {darkestPosition}.");
            Assert.That(farthestOutline, Is.LessThanOrEqualTo(12.25f),
                $"InBounce, pulse width {pulseWidth}: outline spike at {farthestPosition}, distance={farthestOutline}.");
        }

        [TestCase(EaseType.InOutCubic, 0.95f)]
        [TestCase(EaseType.OutElastic, 0.95f)]
        [TestCase(EaseType.OutBounce, 0.95f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.95f)]
        [TestCase(EaseType.InBounce, 0.05f)]
        [TestCase(EaseType.InOutBounce, 0.95f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.97f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.03f)]
        [TestCase(EaseType.OutBounce, 0.374f)]
        [TestCase(EaseType.OutBounce, 0.376f)]
        [TestCase(EaseType.OutBounce, 0.624f)]
        [TestCase(EaseType.OutBounce, 0.626f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.374f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.376f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.624f)]
        [TestCase(EaseType.BeatSaberInOutBounce, 0.626f)]
        public void DenseCurveCoreAndBorderStayContinuous(EaseType easing, float pulseWidth)
        {
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(easing, pulseWidth, Color.gray);
            var reference = new ReferenceCurve(easing, pulseWidth);
            // These cases also check every pixel around the join, sharing the render and distance calculation.
            var checkJoin = pulseWidth == 0.95f
                && easing is EaseType.OutBounce or EaseType.BeatSaberInOutBounce or EaseType.OutElastic;
            var sampleStep = checkJoin ? 1 : 2;
            var split = 13f + (258f * pulseWidth);
            var minimumX = Mathf.Max(0, Mathf.FloorToInt((split - 20f) * 2f));
            var maximumX = Mathf.Min(568, Mathf.CeilToInt((split + 20f) * 2f));
            for (var y = 0; y < 256; y += sampleStep)
            {
                for (var x = 0; x < 568; x += sampleStep)
                {
                    var densePixel = y % 2 == 0 && x % 2 == 0;
                    var joinPixel = checkJoin && x >= minimumX && x < maximumX;
                    if (!densePixel && !joinPixel)
                    {
                        continue;
                    }

                    var distance = reference.Distance(new Vector2((x + 0.5f) * 0.5f, (y + 0.5f) * 0.5f));
                    var pixel = pixels[(y * 568) + x];
                    if (distance < 4.9f)
                    {
                        Assert.That(pixel.r, Is.GreaterThanOrEqualTo(240),
                            $"{easing}, pulse width {pulseWidth}: dark seam inside white core at ({x}, {y}), distance={distance}.");
                    }
                    else if (densePixel && distance > 7f && distance < 10.25f)
                    {
                        Assert.That(pixel.r, Is.LessThanOrEqualTo(15),
                            $"{easing}, pulse width {pulseWidth}: interrupted black outline at ({x}, {y}), distance={distance}.");
                    }
                }
            }
        }
    }
}

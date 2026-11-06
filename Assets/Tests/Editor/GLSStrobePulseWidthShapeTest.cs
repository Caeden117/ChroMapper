using System;
using System.IO;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSStrobePulseWidthShapeTest
    {
        [TestCase("GLS Event")]
        [TestCase("GLS Group")]
        public void AllCyclesUseTenPercentLessThanTheOutCircularWidth(string prefabName)
        {
            Assert.That(GLSStrobeIconRenderer.Size.x, Is.EqualTo(2.84f));
            Assert.That(GLSStrobeIconRenderer.Size.y, Is.EqualTo(1.28f));
            foreach (EaseType easing in Enum.GetValues(typeof(EaseType)))
            {
                var instance = GLSStrobePulseWidthIconTest.CreateIcon(easing, 0.5f, prefabName);
                try
                {
                    var side = instance.transform.Find("Secondary Icon Side");
                    var expectedScale = GLSEventIconView.EasingIconHeight * GLSEventIconView.ColorIconScale * 0.85f;
                    Assert.That(side.localScale.x, Is.EqualTo(expectedScale).Within(0.00001f), easing.ToString());
                    Assert.That(side.localScale.y, Is.EqualTo(expectedScale).Within(0.00001f), easing.ToString());
                    foreach (var face in new[] { "Secondary Icon Side", "Secondary Icon Top" })
                    {
                        var surface = instance.transform.Find($"{face}/Strobe Icon Surface");
                        Assert.That(surface.localScale.x, Is.EqualTo(0.9f).Within(0.00001f), $"{easing}, {face}");
                        Assert.That(surface.localScale.y, Is.EqualTo(1f), $"{easing}, {face}");
                        Assert.That(surface.localPosition, Is.EqualTo(Vector3.zero), "Narrowing must preserve the icon center.");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [TestCase(0f)]
        [TestCase(1f)]
        public void EndpointCyclesDrawTheVerticalReset(float pulseWidth)
        {
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(EaseType.Linear, pulseWidth, Color.gray);
            var x = Mathf.RoundToInt(2f * (13f + (258f * pulseWidth)));
            for (var y = 40; y < 216; y++)
            {
                Assert.That(pixels[y * 568 + x].r, Is.GreaterThan(240), $"Reset line missing at {pulseWidth}, y={y}.");
            }
        }

        [TestCase(0.942f)]
        [TestCase(0.96f)]
        [TestCase(0.04f)]
        public void CoreAndBorderThicknessStayConstantAcrossPulseWidths(float pulseWidth)
        {
            var pixels = GLSStrobePulseWidthIconTest.RenderCurve(EaseType.Linear, pulseWidth, Color.gray);
            var rising = pulseWidth > 0.5f;
            var start = new Vector2(13f + (rising ? 0f : 258f * pulseWidth), rising ? 13f : 115f);
            var end = new Vector2(rising ? 13f + (258f * pulseWidth) : 271f, rising ? 115f : 13f);
            var middle = (start + end) * 0.5f;
            var direction = (end - start).normalized;
            var normal = new Vector2(-direction.y, direction.x);
            foreach (var sign in new[] { -1f, 1f })
            {
                var core = Sample(pixels, middle + (normal * (3.5f * sign)));
                var border = Sample(pixels, middle + (normal * (8f * sign)));
                var outside = Sample(pixels, middle + (normal * (14f * sign)));
                Assert.That(core.r, Is.GreaterThan(240), "The white core must not compress with pulse width.");
                Assert.That(border.r, Is.LessThan(10), "The black border must keep its authored thickness.");
                Assert.That(outside.r, Is.EqualTo(128).Within(2), "The outer stroke must not stretch beyond its authored radius.");
            }
        }

        private static Color32 Sample(Color32[] pixels, Vector2 point)
        {
            return pixels[(Mathf.RoundToInt(point.y * 2f) * 568) + Mathf.RoundToInt(point.x * 2f)];
        }

        [TestCase(0.37f)]
        [TestCase(0.5f)]
        [TestCase(0.942f)]
        public void StrokeWeightsMatchTheExistingEasingIcon(float pulseWidth)
        {
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                original.LoadImage(File.ReadAllBytes("Assets/_Graphics/Textures/GLS Event Icons/Easings/EaseLinear.png"));
                var pixels = GLSStrobePulseWidthIconTest.RenderCurve(EaseType.Linear, pulseWidth, Color.gray);
                var originalMiddle = new Vector2(original.width * 0.5f, original.height * 0.5f);
                var originalNormal = new Vector2(-102f, original.width - 26f).normalized;
                var middle = new Vector2(13f + (129f * pulseWidth), 64f);
                var normal = new Vector2(-102f, 258f * pulseWidth).normalized;
                var oldCoreRadius = 0f;
                var oldOutlineRadius = 0f;
                var coreRadius = 0f;
                var outlineRadius = 0f;
                for (var distance = 0f; distance <= 16f; distance += 0.25f)
                {
                    var oldPosition = originalMiddle + (distance * originalNormal);
                    var oldPixel = original.GetPixel(Mathf.RoundToInt(oldPosition.x), Mathf.RoundToInt(oldPosition.y));
                    var pixel = Sample(pixels, middle + (distance * normal));
                    if (oldPixel.a > 0.5f)
                    {
                        oldOutlineRadius = distance;
                        if (oldPixel.r > 0.78f)
                        {
                            oldCoreRadius = distance;
                        }
                    }

                    if (pixel.r < 64 || pixel.r > 200)
                    {
                        outlineRadius = distance;
                    }

                    if (pixel.r > 200)
                    {
                        coreRadius = distance;
                    }
                }

                Assert.That(coreRadius, Is.EqualTo(oldCoreRadius).Within(1.5f), "White stroke differs from the existing icon.");
                Assert.That(outlineRadius, Is.EqualTo(oldOutlineRadius).Within(1.5f), "Black outline differs from the existing icon.");
            }
            finally
            {
                Object.DestroyImmediate(original);
            }
        }
    }
}

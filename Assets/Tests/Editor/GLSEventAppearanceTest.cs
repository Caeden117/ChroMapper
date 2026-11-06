using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Shared;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSEventAppearanceTest
    {
        // Keep normal bright GLS nodes from rendering a dark strobe band merely because strobe brightness defaults to zero.
        [Test]
        public void BrightNonStrobingColorNodeDoesNotEnableStrobeBand()
        {
            var evt = new BaseLightColorBase
            {
                Brightness = 3.7f,
                Frequency = 0,
                StrobeBrightness = 0f,
                StrobeFade = 0
            };

            Assert.False(GLSEventCommon.IsStrobing(evt));
        }

        // Retain both OEM and Chroma timing forms as valid strobe-band triggers.
        [TestCase(1, null)]
        [TestCase(0, 1f)]
        public void TimedColorNodeEnablesStrobeBand(int frequency, float? chromaInterval)
        {
            var evt = new BaseLightColorBase
            {
                Frequency = frequency,
                ChromaStrobeInterval = chromaInterval
            };

            Assert.True(GLSEventCommon.IsStrobing(evt));
        }

        // Show zero strobe brightness whenever native or custom timing enables strobing.
        [TestCase(2, null)]
        [TestCase(0, 0.5f)]
        public void TimedColorNodeDisplaysZeroStrobeBrightness(int frequency, float? chromaInterval)
        {
            var evt = new BaseLightColorBase
            {
                Frequency = frequency,
                ChromaStrobeInterval = chromaInterval,
                StrobeBrightness = 0f
            };

            var strobeBrightnessLine = GLSEventCommon.GetColorInfo(evt).Split('\n')[1];

            Assert.AreEqual("0", strobeBrightnessLine);
        }

        // Zero brightness does not disable the strobe clock.
        [TestCase(2, null, 2f)]
        [TestCase(0, 0.5f, 2f)]
        public void ZeroBrightnessStrobeKeepsFrequencyIntoTransition(
            int frequency,
            float? chromaInterval,
            float expectedFrequency)
        {
            var source = new BaseLightColorBase
            {
                Brightness = 0f,
                StrobeBrightness = 0f,
                Frequency = frequency,
                ChromaStrobeInterval = chromaInterval,
                StrobeColor = Color.red
            };
            var destination = new BaseLightColorBase
            {
                Brightness = 1f,
                StrobeBrightness = 0f,
                Frequency = 0,
                Easing = (int)Beatmap.Enums.EaseType.Linear
            };
            var sourceState = new LightColorEventStateData(source, 0f) { EndTime = 1f };
            var destinationState = new LightColorEventStateData(destination, 1f);
            sourceState.Next = destinationState;
            destinationState.Previous = sourceState;
            var tween = new LightColorTween();

            LightColorGroupEffect.ConfigureTween(
                tween,
                sourceState,
                Color.blue,
                Color.white,
                Color.red,
                Color.white,
                null);

            Assert.That(tween.StartStrobeFrequency, Is.EqualTo(expectedFrequency));
            Assert.That(tween.StartStrobeBrightness, Is.Zero);
            Assert.That(tween.StartStrobeColor, Is.EqualTo(Color.red));
        }

        [Test]
        public void ColorDistributedNodeInfoOmitsDistributionMarkers()
        {
            var evt = CreateColorDistributedEvent(out _);

            var info = GLSEventCommon.GetColorInfo(evt);

            StringAssert.DoesNotContain("Δ", info);
            StringAssert.DoesNotContain("ΔS", info);
        }

        [Test]
        public void ColorDistributedPreviewCachesSelectedLightsAndBlacksSkippedLights()
        {
            var evt = CreateColorDistributedEvent(out _);
            evt.CustomColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            evt.StrobeColor = new Color(0.2f, 0.1f, 0.4f, 1f);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var mainColors = new Color[4];
            var strobeColors = new Color[4];

            try
            {
                var enabled = PopulateColorTransitionEndpoint(
                    evt,
                    appearance,
                    mainColors,
                    strobeColors);

                Assert.That(enabled, Is.True);
                AssertColor(mainColors[0], 0.1f, 0.2f, 0.3f, 1f);
                AssertColor(mainColors[1], 0f, 0f, 0f, 1f);
                AssertColor(mainColors[2], 1.1f, 0.2f, 0.3f, 1f);
                AssertColor(mainColors[3], 0f, 0f, 0f, 1f);
                AssertColor(strobeColors[0], 0.2f, 0.1f, 0.4f, 1f);
                AssertColor(strobeColors[1], 0f, 0f, 0f, 1f);
                AssertColor(strobeColors[2], 0.2f, 1.1f, 0.4f, 1f);
                AssertColor(strobeColors[3], 0f, 0f, 0f, 1f);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // The l mode uses selected-light progress; omitted and unknown modes use affected-chunk progress.
        [Test]
        public void PerLightColorDistributionPreviewUsesAffectedLightsAcrossBoxAndEventPhases()
        {
            var evt = CreatePerLightColorDistributionEvent(out _);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.black;
            var mainColors = new Color[8];
            var strobeColors = new Color[8];

            try
            {
                var enabled = PopulateColorTransitionEndpoint(
                    evt,
                    appearance,
                    mainColors,
                    strobeColors);

                Assert.That(enabled, Is.True);
                AssertColor(mainColors[0], 0.1f, 0.2f, 0.3f, 1f);
                AssertColor(mainColors[1], 0.2f, 0.4f, 0.3f, 1f);
                AssertColor(mainColors[2], 0f, 0f, 0f, 1f);
                AssertColor(mainColors[4], 0.3f, 0.6f, 0.5f, 1f);
                AssertColor(mainColors[5], 0.4f, 0.8f, 0.5f, 1f);
                AssertColor(strobeColors[0], 0.2f, 0.1f, 0.4f, 1f);
                AssertColor(strobeColors[1], 0.5f, 0.5f, 0.4f, 1f);
                AssertColor(strobeColors[2], 0f, 0f, 0f, 1f);
                AssertColor(strobeColors[4], 0.8f, 0.9f, 0.8f, 1f);
                AssertColor(strobeColors[5], 1.1f, 1.3f, 0.8f, 1f);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerLightPlaybackEndpointsMatchPreviewAtBothStrobePhases(bool explicitStrobeColor)
        {
            var evt = CreatePerLightColorDistributionEvent(out var box);
            if (!explicitStrobeColor)
            {
                evt.StrobeColor = null;
            }
            evt.Brightness = 0.5f;
            evt.StrobeBrightness = 0.25f;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            var effectObject = new GameObject("Per-light color-distribution playback test");
            effectObject.SetActive(false);
            var boost = effectObject.AddComponent<ColorBoostEffect>();
            var effect = effectObject.AddComponent<LightColorGroupEffect>();
            effect.ColorBoostEffect = boost;
            var mainColors = new Color[8];
            var strobeColors = new Color[8];

            try
            {
                Assert.That(PopulateColorTransitionEndpoint(evt, appearance, mainColors, strobeColors), Is.True);
                var filter = IndexFilterHelper.Convert(box.IndexFilter, 8);
                var normalResolver = typeof(LightColorGroupEffect).GetMethod(
                    "ResolveNormalColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var strobeResolver = typeof(LightColorGroupEffect).GetMethod(
                    "ResolveStrobeColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(normalResolver, Is.Not.Null);
                Assert.That(strobeResolver, Is.Not.Null);
                foreach (var entry in filter)
                {
                    var chunkProgress = entry.AffectedChunkOrder / (float)Mathf.Max(filter.VisibleCount - 1, 1);
                    var lightProgress = entry.AffectedLightOrder / (float)Mathf.Max(filter.AffectedLightCount - 1, 1);
                    var state = new LightColorEventStateData(evt, 0f, 0f, box, chunkProgress, lightProgress);
                    var normal = (Color)normalResolver.Invoke(effect, new object[] { state });
                    var strobe = (Color)strobeResolver.Invoke(effect, new object[] { state });
                    // Strobe distributions start from explicit strobeColor, or the unshifted normal base when
                    // it is absent. Inherit the distributed main color only when neither strobe override is
                    // present.
                    var hasStrobeDistributions =
                        box.ParsedStrobeColorDistributions.Count > 0
                        || evt.ParsedStrobeColorDistributions.Count > 0;
                    var strobeSource = evt.StrobeColor
                        ?? (hasStrobeDistributions ? evt.CustomColor!.Value : normal);
                    AssertColor(normal, 0.1f + (0.3f * lightProgress), 0.2f + (0.6f * lightProgress),
                        0.3f + (0.2f * chunkProgress), 1f);
                    AssertColor(strobe, strobeSource.r + (0.9f * lightProgress), strobeSource.g + (1.2f * lightProgress),
                        strobeSource.b + (0.4f * chunkProgress), 1f);
                    var tween = CreateStrobingRendererTween(evt, normal, strobe);
                    tween.UpdateTime(0.25f);
                    AssertColor(mainColors[entry.Element], tween.Color);
                    tween.UpdateTime(0.75f);
                    AssertColor(strobeColors[entry.Element], tween.Color);
                }
                foreach (var skippedLight in new[] { 2, 3, 6, 7 })
                {
                    AssertColor(mainColors[skippedLight], Color.black);
                    AssertColor(strobeColors[skippedLight], Color.black);
                }
            }
            finally
            {
                Object.DestroyImmediate(effectObject);
                Object.DestroyImmediate(appearance);
            }
        }

        [Test]
        public void ReportedHsvColorDistributionPreviewMatchesLightRendererAtBothStrobePhases()
        {
            var evt = CreateReportedHsvColorDistributionEvent(out var box);
            Assert.That(evt.ParsedStrobeColorDistributions.Count, Is.EqualTo(2));
            Assert.That(evt.ParsedStrobeColorDistributions[0].Targets, Is.EqualTo(GLSColorDistributionTargets.Hue | GLSColorDistributionTargets.Saturation));
            Assert.That(evt.ParsedStrobeColorDistributions[0].Offset, Is.EqualTo(0.4f));
            Assert.That(evt.ParsedStrobeColorDistributions[1].Targets, Is.EqualTo(GLSColorDistributionTargets.Value));
            Assert.That(evt.ParsedStrobeColorDistributions[1].Offset, Is.EqualTo(2f));
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.black;
            var mainColors = new Color[8];
            var strobeColors = new Color[8];

            try
            {
                var enabled = PopulateColorTransitionEndpoint(
                    evt,
                    appearance,
                    mainColors,
                    strobeColors);

                Assert.That(enabled, Is.True);
                var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, mainColors.Length);
                Assert.That(indexFilter, Is.Not.Null);
                foreach (var entry in indexFilter)
                {
                    var progress = entry.AffectedChunkOrder
                        / (float)Mathf.Max(indexFilter.VisibleCount - 1, 1);
                    var normalColor = GLSColorDistribution.ApplyNormal(evt.CustomColor.Value, box, evt, progress);
                    var strobeColor = GLSColorDistribution.ApplyStrobe(evt.CustomColor.Value, box, evt, progress);
                    AssertColor(normalColor, EvaluateExpectedHsvColorDistribution(evt.CustomColor.Value, -0.4f, 0f, 0f, progress));
                    AssertColor(strobeColor, EvaluateExpectedHsvColorDistribution(evt.StrobeColor.Value, 0.4f, 0.4f, 2f, progress));
                    var tween = CreateStrobingRendererTween(evt, normalColor, strobeColor);

                    tween.UpdateTime(0.25f);
                    AssertColor(mainColors[entry.Element], tween.Color);
                    tween.UpdateTime(0.75f);
                    AssertColor(strobeColors[entry.Element], tween.Color);
                }
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        [Test]
        public void BrightnessDistributionEnablesPreviewWithoutColorDistributions()
        {
            var box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":0,\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":1,\"t\":1,\"b\":1,\"i\":0," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":0,\"i\":0,\"f\":0,\"sb\":1,\"sf\":0}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            evt.CustomColor = Color.white;
            Assert.That(box.BrightnessDistribution, Is.EqualTo(1f));
            Assert.That(box.BrightnessDistributionType, Is.EqualTo((int)Beatmap.Enums.DistributionType.Wave));
            Assert.That(box.BrightnessAffectFirst, Is.EqualTo(1));
            var filter = IndexFilterHelper.Convert(box.IndexFilter, 3);
            Assert.That(filter, Is.Not.Null);
            var distributionOrders = new List<int>();
            foreach (var entry in filter)
            {
                distributionOrders.Add(entry.DistributionOrder);
            }
            Assert.That(distributionOrders, Is.EqualTo(new[] { 0, 1, 2 }));
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var mainColors = new Color[3];
            var strobeColors = new Color[3];

            try
            {
                var enabled = PopulateColorTransitionEndpoint(
                    evt,
                    appearance,
                    mainColors,
                    strobeColors);

                Assert.That(enabled, Is.True);
                // Match LightColorTween: keep distributed RGB in color and carry distributed brightness in
                // alpha.
                AssertColor(mainColors[0], 1f, 1f, 1f, 0f);
                AssertColor(mainColors[1], 1f, 1f, 1f, 0.5f);
                AssertColor(mainColors[2], 1f, 1f, 1f, 1f);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // CustomDataColorAlphaMultipliesBrightnessWithoutScalingHdrRgb protects basic-event RGBA parity for the normal GLS phase.
        [Test]
        public void CustomDataColorAlphaMultipliesBrightnessWithoutScalingHdrRgb()
        {
            var tween = CreateTween(
                new Color(4f, 0.25f, 2f, 3f),
                new Color(4f, 0.25f, 2f, 3f),
                startBrightness: 2f,
                endBrightness: 2f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 4f, 0.25f, 2f, 6f);
        }

        // ExplicitStrobeColorAlphaMultipliesSbWithoutScalingHdrRgb catches the regression where preview replaces authored alpha with sb.
        [Test]
        public void ExplicitStrobeColorAlphaMultipliesSbWithoutScalingHdrRgb()
        {
            var tween = CreateTween(
                new Color(0.5f, 0.25f, 0.125f, 1f),
                new Color(0.5f, 0.25f, 0.125f, 1f),
                startBrightness: 1f,
                endBrightness: 1f,
                startStrobeColor: new Color(4f, 0.25f, 2f, 3f),
                endStrobeColor: new Color(4f, 0.25f, 2f, 3f),
                startStrobeBrightness: 2f,
                endStrobeBrightness: 2f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 4f, 0.25f, 2f, 6f);
        }

        // InheritedStrobeColorComposesEndpointAlphaBeforeTweening covers omitted strobeColor and crossed HDR alpha/sb transitions.
        [Test]
        public void InheritedStrobeColorComposesEndpointAlphaBeforeTweening()
        {
            var tween = CreateTween(
                new Color(4f, 0.25f, 2f, 2f),
                new Color(0.5f, 3f, 0.125f, 4f),
                startBrightness: 1f,
                endBrightness: 1f,
                startStrobeBrightness: 3f,
                endStrobeBrightness: 5f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 1.375f, 2.3125f, 0.59375f, 16.5f);
        }

        // Select physical lights 0, 1, 4, and 5 so chunk and light progress differ at both interior lights.
        private static BaseLightColorBase CreatePerLightColorDistributionEvent(out BaseLightColorEventBox box)
        {
            box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":4,\"f\":2,\"p\":0,\"t\":2,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":1," +
                "\"customData\":{\"colorDistributions\":[\"r,0.3,L,l\",\"b,0.2,L,future,discard\"]," +
                "\"strobeColorDistributions\":[\"r,0.9,L,l\"]}," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":1,\"i\":0,\"f\":1,\"sb\":1,\"sf\":0," +
                "\"customData\":{\"color\":[0.1,0.2,0.3],\"strobeColor\":[0.2,0.1,0.4]," +
                "\"colorDistributions\":[\"g,0.6,L,l,discard\"]," +
                "\"strobeColorDistributions\":[\"g,1.2,L,l\",\"b,0.4,L\"]}}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            return evt;
        }

        private static BaseLightColorBase CreateReportedHsvColorDistributionEvent(out BaseLightColorEventBox box)
        {
            box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":8,\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":1,\"b\":1,\"i\":0," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":1,\"i\":0,\"f\":1,\"sb\":0,\"sf\":0," +
                "\"customData\":{\"color\":[0.179,1,0],\"strobeColor\":[0.969,0,0.941]," +
                "\"colorDistributions\":[\"h,-0.4,L\"],\"strobeColorDistributions\":[\"hs,0.4,L\",\"v,2,L\"]}}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            return evt;
        }

        private static BaseLightColorBase CreateColorDistributedEvent(out BaseLightColorEventBox box)
        {
            box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":4,\"f\":2,\"p\":0,\"t\":2,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":1," +
                "\"customData\":{\"colorDistributions\":[\"r,1,L\"]}," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":1,\"i\":0,\"f\":1,\"sb\":1,\"sf\":0," +
                "\"customData\":{\"strobeColorDistributions\":[\"g,1,L\"]}}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            return evt;
        }

        // Endpoint fixtures share the production ribbon-cache entry point while keeping their fixed no-boost setup concise.
        private static bool PopulateColorTransitionEndpoint(
            BaseLightColorBase evt,
            EventAppearanceSO appearance,
            Color[] mainColors,
            Color[] strobeColors) =>
            GLSEventCommon.PopulateColorTransitionEndpoint(
                evt,
                mainColors.Length,
                false,
                appearance,
                mainColors,
                strobeColors);

        private static Color EvaluateExpectedHsvColorDistribution(
            Color source,
            float hueOffset,
            float saturationOffset,
            float valueOffset,
            float progress)
        {
            Color.RGBToHSV(source, out var hue, out var saturation, out var value);
            var expected = Color.HSVToRGB(
                Mathf.Repeat(hue + (hueOffset * progress), 1f),
                Mathf.Clamp01(saturation + (saturationOffset * progress)),
                value + (valueOffset * progress),
                true);
            expected.a = source.a;
            return expected;
        }

        private static LightColorTween CreateStrobingRendererTween(
            BaseLightColorBase evt,
            Color normalColor,
            Color strobeColor) =>
            new()
            {
                StartTimeAlpha = 0f,
                StartTimeColor = 0f,
                StartColor = normalColor,
                StartAlpha = evt.Brightness,
                StartStrobeFrequency = evt.Frequency,
                StartStrobeBrightness = evt.StrobeBrightness,
                StartStrobeColor = strobeColor,
                EndTimeAlpha = 1f,
                EndTimeColor = 1f,
                EndColor = normalColor,
                EndAlpha = evt.Brightness,
                EndStrobeFrequency = evt.Frequency,
                EndStrobeBrightness = evt.StrobeBrightness,
                EndStrobeColor = strobeColor,
                ColorLerpType = BasicEventColorLerpType.RGB
            };

        // Tests construct one deterministic cycle so 0.75 beats always selects the hard strobe-on phase.
        private static LightColorTween CreateTween(
            Color startColor,
            Color endColor,
            float startBrightness,
            float endBrightness,
            Color startStrobeColor = default,
            Color endStrobeColor = default,
            float startStrobeBrightness = 0f,
            float endStrobeBrightness = 0f)
        {
            return new LightColorTween
            {
                StartTimeAlpha = 0f,
                StartTimeColor = 0f,
                StartColor = startColor,
                StartAlpha = startBrightness,
                StartStrobeFrequency = startStrobeBrightness > 0f ? 1f : 0f,
                StartStrobeBrightness = startStrobeBrightness,
                StartStrobeColor = startStrobeColor,
                EndTimeAlpha = 1f,
                EndTimeColor = 1f,
                EndColor = endColor,
                EndAlpha = endBrightness,
                EndStrobeFrequency = endStrobeBrightness > 0f ? 1f : 0f,
                EndStrobeBrightness = endStrobeBrightness,
                EndStrobeColor = endStrobeColor,
                ColorLerpType = BasicEventColorLerpType.RGB
            };
        }

        private static void AssertColor(Color actual, Color expected) =>
            AssertColor(actual, expected.r, expected.g, expected.b, expected.a);

        // Component assertions prove HDR channels remain independent instead of hiding a mismatch behind Color equality tolerances.
        private static void AssertColor(Color actual, float red, float green, float blue, float alpha)
        {
            Assert.That(actual.r, Is.EqualTo(red).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(green).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(blue).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(alpha).Within(0.0001f));
        }
    }
}

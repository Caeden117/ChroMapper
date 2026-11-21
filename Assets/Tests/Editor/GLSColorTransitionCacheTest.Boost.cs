using System;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public partial class GLSColorTransitionCacheTest
    {
        // A held palette node must swap to the boost palette while a color boost is on and back afterward,
        // not keep the palette its start resolved under.
        [Test]
        public void HeldPaletteNodeRibbonSwapsAtBoostBoundaries()
        {
            var group = CreateGroup(1f, 0, 0);
            group["e"][0]["e"][0]["c"] = 0;
            var json = CreateDifficultyJson(group);
            json["colorBoostBeatmapEvents"] = CreateBoostEventsJson((3f, true), (5f, false));
            var map = LoadMap(json);
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var boostIndex = CreateBoostIndex(map);
            var appearance = CreateBoostPaletteAppearance();
            var ribbonObject = new GameObject("GLS held boost ribbon test");
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, boostIndex.IsBoostAt, 1,
                    boostEvents: boostIndex);
                var timeline = GLSEventCommon.GetColorTimeline(source, 1);
                foreach (var jsonTime in new[] { 1.5f, 3f, 4f, 5f, 6f })
                {
                    AssertRibbonSampleBoostSemantic(
                        renderer, controller, map, timeline, source, 0, 1, appearance,
                        boostIndex, jsonTime, "held red node");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ribbonObject);
                UnityEngine.Object.DestroyImmediate(appearance);
            }
        }

        // A transition crossing a boost boundary keeps its original progress but renders both endpoints
        // under the boost state at the sampled beat: no premature blending before the boundary and no
        // easing restart after it.
        [Test]
        public void EasedTransitionAcrossBoostBoundaryKeepsEndpointPairAtOriginalProgress()
        {
            var sourceGroup = CreateGroup(1f, 0, 0);
            sourceGroup["e"][0]["e"][0]["c"] = 0;
            var destinationGroup = CreateGroup(9f, 0, 1);
            var json = CreateDifficultyJson(sourceGroup, destinationGroup);
            json["colorBoostBeatmapEvents"] = CreateBoostEventsJson((5f, true));
            var map = LoadMap(json);
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            transition.Easing = (int)EaseType.InOutQuartic;
            var boostIndex = CreateBoostIndex(map);
            var appearance = CreateBoostPaletteAppearance();
            var ribbonObject = new GameObject("GLS eased boost ribbon test");
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, boostIndex.IsBoostAt, 1,
                    boostEvents: boostIndex);
                var timeline = GLSEventCommon.GetColorTimeline(source, 1);
                foreach (var jsonTime in new[] { 2f, 4.9f, 5f, 5.1f, 7f })
                {
                    AssertRibbonSampleBoostSemantic(
                        renderer, controller, map, timeline, source, 0, 1, appearance,
                        boostIndex, jsonTime, "InOutQuartic red-to-blue transition");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ribbonObject);
                UnityEngine.Object.DestroyImmediate(appearance);
            }
        }

        // Explicit custom and strobe colors bypass the palette, so a boost cannot change them; the
        // distributed endpoints and the active strobe must render identically on both sides of the
        // boundaries. This guards the fix from applying boost states to custom colors.
        [Test]
        public void CustomColorAndStrobeStayInvariantAcrossBoostBoundaries()
        {
            var group = CreateGroup(
                1f,
                0,
                0,
                boxCustomData: CreateColorDistributionCustomData(
                    new[] { "r,0.5,L,l" },
                    new[] { "b,0.25,L,l" }));
            var json = CreateDifficultyJson(group);
            json["colorBoostBeatmapEvents"] = CreateBoostEventsJson((2f, true), (4f, false));
            var map = LoadMap(json);
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            source.CustomColor = new Color(0.82f, 0.34f, 0.56f, 1f);
            source.Brightness = 1f;
            source.StrobeColor = new Color(0.12f, 0.74f, 0.92f, 1f);
            source.StrobeBrightness = 1f;
            source.Frequency = 2;
            source.StrobeFade = 0;
            const int lightCount = 2;
            var boostIndex = CreateBoostIndex(map);
            var appearance = CreateBoostPaletteAppearance();
            var ribbonObject = new GameObject("GLS custom-color boost ribbon test");
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, boostIndex.IsBoostAt, lightCount,
                    boostEvents: boostIndex);
                var timeline = GLSEventCommon.GetColorTimeline(source, lightCount);
                // Frequency 2 puts the 0.25-cycle offsets inside the normal half and the 0.75-cycle
                // offsets inside the strobe half of every sampled beat.
                var jsonTimes = new[] { 1.125f, 1.875f, 2.125f, 2.875f, 4.125f, 4.875f };
                for (var light = 0; light < lightCount; light++)
                {
                    foreach (var jsonTime in jsonTimes)
                    {
                        AssertRibbonSampleBoostSemantic(
                            renderer, controller, map, timeline, source, light, lightCount,
                            appearance, boostIndex, jsonTime, "custom-color strobe node");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ribbonObject);
                UnityEngine.Object.DestroyImmediate(appearance);
            }
        }

        // A palette-derived strobe and its channel distributions resolve from the same boost state as the
        // main color, so strobe phases inside a boost show the boosted strobe colors.
        [Test]
        public void PaletteStrobeAndDistributionsSwapAtBoostBoundary()
        {
            var group = CreateGroup(
                1f,
                0,
                0,
                boxCustomData: CreateColorDistributionCustomData(
                    new[] { "b,0.4,L,l" },
                    new[] { "s,0.6,L,l" }));
            group["e"][0]["e"][0]["c"] = 0;
            var json = CreateDifficultyJson(group);
            json["colorBoostBeatmapEvents"] = CreateBoostEventsJson((2f, true), (4f, false));
            var map = LoadMap(json);
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            source.StrobeBrightness = 1f;
            source.Frequency = 2;
            source.StrobeFade = 0;
            const int lightCount = 2;
            var boostIndex = CreateBoostIndex(map);
            var appearance = CreateBoostPaletteAppearance();
            var ribbonObject = new GameObject("GLS palette strobe boost ribbon test");
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, boostIndex.IsBoostAt, lightCount,
                    boostEvents: boostIndex);
                var timeline = GLSEventCommon.GetColorTimeline(source, lightCount);
                // Frequency 2 puts .25-cycle offsets in the normal phase and .75-cycle offsets in the
                // strobe phase of every sampled beat.
                var jsonTimes = new[] { 1.3f, 1.6f, 3.3f, 3.6f, 5.3f, 5.6f };
                for (var light = 0; light < lightCount; light++)
                {
                    foreach (var jsonTime in jsonTimes)
                    {
                        AssertRibbonSampleBoostSemantic(
                            renderer, controller, map, timeline, source, light, lightCount,
                            appearance, boostIndex, jsonTime, "palette strobe node");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ribbonObject);
                UnityEngine.Object.DestroyImmediate(appearance);
            }
        }

        // The strip-edge AA borrows packed neighbor records; under a boost the borrowed record must use
        // the same swapped palette as the owned strips it blends with.
        [Test]
        public void NeighborRecordAcrossStripEdgeSharesBoostState()
        {
            // Both boxes live in one group: ribbon neighbor records come from the owning group's
            // timeline, and only aggregate ribbons make another box's overlapping state visible.
            var sourceGroup = CreateGroup(1f, 0, 0, filterParam: 2, filterParam1: 0, filterChunks: 0);
            sourceGroup["e"][0]["e"][0]["c"] = 0;
            var neighborBox = CreateGroup(1f, 0, 0, filterParam: 2, filterParam1: 1, filterChunks: 0)["e"][0];
            neighborBox["e"][0]["b"] = 3;
            ((JSONArray)sourceGroup["e"]).Add(neighborBox);
            var json = CreateDifficultyJson(sourceGroup);
            json["colorBoostBeatmapEvents"] = CreateBoostEventsJson((5f, true));
            var map = LoadMap(json);
            const int lightCount = 2;
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var neighbor = map.LightColorEventBoxGroups[0].Boxes[1].Events[0];
            var boostIndex = CreateBoostIndex(map);
            var appearance = CreateBoostPaletteAppearance();
            var ribbonObject = new GameObject("GLS neighbor boost ribbon test");
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, boostIndex.IsBoostAt, lightCount,
                    aggregateSameTimeBoxes: true, boostEvents: boostIndex);
                var timeline = GLSEventCommon.GetColorTimeline(source, lightCount);
                Assert.That(
                    timeline.TryGetOutgoing(source, 0, out _),
                    Is.True,
                    "no owned state on the source light.");
                Assert.That(
                    timeline.TryGetOutgoing(neighbor, 1, out _),
                    Is.True,
                    "no neighbor state on the second light.");

                foreach (var (jsonTime, boost) in new[] { (4.5f, false), (6f, true) })
                {
                    var songBpmTime = (float)map.JsonTimeToSongBpmTime(jsonTime);
                    var progress = (songBpmTime - controller.ColorTimelineStart)
                        / controller.ColorTimelineDuration;
                    var boostLabel = boost ? "on" : "off";
                    var expectedColumn = RenderUniformPaletteColumn(
                        timeline, source, true, appearance, boost, songBpmTime, 5,
                        "neighbor strip edge");

                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    var material = CreateWaveSampleMaterial(properties);
                    try
                    {
                        var column = RenderGradientColumn(material, progress, 5);
                        var ownedPixel = column[4].gamma;
                        var expectedOwned = expectedColumn[4].gamma;
                        Assert.That(
                            ownedPixel.r,
                            Is.EqualTo(expectedOwned.r).Within(0.02f),
                            $"owned strip at JSON beat {jsonTime} with boost {boostLabel}: expected "
                            + $"{GLSRibbonTestDiagnostics.FormatColor(expectedOwned)}, rendered "
                            + $"{GLSRibbonTestDiagnostics.FormatColor(ownedPixel)}.");
                        var boundaryPixel = column[2].gamma;
                        var expectedBoundary = expectedColumn[2].gamma;
                        var message = $"strip edge at JSON beat {jsonTime} with boost {boostLabel}: expected "
                            + $"{GLSRibbonTestDiagnostics.FormatColor(expectedBoundary)}, rendered "
                            + $"{GLSRibbonTestDiagnostics.FormatColor(boundaryPixel)}.";
                        Assert.That(boundaryPixel.r, Is.EqualTo(expectedBoundary.r).Within(0.02f), message);
                        Assert.That(boundaryPixel.g, Is.EqualTo(expectedBoundary.g).Within(0.02f), message);
                        Assert.That(boundaryPixel.b, Is.EqualTo(expectedBoundary.b).Within(0.02f), message);
                        Assert.That(
                            column[0].gamma.r + column[0].gamma.g + column[0].gamma.b,
                            Is.EqualTo(0f).Within(0.004f),
                            $"inactive strip interior at JSON beat {jsonTime} must stay dark: "
                            + "neighbor colors only bleed in at the strip edge.");
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(material);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ribbonObject);
                UnityEngine.Object.DestroyImmediate(appearance);
            }
        }

        // Boundary positions follow JSON beats through the map's BPM changes; a boost stored in
        // raw JSON position lands mid-span at the wrong time once a BPM change shifts the conversion.
        [Test]
        public void BoostBoundaryFollowsSongBpmConversionAcrossBpmChanges()
        {
            var group = CreateGroup(1f, 0, 0);
            group["e"][0]["e"][0]["c"] = 0;
            var json = CreateDifficultyJson(group);
            var bpmEvents = new JSONArray();
            bpmEvents.Add(new JSONObject { ["b"] = 4, ["m"] = 25 });
            json["bpmEvents"] = bpmEvents;
            json["colorBoostBeatmapEvents"] = CreateBoostEventsJson((6f, true));
            var map = LoadMap(json);
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var boostIndex = CreateBoostIndex(map);
            var appearance = CreateBoostPaletteAppearance();
            var ribbonObject = new GameObject("GLS bpm boost boundary ribbon test");
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, boostIndex.IsBoostAt, 1,
                    boostEvents: boostIndex);
                var timeline = GLSEventCommon.GetColorTimeline(source, 1);
                foreach (var jsonTime in new[] { 5.8f, 6.2f, 7f })
                {
                    AssertRibbonSampleBoostSemantic(
                        renderer, controller, map, timeline, source, 0, 1, appearance,
                        boostIndex, jsonTime, "bpm-shifted boost boundary");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ribbonObject);
                UnityEngine.Object.DestroyImmediate(appearance);
            }
        }

        // The expected ribbon is rendered, not reimplemented: a reference upload whose boost resolver is
        // pinned to the sample's state resolves every endpoint under one palette without entering the
        // boundary shader branch, so easing, strobes, distributions, and strip-edge AA all come from the
        // production pipeline.
        private static Material CreateUniformPaletteMaterial(
            GLSColorTimeline timeline,
            BaseLightColorBase source,
            bool aggregateSameTimeBoxes,
            EventAppearanceSO appearance,
            bool boost,
            float songBpmTime,
            string context,
            out GLSColorTransitionPreview reference,
            out float progress)
        {
            reference = new GLSColorTransitionPreview();
            var properties = new MaterialPropertyBlock();
            var visible = reference.UpdateTimeline(
                timeline, source, false, appearance, _ => boost, properties,
                out var start, out var end, aggregateSameTimeBoxes);
            if (!visible)
            {
                reference.Dispose();
            }

            Assert.That(
                visible,
                Is.True,
                $"{context}: the uniform-palette reference produced no visible ribbon.");
            progress = (songBpmTime - start) / (end - start);
            return CreateWaveSampleMaterial(properties);
        }

        private static Color RenderUniformPalettePixel(
            GLSColorTimeline timeline,
            BaseLightColorBase source,
            bool aggregateSameTimeBoxes,
            EventAppearanceSO appearance,
            bool boost,
            float songBpmTime,
            float lane,
            string context)
        {
            var material = CreateUniformPaletteMaterial(
                timeline, source, aggregateSameTimeBoxes, appearance, boost, songBpmTime, context,
                out var reference, out var progress);
            try
            {
                return RenderGradientPixel(material, progress, lane).gamma;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                reference.Dispose();
            }
        }

        private static Color[] RenderUniformPaletteColumn(
            GLSColorTimeline timeline,
            BaseLightColorBase source,
            bool aggregateSameTimeBoxes,
            EventAppearanceSO appearance,
            bool boost,
            float songBpmTime,
            int height,
            string context)
        {
            var material = CreateUniformPaletteMaterial(
                timeline, source, aggregateSameTimeBoxes, appearance, boost, songBpmTime, context,
                out var reference, out var progress);
            try
            {
                return RenderGradientColumn(material, progress, height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                reference.Dispose();
            }
        }

        private static JSONArray CreateBoostEventsJson(params (float Beat, bool On)[] boosts)
        {
            var boostArray = new JSONArray();
            foreach (var (beat, on) in boosts)
            {
                boostArray.Add(new JSONObject
                {
                    ["b"] = beat,
                    ["o"] = on,
                });
            }

            return boostArray;
        }

        // Rebuild the index the event grid queries so ribbons resolve map boost events rather than
        // test-supplied timings.
        private static ColorBoostEventIndex CreateBoostIndex(BaseDifficulty map)
        {
            var boostIndex = new ColorBoostEventIndex();
            boostIndex.Load(map.Events);
            return boostIndex;
        }

        private static EventAppearanceSO CreateBoostPaletteAppearance()
        {
            var appearance = CreateWaveAppearance();
            appearance.RedColor = new Color(1f, 0f, 0f, 1f);
            appearance.RedBoostColor = new Color(1f, 0.55f, 0.15f, 1f);
            appearance.BlueColor = new Color(0f, 0f, 1f, 1f);
            appearance.BlueBoostColor = new Color(0.35f, 0.75f, 1f, 1f);
            return appearance;
        }

        // Render the uploaded ribbon at one beat and compare it against a uniform-palette
        // reference render at that beat's boost state: the game's HandleBoostChange swaps palettes on
        // the live endpoints instead of blending between them.
        private static void AssertRibbonSampleBoostSemantic(
            Renderer renderer,
            LightGradientController controller,
            BaseDifficulty map,
            GLSColorTimeline timeline,
            BaseLightColorBase source,
            int light,
            int lightCount,
            EventAppearanceSO appearance,
            ColorBoostEventIndex boostIndex,
            float jsonTime,
            string context)
        {
            Assert.That(timeline, Is.Not.Null, $"{context}: no color timeline for light {light}.");
            Assert.That(
                timeline.TryGetOutgoing(source, light, out _),
                Is.True,
                $"{context}: no outgoing ribbon state for light {light}.");
            Assert.That(
                controller.ColorTimelineDuration,
                Is.GreaterThan(0f),
                $"{context}: the ribbon uploaded no timeline duration.");

            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Assert.That(
                properties.GetFloat(Shader.PropertyToID("_UseLightTimeline")),
                Is.EqualTo(1f),
                $"{context}: the ribbon did not upload a per-light timeline.");

            var material = CreateWaveSampleMaterial(properties);
            try
            {
                var songBpmTime = (float)map.JsonTimeToSongBpmTime(jsonTime);
                var progress = (songBpmTime - controller.ColorTimelineStart)
                    / controller.ColorTimelineDuration;
                var lane = (lightCount - light - 0.5f) / lightCount;
                var pixel = RenderGradientPixel(material, progress, lane).gamma;

                var sampleBoost = boostIndex.IsBoostAt(jsonTime);
                var expectedPixel = RenderUniformPalettePixel(
                    timeline, source, controller.AggregatesSameTimeBoxes, appearance, sampleBoost,
                    songBpmTime, lane, context);
                var message = $"{context} at JSON beat {jsonTime} with boost "
                    + (sampleBoost ? "on" : "off")
                    + $": expected {GLSRibbonTestDiagnostics.FormatColor(expectedPixel)}, "
                    + $"rendered {GLSRibbonTestDiagnostics.FormatColor(pixel)}.";
                Assert.That(pixel.r, Is.EqualTo(expectedPixel.r).Within(0.02f), message);
                Assert.That(pixel.g, Is.EqualTo(expectedPixel.g).Within(0.02f), message);
                Assert.That(pixel.b, Is.EqualTo(expectedPixel.b).Within(0.02f), message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}

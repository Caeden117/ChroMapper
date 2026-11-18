using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Per-light ribbon strip parity on the wave fixture: each physical strip must render the same
    // color the production light shows at that beat, including distributed timing, independent easing
    // tracks, HSV endpoints, and instant-destination holds.
    public class GLSRibbonParityTest : GLSColorPlaybackTestBase
    {
        [Test]
        public void ExplicitLinearColorEasingOverridesCubicInPreviewAndRibbon()
        {
            LoadPlayback(@"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,0,0]}},
                    {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[1,1,1],""easing"":7,""colorEasing"":0}}]}]}]}");
            var expected = new Color(0.25f, 0.25f, 0.25f, 1f);
            AssertColor(ColorAt(0, 4.25f), expected, 0.00001f,
                "A zero colorEasing must override Cubic interpolation with Linear without changing brightness.");
            AssertRibbonPixels(0, 0, 0, 4.25f, -1, expectedPreviewAtLight: _ => expected);
        }

        // Use fixed expected colors from the map so a bug shared by the live tween and ribbon data cannot
        // make both pass.
        private static readonly Dictionary<float, Color[]> ReportedWaveExpectedPreview = new()
        {
            [44f] = new[]
            {
                new Color(0.122f, 0.196f, 0.799f, 4.719f),
                new Color(0.093f, 0.090f, 0.907f, 1.000f),
                new Color(0.169f, 0.280f, 0.883f, 4.721f),
                new Color(0.096f, 0.094f, 0.904f, 1.000f),
                new Color(0.035f, 0.403f, 0.810f, 4.723f),
                new Color(0.097f, 0.099f, 0.900f, 1.000f),
                new Color(0.302f, 0.257f, 0.714f, 4.725f),
                new Color(0.099f, 0.104f, 0.896f, 1.001f),
            },
            [45f] = new[]
            {
                new Color(0.137f, 0.002f, 0.990f, 1.032f),
                new Color(0.046f, 0.045f, 0.954f, 1.000f),
                new Color(0.001f, 0.134f, 1.088f, 1.032f),
                new Color(0.047f, 0.047f, 0.952f, 1.000f),
                new Color(0.056f, 0.327f, 0.858f, 1.032f),
                new Color(0.048f, 0.050f, 0.950f, 1.000f),
                new Color(0.419f, 0.098f, 0.857f, 1.032f),
                new Color(0.050f, 0.052f, 0.948f, 1.000f),
            },
            [45.5f] = new[]
            {
                new Color(0.069f, 0.000f, 0.996f, 1.000f),
                new Color(0.023f, 0.023f, 0.977f, 1.000f),
                new Color(0.000f, 0.067f, 1.045f, 1.000f),
                new Color(0.023f, 0.024f, 0.976f, 1.000f),
                new Color(0.028f, 0.164f, 0.929f, 1.000f),
                new Color(0.024f, 0.025f, 0.975f, 1.000f),
                new Color(0.212f, 0.049f, 0.929f, 1.000f),
                new Color(0.025f, 0.026f, 0.974f, 1.000f),
            },
        };

        // The first source fans out to B/C, and C/final B each keep only their four owned strips until D.
        [Test]
        public void RibbonPixelsMatchEachOwnedPreviewLight()
        {
            foreach (var (group, box, node, beat) in new[]
            {
                (0, 0, 0, 5f), (0, 0, 0, 20f), (1, 0, 0, 14.35f), (1, 0, 2, 33.1f),
                (1, 0, 3, 42.1f), (1, 1, 0, 23.35f), (1, 1, 0, 30.15f), (1, 1, 0, 45.999f)
            })
            {
                AssertRibbonPixels(group, box, node, beat, -1);
            }
        }

        // Probe odd strips directly so a wrong even-light mask cannot hide distribution/easing failures later in the loop.
        [Test]
        public void DistributedTimingAndIndependentEasingsMatchPreview()
        {
            foreach (var (group, box, light, beat) in new[]
            {
                (0, 0, 7, 23.6f), (1, 1, 1, 23.35f), (1, 1, 7, 24.2f), (1, 1, 3, 31.6f)
            })
            {
                AssertRibbonPixels(group, box, 0, beat, light);
            }
        }

        // Instant destinations ignore their stored transition-only metadata and still show the preceding held/strobing strip.
        [Test]
        public void InstantDestinationKeepsHeldRibbonWithoutApplyingStoredEasing()
        {
            Node(2).Easing = (int)Beatmap.Enums.EaseType.None;
            Node(2).CustomLerpType = BasicEventColorLerpType.TrueHSV;
            ColorAt(1, 40f);
            Assert.IsNull(containers[1].Tween.ColorEasing,
                "A no-transition node must not animate the preceding color using a retained custom colorEasing.");
            Assert.AreEqual(Beatmap.Shared.BasicEventColorLerpType.RGB, containers[1].Tween.ColorLerpType,
                "An instant destination must not apply its HSV conversion to held HDR colors.");
            AssertRibbonPixels(1, 1, 0, 40f, 1);
        }

        // Color-distribution-generated HDR values must take the identical HSV conversion path in preview and ribbon pixels.
        [Test]
        public void HdrHsvRibbonMatchesPreviewLight()
        {
            Node(2).CustomLerpType = BasicEventColorLerpType.TrueHSV;
            AssertRibbonPixels(1, 1, 0, 30.15f, 1);
        }

        [Test]
        public void HiddenGlsLightTransitionsHideBothRibbonDirections()
        {
            var outgoingObject = new GameObject("disabled outgoing GLS ribbon");
            var incomingObject = new GameObject("disabled incoming GLS ribbon");
            try
            {
                var outgoing = GLSColorTransitionCacheTest.CreateRibbonController(outgoingObject, out _);
                var incoming = GLSColorTransitionCacheTest.CreateRibbonController(incomingObject, out _);
                Settings.Instance.VisualizeGLSLightTransitions = false;

                GLSEventCommon.UpdateColorTransitionRibbon(
                    outgoing, Node(1, 0, 3), appearance, _ => false, LightCount);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    incoming, Node(1, 0, 0), appearance, _ => false, LightCount);

                Assert.That(outgoingObject.activeSelf, Is.False, "The disable setting must hide outgoing GLS ribbons.");
                Assert.That(incomingObject.activeSelf, Is.False, "The disable setting must hide incoming GLS ribbons.");
            }
            finally
            {
                Settings.Instance.VisualizeGLSLightTransitions = true;
                Object.DestroyImmediate(outgoingObject);
                Object.DestroyImmediate(incomingObject);
            }
        }

        [Test]
        public void EveryColorEasingMatchesRibbonOutput() =>
            AssertEveryEasingMatchesRibbonOutput(RibbonEasingTrack.Color);

        [Test]
        public void EveryStrobeColorEasingMatchesRibbonOutput() =>
            AssertEveryEasingMatchesRibbonOutput(RibbonEasingTrack.StrobeColor);

        [Test]
        public void EveryStrobeEasingMatchesRibbonOutput() =>
            AssertEveryEasingMatchesRibbonOutput(RibbonEasingTrack.Strobe);

        internal static void AssertStrobePulseWidthRibbonPixelsMatchIndependentWaveform(
            float pulseWidth, float phase, bool fade, float expectedAlpha)
        {
            var texture = new Texture2D(1, 9, TextureFormat.RGBAFloat, false, true);
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"));
            try
            {
                var tween = CreateEasingProbeTween(RibbonEasingTrack.Strobe, Easing.Cubic.In);
                tween.StartColor = tween.EndColor = Color.white;
                tween.StartStrobeColor = tween.EndStrobeColor = Color.white;
                tween.StartStrobeBrightness = tween.EndStrobeBrightness = 0f;
                tween.StrobeFade = fade;
                var rows = CreateEasingProbeRows(tween, RibbonEasingTrack.Strobe, Easing.EasingShaderId(7));
                rows[7].a += (pulseWidth - 0.5f) * 0.5f;
                rows[8].a += (pulseWidth - 0.5f) * 0.5f;
                texture.SetPixels(rows);
                texture.Apply(false, false);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 1f);
                material.SetTexture("_LightDistributionTex", texture);
                var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, phase, 0.5f).gamma;
                var expected = GLSColorTransitionCacheTest.CalculateExpectedRibbonPixel(new Color(1f, 1f, 1f, expectedAlpha));
                AssertColorRgb(pixel, expected, 0.02f, "Ribbon pixels must use the authored split and vertically flipped falling curve.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        internal static void AssertStrobePulseWidthTransitionRibbonUsesBothEndpoints(
            float from, float to, float progress, bool fade)
        {
            var texture = new Texture2D(1, 9, TextureFormat.RGBAFloat, false, true);
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"));
            try
            {
                var tween = CreateEasingProbeTween(RibbonEasingTrack.Strobe, Easing.Cubic.In);
                tween.StartColor = tween.EndColor = Color.white;
                tween.StartStrobeColor = tween.EndStrobeColor = Color.white;
                tween.StartStrobeBrightness = tween.EndStrobeBrightness = 0f;
                tween.StrobeFade = fade;
                var rows = CreateEasingProbeRows(tween, RibbonEasingTrack.Strobe, Easing.EasingShaderId(7));
                rows[7].a += (from - 0.5f) * 0.5f;
                rows[8].a += (to - 0.5f) * 0.5f;
                rows[8].r = Easing.EasingShaderId(7);
                texture.SetPixels(rows);
                texture.Apply(false, false);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 1f);
                material.SetTexture("_LightDistributionTex", texture);
                var pulseWidth = from + (to - from) * progress;
                var amount = progress < pulseWidth
                    ? Mathf.Pow(progress / pulseWidth, 3f)
                    : 1f - Mathf.Pow((progress - pulseWidth) / (1f - pulseWidth), 3f);
                var alpha = fade ? 1f - amount : progress >= pulseWidth ? 0f : 1f;
                var expected = GLSColorTransitionCacheTest.CalculateExpectedRibbonPixel(new Color(1f, 1f, 1f, alpha));
                var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, 0.5f).gamma;
                AssertColorRgb(pixel, expected, 0.02f, "Ribbon pulse width must interpolate independently of color easing.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        [TestCase(1)]
        [TestCase(64)]
        public void StrobePulseWidthMovingWithPhaseKeepsRibbonPixelsOnTheStrobeEndpoint(int width)
        {
            var texture = new Texture2D(1, 9, TextureFormat.RGBAFloat, false, true);
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"));
            try
            {
                var tween = CreateEasingProbeTween(RibbonEasingTrack.Strobe, Easing.Linear);
                tween.StartColor = tween.EndColor = Color.white;
                tween.StartStrobeColor = tween.EndStrobeColor = Color.white;
                tween.StartStrobeBrightness = tween.EndStrobeBrightness = 0f;
                tween.StrobeFade = false;
                var rows = CreateEasingProbeRows(tween, RibbonEasingTrack.Strobe, Easing.EasingShaderId(0));
                rows[7].a -= 0.25f;
                rows[8].a += 0.25f;
                texture.SetPixels(rows);
                texture.Apply(false, false);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 1f);
                material.SetTexture("_LightDistributionTex", texture);
                var pixels = GLSColorTransitionCacheTest.RenderGradientRow(material, 0.5f, width);
                var margin = width > 8 ? 4 : 0;
                for (var x = margin; x < width - margin; x++)
                    AssertColorRgb(pixels[x], Color.black, 0.0001f,
                        $"Pixel {x}: phase and pulse width advance together, so the full pixel stays on the strobe endpoint.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        // Near the blue destination, ribbons must match the lasers' washed-out blue. Even lights transition
        // from beat 39 to 46; odd lights transition from beat 24 to 46.
        [Test]
        public void WashedOutColorDistributionRibbonMatchesPreviewLight()
        {
            LoadPlayback(ReportedWaveMapJson);
            foreach (var (group, box, node, beat) in new[]
            {
                (0, 0, 3, 44f), (0, 0, 3, 45f), (0, 0, 3, 45.5f),
                (0, 1, 1, 44f), (0, 1, 1, 45f), (0, 1, 1, 45.5f)
            })
            {
                // The second box owns odd lights and uses equal-brightness RGB endpoints, so those strips must
                // retain their peak intensity.
                System.Func<int, bool> expectsPeakCompensation = box == 1
                    ? light => light % 2 == 1
                    : _ => false;
                var expectedPreview = ReportedWaveExpectedPreview[beat];
                System.Func<int, bool> expectedOwnership = box == 0
                    ? light => light % 2 == 0
                    : light => light % 2 == 1;
                AssertRibbonPixels(
                    group,
                    box,
                    node,
                    beat,
                    -1,
                    expectsPeakCompensation,
                    light => expectedPreview[light],
                    expectedOwnership);
            }
        }

        // At frequency 20, integer and half-beat samples always land on phase zero. Sample the full cycle to
        // expose differences between the ribbon and light strobe clocks.
        [Test]
        public void StripMatchesLightAcrossTheWholeStrobePhaseCycle()
        {
            LoadPlayback(ReportedWaveMapJson);
            var ribbonObject = new GameObject("strip");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    ribbon, Node(0, 0, 3), appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                var material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                try
                {
                    for (var step = 0; step <= 20; step++)
                    {
                        var beat = 44f + (step * 0.025f);
                        for (var light = 0; light < LightCount; light += 2)
                        {
                            var progress = (SongTime(beat) - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                            var lane = (LightCount - light - 0.5f) / LightCount;
                            var strip = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
                            var live = ColorAt(light, beat);
                            var laser = GLSColorTransitionCacheTest.CalculateExpectedRibbonPixel(live);
                            AssertColorRgb(strip.gamma, laser, 0.03f,
                                $"beat={beat} light={light} strip must equal the light at every strobe phase");
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(material);
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // The exhaustive easing regressions compare the real ribbon shader with LightColorTween, the same runtime evaluator used by preview lights.
        private static void AssertEveryEasingMatchesRibbonOutput(RibbonEasingTrack track)
        {
            var texture = new Texture2D(1, 9, TextureFormat.RGBAFloat, false, true);
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient")) { enableInstancing = false };
            try
            {
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 1f);
                material.SetTexture("_LightDistributionTex", texture);
                foreach (var easingEntry in Easing.IDToInternalName.OrderBy(entry => entry.Key))
                {
                    var easing = Easing.FromID(easingEntry.Key);
                    var shaderId = Easing.EasingShaderId(easingEntry.Key);
                    var samples = track == RibbonEasingTrack.Strobe ? new[] { 0.2f, 0.7f } : new[] { 0.37f };
                    var tween = CreateEasingProbeTween(track, easing);
                    texture.SetPixels(CreateEasingProbeRows(tween, track, shaderId));
                    texture.Apply(false, false);
                    foreach (var progress in samples)
                    {
                        tween.UpdateTime(progress);
                        var ribbonPixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, 0.5f).gamma;
                        var lightPixel = GLSColorTransitionCacheTest.CalculateExpectedRibbonPixel(tween.Color);
                        AssertColorRgb(
                            ribbonPixel,
                            lightPixel,
                            0.02f,
                            $"{track} easing {easingEntry.Value} ({easingEntry.Key}) at {progress} must produce the preview light's color.");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        // The probe holds every unrelated track linear so each regression fails only for the selected ribbon easing channel.
        private static LightColorTween CreateEasingProbeTween(
            RibbonEasingTrack track,
            System.Func<float, float> easing)
        {
            // A normal-color probe disables strobing, a strobe-color probe lands in its hard on-phase, and a fade probe uses one cycle for an uncluttered pulse sample.
            var strobeFrequency = 1f;
            if (track == RibbonEasingTrack.Color)
            {
                strobeFrequency = 0f;
            }
            else if (track == RibbonEasingTrack.StrobeColor)
            {
                strobeFrequency = 2f;
            }

            var tween = new LightColorTween
            {
                StartTimeAlpha = 0f,
                StartTimeColor = 0f,
                EndTimeAlpha = 1f,
                EndTimeColor = 1f,
                StartColor = new Color(0.12f, 0.38f, 0.77f, 1f),
                EndColor = new Color(0.91f, 0.22f, 0.08f, 1f),
                StartAlpha = 1f,
                EndAlpha = 1f,
                StartStrobeColor = new Color(0.84f, 0.05f, 0.61f, 1f),
                EndStrobeColor = new Color(0.03f, 0.92f, 0.31f, 1f),
                StartStrobeBrightness = 1f,
                EndStrobeBrightness = 1f,
                StartStrobeFrequency = strobeFrequency,
                EndStrobeFrequency = strobeFrequency,
                StrobeFade = track == RibbonEasingTrack.Strobe,
                ColorLerpType = BasicEventColorLerpType.RGB,
                Easing = Easing.Linear,
                ColorEasing = track == RibbonEasingTrack.Color ? easing : Easing.Linear,
                StrobeColorEasing = track == RibbonEasingTrack.StrobeColor ? easing : Easing.Linear,
                StrobeEasing = track == RibbonEasingTrack.Strobe ? easing : Easing.Cubic.InOut,
                ComposeAlphaAtColorEndpoints = true
            };
            return tween;
        }

        // Match the nine-row layout uploaded by GLSColorTransitionPreview.UpdateTimeline.
        private static Color[] CreateEasingProbeRows(
            LightColorTween tween,
            RibbonEasingTrack track,
            int shaderId)
        {
            var linearShaderId = Easing.EasingShaderId((int)Beatmap.Enums.EaseType.Linear);
            var rows = new Color[9];
            rows[0] = tween.StartColor;
            rows[1] = tween.EndColor;
            rows[2] = tween.StartStrobeColor;
            rows[3] = tween.EndStrobeColor;
            rows[4] = new Color(tween.StartTimeAlpha, tween.EndTimeAlpha, tween.StartTimeColor, tween.EndTimeColor);
            rows[5] = new Color(tween.StartStrobeFrequency, tween.EndStrobeFrequency, tween.StartAlpha, tween.EndAlpha);
            rows[6] = new Color(tween.StartStrobeBrightness, tween.EndStrobeBrightness, tween.StartAlpha, tween.EndAlpha);
            rows[7] = new Color(tween.StartStrobeColor.a, tween.EndStrobeColor.a, 0f, tween.StrobeFade ? 3f : 2f);
            rows[8] = new Color(
                linearShaderId,
                track == RibbonEasingTrack.Color ? shaderId : linearShaderId,
                track == RibbonEasingTrack.StrobeColor ? shaderId : linearShaderId,
                track == RibbonEasingTrack.Strobe ? shaderId : Easing.EasingShaderId("easeInOutCubic"));
            return rows;
        }

        // Named tracks keep the three independent ribbon channels explicit in failure output.
        private enum RibbonEasingTrack
        {
            Color,
            StrobeColor,
            Strobe
        }

        // Spawn the actual outer-lane prefabs to catch overlap between primary and ghost ribbons in both
        // directions. Each physical light should have one lit strip at the sampled beat.
        [Test]
        public void RealOuterLaneRibbonsDoNotStackPerLight()
        {
            LoadPlayback(ReportedWaveMapJson);
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            Assert.IsNotNull(collection, "The playmode scene must contain the outer GLS group collection.");
            var containers = new List<GLSGroupContainer>();
            var ribbons = new List<(string label, LightGradientController ribbon)>();
            try
            {
                foreach (var group in map.LightColorEventBoxGroups)
                {
                    var container = (GLSGroupContainer)collection.CreateContainer();
                    containers.Add(container);
                    container.ObjectData = group;
                    container.GlsLightCount = LightCount;
                    container.ConfigurePreviewNodes(_ => false);
                }

                var ghostField = typeof(GLSGroupContainer).GetField(
                    "previewGhosts", BindingFlags.Instance | BindingFlags.NonPublic);
                var bodies = new List<(string label, GLSGroupContainer container)>();
                foreach (var container in containers)
                {
                    bodies.Add(($"body g{container.EventBoxGroupData.JsonTime}", container));
                    foreach (var ghost in (List<GLSGroupContainer>)ghostField.GetValue(container))
                    {
                        bodies.Add(($"ghost g{container.EventBoxGroupData.JsonTime}@{ghost.PreviewEventData?.JsonTime}", ghost));
                    }
                }

                foreach (var (label, container) in bodies)
                {
                    foreach (var ribbon in container.GetComponentsInChildren<LightGradientController>(true))
                    {
                        var direction = ribbon.IsIncomingColorTransition ? "in" : "out";
                        ribbons.Add((
                            $"{label} {direction} span=[{ribbon.ColorTimelineStart},{ribbon.ColorTimelineStart + ribbon.ColorTimelineDuration}]",
                            ribbon));
                    }
                }

                var sb = new StringBuilder();
                const float beat = 44f;
                var time = SongTime(beat);
                var coverage = new List<(string label, LightGradientController ribbon)>[LightCount];
                for (var light = 0; light < LightCount; light++)
                {
                    coverage[light] = new List<(string label, LightGradientController ribbon)>();
                }

                foreach (var (label, ribbon) in ribbons)
                {
                    sb.Append($"{label}: active={ribbon.gameObject.activeSelf}");
                    if (!ribbon.gameObject.activeSelf)
                    {
                        sb.AppendLine();
                        continue;
                    }

                    var renderer = (MeshRenderer)typeof(LightGradientController)
                        .GetField("meshRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    var material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                    try
                    {
                        var progress = (time - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                        for (var light = 0; light < LightCount; light++)
                        {
                            var lane = (LightCount - light - 0.5f) / LightCount;
                            var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
                            if (pixel.r + pixel.g + pixel.b > 0.02f)
                            {
                                coverage[light].Add((label, ribbon));
                            }
                            sb.Append($" L{light}({pixel.r:0.00},{pixel.g:0.00},{pixel.b:0.00})");
                        }
                        sb.AppendLine();
                    }
                    finally
                    {
                        Object.DestroyImmediate(material);
                    }
                }

                for (var light = 0; light < LightCount; light++)
                {
                    sb.AppendLine($"light={light} lit-by=[{string.Join("; ", coverage[light].Select(c => c.label))}]");
                    Assert.That(coverage[light].Count, Is.LessThanOrEqualTo(1),
                        $"beat={beat} light={light}: {coverage[light].Count} outer ribbons render the same light");
                }
                Debug.Log(sb.ToString());
            }
            finally
            {
                foreach (var container in containers)
                {
                    Object.DestroyImmediate(container.gameObject);
                }
            }
        }

        // On map exit, the track and its ghosts are destroyed together. Do not return those dying ghosts to
        // the runtime pool.
        [Test]
        public void DestroyingOuterTrackWithPreviewGhostsDoesNotReparentDyingObjects()
        {
            LoadPlayback(ReportedWaveMapJson);
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            Assert.IsNotNull(collection, "The playmode scene must contain the outer GLS group collection.");
            var trackObject = new GameObject("GLS teardown regression track");
            try
            {
                var container = (GLSGroupContainer)collection.CreateContainer();
                container.transform.SetParent(trackObject.transform, false);
                container.ObjectData = map.LightColorEventBoxGroups[0];
                container.GlsLightCount = LightCount;
                container.ConfigurePreviewNodes(_ => false);

                Object.DestroyImmediate(trackObject);
                trackObject = null;
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (trackObject != null)
                    Object.DestroyImmediate(trackObject);
            }
        }

    }
}

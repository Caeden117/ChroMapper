using System;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Checks strobe timing across transitions, easing, alternating chunks, held tails, and BPM changes.
    public class GLSStrobePhaseTest : GLSColorPlaybackTestBase
    {
        [TestCase(0.9f, 0.75f, false, 1f)]
        [TestCase(0.9f, 0.95f, false, 0f)]
        [TestCase(0f, 0.25f, false, 0f)]
        [TestCase(1f, 0.75f, false, 1f)]
        [TestCase(-1f, 0.25f, false, 0f)]
        [TestCase(2f, 0.75f, false, 1f)]
        [TestCase(0.9f, 0.45f, true, 0.875f)]
        [TestCase(0.9f, 0.95f, true, 0.125f)]
        [TestCase(0.5f, 0.75f, true, 0.125f)]
        [TestCase(0f, 0.25f, true, 0.015625f)]
        [TestCase(1f, 0.25f, true, 0.984375f)]
        public void StrobePulseWidthControlsPreviewAndRibbon(float pulseWidth, float phase, bool fade, float expectedAlpha)
        {
            var json = JSON.Parse(BpmStrobeTailJson);
            var evt = json["lightColorEventBoxGroups"][0]["e"][0]["e"][0];
            evt["sb"] = 0;
            evt["sf"] = fade ? 1 : 0;
            evt["customData"] = new JSONObject { ["strobePulseWidth"] = pulseWidth };
            if (fade)
                evt["customData"]["strobeEasing"] = 7;
            LoadPlayback(json.ToString());
            appearance.RedColor = Color.white;
            playback.ColorSchemeProvider.ColorScheme.EnvironmentLeftColor = Color.white;
            playback.Refresh();
            var beat = 4f + phase;
            var color = ColorAt(0, beat);
            Assert.That(color.a, Is.EqualTo(expectedAlpha).Within(0.0001f),
                "The pulse split and vertically flipped falling easing must control preview brightness.");
            AssertRibbonPixels(0, 0, 0, beat, 0);
            // The independent texture probe takes normalized widths instead of raw custom-data values.
            if (pulseWidth >= 0f && pulseWidth <= 1f)
            {
                GLSRibbonParityTest.AssertStrobePulseWidthRibbonPixelsMatchIndependentWaveform(
                    pulseWidth, phase, fade, expectedAlpha);
            }
        }

        [Test]
        public void MissingStrobePulseWidthAlsoUsesVerticallyFlippedFallingEasing()
        {
            var json = JSON.Parse(BpmStrobeTailJson);
            var evt = json["lightColorEventBoxGroups"][0]["e"][0]["e"][0];
            evt["sb"] = 0;
            evt["sf"] = 1;
            evt["customData"] = new JSONObject { ["strobeEasing"] = 7 };
            LoadPlayback(json.ToString());
            appearance.RedColor = Color.white;
            playback.ColorSchemeProvider.ColorScheme.EnvironmentLeftColor = Color.white;
            playback.Refresh();
            Assert.That(ColorAt(0, 4.75f).a, Is.EqualTo(0.125f).Within(0.0001f));
            AssertRibbonPixels(0, 0, 0, 4.75f, 0);
        }

        [TestCase(0.1f, 0.9f, 0.25f, 1, false)]
        [TestCase(0.9f, 0.1f, 0.75f, 1, false)]
        [TestCase(0.1f, 0.9f, 0.25f, 1, true)]
        [TestCase(0.9f, 0.1f, 0.75f, 1, true)]
        [TestCase(0.5f, 0.9f, 0.75f, 1, false)]
        [TestCase(0.9f, 0.5f, 0.75f, 1, false)]
        [TestCase(0f, 1f, 0.25f, 1, true)]
        [TestCase(1f, 0f, 0.75f, 1, true)]
        [TestCase(0f, 1f, 0.25f, 1, false)]
        [TestCase(0.1f, 0.9f, 0.25f, 0, false)]
        [TestCase(0.9f, 0.1f, 0.75f, 0, true)]
        [TestCase(0.1f, 0.9f, 0.25f, 2, false)]
        [TestCase(0.9f, 0.1f, 0.75f, 2, true)]
        public void StrobePulseWidthTransitionsMatchPreviewAndRibbon(float from, float to, float progress, int transition, bool fade)
        {
            var json = JSON.Parse(BpmStrobeTailJson);
            json.Remove("bpmEvents");
            var events = json["lightColorEventBoxGroups"][0]["e"][0]["e"];
            var source = events[0];
            source["sb"] = 0;
            source["sf"] = fade ? 1 : 0;
            source["customData"] = new JSONObject();
            if (from != 0.5f)
                source["customData"]["strobePulseWidth"] = from;
            if (fade)
                source["customData"]["strobeEasing"] = 7;
            var next = source.Clone();
            next["b"] = 1;
            next["i"] = transition;
            next["customData"] = new JSONObject();
            if (to != 0.5f)
                next["customData"]["strobePulseWidth"] = to;
            if (transition == 1)
                next["customData"]["easing"] = 7;
            events.Add(next);
            LoadPlayback(json.ToString());
            appearance.RedColor = Color.white;
            playback.ColorSchemeProvider.ColorScheme.EnvironmentLeftColor = Color.white;
            playback.Refresh();
            var pulseWidth = transition == 1 ? from + (to - from) * progress : from;
            var amount = progress < pulseWidth
                ? Mathf.Pow(progress / pulseWidth, 3f)
                : 1f - Mathf.Pow((progress - pulseWidth) / (1f - pulseWidth), 3f);
            var expectedAlpha = fade ? 1f - amount : progress >= pulseWidth ? 0f : 1f;
            var beat = 4f + progress;
            Assert.That(ColorAt(0, beat).a, Is.EqualTo(expectedAlpha).Within(0.0001f),
                "Pulse width must use the linear strobe clock, transition to the next node, and hold on Instant or Extend.");
            AssertRibbonPixels(0, 0, 0, beat, 0);
            // This texture probe isolates linear width interpolation without the model or timeline builder.
            if (transition == 1)
            {
                GLSRibbonParityTest.AssertStrobePulseWidthTransitionRibbonUsesBothEndpoints(
                    from, to, progress, fade);
            }
        }

        // Exact beat-88/93 report: the destination starts an sf=1 strobe with a black (sb=0) alternate phase.
        private const string BlackStrobeStartMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":88,""g"":1,""e"":[{""f"":{""f"":1,""p"":1,""t"":0,""r"":0,""c"":0,""n"":0,""s"":0,""l"":0,""d"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":0.5,""i"":1,""f"":0,""sb"":0,""sf"":1,""customData"":{""color"":[1,0.4,0.913],""colorDistributions"":[""h,-0.2,L,l""],""colorEasing"":30}},
                {""b"":5,""c"":0,""s"":0.5,""i"":1,""f"":1,""sb"":0,""sf"":1,""customData"":{""color"":[0.678,0.4,1],""colorDistributions"":[""h,0.1,L,l""],""strobeColorDistributions"":[""s,-0.2,iobk""],""strobeColor"":[0.969,0.384,0.71],""colorEasing"":22}}]}]}]}";

        // The map starts at 100 BPM, but a beat-0 event changes playback to 240 BPM. SongBpmTime still counts
        // base-BPM beats, so one authored strobe cycle spans 100/240 SongBpmTime beats. The 60-second clip
        // still ends at SongBpmTime 100.
        private const string BpmStrobeTailJson = @"{""version"":""3.3.0"",""bpmEvents"":[{""b"":0,""m"":240}],""lightColorEventBoxGroups"":[
            {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0],""strobeColor"":[0,0,1]}}]}]}]}";

        // A same-lane instant successor three authored beats later: the held 1/1 strobe must reach
        // exactly three cycles so the fourth cycle starts on the frame the yellow node turns on.
        private const string BpmStrobeHoldJson = @"{""version"":""3.3.0"",""bpmEvents"":[{""b"":0,""m"":240}],""lightColorEventBoxGroups"":[
            {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0],""strobeColor"":[0,0,1]}}]}]},
            {""b"":7,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[1,1,0]}}]}]}]}";

        // The same held strobe stretched across a distant successor exercises phase parity far into
        // a transition (196 authored beats, ~81.7 SongBpmTime beats).
        private const string BpmStrobeFarJson = @"{""version"":""3.3.0"",""bpmEvents"":[{""b"":0,""m"":240}],""lightColorEventBoxGroups"":[
            {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0],""strobeColor"":[0,0,1]}}]}]},
            {""b"":200,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[1,1,0]}}]}]}]}";

        // Even lights reach the filtered successor at beat 7 while odd lights hold the strobe to the
        // song end, so one source must carry two different strip bounds and cycle counts.
        private const string BpmStrobeFilteredJson = @"{""version"":""3.3.0"",""bpmEvents"":[{""b"":0,""m"":240}],""lightColorEventBoxGroups"":[
            {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0],""strobeColor"":[0,0,1]}}]}]},
            {""b"":7,""g"":1,""e"":[{""f"":{""f"":2,""p"":0,""t"":2,""c"":8},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[1,1,0]}}]}]}]}";

        // A 0->1 strobe ramp over five beats accumulates 2.5 cycles; it must not finish black and reset bright at beat 93.
        [Test]
        public void StartingBlackStrobeDoesNotSnapBrightAtBeat93()
        {
            LoadPlayback(BlackStrobeStartMapJson);
            foreach (var light in new[] { 0, 3, 7 })
            {
                var before = ColorAt(light, 92.9999f);
                var at = ColorAt(light, 93f);
                var after = ColorAt(light, 93.0001f);
                Debug.Log($"[BlackStrobeStart] light={light} before={before} at={at} after={after}");
                AssertColor(before, at, 0.002f,
                    "The beat-93 strobe must not snap from its black alternate phase to full primary brightness.");
                AssertColor(at, after, 0.002f, "The newly active strobe must continue smoothly after its boundary.");
                Assert.That(at.a, Is.EqualTo(0.5f).Within(0.002f), "The node's native phase-zero primary brightness must remain intact.");
            }
        }

        // The same authored color distributions, Back easing and phase anchor must reach every ribbon strip, not just playback.
        [Test]
        public void StartingBlackStrobeRibbonMatchesPreview()
        {
            LoadPlayback(BlackStrobeStartMapJson);
            foreach (var beat in new[] { 89.25f, 90f, 90.5f, 91f, 91.5f, 92f, 92.999f })
            {
                AssertRibbonPixels(0, 0, 0, beat, -1, _ => true);
            }
        }

        // The inner lanes are the control: each keeps its own four lights and their complementary pulse phases.
        [Test]
        public void InnerAlternatingChunkRibbonsMatchPlayback()
        {
            LoadAlternatingChunks();
            foreach (var beat in new[] { 100.125f, 100.625f })
            {
                AssertRibbonPixels(0, 0, 0, beat, -1);
                AssertRibbonPixels(0, 1, 0, beat, -1);
            }
        }

        // The actual outer preview deduplicates the two timestamps, but its single ribbon must retain both boxes' owned strips.
        [TestCase(100.125f)]
        [TestCase(100.625f)]
        public void OuterAlternatingChunkRibbonsIncludeBothBoxes(float beat)
        {
            LoadAlternatingChunks();
            var root = new GameObject("Alternating chunk outer preview test");
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            var owner = (GLSGroupContainer)collection.CreateContainer();
            owner.transform.SetParent(root.transform, false);
            owner.ObjectData = map.LightColorEventBoxGroups[0];
            owner.Setup();
            owner.GlsLightCount = LightCount;
            var outerAppearance = (GLSGroupAppearanceSO)typeof(GLSGroupContainer)
                .GetField("glsGroupAppearance", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
            var appearanceField = typeof(GLSGroupAppearanceSO).GetField("eventAppearance", BindingFlags.Instance | BindingFlags.NonPublic);
            var originalAppearance = appearanceField.GetValue(outerAppearance);
            var originalOpacity = Settings.Instance.GLSOuterTrackGhostNodeOpacity;
            Material material = null;
            try
            {
                appearanceField.SetValue(outerAppearance, appearance);
                Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
                owner.ConfigurePreviewNodes(_ => false);
                foreach (var rebindWithoutSecondBox in new[] { false, true })
                {
                    // Reusing the same outer renderer must clear rows formerly owned by a removed sibling box.
                    if (rebindWithoutSecondBox)
                    {
                        var json = JSON.Parse(AlternatingChunksMapJson);
                        json["lightColorEventBoxGroups"][0]["e"].Remove(1);
                        LoadPlayback(json.ToString());
                        // Resolve GLS palette colors through the environment's ColorSchemeProvider.
                        playback.ColorSchemeProvider.ColorScheme.EnvironmentLeftColor = Color.white;
                        // The load-time Refresh bakes scheme colors into each tween; re-resolve them after overriding the environment color.
                        playback.Refresh();
                        owner.ObjectData = map.LightColorEventBoxGroups[0];
                        owner.ConfigurePreviewNodes(_ => false);
                    }
                    var ghosts = (System.Collections.Generic.List<GLSGroupContainer>)typeof(GLSGroupContainer)
                        .GetField("previewGhosts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
                    Assert.AreEqual(1, ghosts.Count, "Keep one selectable preview per timestamp; do not overlay duplicate node bodies.");
                    var ribbon = owner.lightGradientController;
                    Assert.IsTrue(ribbon.gameObject.activeSelf);
                    var renderer = (MeshRenderer)typeof(LightGradientController)
                        .GetField("meshRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                    var progress = (SongTime(beat) - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                    var secondHalf = beat - Mathf.Floor(beat) >= 0.5f;
                    for (var light = 0; light < LightCount; light++)
                    {
                        var firstBox = (light / 2) % 2 == 0;
                        var expectedOn = firstBox == secondHalf && (!rebindWithoutSecondBox || firstBox);
                        Assert.That(ColorAt(light, beat).a, Is.EqualTo(expectedOn ? 1f : 0f).Within(0.0001f),
                            $"Playback control: light {light} at {beat} must belong to the expected alternating chunk.");
                        var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress,
                            (LightCount - light - 0.5f) / LightCount);
                        if (expectedOn)
                            Assert.Greater(pixel.maxColorComponent, 0.05f, $"Outer preview omitted lit light {light} at {beat}.");
                        else
                            Assert.Less(pixel.maxColorComponent, 0.02f, $"Outer preview lit the off-phase light {light} at {beat}.");
                    }
                    Object.DestroyImmediate(material);
                    material = null;
                }
            }
            finally
            {
                appearanceField.SetValue(outerAppearance, originalAppearance);
                Settings.Instance.GLSOuterTrackGhostNodeOpacity = originalOpacity;
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(owner.gameObject);
                Object.DestroyImmediate(root);
            }
        }

        // B's no-strobe transition and both final D paths must converge to primary RGBA regardless of the residual phase.
        [Test]
        public void NoStrobeTransitionConvergesBeforeItsBoundary()
        {
            foreach (var (light, targetBeat) in new[] { (0, 17f), (0, 46f), (1, 46f), (3, 46f), (6, 46f), (7, 46f) })
            {
                var before = ColorAt(light, targetBeat - 0.0001f);
                var after = ColorAt(light, targetBeat);
                Debug.Log($"[GLSStrobe] light={light} target={targetBeat} before={before} after={after}");
                AssertColor(before, after, 0.002f, "A transition must not snap from residual strobe color to primary at its boundary.");
            }
        }

        // strobeEasing is node-scoped in ChromaGLS, so C's outgoing pulse must not silently adopt D's default cubic fade.
        [Test]
        public void COutgoingPulseUsesItsAuthoredStrobeEasing()
        {
            ColorAt(1, 30f);
            Assert.NotNull(containers[1].Tween.StrobeEasing,
                "C's authored strobeEasing must remain active until D instead of disappearing on the outgoing interval.");
            Assert.That(containers[1].Tween.StrobeEasing(0.3f), Is.EqualTo(Easing.FromID(19)(0.3f)).Within(0.00001f));
        }

        [Test]
        public void ExtensionsInheritStrobeEasingUntilTheNextNormalNode(
            [Values(EaseType.Linear, EaseType.InQuadratic, EaseType.InOutCubic)] EaseType easing,
            [Values] bool separateGroups)
        {
            LoadPlayback(BuildStrobeExtensionMap(easing, separateGroups));
            var timeline = new GLSColorTimeline(map, LightCount, map.LightColorEventBoxGroups);
            var source = Node(0);
            var expectedOverride = easing == EaseType.InOutCubic ? (int?)null : (int)easing;
            Assert.That(source.ChromaStrobeEasing, Is.EqualTo(expectedOverride));

            for (var nodeIndex = 0; nodeIndex < 5; nodeIndex++)
            {
                var node = separateGroups ? Node(nodeIndex) : Node(0, 0, nodeIndex);
                var extension = nodeIndex == 1 || nodeIndex == 2 || nodeIndex == 4;
                Assert.That(node.UsePrevious, Is.EqualTo(extension ? 1 : 0));
                if (nodeIndex > 0)
                {
                    Assert.That(node.ChromaStrobeEasing, Is.Null);
                    Assert.That(node.CustomData.HasKey("strobeEasing"), Is.False,
                        "Only the first normal node owns the authored strobeEasing metadata.");
                }

                var expectedEasing = nodeIndex < 3 ? Easing.FromID((int)easing) : Easing.Cubic.InOut;
                for (var light = 0; light < LightCount; light++)
                {
                    Assert.That(timeline.TryGetOutgoing(node, light, out var state), Is.True);
                    var ribbonTween = new LightColorTween();
                    timeline.ConfigureTween(ribbonTween, state, appearance, _ => false);

                    // Probe both pulse halves away from their endpoints, where Linear, Quadratic, and native Cubic differ.
                    foreach (var offset in new[] { 0.15f, 1.65f })
                    {
                        var beat = 4f + (2f * nodeIndex) + offset;
                        var phase = offset - Mathf.Floor(offset);
                        var fade = phase < 0.5f
                            ? expectedEasing(phase * 2f)
                            : 1f - expectedEasing((phase - 0.5f) * 2f);
                        var expected = Color.LerpUnclamped(Color.red, new Color(0f, 0f, 1f, 0.25f), fade);
                        Assert.That(StateAt(light, beat).Base, Is.SameAs(node));
                        AssertColor(ColorAt(light, beat), expected, 0.001f,
                            $"light={light} beat={beat}: extensions must keep the resolved source's pulse fade.");
                        Assert.That(containers[light].Tween.StrobeFade, Is.True);
                        Assert.That(containers[light].Tween.EasingShaderIds.w,
                            Is.EqualTo(Easing.EasingShaderId(nodeIndex < 3 ? (int)easing : (int)EaseType.InOutCubic)),
                            "Playback must retain the inherited fade curve for shader evaluation too.");
                        ribbonTween.UpdateTime(SongTime(beat));
                        AssertColor(ribbonTween.Color, expected, 0.001f,
                            $"light={light} beat={beat}: the ribbon timeline must inherit the same strobe fade.");
                        Assert.That(ribbonTween.EasingShaderIds.w,
                            Is.EqualTo(containers[light].Tween.EasingShaderIds.w));
                    }
                }
            }
        }

        private static string BuildStrobeExtensionMap(EaseType easing, bool separateGroups)
        {
            var json = JSON.Parse(@"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""e"":[
                    {""b"":0,""i"":1,""c"":0,""s"":1,""f"":1,""sb"":0.25,""sf"":1,""customData"":{""color"":[1,0,0],""strobeColor"":[0,0,1]}},
                    {""b"":2,""i"":2},
                    {""b"":4,""i"":2},
                    {""b"":6,""i"":1,""c"":0,""s"":1,""f"":1,""sb"":0.25,""sf"":1,""customData"":{""color"":[1,0,0],""strobeColor"":[0,0,1]}},
                    {""b"":8,""i"":2}]}]}]}");
            var group = json["lightColorEventBoxGroups"][0];
            var events = group["e"][0]["e"];
            if (easing != EaseType.InOutCubic)
            {
                events[0]["customData"]["strobeEasing"] = (int)easing;
            }

            if (separateGroups)
            {
                var groups = new JSONArray();
                foreach (var entry in events.Children)
                {
                    var nextGroup = group.Clone();
                    nextGroup["b"] = 4f + entry["b"].AsFloat;
                    var nextEvent = entry.Clone();
                    nextEvent["b"] = 0f;
                    var nextEvents = new JSONArray();
                    nextEvents.Add(nextEvent);
                    nextGroup["e"][0]["e"] = nextEvents;
                    groups.Add(nextGroup);
                }

                json["lightColorEventBoxGroups"] = groups;
            }

            return json.ToString();
        }

        // A strobing final node keeps pulsing after the group; the tail strip must track the same phase as playback.
        [Test]
        public void StrobingTailExtendsAndMatchesPreview()
        {
            var song = BeatSaberSongContainer.Instance;
            var originalSong = song.LoadedSong;
            // Lengthen the fake clip so the last node's held strobe tail has room to render past its own beat.
            song.LoadedSong = AudioClip.Create("Long fake song", 44100 * 120, 1, 44100, false);
            try
            {
                LoadAlternatingChunks();
                foreach (var beat in new[] { 110f, 115.25f })
                {
                    AssertRibbonPixels(0, 0, 1, beat, -1);
                    AssertRibbonPixels(0, 1, 1, beat, -1);
                }
            }
            finally
            {
                song.LoadedSong = originalSong;
            }
        }

        // Under the faster BPM, a held 1/1 strobe must complete three cycles before the yellow node three
        // authored beats later.
        [Test]
        public void BpmScaledStrobeCompletesThreeCyclesBeforeInstantSuccessor()
        {
            LoadPlayback(BpmStrobeHoldJson);
            var entries = CountStrobeEntries(0, 4f, 7f);
            Assert.That(entries, Is.EqualTo(3),
                "A 1/1 strobe over three authored beats must run three cycles, not three base-BPM beats of cycles.");
            Assert.IsTrue(IsStrobeOn(ColorAt(0, 6.95f)),
                "The fourth strobe cycle's bright phase must still be active on the frame before the successor turns on.");
            AssertStrobeRibbonParity(0, 0, 0, 0, 4.7f, 5.3f, 6.8f);
        }

        // A terminal lit strobe holds to the real song end: bounds, ribbon span, retention, and
        // ribbon pixels must all agree with the still-running playback light instead of stopping at
        // an early bound.
        [Test]
        public void BpmScaledStrobeTailExtendsRibbonToRealSongEnd()
        {
            LoadPlayback(BpmStrobeTailJson);
            var source = Node(0);
            var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
            Assert.IsTrue(timeline.TryGetBounds(source, out _, out var boundEnd));
            Assert.That(boundEnd, Is.EqualTo(SongTime(240f)).Within(0.01f),
                "The lit tail must extend to the loaded clip's end (60s at 100 BPM -> SongBpmTime 100).");

            var ribbonObject = new GameObject("BPM strobe tail ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, source, appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf);
                Assert.That(ribbon.ColorTimelineStart, Is.EqualTo(SongTime(4f)).Within(0.001f));
                Assert.That(ribbon.ColorTimelineDuration,
                    Is.EqualTo(SongTime(240f) - SongTime(4f)).Within(0.01f),
                    "The tail ribbon must cover the held segment to the song end.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }

            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(200f), null, retained);
            Assert.IsTrue(retained.Contains(map.LightColorEventBoxGroups[0]),
                "The tail's group must stay retained while its ribbon still renders near the song end.");

            // Parity deep into the tail: the strip must track the live light's phase, never blank out.
            // Integer beats land exactly on phase zero, so probes offset into each phase half to keep
            // the sub-ULP CPU/GPU boundary rounding out of the comparison.
            AssertStrobeRibbonParity(0, 0, 0, 0, 20.3f, 60.6f, 110.3f, 150.7f, 200.2f, 239.4f);
        }

        // Far into a long-running transition the ribbon must still equal the physical light at every
        // probe: the last three authored beats alone must contain three complete cycles.
        [Test]
        public void BpmScaledStrobeRibbonMatchesLightFarIntoTransition()
        {
            LoadPlayback(BpmStrobeFarJson);
            var entries = CountStrobeEntries(0, 197f, 200f);
            Assert.That(entries, Is.EqualTo(3),
                "Beats 197-200 of a 1/1 strobe must still hold three cycles deep into the transition.");
            Assert.IsFalse(IsStrobeOn(ColorAt(0, 199f)),
                "A 1/1 strobe must sit at phase zero (its dim half) on an integer authored beat deep into the transition.");
            AssertStrobeRibbonParity(0, 0, 0, 0, 10.3f, 50.6f, 100.2f, 150.7f, 199.3f);
        }

        // One source splits per light: filtered lights end their strip at the beat-7 successor while
        // the rest run the strobe tail to the song end; each side keeps its authored cycle count.
        [Test]
        public void BpmScaledStrobeFilteredSuccessorSplitsPerLightTail()
        {
            LoadPlayback(BpmStrobeFilteredJson);
            var source = Node(0);
            var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
            Assert.IsTrue(timeline.TryGetOutgoing(source, 0, out var even));
            Assert.That(even.EndTime, Is.EqualTo(SongTime(7f)).Within(0.001f),
                "An even light's strip must end at its filtered successor, not the song end.");
            Assert.IsTrue(timeline.TryGetOutgoing(source, 1, out var odd));
            Assert.That(odd.EndTime, Is.EqualTo(float.MaxValue),
                "An unfiltered light's strip must remain a terminal tail.");
            Assert.IsTrue(timeline.TryGetBounds(source, out _, out var boundEnd));
            Assert.That(boundEnd, Is.EqualTo(SongTime(240f)).Within(0.01f),
                "The source union must reach the song end through its unfiltered lights.");
            Assert.That(CountStrobeEntries(0, 4f, 7f), Is.EqualTo(3),
                "Even lights still run three cycles before their filtered successor.");
            Assert.That(CountStrobeEntries(1, 4f, 12f), Is.EqualTo(8),
                "Odd lights keep counting authored-beat cycles into the terminal tail.");
            AssertStrobeRibbonParity(0, 0, 0, 1, 60.6f, 150.7f);
        }

        // Without BPM events, authored beats and SongBpmTime beats match one-for-one.
        [Test]
        public void ConstantBpmStrobeCompletesThreeCyclesBeforeInstantSuccessor()
        {
            LoadPlayback(@"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":0,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0],""strobeColor"":[0,0,1]}}]}]},
                {""b"":7,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[1,1,0]}}]}]}]}");
            Assert.That(CountStrobeEntries(0, 4f, 7f), Is.EqualTo(3));
            AssertStrobeRibbonParity(0, 0, 0, 0, 4.7f, 5.3f, 6.8f);
        }

        // A strobe phase is bright on [0.5, 1): the held base is red (b=0) while the strobe band is
        // blue (b=1), so a blue-dominant sample means the strobe's alternate phase is showing.
        private static bool IsStrobeOn(Color color) => color.b > color.r + 0.1f;

        // Counts transitions into the strobe's bright half on a real playback light across authored beats.
        private int CountStrobeEntries(int light, float fromBeat, float toBeat)
        {
            var entries = 0;
            var wasOn = false;
            for (var beat = fromBeat; beat < toBeat; beat += 0.005f)
            {
                var on = IsStrobeOn(ColorAt(light, beat));
                if (on && !wasOn)
                {
                    entries++;
                }

                wasOn = on;
            }

            return entries;
        }

        // Like AssertRibbonPixels but reuses one ribbon/material across many beat probes so parity is
        // checked at several points deep into a single transition or tail instead of once per build.
        private void AssertStrobeRibbonParity(int group, int box, int node, int light, params float[] beats)
        {
            var source = Node(group, box, node);
            var ribbonObject = new GameObject("BPM strobe parity ribbon");
            Material material = null;
            Material reference = null;
            var referenceTexture = new Texture2D(LightCount, 4, TextureFormat.RGBAFloat, false, true);
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, source, appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf,
                    $"Source {source.JsonTime} must keep its ribbon for the whole tested span.");
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                reference = new Material(Shader.Find("ChroMapper/Object/Basic Gradient")) { enableInstancing = false };
                reference.SetFloat("_UseLightDistribution", 1f);
                reference.SetFloat("_LightDistributionWidth", LightCount);
                reference.SetTexture("_LightDistributionTex", referenceTexture);
                var lane = (LightCount - light - 0.5f) / LightCount;
                foreach (var beat in beats)
                {
                    var expected = ReferenceEquals(StateAt(light, beat).Base, source)
                        ? ColorAt(light, beat)
                        : Color.black;
                    var colors = new Color[LightCount * 4];
                    Array.Fill(colors, expected);
                    referenceTexture.SetPixels(colors);
                    referenceTexture.Apply(false, false);
                    var progress = (SongTime(beat) - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                    var pixel = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
                    var expectedPixel = GLSColorTransitionCacheTest.RenderGradientPixel(reference, 0.5f, lane);
                    AssertColor(pixel, expectedPixel, 0.02f,
                        $"light={light} beat={beat}: the ribbon strip must match the physical light's strobe phase.");
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(reference);
                Object.DestroyImmediate(referenceTexture);
                Object.DestroyImmediate(ribbonObject);
            }
        }
    }
}

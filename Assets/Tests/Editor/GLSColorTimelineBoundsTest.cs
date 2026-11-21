using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // GLSColorTimeline segment bounds: head/tail sentinel extents, retention intervals, incremental
    // Add/Remove edits, and the ribbon bodies they drive at the start and end of the song.
    public class GLSColorTimelineBoundsTest : GLSColorPlaybackTestBase
    {
        // A lane ending in an unlit hold must render nothing past it: no tail ribbon and no retention.
        private const string DarkTailMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":40,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[1,1,1]}},
                {""b"":5,""c"":0,""s"":0,""i"":1,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,0,1]}}]}]}]}";

        // A single transition node owns only its post-event direction: dark before its own beat, lit after it by its hold.
        private const string SoloTransitionMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":50,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,1,0]}}]}]}]}";

        // An instant first node holds the pre-map black instead, so nothing may extend before its own beat.
        private const string InstantHeadMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":50,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":0,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,1,0]}}]}]}]}";

        // A first node with a transition easing still stays dark to its own beat, even when a BPM event rescales song time.
        private const string BpmLitHeadJson = @"{""version"":""3.3.0"",""bpmEvents"":[{""b"":0,""m"":240}],""lightColorEventBoxGroups"":[
            {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":0,""sf"":0,""customData"":{""color"":[0,1,0]}}]}]}]}";

        // Condense Collider's 40-lane beat-0 group to eight lanes: empty claims and one
        // delayed first event precede the same lime/magenta beat-9 and red beat-90 events.
        private const string SparseFirstGroupStrobeMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":0,""g"":1,""e"":[
                {""f"":{""f"":2,""p"":0,""t"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[]},
                {""f"":{""f"":2,""p"":1,""t"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[]},
                {""f"":{""f"":2,""p"":2,""t"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[]},
                {""f"":{""f"":2,""p"":3,""t"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":1,""i"":1,""f"":1,""sb"":2,""sf"":0,""customData"":{""color"":[1,0,0],""strobeColor"":[0.889,0,1]}}]},
                {""f"":{""f"":2,""p"":4,""t"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0.5,""c"":0,""s"":0.9,""i"":1,""f"":1,""sb"":3,""sf"":0,""customData"":{""color"":[0,1,0.166],""strobeColor"":[0.889,0,1]}}]},
                {""f"":{""f"":2,""p"":5,""t"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[]},
                {""f"":{""f"":2,""p"":6,""t"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[]},
                {""f"":{""f"":2,""p"":7,""t"":0},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[]}]},
            {""b"":9,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":1,""i"":1,""f"":1,""sb"":2,""sf"":0,""customData"":{""color"":[0.452,1,0],""strobeColor"":[0.889,0,1]}}]}]},
            {""b"":90,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[{""b"":0,""c"":0,""s"":1,""i"":1,""f"":1,""sb"":0.985,""sf"":0,""customData"":{""color"":[1,0,0],""strobeColor"":[1,0,0.999]}}]}]}]}";

        // Empty boxes claim their lights at beat 0 but must not turn them on before the first color event at
        // beat 9.
        private void AssertSparseFirstGroupPlaybackStaysOffBeforeFirstEvent()
        {
            Assert.That(ColorAt(0, 4f).a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ColorAt(0, 8.9f).a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ColorAt(3, 4f).a, Is.GreaterThan(0.1f));
            Assert.That(ColorAt(0, 9.1f).a, Is.GreaterThan(0.1f));
        }

        // The incoming beat-9 ribbon may show previously lit lights, but lights with no preceding event must
        // stay dark.
        [Test]
        public void SparseFirstGroupKeepsPlaybackAndRibbonsDarkUntilEachLightsFirstEvent()
        {
            LoadPlayback(SparseFirstGroupStrobeMapJson);
            var ribbonObject = new GameObject("Sparse first group incoming ribbon");
            Material material = null;
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, Node(1), appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                var progress = (SongTime(4f) - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration;
                var unlitLane = (LightCount - 0.5f) / LightCount;
                var litLane = (LightCount - 3.5f) / LightCount;
                var unlit = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, unlitLane);
                var lit = GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, litLane);
                Assert.That(unlit.r + unlit.g + unlit.b, Is.LessThan(0.01f));
                Assert.That(lit.r + lit.g + lit.b, Is.GreaterThan(0.05f));
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(ribbonObject);
            }

            AssertSparseFirstGroupPlaybackStaysOffBeforeFirstEvent();
            AssertDelayedFirstColorEventKeepsLightOffUntilItsOwnBeat();
            AssertDelayedFirstColorEventHasNoPrematureIncomingRibbon();
        }

        // A first node at beat 0.5 must not turn on at its group's beat-0 start.
        private void AssertDelayedFirstColorEventKeepsLightOffUntilItsOwnBeat()
        {
            Assert.That(ColorAt(4, 0.25f).a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ColorAt(4, 0.75f).a, Is.GreaterThan(0.1f));
            Assert.That(ColorAt(3, 0.25f).a, Is.GreaterThan(0.1f));
        }

        // The first node at beat 0.5 has no preceding color to draw before it.
        private void AssertDelayedFirstColorEventHasNoPrematureIncomingRibbon()
        {
            var delayed = Node(0, 4);
            var ribbonObject = new GameObject("Delayed first color incoming ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, delayed, appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf);
                var timeline = GLSEventCommon.GetColorTimeline(delayed, LightCount);
                Assert.IsTrue(timeline.TryGetBounds(delayed, out var start, out _));
                Assert.That(start, Is.GreaterThanOrEqualTo(SongTime(0.5f) - 0.001f));
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // Instant first nodes preserve the sentinel black and have no incoming transition ribbon.
        private void AssertFirstInstantNodeRemainsOffUntilItsBeatAndHasNoIncomingRibbon()
        {
            Assert.That(ColorAt(0, 25f).a, Is.EqualTo(0f).Within(0.001f),
                "GLS lights must remain off before their first authored event.");
            var ribbonObject = new GameObject("First node incoming ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, Node(0), appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf,
                    "The first authored event has no preceding light behavior to render as a ribbon.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // Editing one light must preserve unrelated states and update the indexed retention bounds without rebuilding the ID.
        [Test]
        public void TimelineEditsRetainUnchangedLightsAndRefreshBounds()
        {
            var source = Node(0);
            var oddSource = Node(1, 1);
            var finalFiltered = Node(1, 0, 3);
            var end = Node(2);
            var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
            Assert.IsTrue(timeline.TryGetOutgoing(oddSource, 1, out var unchanged));
            var inserted = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{""b"":44,""g"":1,""e"":[{""f"":{""f"":2,""p"":0,""t"":8},""e"":[{""b"":0,""i"":1,""s"":1,""customData"":{""color"":[1,1,0]}}]}]}"));
            inserted.SetMap(map);
            inserted.RecomputeSongBpmTime();
            map.LightColorEventBoxGroups.Insert(2, inserted);
            GLSEventCommon.AddColorTransitionGroup(inserted);
            Assert.AreSame(timeline, GLSEventCommon.GetColorTimeline(source, LightCount),
                "An edit must update the existing indexed timeline rather than rebuilding every light's states.");
            Assert.IsTrue(timeline.TryGetOutgoing(oddSource, 1, out var afterInsert));
            Assert.AreSame(unchanged, afterInsert, "The unclaimed odd light's state must not be regenerated.");
            Assert.IsTrue(timeline.TryGetOutgoing(finalFiltered, 0, out var even));
            Assert.AreSame(inserted.Boxes[0].Events[0], even.Next.Base);
            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(45f), null, retained);
            Assert.IsTrue(retained.Contains(inserted));
            map.LightColorEventBoxGroups.Remove(inserted);
            GLSEventCommon.RemoveColorTransitionGroup(inserted);
            Assert.AreSame(timeline, GLSEventCommon.GetColorTimeline(source, LightCount));
            Assert.IsTrue(timeline.TryGetOutgoing(finalFiltered, 0, out even));
            Assert.AreSame(end, even.Next.Base);
            Assert.IsTrue(timeline.TryGetOutgoing(oddSource, 1, out var afterRemove));
            Assert.AreSame(unchanged, afterRemove);
            retained.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(45f), null, retained);
            Assert.IsFalse(retained.Contains(inserted), "Retired group intervals must not survive in the viewport index.");
        }

        // Playback's initial sentinel is an internal held state, not an authored source extending an incoming ribbon before the map.
        [Test]
        public void FirstAuthoredNodeHasNoSentinelIncomingRibbon()
        {
            var timeline = GLSEventCommon.GetColorTimeline(Node(0), LightCount);
            for (var light = 0; light < LightCount; light++)
                Assert.IsFalse(timeline.TryGetIncoming(Node(0), light, out _),
                    "An initial sentinel must not become a visible incoming ribbon source.");
        }

        // A held tail needs both its ribbon pixels and its source group after the final node scrolls offscreen.
        [Test]
        public void LitTailRendersAndRetainsSourceGroupUntilSongEnd()
        {
            var final = Node(2);
            var timeline = GLSEventCommon.GetColorTimeline(final, LightCount);
            Assert.IsTrue(timeline.TryGetBounds(final, out _, out var end));
            Assert.That(end, Is.GreaterThan(SongTime(99f)),
                "A lit held tail must extend the source interval toward the loaded song's end.");
            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(80f), null, retained);
            Assert.IsTrue(retained.Contains(map.LightColorEventBoxGroups[2]),
                "The final node's group must stay loaded while its held tail ribbon remains visible.");

            foreach (var (light, beat) in new[] { (0, 47f), (1, 60f), (7, 99.5f) })
            {
                AssertRibbonPixels(2, 0, 0, beat, light);
            }
        }

        // After a cross-group takeover the new last node holds every light; its tail must still reach the song end.
        [Test]
        public void InterruptedTailExtendsAfterTakeover()
        {
            LoadPlayback(InterruptedMapJson);
            foreach (var (light, beat) in new[] { (0, 25f), (7, 60f) })
            {
                AssertRibbonPixels(2, 0, 0, beat, light);
            }
        }

        // A lane that only holds black has nothing to show: no tail ribbon and no retention past the last lit segment.
        [Test]
        public void DarkTailDoesNotExtendOrRetain()
        {
            LoadPlayback(DarkTailMapJson);
            var final = Node(0, 0, 1);
            Assert.That(ColorAt(0, 50f).a, Is.EqualTo(0f).Within(0.001f),
                "Playback control: the lane holds black after the dark node.");
            var timeline = GLSEventCommon.GetColorTimeline(final, LightCount);
            Assert.IsFalse(timeline.TryGetBounds(final, out _, out _),
                "A dark terminal hold contributes no retention interval.");
            var ribbonObject = new GameObject("Dark tail ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, final, appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf, "A lane that only holds black must not draw a tail ribbon.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(60f), null, retained);
            Assert.IsFalse(retained.Contains(map.LightColorEventBoxGroups[0]),
                "Nothing remains visible after the dark node, so its group must not stay loaded.");
        }

        // A transition-type first node is preceded only by the dark sentinel, so neither box's
        // first node may render an inner incoming ribbon.
        private void AssertFirstNodesHaveNoInnerIncomingRibbon(int box)
        {
            var ribbonObject = new GameObject("First node inner incoming ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, Node(0, box), appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf,
                    $"Box {box}'s first node is preceded only by the sentinel, which owns no incoming strip.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
        }

        [Test]
        public void FirstNodesHaveNoInnerOrOuterIncomingRibbon()
        {
            LoadAlternatingChunks();
            for (var box = 0; box < 2; box++)
            {
                AssertFirstNodesHaveNoInnerIncomingRibbon(box);
            }

            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            var owner = (GLSGroupContainer)collection.CreateContainer();
            try
            {
                owner.ObjectData = map.LightColorEventBoxGroups[0];
                owner.Setup();
                owner.PreviewEventData = Node(0);
                Assert.IsNotNull(owner.IncomingLightGradientController,
                    "A spawned outer body must own a dedicated incoming ribbon controller.");
                var ribbon = owner.IncomingLightGradientController;
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, Node(0), appearance, _ => false, LightCount, aggregateSameTimeBoxes: true);
                Assert.IsFalse(ribbon.gameObject.activeSelf,
                    "The outer preview must not project a sentinel fade-in before the shared first timestamp.");
            }
            finally
            {
                Object.DestroyImmediate(owner.gameObject);
            }
        }

        // A lone transition node lights at its own beat and holds after it, but nothing precedes
        // it, so no incoming ribbon may render before it.
        private void AssertSoloTransitionNodeHoldsAfterItsBeatWithoutIncoming()
        {
            var ribbonObject = new GameObject("Solo node incoming ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, Node(0), appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf,
                    "A first transition node has no lit pre-node segment, so no incoming ribbon may render.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
            AssertRibbonPixels(0, 0, 0, 75f, -1);
        }

        // Nothing precedes the first node, so its group must not be retained before its own beat;
        // the held tail still keeps the group loaded afterwards.
        private void AssertFirstNodeGroupIsNotRetainedBeforeItsBeat()
        {
            var node = Node(0);
            var timeline = GLSEventCommon.GetColorTimeline(node, LightCount);
            Assert.IsTrue(timeline.TryGetBounds(node, out var start, out _));
            Assert.That(start, Is.EqualTo(node.SongBpmTime).Within(0.001f),
                "No sentinel head may extend the source interval before the first authored node.");
            var retained = new System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(5f), null, retained);
            Assert.IsFalse(retained.Contains(map.LightColorEventBoxGroups[0]),
                "The first node's group has no visible ribbon before its own beat.");
            GLSEventCommon.GetColorTransitionSourceGroupsAt(SongTime(75f), null, retained);
            Assert.IsTrue(retained.Contains(map.LightColorEventBoxGroups[0]),
                "The lit tail still keeps its group loaded after the first node's beat.");
        }

        // A first Linear node at beat 50 stays dark until that beat; the initial off state must not create an
        // early fade-in.
        [Test]
        public void FirstTransitionNodeStartsPlaybackRibbonsAndRetentionAtItsOwnBeat()
        {
            LoadPlayback(SoloTransitionMapJson);
            foreach (var beat in new[] { 25f, 49f })
            {
                for (var light = 0; light < LightCount; light++)
                {
                    Assert.That(
                        ColorAt(light, beat).a,
                        Is.EqualTo(0f).Within(0.001f),
                        $"light={light} beat={beat}: every lane must stay dark before the first authored " +
                        "color event at beat 50; a transition-type first node cannot invent a lit " +
                        "pre-map segment.");
                }
            }

            for (var light = 0; light < LightCount; light++)
            {
                Assert.That(
                    ColorAt(light, 50.5f).a,
                    Is.GreaterThan(0.1f),
                    $"light={light}: the beat-50 transition event must light its lane once its own beat " +
                    "has passed.");
            }

            var ribbonObject = new GameObject("Interpolated first node incoming ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, Node(0), appearance, _ => false, LightCount);
                Assert.IsFalse(
                    ribbonObject.activeSelf,
                    "The first authored event has no preceding state, so it must not draw an incoming ribbon.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }

            Assert.IsTrue(GLSEventCommon.TryGetColorRibbonBounds(Node(0), LightCount, out var start, out _));
            Assert.That(start, Is.EqualTo(Node(0).SongBpmTime).Within(0.001f),
                "Retention must begin at the first node itself, not at the pre-map sentinel.");
            AssertSoloTransitionNodeHoldsAfterItsBeatWithoutIncoming();
            AssertFirstNodeGroupIsNotRetainedBeforeItsBeat();
        }

        // An instant first node keeps the pre-map black, so neither an incoming ribbon nor early retention may appear.
        [Test]
        public void InstantFirstNodeDoesNotExtendIncoming()
        {
            LoadPlayback(InstantHeadMapJson);
            var node = Node(0);
            Assert.That(ColorAt(0, 49f).a, Is.EqualTo(0f).Within(0.001f),
                "Playback control: an instant first node keeps its lights dark until its beat.");
            var ribbonObject = new GameObject("Instant head ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(ribbon, node, appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf,
                    "An instant first node has no lit pre-node segment, so no incoming ribbon may render.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
            Assert.IsTrue(GLSEventCommon.TryGetColorRibbonBounds(node, LightCount, out var start, out var end));
            Assert.That(start, Is.EqualTo(node.SongBpmTime).Within(0.001f),
                "Retention must begin at the node itself, not at the pre-map sentinel.");
            Assert.That(end, Is.GreaterThan(SongTime(99f)),
                "The instant node's held output still needs its tail interval through the song end.");
            AssertFirstInstantNodeRemainsOffUntilItsBeatAndHasNoIncomingRibbon();
        }

        // A first node stays dark to its own beat even when a BPM event rescales song time;
        // no incoming ribbon may appear before it.
        [Test]
        public void BpmScaledFirstNodeHasNoIncomingRibbon()
        {
            LoadPlayback(BpmLitHeadJson);
            var node = Node(0);
            Assert.That(ColorAt(0, 2f).a, Is.EqualTo(0f).Within(0.001f),
                "Playback control: a BPM-scaled first node keeps its lights dark until its beat.");
            Assert.That(ColorAt(0, 4.5f).a, Is.GreaterThan(0.1f),
                "Playback control: the beat-4 node still lights its lane once its beat has passed.");
            var ribbonObject = new GameObject("BPM head ribbon");
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out _);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(ribbon, node, appearance, _ => false, LightCount);
                Assert.IsFalse(ribbonObject.activeSelf,
                    "A BPM-scaled first node is preceded only by the sentinel, which owns no incoming strip.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // B and C need the preceding group's incoming strips in their own inner lanes without losing their outgoing ribbons.
        [Test]
        public void InnerFirstNodesHaveIncomingRibbonFromA()
        {
            for (var box = 0; box < 2; box++)
            {
                var collection = Object.FindAnyObjectByType<GLSEventGridContainer>();
                var container = (GLSEventContainer)collection.CreateContainer();
                var innerAppearance = ScriptableObject.CreateInstance<GLSEventAppearanceSO>();
                try
                {
                    container.ObjectData = Node(1, box);
                    container.Setup();
                    container.GlsLightCount = LightCount;
                    typeof(GLSEventAppearanceSO).GetField("eventAppearance", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(innerAppearance, appearance);
                    innerAppearance.SetAppearance(container);
                    container.UpdateGridPosition();
                    innerAppearance.UpdateTransitionRibbon(container, _ => false);
                    var incoming = false;
                    var outgoing = false;
                    foreach (var ribbon in container.GetComponentsInChildren<LightGradientController>(true))
                    {
                        if (!ribbon.gameObject.activeSelf)
                            continue;
                        if (ribbon.transform.localPosition.z < -0.1f)
                            incoming = true;
                        else
                            outgoing = true;
                    }
                    Assert.IsTrue(incoming,
                        $"Inner box {box} must show A's incoming transition on its own controlled strips.");
                    Assert.IsTrue(outgoing,
                        $"Inner box {box} must retain its outgoing transition while displaying the cross-group incoming one.");
                }
                finally
                {
                    Object.DestroyImmediate(container.gameObject);
                    Object.DestroyImmediate(innerAppearance);
                }
            }
        }
    }
}

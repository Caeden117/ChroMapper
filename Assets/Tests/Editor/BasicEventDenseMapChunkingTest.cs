using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // The synthetic matrices did not reproduce the missing node, so name this accurately as a dense real-map chunking
    // stress test that checks every in-window visual without claiming coverage of the separate invisible-node report.
    public class BasicEventDenseMapChunkingTest : BasicEventChunkingTestBase
    {
        // Compare the applied shader state before and after recycling so a dictionary-owned but black/transparent stale
        // model cannot satisfy the stronger dense-map regression merely because its renderer remains enabled.
        private static readonly int colorAId = Shader.PropertyToID("_ColorA");
        private static readonly int colorBId = Shader.PropertyToID("_ColorB");
        private static readonly int mainAlphaId = Shader.PropertyToID("_MainAlpha");
        private static readonly int fadeSizeId = Shader.PropertyToID("_FadeSize");

        private const float ScaledBaseBpm = 700f;

        private bool? visualizeGradientsBeforeTest;
        private bool? emulateChromaLiteBeforeTest;
        private int? chunkDistanceBeforeTest;
        private int? gridMeasureSnappingBeforeTest;

        // Thousands of combinatorix assertions read renderer state; reuse one block to avoid test-induced GC altering
        // the frame cadence that the rapid backward-reload regression is intended to exercise.
        private readonly MaterialPropertyBlock rendererProperties = new();

        // Reuse ownership sets at every scrub stop so exhaustive all-window validation stays linear in loaded events.
        private readonly HashSet<BaseObject> orderedPoolObjects = new();
        private readonly HashSet<ObjectContainer> boundWindowContainers = new();

        [UnityTest]
        public IEnumerator DenseNormalLanesForwardUnloadAndBackwardScrubReloadEveryNodeAndRibbon()
        {
            // Load the real 1,235-event beat-450..610 slice instead of reconstructing placement callbacks or selecting a
            // presumed failing node; the production loader establishes its collection order, links, and pooled visuals.
            var difficulty = LoadReportedDifficulty();
            yield return TestUtils.ReloadMap(3, difficulty, beatsPerMinute: ScaledBaseBpm);

            var editModeContext = UnityEngine.Object.FindAnyObjectByType<EditModeContext>();
            editModeContext.EditingMode = EditingMode.BasicEvent;
            var eventsContainer = GetEventsContainer();
            visualizeGradientsBeforeTest = Settings.Instance.VisualizeChromaGradients;
            emulateChromaLiteBeforeTest = Settings.Instance.EmulateChromaLite;
            chunkDistanceBeforeTest = Settings.Instance.ChunkDistance;
            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            gridMeasureSnappingBeforeTest = atsc.GridMeasureSnapping;
            Settings.Instance.VisualizeChromaGradients = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.ChunkDistance = 2;
            // DenseNormalLanesForwardUnloadAndBackwardScrubReloadEveryNodeAndRibbon must reproduce ordinary snapped
            // wheel navigation; fixing precision at 1/32 prevents inherited suite state from changing route thresholds.
            atsc.GridMeasureSnapping = 32;
            eventsContainer.EventTypeToPropagate = (int)EventTypeValue.Event1;
            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
            yield return null;

            var mapEvents = eventsContainer.MapObjects.OfType<BaseEvent>().ToArray();
            Assert.That(mapEvents, Has.Length.EqualTo(1235), "The reported Basic Event fixture did not load intact.");
            var reportedMissing = mapEvents.Single(evt =>
                Mathf.Approximately(evt.JsonTime, 529f)
                && evt.Type == (int)EventTypeValue.Event1
                && evt.Value == (int)LightValue.RedOn);
            var sameLaneBehind = mapEvents.Last(evt =>
                evt.Type == reportedMissing.Type
                && evt.JsonTime < reportedMissing.JsonTime);
            var sameLaneAhead = mapEvents.First(evt =>
                evt.Type == reportedMissing.Type
                && evt.JsonTime > reportedMissing.JsonTime);
            var fadeRegressionNode = mapEvents.Single(evt =>
                Mathf.Approximately(evt.JsonTime, 527f)
                && evt.Type == (int)EventTypeValue.Event12);

            // DenseNormalLanesForwardUnloadAndBackwardScrubReloadEveryNodeAndRibbon formerly spent three full sweeps
            // establishing 1,235 appearance baselines that never exposed the missing node. Retain wide pool churn with
            // five stops while still validating every node and ribbon in each resulting loaded window.
            yield return ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
                new[] { 450f, 610f, 452f, 608f, reportedMissing.SongBpmTime },
                "during compact full-range visual-pool conditioning");
            Assert.That(
                CaptureNodeVisual(fadeRegressionNode).FadeSize,
                Is.EqualTo(0.75f).Within(0.001f),
                "The non-light beat-527 node did not initialize its pooled fade size before playback.");

            // Establish that the exact pink middle-lane node and both chronological neighbors initially render, then
            // force the whole cluster out of the stopped-time visual pool before reversing direction.
            yield return ScrubAndAssertEveryNormalLaneVisual(
                new[] { 516.37f, 523.61f, 529.13f },
                "while approaching the reported beat-529 cluster");
            AssertNodeVisualLoaded(reportedMissing, "before the reported cluster unload");
            AssertNodeVisualLoaded(sameLaneBehind, "before the reported cluster unload");
            AssertNodeVisualLoaded(sameLaneAhead, "before the reported cluster unload");
            var expectedReportedNodeVisual = CaptureNodeVisual(reportedMissing);

            yield return ScrubAndAssertEveryNormalLaneVisual(
                new[] { 535.41f, 540.87f },
                "while scrubbing forward until the reported cluster unloads");
            AssertNodeVisualUnloaded(reportedMissing, "after the reported cluster unloaded ahead of the playhead");

            // Uneven backward stops cross both sides of chunk midpoints and assert the entire authoritative visual
            // window after every production LateUpdate, catching one missing middle-lane node even when neighbors load.
            yield return ScrubAndAssertEveryNormalLaneVisual(
                new[] { 539.63f, 537.18f, 534.91f, 532.49f, 531.26f },
                "during the immediate arbitrary-distance backward scrub");
            AssertNodeVisualLoaded(reportedMissing, "after the immediate backward scrub returned to beat 529");
            AssertNodeVisualLoaded(sameLaneBehind, "after the immediate backward scrub returned to beat 529");
            AssertNodeVisualLoaded(sameLaneAhead, "after the immediate backward scrub returned to beat 529");
            AssertNodeVisualMatches(
                expectedReportedNodeVisual,
                CaptureNodeVisual(reportedMissing),
                "after the immediate backward scrub returned to beat 529");

            // Several wheel reports can be processed before collection LateUpdate. Repeat the unload and queue the
            // complete reversal in one frame so a skipped intermediate chunk cannot strand a recycled node container.
            yield return ScrubThroughJsonTimes(540.87f);
            AssertNodeVisualUnloaded(reportedMissing, "before the same-frame backward reversal");
            var sameFrameStops = new[] { 539.44f, 536.73f, 534.38f, 532.48f, 531.11f };
            for (var stopIndex = 0; stopIndex < sameFrameStops.Length; stopIndex++)
            {
                MoveToSnappedJsonTime(
                    atsc,
                    sameFrameStops[stopIndex],
                    "during the dense same-frame backward reversal");
            }
            yield return null;

            // One immediate and one subsequent frame catch delayed recycling without adding four redundant stable waits.
            for (var stableFrame = 0; stableFrame <= 1; stableFrame++)
            {
                if (stableFrame > 0)
                {
                    yield return null;
                }

                var operation = stableFrame == 0
                    ? "after the dense same-frame backward reversal"
                    : $"on stable frame {stableFrame} after the dense same-frame backward reversal";
                AssertEveryNormalLaneVisualInCurrentWindow(operation);
                AssertNodeVisualLoaded(reportedMissing, operation);
                AssertNodeVisualLoaded(sameLaneBehind, operation);
                AssertNodeVisualLoaded(sameLaneAhead, operation);
                AssertNodeVisualMatches(
                    expectedReportedNodeVisual,
                    CaptureNodeVisual(reportedMissing),
                    operation);
            }

            // Always return through the exact reported node's authored beat. The previous hard-coded song beat 529.25
            // converted to an off-grid JSON time after the fixture BPM event and left most later routes visibly skewed.
            var centralStartSongBpmTime = reportedMissing.SongBpmTime;

            // Run playback handoffs before the stopped-only matrix so a callback/cache failure reports quickly while
            // still using the pool churned across both ends of the real map by the compact conditioning route above.
            var playbackRoutes = BuildPlaybackScrubCombinatorix(centralStartSongBpmTime);
            for (var routeIndex = 0; routeIndex < playbackRoutes.Length; routeIndex++)
            {
                var route = playbackRoutes[routeIndex];
                yield return RunPlaybackScrubRoute(route);
                yield return ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
                    new[] { centralStartSongBpmTime },
                    $"while returning after playback route {route.Name}");
                AssertNodeVisualLoaded(reportedMissing, $"after playback route {route.Name}");
                AssertNodeVisualMatches(
                    expectedReportedNodeVisual,
                    CaptureNodeVisual(reportedMissing),
                    $"after playback route {route.Name}");
                Assert.That(
                    CaptureNodeVisual(fadeRegressionNode).FadeSize,
                    Is.EqualTo(0.75f).Within(0.001f),
                    $"The non-light beat-527 node inherited a pooled fade size after playback route {route.Name}.");
            }

            // Reuse this same loaded map and pool across the complete boundary/direction matrix. Returning to the exact
            // authored central beat between entries prevents fixture-edge clamping without resetting pooled state.
            var scrubRoutes = BuildScrubCombinatorix(centralStartSongBpmTime);
            Assert.That(scrubRoutes, Has.Length.EqualTo(3), "The compact scrub matrix lost a distinct retained route.");
            for (var routeIndex = 0; routeIndex < scrubRoutes.Length; routeIndex++)
            {
                var route = scrubRoutes[routeIndex];
                yield return ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
                    new[] { centralStartSongBpmTime },
                    $"while resetting before combinatorix route {route.Name}");
                yield return ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
                    route.SongBpmTimes,
                    $"during combinatorix route {route.Name}");
                yield return ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
                    new[] { centralStartSongBpmTime },
                    $"while returning after combinatorix route {route.Name}");
                AssertNodeVisualLoaded(reportedMissing, $"after combinatorix route {route.Name}");
                AssertNodeVisualMatches(
                    expectedReportedNodeVisual,
                    CaptureNodeVisual(reportedMissing),
                    $"after combinatorix route {route.Name}");
            }

            // Queue several complete direction histories before one LateUpdate as a second matrix dimension. Each burst
            // reuses the same increasingly recycled pool and is checked once production has processed its final stop.
            var sameFrameRoutes = BuildSameFrameScrubCombinatorix(centralStartSongBpmTime);
            for (var routeIndex = 0; routeIndex < sameFrameRoutes.Length; routeIndex++)
            {
                var route = sameFrameRoutes[routeIndex];
                yield return ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
                    new[] { centralStartSongBpmTime },
                    $"while resetting before same-frame route {route.Name}");
                yield return ScrubSameFrameAndAssertEveryNormalLaneVisual(route);
                yield return ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
                    new[] { centralStartSongBpmTime },
                    $"while returning after same-frame route {route.Name}");
                AssertNodeVisualLoaded(reportedMissing, $"after same-frame route {route.Name}");
                AssertNodeVisualMatches(
                    expectedReportedNodeVisual,
                    CaptureNodeVisual(reportedMissing),
                    $"after same-frame route {route.Name}");
            }

        }

        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            // This fixture deliberately replaces the shared mapper scene with a production-loaded dense map. Restore
            // both settings and an empty V3 scene even when its assertion fails so no later fixture inherits 1,235 events.
            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null && atsc.IsPlaying)
            {
                // A playback-matrix assertion can abort between Play and Pause; stop callback-driven recycling before
                // replacing the map so the failed regression test cannot corrupt later fixtures during scene teardown.
                atsc.CancelPlaying();
            }

            if (visualizeGradientsBeforeTest.HasValue)
            {
                Settings.Instance.VisualizeChromaGradients = visualizeGradientsBeforeTest.Value;
                visualizeGradientsBeforeTest = null;
            }

            // Restore Chroma Lite independently because the reported pink node's applied material state requires it.
            if (emulateChromaLiteBeforeTest.HasValue)
            {
                Settings.Instance.EmulateChromaLite = emulateChromaLiteBeforeTest.Value;
                emulateChromaLiteBeforeTest = null;
            }

            if (chunkDistanceBeforeTest.HasValue)
            {
                Settings.Instance.ChunkDistance = chunkDistanceBeforeTest.Value;
                chunkDistanceBeforeTest = null;
            }

            if (gridMeasureSnappingBeforeTest.HasValue && atsc != null)
            {
                // Restore the user's shared precision after the deterministic 1/32 physical-scroll reproduction.
                atsc.GridMeasureSnapping = gridMeasureSnappingBeforeTest.Value;
                gridMeasureSnappingBeforeTest = null;
            }

            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            // Keep TestBase's subsequent singleton reset aligned with the managers created by this replacement scene.
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private static JSONNode LoadReportedDifficulty()
        {
            // Preserve the real map's 144-to-145 BPM conversion at a 700 BPM scale so beat 610 remains inside the
            // shared 60-second test clip: 144 * (700 / 145) before beat 6, then the 700 BPM base rate afterward.
            var fixturePath = Path.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "BasicEventChunkingReportedBeat528.json");
            var difficulty = JSON.Parse(File.ReadAllText(fixturePath));
            difficulty["bpmEvents"] = JSON.Parse(
                "[{\"b\":0,\"m\":695.1724},{\"b\":6,\"m\":700}]");
            return difficulty;
        }

        private IEnumerator ScrubAndAssertEveryNormalLaneVisual(
            float[] jsonTimes,
            string route)
        {
            // Validate after each independent production LateUpdate rather than only at the destination, because the
            // reported disappearance occurs on one backward reload while surrounding chunks continue to look correct.
            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            for (var timeIndex = 0; timeIndex < jsonTimes.Length; timeIndex++)
            {
                MoveToSnappedJsonTime(
                    atsc,
                    jsonTimes[timeIndex],
                    $"{route} at requested JSON beat {jsonTimes[timeIndex]}");
                yield return null;
                AssertEveryNormalLaneVisualInCurrentWindow(
                    $"{route} at snapped JSON beat {atsc.CurrentJsonTime}");
            }
        }

        private IEnumerator ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
            float[] songBpmTimes,
            string route)
        {
            // Route construction uses production chunk-space boundaries directly; convert each stop back through the
            // map's BPM timeline so AudioTimeSyncController still receives the same JSON-time API used by the editor.
            var map = BeatSaberSongContainer.Instance.Map;
            var jsonTimes = new float[songBpmTimes.Length];
            for (var timeIndex = 0; timeIndex < songBpmTimes.Length; timeIndex++)
            {
                var jsonTime = map.SongBpmTimeToJsonTime(songBpmTimes[timeIndex]);
                Assert.That(
                    jsonTime.HasValue,
                    Is.True,
                    $"The fixture could not convert song beat {songBpmTimes[timeIndex]} for route {route}.");
                jsonTimes[timeIndex] = jsonTime.Value;
            }

            yield return ScrubAndAssertEveryNormalLaneVisual(
                jsonTimes,
                route);
        }

        private IEnumerator ScrubSameFrameAndAssertEveryNormalLaneVisual(ScrubRoute route)
        {
            // Convert and dispatch every pulse before yielding so only the route's final chunk is observed by collection
            // LateUpdate, matching rapid wheel reversals that can otherwise skip intermediate refreshes.
            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            var map = BeatSaberSongContainer.Instance.Map;
            for (var timeIndex = 0; timeIndex < route.SongBpmTimes.Length; timeIndex++)
            {
                var jsonTime = map.SongBpmTimeToJsonTime(route.SongBpmTimes[timeIndex]);
                Assert.That(
                    jsonTime.HasValue,
                    Is.True,
                    $"The fixture could not convert same-frame song beat {route.SongBpmTimes[timeIndex]}.");
                MoveToSnappedJsonTime(
                    atsc,
                    jsonTime.Value,
                    $"during same-frame combinatorix route {route.Name}");
            }

            yield return null;
            AssertEveryNormalLaneVisualInCurrentWindow(
                $"after same-frame combinatorix route {route.Name}");
        }

        private IEnumerator RunPlaybackScrubRoute(PlaybackScrubRoute route)
        {
            yield return ScrubAndAssertEveryNormalLaneVisualAtSongTimes(
                route.BeforePlaybackSongBpmTimes,
                $"before playback route {route.Name}");

            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(atsc.IsPlaying, Is.False, $"Playback route {route.Name} did not start stopped.");
            StartDeterministicPlaybackAtSongBpmTime(atsc, route.PlayToSongBpmTime);

            // Run both production lifecycle frames without relying on AudioSource.time.
            yield return null;
            yield return null;
            Assert.That(atsc.IsPlaying, Is.True, $"Playback route {route.Name} stopped unexpectedly.");
            Assert.That(
                atsc.CurrentSongBpmTime,
                Is.GreaterThanOrEqualTo(route.PlayToSongBpmTime - 0.75f),
                $"Playback route {route.Name} did not reach its requested callback-unload region.");

            PauseDeterministicPlayback(atsc);
            Assert.That(atsc.IsPlaying, Is.False, $"Playback route {route.Name} did not pause.");

            if (route.ReplayToSongBpmTime.HasValue)
            {
                // Replaying before a frame elapses stresses the pause RefreshPool followed immediately by another
                // callback-driven unload, matching rapid Play/Pause input interspersed with scrub reversals.
                StartDeterministicPlaybackAtSongBpmTime(atsc, route.ReplayToSongBpmTime.Value);
                yield return null;
                yield return null;
                Assert.That(atsc.IsPlaying, Is.True, $"Replay route {route.Name} stopped unexpectedly.");
                PauseDeterministicPlayback(atsc);
                Assert.That(atsc.IsPlaying, Is.False, $"Replay route {route.Name} did not pause.");
            }

            if (route.WaitOneFrameAfterPause)
            {
                // Retain one control route where LateUpdate sees the paused playback position before the next scrub;
                // this distinguishes pause rebuilding itself from the immediate pause-and-scrub handoff.
                yield return null;
                AssertEveryNormalLaneVisualInCurrentWindow(
                    $"after the settled pause in playback route {route.Name}");
            }

            // Most routes queue the complete post-pause reversal before the first LateUpdate. This is the key path the
            // earlier stopped-only combinatorix missed: OnPlayToggle(false) refreshes once, then the scrub changes time.
            var map = BeatSaberSongContainer.Instance.Map;
            for (var timeIndex = 0; timeIndex < route.AfterPauseSongBpmTimes.Length; timeIndex++)
            {
                var jsonTime = map.SongBpmTimeToJsonTime(route.AfterPauseSongBpmTimes[timeIndex]);
                Assert.That(
                    jsonTime.HasValue,
                    Is.True,
                    $"The fixture could not convert post-pause song beat {route.AfterPauseSongBpmTimes[timeIndex]}.");
                MoveToSnappedJsonTime(
                    atsc,
                    jsonTime.Value,
                    $"after pausing playback route {route.Name}");
            }

            yield return null;
            AssertEveryNormalLaneVisualInCurrentWindow(
                $"after playback/pause/scrub route {route.Name}");
        }

        private static void MoveToSnappedJsonTime(
            AudioTimeSyncController atsc,
            float requestedJsonTime,
            string operation)
        {
            // The reported node disappears during ordinary snapped scrolling. Direct arbitrary MoveToJsonTime calls
            // broke that invariant after BPM conversion, so every synthetic wheel stop now follows the editor's own
            // MoveToJsonTime-then-SnapToGrid path and proves it landed on the requested 1/32 lane.
            atsc.MoveToJsonTime(requestedJsonTime);
            atsc.SnapToGrid(true);
            var originJsonTime = atsc.VisualBeatOriginJsonTime;
            var expectedJsonTime = (float)Math.Round(
                    (requestedJsonTime - originJsonTime) * atsc.GridMeasureSnapping,
                    MidpointRounding.AwayFromZero)
                / atsc.GridMeasureSnapping;
            expectedJsonTime += originJsonTime;
            Assert.That(
                atsc.CurrentJsonTime,
                Is.EqualTo(expectedJsonTime).Within(0.000001f),
                $"The playhead did not remain aligned to the editor precision grid {operation}.");
            var precisionGridPosition = (atsc.CurrentJsonTime - originJsonTime) * atsc.GridMeasureSnapping;
            Assert.That(
                precisionGridPosition,
                Is.EqualTo(Mathf.Round(precisionGridPosition)).Within(0.00001f),
                $"The playhead drifted between precision-grid lines {operation}.");
        }

        private void AssertEveryNormalLaneVisualInCurrentWindow(string operation)
        {
            var eventsContainer = GetEventsContainer();
            Assert.That(
                eventsContainer.PropagationEditing,
                Is.EqualTo(EventGridContainer.PropMode.Off),
                "The dense-map reproduction left the ordinary Basic Event lanes.");

            // Mirror the stopped-time production bounds and inspect every authoritative point node inside them; loaded
            // ribbon sources outside the lower bound are validated separately and do not distort the point-node count.
            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            var nearestChunk = (int)Math.Round(
                atsc.CurrentSongBpmTime / BeatmapObjectContainerCollection.ChunkSize,
                MidpointRounding.AwayFromZero);
            var chunks = Mathf.RoundToInt(Settings.Instance.ChunkDistance / 2);
            var lowerBound = (nearestChunk - chunks) * BeatmapObjectContainerCollection.ChunkSize;
            var upperBound = (nearestChunk + chunks) * BeatmapObjectContainerCollection.ChunkSize;
            orderedPoolObjects.Clear();
            for (var orderedIndex = 0; orderedIndex < eventsContainer.ObjectsWithContainers.Count; orderedIndex++)
            {
                orderedPoolObjects.Add(eventsContainer.ObjectsWithContainers[orderedIndex]);
            }

            boundWindowContainers.Clear();
            var labels = UnityEngine.Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var expectedEventCount = 0;
            for (var eventIndex = 0; eventIndex < eventsContainer.MapObjects.Count; eventIndex++)
            {
                var evt = eventsContainer.MapObjects[eventIndex];
                if (evt.SongBpmTime < lowerBound || evt.SongBpmTime > upperBound)
                {
                    continue;
                }

                expectedEventCount++;
                var eventOperation = $"{operation} (event type {evt.Type} at JSON beat {evt.JsonTime})";
                var expectedPosition = evt.GetPosition(
                    labels,
                    eventsContainer.PropagationEditing,
                    eventsContainer.EventTypeToPropagate);
                var hasValidVisual = eventsContainer.LoadedContainers.TryGetValue(evt, out var objectContainer)
                    && objectContainer is EventContainer eventContainer
                    && eventContainer.EventData == evt
                    && orderedPoolObjects.Contains(evt)
                    && evt.HasAttachedContainer
                    && eventContainer.gameObject.activeInHierarchy
                    && expectedPosition.HasValue
                    && Mathf.Abs(eventContainer.transform.localPosition.x - expectedPosition.Value.x) <= 0.001f
                    && Mathf.Abs(
                        eventContainer.transform.localPosition.z
                        - (evt.SongBpmTime * EditorScaleController.EditorScale)) <= 0.001f
                    && eventContainer.transform.lossyScale.sqrMagnitude > 0f
                    && eventContainer.VModelController.Actives.Any(model => model.GameObject.activeInHierarchy)
                    && FindVisibleRenderer(eventContainer) != null;
                if (!hasValidVisual)
                {
                    // Preserve the shared assertion's detailed pool diagnostics only on the exceptional path.
                    AssertNodeVisualLoaded(evt, eventOperation);
                }

                var typedContainer = (EventContainer)objectContainer;
                var currentVisual = CaptureNodeVisual(typedContainer, FindVisibleRenderer(typedContainer));
                if (currentVisual.MainAlpha <= 0.001f || currentVisual.ForceRenderingOff)
                {
                    // A renderer can satisfy ownership and bounds while still being intentionally invisible.
                    AssertNodeVisualLoaded(evt, eventOperation);
                    Assert.That(
                        currentVisual.ForceRenderingOff,
                        Is.False,
                        $"The in-window renderer was forced off {eventOperation}.");
                }

                if (!boundWindowContainers.Add(objectContainer))
                {
                    Assert.Fail($"Two in-window events shared one pooled visual container {eventOperation}.");
                }
            }
            Assert.That(expectedEventCount, Is.GreaterThan(0), $"No fixture events occupied the visual window {operation}.");

            // A transition ribbon is mapper-visible whenever its source-to-effective-destination interval overlaps the
            // current point window, including sources retained before the lower bound by production interval lookup.
            for (var sourceIndex = 0; sourceIndex < eventsContainer.MapObjects.Count; sourceIndex++)
            {
                var source = eventsContainer.MapObjects[sourceIndex];
                if (eventsContainer.BeatmapContext.TrackDefinitions.GetBasicOrDefault(source.Type).Kind
                        != BasicEventKind.Lights
                    || source.IsFade
                    || source.IsFlash)
                {
                    continue;
                }

                var transition = eventsContainer.GetEffectiveNextLightEvent(source);
                if (transition == null
                    || !transition.IsTransition
                    || source.SongBpmTime > upperBound
                    || transition.SongBpmTime < lowerBound)
                {
                    continue;
                }

                AssertVisibleRibbon(
                    source,
                    transition,
                    $"{operation} (ribbon {source.JsonTime} -> {transition.JsonTime})");
            }
        }

        private static ScrubRoute[] BuildScrubCombinatorix(float centralStartSongBpmTime)
        {
            // The former 26-route matrix repeated equivalent crossings at eight unrelated chunk midpoints. Preserve
            // both directions at the reported node's unload boundary plus one far zigzag that churns the entire pool.
            return new[]
            {
                new ScrubRoute(
                    "forward-then-backward-across-reported-boundary",
                    new[]
                    {
                        530.25f, 532.49f, 532.5f, 532.51f,
                        534.75f, 532.51f, 532.49f, centralStartSongBpmTime
                    }),
                new ScrubRoute(
                    "backward-then-forward-across-reported-boundary",
                    new[]
                    {
                        534.75f, 532.51f, 532.5f, 532.49f,
                        530.25f, 532.49f, 532.51f, centralStartSongBpmTime
                    }),
                new ScrubRoute(
                    "full-range-forward-backward-zigzag",
                    new[] { 452.1f, 607.9f, 457.4f, 602.6f, 487.1f, 573.2f, centralStartSongBpmTime })
            };
        }

        private static ScrubRoute[] BuildSameFrameScrubCombinatorix(float centralStartSongBpmTime)
        {
            // Keep one far alternating burst and one repeated local boundary burst; the removed routes reached the same
            // final chunks through different intermediate values that LateUpdate never observes.
            return new[]
            {
                new ScrubRoute(
                    "full-range-alternating-ending-center",
                    new[] { 451.3f, 608.2f, 453.7f, 606.1f, 468.4f, 592.7f, centralStartSongBpmTime }),
                new ScrubRoute(
                    "repeat-reported-midpoint-ending-center",
                    new[] { 532.51f, 532.49f, 532.51f, 532.49f, 532.51f, centralStartSongBpmTime })
            };
        }

        private static PlaybackScrubRoute[] BuildPlaybackScrubCombinatorix(float centralStartSongBpmTime)
        {
            // Retain the two distinct lifecycle orderings: a settled pause and an immediate pause/replay/pause reversal.
            // Other removed routes varied only distances already covered by the full-range stopped and same-frame cases.
            return new[]
            {
                new PlaybackScrubRoute(
                    "far-forward-play-settle-then-backward",
                    new[] { 452.3f, 581.7f, 500.2f },
                    548.4f,
                    new[] { 541.2f, 532.49f, centralStartSongBpmTime },
                    true),
                new PlaybackScrubRoute(
                    "play-pause-replay-pause-backward",
                    new[] { 542.7f, 476.8f, 505.3f },
                    535.4f,
                    new[] { 570.1f, 548.6f, 532.49f, centralStartSongBpmTime },
                    replayToSongBpmTime: 592.2f)
            };
        }

        private NodeVisualSnapshot CaptureNodeVisual(BaseEvent evt)
        {
            // Read the property block from the actual active renderer, not the controller's desired block, because the
            // reported symptom is a live light with a grid node whose pooled renderer remains visually absent.
            var eventsContainer = GetEventsContainer();
            var eventContainer = (EventContainer)eventsContainer.LoadedContainers[evt];
            return CaptureNodeVisual(eventContainer, FindVisibleRenderer(eventContainer));
        }

        private NodeVisualSnapshot CaptureNodeVisual(EventContainer eventContainer, Renderer renderer)
        {
            // The caller proves the renderer exists before entering this hot assertion path; retain an explicit guard so
            // a future caller still receives a useful fixture failure instead of a null-reference exception.
            if (renderer == null)
            {
                Assert.Fail("The event container had no visible renderer to snapshot.");
            }
            rendererProperties.Clear();
            renderer.GetPropertyBlock(rendererProperties);
            return new NodeVisualSnapshot(
                rendererProperties.GetColor(colorAId),
                rendererProperties.GetColor(colorBId),
                rendererProperties.GetFloat(mainAlphaId),
                rendererProperties.GetFloat(fadeSizeId),
                eventContainer.transform.lossyScale,
                renderer.bounds.size,
                eventContainer.VModelController.Actives[0].Name,
                renderer.forceRenderingOff);
        }

        private static Renderer FindVisibleRenderer(EventContainer eventContainer)
        {
            // Avoid a LINQ iterator for the per-node/per-stop renderer lookup in the dense combinatorix.
            for (var rendererIndex = 0;
                 rendererIndex < eventContainer.VModelController.Renderers.Count;
                 rendererIndex++)
            {
                var renderer = eventContainer.VModelController.Renderers[rendererIndex];
                if (renderer != null
                    && renderer.enabled
                    && renderer.gameObject.activeInHierarchy
                    && renderer.bounds.size.sqrMagnitude > 0f)
                {
                    return renderer;
                }
            }

            return null;
        }

        private static void AssertNodeVisualMatches(
            NodeVisualSnapshot expected,
            NodeVisualSnapshot actual,
            string operation)
        {
            // The pre-unload snapshot is the production appearance oracle for this exact authored event; matching it
            // catches stale pooled color, alpha, model, scale, and force-rendering state without duplicating appearance code.
            if (!ColorsAreWithin(expected.ColorA, actual.ColorA))
            {
                Assert.Fail(
                    $"The reported node's ColorA changed {operation}. Expected {expected.ColorA}, but rendered {actual.ColorA}.");
            }
            if (!ColorsAreWithin(expected.ColorB, actual.ColorB))
            {
                Assert.Fail(
                    $"The reported node's ColorB changed {operation}. Expected {expected.ColorB}, but rendered {actual.ColorB}.");
            }
            if (Mathf.Abs(actual.MainAlpha - expected.MainAlpha) > 0.001f)
            {
                Assert.Fail(
                    $"The reported node's applied alpha changed {operation}. Expected {expected.MainAlpha}, but was {actual.MainAlpha}.");
            }
            if (Mathf.Abs(actual.FadeSize - expected.FadeSize) > 0.001f)
            {
                Assert.Fail(
                    $"The reported node's applied fade changed {operation}. Expected {expected.FadeSize}, but was {actual.FadeSize}.");
            }
            if (Vector3.Distance(actual.Scale, expected.Scale) > 0.001f)
            {
                Assert.Fail(
                    $"The reported node's rendered scale changed {operation}. Expected {expected.Scale}, but was {actual.Scale}.");
            }
            if (Vector3.Distance(actual.BoundsSize, expected.BoundsSize) > 0.001f)
            {
                Assert.Fail(
                    $"The reported node's renderer bounds changed {operation}. Expected {expected.BoundsSize}, but was {actual.BoundsSize}.");
            }
            if (actual.ModelName != expected.ModelName)
            {
                Assert.Fail(
                    $"The reported node reloaded with the wrong visual model {operation}. Expected {expected.ModelName}, but was {actual.ModelName}.");
            }
            if (actual.ForceRenderingOff)
            {
                Assert.Fail($"The reported node renderer was forced off {operation}.");
            }
        }

        private static bool ColorsAreWithin(Color expected, Color actual)
        {
            // Shader colors can round-trip through Unity property blocks with tiny platform-specific differences.
            return Mathf.Abs(expected.r - actual.r) <= 0.001f
                && Mathf.Abs(expected.g - actual.g) <= 0.001f
                && Mathf.Abs(expected.b - actual.b) <= 0.001f
                && Mathf.Abs(expected.a - actual.a) <= 0.001f;
        }

        private readonly struct NodeVisualSnapshot
        {
            // Keep only mapper-visible state that must survive a pooled unload/reload of the same authored event.
            public NodeVisualSnapshot(
                Color colorA,
                Color colorB,
                float mainAlpha,
                float fadeSize,
                Vector3 scale,
                Vector3 boundsSize,
                string modelName,
                bool forceRenderingOff)
            {
                ColorA = colorA;
                ColorB = colorB;
                MainAlpha = mainAlpha;
                FadeSize = fadeSize;
                Scale = scale;
                BoundsSize = boundsSize;
                ModelName = modelName;
                ForceRenderingOff = forceRenderingOff;
            }

            public Color ColorA { get; }
            public Color ColorB { get; }
            public float MainAlpha { get; }
            public float FadeSize { get; }
            public Vector3 Scale { get; }
            public Vector3 BoundsSize { get; }
            public string ModelName { get; }
            public bool ForceRenderingOff { get; }
        }

        private readonly struct ScrubRoute
        {
            // Name every retained route so a failure identifies the exact directional history and stop sequence.
            public ScrubRoute(string name, float[] songBpmTimes)
            {
                Name = name;
                SongBpmTimes = songBpmTimes;
            }

            public string Name { get; }
            public float[] SongBpmTimes { get; }
        }

        private readonly struct PlaybackScrubRoute
        {
            // Preserve each deterministic playback history as data so expanding boundary coverage does not duplicate
            // the production play/pause/scrub driver or accidentally vary its frame timing between cases.
            public PlaybackScrubRoute(
                string name,
                float[] beforePlaybackSongBpmTimes,
                float playToSongBpmTime,
                float[] afterPauseSongBpmTimes,
                bool waitOneFrameAfterPause = false,
                float? replayToSongBpmTime = null)
            {
                Name = name;
                BeforePlaybackSongBpmTimes = beforePlaybackSongBpmTimes;
                PlayToSongBpmTime = playToSongBpmTime;
                AfterPauseSongBpmTimes = afterPauseSongBpmTimes;
                WaitOneFrameAfterPause = waitOneFrameAfterPause;
                ReplayToSongBpmTime = replayToSongBpmTime;
            }

            public string Name { get; }
            public float[] BeforePlaybackSongBpmTimes { get; }
            public float PlayToSongBpmTime { get; }
            public float[] AfterPauseSongBpmTimes { get; }
            public bool WaitOneFrameAfterPause { get; }
            public float? ReplayToSongBpmTime { get; }
        }
    }
}

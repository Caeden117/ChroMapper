using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // The reported missing Basic Event node is a visual-pool regression, so these tests drive public playhead moves
    // and validate ordinary nodes and transition ribbons together at every relevant chunk edge.
    public class BasicEventNodeChunkingTest : BasicEventChunkingTestBase
    {
        private const float BoundaryOffset = 0.05f;

        private bool? visualizeGradientsBeforeTest;
        private int? chunkDistanceBeforeTest;
        private EventGridContainer.PropMode? propagationEditingBeforeTest;
        private int? propagatedEventTypeBeforeTest;
        // Physical scrub regressions isolate Timeline and mouse-position routing while preserving every shared input map.
        private CMInput physicalTimelineInput;
        private bool? sharedTimelineInputWasEnabled;
        private bool? sharedUtilsInputWasEnabled;
        private bool? invertScrollTimeBeforeTest;
        private int? gridSnappingBeforeTest;
        private float? songBpmBeforeTest;
        private Vector2 physicalScrollScreenPosition;

        protected override void BeforeCleanup()
        {
            // Physical-wheel chunk tests isolate the Timeline action map; release it before cleanup moves or deletes
            // any event visuals so an input callback cannot outlive the test that installed it.
            DisposePhysicalTimelineInput();

            // A failed playback/scrub assertion must not leave callback-driven event recycling active during teardown.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null && atsc.IsPlaying)
            {
                atsc.CancelPlaying();
            }
        }

        protected override void AfterCleanup()
        {
            var eventsContainer = GetEventsContainer();
            // Restore shared editor settings after the matrix narrows the visual window and enables ribbon rendering.
            if (propagationEditingBeforeTest.HasValue)
            {
                // Restore the propagated event type before rebuilding its former lane labels and visibility.
                eventsContainer.EventTypeToPropagate = propagatedEventTypeBeforeTest.Value;
                eventsContainer.PropagationEditing = propagationEditingBeforeTest.Value;
                propagationEditingBeforeTest = null;
                propagatedEventTypeBeforeTest = null;
            }

            if (visualizeGradientsBeforeTest.HasValue)
            {
                Settings.Instance.VisualizeChromaGradients = visualizeGradientsBeforeTest.Value;
                visualizeGradientsBeforeTest = null;
            }

            if (chunkDistanceBeforeTest.HasValue)
            {
                Settings.Instance.ChunkDistance = chunkDistanceBeforeTest.Value;
                chunkDistanceBeforeTest = null;
            }

            // Restore the user's scroll direction and snapping after physical-wheel tests use half-beat pulses.
            if (invertScrollTimeBeforeTest.HasValue)
            {
                Settings.Instance.InvertScrollTime = invertScrollTimeBeforeTest.Value;
                invertScrollTimeBeforeTest = null;
            }

            if (gridSnappingBeforeTest.HasValue)
            {
                Object.FindAnyObjectByType<AudioTimeSyncController>().GridMeasureSnapping = gridSnappingBeforeTest.Value;
                gridSnappingBeforeTest = null;
            }

            // Restore the shared test map's timing basis after high-beat coverage mirrors the reported beat-528 area.
            if (songBpmBeforeTest.HasValue)
            {
                BeatSaberSongContainer.Instance.Info.BeatsPerMinute = songBpmBeforeTest.Value;
                BeatSaberSongContainer.Instance.Map.ValidateBpmEventsAndObjectTimes(songBpmBeforeTest.Value);
                songBpmBeforeTest = null;
            }

            // Cleanup removed the matrix events first; rebuild the normal pool once with the restored shared settings.
            eventsContainer.RefreshPool(true);
        }

        [UnityTest]
        public IEnumerator ScrubForwardStoppingJustBeforeChunkBoundaryLoadsNodeAndRibbonBoundaryMatrix()
        {
            // A stop immediately before beat 37.5 must retain the beat-35 chunk center and its [30, 40] visual range.
            yield return AssertBoundaryMatrix(
                new[] { 1.15f, 13.4f, 29.75f, 37.49f },
                30f,
                40f,
                "after forward scrubbing to just before the chunk boundary");
        }

        [UnityTest]
        public IEnumerator ScrubForwardStoppingOnChunkBoundaryLoadsNodeAndRibbonBoundaryMatrix()
        {
            // The exact midpoint uses away-from-zero rounding and must switch to the beat-40 chunk's [35, 45] range.
            yield return AssertBoundaryMatrix(
                new[] { 2.35f, 17.8f, 31.2f, 37.5f },
                35f,
                45f,
                "after forward scrubbing exactly onto the chunk boundary");
        }

        [UnityTest]
        public IEnumerator ScrubForwardStoppingJustAfterChunkBoundaryLoadsNodeAndRibbonBoundaryMatrix()
        {
            // An arbitrary multi-chunk forward jump just beyond beat 37.5 must produce the same [35, 45] range.
            yield return AssertBoundaryMatrix(
                new[] { 4.6f, 9.85f, 26.3f, 37.51f },
                35f,
                45f,
                "after forward scrubbing to just after the chunk boundary");
        }

        [UnityTest]
        public IEnumerator ScrubBackwardStoppingJustBeforeChunkBoundaryLoadsNodeAndRibbonBoundaryMatrix()
        {
            // Backward traversal must choose the same [30, 40] range as forward traversal just below beat 37.5.
            yield return AssertBoundaryMatrix(
                new[] { 88.4f, 67.15f, 49.6f, 37.49f },
                30f,
                40f,
                "after backward scrubbing to just before the chunk boundary");
        }

        [UnityTest]
        public IEnumerator ScrubBackwardStoppingOnChunkBoundaryLoadsNodeAndRibbonBoundaryMatrix()
        {
            // Landing exactly on the midpoint from ahead must not leave the former forward chunk's pool contents stale.
            yield return AssertBoundaryMatrix(
                new[] { 91.2f, 72.45f, 53.7f, 37.5f },
                35f,
                45f,
                "after backward scrubbing exactly onto the chunk boundary");
        }

        [UnityTest]
        public IEnumerator ScrubBackwardStoppingJustAfterChunkBoundaryLoadsNodeAndRibbonBoundaryMatrix()
        {
            // A backward stop barely above the midpoint must reload every [35, 45] node regardless of jump distance.
            yield return AssertBoundaryMatrix(
                new[] { 86.75f, 64.1f, 46.9f, 37.51f },
                35f,
                45f,
                "after backward scrubbing to just after the chunk boundary");
        }

        [UnityTest]
        public IEnumerator ScrubForwardUntilNodesUnloadThenImmediatelyBackwardReloadsNodesAndRibbon()
        {
            // Reproduce the screenshot sequence: unload a center node by moving ahead, then immediately reverse far
            // enough that nodes behind and ahead of it share its ordinary loaded range again.
            PrepareChunkingScenario();
            var behind = PlaceLightEvent(39.95f, LightValue.RedOn, EventTypeValue.Event2);
            var reportedMissing = PlaceLightEvent(40f, LightValue.BlueOn, EventTypeValue.Event2);
            var ahead = PlaceLightEvent(40.05f, LightValue.WhiteOn, EventTypeValue.Event2);
            var ribbonSource = PlaceLightEvent(39f, LightValue.RedOn, EventTypeValue.Event0);
            var ribbonTarget = PlaceLightEvent(41f, LightValue.BlueTransition, EventTypeValue.Event0);

            yield return ScrubThroughJsonTimes(18.35f, 40f);
            AssertReloadedCluster(behind, reportedMissing, ahead, ribbonSource, ribbonTarget, "before unloading");

            // Follow the ordinary one-beat scroll cadence through every intervening chunk until the cluster unloads.
            yield return ScrubThroughJsonTimes(41f, 42f, 43f, 44f, 45f, 46f, 47f, 48f);
            AssertNodeVisualUnloaded(behind, "after scrubbing forward past the cluster");
            AssertNodeVisualUnloaded(reportedMissing, "after scrubbing forward past the cluster");
            AssertNodeVisualUnloaded(ahead, "after scrubbing forward past the cluster");
            AssertNodeVisualUnloaded(ribbonSource, "after scrubbing beyond the completed ribbon");
            AssertNodeVisualUnloaded(ribbonTarget, "after scrubbing beyond the completed ribbon");

            // Immediately reverse the same incremental scrub path instead of repairing the pool with a direct refresh.
            yield return ScrubThroughJsonTimes(47f, 46f, 45f, 44f, 43f, 42f);
            AssertReloadedCluster(
                behind,
                reportedMissing,
                ahead,
                ribbonSource,
                ribbonTarget,
                "after immediately scrubbing backward into the cluster");
            // The screenshot failure persists rather than flickering for one frame, so require the complete visual
            // cluster to remain correct after any deferred pool/coroutine work has had several frames to run.
            for (var stableFrame = 1; stableFrame <= 4; stableFrame++)
            {
                yield return null;
                AssertReloadedCluster(
                    behind,
                    reportedMissing,
                    ahead,
                    ribbonSource,
                    ribbonTarget,
                    $"on stable frame {stableFrame} after immediately scrubbing backward");
            }
        }

        [UnityTest]
        public IEnumerator LightIdViewScrubForwardThenBackwardReloadsScopedNodesAndRibbon()
        {
            // Reproduce the screenshot in the physical light-ID lane rather than relying on ordinary event-type view.
            const EventTypeValue eventType = EventTypeValue.Event0;
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var lightId = labels.LaneToLightID((int)eventType, 0);
            Assert.That(lightId, Is.GreaterThanOrEqualTo(0), "The test environment has no scoped light-ID lane.");

            yield return AssertLightIdViewReloadsCluster(eventType, new[] { lightId }, "the first scoped light-ID lane");
        }

        [UnityTest]
        public IEnumerator LightIdViewScrubForwardThenBackwardReloadsAllLightsNodesAndRibbon()
        {
            // The synthetic All Lights lane has no custom ID and must obey the same chunk reload lifecycle.
            yield return AssertLightIdViewReloadsCluster(EventTypeValue.Event0, null, "the All Lights lane");
        }

        [UnityTest]
        public IEnumerator RepeatedArbitraryForwardAndBackwardScrubsReloadNodesAndRibbonEveryTime()
        {
            // The observed failure is intermittent, so cross multiple distant chunk buckets and revisit the same
            // node/ribbon cluster repeatedly instead of proving only one favorable unload/reload cycle.
            PrepareChunkingScenario();
            var behind = PlaceLightEvent(39.95f, LightValue.RedOn, EventTypeValue.Event2);
            var reportedMissing = PlaceLightEvent(40f, LightValue.BlueOn, EventTypeValue.Event2);
            var ahead = PlaceLightEvent(40.05f, LightValue.WhiteOn, EventTypeValue.Event2);
            var ribbonSource = PlaceLightEvent(39f, LightValue.RedOn, EventTypeValue.Event0);
            var ribbonTarget = PlaceLightEvent(41f, LightValue.BlueTransition, EventTypeValue.Event0);
            var distantStops = new[] { 47.51f, 68.35f, 52.63f, 83.2f, 57.51f };

            yield return ScrubThroughJsonTimes(21.7f, 42.49f);
            for (var cycle = 0; cycle < distantStops.Length; cycle++)
            {
                yield return ScrubThroughJsonTimes(distantStops[cycle]);
                AssertNodeVisualUnloaded(reportedMissing, $"after distant scrub cycle {cycle + 1}");

                yield return ScrubThroughJsonTimes(42.49f);
                AssertReloadedCluster(
                    behind,
                    reportedMissing,
                    ahead,
                    ribbonSource,
                    ribbonTarget,
                    $"after returning from distant scrub cycle {cycle + 1}");
            }
        }

        [UnityTest]
        public IEnumerator PhysicalWheelBackwardScrubReloadsDenseSameTimeBoundaryNodesAndRibbon()
        {
            // The real map has thousands of Basic Events and several event types at the same beat; land that exact
            // group on the lower chunk bound through the Timeline wheel action instead of calling RefreshPool directly.
            const EventTypeValue selectedEventType = EventTypeValue.Event1;
            PrepareChunkingScenario(EventGridContainer.PropMode.Light, selectedEventType);
            PlaceDenseNeighborEvents(30f, 55f, selectedEventType);

            var target = PlaceLightEvent(40f, LightValue.RedOn, selectedEventType);
            var sameTimeEvent0 = PlaceLightEvent(40f, LightValue.BlueOn, EventTypeValue.Event0);
            var sameTimeEvent2 = PlaceLightEvent(40f, LightValue.WhiteOn, EventTypeValue.Event2);
            var sameTimeEvent3 = PlaceLightEvent(40f, LightValue.RedOn, EventTypeValue.Event3);
            var sameTimeEvent4 = PlaceLightEvent(40f, LightValue.BlueOn, EventTypeValue.Event4);
            var transition = PlaceLightEvent(41f, LightValue.BlueTransition, selectedEventType);

            yield return ScrubThroughJsonTimes(40f);
            AssertNodeVisualLoaded(target, "before the physical forward scrub");
            AssertVisibleRibbon(target, transition, "before the physical forward scrub");
            AssertSameTimeEventsOwned(
                new[] { target, sameTimeEvent0, sameTimeEvent2, sameTimeEvent3, sameTimeEvent4 },
                "before the physical forward scrub");
            PreparePhysicalTimelineInput();
            yield return null;

            // Half-beat wheel pulses cross every real chunk boundary before proving the target truly left the pool.
            yield return PhysicalWheelScrub(1, 16, true);
            Assert.That(
                Object.FindAnyObjectByType<AudioTimeSyncController>().CurrentJsonTime,
                Is.EqualTo(48f).Within(0.001f),
                "The physical forward wheel route did not reach its unload point.");
            AssertNodeVisualUnloaded(target, "after the physical forward scrub unloaded its chunk");

            // Beat 42.5 rounds to chunk 45, whose lower inclusive bound is exactly beat 40. This is the path that can
            // load neighboring members of a same-time group while leaving the mapper's visible node absent.
            yield return PhysicalWheelScrub(-1, 11, true);
            AssertPhysicalBackwardReloadRemainsStable(
                target,
                transition,
                new[] { target, sameTimeEvent0, sameTimeEvent2, sameTimeEvent3, sameTimeEvent4 },
                "after physical backward wheel scrubbing onto the same-time lower boundary");
            for (var frame = 1; frame <= 4; frame++)
            {
                yield return null;
                AssertPhysicalBackwardReloadRemainsStable(
                    target,
                    transition,
                    new[] { target, sameTimeEvent0, sameTimeEvent2, sameTimeEvent3, sameTimeEvent4 },
                    $"on stable frame {frame} after physical backward wheel scrubbing");
            }
        }

        [UnityTest]
        public IEnumerator RapidPhysicalWheelReversalReloadsNodeAndRibbonBeforeLateUpdate()
        {
            // Multiple wheel reports can be processed in one rendered frame; unload first, then reverse across several
            // chunks without yielding so EventGridContainer sees the same timing as a fast physical scroll gesture.
            PrepareChunkingScenario(EventGridContainer.PropMode.Off);
            var beforeTarget = PlaceLightEvent(39.75f, LightValue.RedOn, EventTypeValue.Event2);
            var target = PlaceLightEvent(40f, LightValue.BlueOn, EventTypeValue.Event2);
            var afterTarget = PlaceLightEvent(40.25f, LightValue.WhiteOn, EventTypeValue.Event2);
            var ribbonSource = PlaceLightEvent(39.5f, LightValue.RedOn, EventTypeValue.Event1);
            var transition = PlaceLightEvent(41f, LightValue.BlueTransition, EventTypeValue.Event1);

            yield return ScrubThroughJsonTimes(40f);
            PreparePhysicalTimelineInput();
            yield return null;
            yield return PhysicalWheelScrub(1, 16, true);
            AssertNodeVisualUnloaded(beforeTarget, "before the rapid same-frame backward reversal");
            AssertNodeVisualUnloaded(target, "before the rapid same-frame backward reversal");
            AssertNodeVisualUnloaded(afterTarget, "before the rapid same-frame backward reversal");

            // Return to beat 42 so the ordinary nodes at 39.75, 40, and 40.25 are all inside [35, 45].
            for (var pulse = 0; pulse < 12; pulse++)
            {
                SendPhysicalWheelPulse(-1);
            }
            yield return null;

            AssertNodeVisualLoaded(beforeTarget, "after the rapid same-frame backward reversal");
            AssertNodeVisualLoaded(target, "after the rapid same-frame backward reversal");
            AssertNodeVisualLoaded(afterTarget, "after the rapid same-frame backward reversal");
            AssertVisibleRibbon(ribbonSource, transition, "after the rapid same-frame backward reversal");
            for (var frame = 1; frame <= 4; frame++)
            {
                yield return null;
                AssertNodeVisualLoaded(beforeTarget, $"on stable frame {frame} after the rapid same-frame reversal");
                AssertNodeVisualLoaded(target, $"on stable frame {frame} after the rapid same-frame reversal");
                AssertNodeVisualLoaded(afterTarget, $"on stable frame {frame} after the rapid same-frame reversal");
                AssertVisibleRibbon(ribbonSource, transition, $"on stable frame {frame} after the rapid same-frame reversal");
            }
        }

        [UnityTest]
        public IEnumerator RapidPhysicalWheelReversalInLightIdViewReloadsNodesAndRibbonBeforeLateUpdate()
        {
            // Retain the Light ID variant alongside the normal middle-lane regression: three nodes share one scoped
            // lane, and the node after the reported candidate owns the lane-local transition ribbon.
            const EventTypeValue eventType = EventTypeValue.Event0;
            PrepareChunkingScenario(EventGridContainer.PropMode.Light, eventType);
            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var lightId = labels.LaneToLightID((int)eventType, 0);
            Assert.That(lightId, Is.GreaterThanOrEqualTo(0), "The rapid Light ID test has no scoped event lane.");
            var lightIds = new[] { lightId };
            var beforeTarget = PlaceLightEvent(39.75f, LightValue.RedOn, eventType, lightIds);
            var target = PlaceLightEvent(40f, LightValue.BlueOn, eventType, lightIds);
            var afterTarget = PlaceLightEvent(40.25f, LightValue.WhiteOn, eventType, lightIds);
            var transition = PlaceLightEvent(41f, LightValue.BlueTransition, eventType, lightIds);

            yield return ScrubThroughJsonTimes(40f);
            PreparePhysicalTimelineInput();
            yield return null;
            yield return PhysicalWheelScrub(1, 16, true);
            AssertNodeVisualUnloaded(beforeTarget, "before the rapid Light ID backward reversal");
            AssertNodeVisualUnloaded(target, "before the rapid Light ID backward reversal");
            AssertNodeVisualUnloaded(afterTarget, "before the rapid Light ID backward reversal");

            // Return to beat 42 so the scoped nodes at 39.75, 40, and 40.25 are all inside [35, 45].
            for (var pulse = 0; pulse < 12; pulse++)
            {
                SendPhysicalWheelPulse(-1);
            }
            yield return null;

            // Check several later frames because a stale callback can remove one scoped node after the first reload.
            for (var stableFrame = 0; stableFrame <= 4; stableFrame++)
            {
                if (stableFrame > 0)
                {
                    yield return null;
                }

                var operation = stableFrame == 0
                    ? "after the rapid Light ID backward reversal"
                    : $"on stable frame {stableFrame} after the rapid Light ID backward reversal";
                AssertNodeVisualLoaded(beforeTarget, operation);
                AssertNodeVisualLoaded(target, operation);
                AssertNodeVisualLoaded(afterTarget, operation);
                AssertVisibleRibbon(afterTarget, transition, operation);
            }
        }

        [UnityTest]
        public IEnumerator PlaybackForwardThenImmediateBackwardWheelScrubReloadsNodesAndRibbon()
        {
            // The manual workaround plays forward from behind to restore the node, so exercise callback-driven playback
            // unloads before switching back to the stopped physical-wheel chunk path used when the failure is observed.
            const EventTypeValue selectedEventType = EventTypeValue.Event1;
            PrepareChunkingScenario(EventGridContainer.PropMode.Off, selectedEventType);
            var ribbonSource = PlaceLightEvent(40f, LightValue.RedOn, selectedEventType);
            var ribbonTarget = PlaceLightEvent(41f, LightValue.BlueTransition, selectedEventType);
            var beforeOrdinaryNode = PlaceLightEvent(39.75f, LightValue.RedOn, EventTypeValue.Event2);
            var ordinaryNode = PlaceLightEvent(40f, LightValue.BlueOn, EventTypeValue.Event2);
            var afterOrdinaryNode = PlaceLightEvent(40.25f, LightValue.WhiteOn, EventTypeValue.Event2);

            yield return ScrubThroughJsonTimes(40f);
            AssertNodeVisualLoaded(ribbonSource, "before playback moved ahead");
            AssertNodeVisualLoaded(beforeOrdinaryNode, "before playback moved ahead");
            AssertNodeVisualLoaded(ordinaryNode, "before playback moved ahead");
            AssertNodeVisualLoaded(afterOrdinaryNode, "before playback moved ahead");
            AssertVisibleRibbon(ribbonSource, ribbonTarget, "before playback moved ahead");
            PreparePhysicalTimelineInput();
            yield return null;

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            StartDeterministicPlaybackAtSongBpmTime(atsc, 48f);
            yield return null;
            yield return null;
            Assert.That(atsc.CurrentJsonTime, Is.GreaterThanOrEqualTo(47.5f), "Playback did not reach the unload region.");
            PauseDeterministicPlayback(atsc);
            yield return null;
            Assert.That(atsc.IsPlaying, Is.False, "Playback did not stop before the backward scrub.");
            AssertNodeVisualUnloaded(ribbonSource, "after playback stopped ahead of the cluster");
            AssertNodeVisualUnloaded(beforeOrdinaryNode, "after playback stopped ahead of the cluster");
            AssertNodeVisualUnloaded(ordinaryNode, "after playback stopped ahead of the cluster");
            AssertNodeVisualUnloaded(afterOrdinaryNode, "after playback stopped ahead of the cluster");

            // Return to the chunk whose lower bound contains the missing nodes using real wheel pulses immediately
            // after playback callback teardown, then detect any late callback that removes them again.
            var backwardPulseCount = Mathf.RoundToInt((atsc.CurrentJsonTime - 42f) * 2f);
            Assert.That(backwardPulseCount, Is.GreaterThan(0), "Playback stopped before the backward wheel route.");
            yield return PhysicalWheelScrub(-1, backwardPulseCount, true);
            for (var stableFrame = 0; stableFrame <= 4; stableFrame++)
            {
                if (stableFrame > 0)
                {
                    yield return null;
                }

                var operation = stableFrame == 0
                    ? "after stopping playback and immediately scrubbing backward"
                    : $"on stable frame {stableFrame} after the playback-to-backward-scrub transition";
                AssertNodeVisualLoaded(ribbonSource, operation);
                AssertNodeVisualLoaded(beforeOrdinaryNode, operation);
                AssertNodeVisualLoaded(ordinaryNode, operation);
                AssertNodeVisualLoaded(afterOrdinaryNode, operation);
                AssertVisibleRibbon(ribbonSource, ribbonTarget, operation);
            }
        }

        [UnityTest]
        public IEnumerator HighBeatBpmAdjustedBackwardScrubReloadsNodeAndRibbonNearReportedTime()
        {
            // The reported map's missing node is near JSON beat 528 after a small BPM change. Preserve that high-time
            // float conversion and its non-integral SongBpmTime instead of assuming the low-beat identity map is enough.
            const EventTypeValue selectedEventType = EventTypeValue.Event1;
            UseHighBeatTimeline();
            PrepareChunkingScenario(EventGridContainer.PropMode.Light, selectedEventType);
            PlaceUtils.Place(new BaseBpmEvent(6f, 604f));
            var behind = PlaceLightEvent(528.984f, LightValue.BlueOn, selectedEventType);
            var target = PlaceLightEvent(529f, LightValue.RedOn, selectedEventType);
            var sameTimeRingEvent = PlaceLightEvent(529f, LightValue.BlueOn, EventTypeValue.Event13);
            var ahead = PlaceLightEvent(529.484f, LightValue.BlueOn, selectedEventType);
            var transition = PlaceLightEvent(530.5f, LightValue.RedTransition, selectedEventType);

            yield return ScrubThroughJsonTimes(510.25f, 522.75f, 529f);
            AssertNodeVisualLoaded(behind, "before unloading the high-beat cluster");
            AssertNodeVisualLoaded(target, "before unloading the high-beat cluster");
            AssertNodeVisualLoaded(ahead, "before unloading the high-beat cluster");
            AssertVisibleRibbon(ahead, transition, "before unloading the high-beat cluster");
            AssertSameTimeEventsOwned(
                new[] { target, sameTimeRingEvent },
                "before unloading the high-beat same-time group");

            yield return ScrubThroughJsonTimes(535.25f, 540.75f);
            AssertNodeVisualUnloaded(target, "after unloading the high-beat cluster");

            // Reverse by uneven amounts, stopping around the screenshot's node with its converted SongBpmTime well
            // inside the loaded interval, then keep checking after later-frame visual work has completed.
            yield return ScrubThroughJsonTimes(538.125f, 533.375f, 531.125f);
            for (var stableFrame = 0; stableFrame <= 4; stableFrame++)
            {
                if (stableFrame > 0)
                {
                    yield return null;
                }

                var operation = stableFrame == 0
                    ? "after the high-beat backward scrub"
                    : $"on stable frame {stableFrame} after the high-beat backward scrub";
                AssertNodeVisualLoaded(behind, operation);
                AssertNodeVisualLoaded(target, operation);
                AssertNodeVisualLoaded(ahead, operation);
                AssertVisibleRibbon(ahead, transition, operation);
                AssertSameTimeEventsOwned(new[] { target, sameTimeRingEvent }, operation);
            }
        }

        private IEnumerator AssertBoundaryMatrix(
            float[] scrubRoute,
            float expectedLowerBound,
            float expectedUpperBound,
            string operation)
        {
            // Exercise point-node inclusion immediately around both inclusive pool bounds in every directional case.
            PrepareChunkingScenario();
            var belowLower = PlaceLightEvent(
                expectedLowerBound - BoundaryOffset,
                LightValue.RedOn,
                EventTypeValue.Event2);
            var atLower = PlaceLightEvent(expectedLowerBound, LightValue.BlueOn, EventTypeValue.Event2);
            var aboveLower = PlaceLightEvent(
                expectedLowerBound + BoundaryOffset,
                LightValue.WhiteOn,
                EventTypeValue.Event2);
            var belowUpper = PlaceLightEvent(
                expectedUpperBound - BoundaryOffset,
                LightValue.RedOn,
                EventTypeValue.Event2);
            var atUpper = PlaceLightEvent(expectedUpperBound, LightValue.BlueOn, EventTypeValue.Event2);
            var aboveUpper = PlaceLightEvent(
                expectedUpperBound + BoundaryOffset,
                LightValue.WhiteOn,
                EventTypeValue.Event2);

            // Crossing, inside-starting, entirely-before, and entirely-after ribbons cover retention and recycling.
            var lowerCrossingSource = PlaceLightEvent(
                expectedLowerBound - 2f,
                LightValue.RedOn,
                EventTypeValue.Event0);
            var lowerCrossingTarget = PlaceLightEvent(
                expectedLowerBound + 2f,
                LightValue.BlueTransition,
                EventTypeValue.Event0);
            var upperBoundarySource = PlaceLightEvent(
                expectedUpperBound,
                LightValue.BlueOn,
                EventTypeValue.Event1);
            var upperBoundaryTarget = PlaceLightEvent(
                expectedUpperBound + 2f,
                LightValue.RedTransition,
                EventTypeValue.Event1);
            var beforeWindowSource = PlaceLightEvent(
                expectedLowerBound - 2.5f,
                LightValue.WhiteOn,
                EventTypeValue.Event3);
            var beforeWindowTarget = PlaceLightEvent(
                expectedLowerBound - BoundaryOffset,
                LightValue.BlueTransition,
                EventTypeValue.Event3);
            var afterWindowSource = PlaceLightEvent(
                expectedUpperBound + BoundaryOffset,
                LightValue.RedOn,
                EventTypeValue.Event4);
            var afterWindowTarget = PlaceLightEvent(
                expectedUpperBound + 2.5f,
                LightValue.WhiteTransition,
                EventTypeValue.Event4);

            yield return ScrubThroughJsonTimes(scrubRoute);

            // Assert across later frames because a stale recycle callback can run after the test coroutine's first
            // continuation, making an immediately-correct node disappear only after the old tests had already passed.
            for (var stableFrame = 0; stableFrame <= 3; stableFrame++)
            {
                if (stableFrame > 0)
                {
                    yield return null;
                }

                AssertBoundaryVisualState(
                    new[] { atLower, aboveLower, belowUpper, atUpper, lowerCrossingSource, lowerCrossingTarget, upperBoundarySource },
                    new[] { belowLower, aboveUpper, upperBoundaryTarget, beforeWindowSource, beforeWindowTarget, afterWindowSource, afterWindowTarget },
                    lowerCrossingSource,
                    lowerCrossingTarget,
                    upperBoundarySource,
                    upperBoundaryTarget,
                    stableFrame == 0 ? operation : $"{operation}, stable frame {stableFrame}");
            }
        }

        private IEnumerator AssertLightIdViewReloadsCluster(
            EventTypeValue eventType,
            int[] lightIds,
            string laneDescription)
        {
            // Keep one lane-local chain so its last On node owns the transition ribbon exactly as rendered in the editor.
            PrepareChunkingScenario(EventGridContainer.PropMode.Light, eventType);
            var behind = PlaceLightEvent(39.95f, LightValue.RedOn, eventType, lightIds);
            var reportedMissing = PlaceLightEvent(40f, LightValue.BlueOn, eventType, lightIds);
            var ahead = PlaceLightEvent(40.05f, LightValue.WhiteOn, eventType, lightIds);
            var transition = PlaceLightEvent(41f, LightValue.BlueTransition, eventType, lightIds);

            yield return ScrubThroughJsonTimes(18.35f, 40f);
            AssertNodeVisualLoaded(behind, $"before unloading {laneDescription}");
            AssertNodeVisualLoaded(reportedMissing, $"before unloading {laneDescription}");
            AssertNodeVisualLoaded(ahead, $"before unloading {laneDescription}");
            AssertNodeVisualLoaded(transition, $"before unloading {laneDescription}");
            AssertVisibleRibbon(ahead, transition, $"before unloading {laneDescription}");

            // Traverse every beat forward and immediately backward to mirror successive mouse-wheel scrub frames.
            yield return ScrubThroughJsonTimes(41f, 42f, 43f, 44f, 45f, 46f, 47f, 48f);
            AssertNodeVisualUnloaded(behind, $"after unloading {laneDescription}");
            AssertNodeVisualUnloaded(reportedMissing, $"after unloading {laneDescription}");
            AssertNodeVisualUnloaded(ahead, $"after unloading {laneDescription}");
            AssertNodeVisualUnloaded(transition, $"after unloading {laneDescription}");

            yield return ScrubThroughJsonTimes(47f, 46f, 45f, 44f, 43f, 42f);
            AssertNodeVisualLoaded(behind, $"after scrubbing backward into {laneDescription}");
            AssertNodeVisualLoaded(reportedMissing, $"after scrubbing backward into {laneDescription}");
            AssertNodeVisualLoaded(ahead, $"after scrubbing backward into {laneDescription}");
            AssertNodeVisualLoaded(transition, $"after scrubbing backward into {laneDescription}");
            AssertVisibleRibbon(ahead, transition, $"after scrubbing backward into {laneDescription}");
            // Light-ID lane filtering can update after the first reload frame, so verify neither All Lights nor scoped
            // nodes become hidden when the delayed appearance and recycle work settles.
            for (var stableFrame = 1; stableFrame <= 4; stableFrame++)
            {
                yield return null;
                AssertNodeVisualLoaded(behind, $"on stable frame {stableFrame} after returning to {laneDescription}");
                AssertNodeVisualLoaded(reportedMissing, $"on stable frame {stableFrame} after returning to {laneDescription}");
                AssertNodeVisualLoaded(ahead, $"on stable frame {stableFrame} after returning to {laneDescription}");
                AssertNodeVisualLoaded(transition, $"on stable frame {stableFrame} after returning to {laneDescription}");
                AssertVisibleRibbon(ahead, transition, $"on stable frame {stableFrame} after returning to {laneDescription}");
            }
        }

        private void PrepareChunkingScenario(
            EventGridContainer.PropMode propagationMode = EventGridContainer.PropMode.Off,
            EventTypeValue propagatedEventType = EventTypeValue.Event2)
        {
            var eventsContainer = GetEventsContainer();
            // Preserve the user's settings once while every test uses the narrow deterministic stopped-time pool.
            visualizeGradientsBeforeTest ??= Settings.Instance.VisualizeChromaGradients;
            chunkDistanceBeforeTest ??= Settings.Instance.ChunkDistance;
            propagationEditingBeforeTest ??= eventsContainer.PropagationEditing;
            propagatedEventTypeBeforeTest ??= eventsContainer.EventTypeToPropagate;
            Settings.Instance.VisualizeChromaGradients = true;
            Settings.Instance.ChunkDistance = 2;
            eventsContainer.EventTypeToPropagate = (int)propagatedEventType;
            eventsContainer.PropagationEditing = propagationMode;
        }

        private static void AssertReloadedCluster(
            BaseEvent behind,
            BaseEvent reportedMissing,
            BaseEvent ahead,
            BaseEvent ribbonSource,
            BaseEvent ribbonTarget,
            string operation)
        {
            // The central node must not disappear while its immediate neighbors and its lane's ribbon successfully reload.
            AssertNodeVisualLoaded(behind, operation);
            AssertNodeVisualLoaded(reportedMissing, operation);
            AssertNodeVisualLoaded(ahead, operation);
            AssertNodeVisualLoaded(ribbonSource, operation);
            AssertNodeVisualLoaded(ribbonTarget, operation);
            AssertVisibleRibbon(ribbonSource, ribbonTarget, operation);
        }

        // Keep the six direction/boundary cases on one assertion contract so their multi-frame checks cannot omit a
        // node or ribbon depending on which side of the chunk midpoint the playhead approached from.
        private static void AssertBoundaryVisualState(
            BaseEvent[] loadedNodes,
            BaseEvent[] unloadedNodes,
            BaseEvent lowerRibbonSource,
            BaseEvent lowerRibbonTarget,
            BaseEvent upperRibbonSource,
            BaseEvent upperRibbonTarget,
            string operation)
        {
            for (var nodeIndex = 0; nodeIndex < loadedNodes.Length; nodeIndex++)
            {
                AssertNodeVisualLoaded(loadedNodes[nodeIndex], operation);
            }

            for (var nodeIndex = 0; nodeIndex < unloadedNodes.Length; nodeIndex++)
            {
                AssertNodeVisualUnloaded(unloadedNodes[nodeIndex], operation);
            }

            AssertVisibleRibbon(lowerRibbonSource, lowerRibbonTarget, operation);
            AssertVisibleRibbon(upperRibbonSource, upperRibbonTarget, operation);
        }

        // A dense map can load non-selected event types into the pool while intentionally hiding their lanes; verify
        // all members of the boundary-time group are owned without mistaking propagation visibility for a missing node.
        private static void AssertSameTimeEventsOwned(BaseEvent[] events, string operation)
        {
            var eventsContainer = GetEventsContainer();
            for (var eventIndex = 0; eventIndex < events.Length; eventIndex++)
            {
                var evt = events[eventIndex];
                Assert.That(
                    eventsContainer.LoadedContainers.ContainsKey(evt),
                    Is.True,
                    $"The same-time event type {evt.Type} at beat {evt.JsonTime} was absent from LoadedContainers "
                    + $"{operation}. {DescribeVisualPool(eventsContainer)}");
                Assert.That(
                    eventsContainer.ObjectsWithContainers.Any(candidate => object.ReferenceEquals(candidate, evt)),
                    Is.True,
                    $"The same-time event type {evt.Type} at beat {evt.JsonTime} was absent from the ordered pool "
                    + $"{operation}.");
                Assert.That(
                    evt.HasAttachedContainer,
                    Is.True,
                    $"The same-time event type {evt.Type} at beat {evt.JsonTime} had a false attachment flag {operation}.");
            }
        }

        // Re-check the mapper-visible selected-lane node and ribbon together with every same-time pool member so a
        // delayed recycle cannot pass the first assertion and remove only the reported node on a later frame.
        private static void AssertPhysicalBackwardReloadRemainsStable(
            BaseEvent target,
            BaseEvent transition,
            BaseEvent[] sameTimeEvents,
            string operation)
        {
            AssertNodeVisualLoaded(target, operation);
            AssertVisibleRibbon(target, transition, operation);
            AssertSameTimeEventsOwned(sameTimeEvents, operation);
        }

        // The reported map carries a dense mix of event types around the missing light node. Populate neighboring
        // beats through EventPlacement so pool reuse and chronological insertion match ordinary authoring callbacks.
        private static void PlaceDenseNeighborEvents(
            float firstJsonTime,
            float lastJsonTime,
            EventTypeValue selectedEventType)
        {
            for (var jsonTime = firstJsonTime; jsonTime <= lastJsonTime; jsonTime += 0.5f)
            {
                for (var eventType = (int)EventTypeValue.Event0;
                     eventType <= (int)EventTypeValue.Event4;
                     eventType++)
                {
                    if (eventType == (int)selectedEventType || Mathf.Approximately(jsonTime, 40f))
                    {
                        continue;
                    }

                    // Parenthesize the modulo result so the switch classifies the integer remainder, not LightValue.
                    var value = (eventType % 3) switch
                    {
                        0 => LightValue.RedOn,
                        1 => LightValue.BlueOn,
                        _ => LightValue.WhiteOn
                    };
                    PlaceLightEvent(jsonTime, value, (EventTypeValue)eventType);
                }
            }
        }

        // Raise the base BPM without allocating a multi-minute 44.1 kHz clip, allowing the shared 60-second test song
        // to reach beat 529 while preserving the same JsonTime-to-SongBpmTime conversion used by the real map.
        private void UseHighBeatTimeline()
        {
            songBpmBeforeTest ??= BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            BeatSaberSongContainer.Instance.Info.BeatsPerMinute = 600f;
            BeatSaberSongContainer.Instance.Map.ValidateBpmEventsAndObjectTimes(600f);
        }

        // Drive the generated Timeline action map with the real AudioTimeSyncController callback while disabling the
        // application's duplicate shared map, matching the production wheel binding without double-dispatching tests.
        private void PreparePhysicalTimelineInput()
        {
            Assert.That(physicalTimelineInput, Is.Null, "Physical Timeline input was initialized twice in one test.");
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.That(sharedInput, Is.Not.Null, "The application's shared input asset was not initialized.");
            sharedTimelineInputWasEnabled = sharedInput.Timeline.enabled;
            sharedInput.Timeline.Disable();
            sharedUtilsInputWasEnabled = sharedInput.Utils.enabled;
            sharedInput.Utils.Disable();

            InitializeVirtualInput(false);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            physicalTimelineInput = new CMInput();
            physicalTimelineInput.Timeline.SetCallbacks(atsc);
            // Isolate mouse-position routing with Timeline so KeybindsController.IsMouseInWindow is refreshed by the
            // same physical state event instead of retaining a prior test's offscreen pointer state.
            physicalTimelineInput.Utils.SetCallbacks(Object.FindAnyObjectByType<KeybindsController>());
            physicalTimelineInput.Timeline.Enable();
            physicalTimelineInput.Utils.Enable();

            invertScrollTimeBeforeTest ??= Settings.Instance.InvertScrollTime;
            gridSnappingBeforeTest ??= atsc.GridMeasureSnapping;
            Settings.Instance.InvertScrollTime = false;
            atsc.GridMeasureSnapping = 2;

            // Use the Game view bounds checked by KeybindsController itself; a camera pixel rect can be zero-sized in
            // headless test rendering even though physical editor input still has a valid window.
#if UNITY_EDITOR
            var gameViewSize = UnityEditor.Handles.GetMainGameViewSize();
#else
            var gameViewSize = new Vector2(Screen.width, Screen.height);
#endif
            Assert.That(gameViewSize.x, Is.GreaterThan(2f), "The editor Game view had no usable width.");
            Assert.That(gameViewSize.y, Is.GreaterThan(2f), "The editor Game view had no usable height.");
            physicalScrollScreenPosition = gameViewSize * 0.5f;

            // Force an actual pointer transition rather than accepting cached static state.
            SetVirtualMouseState(-Vector2.one, Vector2.zero);
            Assert.That(
                KeybindsController.IsMouseInWindow,
                Is.False,
                "The virtual Timeline pointer did not leave the editor window before entering it.");
            SetVirtualMouseState(physicalScrollScreenPosition + Vector2.one, Vector2.zero);
            SetVirtualMouseState(physicalScrollScreenPosition, Vector2.zero);
            Assert.That(
                KeybindsController.IsMouseInWindow,
                Is.True,
                "The physical Timeline pointer did not enter the editor window before scrolling.");
        }

        // Yield after each wheel pulse for ordinary scrolling, while the rapid-reversal test calls the single-pulse
        // helper repeatedly before yielding to cover several reports processed ahead of collection LateUpdate.
        private IEnumerator PhysicalWheelScrub(int direction, int pulseCount, bool settleEachPulse)
        {
            for (var pulse = 0; pulse < pulseCount; pulse++)
            {
                SendPhysicalWheelPulse(direction);
                if (settleEachPulse)
                {
                    yield return null;
                }
            }
        }

        // Assert every physical pulse moves exactly one half beat so pointer/UI routing failures are identified as
        // setup failures instead of being misreported as successful node chunking behavior.
        private void SendPhysicalWheelPulse(int direction)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var before = atsc.CurrentJsonTime;
            var expected = before + (direction * 0.5f);

            // Unity's synthetic scroll control can occasionally lose one performed edge across automatic test-runner
            // input updates. Re-establish an in-window pointer delta and retry that edge before evaluating chunk state.
            const int maximumAttempts = 3;
            var attempt = 0;
            while (attempt < maximumAttempts && Mathf.Approximately(atsc.CurrentJsonTime, before))
            {
                var pointerNudge = attempt % 2 == 0 ? Vector2.one : -Vector2.one;
                SetVirtualMouseState(physicalScrollScreenPosition + pointerNudge, Vector2.zero);
                SetVirtualMouseState(physicalScrollScreenPosition, new Vector2(0f, direction));
                SetVirtualMouseState(physicalScrollScreenPosition, Vector2.zero);
                attempt++;
            }

            Assert.That(
                atsc.CurrentJsonTime,
                Is.EqualTo(expected).Within(0.001f),
                $"The physical Timeline wheel pulse did not reach AudioTimeSyncController.OnChangeTimeandPrecision "
                + $"after {attempt} synthetic edge attempts.");
        }

        // Dispose the isolated action asset and any synthetic mouse before restoring the shared Timeline map so bulk
        // test runs cannot retain an extra callback or disabled application binding after this fixture finishes.
        private void DisposePhysicalTimelineInput()
        {
            if (physicalTimelineInput != null)
            {
                physicalTimelineInput.Timeline.Disable();
                physicalTimelineInput.Utils.Disable();
                physicalTimelineInput.Dispose();
                physicalTimelineInput = null;
            }

            TearDownVirtualInput();

            var sharedInput = CMInputCallbackInstaller.InputInstance;
            if (sharedInput != null && sharedTimelineInputWasEnabled == true)
            {
                sharedInput.Timeline.Enable();
            }
            sharedTimelineInputWasEnabled = null;

            if (sharedInput != null && sharedUtilsInputWasEnabled == true)
            {
                sharedInput.Utils.Enable();
            }
            sharedUtilsInputWasEnabled = null;
        }
    }
}

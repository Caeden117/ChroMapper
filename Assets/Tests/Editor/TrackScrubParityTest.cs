using System.Collections;
using System.Linq;
using NUnit.Framework;
using Beatmap.Info;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // TrackScrubParityTest constrains stopped-time seeking through the production map-load and AnimateTrack
    // pipeline: every seek must land synchronously on the state the map would have if it played exactly to that
    // beat, scrubbing back before the first event must restore the freshly-loaded transform, and repeated scrub
    // sequences must be deterministic. Both delivery paths are covered: direct environment targets (the matched
    // object's own track) and track object parents (AssignTrackParent chains).
    public class TrackScrubParityTest : TestBase
    {
        private const string EnvironmentSceneName = "DefaultEnvironment";
        private const string DirectTrackName = "scrubDirect";
        private const string ParentTrackName = "scrubParent";
        private const string ChildTrackName = "scrubChild";

        // Seeks land inside the beat 4..6 interpolation, after the event (post-event hold), and before the first
        // event (restore), in an order that revisits beats and crosses the event window in both directions.
        private static readonly float[] ScrubOrder = { 5f, 10f, 0f, 2f, 5f, 2f, 10f, 0f };

        // SeeksMustLandOnTheAsIfPlayedStateSynchronouslyAndDeterministically reproduces the reported scrubbing
        // breakage: values stayed at (or folded together with) the previously evaluated beat, and objects never
        // returned to their authored transform after scrubbing back before the first event.
        [UnityTest]
        public IEnumerator SeeksMustLandOnTheAsIfPlayedStateSynchronouslyAndDeterministically()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var directMarker = descriptor.ChromaIDMarkers.Single(m => m.ChromaID.EndsWith("NearBuildingLeft (1)"));
            var parentedMarker = descriptor.ChromaIDMarkers.Single(m => m.ChromaID.EndsWith("NearBuildingRight (1)"));
            var directVanilla = directMarker.transform.position;
            var parentedVanilla = parentedMarker.transform.position;

            var results = new System.Collections.Generic.List<(float beat, Vector3 direct, Vector3 parented)>();
            for (var pass = 0; pass < 2; pass++)
            {
                results.Clear();
                foreach (var beat in ScrubOrder)
                {
                    atsc.MoveToJsonTime(beat);
                    // No frame yields: the seek itself must land on the as-if-played state.
                    results.Add((beat, directMarker.transform.position, parentedMarker.transform.position));
                }

                foreach (var (beat, direct, parented) in results)
                {
                    AssertDirectPosition(beat, direct, directVanilla);
                    AssertParentedPosition(beat, parented, parentedVanilla);
                }
            }

            yield break;
        }

        // Salty wall regressions (SaltyBeat219PlacementParityTest
        // .Beat221WallsStayAtAuthoredDepthOnImmediateReverseSeek /
        // SaltyFullMapPlacementParityTest.Beat221WallsSeekSynchronouslyToStableGameDepth): models how a
        // held track contribution must persist across repeated reads while stopped, until the next
        // seek's flush drops it. Current Get() drains transient values to Keep on the first read, so
        // the second read returns preload — the post-frame decay the wall tests observe.
        [Test]
        public void HeldTrackAggregatorValuePersistsUntilStoppedSeekFlush()
        {
            var offset = new Beatmap.Animations.ObjectAnimator.Aggregator<Vector3>(
                Vector3.zero, (a, b) => a + b);
            // Gameplay-object OffsetPosition opts in to holding the pushed value until flush.
            offset.HoldUntilFlush = true;
            offset.Preload(new Vector3(0, 0, -0.25f));
            offset.Add(new Vector3(0, 0, 6.6f));
            Assert.That(offset.Get().z, Is.EqualTo(6.35f).Within(0.001f));
            Assert.That(offset.Get().z, Is.EqualTo(6.35f).Within(0.001f),
                "held value must survive repeated reads until the next stopped seek flushes it");

            offset.Add(new Vector3(0, 0, 8f));
            Assert.That(offset.Get().z, Is.EqualTo(7.75f).Within(0.001f));
            Assert.That(offset.Get().z, Is.EqualTo(7.75f).Within(0.001f));

            offset.Flush();
            Assert.That(offset.Get().z, Is.EqualTo(-0.25f).Within(0.001f));
            offset.Add(new Vector3(0, 0, 6.6f));
            Assert.That(offset.Get().z, Is.EqualTo(6.35f).Within(0.001f));
        }

        // SeeksMustLandOnTheAsIfPlayedStateSynchronouslyAndDeterministically covers the live direct
        // environment path; this unit test protects the default drain semantics for every aggregator
        // that does not opt into the gameplay-object HoldUntilFlush hold.
        [Test]
        public void UnheldAggregatorsKeepDirectEnvironmentSemantics()
        {
            var offset = new Beatmap.Animations.ObjectAnimator.Aggregator<Vector3>(
                Vector3.zero, (a, b) => a + b);
            offset.Preload(new Vector3(0, 0, -0.25f));
            offset.Add(new Vector3(0, 0, 6.6f));
            Assert.That(offset.Get().z, Is.EqualTo(6.35f).Within(0.001f));
            Assert.That(offset.Get().z, Is.EqualTo(-0.25f).Within(0.001f),
                "default aggregators must drain transient values back to Keep on the first read");
            offset.Flush();
            Assert.That(offset.Get().z, Is.EqualTo(-0.25f).Within(0.001f));
        }

        // AnimateTrack "scrubDirect" (event beat 4, duration 2): V2 position is absolute world position scaled by
        // the note line distance, so beats before the event hold the authored transform, beat 5 interpolates to
        // (0, 0, 15) * LaneScale, and beats after the event hold (0, 0, 20) * LaneScale.
        private static void AssertDirectPosition(float beat, Vector3 actual, Vector3 vanilla)
        {
            Vector3 expected;
            if (beat < 4f)
            {
                expected = vanilla;
            }
            else if (beat < 6f)
            {
                expected = new Vector3(0f, 0f, Mathf.Lerp(10f, 20f, (beat - 4f) / 2f)) * BeatmapConstant.LaneSize;
            }
            else
            {
                expected = new Vector3(0f, 0f, 20f) * BeatmapConstant.LaneSize;
            }

            Assert.That(
                Vector3.Distance(actual, expected),
                Is.LessThan(0.001f),
                $"Seeking to beat {beat} left the direct target at {actual} instead of the as-if-played {expected}.");
        }

        // The AssignTrackParent chain animates the child track's object parent, so the matched object rides the
        // animated offset on top of its authored transform instead of being overwritten absolutely.
        private static void AssertParentedPosition(float beat, Vector3 actual, Vector3 vanilla)
        {
            var trackZ = beat < 4f
                ? 0f
                : (beat < 6f
                    ? Mathf.Lerp(10f, 20f, (beat - 4f) / 2f)
                    : 20f) * BeatmapConstant.LaneSize;

            Assert.That(
                Vector3.Distance(actual, vanilla + new Vector3(0f, 0f, trackZ)),
                Is.LessThan(0.001f),
                $"Seeking to beat {beat} left the parented target at {actual} instead of its authored transform " +
                $"plus the animated track offset {trackZ}.");
        }

        // Restore the canonical empty shared map so later fixtures do not inherit the scrub fixture or the
        // DefaultEnvironment enhancements.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The fixture animates two DefaultEnvironment buildings: one directly on its own track and one through an
        // AssignTrackParent chain, both with the same AnimateTrack points so both paths are constrained by the
        // same expected values.
        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");

            yield return TestUtils.ReloadMap(2, CreateScrubDifficulty());
            scrubDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private Beatmap.Info.InfoDifficulty scrubDifficulty;

        // RestoreEmptySharedMap below restores the plain shared difficulty after every test, so any case that
        // does not run first in this fixture keeps the DefaultEnvironment scene but loses the scrub
        // difficulty's AnimateTrack custom events. Re-establish it when the loaded difficulty changed.
        [UnitySetUp]
        public IEnumerator EnsureScrubFixtureLoaded()
        {
            if (!ReferenceEquals(BeatSaberSongContainer.Instance.MapDifficultyInfo, scrubDifficulty))
            {
                yield return TestUtils.ReloadMap(2, CreateScrubDifficulty());
                scrubDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
                TestUtils.CaptureCurrentMapAsSharedBaseline();
            }
        }

        private static JSONNode CreateScrubDifficulty()
        {
            var difficulty = new JSONObject
            {
                ["_version"] = "2.6.0",
                ["_events"] = new JSONArray(),
                ["_notes"] = new JSONArray(),
                ["_obstacles"] = new JSONArray(),
                ["_waypoints"] = new JSONArray(),
                ["_sliders"] = new JSONArray(),
                ["_specialEventsKeywordFilters"] = new JSONObject()
            };
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["_id"] = @"NearBuildingLeft \(1\)$",
                ["_lookupMethod"] = "Regex",
                ["_track"] = DirectTrackName
            });
            environment.Add(new JSONObject
            {
                ["_id"] = @"NearBuildingRight \(1\)$",
                ["_lookupMethod"] = "Regex",
                ["_track"] = ChildTrackName
            });
            var customEvents = new JSONArray();
            customEvents.Add(new JSONObject
            {
                ["_time"] = 4f,
                ["_type"] = "AnimateTrack",
                ["_data"] = new JSONObject
                {
                    ["_track"] = DirectTrackName,
                    ["_duration"] = 2f,
                    ["_position"] = JSON.Parse("[[0,0,10,0],[0,0,20,1]]")
                }
            });
            customEvents.Add(new JSONObject
            {
                ["_time"] = 4f,
                ["_type"] = "AnimateTrack",
                ["_data"] = new JSONObject
                {
                    ["_track"] = ParentTrackName,
                    ["_duration"] = 2f,
                    ["_position"] = JSON.Parse("[[0,0,10,0],[0,0,20,1]]")
                }
            });
            // SimpleJSON's JSONArray exposes Add without implementing IEnumerable, so build it explicitly.
            var childrenTracks = new JSONArray();
            childrenTracks.Add(ChildTrackName);
            customEvents.Add(new JSONObject
            {
                ["_time"] = 0f,
                ["_type"] = "AssignTrackParent",
                ["_data"] = new JSONObject
                {
                    ["_parentTrack"] = ParentTrackName,
                    ["_childrenTracks"] = childrenTracks,
                    ["_worldPositionStays"] = true
                }
            });
            difficulty["_customData"] = new JSONObject
            {
                ["_environment"] = environment,
                ["_customEvents"] = customEvents
            };
            return difficulty;
        }
    }
}

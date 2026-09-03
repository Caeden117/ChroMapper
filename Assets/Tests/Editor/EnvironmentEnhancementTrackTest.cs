using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // EnvironmentEnhancementWith*TrackKeepsDefaultEnvironmentBigRingsVisible reproduces the Plague Doctor map's
    // disappearing DefaultEnvironment rings through the production V2 map and environment-enhancement load path.
    public class EnvironmentEnhancementTrackTest : TestBase
    {
        private const string EnvironmentSceneName = "DefaultEnvironment";
        private const string RingIdSuffix = "BigTrackLaneRing(Clone)";
        private const string TrackName = "regressionBigRing";
        private static readonly Vector3 ExpectedScale = new(2.5f, 1f, 1f);
        private static RingState[] tracklessEnhancementBaseline;

        // EnvironmentEnhancementWith*Track* tests share this immutable production-loaded control snapshot so each
        // variant needs only its own scene reload instead of reloading the same DefaultEnvironment control five times.
        protected override IEnumerator OnMapLoaded()
        {
            // EnvironmentEnhancementTrackTest guards the bulk-run regression where four half-second loading fades made
            // every case two seconds slower before its production scene reload and assertions even began.
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return TestUtils.ReloadMap(2, CreateReportedDifficulty(false, TrackUsage.None));
            yield return MoveToReproductionBeat();
            tracklessEnhancementBaseline = CaptureBigRings();
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // EnvironmentEnhancementWithoutTrackKeepsDefaultEnvironmentBigRingsVisible is the passing control: applying
        // the reported scale without a track must preserve every root ring's authored position, rotation, and renderer.
        [UnityTest]
        public IEnumerator EnvironmentEnhancementWithoutTrackKeepsDefaultEnvironmentBigRingsVisible()
        {
            yield return TestUtils.ReloadMap(2, CreateReportedDifficulty(false, TrackUsage.None, false));
            yield return MoveToReproductionBeat();
            var authored = CaptureBigRings();
            var expected = authored
                .Select(ring => ring.WithLossyScale(Vector3.Scale(ring.LossyScale, ExpectedScale)))
                .ToArray();

            AssertRingsMatch(
                expected,
                tracklessEnhancementBaseline,
                "applying the trackless environment enhancement");
        }

        // EnvironmentEnhancementWithUnusedTrackKeepsDefaultEnvironmentBigRingsVisible guards the reported case where
        // merely assigning an otherwise-unused track must not move, flatten, hide, or rescale the enhanced rings.
        [UnityTest]
        public IEnumerator EnvironmentEnhancementWithUnusedTrackKeepsDefaultEnvironmentBigRingsVisible()
        {
            yield return AssertTrackAssignmentMatchesTracklessEnhancement(TrackUsage.None);
        }

        // EnvironmentEnhancementWithEmptyAnimateTrackKeepsDefaultEnvironmentBigRingsVisible covers a track referenced
        // by an AnimateTrack event with no transform properties, which Chroma leaves at the enhanced OEM transform.
        [UnityTest]
        public IEnumerator EnvironmentEnhancementWithEmptyAnimateTrackKeepsDefaultEnvironmentBigRingsVisible()
        {
            yield return AssertTrackAssignmentMatchesTracklessEnhancement(TrackUsage.EmptyEvent);
        }

        // EnvironmentEnhancementWithSameScaleAnimateTrackMatchesTracklessResult covers Chroma's observed behavior:
        // actively setting the track scale to the enhancement's existing value preserves the complete rendered result.
        [UnityTest]
        public IEnumerator EnvironmentEnhancementWithSameScaleAnimateTrackMatchesTracklessResult()
        {
            yield return AssertTrackAssignmentMatchesTracklessEnhancement(TrackUsage.SameScale);
        }

        // EnvironmentEnhancementWithZeroPositionAnimateTrackKeepsDefaultEnvironmentBigRingsVisibleAtWorldOrigin
        // records Chroma's observed behavior: position zero collapses every matched ring to origin but does not hide it.
        [UnityTest]
        public IEnumerator EnvironmentEnhancementWithZeroPositionAnimateTrackKeepsDefaultEnvironmentBigRingsVisibleAtWorldOrigin()
        {
            var expected = tracklessEnhancementBaseline
                .Select(ring => ring.WithTrackBaseWorldPosition(Vector3.zero))
                .ToArray();

            yield return TestUtils.ReloadMap(2, CreateReportedDifficulty(true, TrackUsage.ZeroWorldPosition));
            yield return MoveToReproductionBeat();
            AssertTrackUsageLoaded(TrackUsage.ZeroWorldPosition);
            var actual = CaptureBigRings();

            AssertRingsMatch(expected, actual, "animating the environment track to world position zero");
        }

        // Every case leaves a valid production map loaded; adopting it keeps TestBase's next per-test reset aligned
        // with the active scene without paying for an otherwise redundant empty-map transition after every assertion.
        [UnityTearDown]
        public IEnumerator SynchronizeSharedMapBaseline()
        {
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            yield break;
        }

        // EnvironmentEnhancementTrackTest must leave the shared test runner on its canonical empty V3 map, but one
        // fixture-level restoration is sufficient because every individual case now synchronizes its loaded baseline.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The trackless load is the known-good Chroma-compatible rendering baseline; the tracked load must preserve
        // every matching marker's world transform and the root ring's visible renderer hierarchy at beat 8.
        private static IEnumerator AssertTrackAssignmentMatchesTracklessEnhancement(TrackUsage trackUsage)
        {
            yield return TestUtils.ReloadMap(2, CreateReportedDifficulty(true, trackUsage));
            yield return MoveToReproductionBeat();
            AssertTrackUsageLoaded(trackUsage);
            var actual = CaptureBigRings();

            AssertRingsMatch(tracklessEnhancementBaseline, actual, "assigning the environment track");
        }

        // Distinguish an unused assigned track from both authored AnimateTrack cases before interpreting transform output.
        private static void AssertTrackUsageLoaded(TrackUsage trackUsage)
        {
            Assert.That(
                BeatSaberSongContainer.Instance.Map.CustomEvents.Any(customEvent =>
                    customEvent.Type == "AnimateTrack"
                    && customEvent.CustomTrack?.Value == TrackName),
                Is.EqualTo(trackUsage != TrackUsage.None),
                "The requested AnimateTrack usage did not load with the tracked environment fixture.");
        }

        // All four Chroma-equivalent cases use this assertion matrix so they also preserve TrackLaneRing's native base
        // offset, animated wave displacement, and internal Z state in addition to the visible root transform.
        private static void AssertRingsMatch(RingState[] expected, RingState[] actual, string operation)
        {
            Assert.That(
                actual,
                Has.Length.EqualTo(expected.Length),
                $"The number of DefaultEnvironment BigTrackLaneRing roots changed after {operation}.");
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i].ChromaId, Is.EqualTo(expected[i].ChromaId));
                Assert.That(
                    actual[i].ActiveInHierarchy,
                    Is.EqualTo(expected[i].ActiveInHierarchy),
                    $"Environment ring {expected[i].ChromaId} changed active state after {operation}.");
                Assert.That(
                    actual[i].HasVisibleRenderer,
                    Is.EqualTo(expected[i].HasVisibleRenderer),
                    $"Environment ring {expected[i].ChromaId} changed its visible renderer hierarchy after {operation}.");
                Assert.That(
                    Vector3.Distance(actual[i].Position, expected[i].Position),
                    Is.LessThan(0.0001f),
                    $"Environment ring {expected[i].ChromaId} moved after {operation}. "
                    + $"Expected {expected[i].Position}, got {actual[i].Position}.");
                Assert.That(
                    Quaternion.Angle(actual[i].Rotation, expected[i].Rotation),
                    Is.LessThan(0.001f),
                    $"Environment ring {expected[i].ChromaId} rotated after {operation}.");
                Assert.That(
                    Vector3.Distance(actual[i].LossyScale, expected[i].LossyScale),
                    Is.LessThan(0.0001f),
                    $"Environment ring {expected[i].ChromaId} changed rendered scale after {operation}. "
                    + $"Expected {expected[i].LossyScale}, got {actual[i].LossyScale}.");
                Assert.That(
                    Vector3.Distance(actual[i].RingPositionOffset, expected[i].RingPositionOffset),
                    Is.LessThan(0.0001f),
                    $"Environment ring {expected[i].ChromaId} changed its native local position offset after {operation}. "
                    + $"Expected {expected[i].RingPositionOffset}, got {actual[i].RingPositionOffset}.");
                Assert.That(
                    Vector3.Distance(actual[i].RingBaseWorldPosition, expected[i].RingBaseWorldPosition),
                    Is.LessThan(0.0001f),
                    $"Environment ring {expected[i].ChromaId} changed its world-space ring base after {operation}. "
                    + $"Expected {expected[i].RingBaseWorldPosition}, got {actual[i].RingBaseWorldPosition}.");
                Assert.That(
                    Vector3.Distance(actual[i].RingWaveLocalOffset, expected[i].RingWaveLocalOffset),
                    Is.LessThan(0.0001f),
                    $"Environment ring {expected[i].ChromaId} changed its local wave displacement after {operation}. "
                    + $"Expected {expected[i].RingWaveLocalOffset}, got {actual[i].RingWaveLocalOffset}.");
                Assert.That(
                    Vector3.Distance(actual[i].RingWaveWorldOffset, expected[i].RingWaveWorldOffset),
                    Is.LessThan(0.0001f),
                    $"Environment ring {expected[i].ChromaId} changed its world-space wave displacement after {operation}. "
                    + $"Expected {expected[i].RingWaveWorldOffset}, got {actual[i].RingWaveWorldOffset}.");
                Assert.That(
                    actual[i].RingPositionZ,
                    Is.EqualTo(expected[i].RingPositionZ).Within(0.0001f),
                    $"Environment ring {expected[i].ChromaId} changed its native wave Z state after {operation}.");
            }
        }

        // Beat 8 is the first retained note/event cluster from the report and forces the editor track hierarchy away
        // from its initial origin before the environment object's final rendered transform is sampled.
        private static IEnumerator MoveToReproductionBeat()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(8f);
            yield return null;
            yield return null;
        }

        // Only root BigTrackLaneRing markers represent complete visible rings; sorting by Chroma ID makes the before
        // and after snapshots deterministic without retaining Unity objects across the intervening scene reload.
        private static RingState[] CaptureBigRings()
        {
            var environmentScene = SceneManager.GetSceneByName(EnvironmentSceneName);
            Assert.That(environmentScene.IsValid() && environmentScene.isLoaded, Is.True, "DefaultEnvironment did not load.");
            var descriptor = environmentScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnvironmentDescriptor>(true))
                .Single();
            var rings = descriptor.ChromaIDMarkers
                .Where(marker => marker.ChromaID.EndsWith(RingIdSuffix))
                .OrderBy(marker => marker.ChromaID)
                .Select(marker => new RingState(marker))
                .ToArray();

            Assert.That(rings, Is.Not.Empty, "DefaultEnvironment exposed no root BigTrackLaneRing markers.");
            Assert.That(
                rings.All(ring => ring.HasTrackLaneRing),
                Is.True,
                "A DefaultEnvironment BigTrackLaneRing root had no native TrackLaneRing offset state.");
            return rings;
        }

        // This compact V2 fixture retains the report's two distant notes/events and all three environment entries,
        // while independently toggling only the problematic track assignment and an identity use of that track.
        private static JSONNode CreateReportedDifficulty(
            bool assignTrack,
            TrackUsage trackUsage,
            bool includeBigRingEnhancement = true)
        {
            var difficulty = new JSONObject
            {
                ["_version"] = "2.6.0",
                ["_events"] = JSON.Parse(
                    "[{\"_time\":8,\"_type\":0,\"_value\":5,\"_floatValue\":1},"
                    + "{\"_time\":323.5,\"_type\":3,\"_value\":5,\"_floatValue\":1,"
                    + "\"_customData\":{\"_color\":[0,0,1,0]}}]"),
                ["_notes"] = JSON.Parse(
                    "[{\"_time\":8,\"_lineIndex\":2,\"_lineLayer\":0,\"_type\":1,\"_cutDirection\":1},"
                    + "{\"_time\":323.5,\"_lineIndex\":0,\"_lineLayer\":1,\"_type\":0,\"_cutDirection\":1}]"),
                ["_obstacles"] = new JSONArray(),
                ["_waypoints"] = new JSONArray(),
                ["_sliders"] = new JSONArray(),
                ["_specialEventsKeywordFilters"] = new JSONObject()
            };
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["_id"] = "NearBuilding",
                ["_lookupMethod"] = "Contains",
                ["_active"] = false
            });
            var bigRing = new JSONObject
            {
                ["_id"] = "BigTrackLaneRing",
                ["_lookupMethod"] = "Contains",
                ["_scale"] = JSON.Parse("[2.5,1,1]")
            };
            if (assignTrack)
            {
                bigRing["_track"] = TrackName;
            }

            if (includeBigRingEnhancement)
            {
                environment.Add(bigRing);
            }

            environment.Add(new JSONObject
            {
                ["_id"] = "SmallTrackLaneRing",
                ["_lookupMethod"] = "Contains",
                ["_scale"] = JSON.Parse("[1,2.5,4]"),
                ["_localRotation"] = JSON.Parse("[0,0,147]")
            });
            var customData = new JSONObject
            {
                ["_environment"] = environment,
                ["_time"] = 265.321f
            };
            if (trackUsage != TrackUsage.None)
            {
                var animationData = new JSONObject
                {
                    ["_track"] = TrackName,
                    ["_duration"] = 1
                };
                if (trackUsage == TrackUsage.ZeroWorldPosition)
                {
                    animationData["_position"] = JSON.Parse("[[0,0,0,0],[0,0,0,1]]");
                }
                else if (trackUsage == TrackUsage.SameScale)
                {
                    animationData["_scale"] = JSON.Parse("[[2.5,1,1,0],[2.5,1,1,1]]");
                }

                // SimpleJSON's JSONArray exposes Add without implementing IEnumerable, so build this fixture explicitly.
                var customEvents = new JSONArray();
                customEvents.Add(
                    new JSONObject
                    {
                        ["_time"] = 0,
                        ["_type"] = "AnimateTrack",
                        ["_data"] = animationData
                    });
                customData["_customEvents"] = customEvents;
            }

            difficulty["_customData"] = customData;
            return difficulty;
        }

        // RingState stores only value data because each reload destroys the prior DefaultEnvironment scene objects.
        private readonly struct RingState
        {
            // Snapshot every user-visible invariant before the scene is replaced by the track-assigned variant.
            public RingState(ChromaIDMarker marker)
            {
                var ring = marker.GetComponent<TrackLaneRing>();
                ChromaId = marker.ChromaID;
                ActiveInHierarchy = marker.gameObject.activeInHierarchy;
                HasVisibleRenderer = marker
                    .GetComponentsInChildren<Renderer>(true)
                    .Any(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy);
                Position = marker.transform.position;
                Rotation = marker.transform.rotation;
                LossyScale = marker.transform.lossyScale;
                HasTrackLaneRing = ring != null;
                RingPositionOffset = ring != null ? ring.PositionOffset : Vector3.zero;
                RingPositionZ = ring != null ? ring.PositionZ : 0f;
                RingWaveLocalOffset = ring != null
                    ? marker.transform.localPosition - ring.PositionOffset
                    : Vector3.zero;
                var parent = marker.transform.parent;
                ParentWorldToLocalMatrix = parent != null ? parent.worldToLocalMatrix : Matrix4x4.identity;
                RingBaseWorldPosition = parent != null
                    ? parent.TransformPoint(RingPositionOffset)
                    : RingPositionOffset;
                RingWaveWorldOffset = Position - RingBaseWorldPosition;
            }

            // The no-track control derives its expected effective scale from the authored DefaultEnvironment transform
            // while retaining the exact same identity, visibility, position, and rotation assertions as tracked cases.
            private RingState(RingState source, Vector3 lossyScale)
            {
                ChromaId = source.ChromaId;
                ActiveInHierarchy = source.ActiveInHierarchy;
                HasVisibleRenderer = source.HasVisibleRenderer;
                Position = source.Position;
                Rotation = source.Rotation;
                LossyScale = lossyScale;
                HasTrackLaneRing = source.HasTrackLaneRing;
                RingPositionOffset = source.RingPositionOffset;
                RingPositionZ = source.RingPositionZ;
                RingWaveLocalOffset = source.RingWaveLocalOffset;
                RingBaseWorldPosition = source.RingBaseWorldPosition;
                RingWaveWorldOffset = source.RingWaveWorldOffset;
                ParentWorldToLocalMatrix = source.ParentWorldToLocalMatrix;
            }

            public RingState WithLossyScale(Vector3 lossyScale) => new(this, lossyScale);

            // Chroma moves the TrackLaneRing base to the absolute track position, then its native ring update reapplies
            // the existing per-segment wave displacement instead of stacking every segment at that position.
            public RingState WithTrackBaseWorldPosition(Vector3 baseWorldPosition) =>
                new(this, baseWorldPosition, true);

            // Derive the expected local base offset and visible world position from the unchanged OEM parent and wave.
            private RingState(RingState source, Vector3 baseWorldPosition, bool preserveWaveOffset)
            {
                ChromaId = source.ChromaId;
                ActiveInHierarchy = source.ActiveInHierarchy;
                HasVisibleRenderer = source.HasVisibleRenderer;
                Position = preserveWaveOffset
                    ? baseWorldPosition + source.RingWaveWorldOffset
                    : baseWorldPosition;
                Rotation = source.Rotation;
                LossyScale = source.LossyScale;
                HasTrackLaneRing = source.HasTrackLaneRing;
                RingPositionOffset = source.ParentWorldToLocalMatrix.MultiplyPoint3x4(baseWorldPosition);
                // The zero-position regression rebases CM's absolute native Z state by the same amount as its base;
                // this preserves the relative wave value that Chroma stores separately from the base offset.
                RingPositionZ = source.RingPositionZ + RingPositionOffset.z - source.RingPositionOffset.z;
                RingWaveLocalOffset = source.RingWaveLocalOffset;
                RingBaseWorldPosition = baseWorldPosition;
                RingWaveWorldOffset = source.RingWaveWorldOffset;
                ParentWorldToLocalMatrix = source.ParentWorldToLocalMatrix;
            }

            public string ChromaId { get; }

            public bool ActiveInHierarchy { get; }

            public bool HasVisibleRenderer { get; }

            public Vector3 Position { get; }

            public Quaternion Rotation { get; }

            public Vector3 LossyScale { get; }

            public bool HasTrackLaneRing { get; }

            public Vector3 RingPositionOffset { get; }

            public float RingPositionZ { get; }

            public Vector3 RingWaveLocalOffset { get; }

            public Vector3 RingBaseWorldPosition { get; }

            public Vector3 RingWaveWorldOffset { get; }

            private Matrix4x4 ParentWorldToLocalMatrix { get; }
        }

        // Separate absence of animation data, both Chroma-equivalent no-op events, and absolute zero-position behavior.
        private enum TrackUsage
        {
            None,
            EmptyEvent,
            SameScale,
            ZeroWorldPosition
        }
    }
}

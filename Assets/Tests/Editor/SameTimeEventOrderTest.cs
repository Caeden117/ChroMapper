using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Animations;
using Beatmap.V2;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Regression source: "As The World Caves In", mapped by Mawntee & Fatalution (BeatSaver ID: 210e3).
    // The embedded ordering maps are synthetic reproductions, not verbatim excerpts.
    // SameTimeEventOrderTest pins Chroma's equal-time event semantics through the production load path:
    // events sharing a JsonTime apply in authored file order, so the last-in-file event wins. As The World
    // Caves In's intro runway depended on this (a beat-0 x=696969 hide followed by a beat-0 show on
    // TrackConstructionParent1 — the show must win), and unstable equal-time sorting let the hide win.
    // Each test loads its own fixture because the shared teardown restores the empty baseline map.
    public class SameTimeEventOrderTest : TestBase
    {
        private const string TrackName = "sameTimeTrack";
        private const string ParentAName = "sameTimeParentA";
        private const string ParentBName = "sameTimeParentB";
        private const string ParentTrackName = "sameTimeParent";
        private const string ChildTrackName = "sameTimeChild";

        private static IEnumerator LoadMap(int version, JSONNode difficulty)
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");

            yield return TestUtils.ReloadMap(version, difficulty);
        }

        private static JSONNode V3MapJson(JSONArray customEvents) => new JSONObject
        {
            ["version"] = "3.3.0",
            ["customData"] = new JSONObject { ["customEvents"] = customEvents }
        };

        private static JSONNode V2MapJson(
            JSONArray customEvents = null,
            JSONArray events = null,
            JSONArray environment = null)
        {
            var difficulty = new JSONObject
            {
                ["_version"] = "2.6.0",
                ["_events"] = events ?? new JSONArray(),
                ["_notes"] = new JSONArray(),
                ["_obstacles"] = new JSONArray()
            };
            var customData = new JSONObject();
            if (customEvents != null) customData["_customEvents"] = customEvents;
            if (environment != null) customData["_environment"] = environment;
            if (customData.Count > 0) difficulty["_customData"] = customData;
            return difficulty;
        }

        private static JSONArray EventArray(params JSONNode[] events)
        {
            var array = new JSONArray();
            foreach (var ev in events) array.Add(ev);
            return array;
        }

        private static JSONObject V3AnimateTrack(float beat, float x, float y, float z) => new()
        {
            ["b"] = beat,
            ["t"] = "AnimateTrack",
            ["d"] = new JSONObject
            {
                ["track"] = TrackName,
                ["duration"] = 0f,
                ["position"] = JSON.Parse($"[[{x}, {y}, {z}, 0]]")
            }
        };

        private static JSONObject V2AnimateTrack(float beat, float x, float y, float z) =>
            V2AnimateTrackOnTrack(beat, TrackName, x, y, z);

        private static JSONObject V2AnimateTrackOnTrack(float beat, string track, float x, float y, float z) => new()
        {
            ["_time"] = beat,
            ["_type"] = "AnimateTrack",
            ["_data"] = new JSONObject
            {
                ["_track"] = track,
                ["_duration"] = 0f,
                ["_position"] = JSON.Parse($"[[{x}, {y}, {z}, 0]]")
            }
        };

        private static JSONObject V3AssignTrackParent(float beat, string parent, params string[] children)
        {
            var childArray = new JSONArray();
            foreach (var child in children) childArray.Add(child);
            return new()
            {
                ["b"] = beat,
                ["t"] = "AssignTrackParent",
                ["d"] = new JSONObject { ["parentTrack"] = parent, ["childrenTracks"] = childArray }
            };
        }

        private static JSONObject V2AssignTrackParent(float beat, string parent, params string[] children)
        {
            var childArray = new JSONArray();
            foreach (var child in children) childArray.Add(child);
            return new()
            {
                ["_time"] = beat,
                ["_type"] = "AssignTrackParent",
                ["_data"] = new JSONObject { ["_parentTrack"] = parent, ["_childrenTracks"] = childArray }
            };
        }

        private static void AssertVector(Vector3 actual, Vector3 expected, string because)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f), $"{because} (x)");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f), $"{because} (y)");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.001f), $"{because} (z)");
        }

        // Chroma fires same-beat AnimateTrack events in file order and the last one supplies the value
        // (Heck's Property.Init makes the last-applied definition the base at an exact shared start beat).
        [UnityTest]
        public IEnumerator SameTimeAnimateTrackLastInFileWins()
        {
            yield return LoadMap(3, V3MapJson(EventArray(
                V3AnimateTrack(4f, 0f, 0f, 0f),
                V3AnimateTrack(4f, 5f, 7f, 9f))));

            var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack(TrackName);
            var property = (AnimateProperty<Vector3>)track.AnimatedProperties["position"];

            var sameTimeEvents = BeatSaberSongContainer.Instance.Map.CustomEvents
                .Where(ev => ev.JsonTime == 4f)
                .ToList();
            Assert.That(sameTimeEvents.Count, Is.EqualTo(2), "The fixture's two beat-4 events must parse.");

            // The animated property's definitions must keep authored file order after their stable sort.
            Assert.That(property.PointDefinitions.Count, Is.EqualTo(2));
            Assert.That(
                ReferenceEquals(property.PointDefinitions[0].Source, sameTimeEvents[0]),
                Is.True,
                "The file-first beat-4 event must stay first after sorting; unstable ordering scrambles it.");
            Assert.That(
                ReferenceEquals(property.PointDefinitions[1].Source, sameTimeEvents[1]),
                Is.True,
                "The file-last beat-4 event must stay last after sorting.");

            AssertVector(
                property.GetLerpedValue(4f),
                new Vector3(5f, 7f, 9f),
                "At the shared beat the file-last event's value must win, matching Chroma.");
            AssertVector(
                property.GetLerpedValue(6f),
                new Vector3(5f, 7f, 9f),
                "After the shared beat the file-last event's value must hold.");
            AssertVector(
                property.GetLerpedValue(2f),
                Vector3.zero,
                "Before the events the property must hold its default, exactly like a restarted map.");

            yield break;
        }

        // The companion direction: with the same two events authored in the opposite order the other value
        // must win, proving the outcome comes from file order rather than value content.
        [UnityTest]
        public IEnumerator SameTimeAnimateTrackReversedFileOrderFlipsResult()
        {
            yield return LoadMap(3, V3MapJson(EventArray(
                V3AnimateTrack(4f, 5f, 7f, 9f),
                V3AnimateTrack(4f, 0f, 0f, 0f))));

            var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack(TrackName);
            var property = (AnimateProperty<Vector3>)track.AnimatedProperties["position"];

            AssertVector(
                property.GetLerpedValue(4f),
                Vector3.zero,
                "With the zero event authored last it must win at the shared beat.");
            AssertVector(
                property.GetLerpedValue(6f),
                Vector3.zero,
                "With the zero event authored last it must hold after the shared beat.");

            yield break;
        }

        // The reported corruption was a V2 map, so the V2 _position path gets the same pin.
        [UnityTest]
        public IEnumerator SameTimeAnimateTrackLastInFileWinsV2()
        {
            yield return LoadMap(2, V2MapJson(EventArray(
                V2AnimateTrack(4f, 0f, 0f, 0f),
                V2AnimateTrack(4f, 5f, 7f, 9f))));

            var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack(TrackName);
            var property = (AnimateProperty<Vector3>)track.AnimatedProperties["_position"];

            AssertVector(
                property.GetLerpedValue(4f),
                new Vector3(5f, 7f, 9f),
                "At the shared beat the file-last V2 event's value must win, matching Chroma.");
            AssertVector(
                property.GetLerpedValue(6f),
                new Vector3(5f, 7f, 9f),
                "After the shared beat the file-last V2 event's value must hold.");

            yield break;
        }

        // AssignTrackParent applies immediately in collection order, so two same-beat parents for one child
        // leave the child under the file-last parent.
        [UnityTest]
        public IEnumerator SameTimeAssignTrackParentLastInFileWins()
        {
            yield return LoadMap(3, V3MapJson(EventArray(
                V3AssignTrackParent(0f, ParentAName, ChildTrackName),
                V3AssignTrackParent(0f, ParentBName, ChildTrackName))));

            var tracks = Object.FindAnyObjectByType<TracksManager>();
            var child = tracks.GetAnimationTrack(ChildTrackName);
            var parentB = tracks.GetAnimationTrack(ParentBName);

            Assert.That(
                child.Track.transform.parent,
                Is.SameAs(parentB.Track.ObjectParentTransform),
                "The file-last beat-0 AssignTrackParent must win: the child belongs under parent B.");

            yield break;
        }

        [UnityTest]
        public IEnumerator SameTimeAssignTrackParentReversedFileOrderFlipsResult()
        {
            yield return LoadMap(3, V3MapJson(EventArray(
                V3AssignTrackParent(0f, ParentBName, ChildTrackName),
                V3AssignTrackParent(0f, ParentAName, ChildTrackName))));

            var tracks = Object.FindAnyObjectByType<TracksManager>();
            var child = tracks.GetAnimationTrack(ChildTrackName);
            var parentA = tracks.GetAnimationTrack(ParentAName);

            Assert.That(
                child.Track.transform.parent,
                Is.SameAs(parentA.Track.ObjectParentTransform),
                "With parent A authored last the child must end under A.");

            yield break;
        }

        // Same-time _events feed light dispatch in list order: the game applies them in file order, so the
        // loaded list must keep authored order (a value tiebreak would reorder [2,0,7] to [0,2,7] and flip
        // which event lands last on the same light).
        // TODO idk if this IS actually necessary. I rolled that code change back and it seems like the env enhancements may have been the real issue all along, and the sort fix on customevents is irrelevant?
        [UnityTest]
        public IEnumerator SameTimeLightEventsKeepAuthoredFileOrder()
        {
            yield return LoadMap(2, V2MapJson(events: JSON.Parse(@"[
                { ""_time"": 5, ""_type"": 8, ""_value"": 2, ""_floatValue"": 1 },
                { ""_time"": 5, ""_type"": 8, ""_value"": 0, ""_floatValue"": 1 },
                { ""_time"": 5, ""_type"": 8, ""_value"": 7, ""_floatValue"": 1 }
            ]").AsArray));

            var values = BeatSaberSongContainer.Instance.Map.Events
                .Where(e => e.JsonTime == 5f && e.Type == 8)
                .Select(e => e.Value)
                .ToList();

            Assert.That(
                values,
                Is.EqualTo(new List<int> { 2, 0, 7 }).AsCollection,
                "Same-time light events must keep authored file order; the last-applied event wins in game.");

            yield break;
        }

        // End-to-end through the real animation chain: a _geometry enhancement bound to the child track
        // gives it an enabled animator (primitive-geometry attach has no AnimationMode gate, unlike gameplay
        // objects), so the parent track's beat-4 same-time events push into the child track's transform.
        // Seeking forward over the shared beat must land on the file-last value, seeking back must restore
        // the pre-event state, and re-seeking must reproduce the same result.
        [UnityTest]
        public IEnumerator SeekingAcrossSameTimeEventsAppliesFileLastResult()
        {
            var environment = EventArray(new JSONObject
            {
                ["_geometry"] = new JSONObject { ["_type"] = "Cube", ["_material"] = "standard" },
                ["_track"] = ChildTrackName
            });
            yield return LoadMap(2, V2MapJson(
                EventArray(
                    V2AssignTrackParent(0f, ParentTrackName, ChildTrackName),
                    V2AnimateTrackOnTrack(4f, ParentTrackName, 1f, 0f, 0f),
                    V2AnimateTrackOnTrack(4f, ParentTrackName, 3f, 0f, 0f)),
                environment: environment));

            // Allow container spawn and the parent/child animator enable chain to settle.
            yield return null;
            yield return null;

            var tracks = Object.FindAnyObjectByType<TracksManager>();
            var child = tracks.GetAnimationTrack(ChildTrackName);
            var parent = tracks.GetAnimationTrack(ParentTrackName);
            var expectedLastWins = 3f * BeatmapConstant.LaneSize; // V2 _position is lane-scaled.

            Assert.That(
                parent.CachedChildren.Length,
                Is.EqualTo(1),
                "The parent track must receive the child track's animator so its events push somewhere.");
            Assert.That(
                child.Track.ObjectParentTransform.localPosition.x,
                Is.EqualTo(0f).Within(0.001f),
                "At map start the beat-4 events must not have applied.");

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            atsc.MoveToJsonTime(6f);
            yield return null;
            Assert.That(
                child.Track.ObjectParentTransform.localPosition.x,
                Is.EqualTo(expectedLastWins).Within(0.01f),
                "Seeking over the shared beat must land on the file-last event's value.");

            atsc.MoveToJsonTime(2f);
            yield return null;
            Assert.That(
                child.Track.ObjectParentTransform.localPosition.x,
                Is.EqualTo(0f).Within(0.001f),
                "Seeking back before the shared beat must restore the un-animated state.");

            atsc.MoveToJsonTime(6f);
            yield return null;
            Assert.That(
                child.Track.ObjectParentTransform.localPosition.x,
                Is.EqualTo(expectedLastWins).Within(0.01f),
                "Re-seeking must reproduce the file-last result deterministically.");

            yield break;
        }

        // The whole chain end to end: authored order must survive save output, and a reload of the saved
        // file must evaluate to the same last-in-file result.
        [UnityTest]
        public IEnumerator SameTimeEventOrderSurvivesSaveAndReload()
        {
            yield return LoadMap(2, V2MapJson(EventArray(
                V2AnimateTrack(4f, 1f, 0f, 0f),
                V2AnimateTrack(4f, 3f, 0f, 0f))));

            var saved = Beatmap.V2.V2Difficulty.GetOutputJson(BeatSaberSongContainer.Instance.Map);
            var savedXs = saved["_customData"]["_customEvents"].AsArray.Children
                .Where(n => n["_type"].Value == "AnimateTrack" && n["_time"].AsFloat == 4f)
                .Select(n => n["_data"]["_position"].AsArray[0].AsArray[0].AsFloat)
                .ToList();
            Assert.That(
                savedXs,
                Is.EqualTo(new List<float> { 1f, 3f }).AsCollection,
                "Saved output must emit the same-time events in authored file order.");

            yield return LoadMap(2, saved);

            var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack(TrackName);
            var property = (AnimateProperty<Vector3>)track.AnimatedProperties["_position"];
            AssertVector(
                property.GetLerpedValue(6f),
                new Vector3(3f, 0f, 0f),
                "A save/reload round-trip must preserve the file-last same-time winner.");

            yield break;
        }

        // Restore the canonical empty shared map so later fixtures do not inherit this fixture. Once per
        // class is enough: every test loads its own map anyway.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

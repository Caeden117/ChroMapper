using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Animations;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class AnimationPropertyVersionParityTest : PreviewWorkflowTestBase
    {
        [UnityTest]
        public IEnumerator LegacyTrackPropertiesMatchHeckAliases()
        {
            yield return CheckTrackProperties(2);
        }

        [UnityTest]
        public IEnumerator CurrentTrackPropertiesMatchHeckNames()
        {
            yield return CheckTrackProperties(3);
        }

        private static IEnumerator CheckTrackProperties(int version)
        {
            var data = AnimationProperties(version);
            data[Key(version, "track")] = "versionProperties";
            data[Key(version, "duration")] = 4;
            data[Key(version, "time")] = JSON.Parse("[0.25]");
            yield return Load(version, null, Event(version, "AnimateTrack", data));
            var properties = Object.FindAnyObjectByType<TracksManager>()
                .GetAnimationTrack("versionProperties").AnimatedProperties;
            CheckProperties(properties, version, "", 2f);
            var interactable = (AnimateProperty<float>)properties[Key(version, "interactable")];
            Assert.That(interactable.GetLerpedValue(0f), Is.EqualTo(1f));
            Assert.That(interactable.GetLerpedValue(2f), Is.EqualTo(0.5f));
            Assert.That(interactable.GetLerpedValue(5f), Is.EqualTo(0f));
            Assert.That(((AnimateProperty<float>)properties[Key(version, "time")]).GetLerpedValue(2f),
                Is.EqualTo(0.25f));
        }

        [UnityTest]
        public IEnumerator LegacyIndividualAndPathPropertiesMatchHeckAliases()
        {
            yield return CheckObjectProperties(2);
        }

        [UnityTest]
        public IEnumerator CurrentIndividualAndPathPropertiesMatchHeckNames()
        {
            yield return CheckObjectProperties(3);
        }

        private static IEnumerator CheckObjectProperties(int version)
        {
            Settings.Instance.Animations = true;
            var path = AnimationProperties(version);
            path[Key(version, "track")] = "versionPath";
            path[Key(version, "definitePosition")] = JSON.Parse("[3,4,5]");
            var individual = AnimationProperties(version);
            individual[Key(version, "definitePosition")] = JSON.Parse("[3,4,5]");
            var notes = new JSONArray();
            notes.Add(Note(version, "versionPath", null));
            notes.Add(Note(version, "versionIndividual", individual));
            yield return Load(version, notes, Event(version, "AssignPathAnimation", path));
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(19.99f);
            yield return null;
            foreach (var note in Object.FindObjectsByType<NoteContainer>(FindObjectsSortMode.None))
            {
                var prefix = note.NoteData.CustomTrack.Value == "versionPath" ? "track_" : "";
                var properties = note.Animator.AnimatedProperties;
                CheckProperties(properties, version, prefix, atsc.CurrentSongBpmTime);
                Assert.That(((AnimateProperty<Vector3>)properties[prefix + Key(version, "definitePosition")])
                    .GetLerpedValue(atsc.CurrentSongBpmTime), Is.EqualTo(new Vector3(3, 4, 5)));
            }
            Assert.That(Object.FindObjectsByType<NoteContainer>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator LegacyPlayerTargetSkipsHandsAndDefaultsToRoot()
        {
            yield return CheckPlayerTargets(2);
        }

        [UnityTest]
        public IEnumerator CurrentPlayerTargetSkipsHandsAndDefaultsToRoot()
        {
            yield return CheckPlayerTargets(3);
        }

        private static IEnumerator CheckPlayerTargets(int version)
        {
            var hand = new JSONObject
            {
                [Key(version, "track")] = "versionHand",
                [Key(version, "target")] = "LeftHand",
                [version == 2 ? "target" : "_target"] = "Root"
            };
            var root = new JSONObject { [Key(version, "track")] = "versionRoot" };
            yield return Load(version, null,
                Event(version, "AssignPlayerToTrack", hand), Event(version, "AssignPlayerToTrack", root));
            var cameras = Object.FindObjectsByType<CameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var player = cameras.Single(camera => (bool)typeof(CameraController)
                .GetField("playerCamera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(camera));
            var tracks = (List<TrackAnimator>)typeof(CameraController)
                .GetField("playerTracks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            Assert.That(tracks.Count, Is.EqualTo(1),
                $"V{version} hand target was ignored and its track incorrectly bound the whole preview camera.");
            Assert.That(tracks[0].Track.name, Is.EqualTo("versionRoot"));
        }

        // Heck aliases _position/_rotation to gameplay offsets, rather than the current transform names.
        private static JSONObject AnimationProperties(int version) => new()
        {
            [OffsetKey(version)] = JSON.Parse("[1,2,3]"),
            [RotationKey(version)] = JSON.Parse("[10,20,30]"),
            [Key(version, "localRotation")] = JSON.Parse("[30,20,10]"),
            [Key(version, "scale")] = JSON.Parse("[2,3,4]"),
            [Key(version, "color")] = JSON.Parse("[0.2,0.3,0.4,0.5]"),
            [Key(version, "dissolve")] = JSON.Parse("[0.6]"),
            [Key(version, "dissolveArrow")] = JSON.Parse("[0.7]"),
            [Key(version, "interactable")] = JSON.Parse("[[1,0],[0,1]]")
        };

        private static void CheckProperties(Dictionary<string, IAnimateProperty> properties, int version,
            string prefix, float time)
        {
            var interactable = prefix + Key(version, "interactable");
            Assert.That(properties.ContainsKey(interactable), Is.True,
                $"V{version} {interactable} was silently dropped.");
            Assert.That(((AnimateProperty<float>)properties[interactable]).GetLerpedValue(time),
                Is.InRange(0f, 1f));
            Assert.That(((AnimateProperty<Vector3>)properties[prefix + OffsetKey(version)]).GetLerpedValue(time),
                Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(Quaternion.Angle(((AnimateProperty<Quaternion>)properties[prefix + RotationKey(version)])
                .GetLerpedValue(time), Quaternion.Euler(10, 20, 30)), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(((AnimateProperty<Quaternion>)properties[prefix + Key(version, "localRotation")])
                .GetLerpedValue(time), Quaternion.Euler(30, 20, 10)), Is.LessThan(0.01f));
            Assert.That(((AnimateProperty<Vector3>)properties[prefix + Key(version, "scale")]).GetLerpedValue(time),
                Is.EqualTo(new Vector3(2, 3, 4)));
            Assert.That(((AnimateProperty<Color>)properties[prefix + Key(version, "color")]).GetLerpedValue(time),
                Is.EqualTo(new Color(0.2f, 0.3f, 0.4f, 0.5f)));
            Assert.That(((AnimateProperty<float>)properties[prefix + Key(version, "dissolve")]).GetLerpedValue(time),
                Is.EqualTo(0.6f));
            Assert.That(((AnimateProperty<float>)properties[prefix + Key(version, "dissolveArrow")]).GetLerpedValue(time),
                Is.EqualTo(0.7f));
        }

        private static string Key(int version, string name) => version == 2 ? "_" + name : name;
        private static string OffsetKey(int version) => version == 2 ? "_position" : "offsetPosition";
        private static string RotationKey(int version) => version == 2 ? "_rotation" : "offsetWorldRotation";

        private static JSONObject Note(int version, string track, JSONObject animation)
        {
            var custom = new JSONObject
            {
                [Key(version, "track")] = track,
                [Key(version, "noteJumpStartBeatOffset")] = 10
            };
            if (animation != null)
            {
                custom[Key(version, "animation")] = animation;
            }
            return version == 2
                ? new JSONObject
                {
                    ["_time"] = 20, ["_lineIndex"] = 0, ["_lineLayer"] = 0, ["_type"] = 0,
                    ["_cutDirection"] = 8, ["_customData"] = custom
                }
                : new JSONObject
                {
                    ["b"] = 20, ["x"] = 0, ["y"] = 0, ["c"] = 0, ["d"] = 8, ["customData"] = custom
                };
        }

        private static JSONObject Event(int version, string type, JSONObject data) => version == 2
            ? new JSONObject { ["_time"] = 0, ["_type"] = type, ["_data"] = data }
            : new JSONObject { ["b"] = 0, ["t"] = type, ["d"] = data };

        private static IEnumerator Load(int version, JSONArray notes, params JSONObject[] events)
        {
            var eventArray = new JSONArray();
            foreach (var ev in events)
            {
                eventArray.Add(ev);
            }
            var map = new JSONObject
            {
                [Key(version, "version")] = version == 2 ? "2.2.0" : "3.2.0",
                [Key(version, "customData")] = new JSONObject { [Key(version, "customEvents")] = eventArray }
            };
            if (notes != null)
            {
                map[version == 2 ? "_notes" : "colorNotes"] = notes;
            }
            yield return TestUtils.ReloadMap(version, map);
        }

        [UnityTearDown]
        public IEnumerator RestoreEmptyMap()
        {
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

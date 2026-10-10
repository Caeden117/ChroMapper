using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class AnimationEventMutationTest : PreviewWorkflowTestBase
    {
        private static readonly string[] Targets =
        {
            "2:Note:AnimateTrack", "2:Obstacle:AnimateTrack", "2:Note:AssignPathAnimation",
            "2:Obstacle:AssignPathAnimation", "3:Note:AnimateTrack", "3:Obstacle:AnimateTrack",
            "3:Arc:AnimateTrack", "3:Chain:AnimateTrack", "3:Note:AssignPathAnimation",
            "3:Obstacle:AssignPathAnimation", "3:Arc:AssignPathAnimation", "3:Chain:AssignPathAnimation",
            "2:Geometry:AnimateTrack", "3:Geometry:AnimateTrack", "2:Environment:AnimateTrack",
            "3:Environment:AnimateTrack", "2:Parent:AnimateTrack", "3:Parent:AnimateTrack",
            "2:Player:AnimateTrack", "3:Player:AnimateTrack", "2:Material:AnimateTrack",
            "3:Material:AnimateTrack", "2:Fog:AnimateTrack", "3:Fog:AnimateComponent",
            "3:Tube:AnimateComponent", "2:Note:AnimateTrack:Scheme", "3:Note:AnimateTrack:Scheme",
            "3:Obstacle:AnimateTrack:Scheme", "3:Arc:AnimateTrack:Scheme", "3:Chain:AnimateTrack:Scheme"
        };

        private AudioTimeSyncController atsc;
        private BeatmapActionContainer actions;
        private int version;
        private string target;
        private string eventType;
        private bool assignmentCase;
        private bool schemeColor;
        private bool playbackCase;
        private bool clockFrozen;
        private bool previousClockEnabled;
        private static readonly PropertyInfo PlaybackSeconds = typeof(AudioTimeSyncController)
            .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
        private static IEnumerable<string> TargetCases => Targets.SelectMany(name => new[] { name, name + ":Playback" });
        private static IEnumerable<string> AssignmentCases => Assignments.SelectMany(name => new[] { name, name + ":Playback" });

        private static readonly string[] RotationTargets = { "2:Geometry", "3:Geometry", "2:Environment", "3:Environment" };

        [UnityTest]
        public IEnumerator TransformLocalRotationTakesPrecedenceOverWorldRotation([ValueSource(nameof(RotationTargets))] string scenario)
        {
            var parts = scenario.Split(':');
            version = int.Parse(parts[0]);
            target = parts[1];
            eventType = "AnimateTrack";
            assignmentCase = false;
            playbackCase = false;
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(version, Fixture(), environmentName: "BillieEnvironment");
            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            var data = Properties(1);
            var ev = Event(4, eventType, data);
            BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(ev, new BaseObject[0], scenario), true);
            yield return Sample();
            var transform = target == "Geometry" ? Geometry().Animator.LocalTarget : EnvironmentTarget();
            Assert.That(Quaternion.Angle(transform.localRotation, Quaternion.Euler(15, 25, 35)), Is.LessThan(0.01f),
                "Heck TransformController writes localRotation when present, otherwise world rotation. It does not multiply both.");
            var worldOnly = (JSONObject)data.Clone();
            worldOnly.Remove(Key("localRotation"));
            Update(ev, Event(4, eventType, worldOnly));
            yield return Sample();
            Assert.That(Quaternion.Angle(transform.rotation, Quaternion.Euler(10, 20, 30)), Is.LessThan(0.01f));
            Undo();
            yield return Sample();
            Assert.That(Quaternion.Angle(transform.localRotation, Quaternion.Euler(15, 25, 35)), Is.LessThan(0.01f));
        }

        private static readonly string[] Assignments =
        {
            "2:Parent:AssignTrackParent", "3:Parent:AssignTrackParent",
            "2:Player:AssignPlayerToTrack", "3:Player:AssignPlayerToTrack", "2:Fog:AssignFogTrack"
        };

        [UnityTest]
        public IEnumerator AssignmentEditDeleteUndoRedoRestoresBindings([ValueSource(nameof(AssignmentCases))] string scenario)
        {
            var parts = scenario.Split(':');
            version = int.Parse(parts[0]);
            target = parts[1];
            eventType = parts[2];
            assignmentCase = true;
            schemeColor = false;
            playbackCase = parts.Contains("Playback");
            Settings.Instance.Animations = true;
            var fixture = Fixture();
            var events = fixture[Key("customData")][Key("customEvents")].AsArray;
            events.Add(EventJson(0, "AnimateTrack", Properties(1)));
            yield return TestUtils.ReloadMap(version, fixture, environmentName: "BillieEnvironment");
            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actions.ClearBeatmapActions();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            yield return Sample();
            var baseline = Capture();
            var assignment = Event(4, eventType, Assignment("mutation"));
            BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(assignment, new BaseObject[0], scenario), true);
            yield return Sample();
            var assigned = Capture();
            AssertDifferent(assigned, baseline, "assigning the target to an animated track");
            var edited = Event(4, eventType, Assignment("unusedMutationTrack"));
            Update(assignment, edited);
            yield return Sample();
            AssertState(baseline, "editing the assignment onto an unanimated track");
            Undo();
            yield return Sample();
            AssertState(assigned, "undoing the assignment edit");
            Redo();
            yield return Sample();
            AssertState(baseline, "redoing the assignment edit");
            Undo();
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(assignment, scenario), true);
            yield return Sample();
            AssertState(baseline, "deleting the only assignment event");
            for (var i = 0; i < 2; ++i)
            {
                Undo();
                yield return Sample();
                AssertState(assigned, "undoing assignment deletion");
                Redo();
                yield return Sample();
                AssertState(baseline, "redoing assignment deletion");
            }
        }

        private JSONObject Assignment(string name) => target == "Parent"
            ? new JSONObject { [Key("childrenTracks")] = "mutationChild", [Key("parentTrack")] = name }
            : new JSONObject { [Key("track")] = name };

        // Use the same editor action stack as node edits and deletion. Direct animator.RemoveEvent calls
        // cannot catch stale path caches, assignment lists, or track routing in CustomEventGridContainer.
        [UnityTest]
        public IEnumerator EditDeleteUndoRedoRestoresEveryAnimatedTarget([ValueSource(nameof(TargetCases))] string scenario)
        {
            var parts = scenario.Split(':');
            version = int.Parse(parts[0]);
            target = parts[1];
            eventType = parts[2];
            assignmentCase = false;
            schemeColor = parts.Contains("Scheme");
            playbackCase = parts.Contains("Playback");
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(version, Fixture(), environmentName: "BillieEnvironment");
            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actions.ClearBeatmapActions();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            yield return Sample();
            var baseline = Capture();

            var original = Event(4, eventType, Properties(1));
            BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(original, new BaseObject[0], scenario), true);
            yield return Sample();
            var animated = Capture();
            AssertDifferent(animated, baseline, "placing the first event");
            Undo();
            yield return Sample();
            AssertState(baseline, "undoing placement of the only event");
            Redo();
            yield return Sample();
            AssertState(animated, "redoing placement");

            var edited = Event(4, eventType, Properties(2));
            Update(original, edited);
            yield return Sample();
            var changed = Capture();
            AssertDifferent(changed, animated, "editing the event's values");
            Undo();
            yield return Sample();
            AssertState(animated, "undoing the value edit");
            Redo();
            yield return Sample();
            AssertState(changed, "redoing the value edit");

            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(edited, scenario), true);
            yield return Sample();
            AssertState(baseline, "deleting the last event that modified the target");
            for (var i = 0; i < 2; ++i)
            {
                Undo();
                yield return Sample();
                AssertState(changed, "undoing deletion");
                Redo();
                yield return Sample();
                AssertState(baseline, "redoing deletion");
            }

            Undo();
            var moved = Event(40, eventType, Properties(2));
            Update(edited, moved);
            yield return Sample();
            AssertState(baseline, "moving the only event after the current playhead");
            Undo();
            yield return Sample();
            AssertState(changed, "undoing the time edit");
            Redo();
            yield return Sample();
            AssertState(baseline, "redoing the time edit");
            Undo();

            var detachedData = Properties(2);
            detachedData[Key("track")] = "unusedMutationTrack";
            var detached = Event(4, eventType, detachedData);
            Update(edited, detached);
            yield return Sample();
            AssertState(baseline, "editing the event onto another track");
            Undo();
            yield return Sample();
            AssertState(changed, "undoing the track edit");
            Redo();
            yield return Sample();
            AssertState(baseline, "redoing the track edit");
            Undo();

            var partialData = Properties(2);
            var removed = target switch { "Fog" => "attenuation", "Tube" => "colorAlphaMultiplier",
                "Material" => "color", _ => "scale" };
            var component = target == "Fog" && version == 3
                ? partialData["BloomFogEnvironment"]
                : target == "Tube" ? partialData["TubeBloomPrePassLight"] : partialData;
            component.Remove(Key(removed));
            var partial = Event(4, eventType, partialData);
            Update(edited, partial);
            yield return Sample();
            AssertRemovedProperty(baseline, changed);
            Undo();
            yield return Sample();
            AssertState(changed, "undoing removal of just one animated property");
            Redo();
            yield return Sample();
            AssertRemovedProperty(baseline, changed);
            Undo();

            var empty = Event(4, eventType, new JSONObject { [Key("track")] = "mutation" });
            Update(edited, empty);
            yield return Sample();
            AssertState(baseline, "removing the event's animated properties");
            Undo();
            yield return Sample();
            AssertState(changed, "undoing property removal");
            Redo();
            yield return Sample();
            AssertState(baseline, "redoing property removal");
            Undo();

            var latest = Event(10, eventType, Properties(1));
            BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(latest, new BaseObject[0], scenario), true);
            yield return Sample();
            var latestState = Capture();
            AssertDifferent(latestState, changed, "adding an overriding event");
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(latest, scenario), true);
            yield return Sample();
            AssertState(changed, "deleting the latest event while retaining an earlier one");
            Undo();
            yield return Sample();
            AssertState(latestState, "undoing deletion of the overriding event");
            Redo();
            yield return Sample();
            AssertState(changed, "redoing deletion of the overriding event");
        }

        private IEnumerator Sample()
        {
            if (playbackCase)
            {
                if (!atsc.IsPlaying)
                {
                    atsc.MoveToJsonTime(19.5f);
                    atsc.TogglePlaying();
                    atsc.SongAudioSource.Stop();
                    atsc.StopScheduled = true;
                    previousClockEnabled = atsc.enabled;
                    clockFrozen = true;
                    atsc.enabled = false;
                }

                PlaybackSeconds.SetValue(atsc, atsc.GetSecondsFromBeat(19.5f));
            }
            else
                atsc.MoveToJsonTime(19.5f);

            yield return null;
            yield return null;
            if (playbackCase)
            {
                Assert.That(atsc.IsPlaying, Is.True);
                Assert.That(atsc.SongAudioSource.isPlaying, Is.False);
            }
        }

        private void Update(BaseCustomEvent original, BaseCustomEvent edited) => BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(edited, original, "Edit animation event"), true);

        private void Undo() => Assert.That(actions.Undo(), Is.Not.Null);
        private void Redo() => Assert.That(actions.Redo(), Is.Not.Null);

        private float[] Capture()
        {
            if (target == "Fog")
            {
                var fog = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.BloomFogParams;
                return new[] { fog.Attenuation, fog.Offset, fog.Height, fog.StartY,
                    Shader.GetGlobalFloat("_CustomFogAttenuation"), Shader.GetGlobalFloat("_CustomFogOffset"),
                    Shader.GetGlobalFloat("_CustomFogHeightFogHeight"), Shader.GetGlobalFloat("_CustomFogHeightFogStartY") };
            }

            if (target == "Tube")
            {
                var light = Geometry().GetComponentInChildren<ParametricBloomFogLightController>(true);
                Assert.That(light.HasInitialized, Is.True);
                return new[] { light.ColorAlphaMultiplier, light.BloomFogIntensityMultiplier,
                    light.BoxLight.AlphaMultiplier, light.BloomFog.IntensityMultiplier };
            }

            ObjectContainer container = null;
            Transform local;
            Transform world;
            if (target is "Geometry" or "Material" or "Parent")
            {
                container = Geometry();
                local = container.Animator.LocalTarget;
                world = container.Animator.WorldTarget;
            }
            else if (target == "Environment")
            {
                local = EnvironmentTarget();
                world = local;
            }
            else if (target == "Player")
            {
                local = Object.FindAnyObjectByType<CameraManager>().SelectedCameraController.transform;
                world = local;
            }
            else
            {
                var kind = System.Enum.Parse<ObjectType>(target);
                var collection = BeatmapObjectContainerCollection.GetCollectionForType(kind);
                Assert.That(collection.LoadedContainers.Count, Is.EqualTo(1), "The animated gameplay target must be visible.");
                container = collection.LoadedContainers.Values.Single();
                local = container.Animator.LocalTarget;
                world = container.Animator.WorldTarget;
            }

            var values = new List<float>();
            Vector(local.position);
            Vector(local.lossyScale);
            Quaternion(local.rotation);
            Quaternion(world.rotation);
            if (container != null)
            {
                var color = container.MpbController.Mpb.GetColor("_Color");
                values.AddRange(new[] { color.r, color.g, color.b, color.a,
                    container.MpbController.Mpb.GetFloat("_Cutout") });
                if (container is NoteContainer note)
                    values.Add(note.ArrowMpbController.Mpb.GetFloat("_Cutout"));

                if (target is "Note" or "Obstacle" or "Arc" or "Chain")
                    Vector(container.transform.position);
            }

            return values.ToArray();

            void Vector(Vector3 value) => values.AddRange(new[] { value.x, value.y, value.z });
            void Quaternion(Quaternion value) => values.AddRange(new[] { value.x, value.y, value.z, value.w });
        }

        private GeometryContainer Geometry() => Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
            .Single(item => item.EnvironmentEnhancement?.Geometry != null);

        private static Transform EnvironmentTarget() => Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.ChromaIDMarkers
            .Single(marker => marker.ChromaID.EndsWith(".[46]BottomPairLasers.[0]PillarL")).transform;

        private void AssertState(float[] expected, string operation)
        {
            var actual = Capture();
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            var failures = new List<string>();
            for (var i = 0; i < expected.Length; ++i)
                if (float.IsNaN(actual[i]) || float.IsInfinity(actual[i])
                    || Mathf.Abs(actual[i] - expected[i]) > 0.00001f * Mathf.Max(1f, Mathf.Abs(expected[i])))
                    failures.Add($"output[{i}]: expected {expected[i]}, actual {actual[i]}");

            Assert.That(failures, Is.Empty, $"V{version} {target} {eventType}: {operation}\n" + string.Join("\n", failures));
        }

        private static void AssertDifferent(float[] actual, float[] baseline, string operation) =>
            Assert.That(actual.Where((value, i) => Mathf.Abs(value - baseline[i]) > 0.0001f), Is.Not.Empty,
                $"The test target did not change when {operation}; the restoration check would be vacuous.");

        private void AssertRemovedProperty(float[] baseline, float[] changed)
        {
            var actual = Capture();
            var indexes = target switch { "Fog" => new[] { 0, 4 }, "Tube" => new[] { 0, 2 },
                "Material" => new[] { 14, 15, 16, 17 }, _ => new[] { 3, 4, 5 } };
            foreach (var i in indexes)
                Assert.That(actual[i], Is.EqualTo(baseline[i]).Within(0.0001f),
                    $"{target}: removing one property retained its output[{i}] instead of the authored baseline.");

            AssertDifferent(actual, baseline, "retaining the event's other animated properties");
            AssertDifferent(actual, changed, "removing just one animated property");
        }

        private JSONObject Properties(int variant)
        {
            var data = new JSONObject { [Key("track")] = "mutation" };
            if (target == "Fog")
            {
                var values = new JSONObject
                {
                    [Key("attenuation")] = JSON.Parse(variant == 1 ? "[0.001]" : "[0.003]"),
                    [Key("offset")] = JSON.Parse(variant == 1 ? "[20]" : "[40]"),
                    [Key("height")] = JSON.Parse(variant == 1 ? "[70]" : "[90]"),
                    [Key("startY")] = JSON.Parse(variant == 1 ? "[5]" : "[10]")
                };
                if (version == 2)
                    foreach (var item in values)
                        data[item.Key] = item.Value;
                else
                    data["BloomFogEnvironment"] = values;

                return data;
            }

            if (target == "Tube")
            {
                data["TubeBloomPrePassLight"] = new JSONObject
                {
                    ["colorAlphaMultiplier"] = JSON.Parse(variant == 1 ? "[2]" : "[4]"),
                    ["bloomFogIntensityMultiplier"] = JSON.Parse(variant == 1 ? "[7]" : "[9]")
                };
                return data;
            }

            if (target != "Material")
            {
                var gameplay = target is "Note" or "Obstacle" or "Arc" or "Chain";
                data[gameplay && version != 2 ? "offsetPosition" : Key("position")] =
                    JSON.Parse(variant == 1 ? "[3,2,1]" : "[6,4,2]");
                data[gameplay && version != 2 ? "offsetWorldRotation" : Key("rotation")] =
                    JSON.Parse(variant == 1 ? "[10,20,30]" : "[20,40,60]");
                data[Key("localRotation")] = JSON.Parse(variant == 1 ? "[15,25,35]" : "[30,50,70]");
                data[Key("scale")] = JSON.Parse(variant == 1 ? "[2,3,4]" : "[3,4,5]");
            }

            if (target is "Material" or "Note" or "Obstacle" or "Arc" or "Chain")
            {
                data[Key("color")] = JSON.Parse(variant == 1 ? "[0.13,0.35,0.71,0.8]" : "[0.7,0.2,0.3,0.6]");
                data[Key("dissolve")] = JSON.Parse(variant == 1 ? "[0.3]" : "[0.7]");
                if (target == "Note")
                    data[Key("dissolveArrow")] = JSON.Parse(variant == 1 ? "[0.2]" : "[0.6]");

                if (target != "Material")
                {
                    data[Key("interactable")] = JSON.Parse(variant == 1 ? "[0.2]" : "[0.8]");
                    if (eventType == "AnimateTrack")
                        data[Key("time")] = JSON.Parse(variant == 1 ? "[0.4]" : "[0.6]");
                    else
                        data[Key("definitePosition")] = JSON.Parse(variant == 1 ? "[3,2,1]" : "[6,4,2]");
                }
            }

            return data;
        }

        private JSONObject Fixture()
        {
            var environment = new JSONArray();
            var events = new JSONArray();
            var custom = new JSONObject { [Key("environment")] = environment, [Key("customEvents")] = events };
            var map = new JSONObject { [Key("version")] = version == 2 ? "2.6.0" : "3.3.0", [Key("customData")] = custom };
            if (target is "Note" or "Obstacle" or "Arc" or "Chain")
            {
                var objectCustom = new JSONObject { [Key("track")] = "mutation", [Key("noteJumpStartBeatOffset")] = 20,
                    [Key("color")] = JSON.Parse("[0.25,0.5,0.75,1]"), [Key("disableNoteLook")] = true };
                if (schemeColor)
                    objectCustom.Remove(Key("color"));
                var note = version == 2
                    ? JSON.Parse("{\"_time\":20,\"_lineIndex\":0,\"_lineLayer\":0,\"_type\":0,\"_cutDirection\":8}")
                    : JSON.Parse("{\"b\":20,\"x\":0,\"y\":0,\"c\":0,\"d\":8,\"tb\":22,\"tx\":1,\"ty\":1,\"tc\":8,\"sc\":4,\"s\":1}");
                if (target == "Obstacle")
                    note = version == 2
                        ? JSON.Parse("{\"_time\":20,\"_lineIndex\":0,\"_type\":0,\"_duration\":5,\"_width\":1}")
                        : JSON.Parse("{\"b\":20,\"x\":0,\"y\":0,\"d\":5,\"w\":1,\"h\":5}");

                note[Key("customData")] = objectCustom;
                var objects = new JSONArray();
                objects.Add(note);
                map[target switch { "Note" => version == 2 ? "_notes" : "colorNotes", "Obstacle" => Key("obstacles"),
                    "Arc" => "sliders", _ => "burstSliders" }] = objects;
            }
            else if (target is "Geometry" or "Material" or "Parent" or "Tube")
            {
                var geometry = new JSONObject { [Key("geometry")] = new JSONObject { [Key("type")] = "Cube",
                    [Key("material")] = target == "Material" ? "mutationMaterial" : "standard" },
                    [Key("track")] = target == "Parent" ? "mutationChild" : "mutation" };
                if (target == "Material")
                    custom[Key("materials")] = new JSONObject { ["mutationMaterial"] = new JSONObject
                        { [Key("shader")] = "Standard", [Key("track")] = "mutation", [Key("color")] = JSON.Parse("[0.25,0.5,0.75,1]") } };

                if (target == "Tube")
                    geometry["components"] = JSON.Parse("{\"ILightWithId\":{\"type\":1,\"lightID\":9999},\"TubeBloomPrePassLight\":{\"colorAlphaMultiplier\":1,\"bloomFogIntensityMultiplier\":3}}");

                environment.Add(geometry);
                if (target == "Parent" && !assignmentCase)
                    events.Add(EventJson(0, "AssignTrackParent", new JSONObject { [Key("childrenTracks")] = "mutationChild", [Key("parentTrack")] = "mutation" }));
            }
            else if (target == "Environment")
                environment.Add(new JSONObject { [Key("id")] = ".[46]BottomPairLasers.[0]PillarL", [Key("lookupMethod")] = "EndsWith", [Key("track")] = "mutation" });
            else if (target == "Player" && !assignmentCase)
                events.Add(EventJson(0, "AssignPlayerToTrack", new JSONObject { [Key("track")] = "mutation" }));
            else if (target == "Fog" && version == 2 && !assignmentCase)
                events.Add(EventJson(0, "AssignFogTrack", new JSONObject { [Key("track")] = "mutation" }));
            else if (target == "Fog" && version == 3)
                environment.Add(new JSONObject { ["id"] = "[0]Environment", ["lookupMethod"] = "EndsWith", ["track"] = "mutation" });

            return map;
        }

        private string Key(string name) => version == 2 ? "_" + name : name;
        private JSONObject EventJson(float beat, string type, JSONObject data) => new()
        {
            [version == 2 ? "_time" : "b"] = beat, [version == 2 ? "_type" : "t"] = type,
            [version == 2 ? "_data" : "d"] = data
        };
        private BaseCustomEvent Event(float beat, string type, JSONObject data) => new(EventJson(beat, type, data));

        [UnityTearDown]
        public IEnumerator RestoreEmptyMap()
        {
            if (atsc != null && atsc.IsPlaying)
                atsc.CancelPlaying();

            if (clockFrozen && atsc != null)
                atsc.enabled = previousClockEnabled;

            clockFrozen = false;
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            if (actions != null)
                actions.ClearBeatmapActions();

            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

using System.Collections;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class VivifyEventMutationTest : TestBase
    {
        private const string Global = "_VivifyMutationValue";
        private VivifyTestAssets assets;
        private CustomEventManager manager;
        private CustomEventStateManager state;
        private AudioTimeSyncController clock;
        private BeatmapActionContainer actions;
        private bool clockWasEnabled;
        private float originalGlobal;
        private float originalShadowDistance;
        private bool originalAnimations;
        private static readonly PropertyInfo Seconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
        private static readonly bool[] PlaybackCases = { false, true };

        [UnitySetUp]
        public IEnumerator LoadSyntheticPrefabFixture()
        {
            originalAnimations = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(3, JSON.Parse("{\"version\":\"3.3.0\",\"colorNotes\":[{\"b\":10,\"x\":0,\"y\":0,\"c\":0,\"d\":1,\"customData\":{\"animation\":{\"definitePosition\":[[0,0,5,0],[0,0,0,1]]}}}]}"));
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            manager = Object.FindAnyObjectByType<CustomEventManager>();
            state = Object.FindAnyObjectByType<CustomEventStateManager>();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clockWasEnabled = clock.enabled;
            clock.enabled = false;
            actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actions.ClearBeatmapActions();
            originalGlobal = Shader.GetGlobalFloat(Global);
            originalShadowDistance = QualitySettings.shadowDistance;
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            assets = new VivifyTestAssets(state.VivifyAssetBundleManager);
            assets.CreatePrefab(clock);
            Place(Event(0, "InstantiatePrefab", "{\"asset\":\"" + VivifyTestAssets.PrefabPath + "\",\"id\":\"subject\"}"));
            Place(Event(1, "SetAnimatorProperty", "{\"id\":\"subject\",\"properties\":[{\"id\":\"Kill\",\"type\":\"Trigger\"}]}"));
            clock.MoveToJsonTime(5);
            yield return null;
            actions.ClearBeatmapActions();
        }

        [UnityTest]
        public IEnumerator PropertyActionsUndoRedoAndBroadRefreshPreserveInstances([ValueSource(nameof(PlaybackCases))] bool playing)
        {
            clock.MoveToJsonTime(5);
            var prefab = Subject();
            var instanceIds = Instances();
            var trigger = BeatSaberSongContainer.Instance.Map.CustomEvents.Single(evt => evt.Type == "SetAnimatorProperty");
            Assert.That(trigger.Data["properties"][0].HasKey("value"), Is.False,
                "Dispatching a trigger with an omitted value must leave its source JSON unchanged.");
            var triggerSource = trigger.Data.ToString();
            if (playing) TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 5);

            foreach (var type in new[] { "SetMaterialProperty", "SetGlobalProperty", "SetAnimatorProperty", "SetRenderingSettings" })
            {
                var baseline = Read(type);
                var original = PropertyEvent(type, 3);
                Place(original);
                yield return Sample();
                AssertState(type, 3, instanceIds, triggerSource);
                var edited = PropertyEvent(type, 7);
                BeatmapActionContainer.AddAction(new BeatmapObjectUpdatedAction(edited, original, "Edit Vivify property"), true);
                yield return Sample();
                AssertState(type, 7, instanceIds, triggerSource);
                Assert.That(actions.Undo(), Is.Not.Null);
                yield return Sample();
                AssertState(type, 3, instanceIds, triggerSource);
                Assert.That(actions.Redo(), Is.Not.Null);
                yield return Sample();
                AssertState(type, 7, instanceIds, triggerSource);
                manager.Refresh();
                AssertState(type, 7, instanceIds, triggerSource);
                BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(edited, "Delete Vivify property"), true);
                yield return Sample();
                AssertState(type, baseline, instanceIds, triggerSource);
                actions.ClearBeatmapActions();
            }
            Assert.That(Subject(), Is.SameAs(prefab));
            if (playing) Assert.That(clock.SongAudioSource.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator MaterialCurveEditsAndTimelineMovesRestorePrecedenceWithoutRecreatingObjects()
        {
            clock.MoveToJsonTime(5);
            var ids = Instances();
            var map = BeatSaberSongContainer.Instance.Map;
            var points = JSON.Parse("[[0,0],[10,1]]").AsArray;
            map.PointDefinitions.Add("vivifyTestCurve", points);
            var animated = Event(4, "SetMaterialProperty", "{\"asset\":\"" + VivifyTestAssets.MaterialPath
                + "\",\"duration\":2,\"properties\":[{\"id\":\"_Value\",\"type\":\"Float\",\"value\":\"vivifyTestCurve\"}]}" );
            Place(animated);
            yield return Sample();
            Assert.That(assets.Material.GetFloat("_Value"), Is.EqualTo(5).Within(.001f));
            map.PointDefinitions["vivifyTestCurve"] = JSON.Parse("[[2,0],[14,1]]").AsArray;
            manager.Refresh();
            Assert.That(assets.Material.GetFloat("_Value"), Is.EqualTo(8).Within(.001f));
            var later = PropertyEvent("SetMaterialProperty", 9);
            later.JsonTime = 5;
            Place(later);
            yield return Sample();
            Assert.That(assets.Material.GetFloat("_Value"), Is.EqualTo(9).Within(.001f));
            var moved = Event(8, later.Type, later.Data.ToString());
            BeatmapActionContainer.AddAction(new BeatmapObjectUpdatedAction(moved, later, "Move Vivify property"), true);
            yield return Sample();
            Assert.That(assets.Material.GetFloat("_Value"), Is.EqualTo(8).Within(.001f));
            Assert.That(actions.Undo(), Is.Not.Null);
            yield return Sample();
            Assert.That(assets.Material.GetFloat("_Value"), Is.EqualTo(9).Within(.001f));
            Assert.That(Instances(), Is.EquivalentTo(ids));
        }

        [UnityTest]
        public IEnumerator DefinitePositionWithoutGravityFlagDoesNotMutateNoteDataOnSeekOrRefresh()
        {
            var note = BeatSaberSongContainer.Instance.Map.Notes.Single();
            Assert.That(note.CustomData.HasKey("_disableNoteGravity"), Is.False);
            Assert.That(note.CustomData.HasKey("disableNoteGravity"), Is.False);
            var source = note.CustomData.ToString();
            foreach (var beat in new[] { 8f, 10f, 5f, 8f })
            {
                clock.MoveToJsonTime(beat);
                manager.Refresh();
                yield return null;
                Assert.That(note.CustomData.ToString(), Is.EqualTo(source));
                var animators = Object.FindObjectsByType<ObjectAnimator>(FindObjectsSortMode.None)
                    .Where(animator => ReferenceEquals(animator.AnimationObject, note))
                    .ToArray();
                if (beat == 8) Assert.That(animators, Is.Not.Empty, "The note must reach the production animation attachment path.");
                Assert.That(animators.All(animator => !animator.HasChangedObjectAnimation), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator MapSuspensionRestoresPropertiesAndReleasesOwnedPrefabs()
        {
            clock.MoveToJsonTime(5);
            var prefab = Subject();
            Place(PropertyEvent("SetGlobalProperty", 7));
            Place(PropertyEvent("SetRenderingSettings", 12));
            yield return Sample();
            Assert.That(Shader.GetGlobalFloat(Global), Is.EqualTo(7));
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(12));
            manager.SuspendForMapChange();
            Assert.That(Shader.GetGlobalFloat(Global), Is.EqualTo(originalGlobal));
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(originalShadowDistance));
            yield return null;
            Assert.That(prefab == null, Is.True);
            Assert.That(Instances(), Is.Empty);
        }

        private IEnumerator Sample()
        {
            if (clock.IsPlaying) Seconds.SetValue(clock, clock.GetSecondsFromBeat(5));
            yield return null;
            Assert.That(clock.CurrentJsonTime, Is.EqualTo(5).Within(.001f));
        }

        private void AssertState(string type, float expected, int[] ids, string triggerSource)
        {
            Assert.That(Read(type), Is.EqualTo(expected).Within(.001f), type);
            Assert.That(Instances(), Is.EquivalentTo(ids), type + " recreated a prefab.");
            Assert.That(BeatSaberSongContainer.Instance.Map.CustomEvents.Single(evt => evt.Type == "SetAnimatorProperty"
                && evt.Data["properties"][0]["type"] == "Trigger").Data.ToString(), Is.EqualTo(triggerSource));
        }

        private float Read(string type) => type switch
        {
            "SetMaterialProperty" => assets.Material.GetFloat("_Value"),
            "SetGlobalProperty" => Shader.GetGlobalFloat(Global),
            "SetAnimatorProperty" => Subject().Animators[0].GetFloat("Value"),
            _ => QualitySettings.shadowDistance
        };

        private static BaseCustomEvent PropertyEvent(string type, float value)
        {
            if (type == "SetRenderingSettings") return Event(4, type,
                new JSONObject { ["qualitySettings"] = new JSONObject { ["shadowDistance"] = value } }.ToString());
            return Event(4, type, new JSONObject
            {
                ["asset"] = VivifyTestAssets.MaterialPath, ["id"] = "subject",
                ["properties"] = new JSONArray { [0] = new JSONObject
                {
                    ["id"] = type == "SetMaterialProperty" ? "_Value" : type == "SetGlobalProperty" ? Global : "Value",
                    ["type"] = "Float", ["value"] = value
                } }
            }.ToString());
        }

        private static BaseCustomEvent Event(float beat, string type, string json) =>
            new(new JSONObject { ["b"] = beat, ["t"] = type, ["d"] = JSON.Parse(json) });

        private static void Place(BaseCustomEvent evt) => BeatmapActionContainer.AddAction(
            new BeatmapObjectPlacementAction(evt, new BaseObject[0], "Place Vivify test event"), true);

        private static VivifyObject Subject() => Object.FindObjectsByType<VivifyObject>(FindObjectsSortMode.None)
            .Single(obj => obj.AssetPath == VivifyTestAssets.PrefabPath);

        private static int[] Instances() => Object.FindObjectsByType<VivifyObject>(FindObjectsSortMode.None)
            .Where(obj => obj.AssetPath == VivifyTestAssets.PrefabPath).Select(obj => obj.GetInstanceID()).ToArray();

        protected override void CleanupTestObjects() { }

        [UnityTearDown]
        public IEnumerator RestoreMapper()
        {
            if (clock != null)
            {
                if (clock.IsPlaying) TestUtils.PauseDeterministicPlayback(clock);
                clock.enabled = clockWasEnabled;
            }
            if (manager != null) manager.SuspendForMapChange();
            assets?.Dispose();
            assets = null;
            Shader.SetGlobalFloat(Global, originalGlobal);
            QualitySettings.shadowDistance = originalShadowDistance;
            Settings.Instance.Animations = originalAnimations;
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            if (actions != null) actions.ClearBeatmapActions();
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

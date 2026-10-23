using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Info;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaGradientSourceEditingTest : TestBase
    {
        private BasicLightEffect effect;
        private ChromaBasicEventRoutingAuditTest.AuditLightController selectedLight;
        private ChromaBasicEventRoutingAuditTest.AuditLightController unselectedLight;
        private AudioTimeSyncController clock;
        private bool previousLite;
        private bool previousAdvanced;
        private bool previousLoadEvents;
        private LightshowMode previousLightshowMode;
        private LightshowController lightshow;
        private const int SelectedLightId = 10001;
        private const int UnselectedLightId = 10002;

        protected override EditingMode InitialEditingMode => EditingMode.BasicEvent;

        protected override IEnumerator OnMapLoaded()
        {
            previousLite = Settings.Instance.EmulateChromaLite;
            previousAdvanced = Settings.Instance.EmulateChromaAdvanced;
            previousLoadEvents = Settings.Instance.Load_Events;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        private IEnumerator LoadGradientScene()
        {
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.EmulateChromaAdvanced = true;
            Settings.Instance.Load_Events = true;
            var data = new JSONObject
            {
                ["version"] = "3.3.0",
                ["bpmEvents"] = new JSONArray { [0] = new JSONObject { ["b"] = 2, ["m"] = 120 } }
            };
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 16,
                CustomRequirements = new List<string> { "Chroma" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(
                3,
                data,
                beatsPerMinute: 120,
                environmentName: "BillieEnvironment",
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            lightshow = Object.FindAnyObjectByType<LightshowController>();
            previousLightshowMode = lightshow.Mode;
            lightshow.SetMode(LightshowMode.Full);
            clock = context.Atsc;
            effect = context.Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(2);
            selectedLight = CreateLight(SelectedLightId);
            unselectedLight = CreateLight(UnselectedLightId);
            effect.Initialize();
            PlaceUtils.Place(Event(0f, 5, new JSONObject { ["color"] = Color.green }));
            PlaceUtils.Place(
                Event(
                    1f,
                    6,
                    new JSONObject
                    {
                        ["lightID"] = new JSONArray { [0] = UnselectedLightId },
                        ["color"] = Color.green,
                        ["easing"] = "easeLinear"
                    }));
        }

        private ChromaBasicEventRoutingAuditTest.AuditLightController CreateLight(int id)
        {
            var owner = new GameObject("Gradient source editing light " + id);
            owner.transform.SetParent(effect.transform, false);
            var light = owner.AddComponent<ChromaBasicEventRoutingAuditTest.AuditLightController>();
            light.Kind = LightController.LightKind.Basic;
            light.Type = 2;
            light.ID = id;
            effect.Register(light, false);
            return light;
        }

        [UnityTest]
        public IEnumerator MovingGradientSourceRebuildsSelectedAndUnselectedLightsThroughUndoRedo()
        {
            yield return LoadGradientScene();
            var source = PlaceGradient();
            AssertAt(5f, new Color(0.75f, 0f, 0.25f));
            SelectionController.Select(source);
            Object.FindAnyObjectByType<SelectionController>().MoveSelection(2f);
            AssertAt(5f, Color.green);
            AssertAt(7f, new Color(0.75f, 0f, 0.25f));
            PlaceUtils.Undo();
            AssertAt(5f, new Color(0.75f, 0f, 0.25f));
            PlaceUtils.Redo();
            AssertAt(5f, Color.green);
            AssertAt(7f, new Color(0.75f, 0f, 0.25f));
        }

        [UnityTest]
        public IEnumerator DeletingGradientSourceRestoresPriorColorsThroughUndoRedo()
        {
            yield return LoadGradientScene();
            var source = PlaceGradient();
            AssertAt(5f, new Color(0.75f, 0f, 0.25f));
            var events = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            events.DeleteObject(source);
            AssertAt(5f, Color.green);
            PlaceUtils.Undo();
            AssertAt(5f, new Color(0.75f, 0f, 0.25f));
            PlaceUtils.Redo();
            AssertAt(5f, Color.green);
        }

        [UnityTest]
        public IEnumerator EditingBpmBeforeGradientSourceRefreshesItsFrozenDurationThroughUndoRedo()
        {
            yield return LoadGradientScene();
            PlaceGradient();
            AssertAt(5f, new Color(0.75f, 0f, 0.25f));
            var events = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);
            var bpm = events.MapObjects.Single(item => item.JsonTime == 2f);
            events.RefreshPool();
            var container = (BpmEventContainer)events.LoadedContainers[bpm];
            BeatmapBPMChangeInputController.ChangeBpm(container, "240");
            AssertAt(5f, new Color(0.75f, 0f, 0.25f));
            PlaceUtils.Undo();
            AssertAt(5f, new Color(0.75f, 0f, 0.25f));
            PlaceUtils.Redo();
            AssertAt(5f, new Color(0.75f, 0f, 0.25f));
        }

        private static BaseEvent PlaceGradient() => PlaceUtils.Place(
            Event(
                4f,
                5,
                new JSONObject
                {
                    ["lightID"] = new JSONArray { [0] = SelectedLightId },
                    ["lightGradient"] = new JSONObject
                    {
                        ["_startColor"] = Color.red,
                        ["_endColor"] = Color.blue,
                        ["_duration"] = 4,
                        ["_easing"] = "easeLinear"
                    }
                }));

        private static BaseEvent Event(float beat, int value, JSONObject customData) => V3BasicEvent.GetFromJson(
            new JSONObject
            {
                ["b"] = beat, ["et"] = 2, ["i"] = value, ["f"] = 1f, ["customData"] = customData
            });

        private void AssertAt(float jsonTime, Color expected)
        {
            clock.MoveToJsonTime(0f);
            clock.MoveToJsonTime(jsonTime + 0.01f);
            clock.MoveToJsonTime(jsonTime);
            foreach (var light in new[] { selectedLight, unselectedLight })
            {
                var actual = light.Color;
                Assert.That(
                    Vector3.Distance(
                        new Vector3(actual.r, actual.g, actual.b),
                        new Vector3(expected.r, expected.g, expected.b)),
                    Is.LessThan(0.0001f),
                    "Gradient source edits must update every previously lit ID at beat " + jsonTime
                    + " ID=" + light.ID + " actual=" + actual + " expected=" + expected);
            }

            Assert.That(
                unselectedLight.Color.a,
                Is.EqualTo(0.7490196f).Within(0.0001f),
                "Refreshing gradient RGB must preserve the unselected light's completed fixed flash clock.");
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            Settings.Instance.EmulateChromaLite = previousLite;
            Settings.Instance.EmulateChromaAdvanced = previousAdvanced;
            Settings.Instance.Load_Events = previousLoadEvents;
            if (lightshow != null)
                lightshow.SetMode(previousLightshowMode);

            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.3.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

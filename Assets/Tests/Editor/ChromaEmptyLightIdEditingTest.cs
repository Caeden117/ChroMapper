using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class ChromaEmptyLightIdEditingTest : TestBase
    {
        private bool previousLite;
        private bool previousAdvanced;
        private int previousPropagationType;
        private EventGridContainer.PropMode previousPropagationMode;

        protected override EditingMode InitialEditingMode => EditingMode.BasicEvent;

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(
                3,
                null,
                beatsPerMinute: 120,
                environmentName: "BillieEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [SetUp]
        public void EnableChroma()
        {
            var events = Object.FindAnyObjectByType<EventGridContainer>();
            previousPropagationType = events.EventTypeToPropagate;
            previousPropagationMode = events.PropagationEditing;
            previousLite = Settings.Instance.EmulateChromaLite;
            previousAdvanced = Settings.Instance.EmulateChromaAdvanced;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.EmulateChromaAdvanced = true;
        }

        protected override void BeforeCleanup()
        {
            Object.FindAnyObjectByType<EventGridContainer>().PropagationEditing = EventGridContainer.PropMode.Off;
        }

        protected override void AfterCleanup()
        {
            Settings.Instance.EmulateChromaLite = previousLite;
            Settings.Instance.EmulateChromaAdvanced = previousAdvanced;
            var events = Object.FindAnyObjectByType<EventGridContainer>();
            events.EventTypeToPropagate = previousPropagationType;
            events.PropagationEditing = previousPropagationMode;
        }

        [Test]
        public void ShiftEventTypePreservesAnEmptyNativeSelection()
        {
            var source = PlaceEmptySelection(2f, Color.green);
            SelectionController.Select(source);
            var selection = Object.FindAnyObjectByType<SelectionController>();
            Assert.DoesNotThrow(() => selection.ShiftSelection(1, 0));

            var shifted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(shifted.Type, Is.Not.EqualTo(source.Type));
            AssertEmptyExport(shifted);
        }

        [Test]
        public void ShiftLightLaneCanReplaceAnEmptyNativeSelection()
        {
            var source = PlaceEmptySelection(2f, Color.green);
            var events = Object.FindAnyObjectByType<EventGridContainer>();
            events.EventTypeToPropagate = source.Type;
            events.PropagationEditing = EventGridContainer.PropMode.Light;
            var firstLightId = events.TypeToManager[source.Type].LaneToLightID[0];
            SelectionController.Select(source);
            var selection = Object.FindAnyObjectByType<SelectionController>();
            Assert.DoesNotThrow(() => selection.ShiftSelection(1, 0));

            var shifted = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            Assert.That(shifted.CustomLightID, Is.EqualTo(new[] { firstLightId }));
        }

        [Test]
        public void GeneratedGradientKeepsEmptySelectionSeparateFromBroadcast()
        {
            var start = PlaceEmptySelection(2f, Color.green);
            var end = PlaceEmptySelection(3f, Color.blue);
            var broadcast = PlaceUtils.Place(
                new BaseEvent
                {
                    JsonTime = 2.5f,
                    Type = 2,
                    Value = (int)LightValue.RedOn,
                    FloatValue = 1f,
                    CustomColor = Color.red
                });
            SelectionController.Select(start);
            SelectionController.Select(end, true);
            var definitions = Object.FindAnyObjectByType<BeatmapRuntimeContext>().TrackDefinitions;
            var generator = Object.FindAnyObjectByType<StrobeGenerator>();
            Assert.DoesNotThrow(
                () => generator.GenerateStrobe(
                    new List<StrobeGeneratorPass>
                    {
                        new StrobeStepGradientPass(
                            definitions,
                            (int)LightValue.BlueOn,
                            false,
                            2,
                            Easing.Linear)
                    }));

            var generated = SelectionController.SelectedObjects.OfType<BaseEvent>().ToList();
            Assert.That(generated.Count, Is.GreaterThan(0));
            foreach (var data in generated)
                AssertEmptyExport(data);

            var middle = generated.Single(data => Mathf.Approximately(data.JsonTime, 2.5f));
            Assert.That(middle.CustomColor, Is.EqualTo(new Color(0f, 0.5f, 0.5f, 1f)));
            Assert.That(Object.FindAnyObjectByType<EventGridContainer>().MapObjects.Contains(broadcast), Is.True);
        }

        private static BaseEvent PlaceEmptySelection(float beat, Color color)
        {
            var source = V3BasicEvent.GetFromJson(
                new JSONObject
                {
                    ["b"] = beat,
                    ["et"] = 2,
                    ["i"] = (int)LightValue.RedOn,
                    ["f"] = 1f,
                    ["customData"] = new JSONObject
                    {
                        ["lightID"] = new JSONArray(),
                        ["color"] = color
                    }
                });
            return PlaceUtils.Place(source);
        }

        private static void AssertEmptyExport(BaseEvent data)
        {
            Assert.That(data.CustomLightID, Is.Not.Null);
            Assert.That(data.CustomLightID, Is.Empty);
            var exported = V3BasicEvent.ToJson(data)["customData"]["lightID"];
            Assert.That(exported.IsArray, Is.True);
            Assert.That(exported.Count, Is.EqualTo(0));
        }
    }
}

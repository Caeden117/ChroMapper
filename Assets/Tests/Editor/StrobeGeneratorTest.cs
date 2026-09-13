using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    public class StrobeGeneratorTest : TestBase
    {
        [Test]
        public void ChromaStepGradient()
        {
            // Use the dev branch's renamed runtime track-definition property.
            var trackDefinitions = Object.FindAnyObjectByType<BeatmapRuntimeContext>().TrackDefinitions;

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject { ["color"] = new Color(0, 1, 0) }
            };

            var eventB = new BaseEvent
            {
                JsonTime = 3,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject { ["color"] = new Color(0, 0, 1) }
            };

            var eventC = new BaseEvent
            {
                JsonTime = 3,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject { ["lightID"] = 1, ["color"] = new Color(1, 0, 0) }
            };

            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);
            eventC = PlaceUtils.Place(eventC);

            SelectionController.Select(eventA);
            SelectionController.Select(eventB, true);
            // eventC is not selected

            var expected = BeatmapFactory.Clone(originalEventA);
            expected.JsonTime = 2.5f;
            expected.Value = (int)LightValue.BlueOn;
            expected.CustomData["color"] = new Color(0, 0.5f, 0.5f);

            var strobeGenerator = Object.FindAnyObjectByType<StrobeGenerator>();
            strobeGenerator.GenerateStrobe(
                new List<StrobeGeneratorPass>
                {
                    new StrobeStepGradientPass(
                        trackDefinitions,
                        (int)LightValue.BlueOn,
                        false,
                        2,
                        Easing.Linear)
                });

            var generatedEvent = SelectionController
                .SelectedObjects
                .OfType<BaseEvent>()
                .Single(evt => Mathf.Approximately(evt.JsonTime, expected.JsonTime));
            BeatmapAssertion.IsEqual(expected, generatedEvent, "Check step Chroma event color");
        }

        [Test]
        public void LightIDChromaStepGradient()
        {
            // Use the dev branch's renamed runtime track-definition property.
            var trackDefinitions = Object.FindAnyObjectByType<BeatmapRuntimeContext>().TrackDefinitions;

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject { ["color"] = new Color(0, 1, 0) }
            };

            var eventB = new BaseEvent
            {
                JsonTime = 3,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject { ["color"] = new Color(0, 0, 1) }
            };

            var eventC = new BaseEvent
            {
                JsonTime = 3,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject { ["lightID"] = 1, ["color"] = new Color(1, 0, 0) }
            };

            var eventD = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject { ["lightID"] = 1, ["color"] = new Color(1, 1, 0) }
            };

            var eventE = new BaseEvent
            {
                JsonTime = 4,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject
                {
                    ["lightID"] = new JSONArray { [0] = 1, [1] = 2 }, ["color"] = new Color(1, 0, 1)
                }
            };

            var eventF = new BaseEvent
            {
                JsonTime = 3,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomData = new JSONObject { ["lightID"] = 3, ["color"] = new Color(0, 1, 1) }
            };

            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);
            var originalEventC = BeatmapFactory.Clone(eventC);
            eventC = PlaceUtils.Place(eventC);
            var originalEventD = BeatmapFactory.Clone(eventD);
            eventD = PlaceUtils.Place(eventD);
            eventE = PlaceUtils.Place(eventE);
            eventF = PlaceUtils.Place(eventF);

            SelectionController.Select(eventC);
            SelectionController.Select(eventD, true);
            SelectionController.Select(eventE, true);

            var expectedStart = BeatmapFactory.Clone(originalEventD);
            expectedStart.JsonTime = 2.5f;
            expectedStart.Value = (int)LightValue.BlueOn;
            expectedStart.CustomData = new JSONObject
            {
                ["color"] = new Color(1, 0.5f, 0), ["lightID"] = new JSONArray { [0] = 1 }
            };

            var expectedEnd = BeatmapFactory.Clone(originalEventC);
            expectedEnd.JsonTime = 3.5f;
            expectedEnd.Value = (int)LightValue.BlueOn;
            expectedEnd.CustomData = new JSONObject
            {
                ["color"] = new Color(1, 0, 0.5f), ["lightID"] = new JSONArray { [0] = 1 }
            };

            var strobeGenerator = Object.FindAnyObjectByType<StrobeGenerator>();
            strobeGenerator.GenerateStrobe(
                new List<StrobeGeneratorPass>
                {
                    new StrobeStepGradientPass(
                        trackDefinitions,
                        (int)LightValue.BlueOn,
                        false,
                        2,
                        Easing.Linear)
                });

            // Current _lightID from the first event is used. As eventC is added first here we always get a single light id
            // If this changes in future then update below, this test wasn't really meant to enforce this behaviour
            var generatedEvents = SelectionController.SelectedObjects.OfType<BaseEvent>().ToList();
            var generatedStart =
                generatedEvents.Single(evt => Mathf.Approximately(evt.JsonTime, expectedStart.JsonTime));
            var generatedEnd =
                generatedEvents.Single(evt => Mathf.Approximately(evt.JsonTime, expectedEnd.JsonTime));

            BeatmapAssertion.IsEqual(expectedStart, generatedStart, "Check start step Chroma light ID event color");
            BeatmapAssertion.IsEqual(expectedEnd, generatedEnd, "Check end step Chroma light ID event color");
        }
    }
}

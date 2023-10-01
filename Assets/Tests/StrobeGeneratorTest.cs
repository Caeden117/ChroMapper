using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class StrobeGeneratorTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupEvents();
        }

        [Test]
        public void ChromaStepGradient()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["color"] = new Color(0, 1, 0)
                    });
                BaseEvent baseEventB = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["color"] = new Color(0, 0, 1)
                    });
                BaseEvent baseEventC = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["lightID"] = 1,
                        ["color"] = new Color(1, 0, 0)
                    });

                foreach (var evt in new[] { baseEventA, baseEventB, baseEventC })
                    PlaceUtils.PlaceEvent(eventPlacement, evt);

                SelectionController.Select(baseEventA);
                SelectionController.Select(baseEventB, true);
                // eventC is not selected

                var strobeGenerator = Object.FindObjectOfType<StrobeGenerator>();
                strobeGenerator.GenerateStrobe(new List<StrobeGeneratorPass>
                {
                    new StrobeStepGradientPass((int)LightValue.BlueOn, false, 2, Easing.Linear)
                });

                CheckUtils.CheckEvent("Check step Chroma event color", eventsContainer, 1, 2.5f,
                    (int)EventTypeValue.RingLights, (int)LightValue.BlueOn, 1f, new JSONObject
                    {
                        ["color"] = new Color(0, 0.5f, 0.5f)
                    });
            }
        }

        [Test]
        public void LightIDChromaStepGradient()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["color"] = new Color(0, 1, 0)
                    });
                BaseEvent baseEventB = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["color"] = new Color(0, 0, 1)
                    });
                BaseEvent baseEventC = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["lightID"] = 1,
                        ["color"] = new Color(1, 0, 0)
                    });
                BaseEvent baseEventD = new V3BasicEvent(2, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["lightID"] = 1,
                        ["color"] = new Color(1, 1, 0)
                    });
                BaseEvent baseEventE = new V3BasicEvent(4, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["lightID"] = new JSONArray
                        {
                            [0] = 1,
                            [1] = 2
                        },
                        ["color"] = new Color(1, 0, 1)
                    });
                BaseEvent baseEventF = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f,
                    new JSONObject
                    {
                        ["lightID"] = 3,
                        ["color"] = new Color(0, 1, 1)
                    });

                foreach (var evt in new[] { baseEventA, baseEventB, baseEventC, baseEventD, baseEventE, baseEventF })
                    PlaceUtils.PlaceEvent(eventPlacement, evt);

                SelectionController.Select(baseEventC);
                SelectionController.Select(baseEventD, true);
                SelectionController.Select(baseEventE, true);

                var strobeGenerator = Object.FindObjectOfType<StrobeGenerator>();
                strobeGenerator.GenerateStrobe(new List<StrobeGeneratorPass>
                {
                    new StrobeStepGradientPass((int)LightValue.BlueOn, false, 2, Easing.Linear)
                });

                // Current _lightID from the first event is used. As eventC is added first here we always get a single light id
                // If this changes in future then update below, this test wasn't really meant to enforce this behaviour
                CheckUtils.CheckEvent("Check start step Chroma light ID event color", eventsContainer, 2, 2.5f,
                    (int)EventTypeValue.RingLights, (int)LightValue.BlueOn, 1f, new JSONObject
                    {
                        ["color"] = new Color(1, 0.5f, 0),
                        ["lightID"] = new JSONArray { [0] = 1 }
                    });
                CheckUtils.CheckEvent("Check end step Chroma light ID event color", eventsContainer, 6, 3.5f,
                    (int)EventTypeValue.RingLights, (int)LightValue.BlueOn, 1f, new JSONObject
                    {
                        ["color"] = new Color(1, 0, 0.5f),
                        ["lightID"] = new JSONArray { [0] = 1 }
                    });
            }
        }
    }
}
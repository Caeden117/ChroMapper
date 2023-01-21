using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V3;
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
            return TestUtils.LoadMapper();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupEvents();
        }

        [Test]
        public void ChromaStepGradient()
        {
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["color"] = new Color(0, 1, 0)
                });
                BaseEvent baseEventB = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["color"] = new Color(0, 0, 1)
                });
                BaseEvent baseEventC = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["lightID"] = 1,
                    ["color"] = new Color(1, 0, 0)
                });

                foreach (BaseEvent evt in new BaseEvent[] { baseEventA, baseEventB, baseEventC })
                {
                    eventPlacement.queuedData = evt;
                    eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                    eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                    eventPlacement.ApplyToMap();
                }

                SelectionController.Select(baseEventA);
                SelectionController.Select(baseEventB, true);
                // eventC is not selected

                StrobeGenerator strobeGenerator = Object.FindObjectOfType<StrobeGenerator>();
                strobeGenerator.GenerateStrobe(new List<StrobeGeneratorPass>()
                {
                    new StrobeStepGradientPass((int)LightValue.BlueOn, false, 2, Easing.Linear)
                });

                CheckUtils.CheckEvent("Check step Chroma event color", eventsContainer, 1, 2.5f, (int)EventTypeValue.RingLights, (int)LightValue.BlueOn,  1f, new JSONObject
                {
                    ["color"] = new Color(0, 0.5f, 0.5f)
                });
            }
        }

        [Test]
        public void LightIDChromaStepGradient()
        {
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["color"] = new Color(0, 1, 0)
                });
                BaseEvent baseEventB = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["color"] = new Color(0, 0, 1)
                });
                BaseEvent baseEventC = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["lightID"] = 1,
                    ["color"] = new Color(1, 0, 0)
                });
                BaseEvent baseEventD = new V3BasicEvent(2, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["lightID"] = 1,
                    ["color"] = new Color(1, 1, 0)
                });
                BaseEvent baseEventE = new V3BasicEvent(4, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["lightID"] = new JSONArray()
                    {
                        [0] = 1,
                        [1] = 2
                    },
                    ["color"] = new Color(1, 0, 1)
                });
                BaseEvent baseEventF = new V3BasicEvent(3, (int)EventTypeValue.RingLights, (int)LightValue.RedOn, 1f, new JSONObject
                {
                    ["lightID"] = 3,
                    ["color"] = new Color(0, 1, 1)
                });

                foreach (BaseEvent evt in new BaseEvent[] { baseEventA, baseEventB, baseEventC, baseEventD, baseEventE, baseEventF })
                {
                    eventPlacement.queuedData = evt;
                    eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                    eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                    eventPlacement.ApplyToMap();
                }

                SelectionController.Select(baseEventC);
                SelectionController.Select(baseEventD, true);
                SelectionController.Select(baseEventE, true);

                StrobeGenerator strobeGenerator = Object.FindObjectOfType<StrobeGenerator>();
                strobeGenerator.GenerateStrobe(new List<StrobeGeneratorPass>()
                {
                    new StrobeStepGradientPass((int)LightValue.BlueOn, false, 2, Easing.Linear)
                });

                // Current _lightID from the first event is used. As eventC is added first here we always get a single light id
                // If this changes in future then update below, this test wasn't really meant to enforce this behaviour
                CheckUtils.CheckEvent("Check start step Chroma light ID event color", eventsContainer, 2, 2.5f, (int)EventTypeValue.RingLights, (int)LightValue.BlueOn, 1f, new JSONObject
                {
                    ["color"] = new Color(1, 0.5f, 0),
                    ["lightID"] = new JSONArray { [0] = 1 }
                });
                CheckUtils.CheckEvent("Check end step Chroma light ID event color", eventsContainer, 6, 3.5f, (int)EventTypeValue.RingLights, (int)LightValue.BlueOn, 1f, new JSONObject
                {
                    ["color"] = new Color(1, 0, 0.5f),
                    ["lightID"] = new JSONArray { [0] = 1 }
                });
            }
        }
    }
}

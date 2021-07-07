using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class StrobeGeneratorTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupEvents();
        }

        public static void CheckEvent(BeatmapObjectContainerCollection container, int idx, float time, int type, int value, JSONNode customData = null)
        {
            var newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<MapEvent>(newObjA);
            if (newObjA is MapEvent newNoteA)
            {
                Assert.AreEqual(time, newNoteA._time);
                Assert.AreEqual(type, newNoteA._type);
                Assert.AreEqual(value, newNoteA._value);

                // ConvertToJSON causes gradient to get updated
                if (customData != null) Assert.AreEqual(customData.ToString(), newNoteA.ConvertToJSON()["_customData"].ToString());
            }
        }

        [Test]
        public void ChromaStepGradient()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            if (containerCollection is EventsContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                var eventA = new MapEvent(2, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_color"] = new Color(0, 1, 0)
                });
                var eventB = new MapEvent(3, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_color"] = new Color(0, 0, 1)
                });
                var eventC = new MapEvent(3, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_lightID"] = 1,
                    ["_color"] = new Color(1, 0, 0)
                });

                foreach (var mapEvent in new MapEvent[] { eventA, eventB, eventC })
                {
                    eventPlacement.queuedData = mapEvent;
                    eventPlacement.queuedValue = eventPlacement.queuedData._value;
                    eventPlacement.RoundedTime = eventPlacement.queuedData._time;
                    eventPlacement.ApplyToMap();
                }

                SelectionController.Select(eventA);
                SelectionController.Select(eventB, true);
                // eventC is not selected

                var strobeGenerator = Object.FindObjectOfType<StrobeGenerator>();
                strobeGenerator.GenerateStrobe(new List<StrobeGeneratorPass>()
                {
                    new StrobeStepGradientPass(MapEvent.LIGHT_VALUE_BLUE_ON, false, 2, Easing.Linear)
                });

                CheckEvent(eventsContainer, 1, 2.5f, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_BLUE_ON, new JSONObject
                {
                    ["_color"] = new Color(0, 0.5f, 0.5f)
                });
            }
        }

        [Test]
        public void LightIDChromaStepGradient()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            if (containerCollection is EventsContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                var eventA = new MapEvent(2, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_color"] = new Color(0, 1, 0)
                });
                var eventB = new MapEvent(3, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_color"] = new Color(0, 0, 1)
                });
                var eventC = new MapEvent(3, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_lightID"] = 1,
                    ["_color"] = new Color(1, 0, 0)
                });
                var eventD = new MapEvent(2, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_lightID"] = 1,
                    ["_color"] = new Color(1, 1, 0)
                });
                var eventE = new MapEvent(4, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_lightID"] = new JSONArray()
                    {
                        [0] = 1,
                        [1] = 2
                    },
                    ["_color"] = new Color(1, 0, 1)
                });
                var eventF = new MapEvent(3, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_RED_ON, new JSONObject
                {
                    ["_lightID"] = 3,
                    ["_color"] = new Color(0, 1, 1)
                });

                foreach (var mapEvent in new MapEvent[] { eventA, eventB, eventC, eventD, eventE, eventF })
                {
                    eventPlacement.queuedData = mapEvent;
                    eventPlacement.queuedValue = eventPlacement.queuedData._value;
                    eventPlacement.RoundedTime = eventPlacement.queuedData._time;
                    eventPlacement.ApplyToMap();
                }

                SelectionController.Select(eventC);
                SelectionController.Select(eventD, true);
                SelectionController.Select(eventE, true);

                var strobeGenerator = Object.FindObjectOfType<StrobeGenerator>();
                strobeGenerator.GenerateStrobe(new List<StrobeGeneratorPass>()
                {
                    new StrobeStepGradientPass(MapEvent.LIGHT_VALUE_BLUE_ON, false, 2, Easing.Linear)
                });

                // Current _lightID from the first event is used. As eventC is added first here we always get a single light id
                // If this changes in future then update below, this test wasn't really meant to enforce this behaviour
                CheckEvent(eventsContainer, 2, 2.5f, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_BLUE_ON, new JSONObject
                {
                    ["_color"] = new Color(1, 0.5f, 0),
                    ["_lightID"] = 1
                });
                CheckEvent(eventsContainer, 6, 3.5f, MapEvent.EVENT_TYPE_RING_LIGHTS, MapEvent.LIGHT_VALUE_BLUE_ON, new JSONObject
                {
                    ["_color"] = new Color(1, 0, 0.5f),
                    ["_lightID"] = 1
                });
            }
        }
    }
}

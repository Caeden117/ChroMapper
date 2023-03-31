using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public static void CheckEvent(BeatmapObjectContainerCollection container, int idx, float time, int type, int value, JSONNode customData = null)
        {
            BeatmapObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<MapEvent>(newObjA);
            if (newObjA is MapEvent newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time);
                Assert.AreEqual(type, newNoteA.Type);
                Assert.AreEqual(value, newNoteA.Value);

                // ConvertToJSON causes gradient to get updated
                if (customData != null)
                {
                    Assert.AreEqual(customData.ToString(), newNoteA.ConvertToJson()["_customData"].ToString());
                }
            }
        }

        [Test]
        public void ChromaStepGradient()
        {
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            if (containerCollection is EventsContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                MapEvent eventA = new MapEvent(2, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "color"] = new Color(0, 1, 0)
                });
                MapEvent eventB = new MapEvent(3, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "color"] = new Color(0, 0, 1)
                });
                MapEvent eventC = new MapEvent(3, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "lightID"] = 1,
                    [MapLoader.heckUnderscore + "color"] = new Color(1, 0, 0)
                });

                foreach (MapEvent mapEvent in new MapEvent[] { eventA, eventB, eventC })
                {
                    eventPlacement.queuedData = mapEvent;
                    eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                    eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                    eventPlacement.ApplyToMap();
                }

                SelectionController.Select(eventA);
                SelectionController.Select(eventB, true);
                // eventC is not selected

                StrobeGenerator strobeGenerator = Object.FindObjectOfType<StrobeGenerator>();
                strobeGenerator.GenerateStrobe(new List<StrobeGeneratorPass>()
                {
                    new StrobeStepGradientPass(MapEvent.LightValueBlueON, false, 2, Easing.Linear)
                });

                CheckEvent(eventsContainer, 1, 2.5f, MapEvent.EventTypeRingLights, MapEvent.LightValueBlueON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "color"] = new Color(0, 0.5f, 0.5f)
                });
            }
        }

        [Test]
        public void LightIDChromaStepGradient()
        {
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            if (containerCollection is EventsContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

                MapEvent eventA = new MapEvent(2, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "color"] = new Color(0, 1, 0)
                });
                MapEvent eventB = new MapEvent(3, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "color"] = new Color(0, 0, 1)
                });
                MapEvent eventC = new MapEvent(3, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "lightID"] = 1,
                    [MapLoader.heckUnderscore + "color"] = new Color(1, 0, 0)
                });
                MapEvent eventD = new MapEvent(2, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "lightID"] = 1,
                    [MapLoader.heckUnderscore + "color"] = new Color(1, 1, 0)
                });
                MapEvent eventE = new MapEvent(4, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "lightID"] = new JSONArray()
                    {
                        [0] = 1,
                        [1] = 2
                    },
                    [MapLoader.heckUnderscore + "color"] = new Color(1, 0, 1)
                });
                MapEvent eventF = new MapEvent(3, MapEvent.EventTypeRingLights, MapEvent.LightValueRedON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "lightID"] = 3,
                    [MapLoader.heckUnderscore + "color"] = new Color(0, 1, 1)
                });

                foreach (MapEvent mapEvent in new MapEvent[] { eventA, eventB, eventC, eventD, eventE, eventF })
                {
                    eventPlacement.queuedData = mapEvent;
                    eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                    eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                    eventPlacement.ApplyToMap();
                }

                SelectionController.Select(eventC);
                SelectionController.Select(eventD, true);
                SelectionController.Select(eventE, true);

                StrobeGenerator strobeGenerator = Object.FindObjectOfType<StrobeGenerator>();
                strobeGenerator.GenerateStrobe(new List<StrobeGeneratorPass>()
                {
                    new StrobeStepGradientPass(MapEvent.LightValueBlueON, false, 2, Easing.Linear)
                });

                // Current _lightID from the first event is used. As eventC is added first here we always get a single light id
                // If this changes in future then update below, this test wasn't really meant to enforce this behaviour
                CheckEvent(eventsContainer, 2, 2.5f, MapEvent.EventTypeRingLights, MapEvent.LightValueBlueON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "color"] = new Color(1, 0.5f, 0),
                    [MapLoader.heckUnderscore + "lightID"] = 1
                });
                CheckEvent(eventsContainer, 6, 3.5f, MapEvent.EventTypeRingLights, MapEvent.LightValueBlueON, new JSONObject
                {
                    [MapLoader.heckUnderscore + "color"] = new Color(1, 0, 0.5f),
                    [MapLoader.heckUnderscore + "lightID"] = 1
                });
            }
        }
    }
}

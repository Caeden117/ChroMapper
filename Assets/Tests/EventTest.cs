using NUnit.Framework;
using System.Collections;
using System.Linq;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EventTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupEvents();
        }

        public static void CheckEvent(BeatmapObjectContainerCollection container, int idx, int time, int type, int value, JSONNode customData = null)
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
        public void Invert()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            if (containerCollection is EventsContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                var eventA = new MapEvent(2, MapEvent.EVENT_TYPE_LATE_ROTATION, MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf(45));
                var eventB = new MapEvent(3, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_RED_FADE);

                eventPlacement.queuedData = eventA;
                eventPlacement.queuedValue = eventPlacement.queuedData._value;
                eventPlacement.RoundedTime = eventPlacement.queuedData._time;
                eventPlacement.ApplyToMap();
                
                eventPlacement.queuedData = eventB;
                eventPlacement.queuedValue = eventPlacement.queuedData._value;
                eventPlacement.RoundedTime = eventPlacement.queuedData._time;
                eventPlacement.ApplyToMap();

                if (eventsContainer.LoadedContainers[eventA] is BeatmapEventContainer containerA)
                {
                    inputController.InvertEvent(containerA);
                }
                if (eventsContainer.LoadedContainers[eventB] is BeatmapEventContainer containerB)
                {
                    inputController.InvertEvent(containerB);
                }

                CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_LATE_ROTATION, MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf(-45));
                CheckEvent(eventsContainer, 1, 3, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_BLUE_FADE);

                // Undo invert
                actionContainer.Undo();
                
                CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_LATE_ROTATION, MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf(-45));
                CheckEvent(eventsContainer, 1, 3, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_RED_FADE);
                
                actionContainer.Undo();
                
                CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_LATE_ROTATION, MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf(45));
                CheckEvent(eventsContainer, 1, 3, MapEvent.EVENT_TYPE_BACK_LASERS, MapEvent.LIGHT_VALUE_RED_FADE);
            }
        }
        
        [Test]
        public void TweakValue()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            if (containerCollection is EventsContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                var eventA = new MapEvent(2, MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED, 2);

                eventPlacement.queuedData = eventA;
                eventPlacement.queuedValue = eventPlacement.queuedData._value;
                eventPlacement.RoundedTime = eventPlacement.queuedData._time;
                eventPlacement.PlacePrecisionRotation = true;
                eventPlacement.ApplyToMap();
                eventPlacement.PlacePrecisionRotation = false;

                if (eventsContainer.LoadedContainers[eventA] is BeatmapEventContainer containerA)
                {
                    inputController.TweakValue(containerA, 1);
                }

                CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED, 3);

                // Undo invert
                actionContainer.Undo();
                
                CheckEvent(eventsContainer, 0, 2, MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED, 2);
            }
        }
    }
}

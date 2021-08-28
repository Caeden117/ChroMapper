using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EventTest
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

        public static void CheckEvent(BeatmapObjectContainerCollection container, int idx, int time, int type, int value, JSONNode customData = null)
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
        public void Invert()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            if (containerCollection is EventsContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();
                BeatmapEventInputController inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                MapEvent eventA = new MapEvent(2, MapEvent.EventTypeLateRotation, MapEvent.LightValueToRotationDegrees.ToList().IndexOf(45));
                MapEvent eventB = new MapEvent(3, MapEvent.EventTypeBackLasers, MapEvent.LightValueRedFade);

                eventPlacement.QueuedData = eventA;
                eventPlacement.QueuedValue = eventPlacement.QueuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.QueuedData.Time;
                eventPlacement.ApplyToMap();

                eventPlacement.QueuedData = eventB;
                eventPlacement.QueuedValue = eventPlacement.QueuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.QueuedData.Time;
                eventPlacement.ApplyToMap();

                if (eventsContainer.LoadedContainers[eventA] is BeatmapEventContainer containerA)
                {
                    inputController.InvertEvent(containerA);
                }
                if (eventsContainer.LoadedContainers[eventB] is BeatmapEventContainer containerB)
                {
                    inputController.InvertEvent(containerB);
                }

                CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeLateRotation, MapEvent.LightValueToRotationDegrees.ToList().IndexOf(-45));
                CheckEvent(eventsContainer, 1, 3, MapEvent.EventTypeBackLasers, MapEvent.LightValueBlueFade);

                // Undo invert
                actionContainer.Undo();

                CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeLateRotation, MapEvent.LightValueToRotationDegrees.ToList().IndexOf(-45));
                CheckEvent(eventsContainer, 1, 3, MapEvent.EventTypeBackLasers, MapEvent.LightValueRedFade);

                actionContainer.Undo();

                CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeLateRotation, MapEvent.LightValueToRotationDegrees.ToList().IndexOf(45));
                CheckEvent(eventsContainer, 1, 3, MapEvent.EventTypeBackLasers, MapEvent.LightValueRedFade);
            }
        }

        [Test]
        public void TweakValue()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            if (containerCollection is EventsContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();
                BeatmapEventInputController inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                MapEvent eventA = new MapEvent(2, MapEvent.EventTypeLeftLasersSpeed, 2);

                eventPlacement.QueuedData = eventA;
                eventPlacement.QueuedValue = eventPlacement.QueuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.QueuedData.Time;
                eventPlacement.PlacePrecisionRotation = true;
                eventPlacement.ApplyToMap();
                eventPlacement.PlacePrecisionRotation = false;

                if (eventsContainer.LoadedContainers[eventA] is BeatmapEventContainer containerA)
                {
                    inputController.TweakValue(containerA, 1);
                }

                CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeLeftLasersSpeed, 3);

                // Undo invert
                actionContainer.Undo();

                CheckEvent(eventsContainer, 0, 2, MapEvent.EventTypeLeftLasersSpeed, 2);
            }
        }
    }
}

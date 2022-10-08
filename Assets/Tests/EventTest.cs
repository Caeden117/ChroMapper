using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V2;
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
            IObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<IEvent>(newObjA);
            if (newObjA is IEvent newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time);
                Assert.AreEqual(type, newNoteA.Type);
                Assert.AreEqual(value, newNoteA.Value);

                // ConvertToJSON causes gradient to get updated
                if (customData != null)
                {
                    Assert.AreEqual(customData.ToString(), newNoteA.ToJson()["_customData"].ToString());
                }
            }
        }

        [Test]
        public void Invert()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();
                BeatmapEventInputController inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                IEvent eventA = new V2Event(2, (int)EventTypeValue.LateLaneRotation, IEvent.LightValueToRotationDegrees.ToList().IndexOf(45));
                IEvent eventB = new V2Event(3, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);

                eventPlacement.queuedData = eventA;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.ApplyToMap();

                eventPlacement.queuedData = eventB;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.ApplyToMap();

                if (eventsContainer.LoadedContainers[eventA] is EventContainer containerA)
                {
                    inputController.InvertEvent(containerA);
                }
                if (eventsContainer.LoadedContainers[eventB] is EventContainer containerB)
                {
                    inputController.InvertEvent(containerB);
                }

                CheckEvent(eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, IEvent.LightValueToRotationDegrees.ToList().IndexOf(-45));
                CheckEvent(eventsContainer, 1, 3, (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade);

                // Undo invert
                actionContainer.Undo();

                CheckEvent(eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, (int)IEvent.LightValueToRotationDegrees.ToList().IndexOf(-45));
                CheckEvent(eventsContainer, 1, 3, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);

                actionContainer.Undo();

                CheckEvent(eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, (int)IEvent.LightValueToRotationDegrees.ToList().IndexOf(45));
                CheckEvent(eventsContainer, 1, 3, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);
            }
        }

        [Test]
        public void TweakValue()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();
                BeatmapEventInputController inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                IEvent eventA = new V2Event(2, (int)EventTypeValue.LeftLaserRotation, 2);

                eventPlacement.queuedData = eventA;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.PlacePrecisionRotation = true;
                eventPlacement.ApplyToMap();
                eventPlacement.PlacePrecisionRotation = false;

                if (eventsContainer.LoadedContainers[eventA] is EventContainer containerA)
                {
                    inputController.TweakValue(containerA, 1);
                }

                CheckEvent(eventsContainer, 0, 2, (int)EventTypeValue.LeftLaserRotation, 3);

                // Undo invert
                actionContainer.Undo();

                CheckEvent(eventsContainer, 0, 2, (int)EventTypeValue.LeftLaserRotation, 2);
            }
        }
    }
}

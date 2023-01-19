using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V3;
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

        public static void CheckEvent(string msg, BeatmapObjectContainerCollection container, int idx, float time, int type, int value, float floatValue = 1f, JSONNode customData = null)
        {
            BaseObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseEvent>(newObjA);
            if (newObjA is BaseEvent newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time, 0.001f, $"{msg}: Mismatched time");
                Assert.AreEqual(type, newNoteA.Type, $"{msg}: Mismatched type");
                Assert.AreEqual(value, newNoteA.Value, $"{msg}: Mismatched value");
                Assert.AreEqual(floatValue, newNoteA.FloatValue, 0.001f, $"{msg}: Mismatched float value");

                // ConvertToJSON causes gradient to get updated
                if (customData != null)
                {
                    // Custom data needed to be saved before compare
                    newNoteA.SaveCustom();
                    Assert.AreEqual(customData.ToString(), newNoteA.CustomData?.ToString(), $"{msg}: Mismatched custom data");
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

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.LateLaneRotation, BaseEvent.LightValueToRotationDegrees.ToList().IndexOf(45));
                BaseEvent baseEventB = new V3BasicEvent(3, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);

                eventPlacement.queuedData = baseEventA;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.ApplyToMap();

                eventPlacement.queuedData = baseEventB;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.ApplyToMap();

                if (eventsContainer.LoadedContainers[baseEventA] is EventContainer containerA)
                {
                    inputController.InvertEvent(containerA);
                }
                if (eventsContainer.LoadedContainers[baseEventB] is EventContainer containerB)
                {
                    inputController.InvertEvent(containerB);
                }

                CheckEvent("Perform first rotation inversion", eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, BaseEvent.LightValueToRotationDegrees.ToList().IndexOf(-45));
                CheckEvent("Perform first light value inversion", eventsContainer, 1, 3, (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade);

                if (eventsContainer.LoadedContainers[baseEventB] is EventContainer containerB2)
                {
                    inputController.InvertEvent(containerB2);
                }

                CheckEvent("Perform second light value inversion", eventsContainer, 1, 3, (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade);

                // Undo invert
                actionContainer.Undo();

                CheckEvent("Undo second light value inversion", eventsContainer, 1, 3, (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade);
                CheckEvent("Check first rotation inversion", eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, (int)BaseEvent.LightValueToRotationDegrees.ToList().IndexOf(-45));

                actionContainer.Undo();

                CheckEvent("Undo first light value inversion", eventsContainer, 1, 3, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);
                CheckEvent("Check first rotation inversion", eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, (int)BaseEvent.LightValueToRotationDegrees.ToList().IndexOf(-45));
                
                actionContainer.Undo();
                
                CheckEvent("Undo first rotation inversion", eventsContainer, 0, 2, (int)EventTypeValue.LateLaneRotation, (int)BaseEvent.LightValueToRotationDegrees.ToList().IndexOf(45));
                CheckEvent("Check initial light value", eventsContainer, 1, 3, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);
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

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.LeftLaserRotation, 2);

                eventPlacement.queuedData = baseEventA;
                eventPlacement.queuedValue = eventPlacement.queuedData.Value;
                eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
                eventPlacement.PlacePrecisionRotation = true;
                eventPlacement.ApplyToMap();
                eventPlacement.PlacePrecisionRotation = false;

                if (eventsContainer.LoadedContainers[baseEventA] is EventContainer containerA)
                {
                    inputController.TweakMain(containerA, 1);
                }

                CheckEvent("Perform tweak value", eventsContainer, 0, 2, (int)EventTypeValue.LeftLaserRotation, 3);

                // Undo invert
                actionContainer.Undo();

                CheckEvent("Undo tweak value", eventsContainer, 0, 2, (int)EventTypeValue.LeftLaserRotation, 2);
            }
        }
    }
}

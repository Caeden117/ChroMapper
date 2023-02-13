using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
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

        // TODO: need to change rotation event here as well, man
        [Test]
        public void Invert()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                BaseEvent baseEventA = new V3RotationEvent(2, 1, 45);
                BaseEvent baseEventB = new V3BasicEvent(3, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventA);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventB);

                // TODO: u know, i forgot this events get converted and now i have to suffer the wrath of test pain 
                if (eventsContainer.LoadedContainers[baseEventA] is EventContainer containerA)
                    inputController.InvertEvent(containerA);
                if (eventsContainer.LoadedContainers[baseEventB] is EventContainer containerB)
                    inputController.InvertEvent(containerB);

                CheckUtils.CheckRotationEvent("Perform first rotation inversion", eventsContainer, 0, 2, 1, -45);
                CheckUtils.CheckEvent("Perform first light value inversion", eventsContainer, 1, 3,
                    (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade);

                if (eventsContainer.LoadedContainers[baseEventB] is EventContainer containerB2)
                    inputController.InvertEvent(containerB2);

                CheckUtils.CheckEvent("Perform second light value inversion", eventsContainer, 1, 3,
                    (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckEvent("Undo second light value inversion", eventsContainer, 1, 3,
                    (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade);
                CheckUtils.CheckRotationEvent("Check first rotation inversion", eventsContainer, 0, 2, 1, -45);

                actionContainer.Undo();

                CheckUtils.CheckEvent("Undo first light value inversion", eventsContainer, 1, 3,
                    (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);
                CheckUtils.CheckRotationEvent("Check first rotation inversion", eventsContainer, 0, 2, 1, -45);

                actionContainer.Undo();

                CheckUtils.CheckRotationEvent("Undo first rotation inversion", eventsContainer, 0, 2, 1, 45);
                CheckUtils.CheckEvent("Check initial light value", eventsContainer, 1, 3,
                    (int)EventTypeValue.BackLasers, (int)LightValue.RedFade);
            }
        }

        [Test]
        public void TweakValue()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.LeftLaserRotation, 2);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventA, true);

                if (eventsContainer.LoadedContainers[baseEventA] is EventContainer containerA)
                    inputController.TweakMain(containerA, 1);

                CheckUtils.CheckEvent("Perform tweak value", eventsContainer, 0, 2,
                    (int)EventTypeValue.LeftLaserRotation, 3);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckEvent("Undo tweak value", eventsContainer, 0, 2, (int)EventTypeValue.LeftLaserRotation,
                    2);
            }
        }
    }
}
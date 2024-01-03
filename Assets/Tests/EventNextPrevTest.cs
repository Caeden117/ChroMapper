using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EventNextPrevTest
    {
        private bool originalLightIDSetting;

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
        public void Placement()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                BaseEvent baseEvent1 = new V3BasicEvent(1, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn);
                BaseEvent baseEvent2 = new V3BasicEvent(2, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn);
                BaseEvent baseEvent3 = new V3BasicEvent(3, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn);
                BaseEvent baseEvent4 = new V3BasicEvent(4, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn);

                // Check state after placing
                // 1 -> 2 -> 3 -> 4
                PlaceUtils.PlaceEvent(eventPlacement, baseEvent1);
                PlaceUtils.PlaceEvent(eventPlacement, baseEvent4);
                PlaceUtils.PlaceEvent(eventPlacement, baseEvent2);
                PlaceUtils.PlaceEvent(eventPlacement, baseEvent3);

                AssertEventLinkOrder(new List<BaseEvent> { baseEvent1, baseEvent2, baseEvent3, baseEvent4 });

                // Check state after deleting
                // 1 ->   -> 3 -> 4
                eventsContainer.DeleteObject(baseEvent2);

                AssertEventLinkOrder(new List<BaseEvent> { baseEvent1, baseEvent3, baseEvent4 });

                // Check state after undo and redo
                actionContainer.Undo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEvent1, baseEvent2, baseEvent3, baseEvent4 });

                actionContainer.Redo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEvent1, baseEvent3, baseEvent4 });
            }
        }

        [Test]
        public void DeletingSelection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                BaseEvent baseEvent1 = new V3BasicEvent(1, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn);
                BaseEvent baseEvent2 = new V3BasicEvent(2, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn);
                BaseEvent baseEvent3 = new V3BasicEvent(3, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn);
                BaseEvent baseEvent4 = new V3BasicEvent(4, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn);

                // Check state after placing
                // 1 -> 2 -> 3 -> 4
                PlaceUtils.PlaceEvent(eventPlacement, baseEvent1);
                PlaceUtils.PlaceEvent(eventPlacement, baseEvent4);
                PlaceUtils.PlaceEvent(eventPlacement, baseEvent2);
                PlaceUtils.PlaceEvent(eventPlacement, baseEvent3);

                AssertEventLinkOrder(new List<BaseEvent> { baseEvent1, baseEvent2, baseEvent3, baseEvent4 }, "Place");

                // Check state after deleting
                // 1 ->   -> 3 ->
                SelectionController.Select(baseEvent2);
                SelectionController.Select(baseEvent4, true);
                selectionController.Delete();

                AssertEventLinkOrder(new List<BaseEvent> { baseEvent1, baseEvent3 }, "Delete");

                // Check state after undo and redo
                actionContainer.Undo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEvent1, baseEvent2, baseEvent3, baseEvent4 }, "Undo");

                actionContainer.Redo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEvent1, baseEvent3 }, "Redo");
            }
        }

        [Test]
        public void ShiftingSelection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                BaseEvent baseEventA1 = new V3BasicEvent(1, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventT2 = new V3BasicEvent(2, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventA3 = new V3BasicEvent(3, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventT4 = new V3BasicEvent(4, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventB1 = new V3BasicEvent(1, (int)EventTypeValue.RightLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventB3 = new V3BasicEvent(3, (int)EventTypeValue.RightLasers, (int)LightValue.BlueOn);

                // Check state after placing
                // A1 -> T2 -> A3 -> T4
                // B1 ->    -> B3 ->
                PlaceUtils.PlaceEvent(eventPlacement, baseEventA1);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventA3);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventB1);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventB3);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventT2);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventT4);

                AssertEventLinkOrder(new List<BaseEvent> { baseEventA1, baseEventT2, baseEventA3, baseEventT4 }, "Place");
                AssertEventLinkOrder(new List<BaseEvent> { baseEventB1, baseEventB3 }, "Place");

                // Check state after shifting eventT
                // A1 ->    -> A3 ->
                // B1 -> T2 -> B3 -> T4
                SelectionController.Select(baseEventT2);
                SelectionController.Select(baseEventT4, true);
                selectionController.ShiftSelection(1, 0);

                AssertEventLinkOrder(new List<BaseEvent> { baseEventA1, baseEventA3 }, "Shift");
                AssertEventLinkOrder(new List<BaseEvent> { baseEventB1, baseEventT2, baseEventB3, baseEventT4 }, "Shift");

                // Check state after undo and redo
                actionContainer.Undo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEventA1, baseEventT2, baseEventA3, baseEventT4 }, "Undo");
                AssertEventLinkOrder(new List<BaseEvent> { baseEventB1, baseEventB3 }, "Undo");

                actionContainer.Redo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEventA1, baseEventA3 }, "Redo");
                AssertEventLinkOrder(new List<BaseEvent> { baseEventB1, baseEventT2, baseEventB3, baseEventT4 }, "Redo");
            }
        }

        [Test]
        public void MovingSelection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

                BaseEvent baseEventA = new V3BasicEvent(1, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventT1 = new V3BasicEvent(1.5f, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventB = new V3BasicEvent(2, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventT2 = new V3BasicEvent(2.5f, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);

                // Check state after placing
                // A -> T1 -> B -> T2
                PlaceUtils.PlaceEvent(eventPlacement, baseEventA);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventB);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventT1);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventT2);

                AssertEventLinkOrder(new List<BaseEvent> { baseEventA, baseEventT1, baseEventB, baseEventT2 }, "Place");

                // Check state after moving eventT
                // A ->   -> B -> T1 -> T2
                SelectionController.Select(baseEventT1);
                SelectionController.Select(baseEventT2, true);
                selectionController.MoveSelection(0.75f);

                AssertEventLinkOrder(new List<BaseEvent> { baseEventA, baseEventB, baseEventT1, baseEventT2 }, "Move");

                // Check state after undo and redo
                actionContainer.Undo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEventA, baseEventT1, baseEventB, baseEventT2 }, "Undo");

                actionContainer.Redo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEventA, baseEventB, baseEventT1, baseEventT2 }, "Redo");
            }
        }

        [Test]
        public void CopyPasteSelection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (containerCollection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapEventInputController>();
                var atsc = root.GetComponentInChildren<AudioTimeSyncController>();

                BaseEvent baseEventA = new V3BasicEvent(1, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
                BaseEvent baseEventB = new V3BasicEvent(2, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);

                // Check state after placing
                // A -> B
                PlaceUtils.PlaceEvent(eventPlacement, baseEventA);
                PlaceUtils.PlaceEvent(eventPlacement, baseEventB);

                AssertEventLinkOrder(new List<BaseEvent> { baseEventA, baseEventB }, "Place");

                // Check state after pasting
                // A -> B -> A Copy -> B copy
                SelectionController.Select(baseEventA);
                SelectionController.Select(baseEventB, true);
                atsc.MoveToJsonTime(3);
                selectionController.Copy();
                selectionController.Paste();

                AssertEventLinkOrder(new List<BaseEvent> { baseEventA, baseEventB,
                    containerCollection.LoadedObjects.ElementAt(2) as BaseEvent,
                    containerCollection.LoadedObjects.ElementAt(3) as BaseEvent }, "Paste");

                // Check state after undo and redo
                actionContainer.Undo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEventA, baseEventB }, "Undo");

                actionContainer.Redo();
                AssertEventLinkOrder(new List<BaseEvent> { baseEventA, baseEventB,
                    containerCollection.LoadedObjects.ElementAt(2) as BaseEvent,
                    containerCollection.LoadedObjects.ElementAt(3) as BaseEvent }, "Redo");
            }
        }

        private void AssertEventLinkOrder(IList<BaseEvent> events, string msg = "")
        {
            if (events.Count == 1)
            {
                CheckUtils.CheckEventPrevAndNext($"{msg} - 0", events[0], null, null);
                return;
            }

            for (var i = 0; i < events.Count; i++)
            {
                if (i == 0)
                    CheckUtils.CheckEventPrevAndNext($"{msg} - {i}", events[i], null, events[i + 1]);
                else if (i == events.Count - 1)
                    CheckUtils.CheckEventPrevAndNext($"{msg} - {i}", events[i], events[i - 1], null);
                else
                    CheckUtils.CheckEventPrevAndNext($"{msg} - {i}", events[i], events[i - 1], events[i + 1]);
            }
        }
    }
}
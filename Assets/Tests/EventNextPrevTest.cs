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
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EventNextPrevTest
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
        public void Placement()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

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
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after deleting
            // 1 ->   -> 3 -> 4
            eventsContainer.DeleteObject(baseEvent2);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after undo and redo
            actionContainer.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            actionContainer.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);
        }

        [Test]
        public void DeletingSelection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

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
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after deleting
            // 1 ->   -> 3 ->
            SelectionController.Select(baseEvent2);
            SelectionController.Select(baseEvent4, true);
            selectionController.Delete();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            // Check state after undo and redo
            actionContainer.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);

            actionContainer.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights);
        }

        [Test]
        public void ShiftingSelection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

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

            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            // Check state after shifting eventT
            // A1 ->    -> A3 ->
            // B1 -> T2 -> B3 -> T4
            SelectionController.Select(baseEventT2);
            SelectionController.Select(baseEventT4, true);
            selectionController.ShiftSelection(1, 0);

            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            // Check state after undo and redo
            actionContainer.Undo();
            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);

            actionContainer.Redo();
            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.RightLasers);
        }

        [Test]
        public void MovingSelection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

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
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after moving eventT
            // A ->   -> B -> T1 -> T2
            SelectionController.Select(baseEventT1);
            SelectionController.Select(baseEventT2, true);
            selectionController.MoveSelection(0.75f);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after undo and redo
            actionContainer.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            actionContainer.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
        }

        [Test]
        public void CopyPasteSelection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();
            var atsc = root.GetComponentInChildren<AudioTimeSyncController>();

            BaseEvent baseEventA = new V3BasicEvent(1, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);
            BaseEvent baseEventB = new V3BasicEvent(2, (int)EventTypeValue.LeftLasers, (int)LightValue.BlueOn);

            // Check state after placing
            // A -> B
            PlaceUtils.PlaceEvent(eventPlacement, baseEventA);
            PlaceUtils.PlaceEvent(eventPlacement, baseEventB);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after pasting
            // A -> B -> A Copy -> B copy
            SelectionController.Select(baseEventA);
            SelectionController.Select(baseEventB, true);
            atsc.MoveToJsonTime(3);
            selectionController.Copy();
            selectionController.Paste();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            // Check state after undo and redo
            actionContainer.Undo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);

            actionContainer.Redo();
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.LeftLasers);
        }

        private void AssertMapObjectsAreLinkedAndSorted(EventGridContainer eventsContainer, int eventType)
        {
            var laneEvents = eventsContainer.MapObjects.Where(x => x.Type == eventType).ToList();
            CheckUtils.CheckEventsLinksAreCorrectAndSorted(laneEvents);
        }
    }
}
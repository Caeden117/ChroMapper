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
    public class EventNextPrevLightIDTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            Settings.Instance.LightIDTransitionSupport = false;
            TestUtils.ReturnSettings();
        }
        
        [OneTimeSetUp]
        public void Setup()
        {
            // This is an opt-in setting
            Settings.Instance.LightIDTransitionSupport = true;
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event).PropagationEditing = EventGridContainer.PropMode.Off;

            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupEvents();
        }

        private BaseEvent EventWithTimeAndLightID(float time, int? lightID)
        {
            Settings.Instance.MapVersion = 3;
            
            var customData = lightID.HasValue
            ? new JSONObject
            {
                ["lightID"] = new JSONArray
                {
                    [0] = lightID
                }
            }
            : null;

            var evt = new BaseEvent
                { JsonTime = time, Type = (int)EventTypeValue.CenterLights, Value = (int)LightValue.BlueOn, CustomData = customData };
            return evt;
        }

        [Test]
        public void Placement()
        {
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            BaseEvent V1 = EventWithTimeAndLightID(1, null);
            BaseEvent V10 = EventWithTimeAndLightID(10, null);

            BaseEvent A2 = EventWithTimeAndLightID(2, 1);
            BaseEvent A4 = EventWithTimeAndLightID(4, 1);
            BaseEvent A12 = EventWithTimeAndLightID(12, 1);

            BaseEvent B3 = EventWithTimeAndLightID(3, 2);
            BaseEvent B5 = EventWithTimeAndLightID(5, 2);
            BaseEvent B13 = EventWithTimeAndLightID(13, 2);

            // Check state after placing
            PlaceUtils.PlaceEvents(eventPlacement, new List<BaseEvent> { V1, A2, B3, A4, B5, V10, A12, B13 });
            AssertMapObjectsLinksState(eventsContainer);
            
            // Check state after deleting
            // V1             V10
            //    A2              A12
            //       B3    B5          B13
            eventsContainer.DeleteObject(A4);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after deleting
            // V1                
            //    A2              A12
            //       B3    B5          B13
            eventsContainer.DeleteObject(V10);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);

            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        [Test]
        public void DeletingSelection()
        {
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            BaseEvent V1 = EventWithTimeAndLightID(1, null);
            BaseEvent V10 = EventWithTimeAndLightID(10, null);

            BaseEvent A2 = EventWithTimeAndLightID(2, 1);
            BaseEvent A4 = EventWithTimeAndLightID(4, 1);
            BaseEvent A12 = EventWithTimeAndLightID(12, 1);

            BaseEvent B3 = EventWithTimeAndLightID(3, 2);
            BaseEvent B5 = EventWithTimeAndLightID(5, 2);
            BaseEvent B13 = EventWithTimeAndLightID(13, 2);

            PlaceUtils.PlaceEvents(eventPlacement, new List<BaseEvent> { V1, A2, B3, A4, B5, V10, A12, B13 });

            // Check state after deleting
            // V1                
            //    A2              A12
            //       B3    B5          B13
            SelectionController.Select(A4);
            SelectionController.Select(V10, true);
            selectionController.Delete();
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);

            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        [Test]
        public void CopyPasteSelection()
        {
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var atsc = Object.FindObjectOfType<AudioTimeSyncController>();

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            BaseEvent V1 = EventWithTimeAndLightID(1, null);
            BaseEvent V10 = EventWithTimeAndLightID(10, null);

            BaseEvent A2 = EventWithTimeAndLightID(2, 1);
            BaseEvent A4 = EventWithTimeAndLightID(4, 1);
            BaseEvent A12 = EventWithTimeAndLightID(12, 1);

            BaseEvent B3 = EventWithTimeAndLightID(3, 2);
            BaseEvent B5 = EventWithTimeAndLightID(5, 2);
            BaseEvent B13 = EventWithTimeAndLightID(13, 2);

            PlaceUtils.PlaceEvents(eventPlacement, new List<BaseEvent> { V1, A2, B3, A4, B5, V10, A12, B13 });

            // Check state after pasting
            // V1             V1C         V10
            //    A2    A4        A2C         A12     A12C
            //       B3    B5         B3C         B13      B13C
            SelectionController.Select(V1);
            SelectionController.Select(A2, true);
            SelectionController.Select(B3, true);
            SelectionController.Select(A12, true);
            SelectionController.Select(B13, true);
            atsc.MoveToJsonTime(6);
            selectionController.Copy();
            selectionController.Paste();

            var V1C = eventsContainer.MapObjects.ElementAt(5) as BaseEvent;
            var A2C = eventsContainer.MapObjects.ElementAt(6) as BaseEvent;
            var B3C = eventsContainer.MapObjects.ElementAt(7) as BaseEvent;
            var A12C = eventsContainer.MapObjects.ElementAt(11) as BaseEvent;
            var B13C = eventsContainer.MapObjects.ElementAt(12) as BaseEvent;

            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);


            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        [Test]
        public void ShiftingSelection()
        {
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Light;

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            BaseEvent V1 = EventWithTimeAndLightID(1, null);
            BaseEvent V10 = EventWithTimeAndLightID(10, null);

            BaseEvent A2 = EventWithTimeAndLightID(2, 1);
            BaseEvent A4 = EventWithTimeAndLightID(4, 1);
            BaseEvent A12 = EventWithTimeAndLightID(12, 1);

            BaseEvent B3 = EventWithTimeAndLightID(3, 2);
            BaseEvent B5 = EventWithTimeAndLightID(5, 2);
            BaseEvent B13 = EventWithTimeAndLightID(13, 2);

            PlaceUtils.PlaceEvents(eventPlacement, new List<BaseEvent> { V1, A2, B3, A4, B5, V10, A12, B13 });

            // Check state after shifting
            // V1                
            //    A2    A4    V10 A12
            //       B3    B5          B13
            SelectionController.Select(V10);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after shifting
            // V1                
            //    A2              A12
            //       B3 A4 B5 V10      B13
            SelectionController.Select(A4);
            SelectionController.Select(V10, true);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);

            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        [Test]
        public void MovingSelection()
        {
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            // These are the events
            // V1             V10
            //    A2    A4        A12
            //       B3    B5          B13
            BaseEvent V1 = EventWithTimeAndLightID(1, null);
            BaseEvent V10 = EventWithTimeAndLightID(10, null);

            BaseEvent A2 = EventWithTimeAndLightID(2, 1);
            BaseEvent A4 = EventWithTimeAndLightID(4, 1);
            BaseEvent A12 = EventWithTimeAndLightID(12, 1);

            BaseEvent B3 = EventWithTimeAndLightID(3, 2);
            BaseEvent B5 = EventWithTimeAndLightID(5, 2);
            BaseEvent B13 = EventWithTimeAndLightID(13, 2);

            PlaceUtils.PlaceEvents(eventPlacement, new List<BaseEvent> { V1, A2, B3, A4, B5, V10, A12, B13 });

            // Check state after moving
            // V1             V10
            //          A4        A12      A2
            //             B5          B13    B3
            SelectionController.Select(A2);
            SelectionController.Select(B3, true);
            selectionController.MoveSelection(12);
            AssertMapObjectsLinksState(eventsContainer);

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertMapObjectsLinksState(eventsContainer);

            actionsContainer.Redo();
            AssertMapObjectsLinksState(eventsContainer);
        }

        private void AssertMapObjectsLinksState(EventGridContainer eventsContainer)
        {
            CheckUtils.CheckEventsAreSorted(eventsContainer.MapObjects);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights, null);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights, 1);
            AssertMapObjectsAreLinkedAndSorted(eventsContainer, (int)EventTypeValue.CenterLights, 2);
        }
        
        private void AssertMapObjectsAreLinkedAndSorted(EventGridContainer eventsContainer, int eventType, int? lightID)
        {
            var laneEvents = lightID == null
                ? eventsContainer.MapObjects.Where(x => x.Type == eventType && x.CustomLightID == null).ToList()
                : eventsContainer.MapObjects.Where(x => x.Type == eventType && x.CustomLightID != null && x.CustomLightID[0] == lightID).ToList();

            CheckUtils.CheckEventsLinksAreCorrectAndSorted(laneEvents);
        }
    }
}
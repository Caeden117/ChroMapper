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
        private bool originalChromaAdvancedSetting;
        private bool originalLightIDSetting;

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
            var customData = lightID.HasValue
            ? new JSONObject
            {
                ["lightID"] = new JSONArray
                {
                    [0] = lightID
                }
            }
            : null;

            return new V3BasicEvent(time, (int)EventTypeValue.CenterLights, (int)LightValue.BlueOn, customData: customData);
        }

        [Test]
        public void Placement()
        {
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();
            var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

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
            AssertEventLinkOrder(new List<BaseEvent> { V1, V10 }, "Place LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A4, A12 }, "Place LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Place LightID B");

            // Check state after deleting
            // V1             V10
            //    A2              A12
            //       B3    B5          B13
            eventsContainer.DeleteObject(A4);
            AssertEventLinkOrder(new List<BaseEvent> { V1, V10 }, "Delete LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A12 }, "Delete LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Delete LightID B");

            // Check state after deleting
            // V1                
            //    A2              A12
            //       B3    B5          B13
            eventsContainer.DeleteObject(V10);
            AssertEventLinkOrder(new List<BaseEvent> { V1 }, "Delete 2 LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A12 }, "Delete 2 LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Delete 2 LightID B");

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertEventLinkOrder(new List<BaseEvent> { V1, V10 }, "Undo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A12 }, "Undo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Undo LightID B");

            actionsContainer.Redo();
            AssertEventLinkOrder(new List<BaseEvent> { V1 }, "Redo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A12 }, "Redo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Redo LightID B");
        }

        [Test]
        public void DeletingSelection()
        {
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();
            var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

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
            AssertEventLinkOrder(new List<BaseEvent> { V1 }, "Delete LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A12 }, "Delete LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Delete LightID B");

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertEventLinkOrder(new List<BaseEvent> { V1, V10 }, "Undo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A4, A12 }, "Undo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Undo LightID B");

            actionsContainer.Redo();
            AssertEventLinkOrder(new List<BaseEvent> { V1 }, "Redo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A12 }, "Redo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Redo LightID B");
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
            var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

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

            AssertEventLinkOrder(new List<BaseEvent> { V1, V1C, V10 }, "Paste LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A4, A2C, A12, A12C }, "Paste LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B3C, B13, B13C }, "Paste LightID B");

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertEventLinkOrder(new List<BaseEvent> { V1, V10 }, "Undo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A4, A12 }, "Undo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Undo LightID B");


            actionsContainer.Redo();
            AssertEventLinkOrder(new List<BaseEvent> { V1, V1C, V10 }, "Redo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A4, A2C, A12, A12C }, "Redo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B3C, B13, B13C }, "Redo LightID B");
        }

        [Test]
        public void ShiftingSelection()
        {
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();
            var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

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
            selectionController.ShiftSelection(1, 0);
            AssertEventLinkOrder(new List<BaseEvent> { V1 }, "Shift LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A4, V10, A12 }, "Shift LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Shift LightID B");

            // Check state after shifting
            // V1                
            //    A2              A12
            //       B3 A4 B5 V10      B13
            SelectionController.Select(A4);
            SelectionController.Select(V10, true);
            selectionController.ShiftSelection(1, 0);
            AssertEventLinkOrder(new List<BaseEvent> { V1 }, "Shift 2 LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A12 }, "Shift 2 LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, A4, B5, V10, B13 }, "Shift 2 LightID B");

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertEventLinkOrder(new List<BaseEvent> { V1 }, "Undo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A4, V10, A12 }, "Undo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Undo LightID B");

            actionsContainer.Redo();
            AssertEventLinkOrder(new List<BaseEvent> { V1 }, "Redo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A12 }, "Redo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, A4, B5, V10, B13 }, "Redo LightID B");
        }

        [Test]
        public void MovingSelection()
        {
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var actionsContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();
            var inputController = root.GetComponentInChildren<BeatmapEventInputController>();

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
            AssertEventLinkOrder(new List<BaseEvent> { V1, V10 }, "Move LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A4, A12, A2 }, "Move LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B5, B13, B3 }, "Move LightID B");

            // Check state after undo and redo
            actionsContainer.Undo();
            AssertEventLinkOrder(new List<BaseEvent> { V1, V10 }, "Undo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A2, A4, A12 }, "Undo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B3, B5, B13 }, "Undo LightID B");

            actionsContainer.Redo();
            AssertEventLinkOrder(new List<BaseEvent> { V1, V10 }, "Redo LightID V");
            AssertEventLinkOrder(new List<BaseEvent> { A4, A12, A2 }, "Redo LightID A");
            AssertEventLinkOrder(new List<BaseEvent> { B5, B13, B3 }, "Redo LightID B");
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
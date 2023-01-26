using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class PaintTest
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
        public void PaintGradientUndo()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var colorPicker = Object.FindObjectOfType<ColorPicker>();
            var painter = Object.FindObjectOfType<PaintSelectedObjects>();

            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            var root = eventsContainer.transform.root;

            var selectionController = root.GetComponentInChildren<SelectionController>();
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            var customData = new JSONObject();
            customData["_lightGradient"] = new ChromaLightGradient(Color.blue, Color.cyan).ToJson();
            BaseEvent baseEventA = new V2Event(2, 1, 1, 1, customData);
            PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

            SelectionController.Select(baseEventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(2, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, ((BaseEvent)eventsContainer.UnsortedObjects[0]).CustomLightGradient.StartColor);

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, ((BaseEvent)eventsContainer.UnsortedObjects[0]).CustomLightGradient.StartColor);

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.blue, ((BaseEvent)eventsContainer.UnsortedObjects[0]).CustomLightGradient.StartColor);
        }

        [Test]
        public void PaintUndo()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var colorPicker = Object.FindObjectOfType<ColorPicker>();
            var painter = Object.FindObjectOfType<PaintSelectedObjects>();

            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            var root = eventsContainer.transform.root;

            var selectionController = root.GetComponentInChildren<SelectionController>();
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            BaseEvent baseEventA = new V3BasicEvent(2, 1, 1);
            PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

            SelectionController.Select(baseEventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(2, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red,
                eventsContainer.UnsortedObjects[0].CustomData[baseEventA.CustomKeyColor].ReadColor());

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red,
                eventsContainer.UnsortedObjects[0].CustomData[baseEventA.CustomKeyColor].ReadColor());

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(true,
                eventsContainer.UnsortedObjects[0].CustomData == null || !eventsContainer.UnsortedObjects[0].CustomData
                    .HasKey(baseEventA.CustomKeyColor));
        }

        [Test]
        public void IgnoresOff()
        {
            var colorPicker = Object.FindObjectOfType<ColorPicker>();
            var painter = Object.FindObjectOfType<PaintSelectedObjects>();

            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            BaseEvent baseEventA = new V3BasicEvent(2, 1, 0);
            PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

            SelectionController.Select(baseEventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(true,
                eventsContainer.UnsortedObjects[0].CustomData == null || !eventsContainer.UnsortedObjects[0].CustomData
                    .HasKey(baseEventA.CustomKeyColor));
        }
    }
}
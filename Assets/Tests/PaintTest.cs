using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.Shared;
using Beatmap.V2;
using Beatmap.V3;
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
            return TestUtils.LoadMapper();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupEvents();
            TestUtils.ReturnSettings();
        }

        [Test]
        public void PaintGradientUndo()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            ColorPicker colorPicker = Object.FindObjectOfType<ColorPicker>();
            PaintSelectedObjects painter = Object.FindObjectOfType<PaintSelectedObjects>();

            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            Transform root = eventsContainer.transform.root;

            SelectionController selectionController = root.GetComponentInChildren<SelectionController>();
            EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

            JSONObject customData = new JSONObject();
            customData["_lightGradient"] = new ChromaLightGradient(Color.blue, Color.cyan, 1, "easeLinear").ToJson();
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
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            ColorPicker colorPicker = Object.FindObjectOfType<ColorPicker>();
            PaintSelectedObjects painter = Object.FindObjectOfType<PaintSelectedObjects>();

            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            Transform root = eventsContainer.transform.root;

            SelectionController selectionController = root.GetComponentInChildren<SelectionController>();
            EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

            BaseEvent baseEventA = new V3BasicEvent(2, 1, 1);
            PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

            SelectionController.Select(baseEventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(2, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, eventsContainer.UnsortedObjects[0].CustomData[baseEventA.CustomKeyColor].ReadColor());

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, eventsContainer.UnsortedObjects[0].CustomData[baseEventA.CustomKeyColor].ReadColor());

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(true, eventsContainer.UnsortedObjects[0].CustomData == null || !eventsContainer.UnsortedObjects[0].CustomData.HasKey(baseEventA.CustomKeyColor));
        }

        [Test]
        public void IgnoresOff()
        {
            ColorPicker colorPicker = Object.FindObjectOfType<ColorPicker>();
            PaintSelectedObjects painter = Object.FindObjectOfType<PaintSelectedObjects>();

            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            Transform root = eventsContainer.transform.root;
            EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

            BaseEvent baseEventA = new V3BasicEvent(2, 1, 0);
            PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

            SelectionController.Select(baseEventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(true, eventsContainer.UnsortedObjects[0].CustomData == null || !eventsContainer.UnsortedObjects[0].CustomData.HasKey(baseEventA.CustomKeyColor));
        }
    }
}

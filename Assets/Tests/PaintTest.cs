using NUnit.Framework;
using SimpleJSON;
using System.Collections;
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
            TestUtils.CleanupEvents();
        }

        [Test]
        public void PaintGradientUndo()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            ColorPicker colorPicker = Object.FindObjectOfType<ColorPicker>();
            PaintSelectedObjects painter = Object.FindObjectOfType<PaintSelectedObjects>();

            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            Transform root = eventsContainer.transform.root;

            SelectionController selectionController = root.GetComponentInChildren<SelectionController>();
            EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

            JSONObject customData = new JSONObject();
            customData["_lightGradient"] = new MapEvent.ChromaGradient(Color.blue, Color.cyan, 1, "easeLinear").ToJsonNode();
            MapEvent noteA = new MapEvent(2, 1, 1, customData);
            eventPlacement.queuedData = noteA;
            eventPlacement.queuedValue = eventPlacement.queuedData.Value;
            eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
            eventPlacement.ApplyToMap();

            SelectionController.Select(noteA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(2, ((MapEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, ((MapEvent)eventsContainer.UnsortedObjects[0]).LightGradient.StartColor);

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((MapEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, ((MapEvent)eventsContainer.UnsortedObjects[0]).LightGradient.StartColor);

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((MapEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.blue, ((MapEvent)eventsContainer.UnsortedObjects[0]).LightGradient.StartColor);
        }

        [Test]
        public void PaintUndo()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            ColorPicker colorPicker = Object.FindObjectOfType<ColorPicker>();
            PaintSelectedObjects painter = Object.FindObjectOfType<PaintSelectedObjects>();

            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            Transform root = eventsContainer.transform.root;

            SelectionController selectionController = root.GetComponentInChildren<SelectionController>();
            EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

            MapEvent noteA = new MapEvent(2, 1, 1);
            eventPlacement.queuedData = noteA;
            eventPlacement.queuedValue = eventPlacement.queuedData.Value;
            eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
            eventPlacement.ApplyToMap();

            SelectionController.Select(noteA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(2, ((MapEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, eventsContainer.UnsortedObjects[0].CustomData["_color"].ReadColor());

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((MapEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, eventsContainer.UnsortedObjects[0].CustomData["_color"].ReadColor());

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((MapEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(true, eventsContainer.UnsortedObjects[0].CustomData == null || !eventsContainer.UnsortedObjects[0].CustomData.HasKey("_color"));
        }

        [Test]
        public void IgnoresOff()
        {
            ColorPicker colorPicker = Object.FindObjectOfType<ColorPicker>();
            PaintSelectedObjects painter = Object.FindObjectOfType<PaintSelectedObjects>();

            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            Transform root = eventsContainer.transform.root;
            EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

            MapEvent noteA = new MapEvent(2, 1, 0);
            eventPlacement.queuedData = noteA;
            eventPlacement.queuedValue = eventPlacement.queuedData.Value;
            eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
            eventPlacement.ApplyToMap();

            SelectionController.Select(noteA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((MapEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(true, eventsContainer.UnsortedObjects[0].CustomData == null || !eventsContainer.UnsortedObjects[0].CustomData.HasKey("_color"));
        }
    }
}

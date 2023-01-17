using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.Shared;
using Beatmap.V2;
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

            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            Transform root = eventsContainer.transform.root;

            SelectionController selectionController = root.GetComponentInChildren<SelectionController>();
            EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

            JSONObject customData = new JSONObject();
            customData["_lightGradient"] = new ChromaLightGradient(Color.blue, Color.cyan, 1, "easeLinear").ToJson();
            BaseEvent noteA = new V2Event(2, 1, 1, 1, customData);
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

            BaseEvent noteA = new V2Event(2, 1, 1);
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
            Assert.AreEqual(2, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, eventsContainer.UnsortedObjects[0].CustomData["_color"].ReadColor());

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(Color.red, eventsContainer.UnsortedObjects[0].CustomData["_color"].ReadColor());

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(true, eventsContainer.UnsortedObjects[0].CustomData == null || !eventsContainer.UnsortedObjects[0].CustomData.HasKey("_color"));
        }

        [Test]
        public void IgnoresOff()
        {
            ColorPicker colorPicker = Object.FindObjectOfType<ColorPicker>();
            PaintSelectedObjects painter = Object.FindObjectOfType<PaintSelectedObjects>();

            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            Transform root = eventsContainer.transform.root;
            EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();

            BaseEvent noteA = new V2Event(2, 1, 0);
            eventPlacement.queuedData = noteA;
            eventPlacement.queuedValue = eventPlacement.queuedData.Value;
            eventPlacement.RoundedTime = eventPlacement.queuedData.Time;
            eventPlacement.ApplyToMap();

            SelectionController.Select(noteA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0].Time);
            Assert.AreEqual(1, ((BaseEvent)eventsContainer.UnsortedObjects[0]).Type);
            Assert.AreEqual(true, eventsContainer.UnsortedObjects[0].CustomData == null || !eventsContainer.UnsortedObjects[0].CustomData.HasKey("_color"));
        }
    }
}

using NUnit.Framework;
using System.Collections;
using System.Reflection;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class PaintTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupEvents();
        }
        
        [Test]
        public void PaintGradientUndo()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var colorPicker = Object.FindObjectOfType<ColorPicker>();
            var painter = Object.FindObjectOfType<PaintSelectedObjects>();
            
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            var root = eventsContainer.transform.root;
            
            var selectionController = root.GetComponentInChildren<SelectionController>();
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            var customData = new JSONObject();
            customData["_lightGradient"] = new MapEvent.ChromaGradient(Color.blue, Color.cyan, 1, "easeLinear").ToJSONNode();
            var noteA = new MapEvent(2, 1, 1, customData);
            eventPlacement.queuedData = noteA;
            eventPlacement.queuedValue = eventPlacement.queuedData._value;
            eventPlacement.RoundedTime = eventPlacement.queuedData._time;
            eventPlacement.ApplyToMap();
            
            SelectionController.Select(noteA);
            
            colorPicker.CurrentColor = Color.red;
            painter.Paint();
            
            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0]._time);
            Assert.AreEqual(2, ((MapEvent) eventsContainer.UnsortedObjects[0])._type);
            Assert.AreEqual(Color.red, ((MapEvent) eventsContainer.UnsortedObjects[0])._lightGradient.StartColor);

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0]._time);
            Assert.AreEqual(1, ((MapEvent) eventsContainer.UnsortedObjects[0])._type);
            Assert.AreEqual(Color.red, ((MapEvent) eventsContainer.UnsortedObjects[0])._lightGradient.StartColor);

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0]._time);
            Assert.AreEqual(1, ((MapEvent) eventsContainer.UnsortedObjects[0])._type);
            Assert.AreEqual(Color.blue, ((MapEvent) eventsContainer.UnsortedObjects[0])._lightGradient.StartColor);
        }

        [Test]
        public void PaintUndo()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var colorPicker = Object.FindObjectOfType<ColorPicker>();
            var painter = Object.FindObjectOfType<PaintSelectedObjects>();
            
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            var root = eventsContainer.transform.root;
            
            var selectionController = root.GetComponentInChildren<SelectionController>();
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            var noteA = new MapEvent(2, 1, 1);
            eventPlacement.queuedData = noteA;
            eventPlacement.queuedValue = eventPlacement.queuedData._value;
            eventPlacement.RoundedTime = eventPlacement.queuedData._time;
            eventPlacement.ApplyToMap();
            
            SelectionController.Select(noteA);
            
            colorPicker.CurrentColor = Color.red;
            painter.Paint();
            
            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0]._time);
            Assert.AreEqual(2, ((MapEvent) eventsContainer.UnsortedObjects[0])._type);
            Assert.AreEqual(Color.red, eventsContainer.UnsortedObjects[0]._customData["_color"].ReadColor());

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0]._time);
            Assert.AreEqual(1, ((MapEvent) eventsContainer.UnsortedObjects[0])._type);
            Assert.AreEqual(Color.red, eventsContainer.UnsortedObjects[0]._customData["_color"].ReadColor());

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0]._time);
            Assert.AreEqual(1, ((MapEvent) eventsContainer.UnsortedObjects[0])._type);
            Assert.AreEqual(true, eventsContainer.UnsortedObjects[0]._customData == null || !eventsContainer.UnsortedObjects[0]._customData.HasKey("_color"));
        }
        
        [Test]
        public void IgnoresOff()
        {
            var colorPicker = Object.FindObjectOfType<ColorPicker>();
            var painter = Object.FindObjectOfType<PaintSelectedObjects>();
            
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);
            var root = eventsContainer.transform.root;
            var eventPlacement = root.GetComponentInChildren<EventPlacement>();

            var noteA = new MapEvent(2, 1, 0);
            eventPlacement.queuedData = noteA;
            eventPlacement.queuedValue = eventPlacement.queuedData._value;
            eventPlacement.RoundedTime = eventPlacement.queuedData._time;
            eventPlacement.ApplyToMap();
            
            SelectionController.Select(noteA);
            
            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            Assert.AreEqual(1, eventsContainer.LoadedObjects.Count);
            Assert.AreEqual(2, eventsContainer.UnsortedObjects[0]._time);
            Assert.AreEqual(1, ((MapEvent) eventsContainer.UnsortedObjects[0])._type);
            Assert.AreEqual(true, eventsContainer.UnsortedObjects[0]._customData == null || !eventsContainer.UnsortedObjects[0]._customData.HasKey("_color"));
        }
    }
}

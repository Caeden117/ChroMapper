using NUnit.Framework;
using System.Collections;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BeatmapActionTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupNotes();
        }

        [Test]
        public void ModifiedAction()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            var root = notesContainer.transform.root;

            var noteA = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_A
            };
            notesContainer.SpawnObject(noteA);

            SelectionController.Select(noteA);

            var selectionController = root.GetComponentInChildren<SelectionController>();
            // Default precision is 3dp, but in editor it's 6dp so check 7dp
            selectionController.MoveSelection(-0.0000001f);

            actionContainer.Undo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0]._time);

            actionContainer.Redo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(1.9999999f, notesContainer.UnsortedObjects[0]._time);
        }

        [Test]
        public void CompositeTest()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            var root = notesContainer.transform.root;
            var selectionController = root.GetComponentInChildren<SelectionController>();
            var notePlacement = root.GetComponentInChildren<NotePlacement>();

            var noteA = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_A
            };
            var noteB = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_B,
                _lineIndex = 1,
                _lineLayer = 1
            };

            notePlacement.queuedData = noteA;
            notePlacement.RoundedTime = notePlacement.queuedData._time;
            notePlacement.ApplyToMap();
            
            SelectionController.Select(noteA);
            
            selectionController.ShiftSelection(1, 1);

            // Should conflict with existing note and delete it
            notePlacement.queuedData = noteB;
            notePlacement.RoundedTime = notePlacement.queuedData._time;
            notePlacement.ApplyToMap();
            
            SelectionController.Select(noteB);
            selectionController.ShiftSelection(1, 1);
            selectionController.Copy(true);
            
            selectionController.Paste();
            selectionController.Delete();
            
            void CheckState(int loadedObjects, int selectedObjects, int time, int type, int index, int layer)
            {
                Assert.AreEqual(loadedObjects, notesContainer.LoadedObjects.Count);
                Assert.AreEqual(selectedObjects, SelectionController.SelectedObjects.Count);
                Assert.AreEqual(time, notesContainer.UnsortedObjects[0]._time);
                Assert.AreEqual(type, ((BeatmapNote) notesContainer.UnsortedObjects[0])._type);
                Assert.AreEqual(index, ((BeatmapNote) notesContainer.UnsortedObjects[0])._lineIndex);
                Assert.AreEqual(layer, ((BeatmapNote) notesContainer.UnsortedObjects[0])._lineLayer);
            }

            // No notes loaded
            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, notesContainer.UnsortedObjects.Count);
            
            // Undo delete action
            actionContainer.Undo();
            CheckState(1, 1, 0, BeatmapNote.NOTE_TYPE_B, 2, 2);
            
            // Undo paste action
            actionContainer.Undo();
            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, notesContainer.UnsortedObjects.Count);
            
            // Undo cut action
            actionContainer.Undo();
            CheckState(1, 1, 2, BeatmapNote.NOTE_TYPE_B, 2, 2);

            // Undo movement
            actionContainer.Undo();
            CheckState(1, 1, 2, BeatmapNote.NOTE_TYPE_B, 1, 1);

            // Undo overwrite
            actionContainer.Undo();
            CheckState(1, 0, 2, BeatmapNote.NOTE_TYPE_A, 1, 1);

            // Undo movement
            actionContainer.Undo();
            CheckState(1, 1, 2, BeatmapNote.NOTE_TYPE_A, 0, 0);

            // Undo placement
            actionContainer.Undo();

            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, SelectionController.SelectedObjects.Count);
            
            // Redo it all! - Selection is lost :(
            actionContainer.Redo();
            CheckState(1, 0, 2, BeatmapNote.NOTE_TYPE_A, 0, 0);
            
            // Moving it selects it
            actionContainer.Redo();
            CheckState(1, 1, 2, BeatmapNote.NOTE_TYPE_A, 1, 1);
            
            // Everything is backwards
            actionContainer.Redo();
            CheckState(1, 0, 2, BeatmapNote.NOTE_TYPE_B, 1, 1);

            actionContainer.Redo();
            CheckState(1, 1, 2, BeatmapNote.NOTE_TYPE_B, 2, 2);

            actionContainer.Redo();
            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, notesContainer.UnsortedObjects.Count);
            
            // Redo paste
            actionContainer.Redo();
            CheckState(1, 1, 0, BeatmapNote.NOTE_TYPE_B, 2, 2);

            // Delete redo should still work even if our object isn't selected
            SelectionController.DeselectAll();
            
            // Redo delete
            actionContainer.Redo();
            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, notesContainer.UnsortedObjects.Count);
        }
        
        [Test]
        public void ModifiedWithConflictingAction()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();

            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();

            notePlacement.queuedData = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_A
            };
            notePlacement.RoundedTime = notePlacement.queuedData._time;
            notePlacement.ApplyToMap();

            notePlacement.queuedData = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_B
            };
            notePlacement.RoundedTime = notePlacement.queuedData._time;
            notePlacement.ApplyToMap();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0]._time);

            actionContainer.Undo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0]._time);

            actionContainer.Redo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0]._time);
        }
    }
}

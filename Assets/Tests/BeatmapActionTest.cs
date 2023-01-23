using NUnit.Framework;
using System.Collections;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V3;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BeatmapActionTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupNotes();
            TestUtils.ReturnSettings();
        }

        [Test]
        public void ModifiedAction()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            Transform root = notesContainer.transform.root;

            BaseNote baseNoteA = new V3ColorNote
            {
                Time = 2,
                Type = (int)NoteType.Red
            };
            notesContainer.SpawnObject(baseNoteA);

            SelectionController.Select(baseNoteA);

            SelectionController selectionController = root.GetComponentInChildren<SelectionController>();
            // Default precision is 3dp, but in editor it's 6dp so check 7dp
            selectionController.MoveSelection(-0.0000001f);

            actionContainer.Undo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0].Time);

            actionContainer.Redo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(1.9999999f, notesContainer.UnsortedObjects[0].Time);
        }

        [Test]
        public void CompositeTest()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            Transform root = notesContainer.transform.root;
            SelectionController selectionController = root.GetComponentInChildren<SelectionController>();
            NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();

            BaseNote baseNoteA = new V3ColorNote
            {
                Time = 2,
                Type = (int)NoteType.Red
            };
            BaseNote baseNoteB = new V3ColorNote
            {
                Time = 2,
                Type = (int)NoteType.Blue,
                PosX = 1,
                PosY = 1
            };

            PlaceUtils.PlaceNote(notePlacement, baseNoteA);

            SelectionController.Select(baseNoteA);

            selectionController.ShiftSelection(1, 1);

            // Should conflict with existing note and delete it
            PlaceUtils.PlaceNote(notePlacement, baseNoteB);

            SelectionController.Select(baseNoteB);
            selectionController.ShiftSelection(1, 1);
            selectionController.Copy(true);

            selectionController.Paste();
            selectionController.Delete();

            void CheckState(int loadedObjects, int selectedObjects, int time, int type, int index, int layer)
            {
                Assert.AreEqual(loadedObjects, notesContainer.LoadedObjects.Count);
                Assert.AreEqual(selectedObjects, SelectionController.SelectedObjects.Count);
                Assert.AreEqual(time, notesContainer.UnsortedObjects[0].Time);
                Assert.AreEqual(type, ((BaseNote)notesContainer.UnsortedObjects[0]).Type);
                Assert.AreEqual(index, ((BaseNote)notesContainer.UnsortedObjects[0]).PosX);
                Assert.AreEqual(layer, ((BaseNote)notesContainer.UnsortedObjects[0]).PosY);
            }

            // No notes loaded
            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, notesContainer.UnsortedObjects.Count);

            // Undo delete action
            actionContainer.Undo();
            CheckState(1, 1, 0, (int)NoteType.Blue, 2, 2);

            // Undo paste action
            actionContainer.Undo();
            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, notesContainer.UnsortedObjects.Count);

            // Undo cut action
            actionContainer.Undo();
            CheckState(1, 1, 2, (int)NoteType.Blue, 2, 2);

            // Undo movement
            actionContainer.Undo();
            CheckState(1, 1, 2, (int)NoteType.Blue, 1, 1);

            // Undo overwrite
            actionContainer.Undo();
            CheckState(1, 0, 2, (int)NoteType.Red, 1, 1);

            // Undo movement
            actionContainer.Undo();
            CheckState(1, 1, 2, (int)NoteType.Red, 0, 0);

            // Undo placement
            actionContainer.Undo();

            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, SelectionController.SelectedObjects.Count);

            // Redo it all! - Selection is lost :(
            actionContainer.Redo();
            CheckState(1, 0, 2, (int)NoteType.Red, 0, 0);

            // Moving it selects it
            actionContainer.Redo();
            CheckState(1, 1, 2, (int)NoteType.Red, 1, 1);

            // Everything is backwards
            actionContainer.Redo();
            CheckState(1, 0, 2, (int)NoteType.Blue, 1, 1);

            actionContainer.Redo();
            CheckState(1, 1, 2, (int)NoteType.Blue, 2, 2);

            actionContainer.Redo();
            Assert.AreEqual(0, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(0, notesContainer.UnsortedObjects.Count);

            // Redo paste
            actionContainer.Redo();
            CheckState(1, 1, 0, (int)NoteType.Blue, 2, 2);

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
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();

            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            Transform root = notesContainer.transform.root;
            NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();

            PlaceUtils.PlaceNote(notePlacement, new V3ColorNote
            {
                Time = 2,
                Type = (int)NoteType.Red
            });
            PlaceUtils.PlaceNote(notePlacement, new V3ColorNote
            {
                Time = 2,
                Type = (int)NoteType.Blue
            });

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0].Time);

            actionContainer.Undo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0].Time);

            actionContainer.Redo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0].Time);
        }
    }
}

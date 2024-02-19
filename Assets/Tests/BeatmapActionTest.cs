using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
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

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupNotes();
        }

        [Test]
        public void ModifiedAction()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var root = notesContainer.transform.root;

            BaseNote baseNoteA = new V3ColorNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            };
            notesContainer.SpawnObject(baseNoteA);

            SelectionController.Select(baseNoteA);

            var selectionController = root.GetComponentInChildren<SelectionController>();
            // Default precision is 3dp, but in editor it's 6dp so check 7dp
            selectionController.MoveSelection(-0.0000001f);

            actionContainer.Undo();

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(2, notesContainer.MapObjects[0].JsonTime);

            actionContainer.Redo();

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(1.9999999f, notesContainer.MapObjects[0].JsonTime);
        }

        [Test]
        public void CompositeTest()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var root = notesContainer.transform.root;
            var selectionController = root.GetComponentInChildren<SelectionController>();
            var notePlacement = root.GetComponentInChildren<NotePlacement>();

            BaseNote baseNoteA = new V3ColorNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            };
            BaseNote baseNoteB = new V3ColorNote
            {
                JsonTime = 2,
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

            void CheckState(int MapObjects, int selectedObjects, int time, int type, int index, int layer)
            {
                Assert.AreEqual(MapObjects, notesContainer.MapObjects.Count);
                Assert.AreEqual(selectedObjects, SelectionController.SelectedObjects.Count);
                Assert.AreEqual(time, notesContainer.MapObjects[0].JsonTime);
                Assert.AreEqual(type, ((BaseNote)notesContainer.MapObjects[0]).Type);
                Assert.AreEqual(index, ((BaseNote)notesContainer.MapObjects[0]).PosX);
                Assert.AreEqual(layer, ((BaseNote)notesContainer.MapObjects[0]).PosY);
            }

            // No notes loaded
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);

            // Undo delete action
            actionContainer.Undo();
            CheckState(1, 1, 0, (int)NoteType.Blue, 2, 2);

            // Undo paste action
            actionContainer.Undo();
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);

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

            Assert.AreEqual(0, notesContainer.MapObjects.Count);
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
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);

            // Redo paste
            actionContainer.Redo();
            CheckState(1, 1, 0, (int)NoteType.Blue, 2, 2);

            // Delete redo should still work even if our object isn't selected
            SelectionController.DeselectAll();

            // Redo delete
            actionContainer.Redo();
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
        }

        [Test]
        public void ModifiedWithConflictingAction()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();

            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();

            PlaceUtils.PlaceNote(notePlacement, new V3ColorNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            });
            PlaceUtils.PlaceNote(notePlacement, new V3ColorNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Blue
            });

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(2, notesContainer.MapObjects[0].JsonTime);

            actionContainer.Undo();

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(2, notesContainer.MapObjects[0].JsonTime);

            actionContainer.Redo();

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(2, notesContainer.MapObjects[0].JsonTime);
        }
    }
}
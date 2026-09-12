using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using Object = UnityEngine.Object;

namespace Tests.Placement
{
    public class ContainerCollectionTest : TestBase
    {
        // Missing values must return the complement of their insertion index so every range caller gets stable boundaries.
        [TestCase(new[] { 2 }, 60, 1)]
        [TestCase(new[] { 2, 60 }, 85, 2)]
        [TestCase(new[] { 2, 80, 95 }, 40, 1)]
        public void BinarySearchBy_MissingValueReturnsComplementOfInsertionIndex(
            int[] values,
            int searchedValue,
            int expectedInsertionIndex)
        {
            var result = values.ToList().BinarySearchBy(searchedValue, value => value);

            Assert.That(result, Is.LessThan(0));
            Assert.That(~result, Is.EqualTo(expectedInsertionIndex));
        }

        // A visual range beyond all map objects must not retain the collection-wide final object.
        [Test]
        public void RefreshPool_WindowAfterFinalObjectLoadsNoContainers()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var finalNote = PlaceUtils.Place(new BaseNote { JsonTime = 2 });

            noteGridContainer.RefreshPool(59.5f, 60.5f, true);

            Assert.That(noteGridContainer.LoadedContainers.ContainsKey(finalNote), Is.False);
        }

        // A visual range between distant objects must not include the object immediately preceding its lower bound.
        [Test]
        public void RefreshPool_WindowBetweenObjectsLoadsNoOutOfRangeContainers()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var precedingNote = PlaceUtils.Place(new BaseNote { JsonTime = 2 });
            var followingNote = PlaceUtils.Place(new BaseNote { JsonTime = 80 });
            var finalGuard = PlaceUtils.Place(new BaseNote { JsonTime = 95 });

            noteGridContainer.RefreshPool(35f, 45f, true);

            Assert.That(noteGridContainer.LoadedContainers.ContainsKey(precedingNote), Is.False);
            Assert.That(noteGridContainer.LoadedContainers.ContainsKey(followingNote), Is.False);
            Assert.That(noteGridContainer.LoadedContainers.ContainsKey(finalGuard), Is.False);
        }

        [Test]
        public void GetBetween()
        {
            var epsilon = BeatmapObjectContainerCollection.Epsilon;
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteBlue0 = new BaseNote { Color = (int)NoteType.Blue };

            var noteBlue1 = new BaseNote { Color = (int)NoteType.Blue, JsonTime = 1 };
            var noteRed1 = new BaseNote { Color = (int)NoteType.Red, JsonTime = 1, PosX = 1 };

            var noteRed2 = new BaseNote { Color = (int)NoteType.Red, JsonTime = 2, PosX = 1 };

            foreach (var note in new List<BaseNote> { noteBlue0, noteBlue1, noteRed1, noteRed2 })
                noteGridContainer.SpawnObject(note);

            // Single point
            var result = noteGridContainer.GetBetween(0 - epsilon, 0 + epsilon);
            Assert.AreEqual(1, result.Length);
            Assert.AreSame(noteBlue0, result[0]);

            result = noteGridContainer.GetBetween(1 - epsilon, 1 + epsilon);
            Assert.AreEqual(2, result.Length);
            Assert.AreSame(noteBlue1, result[0]);
            Assert.AreSame(noteRed1, result[1]);

            result = noteGridContainer.GetBetween(2 - epsilon, 2 + epsilon);
            Assert.AreEqual(1, result.Length);
            Assert.AreSame(noteRed2, result[0]);

            // Sections
            result = noteGridContainer.GetBetween(0 - epsilon, 2 + epsilon);
            Assert.AreEqual(4, result.Length);
            Assert.AreSame(noteBlue0, result[0]);
            Assert.AreSame(noteBlue1, result[1]);
            Assert.AreSame(noteRed1, result[2]);
            Assert.AreSame(noteRed2, result[3]);

            result = noteGridContainer.GetBetween(0 - epsilon, 1 + epsilon);
            Assert.AreEqual(3, result.Length);
            Assert.AreSame(noteBlue0, result[0]);
            Assert.AreSame(noteBlue1, result[1]);
            Assert.AreSame(noteRed1, result[2]);

            result = noteGridContainer.GetBetween(1 - epsilon, 2 + epsilon);
            Assert.AreSame(noteBlue1, result[0]);
            Assert.AreSame(noteRed1, result[1]);
            Assert.AreSame(noteRed2, result[2]);

            // Empty section
            result = noteGridContainer.GetBetween(0.1f, 0.9f);
            Assert.AreEqual(0, result.Length);
        }

        [TestCase(new[] { 0, 1, 2, 3, 4 })]
        [TestCase(new[] { 4, 3, 2, 1, 0 })]
        [TestCase(new[] { 0, 1, 4, 3, 2 })]
        [TestCase(new[] { 2, 0, 1, 3, 4 })]
        public void SpawnObject_MapObjectsAreSorted(int[] insertOrder)
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var note0 = new BaseNote { JsonTime = 0 };
            var note1 = new BaseNote { JsonTime = 1 };
            var note2 = new BaseNote { JsonTime = 2 };
            var note3 = new BaseNote { JsonTime = 3 };
            var note4 = new BaseNote { JsonTime = 4 };
            var notes = new List<BaseNote>
            {
                note0,
                note1,
                note2,
                note3,
                note4
            };

            foreach (var index in insertOrder) noteGridContainer.SpawnObject(notes[index]);

            BeatmapAssertion.CollectionCount<BaseNote>(5);
            Assert.AreSame(note0, noteGridContainer.MapObjects[0]);
            Assert.AreSame(note1, noteGridContainer.MapObjects[1]);
            Assert.AreSame(note2, noteGridContainer.MapObjects[2]);
            Assert.AreSame(note3, noteGridContainer.MapObjects[3]);
            Assert.AreSame(note4, noteGridContainer.MapObjects[4]);
        }

        [Test]
        public void SpawnObject_PreventsStackedNotes()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new BaseNote();
            var noteB = BeatmapFactory.Clone(noteA);
            var noteC = BeatmapFactory.Clone(noteB);

            foreach (var note in new List<BaseNote> { noteA, noteB, noteC }) noteGridContainer.SpawnObject(note);

            BeatmapAssertion.CollectionCount<BaseNote>(1);
            Assert.AreSame(noteC, noteGridContainer.MapObjects[0]);
        }

        [Test]
        public void DeleteObject_StackedNotes([Values(0, 1, 2, 3)] int deleteIndex)
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new BaseNote();
            var noteB = BeatmapFactory.Clone(noteA);
            var noteC = BeatmapFactory.Clone(noteB);
            var noteD = BeatmapFactory.Clone(noteC);

            var notes = new List<BaseNote> { noteA, noteB, noteC, noteD };

            foreach (var note in notes) noteGridContainer.SpawnObject(note, false);

            BeatmapAssertion.CollectionCount<BaseNote>(4);

            PlaceUtils.Delete(notes[deleteIndex % 4]);
            BeatmapAssertion.CollectionCount<BaseNote>(3);

            PlaceUtils.Delete(notes[(deleteIndex + 1) % 4]);
            BeatmapAssertion.CollectionCount<BaseNote>(2);

            PlaceUtils.Delete(notes[(deleteIndex + 2) % 4]);
            BeatmapAssertion.CollectionCount<BaseNote>(1);

            PlaceUtils.Delete(notes[(deleteIndex + 3) % 4]);
            BeatmapAssertion.CollectionCount<BaseNote>(0);
        }

        [Test]
        public void Mirror_MapObjectsAreSorted([Values] bool mirrorA, [Values] bool mirrorB, [Values] bool mirrorC)
        {
            var mirrorSelection = Object.FindAnyObjectByType<MirrorSelection>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new BaseNote { JsonTime = 0, PosX = 0, PosY = 0 };
            var noteB = new BaseNote { JsonTime = 0, PosX = 1, PosY = 1 };
            var noteC = new BaseNote { JsonTime = 0, PosX = 2, PosY = 2 };

            if (mirrorA) noteA = PlaceUtils.Place(noteA);
            if (mirrorB) noteB = PlaceUtils.Place(noteB);
            if (mirrorC) noteC = PlaceUtils.Place(noteC);

            SelectionController.DeselectAll();
            if (mirrorA) SelectionController.Select(noteA);
            if (mirrorB) SelectionController.Select(noteB, true);
            if (mirrorC) SelectionController.Select(noteC, true);
            mirrorSelection.Mirror();

            BeatmapAssertion.IsEqual(BeatmapAssertion.NotesAreSorted, notesContainer.MapObjects, "Notes are sorted");

            PlaceUtils.Undo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.NotesAreSorted, notesContainer.MapObjects, "Notes are sorted");
        }

        [Test]
        public void MirrorInTime_MapObjectsAreSorted(
            [Values] bool mirrorA,
            [Values] bool mirrorB,
            [Values] bool mirrorC)
        {
            var mirrorSelection = Object.FindAnyObjectByType<MirrorSelection>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new BaseNote { JsonTime = 0, PosX = (int)GridX.Left };
            var noteB = new BaseNote { JsonTime = 1, PosX = (int)GridX.MiddleLeft };
            var noteC = new BaseNote { JsonTime = 2, PosX = (int)GridX.MiddleRight };

            if (mirrorA) noteA = PlaceUtils.Place(noteA);
            if (mirrorB) noteB = PlaceUtils.Place(noteB);
            if (mirrorC) noteC = PlaceUtils.Place(noteC);

            SelectionController.DeselectAll();
            if (mirrorA) SelectionController.Select(noteA);
            if (mirrorB) SelectionController.Select(noteB, true);
            if (mirrorC) SelectionController.Select(noteC, true);
            mirrorSelection.MirrorTime();

            BeatmapAssertion.IsEqual(BeatmapAssertion.NotesAreSorted, notesContainer.MapObjects, "Notes are sorted");

            PlaceUtils.Undo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.NotesAreSorted, notesContainer.MapObjects, "Notes are sorted");
        }

        [Test]
        public void ShiftSelection_MapObjectsAreSorted(
            [Values] bool selectA,
            [Values] bool selectB,
            [Values] bool selectC)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new BaseNote { JsonTime = 0, PosY = (int)GridY.Base };
            var noteB = new BaseNote { JsonTime = 0, PosY = (int)GridY.Upper };
            var noteC = new BaseNote { JsonTime = 0, PosY = (int)GridY.Top };

            noteA = PlaceUtils.Place(noteA);
            noteB = PlaceUtils.Place(noteB);
            noteC = PlaceUtils.Place(noteC);

            SelectionController.DeselectAll();
            if (selectA) SelectionController.Select(noteA);
            if (selectB) SelectionController.Select(noteB, true);
            if (selectC) SelectionController.Select(noteC, true);
            selectionController.ShiftSelection(1, 0);

            BeatmapAssertion.IsEqual(BeatmapAssertion.NotesAreSorted, notesContainer.MapObjects, "Notes are sorted");

            PlaceUtils.Undo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.NotesAreSorted, notesContainer.MapObjects, "Notes are sorted");
        }

        [Test]
        public void MoveSelection_MapObjectsAreSorted(
            [Values] bool selectA,
            [Values] bool selectB,
            [Values] bool selectC)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new BaseNote { JsonTime = 0, PosY = (int)GridY.Base };
            var noteB = new BaseNote { JsonTime = 0, PosY = (int)GridY.Upper };
            var noteC = new BaseNote { JsonTime = 0, PosY = (int)GridY.Top };

            noteA = PlaceUtils.Place(noteA);
            noteB = PlaceUtils.Place(noteB);
            noteC = PlaceUtils.Place(noteC);

            SelectionController.DeselectAll();
            if (selectA) SelectionController.Select(noteA);
            if (selectB) SelectionController.Select(noteB, true);
            if (selectC) SelectionController.Select(noteC, true);
            selectionController.MoveSelection(1, true);

            BeatmapAssertion.IsEqual(BeatmapAssertion.NotesAreSorted, notesContainer.MapObjects, "Notes are sorted");

            PlaceUtils.Undo();
            BeatmapAssertion.IsEqual(BeatmapAssertion.NotesAreSorted, notesContainer.MapObjects, "Notes are sorted");
        }

        [Test]
        public void MoveSelection_NoteIntegrity()
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();

            var noteA = new BaseNote { JsonTime = 0 };
            var noteB = new BaseNote { JsonTime = 1 };
            noteA = PlaceUtils.Place(noteA);
            noteB = PlaceUtils.Place(noteB);

            SelectionController.Select(noteA);
            SelectionController.Select(noteB, true);

            selectionController.MoveSelection(1);
            AssertNoteStateAfterMove(1, 2);

            selectionController.MoveSelection(-1);
            AssertNoteStateAfterMove(0, 1);

            PlaceUtils.Undo();
            AssertNoteStateAfterMove(1, 2);

            PlaceUtils.Undo();
            AssertNoteStateAfterMove(0, 1);
        }

        private static void AssertNoteStateAfterMove(
            int firstObjectTime,
            int secondObjectTime)
        {
            BeatmapAssertion.CollectionCount<BaseNote>(2, "Notes should not be deleted");
            Assert.AreEqual(2, SelectionController.SelectedObjects.Count, "Notes should be selected");
            var selectedNotes = SelectionController
                .SelectedObjects.OfType<BaseNote>()
                .OrderBy(note => note.JsonTime)
                .ToList();
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = firstObjectTime,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up,
                    AngleOffset = 0
                },
                selectedNotes[0],
                "First note after move");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = secondObjectTime,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up,
                    AngleOffset = 0
                },
                selectedNotes[1],
                "Second note after move");
        }
    }
}

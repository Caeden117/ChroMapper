using System;
using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Util;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests
{
    public class ContainerCollectionTest
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
            CleanupUtils.CleanupNotes();
        }

        [Test]
        public void GetBetween()
        {
            var epsilon = BeatmapObjectContainerCollection.Epsilon;
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteBlue0 = new V3ColorNote { Color = (int)NoteType.Blue };

            var noteBlue1 = new V3ColorNote { Color = (int)NoteType.Blue, JsonTime = 1 };
            var noteRed1 = new V3ColorNote { Color = (int)NoteType.Red, JsonTime = 1, PosX = 1 };

            var noteRed2 = new V3ColorNote { Color = (int)NoteType.Red, JsonTime = 2, PosX = 1 };

            foreach (var note in new List<BaseNote> { noteBlue0, noteBlue1, noteRed1, noteRed2 })
            {
                noteGridContainer.SpawnObject(note);
            }

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

            var note0 = new V3ColorNote { JsonTime = 0 };
            var note1 = new V3ColorNote { JsonTime = 1 };
            var note2 = new V3ColorNote { JsonTime = 2 };
            var note3 = new V3ColorNote { JsonTime = 3 };
            var note4 = new V3ColorNote { JsonTime = 4 };
            var notes = new List<BaseNote> { note0, note1, note2, note3, note4 };

            foreach (var index in insertOrder)
            {
                noteGridContainer.SpawnObject(notes[index]);
            }

            Assert.AreEqual(5, noteGridContainer.MapObjects.Count);
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

            var noteA = new V3ColorNote();
            var noteB = BeatmapFactory.Clone(noteA);
            var noteC = BeatmapFactory.Clone(noteB);

            foreach (var note in new List<BaseNote> { noteA, noteB, noteC})
            {
                noteGridContainer.SpawnObject(note);
            }

            Assert.AreEqual(1, noteGridContainer.MapObjects.Count);
            Assert.AreSame(noteC, noteGridContainer.MapObjects[0]);
        }
        
        [Test]
        public void DeleteObject_StackedNotes([Values(0, 1, 2, 3)] int deleteIndex)
        {            
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new V3ColorNote();
            var noteB = BeatmapFactory.Clone(noteA);
            var noteC = BeatmapFactory.Clone(noteB);
            var noteD = BeatmapFactory.Clone(noteC);

            var notes = new List<BaseNote> { noteA, noteB, noteC, noteD };
            
            foreach (var note in notes)
            {
                noteGridContainer.SpawnObject(note, false);
            }

            Assert.AreEqual(4, noteGridContainer.MapObjects.Count);
            Assert.AreEqual(4, noteGridContainer.LoadedContainers.Count);

            noteGridContainer.DeleteObject(notes[deleteIndex % 4]);
            Assert.AreEqual(3, noteGridContainer.MapObjects.Count);
            Assert.AreEqual(3, noteGridContainer.LoadedContainers.Count);
            
            noteGridContainer.DeleteObject(notes[(deleteIndex + 1) % 4]);
            Assert.AreEqual(2, noteGridContainer.MapObjects.Count);
            Assert.AreEqual(2, noteGridContainer.LoadedContainers.Count);
            
            noteGridContainer.DeleteObject(notes[(deleteIndex + 2) % 4]);
            Assert.AreEqual(1, noteGridContainer.MapObjects.Count);
            Assert.AreEqual(1, noteGridContainer.LoadedContainers.Count);
            
            noteGridContainer.DeleteObject(notes[(deleteIndex + 3) % 4]);
            Assert.AreEqual(0, noteGridContainer.MapObjects.Count);
            Assert.AreEqual(0, noteGridContainer.LoadedContainers.Count);
        }
        
        [Test]
        public void Mirror_MapObjectsAreSorted([Values]bool mirrorA, [Values]bool mirrorB, [Values]bool mirrorC)
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var mirrorSelection = Object.FindObjectOfType<MirrorSelection>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new V3ColorNote { JsonTime = 0, PosX = 0, PosY = 0 };
            var noteB = new V3ColorNote { JsonTime = 0, PosX = 1, PosY = 1 };
            var noteC = new V3ColorNote { JsonTime = 0, PosX = 2, PosY = 2 };
            
            if (mirrorA) notesContainer.SpawnObject(noteA);
            if (mirrorB) notesContainer.SpawnObject(noteB);
            if (mirrorC) notesContainer.SpawnObject(noteC);
            
            SelectionController.DeselectAll();
            if (mirrorA) SelectionController.Select(noteA);
            if (mirrorB) SelectionController.Select(noteB, true);
            if (mirrorC) SelectionController.Select(noteC, true);
            mirrorSelection.Mirror();

            CheckUtils.CheckNotesAreSorted(notesContainer.MapObjects);
            
            actionContainer.Undo();
            CheckUtils.CheckNotesAreSorted(notesContainer.MapObjects);
        }
        
        [Test]
        public void MirrorInTime_MapObjectsAreSorted([Values]bool mirrorA, [Values]bool mirrorB, [Values]bool mirrorC)
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var mirrorSelection = Object.FindObjectOfType<MirrorSelection>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new V3ColorNote { JsonTime = 0, PosX = (int)GridX.Left };
            var noteB = new V3ColorNote { JsonTime = 1, PosX = (int)GridX.MiddleLeft };
            var noteC = new V3ColorNote { JsonTime = 2, PosX = (int)GridX.MiddleRight };
            
            if (mirrorA) notesContainer.SpawnObject(noteA);
            if (mirrorB) notesContainer.SpawnObject(noteB);
            if (mirrorC) notesContainer.SpawnObject(noteC);
            
            SelectionController.DeselectAll();
            if (mirrorA) SelectionController.Select(noteA);
            if (mirrorB) SelectionController.Select(noteB, true);
            if (mirrorC) SelectionController.Select(noteC, true);
            mirrorSelection.MirrorTime();

            CheckUtils.CheckNotesAreSorted(notesContainer.MapObjects);
            
            actionContainer.Undo();
            CheckUtils.CheckNotesAreSorted(notesContainer.MapObjects);
        }

        [Test]
        public void ShiftSelection_MapObjectsAreSorted([Values]bool selectA, [Values]bool selectB, [Values]bool selectC)
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new V3ColorNote { JsonTime = 0, PosY = (int)GridY.Base };
            var noteB = new V3ColorNote { JsonTime = 0, PosY = (int)GridY.Upper };
            var noteC = new V3ColorNote { JsonTime = 0, PosY = (int)GridY.Top };
            
            notesContainer.SpawnObject(noteA);
            notesContainer.SpawnObject(noteB);
            notesContainer.SpawnObject(noteC);
            
            SelectionController.DeselectAll();
            if (selectA) SelectionController.Select(noteA);
            if (selectB) SelectionController.Select(noteB, true);
            if (selectC) SelectionController.Select(noteC, true);
            selectionController.ShiftSelection(1, 0);

            CheckUtils.CheckNotesAreSorted(notesContainer.MapObjects);
            
            actionContainer.Undo();
            CheckUtils.CheckNotesAreSorted(notesContainer.MapObjects);
        }
        
        [Test]
        public void MoveSelection_MapObjectsAreSorted([Values]bool selectA, [Values]bool selectB, [Values]bool selectC)
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var selectionController = Object.FindObjectOfType<SelectionController>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new V3ColorNote { JsonTime = 0, PosY = (int)GridY.Base };
            var noteB = new V3ColorNote { JsonTime = 0, PosY = (int)GridY.Upper };
            var noteC = new V3ColorNote { JsonTime = 0, PosY = (int)GridY.Top };
            
            notesContainer.SpawnObject(noteA);
            notesContainer.SpawnObject(noteB);
            notesContainer.SpawnObject(noteC);
            
            SelectionController.DeselectAll();
            if (selectA) SelectionController.Select(noteA);
            if (selectB) SelectionController.Select(noteB, true);
            if (selectC) SelectionController.Select(noteC, true);
            selectionController.MoveSelection(1, true);

            CheckUtils.CheckNotesAreSorted(notesContainer.MapObjects);
            
            actionContainer.Undo();
            CheckUtils.CheckNotesAreSorted(notesContainer.MapObjects);
        }
    }
}

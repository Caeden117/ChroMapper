using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine.TestTools;

namespace Tests
{
    public class NotesContainerTest
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
        public void RefreshSpecialAngles()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            BaseNote baseNoteA = new BaseNote
            {
                JsonTime = 4,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.Left
            };
            noteGridContainer.SpawnObject(baseNoteA);
            var containerA = noteGridContainer.LoadedContainers[baseNoteA] as NoteContainer;

            BaseNote baseNoteB = new BaseNote
            {
                JsonTime = 4,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.MiddleLeft
            };
            noteGridContainer.SpawnObject(baseNoteB);
            var containerB = noteGridContainer.LoadedContainers[baseNoteB] as NoteContainer;

            // These tests are based of the examples in this image
            // https://media.discordapp.net/attachments/443569023951568906/681978249139585031/unknown.png

            // ◌◌◌◌
            // ◌→◌◌
            // ◌◌→◌
            UpdateNote(containerA, (int)GridX.MiddleLeft, (int)GridY.Upper, (int)NoteCutDirection.Right);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Right);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(90, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(90, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↙◌
            // ◌◌◌◌
            // ◌◌↙◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(333.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌↓↓◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↙◌◌◌
            // ↙◌◌◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ↙◌◌↙
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Right, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↘◌◌◌
            // ◌◌↘◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(63.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(63.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // Changing this note to be in another beat should stop the angles snapping
            baseNoteA.JsonTime = 13;
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            noteGridContainer.RefreshSpecialAngles(baseNoteB, true, false);
            Assert.AreEqual(45, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // Make cleanup work
            baseNoteA.JsonTime = 14;
        }

        [Test]
        public void RefreshSpecialAnglesOnDirectionChange()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = noteGridContainer.transform.root.GetComponentInChildren<BeatmapNoteInputController>();

            BaseNote baseNoteA = new BaseNote { JsonTime = 4 };
            noteGridContainer.SpawnObject(baseNoteA);
            var containerA = noteGridContainer.LoadedContainers[baseNoteA] as NoteContainer;

            BaseNote baseNoteB = new BaseNote { JsonTime = 4 };
            noteGridContainer.SpawnObject(baseNoteB, removeConflicting: false);
            var containerB = noteGridContainer.LoadedContainers[baseNoteB] as NoteContainer;
            
            // ◌◌↓◌
            // ◌◌◌◌
            // ◌←◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Left);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(270, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↙◌◌
            inputController.UpdateNoteDirection(containerB, true);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            inputController.UpdateNoteDirection(containerB, true);
            Assert.AreEqual(333.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);
            
            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↘◌◌
            inputController.UpdateNoteDirection(containerB, true);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.DirectionTarget.localEulerAngles.z, 0.01);
        }
        
        [Test]
        public void RefreshSpecialAnglesOnDirectionChange2()
        {
            // Test that angles are not changed when they shouldn't be
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = noteGridContainer.transform.root.GetComponentInChildren<BeatmapNoteInputController>();

            BaseNote baseNoteA = new BaseNote { JsonTime = 4 };
            noteGridContainer.SpawnObject(baseNoteA);
            var containerA = noteGridContainer.LoadedContainers[baseNoteA] as NoteContainer;

            BaseNote baseNoteB = new BaseNote { JsonTime = 4 };
            noteGridContainer.SpawnObject(baseNoteB, removeConflicting: false);
            var containerB = noteGridContainer.LoadedContainers[baseNoteB] as NoteContainer;
            
            // ◌◌↓◌
            // ◌◌◌◌
            // ←◌◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(270, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↙◌◌◌
            inputController.UpdateNoteDirection(containerB, true);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↓◌◌◌
            inputController.UpdateNoteDirection(containerB, true);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);
            
            // ◌◌↓◌
            // ◌◌◌◌
            // ↘◌◌◌
            inputController.UpdateNoteDirection(containerB, true);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.DirectionTarget.localEulerAngles.z, 0.01);
        }

        [Test]
        public void RefreshSpecialAnglesIgnoresPrecisionPlacement()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            BaseNote baseNoteA = new BaseNote { JsonTime = 4 };
            noteGridContainer.SpawnObject(baseNoteA);
            var containerA = noteGridContainer.LoadedContainers[baseNoteA] as NoteContainer;

            BaseNote baseNoteB = new BaseNote { JsonTime = 4 };
            noteGridContainer.SpawnObject(baseNoteB, removeConflicting: false);
            var containerB = noteGridContainer.LoadedContainers[baseNoteB] as NoteContainer;
            
            // ME precision placed
            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, 1000);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, 1000);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);
            
            // NE precision placed
            (containerA.ObjectData as BaseNote).CustomCoordinate = new JSONArray { [0] = 0, [1] = 2 };
            (containerB.ObjectData as BaseNote).CustomCoordinate = new JSONArray { [0] = -1, [1] = 0 };
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);
            
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);
            
        }

        private void UpdateNote(NoteContainer container, int PosX, int PosY, int cutDirection)
        {
            var baseNote = (BaseNote)container.ObjectData;
            baseNote.PosX = PosX;
            baseNote.PosY = PosY;
            baseNote.CutDirection = cutDirection;
            container.UpdateGridPosition();
            container.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(baseNote);
        }

        [Test]
        public void ShiftInTime()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var root = notesContainer.transform.root;

            BaseNote baseNoteA = new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            };
            notesContainer.SpawnObject(baseNoteA);

            BaseNote baseNoteB = new BaseNote
            {
                JsonTime = 3,
                Type = (int)NoteType.Red
            };
            notesContainer.SpawnObject(baseNoteB);

            SelectionController.Select(baseNoteB, false, false, false);

            var selectionController = root.GetComponentInChildren<SelectionController>();
            selectionController.MoveSelection(-2);

            notesContainer.DeleteObject(baseNoteB);

            Assert.AreEqual(1, notesContainer.LoadedContainers.Count);
            Assert.AreEqual(1, notesContainer.MapObjects.Count);
        }
    }
}

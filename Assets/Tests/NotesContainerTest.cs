using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
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
                BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note) as NoteGridContainer;

            BaseNote baseNoteA = new V3ColorNote
            {
                JsonTime = 4,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.Left
            };
            noteGridContainer.SpawnObject(baseNoteA);
            var containerA = noteGridContainer.LoadedContainers[baseNoteA] as NoteContainer;

            BaseNote baseNoteB = new V3ColorNote
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
            Assert.AreEqual(90, containerA.directionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(90, containerB.directionTarget.localEulerAngles.z, 0.01);

            // ◌◌↙◌
            // ◌◌◌◌
            // ◌◌↙◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.directionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.directionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(333.43, containerA.directionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.directionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌↓↓◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(0, containerA.directionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.directionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↙◌◌◌
            // ↙◌◌◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.directionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.directionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ↙◌◌↙
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Right, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.directionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.directionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↘◌◌◌
            // ◌◌↘◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(63.43, containerA.directionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(63.43, containerB.directionTarget.localEulerAngles.z, 0.01);

            // Changing this note to be in another beat should stop the angles snapping
            baseNoteA.JsonTime = 13;
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            noteGridContainer.RefreshSpecialAngles(baseNoteB, true, false);
            Assert.AreEqual(45, containerA.directionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.directionTarget.localEulerAngles.z, 0.01);

            // Make cleanup work
            baseNoteA.JsonTime = 14;
        }

        private void UpdateNote(NoteContainer container, int PosX, int PosY, int cutDirection)
        {
            var baseNote = (BaseNote)container.ObjectData;
            baseNote.PosX = PosX;
            baseNote.PosY = PosY;
            baseNote.CutDirection = cutDirection;
            container.UpdateGridPosition();
            container.directionTarget.localEulerAngles = NoteContainer.Directionalize(baseNote);
        }

        [Test]
        public void ShiftInTime()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            var root = notesContainer.transform.root;

            BaseNote baseNoteA = new V3ColorNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            };
            notesContainer.SpawnObject(baseNoteA);

            BaseNote baseNoteB = new V3ColorNote
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
            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
        }
    }
}

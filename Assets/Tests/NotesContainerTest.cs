using NUnit.Framework;
using System.Collections;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V2;
using Tests.Util;
using UnityEngine.TestTools;

namespace Tests
{
    public class NotesContainerTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMapper();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            TestUtils.CleanupNotes();
        }

        [Test]
        public void RefreshSpecialAngles()
        {
            NoteGridContainer noteGridContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note) as NoteGridContainer;

            INote noteA = new V2Note
            {
                Time = 14,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.Left
            };
            noteGridContainer.SpawnObject(noteA);
            NoteContainer containerA = noteGridContainer.LoadedContainers[noteA] as NoteContainer;

            INote noteB = new V2Note
            {
                Time = 14,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.MiddleLeft
            };
            noteGridContainer.SpawnObject(noteB);
            NoteContainer containerB = noteGridContainer.LoadedContainers[noteB] as NoteContainer;

            // These tests are based of the examples in this image
            // https://media.discordapp.net/attachments/443569023951568906/681978249139585031/unknown.png

            // ◌◌◌◌
            // ◌→◌◌
            // ◌◌→◌
            UpdateNote(containerA, (int)GridX.MiddleLeft, (int)GridY.Upper, (int)NoteCutDirection.Right);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Right);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(90, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(90, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌↙◌
            // ◌◌◌◌
            // ◌◌↙◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(333.43, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌↓↓◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(0, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↙◌◌◌
            // ↙◌◌◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ↙◌◌↙
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Right, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↘◌◌◌
            // ◌◌↘◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(63.43, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(63.43, containerB.transform.localEulerAngles.z, 0.01);

            // Changing this note to be in another beat should stop the angles snapping
            noteA.Time = 13;
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            noteGridContainer.RefreshSpecialAngles(noteB, true, false);
            Assert.AreEqual(45, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.transform.localEulerAngles.z, 0.01);

            // Make cleanup work
            noteA.Time = 14;
        }

        private void UpdateNote(NoteContainer container, int PosX, int PosY, int cutDirection)
        {
            INote note = (INote)container.ObjectData;
            note.PosX = PosX;
            note.PosY = PosY;
            note.CutDirection = cutDirection;
            container.UpdateGridPosition();
            container.transform.localEulerAngles = NoteContainer.Directionalize(note);
        }

        [Test]
        public void ShiftInTime()
        {
            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            UnityEngine.Transform root = notesContainer.transform.root;

            INote noteA = new V2Note
            {
                Time = 2,
                Type = (int)NoteType.Red
            };
            notesContainer.SpawnObject(noteA);

            INote noteB = new V2Note
            {
                Time = 3,
                Type = (int)NoteType.Red
            };
            notesContainer.SpawnObject(noteB);

            SelectionController.Select(noteB, false, false, false);

            SelectionController selectionController = root.GetComponentInChildren<SelectionController>();
            selectionController.MoveSelection(-2);

            notesContainer.DeleteObject(noteB);

            Assert.AreEqual(1, notesContainer.LoadedContainers.Count);
            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
        }
    }
}

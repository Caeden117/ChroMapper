using NUnit.Framework;
using System.Collections;
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
            NotesContainer notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note) as NotesContainer;

            BeatmapNote noteA = new BeatmapNote
            {
                Time = 14,
                Type = BeatmapNote.NoteTypeA,
                LineIndex = BeatmapNote.LineIndexFarLeft
            };
            notesContainer.SpawnObject(noteA);
            BeatmapNoteContainer containerA = notesContainer.LoadedContainers[noteA] as BeatmapNoteContainer;

            BeatmapNote noteB = new BeatmapNote
            {
                Time = 14,
                Type = BeatmapNote.NoteTypeA,
                LineIndex = BeatmapNote.LineIndexMidLeft
            };
            notesContainer.SpawnObject(noteB);
            BeatmapNoteContainer containerB = notesContainer.LoadedContainers[noteB] as BeatmapNoteContainer;

            // These tests are based of the examples in this image
            // https://media.discordapp.net/attachments/443569023951568906/681978249139585031/unknown.png

            // ◌◌◌◌
            // ◌→◌◌
            // ◌◌→◌
            UpdateNote(containerA, BeatmapNote.LineIndexMidLeft, BeatmapNote.LineLayerMid, BeatmapNote.NoteCutDirectionRight);
            UpdateNote(containerB, BeatmapNote.LineIndexMidRight, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionRight);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(90, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(90, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌↙◌
            // ◌◌◌◌
            // ◌◌↙◌
            UpdateNote(containerA, BeatmapNote.LineIndexMidRight, BeatmapNote.LineLayerTop, BeatmapNote.NoteCutDirectionDownLeft);
            UpdateNote(containerB, BeatmapNote.LineIndexMidRight, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionDownLeft);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, BeatmapNote.LineIndexMidRight, BeatmapNote.LineLayerTop, BeatmapNote.NoteCutDirectionDown);
            UpdateNote(containerB, BeatmapNote.LineIndexMidLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionDown);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(333.43, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌↓↓◌
            UpdateNote(containerA, BeatmapNote.LineIndexMidRight, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionDown);
            UpdateNote(containerB, BeatmapNote.LineIndexMidLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionDown);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(0, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↙◌◌◌
            // ↙◌◌◌
            UpdateNote(containerA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerMid, BeatmapNote.NoteCutDirectionDownLeft);
            UpdateNote(containerB, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionDownLeft);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ↙◌◌↙
            UpdateNote(containerA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionDownLeft);
            UpdateNote(containerB, BeatmapNote.LineIndexFarRight, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionDownLeft);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↘◌◌◌
            // ◌◌↘◌
            UpdateNote(containerA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerMid, BeatmapNote.NoteCutDirectionDownRight);
            UpdateNote(containerB, BeatmapNote.LineIndexMidRight, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionDownRight);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(63.43, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(63.43, containerB.transform.localEulerAngles.z, 0.01);

            // Changing this note to be in another beat should stop the angles snapping
            noteA.Time = 13;
            UpdateNote(containerA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerMid, BeatmapNote.NoteCutDirectionDownRight);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            notesContainer.RefreshSpecialAngles(noteB, true, false);
            Assert.AreEqual(45, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.transform.localEulerAngles.z, 0.01);

            // Make cleanup work
            noteA.Time = 14;
        }

        private void UpdateNote(BeatmapNoteContainer container, int lineIndex, int lineLayer, int cutDirection)
        {
            BeatmapNote note = (BeatmapNote)container.ObjectData;
            note.LineIndex = lineIndex;
            note.LineLayer = lineLayer;
            note.CutDirection = cutDirection;
            container.UpdateGridPosition();
            container.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(note);
        }

        [Test]
        public void ShiftInTime()
        {
            BeatmapObjectContainerCollection notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);
            UnityEngine.Transform root = notesContainer.transform.root;

            BeatmapNote noteA = new BeatmapNote
            {
                Time = 2,
                Type = BeatmapNote.NoteTypeA
            };
            notesContainer.SpawnObject(noteA);

            BeatmapNote noteB = new BeatmapNote
            {
                Time = 3,
                Type = BeatmapNote.NoteTypeA
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

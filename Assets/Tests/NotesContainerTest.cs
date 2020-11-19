using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class NotesContainerTest
    {
        private GameObject prefab = Resources.Load<GameObject>("Unassigned Note");

        [Test]
        public void TestRefreshSpecialAngles()
        {
            var notesContainer = new NotesContainer();

            var noteA = new BeatmapNote
            {
                _time = 14,
                _type = BeatmapNote.NOTE_TYPE_A
            };
            var containerA = BeatmapNoteContainer.SpawnBeatmapNote(noteA, ref prefab);
            notesContainer.LoadedContainers.Add(noteA, containerA);

            var noteB = new BeatmapNote
            {
                _time = 14,
                _type = BeatmapNote.NOTE_TYPE_A
            };
            var containerB = BeatmapNoteContainer.SpawnBeatmapNote(noteB, ref prefab);
            notesContainer.LoadedContainers.Add(noteB, containerB);

            // These tests are based of the examples in this image
            // https://media.discordapp.net/attachments/443569023951568906/681978249139585031/unknown.png

            // ◌◌◌◌
            // ◌→◌◌
            // ◌◌→◌
            UpdateNote(containerA, BeatmapNote.LINE_INDEX_MID_LEFT, BeatmapNote.LINE_LAYER_MID, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT);
            UpdateNote(containerB, BeatmapNote.LINE_INDEX_MID_RIGHT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(90, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(90, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌↙◌
            // ◌◌◌◌
            // ◌◌↙◌
            UpdateNote(containerA, BeatmapNote.LINE_INDEX_MID_RIGHT, BeatmapNote.LINE_LAYER_TOP, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
            UpdateNote(containerB, BeatmapNote.LINE_INDEX_MID_RIGHT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, BeatmapNote.LINE_INDEX_MID_RIGHT, BeatmapNote.LINE_LAYER_TOP, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
            UpdateNote(containerB, BeatmapNote.LINE_INDEX_MID_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(333.43, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌↓↓◌
            UpdateNote(containerA, BeatmapNote.LINE_INDEX_MID_RIGHT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
            UpdateNote(containerB, BeatmapNote.LINE_INDEX_MID_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(0, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↙◌◌◌
            // ↙◌◌◌
            UpdateNote(containerA, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_MID, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
            UpdateNote(containerB, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ↙◌◌↙
            UpdateNote(containerA, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
            UpdateNote(containerB, BeatmapNote.LINE_INDEX_FAR_RIGHT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.transform.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↘◌◌◌
            // ◌◌↘◌
            UpdateNote(containerA, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_MID, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);
            UpdateNote(containerB, BeatmapNote.LINE_INDEX_MID_RIGHT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(63.43, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(63.43, containerB.transform.localEulerAngles.z, 0.01);

            // Changing this note to be in another beat should stop the angles snapping
            noteA._time = 13;
            UpdateNote(containerA, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_MID, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);

            notesContainer.RefreshSpecialAngles(noteA, true, false);
            notesContainer.RefreshSpecialAngles(noteB, true, false);
            Assert.AreEqual(45, containerA.transform.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.transform.localEulerAngles.z, 0.01);
        }

        private void UpdateNote(BeatmapNoteContainer container, int lineIndex, int lineLayer, int cutDirection)
        {
            var note = (BeatmapNote)container.objectData;
            note._lineIndex = lineIndex;
            note._lineLayer = lineLayer;
            note._cutDirection = cutDirection;
            container.UpdateGridPosition();
            container.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(note);
        }
    }
}

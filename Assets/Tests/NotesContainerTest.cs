using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class NotesContainerTest
    {
        private GameObject prefab = Resources.Load<GameObject>("Unassigned Note");

        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            yield return SceneManager.LoadSceneAsync("00_FirstBoot", LoadSceneMode.Single);
            yield return new WaitUntil(() => SceneManager.GetActiveScene().buildIndex == 1 && !SceneTransitionManager.IsLoading);
            BeatSaberSongContainer.Instance.song = new BeatSaberSong("testmap", new JSONObject());
            var parentSet = new BeatSaberSong.DifficultyBeatmapSet();
            var diff = new BeatSaberSong.DifficultyBeatmap(parentSet);
            diff.customData = new JSONObject();
            BeatSaberSongContainer.Instance.difficultyData = diff;
            BeatSaberSongContainer.Instance.loadedSong = AudioClip.Create("MySinusoid", 44100 * 2, 1, 44100, false);
            SceneTransitionManager.Instance.LoadScene(3);
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
        }

        [Test]
        public void RefreshSpecialAngles()
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

        [Test]
        public void ShiftInTime()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            var root = notesContainer.transform.root;

            var noteA = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_A
            };
            notesContainer.SpawnObject(noteA);

            var noteB = new BeatmapNote
            {
                _time = 3,
                _type = BeatmapNote.NOTE_TYPE_A
            };
            notesContainer.SpawnObject(noteB);

            SelectionController.Select(noteB, false, false, false);

            var selectionController = root.GetComponentInChildren<SelectionController>();
            selectionController.MoveSelection(-2);

            notesContainer.DeleteObject(noteB);

            Assert.AreEqual(1, notesContainer.LoadedContainers.Count);
            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
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

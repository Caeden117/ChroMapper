using NUnit.Framework;
using System.Collections;
using System.Linq;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class NoteTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupNotes();
        }

        public static void CheckNote(BeatmapObjectContainerCollection container, int idx, int time, int type, int index, int layer, int cutDirection, JSONNode customData = null)
        {
            var newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BeatmapNote>(newObjA);
            if (newObjA is BeatmapNote newNoteA)
            {
                Assert.AreEqual(time, newNoteA._time);
                Assert.AreEqual(type, newNoteA._type);
                Assert.AreEqual(index, newNoteA._lineIndex);
                Assert.AreEqual(layer, newNoteA._lineLayer);
                Assert.AreEqual(cutDirection, newNoteA._cutDirection);
                
                if (customData != null) Assert.AreEqual(customData.ToString(), newNoteA._customData.ToString());
            }
        }
        
        [Test]
        public void InvertNote()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            if (containerCollection is NotesContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();
                var inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

                var noteA = new BeatmapNote(2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            
                notePlacement.queuedData = noteA;
                notePlacement.RoundedTime = notePlacement.queuedData._time;
                notePlacement.ApplyToMap();

                if (notesContainer.LoadedContainers[noteA] is BeatmapNoteContainer containerA)
                {
                    inputController.InvertNote(containerA);
                }

                CheckNote(notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_B, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);

                // Undo invert
                actionContainer.Undo();
                
                CheckNote(notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            }
        }
        
        [Test]
        public void UpdateNoteDirection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            if (containerCollection is NotesContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();
                var inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

                var noteA = new BeatmapNote(2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            
                notePlacement.queuedData = noteA;
                notePlacement.RoundedTime = notePlacement.queuedData._time;
                notePlacement.ApplyToMap();

                if (notesContainer.LoadedContainers[noteA] is BeatmapNoteContainer containerA)
                {
                    inputController.UpdateNoteDirection(containerA, true);
                }

                CheckNote(notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT);

                // Undo direction
                actionContainer.Undo();
                
                CheckNote(notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            }
        }
    }
}

using NUnit.Framework;
using System.Collections;
using System.Linq;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class MirrorTest
    {
        private BeatmapActionContainer _actionContainer;
        private MirrorSelection _mirror;
        private BeatmapObjectContainerCollection _notesContainer;
        private Transform _root;
        private NotePlacement _notePlacement;
        
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            yield return TestUtils.LoadMapper();

            _actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            _mirror = Object.FindObjectOfType<MirrorSelection>();
            _notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            _root = _notesContainer.transform.root;
            _notePlacement = _root.GetComponentInChildren<NotePlacement>();
        }

        [SetUp]
        public void SpawnNotes()
        {
            var noteA = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_A,
                _lineIndex = BeatmapNote.LINE_INDEX_FAR_LEFT,
                _lineLayer = BeatmapNote.LINE_LAYER_BOTTOM,
                _cutDirection = BeatmapNote.NOTE_CUT_DIRECTION_LEFT
            };
            var noteB = new BeatmapNote
            {
                _time = 3,
                _type = BeatmapNote.NOTE_TYPE_B,
                _lineIndex = BeatmapNote.LINE_INDEX_FAR_RIGHT,
                _lineLayer = BeatmapNote.LINE_LAYER_TOP,
                _cutDirection = BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT
            };

            _notePlacement.queuedData = noteA;
            _notePlacement.RoundedTime = _notePlacement.queuedData._time;
            _notePlacement.ApplyToMap();

            // Should conflict with existing note and delete it
            _notePlacement.queuedData = noteB;
            _notePlacement.RoundedTime = _notePlacement.queuedData._time;
            _notePlacement.ApplyToMap();
            
            SelectionController.Select(noteA);
            SelectionController.Select(noteB, true);
        }
        
        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupNotes();
        }

        [Test]
        public void MirrorInTime()
        {
            _mirror.MirrorTime();
            
            // Check we can still delete our objects
            var toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);
            
            _actionContainer.Undo();
            
            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_B, BeatmapNote.LINE_INDEX_FAR_RIGHT, BeatmapNote.LINE_LAYER_TOP, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);

            // Undo mirror
            _actionContainer.Undo();

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NOTE_TYPE_B, BeatmapNote.LINE_INDEX_FAR_RIGHT, BeatmapNote.LINE_LAYER_TOP, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
        }
        
        [Test]
        public void Mirror()
        {
            _mirror.Mirror();
            
            // Check we can still delete our objects
            var toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);
            
            _actionContainer.Undo();
            
            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);
            
            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_B, BeatmapNote.LINE_INDEX_FAR_RIGHT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_TOP, BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT);

            // Undo mirror
            _actionContainer.Undo();

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NOTE_TYPE_B, BeatmapNote.LINE_INDEX_FAR_RIGHT, BeatmapNote.LINE_LAYER_TOP, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
        }
        
        [Test]
        public void SwapColors()
        {
            _mirror.Mirror(false);
            
            // Check we can still delete our objects
            var toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);
            
            _actionContainer.Undo();
            
            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);
            
            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_B, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_RIGHT, BeatmapNote.LINE_LAYER_TOP, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);

            // Undo mirror
            _actionContainer.Undo();

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NOTE_TYPE_A, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapNote.LINE_LAYER_BOTTOM, BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NOTE_TYPE_B, BeatmapNote.LINE_INDEX_FAR_RIGHT, BeatmapNote.LINE_LAYER_TOP, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
        }
    }
}

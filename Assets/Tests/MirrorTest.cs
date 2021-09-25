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
            _notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);
            _root = _notesContainer.transform.root;
            _notePlacement = _root.GetComponentInChildren<NotePlacement>();
        }

        [SetUp]
        public void SpawnNotes()
        {
            BeatmapNote noteA = new BeatmapNote
            {
                Time = 2,
                Type = BeatmapNote.NoteTypeA,
                LineIndex = BeatmapNote.LineIndexFarLeft,
                LineLayer = BeatmapNote.LineLayerBottom,
                CutDirection = BeatmapNote.NoteCutDirectionLeft
            };
            BeatmapNote noteB = new BeatmapNote
            {
                Time = 3,
                Type = BeatmapNote.NoteTypeB,
                LineIndex = BeatmapNote.LineIndexFarRight,
                LineLayer = BeatmapNote.LineLayerTop,
                CutDirection = BeatmapNote.NoteCutDirectionUpRight
            };

            _notePlacement.queuedData = noteA;
            _notePlacement.RoundedTime = _notePlacement.queuedData.Time;
            _notePlacement.ApplyToMap();

            // Should conflict with existing note and delete it
            _notePlacement.queuedData = noteB;
            _notePlacement.RoundedTime = _notePlacement.queuedData.Time;
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
            BeatmapObject toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NoteTypeB, BeatmapNote.LineIndexFarRight, BeatmapNote.LineLayerTop, BeatmapNote.NoteCutDirectionUpRight);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);

            // Undo mirror
            _actionContainer.Undo();

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NoteTypeB, BeatmapNote.LineIndexFarRight, BeatmapNote.LineLayerTop, BeatmapNote.NoteCutDirectionUpRight);
        }

        [Test]
        public void Mirror()
        {
            _mirror.Mirror();

            // Check we can still delete our objects
            BeatmapObject toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NoteTypeB, BeatmapNote.LineIndexFarRight, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionRight);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerTop, BeatmapNote.NoteCutDirectionUpLeft);

            // Undo mirror
            _actionContainer.Undo();

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NoteTypeB, BeatmapNote.LineIndexFarRight, BeatmapNote.LineLayerTop, BeatmapNote.NoteCutDirectionUpRight);
        }

        [Test]
        public void SwapColors()
        {
            _mirror.Mirror(false);

            // Check we can still delete our objects
            BeatmapObject toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NoteTypeB, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarRight, BeatmapNote.LineLayerTop, BeatmapNote.NoteCutDirectionUpRight);

            // Undo mirror
            _actionContainer.Undo();

            NoteTest.CheckNote(_notesContainer, 0, 2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);
            NoteTest.CheckNote(_notesContainer, 1, 3, BeatmapNote.NoteTypeB, BeatmapNote.LineIndexFarRight, BeatmapNote.LineLayerTop, BeatmapNote.NoteCutDirectionUpRight);
        }
    }
}

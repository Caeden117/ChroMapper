using NUnit.Framework;
using System.Collections;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V3;
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
            _notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            _root = _notesContainer.transform.root;
            _notePlacement = _root.GetComponentInChildren<NotePlacement>();
        }

        [SetUp]
        public void SpawnNotes()
        {
            BaseNote baseNoteA = new V3ColorNote
            {
                Time = 2,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                CutDirection = (int)NoteCutDirection.Left
            };
            BaseNote baseNoteB = new V3ColorNote
            {
                Time = 3,
                Type = (int)NoteType.Blue,
                PosX = (int)GridX.Right,
                PosY = (int)GridY.Top,
                CutDirection = (int)NoteCutDirection.UpRight
            };

            PlaceUtils.PlaceNote(_notePlacement, baseNoteA);

            // Should conflict with existing note and delete it
            PlaceUtils.PlaceNote(_notePlacement, baseNoteB);

            SelectionController.Select(baseNoteA);
            SelectionController.Select(baseNoteB, true);
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupNotes();
            TestUtils.ReturnSettings();
        }

        [Test]
        public void MirrorInTime()
        {
            _mirror.MirrorTime();

            // Check we can still delete our objects
            BaseObject toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);

            CheckUtils.CheckNote("Check first mirrored time", _notesContainer, 0, 2, (int)NoteType.Blue, (int)GridX.Right, (int)GridY.Top, (int)NoteCutDirection.UpRight, 0);
            CheckUtils.CheckNote("Check second mirrored time", _notesContainer, 1, 3, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);

            // Undo mirror
            _actionContainer.Undo();

            CheckUtils.CheckNote("Check undo first mirrored time", _notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckNote("Check undo second mirrored time ", _notesContainer, 1, 3, (int)NoteType.Blue, (int)GridX.Right, (int)GridY.Top, (int)NoteCutDirection.UpRight, 0);
        }

        [Test]
        public void Mirror()
        {
            _mirror.Mirror();

            // Check we can still delete our objects
            BaseObject toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);

            CheckUtils.CheckNote("Check first mirrored note", _notesContainer, 0, 2, (int)NoteType.Blue, (int)GridX.Right, (int)GridY.Base, (int)NoteCutDirection.Right, 0);
            CheckUtils.CheckNote("Check second mirrored note", _notesContainer, 1, 3, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Top, (int)NoteCutDirection.UpLeft, 0);

            // Undo mirror
            _actionContainer.Undo();

            CheckUtils.CheckNote("Check undo first mirrored note", _notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckNote("Check undo second mirrored note", _notesContainer, 1, 3, (int)NoteType.Blue, (int)GridX.Right, (int)GridY.Top, (int)NoteCutDirection.UpRight, 0);
        }

        [Test]
        public void SwapColors()
        {
            _mirror.Mirror(false);

            // Check we can still delete our objects
            BaseObject toDelete = _notesContainer.UnsortedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);

            CheckUtils.CheckNote("Check first mirrored color swap", _notesContainer, 0, 2, (int)NoteType.Blue, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckNote("Check second mirrored color swap", _notesContainer, 1, 3, (int)NoteType.Red, (int)GridX.Right, (int)GridY.Top, (int)NoteCutDirection.UpRight, 0);

            // Undo mirror
            _actionContainer.Undo();

            CheckUtils.CheckNote("Check undo first mirrored color swap", _notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckNote("Check undo second mirrored color swap", _notesContainer, 1, 3, (int)NoteType.Blue, (int)GridX.Right, (int)GridY.Top, (int)NoteCutDirection.UpRight, 0);
        }
    }
}

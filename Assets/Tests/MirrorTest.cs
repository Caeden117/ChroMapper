using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class MirrorTest
    {
        private BeatmapActionContainer _actionContainer;
        private MirrorSelection _mirror;
        private NotePlacement _notePlacement;
        private ArcPlacement _arcPlacement;
        private BeatmapObjectContainerCollection _notesContainer;
        private BeatmapObjectContainerCollection _arcsContainer;
        private Transform _root;

        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            yield return TestUtils.LoadMap(3);

            _actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            _mirror = Object.FindObjectOfType<MirrorSelection>();
            _notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            _arcsContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            _root = _notesContainer.transform.root;
            _notePlacement = _root.GetComponentInChildren<NotePlacement>();
            _arcPlacement = _root.GetComponentInChildren<ArcPlacement>();
        }

        [SetUp]
        public void SpawnNotesAndArcs()
        {
            BaseNote baseNoteA = new V3ColorNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                CutDirection = (int)NoteCutDirection.Left
            };
            BaseNote baseNoteB = new V3ColorNote
            {
                JsonTime = 3,
                Type = (int)NoteType.Blue,
                PosX = (int)GridX.Right,
                PosY = (int)GridY.Top,
                CutDirection = (int)NoteCutDirection.UpRight
            };
            BaseArc baseArc = new V3Arc
            {
                JsonTime = 2,
                Color = (int)NoteType.Blue,
                PosX = (int)GridX.MiddleLeft,
                PosY = (int)GridY.Base,
                CutDirection = (int)NoteCutDirection.Left,
                HeadControlPointLengthMultiplier = 1,
                TailJsonTime = 3,
                TailPosX = (int)GridX.MiddleRight,
                TailPosY = (int)GridY.Top,
                TailCutDirection = (int)NoteCutDirection.Right,
                TailControlPointLengthMultiplier = 2,
                MidAnchorMode = 0
            };

            PlaceUtils.PlaceNote(_notePlacement, baseNoteA);

            // Should conflict with existing note and delete it
            PlaceUtils.PlaceNote(_notePlacement, baseNoteB);
            PlaceUtils.PlaceArc(_arcPlacement, baseArc);

            SelectionController.Select(baseNoteA);
            SelectionController.Select(baseNoteB, true);
            SelectionController.Select(baseArc, true);
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupNotes();
            CleanupUtils.CleanupArcs();
        }

        [Test]
        public void MirrorInTime()
        {
            _mirror.MirrorTime();

            // Check we can still delete our objects
            var toDelete = _notesContainer.LoadedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);
            Assert.AreEqual(1, _arcsContainer.LoadedObjects.Count);

            CheckUtils.CheckNote("Check first mirrored time", _notesContainer, 0, 2, (int)GridX.Right, (int)GridY.Top,
                (int)NoteType.Blue, (int)NoteCutDirection.UpRight, 0);
            CheckUtils.CheckNote("Check second mirrored time", _notesContainer, 1, 3, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckArc("Check arc mirrored time", _arcsContainer, 0, 2, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteType.Blue,
                (int)NoteCutDirection.Right, 0, 2, 3, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Left, 1, 0);

            // Undo mirror
            _actionContainer.Undo();

            CheckUtils.CheckNote("Check undo first mirrored time", _notesContainer, 0, 2, (int)GridX.Left,
                (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckNote("Check undo second mirrored time ", _notesContainer, 1, 3, (int)GridX.Right,
                (int)GridY.Top, (int)NoteType.Blue, (int)NoteCutDirection.UpRight, 0);
            CheckUtils.CheckArc("Check undo arc mirrored time", _arcsContainer, 0, 2, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteType.Blue,
                (int)NoteCutDirection.Left, 0, 1, 3, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Right, 2, 0);
        }

        [Test]
        public void Mirror()
        {
            _mirror.Mirror();

            // Check we can still delete our objects
            var toDelete = _notesContainer.LoadedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);
            Assert.AreEqual(1, _arcsContainer.LoadedObjects.Count);

            CheckUtils.CheckNote("Check first mirrored note", _notesContainer, 0, 2, (int)GridX.Right, (int)GridY.Base,
                (int)NoteType.Blue, (int)NoteCutDirection.Right, 0);
            CheckUtils.CheckNote("Check second mirrored note", _notesContainer, 1, 3, (int)GridX.Left, (int)GridY.Top,
                (int)NoteType.Red, (int)NoteCutDirection.UpLeft, 0);
            CheckUtils.CheckArc("Check mirrored arc", _arcsContainer, 0, 2, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteType.Red,
                (int)NoteCutDirection.Right, 0, 1, 3, (int)GridX.MiddleLeft, (int)GridY.Top, (int)NoteCutDirection.Left, 2, 0);

            // Undo mirror
            _actionContainer.Undo();

            CheckUtils.CheckNote("Check undo first mirrored note", _notesContainer, 0, 2, (int)GridX.Left,
                (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckNote("Check undo second mirrored note", _notesContainer, 1, 3, (int)GridX.Right,
                (int)GridY.Top, (int)NoteType.Blue, (int)NoteCutDirection.UpRight, 0);
            CheckUtils.CheckArc("Check undo mirrored arc", _arcsContainer, 0, 2, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteType.Blue,
                (int)NoteCutDirection.Left, 0, 1, 3, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Right, 2, 0);
        }

        [Test]
        public void SwapColors()
        {
            _mirror.Mirror(false);

            // Check we can still delete our objects
            var toDelete = _notesContainer.LoadedObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.LoadedObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.LoadedObjects.Count);
            Assert.AreEqual(1, _arcsContainer.LoadedObjects.Count);

            CheckUtils.CheckNote("Check first mirrored color swap", _notesContainer, 0, 2, (int)GridX.Left,
                (int)GridY.Base, (int)NoteType.Blue, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckNote("Check second mirrored color swap", _notesContainer, 1, 3, (int)GridX.Right,
                (int)GridY.Top, (int)NoteType.Red, (int)NoteCutDirection.UpRight, 0);
            CheckUtils.CheckArc("Check mirrored arc color swap", _arcsContainer, 0, 2, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteType.Red,
                (int)NoteCutDirection.Left, 0, 1, 3, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Right, 2, 0);

            // Undo mirror
            _actionContainer.Undo();

            CheckUtils.CheckNote("Check undo first mirrored color swap", _notesContainer, 0, 2, (int)GridX.Left,
                (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            CheckUtils.CheckNote("Check undo second mirrored color swap", _notesContainer, 1, 3, (int)GridX.Right,
                (int)GridY.Top, (int)NoteType.Blue, (int)NoteCutDirection.UpRight, 0);
            CheckUtils.CheckArc("Check undo mirrored arc color swap", _arcsContainer, 0, 2, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteType.Blue,
                (int)NoteCutDirection.Left, 0, 1, 3, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Right, 2, 0);
        }
    }
}
using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class NoteTest
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
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupNotes();
        }

        [Test]
        public void InvertNote()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();
                var inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

                BaseNote baseNoteA = new V3ColorNote(2, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red,
                    (int)NoteCutDirection.Left);
                PlaceUtils.PlaceNote(notePlacement, baseNoteA);

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                    inputController.InvertNote(containerA);

                CheckUtils.CheckNote("Perform note inversion", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Blue, (int)NoteCutDirection.Left, 0);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckNote("Undo note inversion", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            }
        }

        [Test]
        public void UpdateNoteDirection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();
                var inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

                BaseNote baseNoteA = new V3ColorNote(2, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red,
                    (int)NoteCutDirection.Left);
                PlaceUtils.PlaceNote(notePlacement, baseNoteA);

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                    inputController.UpdateNoteDirection(containerA, true);

                CheckUtils.CheckNote("Update note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.UpLeft, 0);

                // Undo direction
                actionContainer.Undo();

                CheckUtils.CheckNote("Undo note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            }
        }
    }
}
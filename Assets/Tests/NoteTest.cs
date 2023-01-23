using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V3;
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
            return TestUtils.LoadMapper();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupNotes();
            TestUtils.ReturnSettings();
        }

        [Test]
        public void InvertNote()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                Transform root = notesContainer.transform.root;
                NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();
                BeatmapNoteInputController inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

                BaseNote baseNoteA = new V3ColorNote(2, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Left);
                PlaceUtils.PlaceNote(notePlacement, baseNoteA);

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                {
                    inputController.InvertNote(containerA);
                }

                CheckUtils.CheckNote("Perform note inversion", notesContainer, 0, 2, (int)NoteType.Blue, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckNote("Undo note inversion", notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
            }
        }

        [Test]
        public void UpdateNoteDirection()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                Transform root = notesContainer.transform.root;
                NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();
                BeatmapNoteInputController inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

                BaseNote baseNoteA = new V3ColorNote(2, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Left);
                PlaceUtils.PlaceNote(notePlacement, baseNoteA);

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                {
                    inputController.UpdateNoteDirection(containerA, true);
                }

                CheckUtils.CheckNote("Update note direction", notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.UpLeft, 0);

                // Undo direction
                actionContainer.Undo();

                CheckUtils.CheckNote("Undo note direction", notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
            }
        }
    }
}

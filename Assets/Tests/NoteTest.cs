using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
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
                    (int)NoteType.Red, (int)NoteCutDirection.DownLeft, 0);

                // Undo direction
                actionContainer.Undo();

                CheckUtils.CheckNote("Undo note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            }
        }

        [Test]
        public void PlacementPersistsCustomProperty()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();

                var customDirection = 69;
                var localRotation = new JSONArray() { [0] = 0, [1] = 1, [2] = 2 };

                BaseNote v3NoteA = new V3ColorNote(2, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red,
                    (int)NoteCutDirection.Left);
                v3NoteA.CustomLocalRotation = localRotation;
                v3NoteA.CustomDirection = customDirection;

                PlaceUtils.PlaceNote(notePlacement, v3NoteA);

                CheckUtils.CheckNote("Applies CustomProperties to v3 CustomData", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0,
                    new JSONObject() { ["localRotation"] = localRotation });

                BaseNote v2NoteB = new V2Note(4, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red,
                (int)NoteCutDirection.Left);
                v2NoteB.CustomDirection = customDirection;
                v2NoteB.CustomLocalRotation = localRotation;

                PlaceUtils.PlaceNote(notePlacement, v2NoteB);

                CheckUtils.CheckNote("Applies CustomProperties to v2 CustomData", notesContainer, 1, 4, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0,
                    new JSONObject() { ["_localRotation"] = localRotation, ["_cutDirection"] = customDirection });
            }
        }
    }
}
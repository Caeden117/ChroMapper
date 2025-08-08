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
            CleanupUtils.CleanupArcs();
            CleanupUtils.CleanupChains();
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

                BaseNote baseNoteA = new BaseNote
                {
                    JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
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
        public void InvertNoteAffectsSlider()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var arcsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var chainsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();
            var arcPlacement = root.GetComponentInChildren<ArcPlacement>();
            var chainPlacement = root.GetComponentInChildren<ChainPlacement>();
            var inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

            BaseNote baseNote1 = new BaseNote
            {
                JsonTime = 1, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            BaseNote baseNote2 = new BaseNote
            {
                JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            BaseNote baseNote3 = new BaseNote
            {
                JsonTime = 3, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };

            BaseArc baseArc12 = new BaseArc { JsonTime = 1, TailJsonTime = 2, Color = (int)NoteColor.Red };
            BaseChain baseChain23 = new BaseChain { JsonTime = 2, TailJsonTime = 3, Color = (int)NoteColor.Red };

            PlaceUtils.PlaceNote(notePlacement, baseNote1);
            PlaceUtils.PlaceNote(notePlacement, baseNote2);
            PlaceUtils.PlaceNote(notePlacement, baseNote3);

            PlaceUtils.PlaceArc(arcPlacement, baseArc12);
            PlaceUtils.PlaceChain(chainPlacement, baseChain23);

            if (notesContainer.LoadedContainers[baseNote1] is NoteContainer container1)
                inputController.InvertNote(container1);

            CheckUtils.CheckArc("Arc inverted", arcsContainer, 0, 1, default, default, (int)NoteColor.Blue, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain not inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Undo arc inversion", arcsContainer, 0, 1, default, default, (int)NoteColor.Red, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain still not inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);

            if (notesContainer.LoadedContainers[baseNote2] is NoteContainer container2)
                inputController.InvertNote(container2);

            CheckUtils.CheckArc("Arc inverted", arcsContainer, 0, 1, default, default, (int)NoteColor.Blue, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Blue, default, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Undo arc inversion", arcsContainer, 0, 1, default, default, (int)NoteColor.Red, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Undo chain inversion", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);

            if (notesContainer.LoadedContainers[baseNote3] is NoteContainer container3)
                inputController.InvertNote(container3);

            CheckUtils.CheckArc("Arc not inverted", arcsContainer, 0, 1, default, default, (int)NoteColor.Red, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain not inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Arc still not inverted", arcsContainer, 0, 1, default, default, (int)NoteColor.Red, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain not inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);
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

                BaseNote baseNoteA = new BaseNote
                {
                    JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                        CutDirection = (int)NoteCutDirection.Left
                };
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
        public void UpdateNoteDirectionMergeAction()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            
            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();
            var inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

            var baseNoteA = new BaseNote
            {
                JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            PlaceUtils.PlaceNote(notePlacement, baseNoteA);

            if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                inputController.UpdateNoteDirection(containerA, true);

            CheckUtils.CheckNote("Update note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.DownLeft, 0);
            
            if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerB)
                inputController.UpdateNoteDirection(containerB, true);

            CheckUtils.CheckNote("Update note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Down, 0);
            
            // Undo merged direction
            actionContainer.Undo();

            CheckUtils.CheckNote("Undo note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Left, 0);

            // Redo merged direction
            actionContainer.Redo();
            
            CheckUtils.CheckNote("Undo note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Down, 0);
        }

        [Test]
        public void UpdateNoteDirectionAffectsSlider()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var arcsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var chainsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();
            var arcPlacement = root.GetComponentInChildren<ArcPlacement>();
            var chainPlacement = root.GetComponentInChildren<ChainPlacement>();
            var inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

            BaseNote baseNote1 = new BaseNote
            {
                JsonTime = 1, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            BaseNote baseNote2 = new BaseNote
            {
                JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Up
            };
            BaseNote baseNote3 = new BaseNote
            {
                JsonTime = 3, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Right
            };

            BaseArc baseArc12 = new BaseArc { JsonTime = 1, TailJsonTime = 2, CutDirection = (int)NoteCutDirection.Left, TailCutDirection = (int)NoteCutDirection.Up };
            BaseChain baseChain23 = new BaseChain { JsonTime = 2, TailJsonTime = 3, CutDirection = (int)NoteCutDirection.Up };

            PlaceUtils.PlaceNote(notePlacement, baseNote1);
            PlaceUtils.PlaceNote(notePlacement, baseNote2);
            PlaceUtils.PlaceNote(notePlacement, baseNote3);

            PlaceUtils.PlaceArc(arcPlacement, baseArc12);
            PlaceUtils.PlaceChain(chainPlacement, baseChain23);

            if (notesContainer.LoadedContainers[baseNote1] is NoteContainer container1)
                inputController.UpdateNoteDirection(container1, false);

            CheckUtils.CheckArc("Arc head direction", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.UpLeft, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Chain direction not changed", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Undo arc head direction", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Chain direction still not changed", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);

            if (notesContainer.LoadedContainers[baseNote2] is NoteContainer container2)
                inputController.UpdateNoteDirection(container2, false);

            CheckUtils.CheckArc("Arc tail direction", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.UpRight, default, default);
            CheckUtils.CheckChain("Chain direction", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.UpRight, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Undo arc tail direction", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Undo chain direction", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);

            if (notesContainer.LoadedContainers[baseNote3] is NoteContainer container3)
                inputController.UpdateNoteDirection(container3, false);

            CheckUtils.CheckArc("Arc direction not changed", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Chain direction not changed", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Arc direction still not changed", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Chain direction still not changed", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);
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

                Settings.Instance.MapVersion = 3;
                BaseNote v3NoteA = new BaseNote
                {
                    JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                v3NoteA.CustomLocalRotation = localRotation;
                v3NoteA.CustomDirection = customDirection;

                PlaceUtils.PlaceNote(notePlacement, v3NoteA);

                CheckUtils.CheckNote("Applies CustomProperties to v3 CustomData", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0,
                    new JSONObject() { ["localRotation"] = localRotation });

                Settings.Instance.MapVersion = 2;
                BaseNote v2NoteB = new BaseNote
                {
                    JsonTime = 4, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
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
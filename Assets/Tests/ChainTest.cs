using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ChainTest
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
            CleanupUtils.CleanupChains();
        }

        [Test]
        public void CreateChain()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();

                BaseNote baseNoteA = new BaseNote
                {
                    JsonTime = 2f, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down
                };
                BaseNote baseNoteB = new BaseNote
                {
                    JsonTime =3f, PosX = (int)GridX.Left, PosY = (int)GridY.Upper, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up
                };
                PlaceUtils.PlaceNote(notePlacement, baseNoteA);
                PlaceUtils.PlaceNote(notePlacement, baseNoteB);

                SelectionController.Select(baseNoteA);
                SelectionController.Select(baseNoteB, true);
            }

            var chainContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (chainContainerCollection is ChainGridContainer chainsContainer)
            {
                var root = chainsContainer.transform.root;
                var chainPlacement = root.GetComponentInChildren<ChainPlacement>();

                var objects = SelectionController.SelectedObjects.ToList();

                Assert.AreEqual(2, objects.Count);

                if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                    Assert.Fail("Both selected objects is not color note");
                var n1 = objects[0] as BaseNote;
                var n2 = objects[1] as BaseNote;

                chainPlacement.TryCreateChainData(n1, n2, out var chain, out var tailNote);
                chainsContainer.SpawnObject(chain);

                CheckUtils.CheckChain("Check generated chain", chainsContainer, 0, 2f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteColor.Red, (int)NoteCutDirection.Down, 0, 3f, (int)GridX.Left, (int)GridY.Upper, 5, 1);
                Assert.AreSame(n2, tailNote);
            }
        }

        [Test]
        public void CreateChainWithCoordinates()
        {
            var headCoordinates = new JSONArray { [0] = 69, [1] = 69 };
            var tailCoordinates = new JSONArray { [0] = 420, [1] = 420 };

            var headCustomData = new JSONObject { ["coordinates"] = headCoordinates };
            var tailCustomData = new JSONObject { ["coordinates"] = tailCoordinates };

            var chainCustomData = new JSONObject
            {
                ["coordinates"] = headCoordinates,
                ["tailCoordinates"] = tailCoordinates
            };

            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();

                BaseNote baseNoteA = new BaseNote
                {
                    JsonTime = 2f, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down, CustomData = headCustomData
                };
                baseNoteA.RefreshCustom();

                BaseNote baseNoteB = new BaseNote
                {
                    JsonTime = 3f, PosX = (int)GridX.Left, PosY = (int)GridY.Upper, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up, CustomData = tailCustomData
                };
                baseNoteB.RefreshCustom();

                PlaceUtils.PlaceNote(notePlacement, baseNoteA);
                PlaceUtils.PlaceNote(notePlacement, baseNoteB);

                SelectionController.Select(baseNoteA);
                SelectionController.Select(baseNoteB, true);
            }

            var chainContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (chainContainerCollection is ChainGridContainer chainsContainer)
            {
                var root = chainsContainer.transform.root;
                var chainPlacement = root.GetComponentInChildren<ChainPlacement>();

                var objects = SelectionController.SelectedObjects.ToList();

                Assert.AreEqual(2, objects.Count);

                if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                    Assert.Fail("Both selected objects is not color note");
                var n1 = objects[0] as BaseNote;
                var n2 = objects[1] as BaseNote;

                var arc = chainPlacement.TryCreateChainData(n1, n2, out var chain, out var note);
                chainsContainer.SpawnObject(chain);

                CheckUtils.CheckChain("Check generated chain", chainsContainer, 0, 2f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteColor.Red, (int)NoteCutDirection.Down, 0, 3f, (int)GridX.Left, (int)GridY.Upper,
                    5, 1, chainCustomData);
            }
        }

        [Test]
        public void InvertChain()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (containerCollection is ChainGridContainer chainsContainer)
            {
                var root = chainsContainer.transform.root;
                var chainPlacement = root.GetComponentInChildren<ChainPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapChainInputController>();

                BaseChain baseChain = new BaseChain
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteColor.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    TailJsonTime = 3f,
                    TailPosX = (int)GridX.Left,
                    TailPosY = (int)GridY.Base,
                    SliceCount = 5,
                    Squish = 1.5f
                };
                PlaceUtils.PlaceChain(chainPlacement, baseChain);

                if (chainsContainer.LoadedContainers[baseChain] is ChainContainer containerA)
                    inputController.InvertChain(containerA);

                CheckUtils.CheckChain("Perform chain inversion", chainsContainer, 0, 2f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteColor.Blue, (int)NoteCutDirection.Left, 0, 3f, (int)GridX.Left,
                    (int)GridY.Base, 5, 1f);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckChain("Undo chain inversion", chainsContainer, 0, 2f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteColor.Red, (int)NoteCutDirection.Left, 0, 3f, (int)GridX.Left, (int)GridY.Base, 5, 1f);
            }
        }

        [Test]
        public void UpdateChainMultiplier()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (containerCollection is ChainGridContainer chainsContainer)
            {
                var root = chainsContainer.transform.root;
                var chainPlacement = root.GetComponentInChildren<ChainPlacement>();
                var inputController = root.GetComponentInChildren<BeatmapChainInputController>();

                BaseChain baseChain = new BaseChain
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteColor.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    TailJsonTime = 3f,
                    TailPosX = (int)GridX.Left,
                    TailPosY = (int)GridY.Base,
                    SliceCount = 5,
                    Squish = 1.5f
                };
                PlaceUtils.PlaceChain(chainPlacement, baseChain);

                if (chainsContainer.LoadedContainers[baseChain] is ChainContainer containerA)
                    inputController.TweakChainSquish(containerA, 0.5f);

                CheckUtils.CheckChain("Update chain multiplier", chainsContainer, 0, 2f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteColor.Red, (int)NoteCutDirection.Left, 0, 3f, (int)GridX.Left,
                    (int)GridY.Base, 5, 1.5f);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckChain("Undo update chain multiplier", chainsContainer, 0, 2f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteColor.Red, (int)NoteCutDirection.Left, 0, 3f, (int)GridX.Left,
                    (int)GridY.Base, 5, 1f);
            }
        }
    }
}
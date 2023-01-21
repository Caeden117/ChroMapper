using NUnit.Framework;
using System.Collections;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.V3;
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
            return TestUtils.LoadMapper();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupNotes();
            TestUtils.CleanupChains();
        }

        [Test]
        public void CreateChain()
        {
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                Transform root = notesContainer.transform.root;
                NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();

                BaseNote baseNoteA = new V3ColorNote(2f, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Down);
                BaseNote baseNoteB = new V3ColorNote(3f, (int)GridX.Left, (int)GridY.Upper, (int)NoteType.Red, (int)NoteCutDirection.Up);
                PlaceUtils.PlaceNote(notePlacement, baseNoteA);
                PlaceUtils.PlaceNote(notePlacement, baseNoteB);

                SelectionController.Select(baseNoteA);
                SelectionController.Select(baseNoteB, true);
                
            }

            BeatmapObjectContainerCollection chainContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (chainContainerCollection is ChainGridContainer chainsContainer)
            {
                Transform root = chainsContainer.transform.root;
                ChainPlacement chainPlacement = root.GetComponentInChildren<ChainPlacement>();
                
                var objects = SelectionController.SelectedObjects.ToList();
                
                Assert.AreEqual(2, objects.Count);
                
                if(!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                {
                    Assert.Fail("Both selected objects is not color note");
                }
                var n1 = objects[0] as BaseNote;
                var n2 = objects[1] as BaseNote;

                chainPlacement.SpawnChain(n1, n2);
                
                CheckUtils.CheckChain("Check generated chain", chainsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Down, 3f, (int)GridX.Left, (int)GridY.Upper, 5, 1);
            }
        }

        [Test]
        public void InvertChain()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (containerCollection is ChainGridContainer chainsContainer)
            {
                Transform root = chainsContainer.transform.root;
                ChainPlacement chainPlacement = root.GetComponentInChildren<ChainPlacement>();
                BeatmapChainInputController inputController = root.GetComponentInChildren<BeatmapChainInputController>();

                BaseChain baseChain = new V3Chain(2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0, 3f, (int)GridX.Left, (int)GridY.Base, 5, 1f);
                PlaceUtils.PlaceChain(chainPlacement, baseChain);

                if (chainsContainer.LoadedContainers[baseChain] is ChainContainer containerA)
                {
                    inputController.InvertChain(containerA);
                }

                CheckUtils.CheckChain("Perform chain inversion", chainsContainer, 0, 2f, (int)NoteColor.Blue, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 3f, (int)GridX.Left, (int)GridY.Base, 5, 1f);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckChain("Undo chain inversion", chainsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 3f, (int)GridX.Left, (int)GridY.Base, 5, 1f);
            }
        }

        [Test]
        public void UpdateChainMultiplier()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (containerCollection is ChainGridContainer chainsContainer)
            {
                Transform root = chainsContainer.transform.root;
                ChainPlacement chainPlacement = root.GetComponentInChildren<ChainPlacement>();
                BeatmapChainInputController inputController = root.GetComponentInChildren<BeatmapChainInputController>();

                BaseChain baseChain = new V3Chain(2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0, 3f, (int)GridX.Left, (int)GridY.Base, 5, 1f);
                PlaceUtils.PlaceChain(chainPlacement, baseChain);

                if (chainsContainer.LoadedContainers[baseChain] is ChainContainer containerA)
                {
                    inputController.TweakChainSquish(containerA, 0.5f);
                }

                CheckUtils.CheckChain("Update chain multiplier", chainsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 3f, (int)GridX.Left, (int)GridY.Base, 5, 1.5f);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckChain("Undo update chain multiplier", chainsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 3f, (int)GridX.Left, (int)GridY.Base, 5, 1f);
            }
        }
    }
}

using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    public class ChainTest : TestBase
    {
        [Test]
        public void CreateChain()
        {
            var noteA = new BaseNote
            {
                JsonTime = 2f,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            };
            var noteB = new BaseNote
            {
                JsonTime = 3f,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Upper,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Up
            };
            noteA = PlaceUtils.Place(noteA);
            noteB = PlaceUtils.Place(noteB);

            SelectionController.Select(noteA);
            SelectionController.Select(noteB, true);

            var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();

            var objects = SelectionController.SelectedObjects.ToList();

            Assert.AreEqual(2, objects.Count);

            Assert.IsTrue(
                ArcPlacement.IsColorNote(objects[0]) && ArcPlacement.IsColorNote(objects[1]),
                "Both selected objects is not color note");
            var n1 = (BaseNote)objects[0];
            var n2 = (BaseNote)objects[1];

            chainPlacement.TryCreateChainData(n1, n2, out var chain, out var tailNote);
            var originalChain = BeatmapFactory.Clone(chain);
            chain = PlaceUtils.Place(chain);

            var expected = BeatmapFactory.Clone(originalChain);
            BeatmapAssertion.IsEqual(expected, chain, "Check generated chain");
            Assert.AreSame(n2, tailNote);
        }

        [Test]
        public void CreateChainWithCoordinates()
        {
            var headCoordinates = new JSONArray { [0] = 69, [1] = 69 };
            var tailCoordinates = new JSONArray { [0] = 420, [1] = 420 };

            var headCustomData = new JSONObject { ["coordinates"] = headCoordinates };
            var tailCustomData = new JSONObject { ["coordinates"] = tailCoordinates };

            Settings.Instance.MapVersion = 3;

            var noteA = new BaseNote
            {
                JsonTime = 2f,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down,
                CustomData = headCustomData
            };

            var noteB = new BaseNote
            {
                JsonTime = 3f,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Upper,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Up,
                CustomData = tailCustomData
            };

            noteA = PlaceUtils.Place(noteA);
            noteB = PlaceUtils.Place(noteB);

            SelectionController.Select(noteA);
            SelectionController.Select(noteB, true);

            var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();

            var objects = SelectionController.SelectedObjects.ToList();

            Assert.AreEqual(2, objects.Count);

            Assert.IsTrue(
                ArcPlacement.IsColorNote(objects[0]) && ArcPlacement.IsColorNote(objects[1]),
                "Both selected objects is not color note");
            var n1 = (BaseNote)objects[0];
            var n2 = (BaseNote)objects[1];

            chainPlacement.TryCreateChainData(n1, n2, out var chain, out _);
            var originalChain = BeatmapFactory.Clone(chain);
            chain = PlaceUtils.Place(chain);

            var expected = BeatmapFactory.Clone(originalChain);
            BeatmapAssertion.IsEqual(expected, chain, "Check generated chain");
        }

        [Test]
        public void InvertChain()
        {
            var chainsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

            var chain = new BaseChain
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
                Squish = 1f
            };
            var baselineChain = BeatmapFactory.Clone(chain);
            chain = PlaceUtils.Place(chain);

            var containerA = chainsContainer.LoadedContainers[chain] as ChainContainer;
            Assert.IsNotNull(containerA);
            chain = SliderCommand.InvertColor(containerA.ChainData) as BaseChain;

            BeatmapAssertion.IsEqualWithChanges(
                baselineChain,
                chain,
                c => { c.Color = (int)NoteColor.Blue; },
                "Perform chain inversion");

            // Undo invert
            var undoObjects = PlaceUtils.Undo<BaseChain>().ToList();

            BeatmapAssertion.IsUnchanged(baselineChain, undoObjects[0], "Undo chain inversion");
        }

        [Test]
        public void UpdateChainMultiplier()
        {
            var chainsCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
            var inputController = Object.FindAnyObjectByType<BeatmapChainInputController>();

            var chain = new BaseChain
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
                Squish = 1f
            };
            var baselineChain = BeatmapFactory.Clone(chain);
            chain = PlaceUtils.Place(chain);

            var containerA = chainsCollection.LoadedContainers[chain] as ChainContainer;
            Assert.IsNotNull(containerA);
            inputController.TweakChainSquish(containerA, 0.5f);

            // Hover tweaks are selection-neutral now, so resolve the live replacement from the collection.
            chain = chainsCollection.LoadedObjects.OfType<BaseChain>().Single();

            BeatmapAssertion.IsEqualWithChanges(
                baselineChain,
                chain,
                c => { c.Squish += 0.5f; },
                "Update chain multiplier");

            // Undo invert
            var undoObjects = PlaceUtils.Undo<BaseChain>().ToList();

            BeatmapAssertion.IsUnchanged(baselineChain, undoObjects[0], "Undo update chain multiplier");
        }
    }
}
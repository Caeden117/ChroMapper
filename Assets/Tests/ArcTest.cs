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
    public class ArcTest
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
            TestUtils.CleanupArcs();
        }

        [Test]
        public void CreateArc()
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

            BeatmapObjectContainerCollection arcContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (arcContainerCollection is ArcGridContainer arcsContainer)
            {
                Transform root = arcsContainer.transform.root;
                ArcPlacement arcPlacement = root.GetComponentInChildren<ArcPlacement>();
                
                var objects = SelectionController.SelectedObjects.ToList();
                
                Assert.AreEqual(2, objects.Count);
                
                if(!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                {
                    Assert.Fail("Both selected objects is not color note");
                }
                var n1 = objects[0] as BaseNote;
                var n2 = objects[1] as BaseNote;

                arcPlacement.SpawnArc(n1, n2);
                
                CheckUtils.CheckArc("Check generated arc", arcsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Down, 1, 3f, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.Up, 1, 0);
            }
        }

        [Test]
        public void InvertArc()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (containerCollection is ArcGridContainer arcsContainer)
            {
                Transform root = arcsContainer.transform.root;
                ArcPlacement arcPlacement = root.GetComponentInChildren<ArcPlacement>();
                BeatmapArcInputController inputController = root.GetComponentInChildren<BeatmapArcInputController>();

                BaseArc baseArc = new V3Arc(2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0, 1f, 3f, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);
                PlaceUtils.PlaceArc(arcPlacement, baseArc);

                if (arcsContainer.LoadedContainers[baseArc] is ArcContainer containerA)
                {
                    inputController.InvertArc(containerA);
                }

                CheckUtils.CheckArc("Perform arc inversion", arcsContainer, 0, 2f, (int)NoteColor.Blue, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 3f, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckArc("Undo arc inversion", arcsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 3f, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);
            }
        }

        [Test]
        public void UpdateArcMultiplier()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (containerCollection is ArcGridContainer arcsContainer)
            {
                Transform root = arcsContainer.transform.root;
                ArcPlacement arcPlacement = root.GetComponentInChildren<ArcPlacement>();
                BeatmapArcInputController inputController = root.GetComponentInChildren<BeatmapArcInputController>();

                BaseArc baseArc = new V3Arc(2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0, 1f, 3f, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);
                PlaceUtils.PlaceArc(arcPlacement, baseArc);

                if (arcsContainer.LoadedContainers[baseArc] is ArcContainer containerA)
                {
                    inputController.ChangeMu(containerA, 0.5f);
                }

                CheckUtils.CheckArc("Update arc multiplier", arcsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1.5f, 3f, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);

                if (arcsContainer.LoadedContainers[baseArc] is ArcContainer containerA2)
                {
                    inputController.ChangeTmu(containerA2, 0.5f);
                }

                CheckUtils.CheckArc("Update arc tail multiplier", arcsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1.5f, 3f, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1.5f, 0);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckArc("Undo update arc tail multiplier", arcsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1.5f, 3f, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);

                actionContainer.Undo();

                CheckUtils.CheckArc("Undo update arc multiplier", arcsContainer, 0, 2f, (int)NoteColor.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 3f, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);
            }
        }
    }
}

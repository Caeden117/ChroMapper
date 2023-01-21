using NUnit.Framework;
using System.Collections;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V3;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;

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
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                Transform root = notesContainer.transform.root;
                NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();

                BaseNote baseNoteA = new V3ColorNote(2f, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Down);
                BaseNote baseNoteB = new V3ColorNote(3f, (int)GridX.Left, (int)GridY.Upper, (int)NoteType.Red, (int)NoteCutDirection.Up);

                notePlacement.queuedData = baseNoteA;
                notePlacement.RoundedTime = notePlacement.queuedData.Time;
                notePlacement.ApplyToMap();

                notePlacement.queuedData = baseNoteB;
                notePlacement.RoundedTime = notePlacement.queuedData.Time;
                notePlacement.ApplyToMap();

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
                
                CheckUtils.CheckArc("Check generated arc", arcsContainer, 0, 2f, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Down, 1, 3f, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.Up, 1, 0);
            }
        }
    }
}

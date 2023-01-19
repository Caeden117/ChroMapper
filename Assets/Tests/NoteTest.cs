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
            TestUtils.CleanupNotes();
        }

        public static void CheckNote(string msg, BeatmapObjectContainerCollection container, int idx, int time, int type, int index, int layer, int cutDirection, int angleOffset, JSONNode customData = null)
        {
            BaseObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseNote>(newObjA);
            if (newObjA is BaseNote newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time, 0.001f, $"{msg}: Mismatched time");
                Assert.AreEqual(type, newNoteA.Type, $"{msg}: Mismatched type");
                Assert.AreEqual(index, newNoteA.PosX, $"{msg}: Mismatched position X");
                Assert.AreEqual(layer, newNoteA.PosY, $"{msg}: Mismatched position Y");
                Assert.AreEqual(cutDirection, newNoteA.CutDirection, $"{msg}: Mismatched cut direction");
                Assert.AreEqual(angleOffset, newNoteA.AngleOffset, $"{msg}: Mismatched angle offset");

                if (customData != null)
                {
                    Assert.AreEqual(customData.ToString(), newNoteA.CustomData.ToString(), $"{msg}: Mismatched custom data");
                }
            }
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

                BaseNote baseNoteA = new V3ColorNote(2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left);

                notePlacement.queuedData = baseNoteA;
                notePlacement.RoundedTime = notePlacement.queuedData.Time;
                notePlacement.ApplyToMap();

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                {
                    inputController.InvertNote(containerA);
                }

                CheckNote("Perform note inversion", notesContainer, 0, 2, (int)NoteType.Blue, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);

                // Undo invert
                actionContainer.Undo();

                CheckNote("Undo note inversion", notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
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

                BaseNote baseNoteA = new V3ColorNote(2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left);

                notePlacement.queuedData = baseNoteA;
                notePlacement.RoundedTime = notePlacement.queuedData.Time;
                notePlacement.ApplyToMap();

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                {
                    inputController.UpdateNoteDirection(containerA, true);
                }

                CheckNote("Update note direction", notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.UpLeft, 0);

                // Undo direction
                actionContainer.Undo();

                CheckNote("Undo note direction", notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left, 0);
            }
        }
    }
}

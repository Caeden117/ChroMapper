using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V2;
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

        public static void CheckNote(BeatmapObjectContainerCollection container, int idx, int time, int type, int index, int layer, int cutDirection, JSONNode customData = null)
        {
            IObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<INote>(newObjA);
            if (newObjA is INote newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time);
                Assert.AreEqual(type, newNoteA.Type);
                Assert.AreEqual(index, newNoteA.PosX);
                Assert.AreEqual(layer, newNoteA.PosY);
                Assert.AreEqual(cutDirection, newNoteA.CutDirection);

                if (customData != null)
                {
                    Assert.AreEqual(customData.ToString(), newNoteA.CustomData.ToString());
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

                INote noteA = new V2Note(2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left);

                notePlacement.queuedData = noteA;
                notePlacement.RoundedTime = notePlacement.queuedData.Time;
                notePlacement.ApplyToMap();

                if (notesContainer.LoadedContainers[noteA] is NoteContainer containerA)
                {
                    inputController.InvertNote(containerA);
                }

                CheckNote(notesContainer, 0, 2, (int)NoteType.Blue, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left);

                // Undo invert
                actionContainer.Undo();

                CheckNote(notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left);
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

                INote noteA = new V2Note(2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left);

                notePlacement.queuedData = noteA;
                notePlacement.RoundedTime = notePlacement.queuedData.Time;
                notePlacement.ApplyToMap();

                if (notesContainer.LoadedContainers[noteA] is NoteContainer containerA)
                {
                    inputController.UpdateNoteDirection(containerA, true);
                }

                CheckNote(notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.UpLeft);

                // Undo direction
                actionContainer.Undo();

                CheckNote(notesContainer, 0, 2, (int)NoteType.Red, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.Left);
            }
        }
    }
}

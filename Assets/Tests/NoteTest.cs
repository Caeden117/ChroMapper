using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
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
            BeatmapObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BeatmapNote>(newObjA);
            if (newObjA is BeatmapNote newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time);
                Assert.AreEqual(type, newNoteA.Type);
                Assert.AreEqual(index, newNoteA.LineIndex);
                Assert.AreEqual(layer, newNoteA.LineLayer);
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
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);
            if (containerCollection is NotesContainer notesContainer)
            {
                Transform root = notesContainer.transform.root;
                NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();
                BeatmapNoteInputController inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

                BeatmapNote noteA = new BeatmapNote(2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);

                notePlacement.QueuedData = noteA;
                notePlacement.RoundedTime = notePlacement.QueuedData.Time;
                notePlacement.ApplyToMap();

                if (notesContainer.LoadedContainers[noteA] is BeatmapNoteContainer containerA)
                {
                    inputController.InvertNote(containerA);
                }

                CheckNote(notesContainer, 0, 2, BeatmapNote.NoteTypeB, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);

                // Undo invert
                actionContainer.Undo();

                CheckNote(notesContainer, 0, 2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);
            }
        }

        [Test]
        public void UpdateNoteDirection()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);
            if (containerCollection is NotesContainer notesContainer)
            {
                Transform root = notesContainer.transform.root;
                NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();
                BeatmapNoteInputController inputController = root.GetComponentInChildren<BeatmapNoteInputController>();

                BeatmapNote noteA = new BeatmapNote(2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);

                notePlacement.QueuedData = noteA;
                notePlacement.RoundedTime = notePlacement.QueuedData.Time;
                notePlacement.ApplyToMap();

                if (notesContainer.LoadedContainers[noteA] is BeatmapNoteContainer containerA)
                {
                    inputController.UpdateNoteDirection(containerA, true);
                }

                CheckNote(notesContainer, 0, 2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionUpLeft);

                // Undo direction
                actionContainer.Undo();

                CheckNote(notesContainer, 0, 2, BeatmapNote.NoteTypeA, BeatmapNote.LineIndexFarLeft, BeatmapNote.LineLayerBottom, BeatmapNote.NoteCutDirectionLeft);
            }
        }
    }
}

using NUnit.Framework;
using System.Collections;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BeatmapActionTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void ContainerCleanup()
        {
            TestUtils.CleanupNotes();
        }

        [Test]
        public void ModifiedAction()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);
            var root = notesContainer.transform.root;

            var noteA = new BeatmapNote
            {
                _time = 2,
                _type = BeatmapNote.NOTE_TYPE_A
            };
            notesContainer.SpawnObject(noteA);

            SelectionController.Select(noteA);

            var selectionController = root.GetComponentInChildren<SelectionController>();
            // Default precision is 3dp, but in editor it's 6dp so check 7dp
            selectionController.MoveSelection(-0.0000001f);

            actionContainer.Undo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(2, notesContainer.UnsortedObjects[0]._time);

            actionContainer.Redo();

            Assert.AreEqual(1, notesContainer.LoadedObjects.Count);
            Assert.AreEqual(1.9999999f, notesContainer.UnsortedObjects[0]._time);
        }
    }
}

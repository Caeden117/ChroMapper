using NUnit.Framework;
using System.Collections;
using System.Linq;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BPMTest
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
            TestUtils.CleanupBPMChanges();
        }

        private static void CheckBPM(BeatmapObjectContainerCollection container, int idx, int time, int bpm)
        {
            BeatmapObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BeatmapBPMChange>(newObjA);
            if (newObjA is BeatmapBPMChange newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time);
                Assert.AreEqual(bpm, newNoteA.Bpm);
            }
        }

        [Test]
        public void ModifyEvent()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.BpmChange);
            if (collection is BPMChangesContainer bpmCollection)
            {
                BeatmapBPMChange bpmChange = new BeatmapBPMChange(50, 10);
                bpmCollection.SpawnObject(bpmChange);

                if (bpmCollection.LoadedContainers[bpmChange] is BeatmapBPMChangeContainer container)
                {
                    BeatmapBPMChangeInputController.ChangeBpm(container, "60");
                }

                Assert.AreEqual(1, bpmCollection.LoadedObjects.Count);
                CheckBPM(bpmCollection, 0, 10, 60);

                actionContainer.Undo();

                Assert.AreEqual(1, bpmCollection.LoadedObjects.Count);
                CheckBPM(bpmCollection, 0, 10, 50);

                actionContainer.Redo();

                Assert.AreEqual(1, bpmCollection.LoadedObjects.Count);
                CheckBPM(bpmCollection, 0, 10, 60);
            }
        }
    }
}

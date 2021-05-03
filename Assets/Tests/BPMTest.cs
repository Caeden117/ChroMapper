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
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupBPMChanges();
        }

        private static void CheckBPM(BeatmapObjectContainerCollection container, int idx, int time, int bpm)
        {
            var newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BeatmapBPMChange>(newObjA);
            if (newObjA is BeatmapBPMChange newNoteA)
            {
                Assert.AreEqual(time, newNoteA._time);
                Assert.AreEqual(bpm, newNoteA._BPM);
            }
        }
        
        [Test]
        public void ModifyEvent()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.BPM_CHANGE);
            if (collection is BPMChangesContainer bpmCollection)
            {
                var bpmChange = new BeatmapBPMChange(50, 10);
                bpmCollection.SpawnObject(bpmChange);

                if (bpmCollection.LoadedContainers[bpmChange] is BeatmapBPMChangeContainer container)
                {
                    BeatmapBPMChangeInputController.ChangeBPM(container, "60");
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

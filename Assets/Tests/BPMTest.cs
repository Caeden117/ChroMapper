using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3.Customs;
using NUnit.Framework;
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
            return TestUtils.LoadMap(3);
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupBPMChanges();
        }

        private static void CheckBPM(string msg, BeatmapObjectContainerCollection container, int idx, int time, int bpm)
        {
            var newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseBpmEvent>(newObjA);
            if (newObjA is BaseBpmEvent newNoteA)
            {
                Assert.AreEqual(time, newNoteA.JsonTime, $"{msg}: Mismatched time");
                Assert.AreEqual(bpm, newNoteA.Bpm, $"{msg}: Mismatched BPM");
            }
        }

        [Test]
        public void ModifyEvent()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.BpmChange);
            if (collection is BPMChangeGridContainer bpmCollection)
            {
                BaseBpmEvent baseBpmChange = new V3BpmChange(10, 50);
                bpmCollection.SpawnObject(baseBpmChange);

                if (bpmCollection.LoadedContainers[baseBpmChange] is BpmEventContainer container)
                    BeatmapBPMChangeInputController.ChangeBpm(container, "60");

                Assert.AreEqual(1, bpmCollection.LoadedObjects.Count);
                CheckBPM("Update BPM change", bpmCollection, 0, 10, 60);

                actionContainer.Undo();

                Assert.AreEqual(1, bpmCollection.LoadedObjects.Count);
                CheckBPM("Undo BPM change", bpmCollection, 0, 10, 50);

                actionContainer.Redo();

                Assert.AreEqual(1, bpmCollection.LoadedObjects.Count);
                CheckBPM("Redo BPM change", bpmCollection, 0, 10, 60);
            }
        }
    }
}
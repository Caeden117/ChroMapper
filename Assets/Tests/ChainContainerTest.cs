using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ChainContainerTest
    {
        ChainGridContainer chainsCollection;
        BaseChain placedChain;

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

        [SetUp]
        public void PlaceChain()
        {
            chainsCollection = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();

            var root = chainsCollection.transform.root;
            var chainPlacement = root.GetComponentInChildren<ChainPlacement>();
            var inputController = root.GetComponentInChildren<BeatmapChainInputController>();

            placedChain = new V3Chain
            {
                JsonTime = 0,
                TailJsonTime = 2,
                SliceCount = 5,
            };
            PlaceUtils.PlaceChain(chainPlacement, placedChain);

            chainsCollection.LoadedContainers.TryGetValue(placedChain, out var chainContainer);

            // Chain links
            var links = chainContainer.GetComponentsInChildren<ChainComponentsFetcher>();
            var linkTransforms = links.Select(x => x.GetComponent<Transform>()).OrderBy(t => t.transform.position.z).ToList();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupChains();
            CleanupUtils.CleanupBPMChanges();
        }

        [Test]
        public void ScalesWithBpmEventsCorrectly()
        {
            if (!chainsCollection.LoadedContainers.TryGetValue(placedChain, out var chainContainer))
            {
                Assert.Fail("Chain container not found");
            }

            // Chain links
            var links = chainContainer.GetComponentsInChildren<ChainComponentsFetcher>();
            var linkTransforms = links.Select(x => x.GetComponent<Transform>()).OrderBy(t => t.transform.position.z).ToList();

            var bpmCollection = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);
            bpmCollection.SpawnObject(new V3BpmEvent { JsonTime = 0, Bpm = 100 });

            var firstZ = chainContainer.transform.position.z;
            var lastZ = linkTransforms.Last().position.z;
            var originalZDistance = lastZ - firstZ;

            // Test each link is equidistant in space
            for (var i = 0; i < linkTransforms.Count; i++)
            {
                Assert.AreEqual(firstZ + ((i + 1.0) / linkTransforms.Count * originalZDistance), linkTransforms[i].transform.position.z, 0.001f);
            }

            // Chain should now be 3/4 of its original length and each link should remain equidistant in space
            bpmCollection.SpawnObject(new V3BpmEvent { JsonTime = 1, Bpm = 200 });
            for (var i = 0; i < linkTransforms.Count; i++)
            {
                Assert.AreEqual(firstZ + ((3f / 4f) * (i + 1.0) / linkTransforms.Count * originalZDistance), linkTransforms[i].transform.position.z, 0.001f);
            }
        }
    }
}
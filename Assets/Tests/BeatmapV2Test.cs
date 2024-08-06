using System;
using System.Collections;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine.TestTools;

namespace Tests
{
    public class BeatmapV2Test
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(2);
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
            CleanupUtils.CleanupNotes();
            CleanupUtils.CleanupObstacles();
            CleanupUtils.CleanupEvents();
            CleanupUtils.CleanupArcs();
            CleanupUtils.CleanupChains();
        }

        [Test]
        public void IsV2Map()
        {
            Assert.IsInstanceOf<V2Difficulty>(BeatSaberSongContainer.Instance.Map,
                "Beatmap instance version should be v2");
            Assert.IsFalse(Settings.Instance.MapVersion == 3,
                "Settings Load Beatmap V3 should be false, otherwise may cause unnecessary issue");
        }
    }
}
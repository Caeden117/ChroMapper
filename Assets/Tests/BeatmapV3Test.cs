using System;
using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine.TestTools;

namespace Tests
{
    public class BeatmapV3Test
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
            CleanupUtils.CleanupNotes();
            CleanupUtils.CleanupObstacles();
            CleanupUtils.CleanupEvents();
            CleanupUtils.CleanupArcs();
            CleanupUtils.CleanupChains();
        }

        [Test]
        public void IsV3Map()
        {
            Assert.IsInstanceOf<V3Difficulty>(BeatSaberSongContainer.Instance.Map,
                "Beatmap instance version should be v3");
            Assert.IsTrue(Settings.Instance.MapVersion == 3,
                "Settings Load Beatmap V3 should be true, otherwise may cause unnecessary issue");
        }
    }
}
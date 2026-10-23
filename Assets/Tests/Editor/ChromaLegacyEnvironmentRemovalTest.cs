using System.Collections;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaLegacyEnvironmentRemovalTest : TestBase
    {
        [UnityTest]
        public IEnumerator LegacyTrackLaneRingRemovalPreservesBigRings()
        {
            yield return Load("TrackLaneRing", false);
            var markers = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.ChromaIDMarkers;
            var small = markers.Where(marker => marker.name == "SmallTrackLaneRing(Clone)").ToArray();
            var big = markers.Where(marker => marker.name == "BigTrackLaneRing(Clone)").ToArray();
            Assert.That(small, Is.Not.Empty, "The native legacy request requires the small-ring family.");
            Assert.That(big, Is.Not.Empty, "The exclusion control requires the big-ring family.");
            Assert.That(small.All(marker => !marker.gameObject.activeSelf), Is.True);
            Assert.That(
                big.All(marker => marker.gameObject.activeSelf),
                Is.True,
                "Native TrackLaneRing removal excludes names containing Big.");
        }

        [UnityTest]
        public IEnumerator PresentLegacyRemovalSkipsEnhancementInstructions()
        {
            yield return Load("NoSuchLegacyObject", true);
            var big = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.ChromaIDMarkers
                .Where(marker => marker.name == "BigTrackLaneRing(Clone)").ToArray();
            Assert.That(big, Is.Not.Empty);
            Assert.That(
                big.All(marker => marker.gameObject.activeSelf),
                Is.True,
                "Native legacy removal returns before processing enhancements, even when its request matches nothing.");
        }

        private static IEnumerator Load(string removal, bool includeEnhancement)
        {
            var info = new InfoDifficulty(new InfoDifficultySet());
            var removals = new JSONArray();
            removals.Add(removal);
            info.CustomData["_environmentRemoval"] = removals;
            var enhancements = new JSONArray();
            if (includeEnhancement)
                enhancements.Add(
                    new JSONObject
                    {
                        ["_id"] = "BigTrackLaneRing(Clone)", ["_lookupMethod"] = "EndsWith", ["_active"] = false
                    });

            var data = new JSONObject
            {
                ["_version"] = "2.6.0",
                ["_customData"] = new JSONObject { ["_environment"] = enhancements }
            };
            yield return TestUtils.ReloadMap(2, data, environmentName: "DefaultEnvironment", difficultyInfo: info);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

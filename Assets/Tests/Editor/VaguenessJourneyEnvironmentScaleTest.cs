using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "Vagueness & JOURNEY", mapped by Kival Evan (BeatSaver ID: 1d28c).
    // The fixture retains all 138 Expert+ Standard V2 environment enhancements in their
    // original authored order and every customData section. A later CM save scrambled the
    // installed map's enhancement order, moving the left source scale behind 27 clones.
    // Notes, walls, and events after beat four are trimmed; none change initial ring scale.
    public class VaguenessJourneyEnvironmentScaleTest : TestBase
    {
        private const string LeftRootId =
            "BTSEnvironment.[0]Environment.[14]PillarTrackLaneRingsR (1)(Clone)";
        private const string RightRootId =
            "BTSEnvironment.[0]Environment.[13]PillarTrackLaneRingsR(Clone)";

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "VaguenessJourneyExpertPlusEnvironmentFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 185,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 745);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        protected override void CleanupTestObjects()
        {
        }

        // Every clone uses the same native ring cube mesh. In the reported game view the
        // left and right copies have the same horizontal cross-section after enhancement.
        [UnityTest]
        public IEnumerator LeftClonedRingCubesKeepTheSameHorizontalScaleAsRightClones()
        {
            yield return null;

            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var left = descriptor.ChromaIDMarkers
                .Where(marker => marker.ChromaID == LeftRootId)
                .Select(marker => marker.transform)
                .ToList();
            var right = descriptor.ChromaIDMarkers
                .Where(marker => marker.ChromaID == RightRootId)
                .Select(marker => marker.transform)
                .ToList();

            Assert.That(left, Has.Count.EqualTo(52), "The Expert+ fixture did not create every left clone.");
            Assert.That(right, Has.Count.EqualTo(52), "The Expert+ fixture did not create every right clone.");
            Assert.That(right.Count(transform =>
                Mathf.Abs(transform.localScale.x - 0.25f) > 0.001f
                || Mathf.Abs(transform.localScale.y - 0.25f) > 0.001f), Is.Zero,
                "The right-side control clones did not all retain the authored narrow cross-section.");

            var oversizedLeft = left
                .Where(transform =>
                    Mathf.Abs(transform.localScale.x - 0.25f) > 0.001f
                    || Mathf.Abs(transform.localScale.y - 0.25f) > 0.001f)
                .ToList();
            var examples = string.Join(", ", oversizedLeft.Take(4)
                .Select(transform => $"{transform.position} scale {transform.localScale}"));
            Assert.That(oversizedLeft.Count, Is.Zero,
                $"Left ring cubes have a wider/deeper parent scale than right cubes. " +
                $"{oversizedLeft.Count} of {left.Count} are oversized; examples: {examples}.");
        }

        // The original authored file scaled both source rings before any duplicates; this
        // catches another save/import reorder that would recreate the 27 oversized cubes.
        [Test]
        public void ExpertPlusFixtureKeepsBothSourceScalesBeforeEveryClone()
        {
            var root = JSON.Parse(File.ReadAllText(FixturePath));
            var enhancements = root["_customData"]["_environment"].AsArray;
            Assert.That(enhancements.Count, Is.EqualTo(138));
            Assert.That(enhancements[0]["_id"].Value, Is.EqualTo(@"\[\d+\]PillarTrackLaneRingsR$"));
            Assert.That(enhancements[0]["_scale"][0].AsFloat, Is.EqualTo(0.25f));
            Assert.That(enhancements[1]["_id"].Value,
                Is.EqualTo(@"\[\d+\]PillarTrackLaneRingsR.?\(1\)$"));
            Assert.That(enhancements[1]["_scale"][0].AsFloat, Is.EqualTo(0.25f));
            Assert.That(root["_customData"]["_customEvents"].AsArray.Count, Is.EqualTo(16));
        }

        // The reported map was saved with its scale entry after 27 left duplicates. Its
        // unchanged authored instruction sequence must survive a V2 parse/save round trip.
        [Test]
        public void SavingExpertPlusEnvironmentRetainsSequentialCloneOrder()
        {
            var input = JSON.Parse(File.ReadAllText(FixturePath));
            // V2 parsing removes extracted customData arrays from its JSON argument, so
            // keep the untouched fixture tree as the authored-order comparison.
            var map = V2Difficulty.GetFromJson(input.Clone(), "VaguenessJourneyExpertPlus");

            var output = V2Difficulty.GetOutputJson(map);
            Assert.That(output, Is.Not.Null);
            var authored = input["_customData"]["_environment"].AsArray;
            var saved = output["_customData"]["_environment"].AsArray;
            Assert.That(saved.Count, Is.EqualTo(authored.Count));
            for (var i = 0; i < authored.Count; i++)
            {
                Assert.That(saved[i]["_id"].Value, Is.EqualTo(authored[i]["_id"].Value),
                    $"Enhancement {i} changed lookup order on save.");
                Assert.That(saved[i].HasKey("_position"), Is.EqualTo(authored[i].HasKey("_position")),
                    $"Enhancement {i} changed the presence of its position on save.");
                if (authored[i].HasKey("_position"))
                {
                    Assert.That(saved[i]["_position"][0].AsFloat,
                        Is.EqualTo(authored[i]["_position"][0].AsFloat).Within(0.001f),
                        $"Enhancement {i} changed clone/source order on save.");
                }
            }
        }

        // Environment arrays are ordered instructions rather than timed objects. A V2 save
        // must write the supplied list order instead of sorting by inherited BaseObject
        // file-order metadata and silently changing the command sequence.
        [Test]
        public void SavingV2EnvironmentEnhancementsKeepsCurrentInstructionOrder()
        {
            var map = V2Difficulty.GetFromJson(
                JSON.Parse(File.ReadAllText(FixturePath)), "VaguenessJourneyExpertPlus");
            map.EnvironmentEnhancements.Reverse();

            var output = V2Difficulty.GetOutputJson(map);
            var saved = output["_customData"]["_environment"].AsArray;
            Assert.That(saved.Count, Is.EqualTo(138));
            Assert.That(saved[137]["_id"].Value, Is.EqualTo(@"\[\d+\]PillarTrackLaneRingsR$"));
            Assert.That(saved[136]["_id"].Value,
                Is.EqualTo(@"\[\d+\]PillarTrackLaneRingsR.?\(1\)$"));
            Assert.That(saved[136]["_scale"][0].AsFloat, Is.EqualTo(0.25f));
        }

        // V3's environment field has the same Chroma instruction-order contract as V2;
        // saving a changed list must not sort it by inherited beat or file-order metadata.
        [Test]
        public void SavingV3EnvironmentEnhancementsKeepsCurrentInstructionOrder()
        {
            var previousVersion = Settings.Instance.MapVersion;
            Settings.Instance.MapVersion = 3;
            try
            {
                var map = V3Difficulty.GetFromJson(JSON.Parse(@"{
                    ""version"": ""3.3.0"",
                    ""customData"": {
                        ""environment"": [
                            { ""id"": ""first"", ""lookupMethod"": ""Exact"" },
                            { ""id"": ""second"", ""lookupMethod"": ""Exact"" }
                        ]
                    }
                }"), "V3EnvironmentOrder");
                map.EnvironmentEnhancements.Reverse();

                var output = V3Difficulty.GetOutputJson(map);
                var saved = output["customData"]["environment"].AsArray;
                Assert.That(saved.Count, Is.EqualTo(2));
                Assert.That(saved[0]["id"].Value, Is.EqualTo("second"));
                Assert.That(saved[1]["id"].Value, Is.EqualTo("first"));
            }
            finally
            {
                Settings.Instance.MapVersion = previousVersion;
            }
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

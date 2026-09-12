using System.Linq;
using System.Text.RegularExpressions;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestsEditMode
{
    // Cover legacy SongCore removal migration so valid string IDs cannot regress into null JSON values during a save.
    public class DifficultySettingsTest
    {
        [SetUp]
        public void Setup()
        {
            Settings.Instance.MapVersion = 2;
        }

        // Legacy Info.dat string removals must become disabled Contains enhancements and remain serializable as Chroma environment data.
        [Test]
        public void LegacyStringRemovalsSerializeAsEnvironmentEnhancements()
        {
            var environmentRemovals = new JSONArray
            {
                [0] = "Construction",
                [1] = "TrackMirror"
            };

            var enhancements = DifficultySettings
                .GetEnvironmentEnhancementsFromLegacyRemovals(environmentRemovals, 2, "ExpertPlus")
                .ToList();

            Assert.That(enhancements, Has.Count.EqualTo(2));
            Assert.That(enhancements.Select(x => x.ID), Is.EqualTo(new[] { "Construction", "TrackMirror" }));
            Assert.That(enhancements.All(x => x.LookupMethod == EnvironmentLookupMethod.Contains), Is.True);
            Assert.That(enhancements.All(x => !x.Active.AsBool), Is.True);

            var output = V2Difficulty.GetOutputJson(new BaseDifficulty
            {
                Version = "2.6.0",
                EnvironmentEnhancements = enhancements
            });

            Assert.That(output["_customData"]["_environment"], Has.Count.EqualTo(2));
            Assert.That(output["_customData"]["_environment"][0]["_id"].Value, Is.EqualTo("Construction"));
            Assert.That(output["_customData"]["_environment"][1]["_id"].Value, Is.EqualTo("TrackMirror"));
        }

        // Existing object-form removals must stay supported while the legacy-string migration path is added.
        [Test]
        public void ObjectRemovalRetainsItsChromaProperties()
        {
            var environmentRemovals = new JSONArray
            {
                [0] = new JSONObject
                {
                    ["_id"] = "TrackMirror",
                    ["_lookupMethod"] = "Exact",
                    ["_active"] = false
                }
            };

            var enhancement = DifficultySettings
                .GetEnvironmentEnhancementsFromLegacyRemovals(environmentRemovals, 2, "ExpertPlus")
                .Single();

            Assert.That(enhancement.ID, Is.EqualTo("TrackMirror"));
            Assert.That(enhancement.LookupMethod, Is.EqualTo(EnvironmentLookupMethod.Exact));
            Assert.That(enhancement.Active.AsBool, Is.False);
        }

        // Malformed removals must not reach serialization; the error must identify the entry and point users to valid syntax.
        [Test]
        public void InvalidRemovalIsSkippedWithActionableError()
        {
            var environmentRemovals = new JSONArray { [0] = 123 };
            LogAssert.Expect(
                LogType.Error,
                new Regex("\\[Environment Removal\\] Skipping invalid _environmentRemoval entry 0 in difficulty 'ExpertPlus'.*environment/environment/"));

            var enhancements = DifficultySettings
                .GetEnvironmentEnhancementsFromLegacyRemovals(environmentRemovals, 2, "ExpertPlus")
                .ToList();

            Assert.That(enhancements, Is.Empty);
        }
    }
}

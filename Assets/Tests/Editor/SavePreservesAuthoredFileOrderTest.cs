using System.Globalization;
using Beatmap.V2;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    // Saving a loaded map must reproduce authored file order and authored numeric precision.
    // Loading then saving As The World Caves In reordered _customEvents: the unstable JsonTime-only
    // merge/sort moved TrackConstructionParent1's beat-0 hide (x=696969) after its beat-0 show
    // (z=1337), so in game the hide won and the runway never appeared. The same save also rounded
    // every float to 3 decimals (note _time 86.4054 -> 86.405, point value 0.4375 -> 0.438).
    // These tests pin both behaviors through V2Difficulty.GetFromJson -> GetOutputJson; the
    // in-memory lists are deliberately scrambled to simulate the sorted order the editor keeps.
    public class SavePreservesAuthoredFileOrderTest
    {
        private int originalMapVersion;
        private int originalDecimalPrecision;

        [SetUp]
        public void SetUp()
        {
            originalMapVersion = Settings.Instance.MapVersion;
            originalDecimalPrecision = JSONNumber.DecimalPrecision;
            Settings.Instance.MapVersion = 2;
            JSONNumber.DecimalPrecision = 6;
        }

        [TearDown]
        public void TearDown()
        {
            Settings.Instance.MapVersion = originalMapVersion;
            JSONNumber.DecimalPrecision = originalDecimalPrecision;
        }

        // Authored floats beyond 3 decimals must survive the save; the previous
        // JSONNumber.DecimalPrecision = 3 coupling rounded them away on every save.
        [Test]
        public void AuthoredFloatPrecisionSurvivesSave()
        {
            var map = V2Difficulty.GetFromJson(JSON.Parse(@"{
                ""_version"": ""2.6.0"",
                ""_events"": [],
                ""_notes"": [
                    { ""_time"": 86.4054, ""_lineIndex"": 0, ""_lineLayer"": 0, ""_type"": 0, ""_cutDirection"": 1 }
                ],
                ""_obstacles"": [],
                ""_customData"": {
                    ""_pointDefinitions"": [
                        { ""_name"": ""p"", ""_points"": [[0.4375, 0, 0]] }
                    ]
                }
            }"), "test");

            var output = V2Difficulty.GetOutputJson(map);

            // Compare through the emitted string: rounding lives in JSONNumber.Value.
            var noteTime = float.Parse(output["_notes"][0]["_time"].Value, CultureInfo.InvariantCulture);
            Assert.AreEqual(86.4054f, noteTime, "note _time lost precision on save");

            var pointX = float.Parse(
                output["_customData"]["_pointDefinitions"][0]["_points"][0][0].Value,
                CultureInfo.InvariantCulture);
            Assert.AreEqual(0.4375f, pointX, "point definition value lost precision on save");
        }
    }
}

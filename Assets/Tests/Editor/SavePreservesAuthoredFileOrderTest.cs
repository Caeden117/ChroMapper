using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
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
        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "WorldCavesInEnvironmentEssence.json");

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

        // The reported corruption: the fixture's beat-0 hide must stay before its beat-0 show on
        // TrackConstructionParent1 even after the in-memory list is scrambled.
        [Test]
        public void CustomEventsSaveInAuthoredFileOrderAfterInMemoryScramble()
        {
            var map = V2Difficulty.GetFromJson(JSON.Parse(File.ReadAllText(FixturePath)), "test");
            map.CustomEvents.Reverse();

            var output = V2Difficulty.GetOutputJson(map);
            var runwayXs = output["_customData"]["_customEvents"].AsArray.Children
                .Where(n => n["_type"].Value == "AnimateTrack"
                    && n["_data"]["_track"].Value == "TrackConstructionParent1"
                    && n["_time"].AsFloat == 0f
                    && n["_data"]["_position"] is JSONArray)
                .Select(n => n["_data"]["_position"].AsArray[0].AsArray[0].AsFloat)
                .ToList();

            Assert.AreEqual(
                new List<float> { 696969f, 0f },
                runwayXs,
                "Beat-0 hide (x=696969) must precede beat-0 show (x=0) exactly as authored; "
                    + "Chroma applies equal-time events last-in-file");
        }

        // Same-time events whose CompareTo keys differ (here: _value) get scrambled by the
        // in-memory sort at parse; the save must still emit the authored order.
        [Test]
        public void SameTimeEventsSaveInAuthoredFileOrder()
        {
            var map = V2Difficulty.GetFromJson(JSON.Parse(@"{
                ""_version"": ""2.6.0"",
                ""_events"": [
                    { ""_time"": 5, ""_type"": 8, ""_value"": 2, ""_floatValue"": 1 },
                    { ""_time"": 5, ""_type"": 8, ""_value"": 0, ""_floatValue"": 1 },
                    { ""_time"": 5, ""_type"": 8, ""_value"": 7, ""_floatValue"": 1 }
                ],
                ""_notes"": [],
                ""_obstacles"": []
            }"), "test");

            var output = V2Difficulty.GetOutputJson(map);
            var values = output["_events"].AsArray.Children
                .Where(n => n["_type"].AsInt == 8 && n["_time"].AsFloat == 5f)
                .Select(n => n["_value"].AsInt)
                .ToList();

            Assert.AreEqual(new List<int> { 2, 0, 7 }, values);
        }

        // Objects spawned in the editor (no file position) must still serialize, landing at the
        // tail of their same-time group rather than vanishing or corrupting order.
        [Test]
        public void SpawnedCustomEventsAppendAtEndOfTheirTimeGroup()
        {
            var map = V2Difficulty.GetFromJson(JSON.Parse(@"{
                ""_version"": ""2.6.0"",
                ""_events"": [],
                ""_notes"": [],
                ""_obstacles"": [],
                ""_customData"": {
                    ""_customEvents"": [
                        { ""_time"": 0, ""_type"": ""AnimateTrack"", ""_data"": { ""_track"": ""T"", ""_position"": [[1, 0, 0, 0]], ""_duration"": 0 } }
                    ]
                }
            }"), "test");

            map.CustomEvents.Add(new BaseCustomEvent
            {
                JsonTime = 0f,
                Type = "AnimateTrack",
                Data = JSON.Parse(@"{ ""_track"": ""T"", ""_position"": [[2, 0, 0, 0]], ""_duration"": 0 }")
            });

            var output = V2Difficulty.GetOutputJson(map);
            var positions = output["_customData"]["_customEvents"].AsArray.Children
                .Select(n => n["_data"]["_position"].AsArray[0].AsArray[0].AsFloat)
                .ToList();

            Assert.AreEqual(new List<float> { 1f, 2f }, positions);
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

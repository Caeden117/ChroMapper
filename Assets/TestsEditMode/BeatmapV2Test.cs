using System;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class BeatmapV2Test
    {
        private const string fileJson = @"
{
    ""_version"": ""2.6.0"",
    ""_notes"": [
        {
            ""_time"": 10,
            ""_lineIndex"": 1,
            ""_lineLayer"": 0,
            ""_type"": 0,
            ""_cutDirection"": 1
        },
        {
            ""_time"": 10,
            ""_lineIndex"": 1,
            ""_lineLayer"": 0,
            ""_type"": 3,
            ""_cutDirection"": 0
        }
    ],
    ""_obstacles"": [
        {
            ""_type"": 1,
            ""_time"": 10,
            ""_duration"": 5,
            ""_lineIndex"": 1,
            ""_width"": 1
        },
        {
            ""_type"": 0,
            ""_time"": 10,
            ""_duration"": 5,
            ""_lineIndex"": 2,
            ""_width"": 1
        }
    ],
    ""_sliders"": [
        {
            ""_colorType"": 1,
            ""_headTime"": 10,
            ""_headLineIndex"": 1,
            ""_headLineLayer"": 0,
            ""_headCutDirection"": 1,
            ""_headControlPointLengthMultiplier"": 1,
            ""_tailTime"": 20,
            ""_tailLineIndex"": 2,
            ""_tailLineLayer"": 2,
            ""_tailCutDirection"": 0,
            ""_tailControlPointLengthMultiplier"": 1,
            ""_sliderMidAnchorMode"": 0
        }
     ],
    ""_events"": [
        {
            ""_time"": 10,
            ""_type"": 1,
            ""_value"": 3,
            ""_floatValue"": 1
        },
        {
            ""_time"": 10,
            ""_type"": 5,
            ""_value"": 1,
            ""_floatValue"": 0
        },
        {
            ""_time"": 10,
            ""_type"": 14,
            ""_value"": 4,
            ""_floatValue"": 0
        },
        {
            ""_time"": 15,
            ""_type"": 15,
            ""_value"": 4,
            ""_floatValue"": 0
        },
        {
            ""_time"": 10,
            ""_type"": 100,
            ""_value"": 0,
            ""_floatValue"": 128
        }
    ],
    ""_waypoints"": [
        {
            ""_time"": 10,
            ""_lineIndex"": 1,
            ""_lineLayer"": 0,
            ""_offsetDirection"": 1
        }
    ],
    ""_specialEventsKeywordFilters"": {
        ""_keywords"": [
            {
                ""_keyword"": ""SECRET"",
                ""_specialEvents"": [
                    40,
                    41,
                    42,
                    43
                ]
            }
        ]
    }
}";


        // For use in PlayMode
        public void TestEverything()
        {
        }

        [SetUp]
        public void Setup()
        {
            Settings.Instance.MapVersion = 2;
        }

        [Test]
        public void GetFromJson()
        {
            var difficulty = V2Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");
            
            Assert.AreEqual("2.6.0",difficulty.Version);
            AssertDifficulty(difficulty);
        }

        [Test]
        public void GetOutputJson()
        {
            var difficulty = V2Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");
            var outputJson = V2Difficulty.GetOutputJson(difficulty);
            var reparsed = V2Difficulty.GetFromJson(outputJson, "");
            
            reparsed.BpmEvents.RemoveAt(0); // Remove inserted bpm
            
            AssertDifficulty(reparsed); // This should have the same stuff
        }
        
        [Test]
        public void GetOutputJsonAfterSwitchingToV3()
        {
            var difficulty = V2Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");

            Settings.Instance.MapVersion = 3;
            var outputJson = V3Difficulty.GetOutputJson(difficulty);
            var reparsed = V3Difficulty.GetFromJson(outputJson, "");
            
            reparsed.BpmEvents.RemoveAt(0); // Remove inserted bpm

            AssertDifficulty(reparsed); // This should have the same stuff
        }

        private static void AssertDifficulty(BaseDifficulty difficulty)
        {
            Assert.AreEqual(2, difficulty.Notes.Count);
            BeatmapAssert.NotePropertiesAreEqual(difficulty.Notes[0], 10, 1, 0, 0, 1, 0);
            BeatmapAssert.NotePropertiesAreEqual(difficulty.Notes[1], 10, 1, 0, 3, 0, 0);
            
            Assert.AreEqual(2, difficulty.Obstacles.Count);
            BeatmapAssert.ObstaclePropertiesAreEqual(difficulty.Obstacles[0], 10, 1, 2, 1, 1, 3, 5);
            BeatmapAssert.ObstaclePropertiesAreEqual(difficulty.Obstacles[1], 10, 2, 0, 0, 1, 5, 5);
            
            Assert.AreEqual(0, difficulty.Arcs.Count); // We do not load arcs from v2
            
            Assert.AreEqual(1, difficulty.BpmEvents.Count);
            BeatmapAssert.BpmEventPropertiesAreEqual(difficulty.BpmEvents[0], 10, 128);
            
            Assert.AreEqual(4, difficulty.Events.Count);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[0], 10, 1, 3, 1);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[1], 10, 5, 1, 0);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[2], 10, 14, 4, 0);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[3], 15, 15, 4, 0);
        }
    }
}
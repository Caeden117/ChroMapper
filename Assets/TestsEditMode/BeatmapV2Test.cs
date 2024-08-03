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
        private const float epsilon = 0.001f;
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
            ""_type"": 2,
            ""_time"": 10,
            ""_duration"": 5,
            ""_lineIndex"": 1,
            ""_lineLayer"": 0,
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
            
            Assert.AreEqual(2, difficulty.Notes.Count);
            AssertNoteProperties(difficulty.Notes[0], 10, 1, 0, 0, 1, 0);
            AssertNoteProperties(difficulty.Notes[1], 10, 1, 0, 3, 0, 0);
            
            Assert.AreEqual(1, difficulty.Obstacles.Count);
            AssertObstacleProperties(difficulty.Obstacles[0], 10, 1, 0, 2, 1, 5, 5);
            
            Assert.AreEqual(0, difficulty.Arcs.Count); // We do not load arcs from v2
            
            Assert.AreEqual(1, difficulty.BpmEvents.Count);
            AssertBpmEventProperties(difficulty.BpmEvents[0], 10, 128);
            
            Assert.AreEqual(4, difficulty.Events.Count);
            AssertEventProperties(difficulty.Events[0], 10, 1, 3, 1);
            AssertEventProperties(difficulty.Events[1], 10, 5, 1, 0);
            AssertEventProperties(difficulty.Events[2], 10, 14, 4, 0);
            AssertEventProperties(difficulty.Events[3], 15, 15, 4, 0);
        }

        private static void AssertNoteProperties(BaseNote note, float jsonTime, int x, int y, int type, int cutDirection, int angleOffset)
        {
            Assert.AreEqual(jsonTime, note.JsonTime, epsilon);
            Assert.AreEqual(x, note.PosX);
            Assert.AreEqual(y, note.PosY);
            Assert.AreEqual(type, note.Type);
            Assert.AreEqual(cutDirection, note.CutDirection);
            Assert.AreEqual(angleOffset, note.AngleOffset);
        }

        private static void AssertObstacleProperties(BaseObstacle obstacle, float jsonTime, int x, int y, int type, int width,
            int height, float duration)
        {
            Assert.AreEqual(jsonTime, obstacle.JsonTime, epsilon);
            Assert.AreEqual(x, obstacle.PosX);
            Assert.AreEqual(y, obstacle.PosY);
            Assert.AreEqual(type, obstacle.Type);
            Assert.AreEqual(width, obstacle.Width);
            Assert.AreEqual(height, obstacle.Height);
            Assert.AreEqual(duration, obstacle.Duration, epsilon);
            
        }

        private static void AssertBpmEventProperties(BaseBpmEvent bpmEvent, float jsonTime, float floatValue)
        {
            Assert.AreEqual(jsonTime, bpmEvent.JsonTime, epsilon);
            Assert.AreEqual((int)EventTypeValue.BpmChange, bpmEvent.Type);
            Assert.AreEqual(floatValue, bpmEvent.FloatValue, epsilon);
        }
        
        private static void AssertEventProperties(BaseEvent evt, float jsonTime, int type, int value, float floatValue)
        {
            Assert.AreEqual(jsonTime, evt.JsonTime, epsilon);
            Assert.AreEqual(type, evt.Type);
            Assert.AreEqual(value, evt.Value);
            Assert.AreEqual(floatValue, evt.FloatValue);
        }
    }
}
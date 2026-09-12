using System;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

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
    },
    ""_customData"": 
    {
        ""_foo"": ""_bar"",
        ""_time"": 123.456
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
        
        [Test]
        public void RootCustomDataPropertiesPersist()
        {
            var difficulty = V2Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");
            Assert.AreEqual("_bar", difficulty.CustomData["_foo"].Value);
            
            Assert.IsFalse(difficulty.CustomData.HasKey("_time"));
            Assert.AreEqual(123.456f, difficulty.Time, 0.001);

            var output = V2Difficulty.GetOutputJson(difficulty);
            Assert.AreEqual("_bar", output["_customData"]["_foo"].Value);
            Assert.AreEqual(123.456f, output["_customData"]["_time"].AsFloat, 0.001);
        }

        // The reported map and node editor contain the same V2 gradient payload, so preserve omitted opaque alpha and
        // explicit zero alpha through production parsing and serialization before preview-cache tests consume the event.
        [Test]
        public void LegacyAlphaZeroLightGradientRoundTripsWithoutConversion()
        {
            var gradientJson = JSONNode.Parse(@"
            {
                ""_time"": 32,
                ""_type"": 2,
                ""_value"": 1,
                ""_floatValue"": 1,
                ""_customData"": {
                    ""_lightGradient"": {
                        ""_duration"": 0.25,
                        ""_startColor"": [0.298, 1, 0.584, 0],
                        ""_endColor"": [0.298, 1, 0.584],
                        ""_easing"": ""easeLinear""
                    }
                }
            }");

            var evt = V2Event.GetFromJson(gradientJson);

            Assert.That(evt.Value, Is.EqualTo((int)LightValue.BlueOn));
            Assert.That(evt.CustomLightGradient, Is.Not.Null);
            Assert.That(evt.CustomLightGradient.Duration, Is.EqualTo(0.25f));
            Assert.That(evt.CustomLightGradient.StartColor, Is.EqualTo(new Color(0.298f, 1f, 0.584f, 0f)));
            Assert.That(evt.CustomLightGradient.EndColor, Is.EqualTo(new Color(0.298f, 1f, 0.584f, 1f)));

            var output = V2Event.ToJson(evt);
            Assert.That(output["_value"].AsInt, Is.EqualTo((int)LightValue.BlueOn));
            Assert.That(output["_customData"]["_lightGradient"]["_startColor"][3].AsFloat, Is.EqualTo(0f));
            Assert.That(output["_customData"]["_lightGradient"]["_endColor"].Count, Is.EqualTo(3));
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
            
            foreach (var difficultyEvent in difficulty.BpmEvents)
            {
                Debug.Log(difficultyEvent);
            }
            Assert.AreEqual(1, difficulty.BpmEvents.Count);
            BeatmapAssert.BpmEventPropertiesAreEqual(difficulty.BpmEvents[0], 10, 128);
            
            foreach (var difficultyEvent in difficulty.Events)
            {
                Debug.Log(difficultyEvent);
            }
            Assert.AreEqual(2, difficulty.Events.Count);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[0], 10, 1, 3, 1, null);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[1], 10, 5, 1, 0, null);
            
            Assert.AreEqual(2, difficulty.RotationEvents.Count);
            BeatmapAssert.RotationEventPropertiesAreEqual(difficulty.RotationEvents[0], 10, 14, 4, ExecutionTime.Early, 15f);
            BeatmapAssert.RotationEventPropertiesAreEqual(difficulty.RotationEvents[1], 15, 15, 4, ExecutionTime.Late, 15f);
        }
    }
}

using System;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class BeatmapV3Test
    {
        private const string fileJson = @"
{
    ""version"": ""3.3.0"",
    ""bpmEvents"": [
        {
            ""b"": 10,
            ""m"": 128
        }
    ],
    ""rotationEvents"": [
        {
            ""b"": 10,
            ""e"": 0,
            ""r"": 15
        },
        {
            ""b"": 15,
            ""e"": 1,
            ""r"": 15
        }
    ],
    ""colorNotes"": [
        {
            ""b"": 10,
            ""x"": 1,
            ""y"": 0,
            ""c"": 0,
            ""d"": 1,
            ""a"": 0
        }
    ],
    ""bombNotes"": [
        {
            ""b"": 10,
            ""x"": 1,
            ""y"": 0
        }
    ],
    ""obstacles"": [
        {
            ""b"": 10,
            ""d"": 5,
            ""x"": 1,
            ""y"": 2,
            ""w"": 1,
            ""h"": 3
        },
        {
            ""b"": 10,
            ""d"": 5,
            ""x"": 2,
            ""y"": 0,
            ""w"": 1,
            ""h"": 5
        }
    ],
    ""sliders"": [
        {
            ""c"": 0,
            ""b"": 10,
            ""x"": 1,
            ""y"": 0,
            ""d"": 1,
            ""mu"": 1,
            ""tb"": 15,
            ""tx"": 2,
            ""ty"": 2,
            ""tc"": 0,
            ""tmu"": 1,
            ""m"": 0
        }
    ],
    ""burstSliders"": [
        {
            ""c"": 0,
            ""b"": 10,
            ""x"": 1,
            ""y"": 0,
            ""d"": 1,
            ""tb"": 15,
            ""tx"": 2,
            ""ty"": 2,
            ""sc"": 3,
            ""s"": 0.5
        }
    ],
    ""basicBeatmapEvents"": [
        {
            ""b"": 10,
            ""et"": 1,
            ""i"": 3,
            ""f"": 1
        }
    ],
    ""colorBoostBeatmapEvents"": [
        {
            ""b"": 10,
            ""o"": true
        }
    ],
    ""waypoints"": [
        {
            ""b"": 10,
            ""x"": 1,
            ""y"": 0,
            ""d"": 1
        }
    ],
    ""basicEventTypesWithKeywords"": {
        ""d"": [
            {
                ""k"": ""SECRET"",
                ""e"": [
                    40,
                    41,
                    42,
                    43
                ]
            }
        ]
    },
    ""lightColorEventBoxGroups"": [
        {
            ""b"": 2,
            ""g"": 0,
            ""e"": [
                {
                    ""f"": {
                        ""c"": 1,
                        ""f"": 1,
                        ""p"": 1,
                        ""t"": 0,
                        ""r"": 0,
                        ""n"": 0,
                        ""s"": 0,
                        ""l"": 0,
                        ""d"": 0
                    },
                    ""w"": 1,
                    ""d"": 1,
                    ""r"": 1,
                    ""t"": 1,
                    ""b"": 1,
                    ""i"": 0,
                    ""e"": [
                        {
                            ""b"": 0,
                            ""i"": 0,
                            ""c"": 1,
                            ""s"": 1,
                            ""f"": 0,
                            ""sb"": 0,
                            ""sf"": 0
                        }
                    ]
                }
            ]
        }
    ],
    ""lightRotationEventBoxGroups"": [
        {
            ""b"": 2,
            ""g"": 0,
            ""e"": [
                {
                    ""f"": {
                        ""c"": 1,
                        ""f"": 1,
                        ""p"": 1,
                        ""t"": 0,
                        ""r"": 0,
                        ""n"": 0,
                        ""s"": 0,
                        ""l"": 0,
                        ""d"": 0
                    },
                    ""w"": 1,
                    ""d"": 1,
                    ""s"": 1,
                    ""t"": 1,
                    ""b"": 1,
                    ""i"": 0,
                    ""a"": 0,
                    ""r"": 1,
                    ""l"": [
                        {
                            ""b"": 0,
                            ""p"": 0,
                            ""e"": 1,
                            ""r"": 340,
                            ""o"": 1,
                            ""l"": 1
                        }
                    ]
                }
            ]
        }
    ],
    ""lightTranslationEventBoxGroups"": [
        {
            ""b"": 2,
            ""g"": 0,
            ""e"": [
                {
                    ""f"": {
                        ""c"": 1,
                        ""f"": 1,
                        ""p"": 1,
                        ""t"": 0,
                        ""r"": 0,
                        ""n"": 0,
                        ""s"": 0,
                        ""l"": 0,
                        ""d"": 0
                    },
                    ""w"": 1,
                    ""d"": 1,
                    ""s"": 1,
                    ""t"": 1,
                    ""b"": 1,
                    ""i"": 0,
                    ""a"": 0,
                    ""r"": 1,
                    ""l"": [
                        {
                            ""b"": 0,
                            ""p"": 0,
                            ""e"": 1,
                            ""t"": 100
                        }
                    ]
                }
            ]
        }
    ],
    ""vfxEventBoxGroups"": [
        {
            ""b"": 2,
            ""g"": 0,
            ""e"": [
                {
                    ""f"": {
                        ""c"": 1,
                        ""f"": 1,
                        ""p"": 1,
                        ""t"": 0,
                        ""r"": 0,
                        ""n"": 0,
                        ""s"": 0,
                        ""l"": 0,
                        ""d"": 0
                    },
                    ""w"": 1,
                    ""d"": 1,
                    ""s"": 1,
                    ""t"": 1,
                    ""b"": 1,
                    ""i"": 0,
                    ""l"": [
                        0
                    ]
                }
            ]
        }
    ],
    ""_fxEventsCollections"": {
        ""_fl"": [
            {
                ""b"": 0,
                ""p"": 0,
                ""i"": 1,
                ""v"": 100
            }
        ],
        ""_il"": []
    },
    ""useNormalEventsAsCompatibleEvents"": false
}";


        // For use in PlayMode
        public void TestEverything()
        {
        }

        [SetUp]
        public void Setup()
        {
            Settings.Instance.MapVersion = 3;
        }

        [Test]
        public void GetFromJson()
        {
            var difficulty = V3Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");
            
            Assert.AreEqual("3.3.0",difficulty.Version);
            AssertDifficulty(difficulty, true);
        }
        
        [Test]
        public void GetOutputJson()
        {
            var difficulty = V3Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");
            var outputJson = V3Difficulty.GetOutputJson(difficulty);
            var reparsed = V3Difficulty.GetFromJson(outputJson, "");
            
            reparsed.BpmEvents.RemoveAt(0); // Remove inserted bpm
            
            AssertDifficulty(reparsed, true); // This should have the same stuff
        }

        [Test]
        public void GetOutputJsonAfterSwitchingToV2()
        {
            var difficulty = V3Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");

            Settings.Instance.MapVersion = 2;
            var outputJson = V2Difficulty.GetOutputJson(difficulty);
            var reparsed = V2Difficulty.GetFromJson(outputJson, "");
            
            reparsed.BpmEvents.RemoveAt(0); // Remove inserted bpm

            AssertDifficulty(reparsed, false); // This should have the same stuff
        }

        // Chains are tested outside of this method
        private static void AssertDifficulty(BaseDifficulty difficulty, bool slidersExist)
        {
            Assert.AreEqual(2, difficulty.Notes.Count);
            BeatmapAssert.NotePropertiesAreEqual(difficulty.Notes[0], 10, 1, 0, 0, 1, 0);
            BeatmapAssert.NotePropertiesAreEqual(difficulty.Notes[1], 10, 1, 0, 3, 0, 0);
            
            Assert.AreEqual(2, difficulty.Obstacles.Count);
            BeatmapAssert.ObstaclePropertiesAreEqual(difficulty.Obstacles[0], 10, 1, 2, 1, 1, 3, 5);
            BeatmapAssert.ObstaclePropertiesAreEqual(difficulty.Obstacles[1], 10, 2, 0, 0, 1, 5, 5);
            
            if (slidersExist)
            {
                Assert.AreEqual(1, difficulty.Arcs.Count);
                BeatmapAssert.ArcPropertiesAreEqual(difficulty.Arcs[0], 10, 1, 0, 0, 1, 1, 15, 2, 2, 0, 1, 0);

                Assert.AreEqual(1, difficulty.Chains.Count);
                BeatmapAssert.ChainPropertiesAreEqual(difficulty.Chains[0], 10, 1, 0, 0, 1, 15, 2, 2, 3, 0.5f);
            }
            else
            {
                Assert.AreEqual(0, difficulty.Arcs.Count);
                Assert.AreEqual(0, difficulty.Chains.Count);
            }

            Assert.AreEqual(1, difficulty.BpmEvents.Count);
            BeatmapAssert.BpmEventPropertiesAreEqual(difficulty.BpmEvents[0], 10, 128);
            
            Assert.AreEqual(4, difficulty.Events.Count);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[0], 10, 1, 3, 1, null);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[1], 10, 5, 1, 0, null);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[2], 10, 14, 4, null, 15f);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[3], 15, 15, 4, null, 15f);
        }
    }
}
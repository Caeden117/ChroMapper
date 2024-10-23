using System;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using Beatmap.V4;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class BeatmapV4Test
    {
        private const string beatmapFileJson = @"
{
    ""version"": ""4.0.0"",
    ""colorNotes"": [ {""b"": 10, ""r"": 0, ""i"": 0} ],
    ""colorNotesData"": [
        {""x"": 1, ""y"": 0, ""c"": 0, ""d"": 1, ""a"": 0},
        {""x"": 2, ""y"": 2, ""c"": 0, ""d"": 0, ""a"": 0}
    ],
    ""bombNotes"": [ {""b"": 10, ""r"": 0, ""i"": 0} ],
    ""bombNotesData"": [ {""x"": 1, ""y"": 0} ],
    ""obstacles"": [
        {""b"": 10, ""r"": 0, ""i"": 0},
        {""b"": 10, ""r"": 0, ""i"": 1}
    ],
    ""obstaclesData"": [
        {""d"": 5, ""x"": 1, ""y"": 2, ""w"": 1, ""h"": 3},
        {""d"": 5, ""x"": 2, ""y"": 0, ""w"": 1, ""h"": 5}
    ],
    ""arcs"": [
        {""hb"": 10, ""tb"": 15, ""hr"": 0, ""tr"": 0, ""hi"": 0, ""ti"": 1, ""ai"": 0}
    ],
    ""arcsData"": [ {""m"": 1, ""tm"": 1, ""a"": 0} ],
    ""chains"": [ {""hb"": 10, ""tb"": 15, ""hr"": 0, ""tr"": 0, ""i"": 0, ""ci"": 0} ],
    ""chainsData"": [ {""tx"": 2, ""ty"": 2, ""c"": 3, ""s"": 0.5} ],
    ""spawnRotations"": [ {""b"": 10, ""i"": 0}, {""b"": 15, ""i"": 1} ],
    ""spawnRotationsData"": [ {""t"": 0, ""r"": 15}, {""t"": 1, ""r"": 15} ]
}
";
        private const string lightshowFileJson = @"
{
    ""version"": ""4.0.0"",
    ""basicEvents"": [ {""b"": 10, ""i"": 0} ],
    ""basicEventsData"": [ {""t"": 1, ""i"": 3, ""f"": 1} ],
    ""colorBoostEvents"": [ {""b"": 10, ""i"": 0} ],
    ""colorBoostEventsData"": [ {""b"": 1} ],
    ""waypoints"": [ {""b"": 10, ""i"": 0} ],
    ""waypointsData"": [ {""x"": 1, ""y"": 0, ""d"": 1} ],
    ""basicEventTypesWithKeywords"": {
        ""d"": [
            { ""k"": ""SECRET"", ""e"": [40, 41, 42, 43] }
        ]
    },
    ""eventBoxGroups"": [
        {
            ""b"": 2,
            ""g"": 0,
            ""t"": 1,
            ""e"": [
                {
                    ""f"": 0,
                    ""e"": 0,
                    ""l"": [ {""b"": 0, ""i"": 0} ]
                }
            ]
        },
        {
            ""b"": 2,
            ""g"": 0,
            ""t"": 2,
            ""e"": [
                {
                    ""f"": 0,
                    ""e"": 0,
                    ""l"": [ {""b"": 0, ""i"": 0} ]
                }
            ]
        },
        {
            ""b"": 2,
            ""g"": 0,
            ""t"": 3,
            ""e"": [
                {
                    ""f"": 0,
                    ""e"": 0,
                    ""l"": [ {""b"": 0, ""i"": 0} ]
                }
            ]
        },
        {
            ""b"": 2,
            ""g"": 0,
            ""t"": 4,
            ""e"": [
                {
                    ""f"": 0,
                    ""e"": 0,
                    ""l"": [ {""b"": 0, ""i"": 0} ]
                }
            ]
        }
    ],
    ""indexFilters"": [
        {""c"": 1, ""f"": 1, ""p"": 1, ""t"": 0, ""r"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0}
    ],
    ""lightColorEventBoxes"": [
        {""w"": 1, ""d"": 1, ""s"": 1, ""t"": 1, ""b"": 1, ""e"": 0}
    ],
    ""lightColorEvents"": [
        {""p"": 0, ""e"": -1, ""c"": 1, ""b"": 1, ""f"": 0, ""sb"": 0, ""sf"": 0}
    ],
    ""lightRotationEventBoxes"": [
        {""w"": 1, ""d"": 1, ""s"": 1, ""t"": 1, ""b"": 1, ""e"": 0, ""a"": 0, ""f"": 1}
    ],
    ""lightRotationEvents"": [ {""p"": 0, ""e"": 1, ""r"": 340, ""d"": 1, ""l"": 1} ],
    ""lightTranslationEventBoxes"": [
        {""w"": 1, ""d"": 1, ""s"": 1, ""t"": 1, ""b"": 1, ""e"": 0, ""a"": 0, ""f"": 1}
    ],
    ""lightTranslationEvents"": [ {""p"": 0, ""e"": 1, ""t"": 100} ],
    ""fxEventBoxes"": [ {""w"": 1, ""d"": 1, ""s"": 1, ""t"": 1, ""b"": 1, ""e"": 0} ],
    ""floatFxEvents"": [ {""p"": 0, ""e"": 1, ""v"": 100} ],
    ""useNormalEventsAsCompatibleEvents"": false
}";



        // For use in PlayMode
        public void TestEverything()
        {
        }

        [SetUp]
        public void Setup()
        {
            Settings.Instance.MapVersion = 4;
            Settings.Instance.SaveWithoutDefaultValues = false;
        }

        [Test]
        public void GetFromJson()
        {
            var difficulty = V4Difficulty.GetFromJson(JSONNode.Parse(beatmapFileJson), "");
            
            Assert.AreEqual("4.0.0",difficulty.Version);
            AssertBeatmap(difficulty);
        }
        
        [Test]
        public void GetOutputJson()
        {
            var difficulty = V4Difficulty.GetFromJson(JSONNode.Parse(beatmapFileJson), "");
            var outputJson = V4Difficulty.GetOutputJson(difficulty);
            var reparsed = V4Difficulty.GetFromJson(outputJson, "");
            
            AssertBeatmap(reparsed); // This should have the same stuff
        }

        [Test]
        public void GetOutputJsonAfterSwitchingToV3()
        {
            var difficulty = V4Difficulty.GetFromJson(JSONNode.Parse(beatmapFileJson), "");

            Settings.Instance.MapVersion = 3;
            var outputJson = V3Difficulty.GetOutputJson(difficulty);
            var reparsed = V3Difficulty.GetFromJson(outputJson, "");
            
            AssertBeatmap(reparsed); // This should have the same stuff
        }

        private static void AssertBeatmap(BaseDifficulty difficulty)
        {
            Assert.AreEqual(2, difficulty.Notes.Count);
            BeatmapAssert.NotePropertiesAreEqual(difficulty.Notes[0], 10, 1, 0, 0, 1, 0);
            BeatmapAssert.NotePropertiesAreEqual(difficulty.Notes[1], 10, 1, 0, 3, 0, 0);
            
            Assert.AreEqual(2, difficulty.Obstacles.Count);
            BeatmapAssert.ObstaclePropertiesAreEqual(difficulty.Obstacles[0], 10, 1, 2, 1, 1, 3, 5);
            BeatmapAssert.ObstaclePropertiesAreEqual(difficulty.Obstacles[1], 10, 2, 0, 0, 1, 5, 5);
            
            Assert.AreEqual(1, difficulty.Arcs.Count);
            BeatmapAssert.ArcPropertiesAreEqual(difficulty.Arcs[0], 10, 1, 0, 0, 1, 1, 15, 2, 2, 0, 1, 0);

            Assert.AreEqual(1, difficulty.Chains.Count);
            BeatmapAssert.ChainPropertiesAreEqual(difficulty.Chains[0], 10, 1, 0, 0, 1, 15, 2, 2, 3, 0.5f);
        }
        
        private static void AssertLightshow(BaseDifficulty difficulty, bool slidersExist)
        {
            Assert.AreEqual(4, difficulty.Events.Count);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[0], 10, 1, 3, 1, null);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[1], 10, 5, 1, 0, null);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[2], 10, 14, 4, null, 15f);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[3], 15, 15, 4, null, 15f);
        }
    }
}
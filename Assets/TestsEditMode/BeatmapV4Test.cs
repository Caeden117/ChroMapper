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
    ""version"": ""4.1.0"",
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
    ""spawnRotationsData"": [ {""t"": 0, ""r"": 15}, {""t"": 1, ""r"": 15} ],
    ""njsEvents"": [ {""b"": 1 } ],
    ""njsEventData"": [ {""p"": 1, ""e"": 2, ""d"": 3} ],
}
";
        private const string lightshowFileJson = @"
{
    ""version"": ""4.0.0"",
    ""basicEvents"": [ {""b"": 10.5, ""i"": 0} ],
    ""basicEventsData"": [ {""t"": 1, ""i"": 3, ""f"": 1} ],
    ""colorBoostEvents"": [ {""b"": 10.5, ""i"": 0} ],
    ""colorBoostEventsData"": [ {""b"": 1} ],
    ""waypoints"": [ {""b"": 10.5, ""i"": 0} ],
    ""waypointsData"": [ {""x"": 1, ""y"": 0, ""d"": 1} ],
    ""basicEventTypesWithKeywords"": {
        ""d"": [
            { ""k"": ""SECRET"", ""e"": [40, 41, 42, 43] }
        ]
    },
    ""eventBoxGroups"": [
        {
            ""b"": 2.5,
            ""g"": 0,
            ""t"": 1,
            ""e"": [
                {
                    ""f"": 0,
                    ""e"": 0,
                    ""l"": [ {""b"": 0.5, ""i"": 0} ]
                }
            ]
        },
        {
            ""b"": 2.5,
            ""g"": 0,
            ""t"": 2,
            ""e"": [
                {
                    ""f"": 0,
                    ""e"": 0,
                    ""l"": [ {""b"": 0.5, ""i"": 0} ]
                }
            ]
        },
        {
            ""b"": 2.5,
            ""g"": 0,
            ""t"": 3,
            ""e"": [
                {
                    ""f"": 0,
                    ""e"": 0,
                    ""l"": [ {""b"": 0.5, ""i"": 0} ]
                }
            ]
        },
        {
            ""b"": 2.5,
            ""g"": 0,
            ""t"": 4,
            ""e"": [
                {
                    ""f"": 0,
                    ""e"": 0,
                    ""l"": [ {""b"": 0.5, ""i"": 0} ]
                }
            ]
        }
    ],
    ""indexFilters"": [
        {""c"": 1, ""f"": 1, ""p"": 1, ""t"": 0, ""r"": 0, ""n"": 0, ""s"": 0, ""l"": 0.5, ""d"": 0}
    ],
    ""lightColorEventBoxes"": [
        {""w"": 1.5, ""d"": 1, ""s"": 1.5, ""t"": 1, ""b"": 1, ""e"": 0}
    ],
    ""lightColorEvents"": [
        {""p"": 0, ""e"": 1, ""c"": 1, ""b"": 1.5, ""f"": 0, ""sb"": 0.5, ""sf"": 0}
    ],
    ""lightRotationEventBoxes"": [
        {""w"": 1.5, ""d"": 1, ""s"": 1.5, ""t"": 1, ""b"": 1, ""e"": 0, ""a"": 1, ""f"": 1}
    ],
    ""lightRotationEvents"": [ {""p"": 0, ""e"": 1, ""r"": 340.5, ""d"": 1, ""l"": 1} ],
    ""lightTranslationEventBoxes"": [
        {""w"": 1.5, ""d"": 1, ""s"": 1.5, ""t"": 1, ""b"": 1, ""e"": 0, ""a"": 2, ""f"": 1}
    ],
    ""lightTranslationEvents"": [ {""p"": 0, ""e"": 1, ""t"": 100.5} ],
    ""fxEventBoxes"": [ {""w"": 1.5, ""d"": 1, ""s"": 1.5, ""t"": 1, ""b"": 1, ""e"": 0} ],
    ""floatFxEvents"": [ {""p"": 0, ""e"": 1, ""v"": 100.5} ],
    ""useNormalEventsAsCompatibleEvents"": false
}";

        // BeatSaver permits extensions only under customData, so the flat V3 VNJS fixture nests the array there without a V4 common-data table.
        private const string flatV3VNJSJson = @"
{
    ""version"": ""3.3.0"",
    ""customData"": {
        ""njsEvents"": [
            {""b"": 2, ""d"": 3, ""p"": 0, ""e"": 1},
            {""b"": 4, ""d"": -2, ""p"": 1, ""e"": 0}
        ]
    }
}";

        // This is the exact beat/index sequence from the reported V4-to-V3 regression, including repeated common-data references.
        private static readonly (float Beat, int Index)[] indexedV4VNJSEvents =
        {
            (4, 0),
            (5, 1),
            (7, 2),
            (7.25f, 3),
            (7.5f, 2),
            (8, 4),
            (40, 5),
            (46, 6),
            (110, 2),
            (110.5f, 7),
            (112, 8),
            (128, 2),
            (136, 0),
            (140, 2),
            (140.25f, 3),
            (142, 2),
            (143.5f, 9),
            (144, 10),
            (176, 11),
            (177.5f, 1),
            (178, 2),
            (178.5f, 12),
            (206, 2),
            (208, 13),
            (244, 2),
            (244.25f, 14),
            (245, 2),
            (248, 10),
            (262, 2),
            (262.75f, 15),
            (263, 2),
            (263.375f, 16),
            (264, 4),
            (294, 2),
            (294.5f, 15),
            (295, 2),
            (296, 4),
            (312, 17),
            (312.5f, 3),
            (313, 2),
            (313.5f, 4),
            (313.531f, 17),
            (314, 18),
            (314.5f, 2),
            (315, 4),
            (315.031f, 19),
            (317.5f, 1),
            (318.5f, 4),
            (345, 5),
            (352, 14),
            (353, 10)
        };

        // This is the exact indexed common-data table from the regression, including negative and extended easing identifiers.
        private static readonly (int UsePrevious, int Easing, float RelativeNJS)[] indexedV4VNJSEventData =
        {
            (0, 0, 0),
            (0, 2, -2),
            (1, 0, 0),
            (0, 20, -10),
            (0, 1, 0),
            (0, -1, 80),
            (0, 20, -3),
            (0, 20, 0),
            (0, 1, -2),
            (0, 20, 2),
            (0, 19, 0),
            (0, -1, 20),
            (0, 2, 0),
            (0, 20, -1),
            (0, 20, -15),
            (0, 20, -8),
            (0, 2, 3),
            (0, -1, 10),
            (0, 2, -10),
            (0, -1, 180)
        };



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
            
            Assert.AreEqual("4.1.0",difficulty.Version);
            AssertBeatmap(difficulty, containsRotationEvent: true, containsNJSEvent: true);
        }
        
        [Test]
        public void LoadLightsFromJson()
        {
            var difficulty = new BaseDifficulty();
            V4Difficulty.LoadLightsFromJson(difficulty, JSONNode.Parse(lightshowFileJson));
            
            AssertLightshow(difficulty);
        }

        [Test]
        public void GetOutputJson()
        {
            var difficulty = V4Difficulty.GetFromJson(JSONNode.Parse(beatmapFileJson), "");
            var outputJson = V4Difficulty.GetOutputJson(difficulty);
            var reparsed = V4Difficulty.GetFromJson(outputJson, "");
            
            AssertBeatmap(reparsed, containsRotationEvent: false, containsNJSEvent: true); // This should compatible stuff
        }

        [Test]
        public void GetLightshowOutputJson()
        {
            var difficulty = new BaseDifficulty();
            V4Difficulty.LoadLightsFromJson(difficulty, JSONNode.Parse(lightshowFileJson));
            var outputJson = V4Difficulty.GetLightshowOutputJson(difficulty);

            var reparsed = new BaseDifficulty();
            V4Difficulty.LoadLightsFromJson(reparsed, outputJson);
            
            AssertLightshow(reparsed); // This should have the same stuff
        }

        [Test]
        public void GetOutputJsonAfterSwitchingToV3()
        {
            var difficulty = V4Difficulty.GetFromJson(JSONNode.Parse(beatmapFileJson), "");

            Settings.Instance.MapVersion = 3;
            var outputJson = V3Difficulty.GetOutputJson(difficulty);

            // The V3 loader consumes recognized customData from its input node, so validate the serialized BeatSaver-safe VNJS shape before reparsing it.
            Assert.IsFalse(outputJson.HasKey("njsEvents"));
            Assert.IsFalse(outputJson.HasKey("njsEventData"));
            var njsEvent = outputJson["customData"]["njsEvents"][0];
            Assert.IsTrue(njsEvent.HasKey("b"));
            Assert.IsTrue(njsEvent.HasKey("d"));
            Assert.IsTrue(njsEvent.HasKey("p"));
            Assert.IsTrue(njsEvent.HasKey("e"));
            Assert.AreEqual(1f, njsEvent["b"].AsFloat);
            Assert.AreEqual(3f, njsEvent["d"].AsFloat);
            Assert.AreEqual(1, njsEvent["p"].AsInt);
            Assert.AreEqual(2, njsEvent["e"].AsInt);

            // The second half of the regression verifies that the same nested flat output survives a V3 load.
            var reparsed = V3Difficulty.GetFromJson(outputJson, "");
            AssertBeatmap(reparsed, containsRotationEvent: true, containsNJSEvent: true); // This should have compatible stuff
        }

        [Test]
        public void IndexedV4VNJSRegressionFlattensInsideCustomDataAndStripsBothRootArrays()
        {
            // Converting the reported indexed V4 payload must resolve every common-data reference before writing BeatSaver-safe V3 JSON.
            var difficulty = V4Difficulty.GetFromJson(CreateIndexedV4VNJSRegressionJson(), "");
            Settings.Instance.MapVersion = 3;

            var outputJson = V3Difficulty.GetOutputJson(difficulty);

            Assert.IsFalse(outputJson.HasKey("njsEvents"));
            Assert.IsFalse(outputJson.HasKey("njsEventData"));
            Assert.IsFalse(outputJson["customData"].HasKey("njsEventData"));

            var flatEvents = outputJson["customData"]["njsEvents"].AsArray;
            Assert.IsNotNull(flatEvents);
            Assert.AreEqual(indexedV4VNJSEvents.Length, flatEvents.Count);

            // Every output record must inline the referenced data and discard the V4 index field.
            for (var i = 0; i < indexedV4VNJSEvents.Length; i++)
            {
                var sourceEvent = indexedV4VNJSEvents[i];
                var sourceData = indexedV4VNJSEventData[sourceEvent.Index];
                var flatEvent = flatEvents[i];

                Assert.IsFalse(flatEvent.HasKey("i"));
                Assert.AreEqual(sourceEvent.Beat, flatEvent["b"].AsFloat);
                Assert.AreEqual(sourceData.RelativeNJS, flatEvent["d"].AsFloat);
                Assert.AreEqual(sourceData.UsePrevious, flatEvent["p"].AsInt);
                Assert.AreEqual(sourceData.Easing, flatEvent["e"].AsInt);
            }
        }

        [Test]
        public void FlatV3VNJSEventsLoadWithoutCommonDataIndexes()
        {
            // Direct V3 loading must preserve every customData-nested flat field without consulting njsEventData.
            var difficulty = V3Difficulty.GetFromJson(JSONNode.Parse(flatV3VNJSJson), "");

            Assert.AreEqual(2, difficulty.NJSEvents.Count);
            Assert.AreEqual(2f, difficulty.NJSEvents[0].JsonTime);
            Assert.AreEqual(3f, difficulty.NJSEvents[0].RelativeNJS);
            Assert.AreEqual(0, difficulty.NJSEvents[0].UsePrevious);
            Assert.AreEqual(1, difficulty.NJSEvents[0].Easing);
            Assert.AreEqual(4f, difficulty.NJSEvents[1].JsonTime);
            Assert.AreEqual(-2f, difficulty.NJSEvents[1].RelativeNJS);
            Assert.AreEqual(1, difficulty.NJSEvents[1].UsePrevious);
            Assert.AreEqual(0, difficulty.NJSEvents[1].Easing);
        }

        [Test]
        public void FlatV3VNJSEventsKeepZeroFieldsWhenDefaultsAreRemoved()
        {
            // All four extension fields are structural, so default-value stripping must not erase zero-valued VNJS data.
            Settings.Instance.MapVersion = 3;
            Settings.Instance.SaveWithoutDefaultValues = true;
            var difficulty = new BaseDifficulty
            {
                NJSEvents = new()
                {
                    new BaseNJSEvent
                    {
                        JsonTime = 0,
                        RelativeNJS = 0,
                        UsePrevious = 0,
                        Easing = 0
                    }
                }
            };

            var outputJson = V3Difficulty.GetOutputJson(difficulty);
            var njsEvent = outputJson["customData"]["njsEvents"][0];

            Assert.IsFalse(outputJson.HasKey("njsEvents"));
            Assert.IsFalse(outputJson.HasKey("njsEventData"));
            Assert.IsTrue(njsEvent.HasKey("b"));
            Assert.IsTrue(njsEvent.HasKey("d"));
            Assert.IsTrue(njsEvent.HasKey("p"));
            Assert.IsTrue(njsEvent.HasKey("e"));
            Assert.AreEqual(0f, njsEvent["b"].AsFloat);
            Assert.AreEqual(0f, njsEvent["d"].AsFloat);
            Assert.AreEqual(0, njsEvent["p"].AsInt);
            Assert.AreEqual(0, njsEvent["e"].AsInt);
        }

        [Test]
        public void V3VNJSOptOutRetainsLegacyOmission()
        {
            // Choosing No in the converter records this opt-out, which must omit both possible VNJS representations.
            Settings.Instance.MapVersion = 3;
            var difficulty = new BaseDifficulty
            {
                SaveVNJSEventsInV3 = false,
                NJSEvents = new() { new BaseNJSEvent { JsonTime = 1, RelativeNJS = 2 } }
            };

            var outputJson = V3Difficulty.GetOutputJson(difficulty);

            Assert.IsFalse(outputJson.HasKey("njsEvents"));
            Assert.IsFalse(outputJson.HasKey("njsEventData"));
            Assert.IsFalse(outputJson["customData"].HasKey("njsEvents"));
        }

        // Building the regression JSON from authoritative tuple tables keeps every supplied beat/index/data value independently assertable.
        private static JSONObject CreateIndexedV4VNJSRegressionJson()
        {
            var events = new JSONArray();
            foreach (var (beat, index) in indexedV4VNJSEvents)
            {
                events.Add(new JSONObject
                {
                    ["b"] = beat,
                    ["i"] = index
                });
            }

            var eventData = new JSONArray();
            foreach (var (usePrevious, easing, relativeNJS) in indexedV4VNJSEventData)
            {
                eventData.Add(new JSONObject
                {
                    ["p"] = usePrevious,
                    ["e"] = easing,
                    ["d"] = relativeNJS
                });
            }

            return new JSONObject
            {
                ["version"] = "4.1.0",
                ["njsEvents"] = events,
                ["njsEventData"] = eventData
            };
        }

        private static void AssertBeatmap(BaseDifficulty difficulty, bool containsRotationEvent = false, bool containsNJSEvent = false)
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
            
            // Present on load but not after save
            if (containsRotationEvent)
            {
                Assert.AreEqual(2, difficulty.RotationEvents.Count);
                BeatmapAssert.RotationEventPropertiesAreEqual(difficulty.RotationEvents[0], 10, 14, 4, ExecutionTime.Early, 15f);
                BeatmapAssert.RotationEventPropertiesAreEqual(difficulty.RotationEvents[1], 15, 15, 4, ExecutionTime.Late, 15f);
            }
            else
            {
                Assert.AreEqual(0, difficulty.Events.Count);
            }

            if (containsNJSEvent)
            {
                Assert.AreEqual(1, difficulty.NJSEvents.Count);
                BeatmapAssert.NJSEventPropertiesAreEqual(difficulty.NJSEvents[0], 1, 1, 2, 3f);
            }
            else
            {
                Assert.AreEqual(0, difficulty.NJSEvents.Count);
            }
        }
        
        private static void AssertLightshow(BaseDifficulty difficulty)
        {
            // Basic + Boost
            Assert.AreEqual(2, difficulty.Events.Count);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[0], 10.5f, 1, 3, 1, null);
            BeatmapAssert.EventPropertiesAreEqual(difficulty.Events[1], 10.5f, 5, 1, 0, null);

            // Color
            Assert.AreEqual(1, difficulty.LightColorEventBoxGroups.Count);
            var colorGroup = difficulty.LightColorEventBoxGroups[0];
            
            Assert.AreEqual(2.5f, colorGroup.JsonTime);
            Assert.AreEqual(0f, colorGroup.ID);
            
            Assert.AreEqual(1, colorGroup.Boxes.Count);
            var colorGroupBox = colorGroup.Boxes[0];
            
            var colorIndexFilter = colorGroupBox.IndexFilter;
            Assert.AreEqual(1, colorIndexFilter.Chunks);
            Assert.AreEqual(1, colorIndexFilter.Type);
            Assert.AreEqual(1, colorIndexFilter.Param0);
            Assert.AreEqual(0, colorIndexFilter.Param1);
            Assert.AreEqual(0, colorIndexFilter.Reverse);
            Assert.AreEqual(0, colorIndexFilter.Random);
            Assert.AreEqual(0, colorIndexFilter.Seed);
            Assert.AreEqual(0.5f, colorIndexFilter.Limit);
            Assert.AreEqual(0, colorIndexFilter.LimitAffectsType);

            Assert.AreEqual(1.5f, colorGroupBox.BeatDistribution);
            Assert.AreEqual(1, colorGroupBox.BeatDistributionType);
            Assert.AreEqual(1.5f, colorGroupBox.BrightnessDistribution);
            Assert.AreEqual(1, colorGroupBox.BrightnessDistributionType);
            Assert.AreEqual(0, colorGroupBox.Easing);
            Assert.AreEqual(1, colorGroupBox.BrightnessAffectFirst);

            Assert.AreEqual(1, colorGroupBox.Events.Length);
            var colorGroupEvent = colorGroupBox.Events[0];
            
            Assert.AreEqual(0.5f, colorGroupEvent.RelativeJsonTime);
            Assert.AreEqual(0, colorGroupEvent.UsePrevious);
            Assert.AreEqual(1, colorGroupEvent.Color);
            Assert.AreEqual(1.5f, colorGroupEvent.Brightness);
            Assert.AreEqual(0, colorGroupEvent.Frequency);
            Assert.AreEqual(0.5f, colorGroupEvent.StrobeBrightness);
            Assert.AreEqual(0, colorGroupEvent.StrobeFade);
            
            // Rotation
            Assert.AreEqual(1, difficulty.LightRotationEventBoxGroups.Count);
            var rotationGroup = difficulty.LightRotationEventBoxGroups[0];
            
            Assert.AreEqual(2.5f, rotationGroup.JsonTime);
            Assert.AreEqual(0f, rotationGroup.ID);
            
            Assert.AreEqual(1, rotationGroup.Boxes.Count);
            var rotationGroupBox = rotationGroup.Boxes[0];
            
            var rotationIndexFilter = rotationGroupBox.IndexFilter;
            Assert.AreEqual(1, rotationIndexFilter.Chunks);
            Assert.AreEqual(1, rotationIndexFilter.Type);
            Assert.AreEqual(1, rotationIndexFilter.Param0);
            Assert.AreEqual(0, rotationIndexFilter.Param1);
            Assert.AreEqual(0, rotationIndexFilter.Reverse);
            Assert.AreEqual(0, rotationIndexFilter.Random);
            Assert.AreEqual(0, rotationIndexFilter.Seed);
            Assert.AreEqual(0.5f, rotationIndexFilter.Limit);
            Assert.AreEqual(0, rotationIndexFilter.LimitAffectsType);

            Assert.AreEqual(1.5f, rotationGroupBox.BeatDistribution);
            Assert.AreEqual(1, rotationGroupBox.BeatDistributionType);
            Assert.AreEqual(1.5f, rotationGroupBox.RotationDistribution);
            Assert.AreEqual(1, rotationGroupBox.RotationDistributionType);
            Assert.AreEqual(0, rotationGroupBox.Easing);
            Assert.AreEqual(1, rotationGroupBox.RotationAffectFirst);
            Assert.AreEqual(1, rotationGroupBox.Axis);
            Assert.AreEqual(1, rotationGroupBox.Flip);

            Assert.AreEqual(1, rotationGroupBox.Events.Length);
            var rotationGroupEvent = rotationGroupBox.Events[0];
            
            Assert.AreEqual(0.5f, rotationGroupEvent.RelativeJsonTime);
            Assert.AreEqual(0, rotationGroupEvent.UsePrevious);
            Assert.AreEqual(1, rotationGroupEvent.EaseType);
            Assert.AreEqual(340.5f, rotationGroupEvent.Rotation);
            Assert.AreEqual(1, rotationGroupEvent.Direction);
            Assert.AreEqual(1, rotationGroupEvent.Loop);
            
            // Translation
            Assert.AreEqual(1, difficulty.LightTranslationEventBoxGroups.Count);
            var translationGroup = difficulty.LightTranslationEventBoxGroups[0];
            
            Assert.AreEqual(2.5f, translationGroup.JsonTime);
            Assert.AreEqual(0f, translationGroup.ID);
            
            Assert.AreEqual(1, translationGroup.Boxes.Count);
            var translationGroupBox = translationGroup.Boxes[0];
            
            var translationIndexFilter = translationGroupBox.IndexFilter;
            Assert.AreEqual(1, translationIndexFilter.Chunks);
            Assert.AreEqual(1, translationIndexFilter.Type);
            Assert.AreEqual(1, translationIndexFilter.Param0);
            Assert.AreEqual(0, translationIndexFilter.Param1);
            Assert.AreEqual(0, translationIndexFilter.Reverse);
            Assert.AreEqual(0, translationIndexFilter.Random);
            Assert.AreEqual(0, translationIndexFilter.Seed);
            Assert.AreEqual(0.5f, translationIndexFilter.Limit);
            Assert.AreEqual(0, translationIndexFilter.LimitAffectsType);

            Assert.AreEqual(1.5f, translationGroupBox.BeatDistribution);
            Assert.AreEqual(1, translationGroupBox.BeatDistributionType);
            Assert.AreEqual(1.5f, translationGroupBox.TranslationDistribution);
            Assert.AreEqual(1, translationGroupBox.TranslationDistributionType);
            Assert.AreEqual(0, translationGroupBox.Easing);
            Assert.AreEqual(1, translationGroupBox.TranslationAffectFirst);
            Assert.AreEqual(2, translationGroupBox.Axis);
            Assert.AreEqual(1, translationGroupBox.Flip);

            Assert.AreEqual(1, translationGroupBox.Events.Length);
            var translationGroupEvent = translationGroupBox.Events[0];
            
            Assert.AreEqual(0.5f, translationGroupEvent.RelativeJsonTime);
            Assert.AreEqual(0, translationGroupEvent.UsePrevious);
            Assert.AreEqual(1, translationGroupEvent.EaseType);
            Assert.AreEqual(100.5f, translationGroupEvent.Translation);
            
            // FloatFX
            Assert.AreEqual(1, difficulty.VfxEventBoxGroups.Count);
            var vfxGroup = difficulty.VfxEventBoxGroups[0];
            
            Assert.AreEqual(2.5f, vfxGroup.JsonTime);
            Assert.AreEqual(0, vfxGroup.ID);
            
            Assert.AreEqual(1, vfxGroup.Boxes.Count);
            var vfxGroupBox = vfxGroup.Boxes[0];
            
            var vfxIndexFilter = vfxGroupBox.IndexFilter;
            Assert.AreEqual(1, vfxIndexFilter.Chunks);
            Assert.AreEqual(1, vfxIndexFilter.Type);
            Assert.AreEqual(1, vfxIndexFilter.Param0);
            Assert.AreEqual(0, vfxIndexFilter.Param1);
            Assert.AreEqual(0, vfxIndexFilter.Reverse);
            Assert.AreEqual(0, vfxIndexFilter.Random);
            Assert.AreEqual(0, vfxIndexFilter.Seed);
            Assert.AreEqual(0.5f, vfxIndexFilter.Limit);
            Assert.AreEqual(0, vfxIndexFilter.LimitAffectsType);

            Assert.AreEqual(1.5f, vfxGroupBox.BeatDistribution);
            Assert.AreEqual(1, vfxGroupBox.BeatDistributionType);
            Assert.AreEqual(1.5f, vfxGroupBox.VfxDistribution);
            Assert.AreEqual(1, vfxGroupBox.VfxDistributionType);
            Assert.AreEqual(0, vfxGroupBox.Easing);
            Assert.AreEqual(1, vfxGroupBox.VfxAffectFirst);

            Assert.AreEqual(1, vfxGroupBox.Events.Length);
            var fxFloatEvent = vfxGroupBox.Events[0];
            Assert.AreEqual(0.5f, fxFloatEvent.RelativeJsonTime);
            Assert.AreEqual(0, fxFloatEvent.UsePrevious);
            Assert.AreEqual(1, fxFloatEvent.Easing);
            Assert.AreEqual(100.5f, fxFloatEvent.Value);
        }
    }
}

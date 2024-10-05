using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace TestsEditMode
{
    public class InfoTest
    {
        private const string v2FileInfo = @"
{
    ""_version"": ""2.1.0"",
    ""_songName"": ""Magic"",
    ""_songSubName"": ""ft. Meredith Bull"",
    ""_songAuthorName"": ""Jaroslav Beck"",
    ""_levelAuthorName"": ""Freeek"",
    ""_beatsPerMinute"": 208,
    ""_songTimeOffset"": 0,
    ""_shuffle"": 0,
    ""_shufflePeriod"": 0,
    ""_previewStartTime"": 0,
    ""_previewDuration"": 0,
    ""_songFilename"": ""Magic.wav"",
    ""_coverImageFilename"": ""cover.png"",
    ""_environmentName"": ""WeaveEnvironment"",
    ""_allDirectionsEnvironmentName"": ""GlassDesertEnvironment"",
    ""_environmentNames"": [""WeaveEnvironment"", ""GlassDesertEnvironment""],
    ""_colorSchemes"": [
        {
            ""useOverride"": true,
            ""colorScheme"": {
                ""colorSchemeId"": ""Weave"",
                ""saberAColor"": {
                    ""r"": 0.78431370,
                    ""g"": 0.07843138,
                    ""b"": 0.07843138,
                    ""a"": 1.00000000
                },
                ""saberBColor"": {
                    ""r"": 0.1568627,
                    ""g"": 0.5568627,
                    ""b"": 0.8235294,
                    ""a"": 1.0000000
                },
                ""environmentColor0"": {
                    ""r"": 0.85000000,
                    ""g"": 0.08499997,
                    ""b"": 0.08499997,
                    ""a"": 1.00000000
                },
                ""environmentColor1"": {
                    ""r"": 0.1882353,
                    ""g"": 0.6752940,
                    ""b"": 1.0000000,
                    ""a"": 1.0000000
                },
                ""obstaclesColor"": {
                    ""r"": 1.0000000,
                    ""g"": 0.1882353,
                    ""b"": 0.1882353,
                    ""a"": 1.0000000
                },
                ""environmentColor0Boost"": {
                    ""r"": 0.82184090,
                    ""g"": 0.08627451,
                    ""b"": 0.85098040,
                    ""a"": 1.00000000
                },
                ""environmentColor1Boost"": {
                    ""r"": 0.5320754,
                    ""g"": 0.5320754,
                    ""b"": 0.5320754,
                    ""a"": 1.0000000
                }
            }
        }
    ],
    ""_difficultyBeatmapSets"": [
        {
            ""_beatmapCharacteristicName"": ""Standard"",
            ""_difficultyBeatmaps"": [
                {
                    ""_difficulty"": ""Easy"",
                    ""_difficultyRank"": 1,
                    ""_beatmapFilename"": ""Easy.dat"",
                    ""_noteJumpMovementSpeed"": 10,
                    ""_noteJumpStartBeatOffset"": 0,
                    ""_beatmapColorSchemeIdx"": 0,
                    ""_environmentNameIdx"": 0,
                    ""_customData"": {
                        ""_oneSaber"" : true,
                        ""_showRotationNoteSpawnLines"" : true,
						""_difficultyLabel"": ""ACustomLabel"",
                        ""_warnings"": [
                            ""Warning1"",
                            ""Warning2""
                        ],
						""_information"": [
                            ""Info""
                        ],
						""_suggestions"": [
                            ""Chroma""
                        ],
						""_requirements"": [
							""Noodle Extensions""
						]
                    }
                },
                {
                    ""_difficulty"": ""Normal"",
                    ""_difficultyRank"": 3,
                    ""_beatmapFilename"": ""Normal.dat"",
                    ""_noteJumpMovementSpeed"": 10,
                    ""_noteJumpStartBeatOffset"": 0,
                    ""_beatmapColorSchemeIdx"": 0,
                    ""_environmentNameIdx"": 0
                },
                {
                    ""_difficulty"": ""Hard"",
                    ""_difficultyRank"": 5,
                    ""_beatmapFilename"": ""Hard.dat"",
                    ""_noteJumpMovementSpeed"": 10,
                    ""_noteJumpStartBeatOffset"": 0,
                    ""_beatmapColorSchemeIdx"": 0,
                    ""_environmentNameIdx"": 0
                },
                {
                    ""_difficulty"": ""Expert"",
                    ""_difficultyRank"": 7,
                    ""_beatmapFilename"": ""Expert.dat"",
                    ""_noteJumpMovementSpeed"": 16,
                    ""_noteJumpStartBeatOffset"": 1,
                    ""_beatmapColorSchemeIdx"": 0,
                    ""_environmentNameIdx"": 0
                },
                {
                    ""_difficulty"": ""ExpertPlus"",
                    ""_difficultyRank"": 9,
                    ""_beatmapFilename"": ""ExpertPlus.dat"",
                    ""_noteJumpMovementSpeed"": 18,
                    ""_noteJumpStartBeatOffset"": 0.5,
                    ""_beatmapColorSchemeIdx"": 0,
                    ""_environmentNameIdx"": 0
                }
            ]
        }
    ],
    ""_customData"": {
        ""_contributors"": [
            {
                ""_name"": ""Bullet"",
                ""_role"": ""Everything"",
                ""_iconPath"": ""Bullet.png""
            }
        ]
    }
}
";
        
        private const string v4FileInfo = @"
{
    ""version"": ""4.0.0"",
    ""song"": {
        ""title""   : ""Magic""            ,
        ""subTitle"": ""ft. Meredith Bull"",
        ""author""  : ""Jaroslav Beck""
    },
    ""audio"": {
        ""songFilename"": ""song.ogg"",
        ""songDuration"": 202,
        ""audioDataFilename"": ""BPMInfo.dat"",
        ""bpm"": 208,
        ""lufs"": 0,
        ""previewStartTime"": 0,
        ""previewDuration"": 0
    },
    ""songPreviewFilename"": ""song.ogg"",
    ""coverImageFilename"": ""cover.png"",
    ""environmentNames"": [""WeaveEnvironment"", ""GlassDesertEnvironment""],
    ""colorSchemes"": [
        {
            ""useOverride"": true,
            ""colorSchemeName"": ""Weave"",
            ""saberAColor"": ""#C81414FF"",
            ""saberBColor"": ""#288ED2FF"",
            ""obstaclesColor"": ""#FF3030FF"",
            ""environmentColor0"": ""#D91616FF"",
            ""environmentColor1"": ""#30ACFFFF"",
            ""environmentColor0Boost"": ""#D216D9FF"",
            ""environmentColor1Boost"": ""#888888FF""
            // ^ This is modified from the wiki as this was different from v2 example
        }
    ],
    ""difficultyBeatmaps"": [
        {
            ""characteristic"": ""Standard"",
            ""difficulty"": ""Easy"",
            ""beatmapAuthors"": {
                ""mappers"" : [""Freeek""],
                ""lighters"": [""Freeek""]
            },
            ""environmentNameIdx"": 0,
            ""beatmapColorSchemeIdx"": 0,
            ""noteJumpMovementSpeed"": 10,
            ""noteJumpStartBeatOffset"": 0,
            ""beatmapDataFilename"": ""Easy.dat"",
            ""lightshowDataFilename"": ""Lightshow.dat"",
            ""customData"": {
                ""oneSaber"" : true,
                ""showRotationNoteSpawnLines"" : true,
				""difficultyLabel"": ""ACustomLabel"",
                ""warnings"": [
                    ""Warning1"",
                    ""Warning2""
                ],
				""information"": [
                    ""Info""
                ],
				""suggestions"": [
                    ""Chroma""
                ],
				""requirements"": [
					""Noodle Extensions""
				]
            }
        },
        {
            ""characteristic"": ""Standard"",
            ""difficulty"": ""Normal"",
            ""beatmapAuthors"": {
                ""mappers"" : [""Freeek""],
                ""lighters"": [""Freeek""]
            },
            ""environmentNameIdx"": 0,
            ""beatmapColorSchemeIdx"": 0,
            ""noteJumpMovementSpeed"": 10,
            ""noteJumpStartBeatOffset"": 0,
            ""beatmapDataFilename"": ""Normal.dat"",
            ""lightshowDataFilename"": ""Lightshow.dat""
        },
        {
            ""characteristic"": ""Standard"",
            ""difficulty"": ""Hard"",
            ""beatmapAuthors"": {
                ""mappers"" : [""Freeek""],
                ""lighters"": [""Freeek""]
            },
            ""environmentNameIdx"": 0,
            ""beatmapColorSchemeIdx"": 0,
            ""noteJumpMovementSpeed"": 10,
            ""noteJumpStartBeatOffset"": 0,
            ""beatmapDataFilename"": ""Hard.dat"",
            ""lightshowDataFilename"": ""Lightshow.dat""
        },
        {
            ""characteristic"": ""Standard"",
            ""difficulty"": ""Expert"",
            ""beatmapAuthors"": {
                ""mappers"" : [""Freeek""],
                ""lighters"": [""Freeek""]
            },
            ""environmentNameIdx"": 0,
            ""beatmapColorSchemeIdx"": 0,
            ""noteJumpMovementSpeed"": 16,
            ""noteJumpStartBeatOffset"": 1,
            ""beatmapDataFilename"": ""Expert.dat"",
            ""lightshowDataFilename"": ""Lightshow.dat""
        },
        {
            ""characteristic"": ""Standard"",
            ""difficulty"": ""ExpertPlus"",
            ""beatmapAuthors"": {
                ""mappers"" : [""Freeek""],
                ""lighters"": [""Freeek""]
            },
            ""environmentNameIdx"": 0,
            ""beatmapColorSchemeIdx"": 0,
            ""noteJumpMovementSpeed"": 18,
            ""noteJumpStartBeatOffset"": 0.5,
            ""beatmapDataFilename"": ""ExpertPlus.dat"",
            ""lightshowDataFilename"": ""LightshowPlus.dat""
        }
    ],
    ""customData"": {
        ""contributors"": [
            {
                ""name"": ""Bullet"",
                ""role"": ""Everything"",
                ""iconPath"": ""Bullet.png""
            }
        ]
    }
}
";

        [Test]
        public void V2_GetFromJson()
        {
            var info = V2Info.GetFromJson(JSONNode.Parse(v2FileInfo));
            AssertV2Info(info);
        }

        [Test]
        public void V2_GetOutputJson()
        {
            var info = V2Info.GetFromJson(JSONNode.Parse(v2FileInfo));
            var output = V2Info.GetOutputJson(info);
            var reparsed = V2Info.GetFromJson(output);
            
            AssertV2Info(reparsed); // This should have the same stuff
        }
        
        [Test]
        public void V4_GetFromJson()
        {
            var info = V4Info.GetFromJson(JSONNode.Parse(v4FileInfo));
            AssertV4Info(info);
        }

        [Test]
        public void V4_GetOutputJson()
        {
            var info = V4Info.GetFromJson(JSONNode.Parse(v4FileInfo));
            var output = V4Info.GetOutputJson(info);
            var reparsed = V4Info.GetFromJson(output);
            
            AssertV4Info(reparsed); // This should have the same stuff
        }

        private void AssertV2Info(BaseInfo info)
        {
            Assert.AreEqual("2.1.0", info.Version);
            
            AssertCommonInfo(info);
            
            Assert.AreEqual("Freeek", info.LevelAuthorName);

            Assert.AreEqual("Magic.wav", info.SongFilename);

            Assert.AreEqual("WeaveEnvironment", info.EnvironmentName);
            Assert.AreEqual("GlassDesertEnvironment", info.AllDirectionsEnvironmentName);
        }

        private void AssertV4Info(BaseInfo info)
        {
            Assert.AreEqual("4.0.0", info.Version);
            
            AssertCommonInfo(info);
            
            Assert.AreEqual("BPMInfo.dat", info.AudioDataFilename);
            Assert.AreEqual(0, info.Lufs);

            Assert.AreEqual("song.ogg", info.SongFilename);
            Assert.AreEqual("song.ogg", info.SongPreviewFilename);

            foreach (var difficulty in info.DifficultySets.SelectMany(x => x.Difficulties))
            {
                Assert.AreEqual(1, difficulty.Mappers.Count);
                Assert.AreEqual("Freeek", difficulty.Mappers[0]); 
                
                Assert.AreEqual(1, difficulty.Lighters.Count);
                Assert.AreEqual("Freeek", difficulty.Lighters[0]);

                var lightshowFileName = difficulty.Difficulty == "ExpertPlus" ? "LightshowPlus.dat" : "Lightshow.dat";
                Assert.AreEqual(lightshowFileName, difficulty.LightshowFileName);
            }
        }

        private void AssertCommonInfo(BaseInfo info)
        {
            Assert.AreEqual("Magic", info.SongName);
            Assert.AreEqual("ft. Meredith Bull", info.SongSubName);
            Assert.AreEqual("Jaroslav Beck", info.SongAuthorName);

            Assert.AreEqual(208f, info.BeatsPerMinute);

            Assert.AreEqual(0f, info.SongTimeOffset);
            Assert.AreEqual(0f, info.Shuffle);
            Assert.AreEqual(0f, info.ShufflePeriod);

            Assert.AreEqual(0f, info.PreviewStartTime);
            Assert.AreEqual(0f, info.PreviewDuration);

            Assert.AreEqual("cover.png", info.CoverImageFilename);

            Assert.AreEqual(2, info.EnvironmentNames.Count);
            Assert.AreEqual("WeaveEnvironment", info.EnvironmentNames[0]);
            Assert.AreEqual("GlassDesertEnvironment", info.EnvironmentNames[1]);
            
            Assert.AreEqual(1, info.ColorSchemes.Count);

            var colorScheme = info.ColorSchemes[0];
            Assert.AreEqual(true, colorScheme.UseOverride);
            Assert.AreEqual("Weave", colorScheme.ColorSchemeName);
            AssertColorsAreEqual(new Color(0.7843137f, 0.07843138f, 0.07843138f), colorScheme.SaberAColor);
            AssertColorsAreEqual(new Color(0.1568627f, 0.55686270f, 0.82352940f), colorScheme.SaberBColor);
            AssertColorsAreEqual(new Color(0.8500000f, 0.08499997f, 0.08499997f), colorScheme.EnvironmentColor0);
            AssertColorsAreEqual(new Color(0.1882353f, 0.67529400f, 1.00000000f), colorScheme.EnvironmentColor1);
            AssertColorsAreEqual(new Color(1.0000000f, 0.18823530f, 0.18823530f), colorScheme.ObstaclesColor);
            AssertColorsAreEqual(new Color(0.8218409f, 0.08627451f, 0.85098040f), colorScheme.EnvironmentColor0Boost);
            AssertColorsAreEqual(new Color(0.5320754f, 0.53207540f, 0.53207540f), colorScheme.EnvironmentColor1Boost);
            
            Assert.AreEqual(1, info.DifficultySets.Count);

            var difficultySet = info.DifficultySets[0];
            Assert.AreEqual("Standard", difficultySet.Characteristic);
            Assert.AreEqual(5, difficultySet.Difficulties.Count);

            var easyDifficulty = difficultySet.Difficulties[0];
            Assert.AreEqual("Easy", easyDifficulty.Difficulty);
            Assert.AreEqual(1, easyDifficulty.DifficultyRank);
            Assert.AreEqual("Easy.dat", easyDifficulty.BeatmapFileName);
            Assert.AreEqual(10, easyDifficulty.NoteJumpSpeed);
            Assert.AreEqual(0, easyDifficulty.NoteStartBeatOffset);
            Assert.AreEqual(0, easyDifficulty.ColorSchemeIndex);
            Assert.AreEqual(0, easyDifficulty.EnvironmentNameIndex);
            
            // Custom properties for Easy
            Assert.IsTrue(easyDifficulty.CustomOneSaberFlag);
            Assert.IsTrue(easyDifficulty.CustomShowRotationNoteSpawnLinesFlag);
            Assert.AreEqual("ACustomLabel", easyDifficulty.CustomLabel);
            
            Assert.AreEqual(1, easyDifficulty.CustomInformation.Count);
            Assert.AreEqual("Info", easyDifficulty.CustomInformation[0]);
            Assert.AreEqual(2, easyDifficulty.CustomWarnings.Count);
            Assert.AreEqual("Warning1", easyDifficulty.CustomWarnings[0]);
            Assert.AreEqual("Warning2", easyDifficulty.CustomWarnings[1]);
            Assert.AreEqual(1, easyDifficulty.CustomSuggestions.Count);
            Assert.AreEqual("Chroma", easyDifficulty.CustomSuggestions[0]);
            Assert.AreEqual(1, easyDifficulty.CustomRequirements.Count);
            Assert.AreEqual("Noodle Extensions", easyDifficulty.CustomRequirements[0]);
            
            var normalDifficulty = difficultySet.Difficulties[1];
            Assert.AreEqual("Normal", normalDifficulty.Difficulty);
            Assert.AreEqual(3, normalDifficulty.DifficultyRank);
            Assert.AreEqual("Normal.dat", normalDifficulty.BeatmapFileName);
            Assert.AreEqual(10, normalDifficulty.NoteJumpSpeed);
            Assert.AreEqual(0, normalDifficulty.NoteStartBeatOffset);
            Assert.AreEqual(0, normalDifficulty.ColorSchemeIndex);
            Assert.AreEqual(0, normalDifficulty.EnvironmentNameIndex);
            
            // Non-existent custom properties for normal
            Assert.IsNull(normalDifficulty.CustomOneSaberFlag);
            Assert.IsNull(normalDifficulty.CustomShowRotationNoteSpawnLinesFlag);
            Assert.IsTrue(string.IsNullOrWhiteSpace(normalDifficulty.CustomLabel));
            
            Assert.AreEqual(0, normalDifficulty.CustomInformation.Count);
            Assert.AreEqual(0, normalDifficulty.CustomWarnings.Count);
            Assert.AreEqual(0, normalDifficulty.CustomSuggestions.Count);
            Assert.AreEqual(0, normalDifficulty.CustomRequirements.Count);

            var hardDifficulty = difficultySet.Difficulties[2];
            Assert.AreEqual("Hard", hardDifficulty.Difficulty);
            Assert.AreEqual(5, hardDifficulty.DifficultyRank);
            Assert.AreEqual("Hard.dat", hardDifficulty.BeatmapFileName);
            Assert.AreEqual(10, hardDifficulty.NoteJumpSpeed);
            Assert.AreEqual(0, hardDifficulty.NoteStartBeatOffset);
            Assert.AreEqual(0, hardDifficulty.ColorSchemeIndex);
            Assert.AreEqual(0, hardDifficulty.EnvironmentNameIndex);
            
            var expertDifficulty = difficultySet.Difficulties[3];
            Assert.AreEqual("Expert", expertDifficulty.Difficulty);
            Assert.AreEqual(7, expertDifficulty.DifficultyRank);
            Assert.AreEqual("Expert.dat", expertDifficulty.BeatmapFileName);
            Assert.AreEqual(16, expertDifficulty.NoteJumpSpeed);
            Assert.AreEqual(1, expertDifficulty.NoteStartBeatOffset);
            Assert.AreEqual(0, expertDifficulty.ColorSchemeIndex);
            Assert.AreEqual(0, expertDifficulty.EnvironmentNameIndex);
            
            var expertPlusDifficulty = difficultySet.Difficulties[4];
            Assert.AreEqual("ExpertPlus", expertPlusDifficulty.Difficulty);
            Assert.AreEqual(9, expertPlusDifficulty.DifficultyRank);
            Assert.AreEqual("ExpertPlus.dat", expertPlusDifficulty.BeatmapFileName);
            Assert.AreEqual(18, expertPlusDifficulty.NoteJumpSpeed);
            Assert.AreEqual(0.5f, expertPlusDifficulty.NoteStartBeatOffset);
            Assert.AreEqual(0, expertPlusDifficulty.ColorSchemeIndex);
            Assert.AreEqual(0, expertPlusDifficulty.EnvironmentNameIndex);
            
            // CustomData properties
            Assert.AreEqual(1, info.CustomContributors.Count);

            var contributor = info.CustomContributors[0];
            Assert.AreEqual("Bullet", contributor.Name);
            Assert.AreEqual("Everything", contributor.Role);
            Assert.AreEqual("Bullet.png", contributor.LocalImageLocation);
        }

        private void AssertColorsAreEqual(Color expected, Color actual)
        {
            // Because v4 saves color schemes as a Html string, we can expect an error of ±0.5 / 255;
            var delta = 0.5f / 255;
            
            Assert.AreEqual(expected.r, actual.r, delta, "red component");
            Assert.AreEqual(expected.g, actual.g, delta, "green component");
            Assert.AreEqual(expected.b, actual.b, delta, "blue component");
            Assert.AreEqual(expected.a, actual.a, delta, "alpha component");
        }
        
    }
}
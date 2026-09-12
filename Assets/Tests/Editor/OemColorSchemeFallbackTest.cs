using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class OemColorSchemeFallbackTest : TestBase
    {
        private const string V4InfoWithOemColorScheme = @"
{
    ""version"": ""4.0.1"",
    ""song"": { ""title"": ""Stop Your Self Control"", ""subTitle"": """", ""author"": ""MARKO POLO"" },
    ""audio"": {
        ""songFilename"": ""song.egg"", ""songDuration"": 317.5, ""audioDataFilename"": ""AudioData.dat"",
        ""bpm"": 157, ""lufs"": 0, ""previewStartTime"": 67, ""previewDuration"": 20
    },
    ""songPreviewFilename"": ""song.egg"",
    ""coverImageFilename"": ""cover.png"",
    ""environmentNames"": [""SkrillexEnvironment""],
    ""colorSchemes"": [
        {
            ""colorSchemeName"": ""not kaleidoscope"", ""overrideNotes"": true,
            ""saberAColor"": ""A82020FF"", ""saberBColor"": ""484848FF"", ""obstaclesColor"": ""404040FF"",
            ""overrideLights"": true, ""environmentColor0"": ""A82020FF"", ""environmentColor1"": ""7A67FFFF"",
            ""environmentColor0Boost"": ""EEA31FFF"", ""environmentColor1Boost"": ""7E0089FF""
        }
    ],
    ""difficultyBeatmaps"": [
        {
            ""characteristic"": ""Standard"", ""difficulty"": ""ExpertPlus"",
            ""beatmapAuthors"": { ""mappers"": [""WearyOlly""], ""lighters"": [""WearyOlly""] },
            ""environmentNameIdx"": 0, ""beatmapColorSchemeIdx"": 0,
            ""noteJumpMovementSpeed"": 17, ""noteJumpStartBeatOffset"": -0.5,
            ""beatmapDataFilename"": ""ExpertPlusStandard.dat"", ""lightshowDataFilename"": ""Lightshow.dat""
        }
    ]
}";

        private const string V2InfoWithOemColorScheme = @"
{
    ""_version"": ""2.1.0"",
    ""_songName"": ""Stop Your Self Control"",
    ""_songSubName"": """",
    ""_songAuthorName"": ""MARKO POLO"",
    ""_levelAuthorName"": ""WearyOlly"",
    ""_beatsPerMinute"": 157,
    ""_songFilename"": ""song.egg"",
    ""_coverImageFilename"": ""cover.png"",
    ""_environmentName"": ""SkrillexEnvironment"",
    ""_environmentNames"": [""SkrillexEnvironment""],
    ""_colorSchemes"": [
        {
            ""useOverride"": true,
            ""colorScheme"": {
                ""colorSchemeId"": ""not kaleidoscope"",
                ""saberAColor"": { ""r"": 0.6588235, ""g"": 0.1254902, ""b"": 0.1254902, ""a"": 1 },
                ""saberBColor"": { ""r"": 0.2823529, ""g"": 0.2823529, ""b"": 0.2823529, ""a"": 1 },
                ""obstaclesColor"": { ""r"": 0.2509804, ""g"": 0.2509804, ""b"": 0.2509804, ""a"": 1 },
                ""environmentColor0"": { ""r"": 0.6588235, ""g"": 0.1254902, ""b"": 0.1254902, ""a"": 1 },
                ""environmentColor1"": { ""r"": 0.4784314, ""g"": 0.4039216, ""b"": 1, ""a"": 1 },
                ""environmentColor0Boost"": { ""r"": 0.9333333, ""g"": 0.6392157, ""b"": 0.1215686, ""a"": 1 },
                ""environmentColor1Boost"": { ""r"": 0.4941176, ""g"": 0, ""b"": 0.5372549, ""a"": 1 }
            }
        }
    ],
    ""_difficultyBeatmapSets"": [
        {
            ""_beatmapCharacteristicName"": ""Standard"",
            ""_difficultyBeatmaps"": [
                {
                    ""_difficulty"": ""ExpertPlus"", ""_beatmapFilename"": ""ExpertPlusStandard.dat"",
                    ""_environmentNameIdx"": 0, ""_beatmapColorSchemeIdx"": 0,
                    ""_noteJumpMovementSpeed"": 17, ""_noteJumpStartBeatOffset"": -0.5
                }
            ]
        }
    ]
}";

        // The UI must replace the environment palette with an OEM Info.dat palette when a map supplies no Chroma colors.
        [Test]
        public void V4OemColorSchemeReplacesEnvironmentPaletteWhenDifficultyHasNoCustomColors()
        {
            var info = V4Info.GetFromJson(JSONNode.Parse(V4InfoWithOemColorScheme));
            AssertOemPaletteReplacesEnvironmentPalette(info);
        }

        // V3 beatmaps retain the v2 Info.dat schema, including the _colorSchemes array and index reference.
        [Test]
        public void V3InfoDatOemColorSchemeReplacesEnvironmentPaletteWhenDifficultyHasNoCustomColors()
        {
            var info = V2Info.GetFromJson(JSONNode.Parse(V2InfoWithOemColorScheme));
            AssertOemPaletteReplacesEnvironmentPalette(info);
        }

        // Both Info.dat schemas select an OEM palette through the difficulty's color-scheme index.
        private static void AssertOemPaletteReplacesEnvironmentPalette(BaseInfo info)
        {
            var difficulty = info.DifficultySets[0].Difficulties[0];
            Assert.That(difficulty.CustomEnvColorLeft, Is.Null);
            BeatSaberSongContainer.Instance.Info = info;
            BeatSaberSongContainer.Instance.MapDifficultyInfo = difficulty;

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var mapLoader = Object.FindAnyObjectByType<LoadInitialMap>();
            var customColors = Object.FindAnyObjectByType<CustomColorsUIController>();
            Assert.That(context, Is.Not.Null);
            Assert.That(mapLoader, Is.Not.Null);
            Assert.That(customColors, Is.Not.Null);

            context.ColorScheme.EnvironmentLeftColor = Color.green;
            context.NotifyColorScheme();

            mapLoader.PopulateColorsFromMapInfo();
            context.NotifyColorScheme();

            AssertColorApproximately(
                (Color)new Color32(0xA8, 0x20, 0x20, 0xFF),
                context.ColorScheme.EnvironmentLeftColor);
        }

        // JSON decimal colors and Color32 values can differ by a few floating-point units without changing their displayed RGBA value.
        private static void AssertColorApproximately(Color expected, Color actual)
        {
            const float colorTolerance = 0.00001f;
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(colorTolerance), "red component");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(colorTolerance), "green component");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(colorTolerance), "blue component");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(colorTolerance), "alpha component");
        }
    }
}

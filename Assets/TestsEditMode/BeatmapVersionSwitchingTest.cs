using System;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Info;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace TestsEditMode
{
    public class BeatmapVersionSwitchingTest
    {
        private const string fileJson = @"
{
    ""version"": ""3.3.0"",
    ""colorNotes"": [
        {
            ""b"": 10,
            ""x"": 1,
            ""y"": 0,
            ""c"": 0,
            ""d"": 1,
            ""a"": 0,
            ""customData"": {
                ""coordinates"": [5,6],
                ""somePropertyThatCMShouldNotTouch"" : ""HelloWorld!""
            }
        }
    ],
    ""customData"": {
        ""foo"": ""bar"",
        ""time"": ""123.456""
    }
}";


        // For use in PlayMode
        public void TestEverything()
        {
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void RemoveEmptyCustomDataFromOutputTest()
        {
            Settings.Instance.MapVersion = 3;
            var difficulty = V3Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");

            difficulty.Notes[0].CustomData.Remove("coordinates");
            difficulty.Notes[0].CustomData.Remove("somePropertyThatCMShouldNotTouch");

            Assert.AreEqual(0, difficulty.Notes[0].CustomData.Children.Count());

            difficulty.CustomData.Remove("foo");
            difficulty.CustomData.Remove("time");
            difficulty.Time = 0;
            Assert.AreEqual(0, difficulty.CustomData.Children.Count());

            var outputJson = V3Difficulty.GetOutputJson(difficulty);
            Assert.IsFalse(outputJson["colorNotes"].HasKey("customData"));
            Assert.IsFalse(outputJson.HasKey("customData"));
        }
        

        [Test]
        public void NoteV3ToV2ToV3CustomDataTest()
        {
            Settings.Instance.MapVersion = 3;
            var difficulty = V3Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");

            Assert.AreEqual("3.3.0", difficulty.Version);
            Assert.AreEqual(1, difficulty.Notes.Count);
            
            // Making sure the CustomData is loaded as expected
            Assert.IsTrue(difficulty.Notes[0].CustomData != null);
            Assert.AreEqual(2, difficulty.Notes[0].CustomData.Children.Count());
            Assert.AreEqual("HelloWorld!", difficulty.Notes[0].CustomData["somePropertyThatCMShouldNotTouch"].Value);
            Assert.AreEqual(5, difficulty.Notes[0].CustomData["coordinates"][0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomData["coordinates"][1].AsInt);
            Assert.IsTrue(difficulty.Notes[0].CustomCoordinate is JSONArray);
            Assert.AreEqual(5, difficulty.Notes[0].CustomCoordinate[0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomCoordinate[1].AsInt);
            
            Assert.AreEqual("bar", difficulty.CustomData["foo"].Value);
            Assert.AreEqual(123.456f, difficulty.Time, 0.001);
            Assert.IsFalse(difficulty.CustomData.HasKey("time"));
            
            difficulty.ConvertCustomDataVersion(fromVersion: 3, toVersion: 2);
            
            // Check CustomData
            Assert.IsTrue(difficulty.Notes[0].CustomData != null);
            Assert.AreEqual(2, difficulty.Notes[0].CustomData.Children.Count());
            Assert.AreEqual("HelloWorld!", difficulty.Notes[0].CustomData["somePropertyThatCMShouldNotTouch"].Value);
            Assert.AreEqual(5, difficulty.Notes[0].CustomData["_position"][0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomData["_position"][1].AsInt);
            Assert.IsTrue(difficulty.Notes[0].CustomCoordinate is JSONArray);
            Assert.AreEqual(5, difficulty.Notes[0].CustomCoordinate[0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomCoordinate[1].AsInt);
            
            Assert.AreEqual("bar", difficulty.CustomData["foo"].Value);
            Assert.AreEqual(123.456f, difficulty.Time, 0.001);
            Assert.IsFalse(difficulty.CustomData.HasKey("time"));
            Assert.IsFalse(difficulty.CustomData.HasKey("_time"));
            
            difficulty.ConvertCustomDataVersion(fromVersion: 2, toVersion: 3);
            
            // Back to original
            Assert.IsTrue(difficulty.Notes[0].CustomData != null);
            Assert.AreEqual(2, difficulty.Notes[0].CustomData.Children.Count());
            Assert.AreEqual("HelloWorld!", difficulty.Notes[0].CustomData["somePropertyThatCMShouldNotTouch"].Value);
            Assert.AreEqual(5, difficulty.Notes[0].CustomData["coordinates"][0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomData["coordinates"][1].AsInt);
            Assert.IsTrue(difficulty.Notes[0].CustomCoordinate is JSONArray);
            Assert.AreEqual(5, difficulty.Notes[0].CustomCoordinate[0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomCoordinate[1].AsInt);
            
            Assert.AreEqual("bar", difficulty.CustomData["foo"].Value);
            Assert.AreEqual(123.456f, difficulty.Time, 0.001);
            Assert.IsFalse(difficulty.CustomData.HasKey("time"));
            Assert.IsFalse(difficulty.CustomData.HasKey("_time"));
        }

        // V4 upper-lane walls currently become Mapping Extensions walls after a declined V3 compatibility conversion,
        // so this verifies the safe vanilla fallback moves every unsupported upper lane to y=0.
        [TestCase(3)]
        [TestCase(4)]
        public void DecliningV4UpperLaneWallConversionMovesWallToV3Floor(int posY)
        {
            RunWithV4UpperLaneWall(
                posY,
                (controller, songContainer, difficulty, wall) =>
                {
                    InvokePrivateConversion(controller, "OnChangeToV3WithoutUpperWallMod");

                    Assert.AreEqual(3, Settings.Instance.MapVersion);
                    Assert.AreEqual(0, wall.PosY);
                    Assert.AreEqual(
                        RequirementCheck.RequirementType.None,
                        new MappingExtensionsReq().IsRequiredOrSuggested(songContainer.MapDifficultyInfo, difficulty));
                });
        }

        // BeatToTheFuture supports V4 upper-lane wall coordinates directly, so this verifies the choice preserves the
        // wall, adds BeatToTheFuture, and suppresses the obsolete Mapping Extensions requirement.
        [TestCase(3)]
        [TestCase(4)]
        public void BeatToTheFutureV4UpperLaneWallConversionPreservesWallWithoutMappingExtensions(int posY)
        {
            RunWithV4UpperLaneWall(
                posY,
                (controller, songContainer, difficulty, wall) =>
                {
                    InvokePrivateConversion(controller, "OnChangeToV3WithBeatToTheFuture");

                    Assert.AreEqual(3, Settings.Instance.MapVersion);
                    Assert.AreEqual(posY, wall.PosY);
                    Assert.Contains("BeatToTheFuture", songContainer.MapDifficultyInfo.CustomRequirements);
                    Assert.AreEqual(
                        RequirementCheck.RequirementType.Requirement,
                        new BeatToTheFutureReq().IsRequiredOrSuggested(songContainer.MapDifficultyInfo, difficulty));
                    Assert.AreEqual(
                        RequirementCheck.RequirementType.None,
                        new MappingExtensionsReq().IsRequiredOrSuggested(songContainer.MapDifficultyInfo, difficulty));
                });
        }

        // Noodle Extensions needs custom wall coordinates instead of unsupported raw V3 y values, so this verifies
        // the choice moves the raw wall to y=0, preserves its visual y coordinate, and adds only Noodle Extensions.
        [TestCase(3)]
        [TestCase(4)]
        public void NoodleExtensionsV4UpperLaneWallConversionCreatesNoodleWall(int posY)
        {
            RunWithV4UpperLaneWall(
                posY,
                (controller, songContainer, difficulty, wall) =>
                {
                    InvokePrivateConversion(controller, "OnChangeToV3WithNoodleExtensions");

                    Assert.AreEqual(3, Settings.Instance.MapVersion);
                    Assert.AreEqual(0, wall.PosY);
                    Assert.IsNotNull(wall.CustomCoordinate);
                    Assert.AreEqual(-1, wall.CustomCoordinate[0].AsInt);
                    Assert.AreEqual(posY, wall.CustomCoordinate[1].AsInt);
                    Assert.Contains("Noodle Extensions", songContainer.MapDifficultyInfo.CustomRequirements);
                    Assert.IsFalse(songContainer.MapDifficultyInfo.CustomRequirements.Contains("Mapping Extensions"));
                    Assert.AreEqual(
                        RequirementCheck.RequirementType.None,
                        new MappingExtensionsReq().IsRequiredOrSuggested(songContainer.MapDifficultyInfo, difficulty));
                });
        }

        // VNJS and unsupported upper-lane walls are independent V4-to-V3 compatibility decisions, so this verifies
        // a map containing both still reaches the wall prompt after the existing VNJS prompt is required.
        [Test]
        public void V4MapWithVNJSAndUpperLaneWallsNeedsBothConversionPrompts()
        {
            RunWithV4UpperLaneWall(
                3,
                (controller, songContainer, difficulty, wall) =>
                {
                    difficulty.NJSEvents.Add(new BaseNJSEvent());

                    Assert.IsTrue(InvokePrivateBoolean(controller, "ShouldPromptForV4VNJS"));
                    Assert.IsTrue(InvokePrivateBoolean(controller, "ShouldPromptForV4UpperWalls"));
                });
        }

        // An existing BeatToTheFuture requirement makes raw y=3/4 walls compatible before the wall decision is reached,
        // so this verifies the wall prompt gate skips a redundant compatibility question.
        [Test]
        public void ExistingBeatToTheFutureRequirementSkipsV4UpperLaneWallPrompt()
        {
            RunWithV4UpperLaneWall(
                4,
                (controller, songContainer, difficulty, wall) =>
                {
                    difficulty.NJSEvents.Add(new BaseNJSEvent());
                    songContainer.MapDifficultyInfo.CustomRequirements.Add("BeatToTheFuture");

                    Assert.IsTrue(InvokePrivateBoolean(controller, "ShouldPromptForV4VNJS"));
                    Assert.IsFalse(InvokePrivateBoolean(controller, "ShouldPromptForV4UpperWalls"));
                });
        }

        // The migration warning is user-facing policy, so lock its exact wording to prevent Mapping Extensions from
        // silently returning to an ordinary automatic requirement without explaining its unsupported status.
        [Test]
        public void MappingExtensionsWallMigrationUsesUnsupportedModWarning()
        {
            var messageField = typeof(BeatmapVersionSwitchInputController).GetField(
                "MappingExtensionsWallPromptMessage",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(messageField);
            Assert.AreEqual(
                "Mapping Extensions is no longer supported in latest Beat Saber versions, and appears unmaintained " +
                "moving forward (so likely never will be). Do you want to convert your modded walls from " +
                "MappingExtensions to Noodle?",
                messageField.GetRawConstantValue());
        }

        // Every legacy Mapping Extensions wall encoding needs the migration prompt, including encoded type geometry
        // that the old automatic requirement check did not inspect consistently.
        [TestCase(3, 1500, 0, 1, 5, int.MinValue, TestName = "ME prompt: positive encoded x")]
        [TestCase(3, -1500, 0, 1, 5, int.MinValue, TestName = "ME prompt: negative encoded x")]
        [TestCase(3, 0, 0, 1500, 5, int.MinValue, TestName = "ME prompt: positive encoded width")]
        [TestCase(3, 0, 0, -1500, 5, int.MinValue, TestName = "ME prompt: negative encoded width")]
        [TestCase(3, 0, -1, 1, 1, int.MinValue, TestName = "ME prompt: negative y")]
        [TestCase(3, 0, 5, 1, 1, int.MinValue, TestName = "ME prompt: y above V4 upper lanes")]
        [TestCase(3, 0, 0, 1, 6, int.MinValue, TestName = "ME prompt: oversized height")]
        [TestCase(3, 0, 0, 1, -1500, int.MinValue, TestName = "ME prompt: negative encoded height")]
        [TestCase(3, 0, 0, 1, 5, 750, TestName = "ME prompt: encoded start height type")]
        [TestCase(3, 0, 0, 1, 5, 2000, TestName = "ME prompt: encoded height type")]
        [TestCase(3, 0, 0, 1, 5, 5001, TestName = "ME prompt: encoded start and wall height type")]
        [TestCase(2, -1, 0, 1, 5, int.MinValue, TestName = "ME prompt: V2 out-of-lane x")]
        [TestCase(2, 4, 0, 1, 5, int.MinValue, TestName = "ME prompt: V2 positive out-of-lane x")]
        public void MappingExtensionsWallEncodingTriggersMigrationPrompt(
            int mapVersion,
            int posX,
            int posY,
            int width,
            int height,
            int type)
        {
            var wall = CreateMappingExtensionsWall(posX, posY, width, height, type);
            RunWithWall(
                mapVersion,
                wall,
                (controller, songContainer, difficulty, testWall) =>
                {
                    Assert.IsTrue(InvokePrivateBoolean(controller, "ShouldPromptForMappingExtensionsWalls"));
                });
        }

        // BeatToTheFuture owns raw V4 upper lanes and vanilla/Noodle walls are already supported, so none of these
        // walls should be misrouted into the Mapping Extensions migration prompt.
        [TestCase(3)]
        [TestCase(4)]
        public void SupportedUpperLaneWallsDoNotTriggerMappingExtensionsMigration(int posY)
        {
            var wall = CreateMappingExtensionsWall(0, posY, 1, 1, int.MinValue);
            RunWithWall(
                3,
                wall,
                (controller, songContainer, difficulty, testWall) =>
                {
                    Assert.IsFalse(InvokePrivateBoolean(controller, "ShouldPromptForMappingExtensionsWalls"));
                });
        }

        // Vanilla walls, existing Noodle walls, and a stale requirement without matching wall data need no migration
        // prompt because there is no Mapping Extensions wall geometry for the converter to replace.
        [TestCase(false, false, TestName = "ME prompt skipped: vanilla wall")]
        [TestCase(true, false, TestName = "ME prompt skipped: existing Noodle wall")]
        [TestCase(false, true, TestName = "ME prompt skipped: stale requirement only")]
        public void MapsWithoutMappingExtensionsWallGeometryDoNotPrompt(bool makeNoodle, bool addStaleRequirement)
        {
            var wall = CreateMappingExtensionsWall(makeNoodle ? 1500 : 1, 0, 1, 5, int.MinValue);
            RunWithWall(
                3,
                wall,
                (controller, songContainer, difficulty, testWall) =>
                {
                    if (makeNoodle)
                    {
                        testWall.CustomCoordinate = new JSONArray { [0] = -1f, [1] = 0f };
                        testWall.CustomSize = new JSONArray { [0] = 1f, [1] = 5f };
                        testWall.WriteCustom();
                    }

                    if (addStaleRequirement)
                    {
                        songContainer.MapDifficultyInfo.CustomRequirements.Add("Mapping Extensions");
                    }

                    Assert.IsFalse(InvokePrivateBoolean(controller, "ShouldPromptForMappingExtensionsWalls"));
                });
        }

        // Converting any legacy encoding must preserve its rendered bounds exactly in Noodle coordinates and size,
        // normalize the raw wall fields, and replace the obsolete requirement without leaving Mapping Extensions data.
        [TestCase(3, 1500, 0, 1, 5, int.MinValue, TestName = "ME conversion: positive encoded x")]
        [TestCase(3, -1500, 0, 1, 5, int.MinValue, TestName = "ME conversion: negative encoded x")]
        [TestCase(3, 0, 0, 1500, 5, int.MinValue, TestName = "ME conversion: positive encoded width")]
        [TestCase(3, 0, 0, -1500, 5, int.MinValue, TestName = "ME conversion: negative encoded width")]
        [TestCase(3, 0, -1, 1, 1, int.MinValue, TestName = "ME conversion: negative y")]
        [TestCase(3, 0, 5, 1, 1, int.MinValue, TestName = "ME conversion: y above V4 upper lanes")]
        [TestCase(3, 0, 0, 1, 6, int.MinValue, TestName = "ME conversion: oversized height")]
        [TestCase(3, 0, 0, 1, -1500, int.MinValue, TestName = "ME conversion: negative encoded height")]
        [TestCase(3, 0, 0, 1, 5, 750, TestName = "ME conversion: encoded start height type")]
        [TestCase(3, 0, 0, 1, 5, 2000, TestName = "ME conversion: encoded height type")]
        [TestCase(3, 0, 0, 1, 5, 5001, TestName = "ME conversion: encoded start and wall height type")]
        [TestCase(2, -1, 0, 1, 5, int.MinValue, TestName = "ME conversion: V2 out-of-lane x")]
        [TestCase(2, 4, 0, 1, 5, int.MinValue, TestName = "ME conversion: V2 positive out-of-lane x")]
        public void MappingExtensionsWallMigrationPreservesShapeAsNoodle(
            int mapVersion,
            int posX,
            int posY,
            int width,
            int height,
            int type)
        {
            var wall = CreateMappingExtensionsWall(posX, posY, width, height, type);
            RunWithWall(
                mapVersion,
                wall,
                (controller, songContainer, difficulty, testWall) =>
                {
                    songContainer.MapDifficultyInfo.CustomRequirements.Add("Mapping Extensions");
                    var originalShape = testWall.GetShape();

                    InvokePrivateConversion(controller, "OnConvertMappingExtensionsWallsToNoodleExtensions");

                    Assert.AreEqual(originalShape.Position, testWall.CustomCoordinate[0].AsFloat, 0.0001f);
                    Assert.AreEqual(originalShape.StartHeight, testWall.CustomCoordinate[1].AsFloat, 0.0001f);
                    Assert.AreEqual(originalShape.Width, testWall.CustomSize[0].AsFloat, 0.0001f);
                    Assert.AreEqual(originalShape.Height, testWall.CustomSize[1].AsFloat, 0.0001f);
                    Assert.IsTrue(testWall.IsNoodleExtensions());
                    Assert.IsFalse(testWall.IsMappingExtensions());
                    Assert.IsFalse(songContainer.MapDifficultyInfo.CustomRequirements.Contains("Mapping Extensions"));
                    Assert.Contains("Noodle Extensions", songContainer.MapDifficultyInfo.CustomRequirements);
                    Assert.AreEqual(
                        RequirementCheck.RequirementType.Requirement,
                        new NoodleExtensionsReq().IsRequiredOrSuggested(songContainer.MapDifficultyInfo, difficulty));
                });
        }

        // The No path must be nondestructive because the mapper explicitly declined migration, so retain both the raw
        // Mapping Extensions wall encoding and its existing requirement without adding Noodle Extensions.
        [Test]
        public void DecliningMappingExtensionsWallMigrationLeavesMapUnchanged()
        {
            var wall = CreateMappingExtensionsWall(1500, 0, 1500, 5, 2000);
            RunWithWall(
                3,
                wall,
                (controller, songContainer, difficulty, testWall) =>
                {
                    songContainer.MapDifficultyInfo.CustomRequirements.Add("Mapping Extensions");

                    InvokePrivateConversion(controller, "OnKeepMappingExtensionsWalls");

                    Assert.AreEqual(1500, testWall.PosX);
                    Assert.AreEqual(1500, testWall.Width);
                    Assert.AreEqual(2000, testWall.Type);
                    Assert.IsNull(testWall.CustomCoordinate);
                    Assert.IsNull(testWall.CustomSize);
                    Assert.Contains("Mapping Extensions", songContainer.MapDifficultyInfo.CustomRequirements);
                    Assert.IsFalse(songContainer.MapDifficultyInfo.CustomRequirements.Contains("Noodle Extensions"));
                });
        }

        // Mixed maps should convert every Mapping Extensions wall in one accepted migration while leaving ordinary
        // vanilla walls untouched and adding each replacement requirement at most once.
        [Test]
        public void MappingExtensionsMigrationConvertsAllModdedWallsOnlyOnce()
        {
            var firstModdedWall = CreateMappingExtensionsWall(1500, 0, 1, 5, int.MinValue);
            RunWithWall(
                3,
                firstModdedWall,
                (controller, songContainer, difficulty, testWall) =>
                {
                    var secondModdedWall = CreateMappingExtensionsWall(0, 0, 1500, 5, int.MinValue);
                    var vanillaWall = CreateMappingExtensionsWall(1, 0, 1, 5, int.MinValue);
                    var beatToTheFutureWall = CreateMappingExtensionsWall(1, 3, 1, 1, int.MinValue);
                    difficulty.Obstacles.Add(secondModdedWall);
                    difficulty.Obstacles.Add(vanillaWall);
                    difficulty.Obstacles.Add(beatToTheFutureWall);
                    songContainer.MapDifficultyInfo.CustomRequirements.Add("Mapping Extensions");
                    songContainer.MapDifficultyInfo.CustomRequirements.Add("Noodle Extensions");

                    InvokePrivateConversion(controller, "OnConvertMappingExtensionsWallsToNoodleExtensions");

                    Assert.IsTrue(testWall.IsNoodleExtensions());
                    Assert.IsTrue(secondModdedWall.IsNoodleExtensions());
                    Assert.IsFalse(vanillaWall.IsNoodleExtensions());
                    Assert.IsFalse(beatToTheFutureWall.IsNoodleExtensions());
                    Assert.AreEqual(1, vanillaWall.PosX);
                    Assert.AreEqual(1, vanillaWall.Width);
                    Assert.AreEqual(3, beatToTheFutureWall.PosY);
                    Assert.AreEqual(
                        1,
                        songContainer.MapDifficultyInfo.CustomRequirements.Count(x => x == "Noodle Extensions"));
                });
        }

        // Test walls use the same public fields as loaded maps, with the sentinel preserving ordinary inferred type
        // fields while explicit legacy type values exercise Mapping Extensions' encoded vertical geometry.
        private static BaseObstacle CreateMappingExtensionsWall(
            int posX,
            int posY,
            int width,
            int height,
            int type)
        {
            var wall = new BaseObstacle
            {
                PosX = posX,
                PosY = posY,
                Width = width,
                Height = height
            };
            if (type != int.MinValue)
            {
                wall.Type = type;
            }

            return wall;
        }

        // These conversion tests require the editor singleton callback path, so this helper owns and restores the
        // temporary Unity objects and BeatSaberSongContainer instance around each independently asserted choice.
        private static void RunWithV4UpperLaneWall(
            int posY,
            Action<BeatmapVersionSwitchInputController, BeatSaberSongContainer, BaseDifficulty, BaseObstacle> test)
        {
            var wall = new BaseObstacle
            {
                PosX = 1,
                PosY = posY,
                Width = 1,
                Height = 1
            };
            RunWithWall(4, wall, test);
        }

        // Mapping Extensions cases span V2 and V3 formats, so this shared fixture makes the requested version
        // authoritative for both requirement predicates and legacy GetShape geometry decoding.
        private static void RunWithWall(
            int mapVersion,
            BaseObstacle wall,
            Action<BeatmapVersionSwitchInputController, BeatSaberSongContainer, BaseDifficulty, BaseObstacle> test)
        {
            Settings.Instance.MapVersion = mapVersion;
            var difficulty = new BaseDifficulty
            {
                Version = $"{mapVersion}.0.0"
            };
            difficulty.Obstacles.Add(wall);

            var songContainerObject = new GameObject("BeatSaberSongContainer test");
            var songContainer = songContainerObject.AddComponent<BeatSaberSongContainer>();
            songContainer.Map = difficulty;
            songContainer.MapDifficultyInfo = new InfoDifficulty(new InfoDifficultySet());

            // EditMode does not invoke BeatSaberSongContainer.Awake reliably, so install the test container explicitly for the conversion callback.
            var instanceProperty = typeof(BeatSaberSongContainer).GetProperty(
                nameof(BeatSaberSongContainer.Instance),
                BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(instanceProperty);
            var previousSongContainer = instanceProperty.GetValue(null);
            instanceProperty.SetValue(null, songContainer);

            var controllerObject = new GameObject("BeatmapVersionSwitchInputController test");
            var controller = controllerObject.AddComponent<BeatmapVersionSwitchInputController>();

            try
            {
                test(controller, songContainer, difficulty, wall);
            }
            finally
            {
                instanceProperty.SetValue(null, previousSongContainer);
                UnityEngine.Object.DestroyImmediate(controllerObject);
                UnityEngine.Object.DestroyImmediate(songContainerObject);
            }
        }

        // The conversion callbacks are intentionally private UI handlers, so reflection lets EditMode tests exercise
        // the same choice endpoints as the dialog buttons without exposing editor-only methods as public API.
        private static void InvokePrivateConversion(
            BeatmapVersionSwitchInputController controller,
            string methodName,
            params object[] arguments)
        {
            var conversion = typeof(BeatmapVersionSwitchInputController).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(conversion, $"Missing conversion callback {methodName}.");
            conversion.Invoke(controller, arguments);
        }

        // Prompt tests need the production gate's current answer after earlier choices mutate requirements, so this
        // invokes that private decision at the same point where the prompt chain will reevaluate it.
        private static bool InvokePrivateBoolean(
            BeatmapVersionSwitchInputController controller,
            string methodName)
        {
            var decision = typeof(BeatmapVersionSwitchInputController).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(decision, $"Missing prompt decision {methodName}.");
            return (bool)decision.Invoke(controller, Array.Empty<object>());
        }
    }
}

// PR #669 removes direct requirement edits; exercise conversion callbacks and the save-time checker together.
using System.Reflection;
using Beatmap.Base;
using Beatmap.Info;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    // Conversion choices must preserve their data while leaving dependency metadata to the existing requirement system.
    public class VersionSwitchRequirementTest : TestBase
    {
        // Each callback previously added a requirement immediately instead of respecting the centralized refresh lifecycle.
        [TestCase("OnChangeToV3WithVNJS", "BeatToTheFuture", false)]
        [TestCase("OnChangeToV3WithVNJS", "BeatToTheFuture", true)]
        [TestCase("OnChangeToV3WithBeatToTheFuture", "BeatToTheFuture", true)]
        [TestCase("OnChangeToV3WithNoodleExtensions", "Noodle Extensions", true)]
        [TestCase("OnConvertMappingExtensionsWallsToNoodleExtensions", "Noodle Extensions", true)]
        public void ConversionDefersMetadataToRequirementChecks(string callback, string expectedRequirement, bool includeWall)
        {
            var song = BeatSaberSongContainer.Instance;
            var originalMap = song.Map;
            var originalInfo = song.MapDifficultyInfo;
            var originalVersion = Settings.Instance.MapVersion;
            var originalAutomatic = Settings.Instance.AutomaticModRequirements;
            // Wall shape conversion uses the loaded format as well as the selected output format.
            var map = new BaseDifficulty { Version = "4.0.0" };
            var info = new InfoDifficulty(new InfoDifficultySet());
            var controllerObject = new GameObject("Version switch requirement test");
            try
            {
                song.Map = map;
                song.MapDifficultyInfo = info;
                Settings.Instance.MapVersion = 4;
                Settings.Instance.AutomaticModRequirements = true;
                if (callback == "OnChangeToV3WithVNJS")
                {
                    map.NJSEvents.Add(new BaseNJSEvent { JsonTime = 1 });
                }
                // VNJS consent must cover upper walls in the same map without opening a second backport prompt.
                if (includeWall)
                {
                    map.Obstacles.Add(new BaseObstacle
                    {
                        JsonTime = 1,
                        PosX = 0,
                        PosY = 3,
                        Width = 1,
                        Height = 2,
                        Duration = 1
                    });
                }

                if (callback == "OnConvertMappingExtensionsWallsToNoodleExtensions")
                {
                    map.Obstacles[0].PosX = 1000;
                    map.Obstacles[0].PosY = 0;
                    info.CustomRequirements.Add("Mapping Extensions");
                    Settings.Instance.MapVersion = 3;
                }

                var requirementsBefore = info.CustomRequirements.ToArray();
                var controller = controllerObject.AddComponent<BeatmapVersionSwitchInputController>();
                var method = typeof(BeatmapVersionSwitchInputController).GetMethod(
                    callback, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                method.Invoke(controller, callback == "OnChangeToV3WithVNJS" ? new object[] { true } : null);

                Assert.That(Settings.Instance.MapVersion, Is.EqualTo(3));
                Assert.That(info.CustomRequirements, Is.EqualTo(requirementsBefore),
                    "Conversion directly changed requirement metadata before the normal requirement refresh.");
                info.RefreshRequirementsAndWarnings(map);
                Assert.That(info.CustomRequirements, Does.Contain(expectedRequirement));
                Assert.That(info.CustomRequirements, Does.Not.Contain("Mapping Extensions"));
                if (expectedRequirement == "Noodle Extensions")
                {
                    Assert.That(map.Obstacles[0].CustomCoordinate, Is.Not.Null);
                    Assert.That(map.Obstacles[0].PosY, Is.Zero);
                }
                else if (map.NJSEvents.Count > 0)
                {
                    Assert.That(map.SaveVNJSEventsInV3, Is.True);
                }
                else
                {
                    Assert.That(map.Obstacles[0].PosY, Is.EqualTo(3));
                }
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                song.Map = originalMap;
                song.MapDifficultyInfo = originalInfo;
                Settings.Instance.MapVersion = originalVersion;
                Settings.Instance.AutomaticModRequirements = originalAutomatic;
            }
        }
    }
}

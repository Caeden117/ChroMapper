using System.Collections;
using System.Linq;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class LegacyChromaGradientMapLoadTest : TestBase
    {
        private bool? emulateChromaLiteBeforeTest;

        // Type 2 must fail independently so its first lit controller cannot prevent the matching type-3 load regression
        // from running as a separate Unity test against a freshly loaded copy of the reported V2 sequence.
        [UnityTest]
        public IEnumerator ProductionV2MapLoadKeepsType2LasersOffAfterFinalAlphaZeroEvent()
        {
            yield return AssertProductionV2MapLoadKeepsLasersOff((int)EventTypeValue.Event2);
        }

        // Type 3 uses the same production load path but remains isolated from the type-2 assertion, proving both laser
        // sets retain the incorrect post-destination state before any mapper edit repairs their caches.
        [UnityTest]
        public IEnumerator ProductionV2MapLoadKeepsType3LasersOffAfterFinalAlphaZeroEvent()
        {
            yield return AssertProductionV2MapLoadKeepsLasersOff((int)EventTypeValue.Event3);
        }

        // This fixture replaces the shared mapper scene to exercise production loading; restore the canonical empty V3
        // scene and its baseline even when the pre-edit final-off assertion fails.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            if (emulateChromaLiteBeforeTest.HasValue)
            {
                Settings.Instance.EmulateChromaLite = emulateChromaLiteBeforeTest.Value;
                emulateChromaLiteBeforeTest = null;
            }

            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // Read the environment-owned controllers populated during scene loading rather than registering a test light,
        // which would require reinitializing the effect and accidentally repair the exact load-time cache under test.
        private static void AssertEnvironmentLightsAreOff(int eventType, float jsonTime)
        {
            var lights = Object.FindObjectsByType<LightController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(light => light.Kind == LightController.LightKind.Basic && light.Type == eventType)
                .ToArray();
            Assert.That(
                lights,
                Is.Not.Empty,
                $"DefaultEnvironment had no active Basic Event type {eventType} lights to inspect.");
            for (var lightIndex = 0; lightIndex < lights.Length; lightIndex++)
            {
                Assert.That(
                    System.Math.Round(lights[lightIndex].Color.a, 3),
                    Is.EqualTo(0d),
                    $"Production V2 map loading left Basic Event type {eventType}, light ID "
                    + $"{lights[lightIndex].ID} lit at JSON beat {jsonTime}: {lights[lightIndex].Color}.");
            }
        }

        // Load separately for each laser type so every assertion observes untouched production initialization and no
        // result depends on test ordering or a prior seek through the other lane's failing state.
        private IEnumerator AssertProductionV2MapLoadKeepsLasersOff(int eventType)
        {
            emulateChromaLiteBeforeTest = Settings.Instance.EmulateChromaLite;
            Settings.Instance.EmulateChromaLite = true;
            yield return TestUtils.ReloadMap(2, CreateReportedDifficulty());

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(34f);
            yield return null;

            AssertEnvironmentLightsAreOff(eventType, 34f);
        }

        // Include the same-time neighboring lanes and the later type-2/type-3 gradient because bulk-load insertion of
        // future events can corrupt an earlier final state even though an isolated three-node reconstruction stays off.
        private static JSONNode CreateReportedDifficulty() => JSON.Parse(@"
        {
            ""_version"": ""2.6.0"",
            ""_events"": [
                { ""_time"": 32, ""_type"": 0, ""_value"": 0, ""_floatValue"": 1 },
                { ""_time"": 32, ""_type"": 2, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_lightGradient"": { ""_duration"": 0.25,
                    ""_startColor"": [0.298, 1, 0.584, 0], ""_endColor"": [0.298, 1, 0.584],
                    ""_easing"": ""easeLinear"" } } },
                { ""_time"": 32.25, ""_type"": 2, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_lightGradient"": { ""_duration"": 0.5,
                    ""_startColor"": [0.298, 1, 0.584], ""_endColor"": [0.298, 1, 0.584, 0],
                    ""_easing"": ""easeLinear"" } } },
                { ""_time"": 32.75, ""_type"": 2, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_color"": [0.298, 1, 0.584, 0] } },
                { ""_time"": 32.75, ""_type"": 3, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_lightGradient"": { ""_duration"": 0.25,
                    ""_startColor"": [0.755, 1, 0.584, 0], ""_endColor"": [0.755, 1, 0.584],
                    ""_easing"": ""easeLinear"" } } },
                { ""_time"": 33, ""_type"": 3, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_lightGradient"": { ""_duration"": 0.5,
                    ""_startColor"": [0.755, 1, 0.584], ""_endColor"": [0.755, 1, 0.584, 0],
                    ""_easing"": ""easeLinear"" } } },
                { ""_time"": 33.5, ""_type"": 4, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_lightGradient"": { ""_duration"": 0.25,
                    ""_startColor"": [0, 1, 1, 0], ""_endColor"": [0, 0.448, 0.65],
                    ""_easing"": ""easeLinear"" } } },
                { ""_time"": 33.5, ""_type"": 3, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_color"": [0.755, 1, 0.584, 0] } },
                { ""_time"": 33.75, ""_type"": 4, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_lightGradient"": { ""_duration"": 0.5,
                    ""_startColor"": [0, 0.448, 0.65], ""_endColor"": [0, 1, 1, 0],
                    ""_easing"": ""easeLinear"" } } },
                { ""_time"": 34.25, ""_type"": 4, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_color"": [0, 1, 1, 0] } },
                { ""_time"": 35.5, ""_type"": 2, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_lightGradient"": { ""_duration"": 2,
                    ""_startColor"": [0, 0, 3], ""_endColor"": [0.73, 0, 0, 0],
                    ""_easing"": ""easeOutSine"" } } },
                { ""_time"": 35.5, ""_type"": 3, ""_value"": 1, ""_floatValue"": 1,
                  ""_customData"": { ""_lightGradient"": { ""_duration"": 2,
                    ""_startColor"": [0, 0, 3], ""_endColor"": [0.73, 0, 0, 0],
                    ""_easing"": ""easeOutSine"" } } }
            ]
        }");
    }
}

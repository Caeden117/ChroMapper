using System.Collections;
using System.Collections.Generic;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaCustomEventEnumAliasTest : TestBase
    {
        [UnityTest]
        public IEnumerator QuotedNumericComponentEasingUsesTheNativeEnum()
        {
            yield return CheckEasing("2", 6f);
        }

        [UnityTest]
        public IEnumerator QuotedWhitespaceComponentEasingUsesTheNativeEnum()
        {
            yield return CheckEasing(" easeInQuad ", 6f);
        }

        [UnityTest]
        public IEnumerator QuotedCombinedComponentEasingUsesTheNativeEnum()
        {
            yield return CheckEasing("easeInQuad, easeOutQuad", 10f);
        }

        [UnityTest]
        public IEnumerator NumericJsonComponentEasingKeepsItsExistingLinearFallback()
        {
            yield return CheckEasing(2, 8f);
        }

        private static IEnumerator CheckEasing(JSONNode easing, float expectedHeight)
        {
            var data = JSON.Parse(
                @"{
                ""version"":""3.3.0"",
                ""customData"": {
                    ""environment"": [{
                        ""id"":""[0]Environment"",
                        ""lookupMethod"":""EndsWith"",
                        ""track"":""aliasFog"",
                        ""components"": {""BloomFogEnvironment"": {""height"":4}}
                    }],
                    ""customEvents"": [{
                        ""b"":0,
                        ""t"":""AnimateComponent"",
                        ""d"": {
                            ""track"":""aliasFog"",
                            ""duration"":4,
                            ""BloomFogEnvironment"": {""height"":[[4,0],[12,1]]}
                        }
                    }]
                }
            }");
            data["customData"]["customEvents"][0]["d"]["easing"] = easing;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                LightshowFileName = "MissingTestLightshow.dat",
                CustomRequirements = new List<string> { "Chroma" }
            };
            yield return TestUtils.ReloadMap(
                3,
                data,
                beatsPerMinute: 120,
                environmentName: "BillieEnvironment",
                difficultyInfo: difficulty);
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            foreach (var beat in new[] { 2f, 3f, 2f })
                clock.MoveToJsonTime(beat);

            Assert.That(context.Descriptor.BloomFogParams.Height, Is.EqualTo(expectedHeight).Within(0.0001f));
        }

        [UnityTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

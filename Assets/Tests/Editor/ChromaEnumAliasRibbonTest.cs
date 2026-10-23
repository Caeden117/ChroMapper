using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Info;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class ChromaEnumAliasRibbonTest : BasicEventChunkingTestBase
    {
        private bool previousVisualize;
        private bool previousLite;
        private bool previousAdvanced;

        protected override IEnumerator OnMapLoaded()
        {
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                CustomRequirements = new List<string> { "Chroma" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(3, null, difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [SetUp]
        public void EnableRibbons()
        {
            previousVisualize = Settings.Instance.VisualizeChromaGradients;
            previousLite = Settings.Instance.EmulateChromaLite;
            previousAdvanced = Settings.Instance.EmulateChromaAdvanced;
            Settings.Instance.VisualizeChromaGradients = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.EmulateChromaAdvanced = true;
            GetEventsContainer().PropagationEditing = EventGridContainer.PropMode.Off;
        }

        protected override void AfterCleanup()
        {
            Settings.Instance.VisualizeChromaGradients = previousVisualize;
            Settings.Instance.EmulateChromaLite = previousLite;
            Settings.Instance.EmulateChromaAdvanced = previousAdvanced;
        }

        [Test]
        public void QuotedEnumsReachTheRenderedRibbon()
        {
            CheckRibbon("2", "1", 1, 1);
        }

        [Test]
        public void NumericJsonKeepsTheExistingRibbonFallback()
        {
            CheckRibbon(2, 1, 0, 0);
        }

        private static void CheckRibbon(JSONNode easing, JSONNode lerpType, int expectedEasing, int expectedLerp)
        {
            var source = PlaceUtils.Place(
                V3BasicEvent.GetFromJson(
                    new JSONObject
                    {
                        ["b"] = 2f,
                        ["et"] = 2,
                        ["i"] = 5,
                        ["f"] = 1f,
                        ["customData"] = new JSONObject
                        {
                            ["color"] = JSON.Parse("[0,1,0,1]"),
                            ["easing"] = easing,
                            ["lerpType"] = lerpType
                        }
                    }));
            PlaceUtils.Place(
                new BaseEvent
                {
                    JsonTime = 4f,
                    Type = 2,
                    Value = 8,
                    FloatValue = 1f,
                    CustomColor = Color.red
                });
            var loaded = GetEventsContainer().LoadedContainers[source];
            var ribbon = loaded.GetComponentInChildren<LightGradientController>(true);
            Assert.That(ribbon, Is.Not.Null);
            var renderer = ribbon.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(renderer.enabled, Is.True);
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetInt("_EasingID"), Is.EqualTo(expectedEasing));
            Assert.That(properties.GetInt("_UseHSV"), Is.EqualTo(expectedLerp));
        }
    }
}

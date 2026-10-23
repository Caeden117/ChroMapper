using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class ChromaBasicEventRoutingAuditTest : TestBase
    {
        private GameObject effectObject;
        private GameObject ringManagerObject;
        private BasicLightEffect effect;
        private AuditLightController[] lights;
        private int previousVersion;
        private bool previousLite;
        private bool previousAdvanced;
        private bool? previousSchemeBoost;
        private float? previousJsonTime;
        private ColorSchemeSO whitePalette;
        private string[] previousRequirements;
        private string[] previousSuggestions;

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(
                3,
                null,
                beatsPerMinute: 120,
                environmentName: "BillieEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [SetUp]
        public void EnableChromaRequests()
        {
            var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;
            previousRequirements = info.CustomRequirements.ToArray();
            previousSuggestions = info.CustomSuggestions.ToArray();
            info.CustomRequirements.Add("Chroma");
            previousVersion = Settings.Instance.MapVersion;
            previousLite = Settings.Instance.EmulateChromaLite;
            previousAdvanced = Settings.Instance.EmulateChromaAdvanced;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.EmulateChromaAdvanced = true;
        }

        // Shipping Chroma b38d924 uses Heck easing on fixed tweens, including its defaults.
        [TestCase(2, 6, 0.6f, 0.8745098f)]
        [TestCase(3, 6, 0.6f, 0.8745098f)]
        [TestCase(2, 7, 1.5f, 0.5f)]
        [TestCase(3, 7, 1.5f, 0.5f)]
        [TestCase(2, -1, 1.5f, 0.5f)]
        [TestCase(3, -1, 1.5f, 0.5f)]
        public void FixedDurationRequestUsesAuthoredLinearEasing(
            int version,
            int value,
            float sampleBeat,
            float expectedAlpha)
        {
            CreateEffect(version);
            var data = new JSONObject
            {
                [CustomKey("color")] = JSON.Parse("[0.2,0.4,0.8,1]"),
                [CustomKey("easing")] = "easeLinear"
            };
            InsertEvent(0f, value, data);
            foreach (var beat in new[] { sampleBeat, sampleBeat + 0.01f, sampleBeat })
                effect.UpdateTime(false, beat);

            Assert.That(
                lights[0].Color.a,
                Is.EqualTo(expectedAlpha).Within(0.0001f),
                $"V{version} value {value} must use the requested linear fixed-duration tween.");
        }

        [TestCase(2, 6, 0.6f, 0.7803922f)]
        [TestCase(3, 6, 0.6f, 0.7803922f)]
        [TestCase(2, 7, 1.5f, 31f / 1023f)]
        [TestCase(3, 7, 1.5f, 31f / 1023f)]
        public void DeclaredChromaRequestUsesTheShippingDefaultEasing(
            int version,
            int value,
            float sampleBeat,
            float expectedAlpha)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                value,
                new JSONObject { [CustomKey("color")] = JSON.Parse("[0.2,0.4,0.8,1]") });
            effect.UpdateTime(false, sampleBeat);
            Assert.That(lights[0].Color.a, Is.EqualTo(expectedAlpha).Within(0.0001f));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void ScalarLightIdSelectsItsRegisteredLight(int version)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    [CustomKey("lightID")] = 1,
                    [CustomKey("color")] = JSON.Parse("[0,1,0,1]")
                });
            effect.UpdateTime(false, 1f);
            Assert.That(lights[0].Color.a, Is.EqualTo(0f));
            AssertColor(lights[1].Color, new Color(0f, 1f, 0f, 0.7490196f));
            Assert.That(lights[2].Color.a, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator ColorBoostRefreshUsesTheLatestTypeColorForEverySelectedTween()
        {
            CreateEffect(3);
            previousSchemeBoost = effect.ColorBoostEffect.Boost;
            var boost = effectObject.AddComponent<ColorBoostEffect>();
            boost.Atsc = effect.Atsc;
            boost.ID = 5;
            boost.ColorSchemeProvider = effect.ColorSchemeProvider;
            boost.Initialize();
            effect.ColorBoostEffect = boost;
            effectObject.SetActive(true);
            yield return null;
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    ["lightID"] = 0,
                    ["color"] = JSON.Parse("[1,0,0,1]")
                });
            InsertEvent(
                1f,
                5,
                new JSONObject
                {
                    ["lightID"] = 1,
                    ["color"] = JSON.Parse("[0,1,0,1]")
                });
            effect.UpdateTime(false, 1.5f);
            AssertColor(lights[0].Color, new Color(1f, 0f, 0f, 0.7490196f));
            AssertColor(lights[1].Color, new Color(0f, 1f, 0f, 0.7490196f));
            var boostEvent = V3BasicEvent.GetFromJson(
                new JSONObject { ["b"] = 2f, ["et"] = 5, ["i"] = 1, ["f"] = 1f });
            boost.InsertData(boostEvent);
            previousJsonTime = effect.Atsc.CurrentJsonTime;
            effect.Atsc.MoveToJsonTime(2f);
            boost.UpdateTime(false, 2f);
            Assert.That(
                lights[1].Color.a,
                Is.EqualTo(0.8f).Within(0.0001f),
                "The owned boost must reach its subscribed light effect through the production callback.");
            AssertColor(lights[0].Color, new Color(0f, 1f, 0f, 0.8f));
            AssertColor(lights[1].Color, new Color(0f, 1f, 0f, 0.8f));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void NegativeFadeAliasUsesTheFirstColorChannel(int version)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                -1,
                new JSONObject { [CustomKey("easing")] = "easeLinear" });
            effect.UpdateTime(false, 1.5f);
            var expected = effect.ColorSchemeProvider.ColorScheme.GetColorFrom(LightColor.Blue, false);
            expected.a *= 0.5f;
            AssertColor(lights[0].Color, expected);
        }

        [TestCase(2)]
        [TestCase(3)]
        public void ExplicitEmptyLightIdSelectsNoLights(int version)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    [CustomKey("lightID")] = new JSONArray(),
                    [CustomKey("color")] = JSON.Parse("[0,1,0,1]")
                });
            effect.UpdateTime(false, 1f);
            foreach (var light in lights)
                Assert.That(
                    light.Color.a,
                    Is.EqualTo(0f),
                    "An explicit empty selector must not fall back to broadcasting the event.");
        }

        // Native propagation groups preserve first occurrence of rounded Z, rather than sorting the positions.
        [Test]
        public void LegacyPropIdSelectsItsGroupBeforeLightId()
        {
            CreateEffect(2);
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    ["_propID"] = 0,
                    ["_lightID"] = 1,
                    ["_color"] = JSON.Parse("[0,1,0,1]")
                });
            effect.UpdateTime(false, 1f);
            Assert.That(lights[0].Color.a, Is.EqualTo(0.7490196f).Within(0.0001f));
            Assert.That(lights[2].Color.a, Is.EqualTo(0.7490196f).Within(0.0001f));
            Assert.That(lights[1].Color.a, Is.EqualTo(0f));
        }

        [Test]
        public void LegacyPropIdKeepsOneRegisteredRingTogetherAcrossWorldPositions()
        {
            CreateEffect(2);
            ringManagerObject = new GameObject("Canonical propagation ring manager");
            var manager = ringManagerObject.AddComponent<TrackLaneRingsManager>();
            manager.UseCached = true;
            manager.Rings = new List<TrackLaneRing>();
            for (var i = 0; i < 2; i++)
            {
                var ringObject = new GameObject("Registered propagation ring " + i);
                ringObject.transform.SetParent(effectObject.transform);
                var ring = ringObject.AddComponent<TrackLaneRing>();
                ring.ParentManager = manager;
                manager.Rings.Add(ring);
            }

            lights[0].transform.SetParent(manager.Rings[0].transform);
            lights[1].transform.SetParent(manager.Rings[1].transform);
            lights[2].transform.SetParent(manager.Rings[0].transform);
            lights[0].transform.position = Vector3.zero;
            lights[1].transform.position = Vector3.zero;
            lights[2].transform.position = Vector3.forward * 10f;
            effectObject.SetActive(true);
            effect.Initialize();
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    ["_propID"] = 0,
                    ["_color"] = JSON.Parse("[0,1,0,1]")
                });
            effect.UpdateTime(false, 1f);
            Assert.That(lights[0].Color.a, Is.EqualTo(0.7490196f).Within(0.0001f));
            Assert.That(lights[2].Color.a, Is.EqualTo(0.7490196f).Within(0.0001f));
            Assert.That(lights[1].Color.a, Is.EqualTo(0f));
        }

        [Test]
        public void ExplicitColorOverridesTheLegacyRgbMarker()
        {
            CreateEffect(2);
            InsertEvent(0f, 2000000255, new JSONObject());
            InsertEvent(
                1f,
                5,
                new JSONObject { ["_color"] = JSON.Parse("[0,1,0,1]") });
            effect.UpdateTime(false, 2f);
            AssertColor(lights[0].Color, new Color(0f, 1f, 0f, 0.7490196f));
        }

        [Test]
        public void LegacyRgbColorCancelsTheSameEventGradient()
        {
            CreateEffect(2);
            InsertEvent(0f, 2000000255, new JSONObject());
            InsertEvent(1f, 5, new JSONObject { ["_lightGradient"] = Gradient() });
            effect.UpdateTime(false, 2f);
            AssertColor(lights[0].Color, new Color(0f, 0f, 1f, 0.7490196f));
        }

        [Test]
        public void LegacyGradientRefreshesAnEarlierUnselectedLight()
        {
            CreateEffect(2);
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    ["_lightID"] = 1,
                    ["_color"] = JSON.Parse("[0,1,0,1]")
                });
            InsertEvent(
                1f,
                5,
                new JSONObject
                {
                    ["_lightID"] = 0,
                    ["_lightGradient"] = Gradient()
                });
            effect.UpdateTime(false, 1.5f);
            AssertColor(lights[0].Color, new Color(0.875f, 0f, 0.125f, 0.7490196f));
            AssertColor(lights[1].Color, new Color(0.875f, 0f, 0.125f, 0.7490196f));
        }

        [Test]
        public void LegacyGradientKeepsTheFixedFlashContrastClock()
        {
            CreateEffect(2);
            InsertEvent(0f, 6, new JSONObject { ["_lightGradient"] = Gradient() });
            effect.UpdateTime(false, 0.6f);
            AssertColor(lights[0].Color, new Color(0.85f, 0f, 0.15f, 0.7803922f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void CompletedGradientUsesItsEndColorWithoutRefreshingOtherIds(float duration)
        {
            CreateEffect(2);
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    ["_lightID"] = 1,
                    ["_color"] = JSON.Parse("[0,1,0,1]")
                });
            var gradient = Gradient();
            gradient["_duration"] = duration;
            InsertEvent(
                1f,
                5,
                new JSONObject
                {
                    ["_lightID"] = 0,
                    ["_lightGradient"] = gradient
                });
            effect.UpdateTime(false, 2f);
            AssertColor(lights[0].Color, new Color(0f, 0f, 1f, 0.7490196f));
            AssertColor(lights[1].Color, new Color(0f, 1f, 0f, 0.7490196f));
        }

        [Test]
        public void ExplicitColorCancelsTheSameEventGradient()
        {
            CreateEffect(2);
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    ["_color"] = JSON.Parse("[0,1,0,1]"),
                    ["_lightGradient"] = Gradient()
                });
            foreach (var beat in new[] { 1f, 3f, 1f })
            {
                effect.UpdateTime(false, beat);
                AssertColor(lights[0].Color, new Color(0f, 1f, 0f, 0.7490196f));
            }
        }

        [Test]
        public void ExplicitColorCancelsAnEarlierGradient()
        {
            CreateEffect(2);
            InsertEvent(0f, 5, new JSONObject { ["_lightGradient"] = Gradient() });
            InsertEvent(
                1f,
                5,
                new JSONObject { ["_color"] = JSON.Parse("[0,1,0,1]") });
            effect.UpdateTime(false, 2f);
            AssertColor(lights[0].Color, new Color(0f, 1f, 0f, 0.7490196f));
            effect.UpdateTime(false, 0.5f);
            AssertColor(lights[0].Color, new Color(0.875f, 0f, 0.125f, 0.7490196f));
            effect.UpdateTime(false, 2f);
            AssertColor(lights[0].Color, new Color(0f, 1f, 0f, 0.7490196f));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void VanillaFadeKeepsTheNativeDefaultEasing(int version)
        {
            var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;
            info.CustomRequirements.Clear();
            info.CustomSuggestions.Clear();
            CreateEffect(version);
            InsertEvent(0f, 7, new JSONObject());
            effect.UpdateTime(false, 1.5f);
            Assert.That(lights[0].Color.a, Is.EqualTo(0.03125f).Within(0.0001f));
        }

        [Test]
        public void WhiteRequestUsesTheConfiguredPaletteInput()
        {
            CreateEffect(2);
            ConfigureOwnedWhitePalette();
            InsertEvent(0f, 9, new JSONObject());
            effect.UpdateTime(false, 1f);
            AssertColor(lights[0].Color, Color.white);
        }

        [Test]
        public void WhiteRequestIgnoresLegacyRgbColor()
        {
            CreateEffect(2);
            ConfigureOwnedWhitePalette();
            InsertEvent(0f, 2000000255, new JSONObject());
            InsertEvent(1f, 9, new JSONObject());
            effect.UpdateTime(false, 2f);
            AssertColor(lights[0].Color, Color.white);
        }

        [Test]
        public void WhiteRequestIgnoresLegacyGradientColor()
        {
            CreateEffect(2);
            ConfigureOwnedWhitePalette();
            InsertEvent(0f, 9, new JSONObject { ["_lightGradient"] = Gradient() });
            effect.UpdateTime(false, 1f);
            AssertColor(lights[0].Color, Color.white);
        }

        [UnityTest]
        public IEnumerator LegacyGradientUsesTheBpmAtItsStart()
        {
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(
                    @"{
                    ""_version"":""2.6.0"",
                    ""_events"":[
                        {""_time"":0,""_type"":100,""_floatValue"":120},
                        {""_time"":4,""_type"":100,""_floatValue"":240}
                    ]
                }"),
                beatsPerMinute: 120,
                environmentName: "BillieEnvironment");
            CreateEffect(2);
            InsertEvent(4f, 5, new JSONObject { ["_lightGradient"] = Gradient() });
            foreach (var beat in new[] { 5f, 6f, 5f })
                effect.UpdateTime(false, beat);

            // Four authored beats at 240 BPM last one second, or two beats on the 120 BPM song clock.
            AssertColor(lights[0].Color, new Color(0.5f, 0f, 0.5f, 0.7490196f));
        }

        [TestCase(2, "2", 0.25f)]
        [TestCase(3, "2", 0.25f)]
        [TestCase(2, " easeInQuad ", 0.25f)]
        [TestCase(3, " easeInQuad ", 0.25f)]
        [TestCase(2, "easeInQuad, easeOutQuad", 0.75f)]
        [TestCase(3, "easeInQuad, easeOutQuad", 0.75f)]
        public void QuotedEasingEnumAliasesDriveFixedTweens(int version, string alias, float easedProgress)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                6,
                new JSONObject
                {
                    [CustomKey("color")] = JSON.Parse("[0.2,0.4,0.8,1]"),
                    [CustomKey("easing")] = alias
                });
            effect.UpdateTime(false, 0.6f);
            Assert.That(
                lights[0].Color.a,
                Is.EqualTo(1f - ((1f - 0.7490196f) * easedProgress)).Within(0.0001f));
        }

        [TestCase(2, "1")]
        [TestCase(3, "1")]
        [TestCase(2, " HSV ")]
        [TestCase(3, " HSV ")]
        [TestCase(2, "RGB, HSV")]
        [TestCase(3, "RGB, HSV")]
        public void QuotedLerpEnumAliasesDriveLegacyHsvTransitions(int version, string alias)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    [CustomKey("color")] = JSON.Parse("[0,1,0,1]"),
                    [CustomKey("lerpType")] = alias
                });
            InsertEvent(2f, 8, new JSONObject { [CustomKey("color")] = JSON.Parse("[1,0,0,1]") });
            effect.UpdateTime(false, 1f);
            AssertColor(lights[0].Color, new Color(1f, 1f, 0f, 0.7490196f));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void NumericJsonEasingRetainsItsExistingLinearFallback(int version)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                6,
                new JSONObject
                {
                    [CustomKey("color")] = JSON.Parse("[0.2,0.4,0.8,1]"),
                    [CustomKey("easing")] = 2
                });
            effect.UpdateTime(false, 0.6f);
            Assert.That(lights[0].Color.a, Is.EqualTo(0.8745098f).Within(0.0001f));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void NumericJsonLerpTypeRetainsItsExistingRgbFallback(int version)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                5,
                new JSONObject
                {
                    [CustomKey("color")] = JSON.Parse("[0,1,0,1]"),
                    [CustomKey("lerpType")] = 1
                });
            InsertEvent(2f, 8, new JSONObject { [CustomKey("color")] = JSON.Parse("[1,0,0,1]") });
            effect.UpdateTime(false, 1f);
            AssertColor(lights[0].Color, new Color(0.5f, 0.5f, 0f, 0.7490196f));
        }

        [TestCase("2", 0.0625f)]
        [TestCase(" easeInQuad ", 0.0625f)]
        [TestCase("easeInQuad, easeOutQuad", 0.4375f)]
        public void QuotedGradientEasingUsesTheNativeEnum(string alias, float progress)
        {
            CreateEffect(2);
            var gradient = Gradient();
            gradient["_easing"] = alias;
            InsertEvent(0f, 5, new JSONObject { ["_lightGradient"] = gradient });
            effect.UpdateTime(false, 1f);
            AssertColor(lights[0].Color, new Color(1f - progress, 0f, progress, 0.7490196f));
        }

        [Test]
        public void NumericJsonGradientEasingKeepsItsExistingLinearFallback()
        {
            CreateEffect(2);
            var gradient = Gradient();
            gradient["_easing"] = 2;
            InsertEvent(0f, 5, new JSONObject { ["_lightGradient"] = gradient });
            effect.UpdateTime(false, 1f);
            AssertColor(lights[0].Color, new Color(0.75f, 0f, 0.25f, 0.7490196f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrNullGradientEasingUsesItsNativeLinearDefault(bool explicitNull)
        {
            CreateEffect(2);
            var gradient = Gradient();
            gradient.Remove("_easing");
            if (explicitNull)
                gradient["_easing"] = JSONNull.CreateOrGet();

            Assert.DoesNotThrow(() => InsertEvent(0f, 5, new JSONObject { ["_lightGradient"] = gradient }));
            effect.UpdateTime(false, 1f);
            AssertColor(lights[0].Color, new Color(0.75f, 0f, 0.25f, 0.7490196f));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void NullBasicEasingUsesTheNativeDeclaredDefault(int version)
        {
            CreateEffect(version);
            InsertEvent(
                0f,
                7,
                new JSONObject
                {
                    [CustomKey("color")] = JSON.Parse("[0.2,0.4,0.8,1]"),
                    [CustomKey("easing")] = JSONNull.CreateOrGet()
                });
            effect.UpdateTime(false, 1.5f);
            Assert.That(lights[0].Color.a, Is.EqualTo(31f / 1023f).Within(0.0001f));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void UnsupportedNumericEnumFieldsKeepTheirRawExportTypes(int version)
        {
            CreateEffect(version);
            var data = ParseEvent(
                0f,
                5,
                new JSONObject
                {
                    [CustomKey("easing")] = 2,
                    [CustomKey("lerpType")] = 1
                });
            var exported = data.ToJson();
            var custom = exported[
                version == 2
                ? "_customData"
                : "customData"];
            Assert.That(custom[CustomKey("easing")].IsNumber, Is.True);
            Assert.That(custom[CustomKey("easing")].AsInt, Is.EqualTo(2));
            Assert.That(custom[CustomKey("lerpType")].IsNumber, Is.True);
            Assert.That(custom[CustomKey("lerpType")].AsInt, Is.EqualTo(1));
        }

        private void ConfigureOwnedWhitePalette()
        {
            // Native ColorManager returns its configured white palette, not a fixed RGB value.
            // Own that input so legacy-color rejection is independent of Billie's default gray.
            whitePalette = effect.ColorSchemeProvider.ColorScheme.Clone();
            whitePalette.EnvironmentWhiteColor = Color.white;
            whitePalette.EnvironmentWhiteBoostColor = Color.white;
            var provider = effectObject.AddComponent<ColorSchemeProvider>();
            provider.ColorScheme = whitePalette;
            effect.ColorSchemeProvider = provider;
        }

        private void CreateEffect(int version)
        {
            Settings.Instance.MapVersion = version;
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var template = context.Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(2);
            effectObject = new GameObject("Canonical Chroma event route");
            effectObject.SetActive(false);
            effect = effectObject.AddComponent<BasicLightEffect>();
            effect.Atsc = template.Atsc;
            effect.ID = 2;
            effect.ColorBoostEffect = template.ColorBoostEffect;
            effect.ColorSchemeProvider = template.ColorSchemeProvider;
            effect.OffIntensity = 0f;
            lights = new AuditLightController[3];
            var positions = new[] { 20f, -10f, 20f };
            for (var i = 0; i < lights.Length; i++)
            {
                var lightObject = new GameObject("Registered native-order light " + i);
                lightObject.transform.SetParent(effectObject.transform);
                lightObject.transform.position = new Vector3(0f, 0f, positions[i]);
                var light = lightObject.AddComponent<AuditLightController>();
                light.Kind = LightController.LightKind.Basic;
                light.Type = 2;
                light.ID = i;
                effect.Register(light, false);
                lights[i] = light;
            }

            effect.Initialize();
        }

        private void InsertEvent(float beat, int value, JSONObject customData) =>
            effect.InsertData(ParseEvent(beat, value, customData));

        private static BaseEvent ParseEvent(float beat, int value, JSONObject customData)
        {
            if (Settings.Instance.MapVersion == 2)
                return V2Event.GetFromJson(
                    new JSONObject
                    {
                        ["_time"] = beat,
                        ["_type"] = 2,
                        ["_value"] = value,
                        ["_floatValue"] = 1f,
                        ["_customData"] = customData
                    });

            return V3BasicEvent.GetFromJson(
                new JSONObject
                {
                    ["b"] = beat,
                    ["et"] = 2,
                    ["i"] = value,
                    ["f"] = 1f,
                    ["customData"] = customData
                });
        }

        private static string CustomKey(string name) => Settings.Instance.MapVersion == 2
            ? "_" + name
            : name;

        private static JSONObject Gradient() => new()
        {
            ["_startColor"] = JSON.Parse("[1,0,0,1]"),
            ["_endColor"] = JSON.Parse("[0,0,1,1]"),
            ["_duration"] = 4f,
            ["_easing"] = "easeLinear"
        };

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }

        protected override void BeforeCleanup()
        {
            if (previousSchemeBoost.HasValue)
            {
                effect.ColorSchemeProvider.ColorScheme.SwapEnvironmentColors(previousSchemeBoost.Value);
                previousSchemeBoost = null;
            }

            if (previousJsonTime.HasValue)
            {
                effect.Atsc.MoveToJsonTime(previousJsonTime.Value);
                previousJsonTime = null;
            }

            if (ringManagerObject != null)
                Object.DestroyImmediate(ringManagerObject);

            if (effectObject != null)
                Object.DestroyImmediate(effectObject);

            if (whitePalette != null)
                Object.DestroyImmediate(whitePalette);

            var info = BeatSaberSongContainer.Instance.MapDifficultyInfo;
            info.CustomRequirements.Clear();
            info.CustomRequirements.AddRange(previousRequirements);
            info.CustomSuggestions.Clear();
            info.CustomSuggestions.AddRange(previousSuggestions);
            Settings.Instance.MapVersion = previousVersion;
            Settings.Instance.EmulateChromaLite = previousLite;
            Settings.Instance.EmulateChromaAdvanced = previousAdvanced;
        }

        public sealed class AuditLightController : LightController
        {
            public override bool IsPhysical => true;

            protected override bool Initialize() => true;

            public override void SetColor(Color color) => Color = color;
        }
    }
}


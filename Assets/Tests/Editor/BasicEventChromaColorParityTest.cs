using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    // Ribbons display authored brightness, while native scene dispatch also applies the switch's ColorSO alpha.
    public class BasicEventChromaColorParityTest : BasicEventChunkingTestBase
    {
        private static readonly int colorAId = Shader.PropertyToID("_ColorA");
        private static readonly int colorBId = Shader.PropertyToID("_ColorB");
        private static readonly int easingId = Shader.PropertyToID("_EasingID");
        private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");

        private bool? emulateChromaLiteBeforeTest;
        private bool? visualizeGradientsBeforeTest;
        private GameObject previewLightObject;

        // Chroma evaluates the legacy gradient in RGB, then multiplies its interpolated alpha by the active event brightness.
        [TestCase("linearAlpha", 0.5f)]
        [TestCase("rgbDespiteLerpType", 0.5f)]
        public void LegacyGradientRibbonAndLightPreviewMatchChroma(string scenario, float progress)
        {
            PrepareColorPreview();
            var data = GetGradientScenario(scenario);
            var previewLight = CreatePreviewLight((int)EventTypeValue.Event2);
            var source = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2f,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.RedOn,
                FloatValue = data.Brightness,
                CustomLerpType = data.SerializedLerpType,
                CustomLightGradient = new ChromaLightGradient(
                    data.StartColor,
                    data.EndColor,
                    2f,
                    data.Easing)
            });

            var easedProgress = Easing.Named(data.Easing)(progress);
            var expected = Color.LerpUnclamped(data.StartColor, data.EndColor, easedProgress);
            expected.a *= data.Brightness;

            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(2f + (2f * progress));
            var ribbonColor = EvaluateRibbonColor(source, progress);

            AssertRibbonAndPreviewMatch(expected, ribbonColor, previewLight.Color, scenario);
        }

        // Chroma composes authored alpha with brightness at both endpoints before applying transition easing and color interpolation.
        [TestCase("linearRgb", 0.5f)]
        [TestCase("easedRgb", 0.5f)]
        public void TransitionRibbonAndLightPreviewMatchChroma(string scenario, float progress)
        {
            PrepareColorPreview();
            var data = GetTransitionScenario(scenario);
            var previewLight = CreatePreviewLight((int)EventTypeValue.Event2);
            var source = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2f,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.RedOn,
                FloatValue = data.StartBrightness,
                CustomColor = data.StartColor,
                CustomEasing = data.Easing
            });
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 4f,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.BlueTransition,
                FloatValue = data.EndBrightness,
                CustomColor = data.EndColor
            });

            var start = data.StartColor;
            start.a *= data.StartBrightness;
            var end = data.EndColor;
            end.a *= data.EndBrightness;
            var expected = Color.LerpUnclamped(start, end, Easing.Named(data.Easing)(progress));

            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(2f + (2f * progress));
            var ribbonColor = EvaluateRibbonColor(source, progress);

            AssertRibbonAndPreviewMatch(expected, ribbonColor, previewLight.Color, scenario);
        }

        // ChromaGradientController eases _lightGradient colors through Heck's own function table, whose
        // Bounce family is a min-parabola variant rather than the easings.net curve - WorldCavesIn's
        // beat-280 fade-out uses easeInBounce. The ribbon shader keeps its own curve set (no in-game
        // equivalent), so only the live light output, the parity surface, is asserted here.
        [TestCase(0.4f)]
        [TestCase(0.55f)]
        public void LegacyGradientLightPreviewUsesHeckBounceEasing(float progress)
        {
            PrepareColorPreview();
            var previewLight = CreatePreviewLight((int)EventTypeValue.Event2);
            var start = new Color(0.125f, 0.125f, 0.5f, 1.5f);
            var end = new Color(0f, 0f, 0f, 0f);
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2f,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomLightGradient = new ChromaLightGradient(start, end, 2f, "easeInBounce")
            });

            var expected = Color.LerpUnclamped(start, end, HeckEaseInBounce(progress));
            expected.a *= .7490196f;

            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(2f + (2f * progress));

            Assert.That(
                ColorsMatch(expected, previewLight.Color, 0.0001f),
                Is.True,
                $"The light preview showed {previewLight.Color} at gradient progress {progress} instead " +
                $"of the Heck-eased {expected}; _lightGradient._easing resolves through Chroma's own " +
                "min-parabola Bounce variant, not the easings.net curve.");
        }

        // Independent reference for Heck's EaseInBounce: 1 - OutBounce(1 - p), where OutBounce is the
        // minimum of four parabolas with collision points 4/11, 8/11, 9/11, 1.
        private static float HeckEaseInBounce(float p)
        {
            var k = 1f - p;
            var x = (121f / 16f) * k * k;
            var q1 = k - (6f / 11f);
            var b = ((363f / 40f) * q1 * q1) + (7f / 10f);
            if (b < x) x = b;
            var q2 = k - (179f / 220f);
            var c = ((4356f / 361f) * q2 * q2) + (91f / 100f);
            if (c < x) x = c;
            var q3 = k - (19f / 20f);
            var d = ((54f / 5f) * q3 * q3) + (973f / 1000f);
            if (d < x) x = d;
            return 1f - x;
        }

        // Test-owned preview registration and shared appearance settings must not leak into other editor fixtures.
        protected override void AfterCleanup()
        {
            if (previewLightObject != null)
            {
                var previewLight = previewLightObject.GetComponent<ParityPreviewLightController>();
                var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
                context.Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(previewLight.Type)
                    .Unregister(previewLight);
                context.Descriptor.BasicEventEffectManager.Reinitialize();
                Object.DestroyImmediate(previewLightObject);
                previewLightObject = null;
            }

            if (emulateChromaLiteBeforeTest.HasValue)
            {
                Settings.Instance.EmulateChromaLite = emulateChromaLiteBeforeTest.Value;
                emulateChromaLiteBeforeTest = null;
            }

            if (visualizeGradientsBeforeTest.HasValue)
            {
                Settings.Instance.VisualizeChromaGradients = visualizeGradientsBeforeTest.Value;
                visualizeGradientsBeforeTest = null;
            }
        }

        // Reading the production ribbon's material inputs keeps this assertion tied to what the grid actually renders.
        private static Color EvaluateRibbonColor(BaseEvent source, float progress)
        {
            var eventsContainer = GetEventsContainer();
            Assert.That(
                eventsContainer.LoadedContainers.TryGetValue(source, out var objectContainer),
                Is.True,
                $"The ribbon source at beat {source.JsonTime} was not loaded.");
            Assert.That(objectContainer, Is.TypeOf<EventContainer>());

            var ribbon = objectContainer.GetComponentInChildren<LightGradientController>(true);
            Assert.That(ribbon, Is.Not.Null, "The loaded source had no ribbon controller.");
            var renderer = ribbon.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(renderer, Is.Not.Null, "The ribbon controller had no renderer.");
            Assert.That(ribbon.gameObject.activeInHierarchy, Is.True, "The production ribbon was hidden.");
            Assert.That(renderer.enabled, Is.True, "The production ribbon renderer was disabled.");

            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            var easedProgress = Easing.FromID(properties.GetInt(easingId))(progress);
            return BasicEventColorLerp.Interpolate(
                properties.GetColor(colorAId),
                properties.GetColor(colorBId),
                easedProgress,
                (BasicEventColorLerpType)properties.GetInt(useHsvId));
        }

        // The ribbon retains authored alpha. Chroma multiplies the native normal ColorSO into scene-light alpha.
        private static void AssertRibbonAndPreviewMatch(
            Color expected,
            Color ribbon,
            Color preview,
            string scenario)
        {
            const float tolerance = 0.0001f;
            Assert.That(ColorsMatch(expected, ribbon, tolerance), Is.True,
                $"Authored ribbon mismatch for {scenario}; expected {expected}, calculated {ribbon}.");
            expected.a *= .7490196f;
            Assert.That(ColorsMatch(expected, preview, tolerance), Is.True,
                $"Native Chroma preview mismatch for {scenario}; expected {expected}, calculated {preview}.");
        }

        // Component-wise comparison makes HDR alpha failures visible without relying on Unity Color equality semantics.
        private static bool ColorsMatch(Color expected, Color actual, float tolerance) =>
            Mathf.Abs(expected.r - actual.r) <= tolerance
            && Mathf.Abs(expected.g - actual.g) <= tolerance
            && Mathf.Abs(expected.b - actual.b) <= tolerance
            && Mathf.Abs(expected.a - actual.a) <= tolerance;

        // Every test uses the actual BasicLightEffect output and the actual EventContainer ribbon material properties.
        private ParityPreviewLightController CreatePreviewLight(int eventType)
        {
            previewLightObject = new GameObject($"Chroma Parity Preview Light {eventType}");
            var previewLight = previewLightObject.AddComponent<ParityPreviewLightController>();
            previewLight.Type = eventType;
            previewLight.ID = -1;

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var effect = context.Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(eventType);
            effect.Register(previewLight);
            effect.Initialize();
            return previewLight;
        }

        // Appearance flags are enabled before placement so both preview consumers receive production updates.
        private void PrepareColorPreview()
        {
            emulateChromaLiteBeforeTest ??= Settings.Instance.EmulateChromaLite;
            visualizeGradientsBeforeTest ??= Settings.Instance.VisualizeChromaGradients;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.VisualizeChromaGradients = true;
            GetEventsContainer().PropagationEditing = EventGridContainer.PropMode.Off;
        }

        // The second scenario proves legacy gradients ignore the source event's ordinary-transition lerpType in Chroma.
        private static GradientScenario GetGradientScenario(string scenario)
        {
            return scenario switch
            {
                "linearAlpha" => new GradientScenario(
                    new Color(0.2f, 0.4f, 0.8f, 0.25f),
                    new Color(0.8f, 0.6f, 0.1f, 0.75f),
                    2f,
                    "easeLinear",
                    null),
                "rgbDespiteLerpType" => new GradientScenario(
                    new Color(1f, 0f, 0.6f, 0.4f),
                    new Color(0f, 1f, 0.2f, 0.8f),
                    0.5f,
                    "easeInQuad",
                    "HSV"),
                _ => throw new System.ArgumentOutOfRangeException(nameof(scenario), scenario, null)
            };
        }

        // Both ordinary-transition scenarios vary alpha and brightness independently so endpoint composition is observable.
        private static TransitionScenario GetTransitionScenario(string scenario)
        {
            return scenario switch
            {
                "linearRgb" => new TransitionScenario(
                    new Color(0.2f, 0.4f, 0.8f, 0.25f),
                    new Color(0.8f, 0.6f, 0.1f, 0.75f),
                    2f,
                    4f,
                    "easeLinear"),
                "easedRgb" => new TransitionScenario(
                    new Color(0.9f, 0.1f, 0.3f, 0.8f),
                    new Color(0.1f, 0.7f, 0.9f, 0.2f),
                    0.5f,
                    3f,
                    "easeInQuad"),
                _ => throw new System.ArgumentOutOfRangeException(nameof(scenario), scenario, null)
            };
        }

        private readonly struct GradientScenario
        {
            public GradientScenario(
                Color startColor,
                Color endColor,
                float brightness,
                string easing,
                string serializedLerpType)
            {
                StartColor = startColor;
                EndColor = endColor;
                Brightness = brightness;
                Easing = easing;
                SerializedLerpType = serializedLerpType;
            }

            public Color StartColor { get; }
            public Color EndColor { get; }
            public float Brightness { get; }
            public string Easing { get; }
            public string SerializedLerpType { get; }
        }

        private readonly struct TransitionScenario
        {
            public TransitionScenario(
                Color startColor,
                Color endColor,
                float startBrightness,
                float endBrightness,
                string easing)
            {
                StartColor = startColor;
                EndColor = endColor;
                StartBrightness = startBrightness;
                EndBrightness = endBrightness;
                Easing = easing;
            }

            public Color StartColor { get; }
            public Color EndColor { get; }
            public float StartBrightness { get; }
            public float EndBrightness { get; }
            public string Easing { get; }
        }

        // A test-only controller captures the exact color delivered by the production BasicLightEffect tween.
        public sealed class ParityPreviewLightController : LightController
        {
            protected override bool Initialize() => true;

            public override void SetColor(Color color) => Color = color;
        }
    }
}

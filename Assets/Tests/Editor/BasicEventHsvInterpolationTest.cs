using System.Collections;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Basic Event HSV regressions must validate the separately implemented preview tween and ribbon shader paths.
    public class BasicEventHsvInterpolationTest : BasicEventChunkingTestBase
    {
        private static readonly int colorAId = Shader.PropertyToID("_ColorA");
        private static readonly int colorBId = Shader.PropertyToID("_ColorB");
        private static readonly int easingId = Shader.PropertyToID("_EasingID");
        private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");

        private bool? emulateChromaLiteBeforeTest;
        private bool? visualizeGradientsBeforeTest;
        private GameObject previewLightObject;

        // Each test changes shared preview or ribbon settings and may register a synthetic light with the environment effect.
        protected override void AfterCleanup()
        {
            if (previewLightObject != null)
            {
                var previewLight = previewLightObject.GetComponent<HsvPreviewLightController>();
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

        // Both reported transitions must expose legacy HSV's green detour and trueHSV's shortest red/pink arc.
        [TestCase("whiteToPink", "HSV", 0.5f, 1f, 0.85f)]
        [TestCase("whiteToPink", "trueHSV", 1f, 0.5f, 0.65f)]
        [TestCase("pinkToRed", "HSV", 0f, 1f, 0.7f)]
        [TestCase("pinkToRed", "trueHSV", 1f, 0f, 0.3f)]
        public void PreviewUsesInterpolationSelectedByLerpType(
            string transition,
            string lerpType,
            float expectedRed,
            float expectedGreen,
            float expectedBlue)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previewLight = CreatePreviewLight((int)EventTypeValue.Event2);
            EnableChromaLitePreview();
            var colors = GetTransitionColors(transition);
            PlaceHsvTransition(lerpType, EventTypeValue.Event2, colors.Start, colors.End);

            atsc.MoveToJsonTime(1f);
            atsc.MoveToJsonTime(2f);

            AssertColor(
                new Color(expectedRed, expectedGreen, expectedBlue, 1f),
                previewLight.Color,
                $"Preview {transition} lerpType {lerpType}");
        }

        // Existing HSV data is legacy normalized interpolation and must be visibly distinguished from trueHSV on nodes.
        [UnityTest]
        public IEnumerator LegacyHsvNodeDisplaysLhsvLabel()
        {
            yield return AssertNodeLerpTypeLabel("HSV", "LHSV");
        }

        // Authored trueHSV data keeps the concise HSV label because it represents conventional angular interpolation.
        [UnityTest]
        public IEnumerator TrueHSVNodeDisplaysHSVLabel()
        {
            yield return AssertNodeLerpTypeLabel("trueHSV", "HSV");
        }

        // The legacy HSV ribbon must pass through green when white's zero hue is interpolated numerically toward pink.
        [UnityTest]
        public IEnumerator WhiteToPinkRibbonUsesLegacyNormalizedHueLerpForHsv()
        {
            EnableRibbonRendering();
            var whiteToPink = PlaceHsvTransition(
                "HSV",
                EventTypeValue.Event2,
                Color.white,
                new Color(1f, 0f, 0.6f, 1f));
            yield return null;

            AssertRibbonMidpoint(
                whiteToPink,
                true,
                "whiteToPink lerpType HSV");
        }

        // The legacy HSV ribbon must traverse most of the wheel through green when pink's hue lerps numerically to red.
        [UnityTest]
        public IEnumerator PinkToRedRibbonUsesLegacyNormalizedHueLerpForHsv()
        {
            EnableRibbonRendering();
            var pinkToRed = PlaceHsvTransition(
                "HSV",
                EventTypeValue.Event2,
                new Color(1f, 0f, 0.6f, 1f),
                Color.red);
            yield return null;

            AssertRibbonMidpoint(
                pinkToRed,
                true,
                "pinkToRed lerpType HSV");
        }

        // The trueHSV ribbon must take the shortest red/pink arc from white instead of using RGB or legacy hue lerping.
        [UnityTest]
        public IEnumerator WhiteToPinkRibbonUsesAngularHueLerpForTrueHSV()
        {
            EnableRibbonRendering();
            var whiteToPink = PlaceHsvTransition(
                "trueHSV",
                EventTypeValue.Event2,
                Color.white,
                new Color(1f, 0f, 0.6f, 1f));
            yield return null;

            AssertRibbonMidpoint(
                whiteToPink,
                false,
                "whiteToPink lerpType trueHSV");
        }

        // The trueHSV ribbon must cross the hue seam directly from pink to red without visiting green.
        [UnityTest]
        public IEnumerator PinkToRedRibbonUsesAngularHueLerpForTrueHSV()
        {
            EnableRibbonRendering();
            var pinkToRed = PlaceHsvTransition(
                "trueHSV",
                EventTypeValue.Event2,
                new Color(1f, 0f, 0.6f, 1f),
                Color.red);
            yield return null;

            AssertRibbonMidpoint(
                pinkToRed,
                false,
                "pinkToRed lerpType trueHSV");
        }

        // Rendering the production ribbon shader catches a mode that is routed correctly but executes the wrong hue math.
        private static void AssertRibbonMidpoint(
            (BaseEvent Source, BaseEvent Target) events,
            bool expectGreenPath,
            string scenario)
        {
            AssertVisibleRibbon(events.Source, events.Target, $"for {scenario}");
            var sourceContainer = (EventContainer)GetEventsContainer().LoadedContainers[events.Source];
            var ribbon = sourceContainer.GetComponentInChildren<LightGradientController>(true);
            var renderer = ribbon.GetComponentInChildren<MeshRenderer>(true);
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);

            // The GPU assertion is meaningful only when the production ribbon retained both authored custom colors.
            AssertColor(
                events.Source.CustomColor.Value,
                properties.GetColor(colorAId),
                $"Ribbon shader {scenario} start endpoint");
            AssertColor(
                events.Target.CustomColor.Value,
                properties.GetColor(colorBId),
                $"Ribbon shader {scenario} end endpoint");

            // The property mode proves authored lerpType reached the shader independently from its rendered color result.
            var expectedMode = BasicEventColorLerp.FromSerializedName(events.Source.CustomLerpType);
            Assert.That(
                properties.GetInt(useHsvId),
                Is.EqualTo((int)expectedMode),
                $"Ribbon shader {scenario} interpolation mode");

            // Tone mapping and SrcColor blending preserve channel order, which directly distinguishes green detours.
            var renderedMidpoint = RenderMidpoint(renderer.sharedMaterial, properties);
            if (expectGreenPath)
            {
                Assert.That(
                    renderedMidpoint.g,
                    Is.GreaterThan(renderedMidpoint.r),
                    $"Ribbon shader {scenario} did not pass through green: {renderedMidpoint}");
                Assert.That(
                    renderedMidpoint.g,
                    Is.GreaterThan(renderedMidpoint.b),
                    $"Ribbon shader {scenario} did not make green the dominant midpoint channel: {renderedMidpoint}");
            }
            else
            {
                Assert.That(
                    renderedMidpoint.r,
                    Is.GreaterThan(renderedMidpoint.g),
                    $"Ribbon shader {scenario} crossed green instead of staying on the red/pink arc: {renderedMidpoint}");
                Assert.That(
                    renderedMidpoint.r,
                    Is.GreaterThan(renderedMidpoint.b),
                    $"Ribbon shader {scenario} did not keep red dominant on the shortest arc: {renderedMidpoint}");
            }
        }

        // Node labels must come from the production EventAppearanceSO output rather than a parallel test formatter.
        private static IEnumerator AssertNodeLerpTypeLabel(string lerpType, string expectedLabel)
        {
            var source = PlaceHsvTransition(
                lerpType,
                EventTypeValue.Event2,
                Color.white,
                new Color(1f, 0f, 0.6f, 1f)).Source;
            yield return null;

            var sourceContainer = (EventContainer)GetEventsContainer().LoadedContainers[source];
            var valueDisplayField = typeof(EventContainer).GetField(
                "valueDisplay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(valueDisplayField, Is.Not.Null, "The Basic Event value display field was unavailable.");
            var valueDisplay = (TextMeshPro)valueDisplayField.GetValue(sourceContainer);
            var labelLines = valueDisplay.text.Split('\n');
            Assert.That(
                labelLines[labelLines.Length - 1],
                Is.EqualTo(expectedLabel),
                $"Node label for lerpType {lerpType}");
        }

        // Both preview cases use the same real BasicLightEffect registration so only lerpType changes between fixtures.
        private HsvPreviewLightController CreatePreviewLight(int eventType)
        {
            previewLightObject = new GameObject($"HSV Preview Test Light {eventType}");
            var previewLight = previewLightObject.AddComponent<HsvPreviewLightController>();
            previewLight.Type = eventType;
            previewLight.ID = -1;

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var effect = context.Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(eventType);
            effect.Register(previewLight);
            effect.Initialize();
            return previewLight;
        }

        // Each authored pair differs only by endpoint colors, event lane, and the customData lerpType under test.
        private static (BaseEvent Source, BaseEvent Target) PlaceHsvTransition(
            string lerpType,
            EventTypeValue eventType,
            Color startColor,
            Color endColor)
        {
            var source = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 1f,
                Type = (int)eventType,
                Value = (int)LightValue.RedOn,
                FloatValue = 1f,
                CustomColor = startColor,
                CustomLerpType = lerpType
            });
            var target = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 3f,
                Type = (int)eventType,
                Value = (int)LightValue.BlueTransition,
                FloatValue = 1f,
                CustomColor = endColor
            });
            return (source, target);
        }

        // Named scenarios keep NUnit cases readable while sharing the exact colors used by ribbon coverage.
        private static (Color Start, Color End) GetTransitionColors(string transition)
        {
            return transition switch
            {
                "whiteToPink" => (Color.white, new Color(1f, 0f, 0.6f, 1f)),
                "pinkToRed" => (new Color(1f, 0f, 0.6f, 1f), Color.red),
                _ => throw new System.ArgumentOutOfRangeException(nameof(transition), transition, null)
            };
        }

        // Custom colors are required for the live preview to expose the selected HSV interpolation semantics.
        private void EnableChromaLitePreview()
        {
            emulateChromaLiteBeforeTest ??= Settings.Instance.EmulateChromaLite;
            Settings.Instance.EmulateChromaLite = true;
        }

        // Ribbon tests opt into transition visualization before placement so production appearance builds the shader data.
        private void EnableRibbonRendering()
        {
            visualizeGradientsBeforeTest ??= Settings.Instance.VisualizeChromaGradients;
            emulateChromaLiteBeforeTest ??= Settings.Instance.EmulateChromaLite;
            Settings.Instance.VisualizeChromaGradients = true;
            Settings.Instance.EmulateChromaLite = true;
            GetEventsContainer().PropagationEditing = EventGridContainer.PropMode.Off;
        }

        // The actual ribbon property block supplies its authored colors, easing, and interpolation mode to the GPU test.
        private static Color RenderMidpoint(Material sourceMaterial, MaterialPropertyBlock properties)
        {
            return RenderShaderColor(
                sourceMaterial,
                properties.GetColor(colorAId),
                properties.GetColor(colorBId),
                properties.GetInt(easingId),
                properties.GetInt(useHsvId));
        }

        // Explicit UV-0.5 geometry produces the transition midpoint through the shipped ribbon shader on every backend.
        private static Color RenderShaderColor(
            Material sourceMaterial,
            Color startColor,
            Color endColor,
            int easing,
            int useHsv)
        {
            Assert.That(sourceMaterial, Is.Not.Null, "The transition ribbon had no source material.");
            var material = new Material(sourceMaterial);
            var renderTexture = new RenderTexture(
                1,
                1,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            var resultTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            var midpointMesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0f, 0f),
                    new Vector3(1f, 0f),
                    new Vector3(1f, 1f),
                    new Vector3(0f, 1f)
                },
                uv = new[]
                {
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            var previousRenderTexture = RenderTexture.active;

            try
            {
                material.SetColor(colorAId, startColor);
                material.SetColor(colorBId, endColor);
                material.SetInt(easingId, easing);
                material.SetInt(useHsvId, useHsv);
                renderTexture.Create();
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.black);
                // Direct3D one-pixel blits can expose UV 0, so draw explicit UV-0.5 geometry for a true midpoint sample.
                GL.PushMatrix();
                GL.LoadOrtho();
                material.SetPass(0);
                Graphics.DrawMeshNow(midpointMesh, Matrix4x4.identity);
                GL.PopMatrix();
                resultTexture.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0, false);
                resultTexture.Apply(false, false);
                return resultTexture.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                renderTexture.Release();
                Object.DestroyImmediate(resultTexture);
                Object.DestroyImmediate(midpointMesh);
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(material);
            }
        }

        // Channel-level assertions distinguish the green legacy midpoint from angular pink and ordinary RGB blending.
        private static void AssertColor(Color expected, Color actual, string context, float tolerance = 0.001f)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), $"{context} red channel");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), $"{context} green channel");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), $"{context} blue channel");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), $"{context} alpha channel");
        }

        // This fixture needs an independent scene light so its preview assertions do not depend on another test class's nested type.
        public sealed class HsvPreviewLightController : LightController
        {
            protected override bool Initialize() => true;

            public override void SetColor(Color color) => Color = color;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    // Checks ribbon source retention, cache updates after edits, and rendered output using the production
    // timeline and materials.
    public partial class GLSColorTransitionCacheTest : TestBase
    {
        private static readonly int colorAId = Shader.PropertyToID("_ColorA");
        private static readonly int colorBId = Shader.PropertyToID("_ColorB");
        private static readonly int easingId = Shader.PropertyToID("_EasingID");
        private static readonly int strobeColorAId = Shader.PropertyToID("_StrobeColorA");
        private static readonly int strobeColorBId = Shader.PropertyToID("_StrobeColorB");
        private static readonly int strobeFadeId = Shader.PropertyToID("_StrobeFade");
        private static readonly int strobeFrequencyAId = Shader.PropertyToID("_StrobeFrequencyA");
        private static readonly int strobeFrequencyBId = Shader.PropertyToID("_StrobeFrequencyB");
        private static readonly int strobeDurationId = Shader.PropertyToID("_StrobeDuration");
        private static readonly int useStrobeColorsId = Shader.PropertyToID("_UseStrobeColors");
        private static readonly int lightDistributionTextureId = Shader.PropertyToID("_LightDistributionTex");
        private static readonly int lightDistributionWidthId = Shader.PropertyToID("_LightDistributionWidth");
        private static readonly int useLightDistributionId = Shader.PropertyToID("_UseLightDistribution");
        private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");

        private BaseDifficulty originalMap;

        [OneTimeSetUp]
        public void CaptureOriginalMap()
        {
            // Restore the scene-owned map after this isolated cache fixture so subsequent editor tests keep their shared state.
            originalMap = BeatSaberSongContainer.Instance.Map;
        }

        [OneTimeTearDown]
        public void RestoreOriginalMap()
        {
            // Avoid leaking the synthetic cache fixture map into test fixtures that run after this one.
            BeatSaberSongContainer.Instance.Map = originalMap;
        }

        [Test]
        public void DenseNeighborTimelineFitsHardwareAndRendersItsFinalTransition()
        {
            var maximumSize = SystemInfo.maxTextureSize;
            var neighborCount = maximumSize / 9 + 16;
            var groupJson = CreateGroup(0f, 0, 0, filterParam: 0, filterType: 2,
                filterChunks: 0, brightnessDistribution: 0f);
            var neighborBox = groupJson["e"][0].Clone();
            neighborBox["f"]["p"] = 1;
            var events = new JSONArray();
            for (var index = 0; index < neighborCount; index++)
            {
                events.Add(new JSONObject
                {
                    ["b"] = index * 10f / (neighborCount - 1),
                    ["i"] = 0,
                    ["c"] = 1,
                    ["s"] = 1
                });
            }
            neighborBox["e"] = events;
            ((JSONArray)groupJson["e"]).Add(neighborBox);
            var map = LoadMap(CreateDifficultyJson(groupJson));
            var group = map.LightColorEventBoxGroups[0];
            var owner = group.Boxes[0].Events[0];
            owner.CustomColor = Color.red;
            foreach (var evt in group.Boxes[1].Events)
            {
                evt.CustomColor = Color.blue;
            }
            group.Boxes[1].Events[^1].CustomColor = Color.green;

            var timeline = new GLSColorTimeline(map, 2, map.LightColorEventBoxGroups);
            using var preview = new GLSColorTransitionPreview();
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            Material material = null;
            try
            {
                var properties = new MaterialPropertyBlock();
                Assert.That(preview.UpdateTimeline(
                    timeline, owner, false, appearance, _ => false, properties,
                    out var start, out var end, true), Is.True);
                var texture = properties.GetTexture(lightDistributionTextureId);
                Assert.That(texture.width, Is.LessThanOrEqualTo(maximumSize));
                Assert.That(texture.height, Is.LessThanOrEqualTo(maximumSize));
                Assert.That(end, Is.GreaterThan(10.5f));
                material = CreateWaveSampleMaterial(properties);
                // Put the pixel center inside the owned strip while one quarter samples its neighbor.
                // A center on the foreign strip belongs to that other node's independently rendered ribbon.
                const float inset = 0.25f / 101f;
                var early = RenderGradientColumn(
                    material, (0.5f - start) / (end - start), 101, -inset, 1f - inset)[50].gamma;
                var late = RenderGradientColumn(
                    material, (10.5f - start) / (end - start), 101, -inset, 1f - inset)[50].gamma;
                var red = CalculateExpectedRibbonPixel(Color.red);
                var blue = CalculateExpectedRibbonPixel(Color.blue);
                var green = CalculateExpectedRibbonPixel(Color.green);
                var expectedEarly = Color.Lerp(red.linear, blue.linear, 0.25f).gamma;
                var expectedLate = Color.Lerp(red.linear, green.linear, 0.25f).gamma;
                expectedEarly.a = 1f;
                expectedLate.a = 1f;
                AssertColor(early, expectedEarly,
                    "The first neighboring intervals must still blend blue at the strip edge.", 0.025f);
                AssertColor(late, expectedLate,
                    "The final neighboring interval must remain visible beyond the old texture-height limit.", 0.025f);
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(appearance);
            }
        }

        [Test]
        public void IoElPhysicalTimelineUploadsTheBeatSaberShaderCase()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = Color.red;
            transition.CustomColor = Color.blue;
            transition.Easing = (int)EaseType.BeatSaberInOutElastic;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS physical IOEl ribbon test");
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 1);

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(Shader.PropertyToID("_UseLightTimeline")), Is.EqualTo(1f));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                Assert.That(
                    Mathf.RoundToInt(texture.GetPixel(0, 8).g),
                    Is.EqualTo(Easing.EasingShaderId("easeBeatSaberInOutElastic")),
                    "The physical GLS color timeline must upload IOEl, not Linear or the standard IOTEl curve.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // The light shader consumes tween colors directly. An extra sRGB conversion would turn this constant-
        // brightness yellow midpoint into a dim brown ribbon.
        [Test]
        public void HundredBrightnessRedToGreenRibbonMatchesGameYellowMidpoint()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(38f, 2, 0),
                CreateGroup(42.016f, 2, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = Color.red;
            source.Brightness = 1f;
            transition.CustomColor = Color.green;
            transition.Brightness = 1f;
            transition.Easing = (int)EaseType.InOutQuartic;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS constant-brightness red-green ribbon test");
            Material ribbonMaterial = null;
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 1);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                ribbonMaterial = CreateWaveSampleMaterial(properties);

                var startPixel = RenderGradientPixel(ribbonMaterial, 0f, 0.5f).gamma;
                var midpointPixel = RenderGradientPixel(ribbonMaterial, 0.5f, 0.5f).gamma;
                var endPixel = RenderGradientPixel(ribbonMaterial, 1f, 0.5f).gamma;
                Assert.That(
                    midpointPixel.r,
                    Is.EqualTo(midpointPixel.g).Within(0.02f),
                    $"The transition midpoint must be yellow, not hue-shifted: {midpointPixel}.");
                Assert.That(
                    midpointPixel.maxColorComponent,
                    Is.EqualTo(Mathf.Min(startPixel.maxColorComponent, endPixel.maxColorComponent)).Within(0.03f),
                    $"Brightness 100 -> 100 must not visually dip at the midpoint: {startPixel} -> {midpointPixel} -> {endPixel}.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonMaterial);
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // At brightness 400, ribbons blend halfway to white. Even extreme brightness must retain at least 15%
        // of the original color.
        [Test]
        public void HighBrightnessRibbonUsesAsymptoticWhiteBlend()
        {
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient")) { enableInstancing = false };
            var originalBaseColorBoost = Shader.GetGlobalFloat("_BaseColorBoost");
            var originalBaseColorBoostThreshold = Shader.GetGlobalFloat("_BaseColorBoostThreshold");
            try
            {
                Shader.SetGlobalFloat("_BaseColorBoost", 1f);
                Shader.SetGlobalFloat("_BaseColorBoostThreshold", 0.1f);
                material.SetInt("_EasingID", 0);
                material.SetInt("_UseHSV", 0);
                var authored = new Color(0.141f, 1f, 0.969f, 1.5f);
                material.SetVector("_ColorA", authored);
                material.SetVector("_ColorB", authored);
                var at150 = RenderGradientPixel(material, 0.5f, 0.5f).gamma;
                var normalized150 = at150 / at150.maxColorComponent;
                Assert.That(normalized150.r, Is.LessThan(0.35f), $"Level 150 cyan must not clip white: {at150}.");
                Assert.That(normalized150.b, Is.GreaterThan(0.9f), $"Level 150 must retain the authored cyan hue: {at150}.");

                authored.a = 4f;
                material.SetVector("_ColorA", authored);
                material.SetVector("_ColorB", authored);
                var at400 = RenderGradientPixel(material, 0.5f, 0.5f).gamma;
                var normalized400 = at400 / at400.maxColorComponent;
                Assert.That(normalized400.r, Is.EqualTo(0.5705f).Within(0.02f), $"Level 400 must blend the red channel halfway to white: {at400}.");
                Assert.That(normalized400.b, Is.EqualTo(0.9845f).Within(0.02f), $"Level 400 must blend the blue channel halfway to white: {at400}.");

                // Sample extreme brightness to catch a white blend that incorrectly approaches 100%.
                authored.a = 100000f;
                material.SetVector("_ColorA", authored);
                material.SetVector("_ColorB", authored);
                var nearAsymptote = RenderGradientPixel(material, 0.5f, 0.5f).gamma;
                var normalizedAsymptote = nearAsymptote / nearAsymptote.maxColorComponent;
                Assert.That(normalizedAsymptote.r, Is.EqualTo(0.8712f).Within(0.02f), $"Extreme brightness must approach an 85% white blend: {nearAsymptote}.");
                Assert.That(normalizedAsymptote.b, Is.EqualTo(0.9954f).Within(0.02f), $"Extreme brightness must retain some authored cyan below the 85% white cap: {nearAsymptote}.");
            }
            finally
            {
                Shader.SetGlobalFloat("_BaseColorBoost", originalBaseColorBoost);
                Shader.SetGlobalFloat("_BaseColorBoostThreshold", originalBaseColorBoostThreshold);
                Object.DestroyImmediate(material);
            }
        }

        // Each light needs its own normal and strobe endpoints in the lookup texture.
        [Test]
        public void LightIdTransitionRibbonSplitsIntoPerLightColorDistributionStrips()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(
                    1f,
                    0,
                    0,
                    filterParam: 1,
                    boxCustomData: CreateColorDistributionCustomData(
                        new[] { "r,0.5,L,l" },
                        new[] { "b,0.25,L,l" }),
                    filterType: (int)IndexFilterType.Division),
                CreateGroup(
                    5f,
                    0,
                    1,
                    filterParam: 1,
                    boxCustomData: CreateColorDistributionCustomData(
                        new[] { "g,0.75,L,l" },
                        new[] { "r,0.6,L,l" }),
                    filterType: (int)IndexFilterType.Division)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            source.Brightness = 0.5f;
            source.StrobeColor = new Color(0.2f, 0.1f, 0.4f, 1f);
            source.StrobeBrightness = 0.4f;
            source.Frequency = 1;
            transition.CustomColor = new Color(0.2f, 0.1f, 1f, 1f);
            transition.Brightness = 1f;
            transition.StrobeColor = new Color(0.9f, 0.3f, 0.2f, 1f);
            transition.StrobeBrightness = 0.75f;
            transition.Frequency = 2;
            transition.Easing = (int)EaseType.InQuadratic;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS light-id ribbon test");

            try
            {
                AssertColor(
                    GLSEventCommon.GetLightColor(source, false, appearance),
                    new Color(0.1f, 0.2f, 0.3f, 0.5f));
                var sourceMainColors = new Color[4];
                var sourceStrobeColors = new Color[4];
                Assert.That(
                    GLSEventCommon.PopulateColorTransitionEndpoint(
                        source,
                        4,
                        false,
                        appearance,
                        sourceMainColors,
                        sourceStrobeColors),
                    Is.True);
                AssertColor(
                    sourceMainColors[0],
                    new Color(0.1f, 0.2f, 0.3f, 0.5f),
                    $"source main endpoint table: {string.Join(", ", sourceMainColors.Select(x => GLSRibbonTestDiagnostics.FormatColor(x)))}");
                var controller = CreateRibbonController(ribbonObject, out var renderer);

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 4);

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(
                    properties.GetFloat(useLightDistributionId),
                    Is.EqualTo(1f),
                    "A filtered GLS transition must upload one endpoint strip per physical light.");
                Assert.That(properties.GetFloat(lightDistributionWidthId), Is.EqualTo(4f));
                Assert.That(properties.GetFloat(Shader.PropertyToID("_UseLightTimeline")), Is.EqualTo(1f));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(4));
                // The shader reads complete neighboring tweens when needed;
                // validate the rendered endpoints below rather than bank capacity.
                var textureDump = DescribeTexture(texture);
                for (var lightIndex = 0; lightIndex < 4; lightIndex++)
                {
                    var lightProgress = lightIndex / 3f;
                    var textureX = 3 - lightIndex;
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        new Color(0.1f + (0.5f * lightProgress), 0.2f, 0.3f, 0.5f),
                        textureDump,
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 1),
                        new Color(0.2f, 0.1f + (0.75f * lightProgress), 1f, 1f),
                        textureDump,
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 2),
                        new Color(0.2f, 0.1f, 0.4f + (0.25f * lightProgress), 0.4f),
                        textureDump,
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 3),
                        new Color(0.9f + (0.6f * lightProgress), 0.3f, 0.2f, 0.75f),
                        textureDump,
                        0.001f);
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        [Test]
        public void LightIdTransitionRibbonKeepsSparseLanesBlackWithoutColorDistributions()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(
                    1f,
                    0,
                    0,
                    filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2,
                    filterChunks: 0,
                    brightnessDistribution: 0f),
                CreateGroup(
                    5f,
                    0,
                    1,
                    filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2,
                    filterChunks: 0,
                    brightnessDistribution: 0f)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = new Color(0.4f, 0.1f, 0.1f, 1f);
            source.Brightness = 0.5f;
            transition.CustomColor = new Color(0.1f, 0.1f, 0.4f, 1f);
            transition.Brightness = 1f;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS sparse light-id ribbon test");

            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 4);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                Assert.That(properties.GetFloat(useStrobeColorsId), Is.EqualTo(0f));
                Assert.That(texture, Is.Not.Null);
                var textureDump = DescribeTexture(texture);
                for (var lightIndex = 0; lightIndex < 4; lightIndex++)
                {
                    var textureX = 3 - lightIndex;
                    var selected = lightIndex is 0 or 2;
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        selected
                            ? new Color(0.4f, 0.1f, 0.1f, 0.5f)
                            : Color.black,
                        textureDump);
                    AssertColor(
                        texture.GetPixel(textureX, 1),
                        selected
                            ? new Color(0.1f, 0.1f, 0.4f, 1f)
                            : Color.black,
                        textureDump,
                        0.001f);
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // AllLightTransitionRibbonClearsPooledDistributionState guards uniform endpoint replacement and stale lookup cleanup when a pooled renderer changes transition types.
        [Test]
        public void AllLightTransitionRibbonClearsPooledDistributionState()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(
                    1f,
                    0,
                    0,
                    boxCustomData: CreateColorDistributionCustomData(
                        new[] { "r,0.5,L,l" },
                        System.Array.Empty<string>())),
                CreateGroup(5f, 0, 1),
                CreateGroup(6f, 0, 0, brightnessDistribution: 0f),
                CreateGroup(10f, 0, 1, brightnessDistribution: 0f)));
            var distributedSource = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var uniformSource = map.LightColorEventBoxGroups[2].Boxes[0].Events[0];
            distributedSource.CustomColor = new Color(0.4f, 0.1f, 0.1f, 1f);
            uniformSource.CustomColor = new Color(0.1f, 0.1f, 0.4f, 1f);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS uniform ribbon test");

            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                var properties = new MaterialPropertyBlock();
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller,
                    distributedSource,
                    appearance,
                    _ => false,
                    4);
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));

                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller,
                    uniformSource,
                    appearance,
                    _ => false,
                    4);
                renderer.GetPropertyBlock(properties);
                // A uniform physical timeline still needs its independent clocks; verify it erased all old color-distributed endpoint rows.
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                Assert.That(properties.GetFloat(lightDistributionWidthId), Is.EqualTo(4f));
                var uniformTexture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.NotNull(uniformTexture);
                for (var light = 0; light < 4; light++)
                    AssertColor(uniformTexture.GetPixel(light, 0), new Color(0.1f, 0.1f, 0.4f, 1f));
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // OuterGlsNodeTransitionRibbonUsesPhysicalLightStrips proves the outer preview passes its represented event and environment light count into the shared distributed-ribbon path.
        [Test]
        public void OuterGlsNodeTransitionRibbonUsesPhysicalLightStrips()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(
                    1f,
                    0,
                    0,
                    boxCustomData: CreateColorDistributionCustomData(
                        new[] { "r,0.5,L,l" },
                        System.Array.Empty<string>()),
                    brightnessDistribution: 0f),
                CreateGroup(
                    5f,
                    0,
                    1,
                    boxCustomData: CreateColorDistributionCustomData(
                        new[] { "g,0.75,L,l" },
                        System.Array.Empty<string>()),
                    brightnessDistribution: 0f)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            source.Brightness = 0.5f;
            transition.CustomColor = new Color(0.2f, 0.1f, 1f, 1f);
            transition.Brightness = 1f;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var groupAppearance = ScriptableObject.CreateInstance<GLSGroupAppearanceSO>();
            var appearanceField = typeof(GLSGroupAppearanceSO).GetField(
                "eventAppearance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(appearanceField, Is.Not.Null);
            appearanceField.SetValue(groupAppearance, appearance);
            var containerObject = new GameObject("GLS outer transition ribbon test");
            try
            {
                var container = containerObject.AddComponent<GLSGroupContainer>();
                // OuterGlsNodeTransitionRibbonUsesPhysicalLightStrips uses a preview ghost so test cleanup does not require collection singletons.
                var previewGhostField = typeof(GLSGroupContainer).GetField(
                    "isPreviewGhost",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(previewGhostField, Is.Not.Null);
                previewGhostField.SetValue(container, true);
                container.PreviewEventData = source;
                container.GlsLightCount = 4;
                container.lightGradientController = CreateRibbonController(
                    containerObject,
                    out var renderer);
                groupAppearance.UpdateTransitionRibbon(container, _ => false);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                Assert.That(texture, Is.Not.Null);
                AssertColor(
                    texture.GetPixel(3, 0),
                    new Color(0.1f, 0.2f, 0.3f, 0.5f),
                    DescribeTexture(texture),
                    0.001f);
            }
            finally
            {
                Object.DestroyImmediate(containerObject);
                Object.DestroyImmediate(groupAppearance);
                Object.DestroyImmediate(appearance);
            }
        }

        // DistributedStrobeRibbonRendersEveryLightStripAcrossWholeGradient draws isolated UV points so the shader itself proves strip selection and the removed right-half strobe restriction.
        [Test]
        public void DistributedStrobeRibbonRendersEveryLightStripAcrossWholeGradient()
        {
            var shader = Shader.Find("ChroMapper/Object/Basic Gradient");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader)
            {
                enableInstancing = false
            };
            var endpointTexture = new Texture2D(
                4,
                4,
                TextureFormat.RGBAHalf,
                false,
                true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var textureColors = new Color[16];
                // The shader reads row 0/1 as normal endpoints and row 2/3 as strobe endpoints; lane one is red→green and lane two is blue→blue.
                textureColors[1 + 4] = Color.red;
                textureColors[2 + 4] = Color.blue;
                textureColors[1 + 12] = Color.green;
                textureColors[2 + 12] = Color.blue;
                endpointTexture.SetPixels(textureColors);
                endpointTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", endpointTexture);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 4f);
                material.SetFloat("_UseStrobeColors", 1f);
                material.SetFloat("_StrobeFade", 0f);
                material.SetFloat("_StrobeDuration", 1f);
                material.SetFloat("_StrobeFrequencyA", 0f);
                material.SetFloat("_StrobeFrequencyB", 1f);
                material.SetFloat("_EasingID", 0f);
                material.SetFloat("_UseHSV", 0f);
                material.SetVector("_ColorA", Color.black);
                material.SetVector("_ColorB", Color.black);
                material.SetVector("_StrobeColorA", Color.black);
                material.SetVector("_StrobeColorB", Color.black);

                var firstNormal = RenderGradientPixel(material, 0.5f, 0.3f);
                var secondNormal = RenderGradientPixel(material, 0.5f, 0.7f);
                var firstStrobe = RenderGradientPixel(material, 1f, 0.3f);
                var secondStrobe = RenderGradientPixel(material, 1f, 0.7f);

                Assert.That(firstNormal.r, Is.GreaterThan(firstNormal.g));
                Assert.That(secondNormal.b, Is.GreaterThan(secondNormal.r));
                Assert.That(firstStrobe.g, Is.GreaterThan(firstStrobe.r));
                Assert.That(secondStrobe.b, Is.GreaterThan(secondStrobe.g));
            }
            finally
            {
                Object.DestroyImmediate(endpointTexture);
                Object.DestroyImmediate(material);
            }
        }

        // A pixel centered between blue and white strips should receive half of each color; pixels wholly
        // inside either strip remain unchanged.
        [Test]
        public void StripBoundaryAntiAliasingBlendsPixelsStraddlingStripEdges()
        {
            var shader = Shader.Find("ChroMapper/Object/Basic Gradient");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader)
            {
                enableInstancing = false
            };
            var endpointTexture = new Texture2D(
                2,
                4,
                TextureFormat.RGBAHalf,
                false,
                true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var textureColors = new Color[8];
                textureColors[0] = Color.blue;
                textureColors[1] = Color.white;
                textureColors[2] = Color.blue;
                textureColors[3] = Color.white;
                endpointTexture.SetPixels(textureColors);
                endpointTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", endpointTexture);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 2f);
                material.SetFloat("_EasingID", 0f);
                material.SetVector("_ColorA", Color.black);
                material.SetVector("_ColorB", Color.black);

                // 101 rows puts row 50's centre exactly on the strip boundary; rows 49/51 sit fully inside each strip.
                var column = RenderGradientColumn(material, 0f, 101);

                var expectedBelow = CalculateExpectedRibbonPixel(Color.blue);
                var expectedAbove = CalculateExpectedRibbonPixel(Color.white);
                // AA blends the two strips' presented colors, so the boundary is the midpoint of what each strip renders.
                // Pixel coverage averages linear displayed light, matching a
                // supersampled render before conversion to the PNG/screen gamma.
                var expectedBoundary = ((expectedBelow.linear + expectedAbove.linear) * 0.5f).gamma;
                Assert.That(column[49].gamma.r, Is.EqualTo(expectedBelow.r).Within(0.02f));
                Assert.That(column[51].gamma.r, Is.EqualTo(expectedAbove.r).Within(0.02f));
                Assert.That(column[50].gamma.r, Is.EqualTo(expectedBoundary.r).Within(0.02f),
                    "The pixel straddling the strip boundary must blend both strips instead of snapping to one");
                Assert.That(column[50].gamma.g, Is.EqualTo(expectedBoundary.g).Within(0.02f));
                Assert.That(column[50].gamma.b, Is.EqualTo(expectedBoundary.b).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(endpointTexture);
                Object.DestroyImmediate(material);
            }
        }

        // Blend evaluated light colors at strip edges. Interpolating packed times, frequencies, or flags
        // would invent a transition neither light has.
        [Test]
        public void StripBoundaryAntiAliasingBlendsEvaluatedTimelinesNotPackedRows()
        {
            var shader = Shader.Find("ChroMapper/Object/Basic Gradient");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader)
            {
                enableInstancing = false
            };
            var timelineTexture = new Texture2D(
                2,
                9,
                TextureFormat.RGBAFloat,
                false,
                true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var rows = new Color[18];
                // Give neighboring strips distinct colors and control values so blending packed rows cannot
                // pass as color anti-aliasing.
                rows[0] = new Color(0.5f, 0f, 0f, 1f);
                rows[2] = new Color(0.5f, 0f, 0f, 1f);
                rows[4 * 2] = new Color(0f, 1f, 0f, 1f);
                rows[5 * 2] = new Color(0f, 0f, 1f, 1f);
                rows[6 * 2] = new Color(0f, 0f, 1f, 1f);
                rows[7 * 2] = new Color(0f, 0f, 0f, 2f);
                // Column 1 is a plain active segment holding authored blue 0.5 over [0,1].
                rows[1] = new Color(0f, 0f, 0.5f, 1f);
                rows[3] = new Color(0f, 0f, 0.5f, 1f);
                rows[4 * 2 + 1] = new Color(0f, 1f, 0f, 1f);
                rows[5 * 2 + 1] = new Color(0f, 0f, 1f, 1f);
                rows[6 * 2 + 1] = new Color(0f, 0f, 1f, 1f);
                rows[7 * 2 + 1] = new Color(0f, 0f, 0f, 2f);
                timelineTexture.SetPixels(rows);
                timelineTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", timelineTexture);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 2f);
                material.SetVector("_ColorA", Color.black);
                material.SetVector("_ColorB", Color.black);

                var below = RenderGradientPixel(material, 0.5f, 0.25f);
                var above = RenderGradientPixel(material, 0.5f, 0.75f);
                var column = RenderGradientColumn(material, 0.5f, 101);

                // Match the linear-light area average of the evaluated pixels.
                var expectedBoundary = ((below + above) * 0.5f).gamma;
                Assert.That(column[49].gamma.r, Is.EqualTo(below.gamma.r).Within(0.02f));
                Assert.That(column[51].gamma.r, Is.EqualTo(above.gamma.r).Within(0.02f));
                Assert.That(column[50].gamma.b, Is.EqualTo(expectedBoundary.b).Within(0.02f),
                    "The boundary pixel must average the strips' evaluated results, not their packed rows");
                Assert.That(column[50].gamma.g, Is.EqualTo(expectedBoundary.g).Within(0.02f));
                Assert.That(column[50].gamma.r, Is.EqualTo(expectedBoundary.r).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(timelineTexture);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void StripBoundaryAntiAliasingBlendsLitToInactive()
        {
            var shader = Shader.Find("ChroMapper/Object/Basic Gradient");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader)
            {
                enableInstancing = false
            };
            var timelineTexture = new Texture2D(
                2,
                9,
                TextureFormat.RGBAFloat,
                false,
                true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var rows = new Color[18];
                // Column 0 is a plain active segment holding authored blue 0.5 over [0,1].
                rows[0] = new Color(0f, 0f, 0.5f, 1f);
                rows[2] = new Color(0f, 0f, 0.5f, 1f);
                rows[4 * 2] = new Color(0f, 1f, 0f, 1f);
                rows[5 * 2] = new Color(0f, 0f, 1f, 1f);
                rows[6 * 2] = new Color(0f, 0f, 1f, 1f);
                rows[7 * 2] = new Color(0f, 0f, 0f, 2f);
                // Column 1 mirrors the production inactive-light payload: only the times row carries the
                // invalid (0,-1,0,-1) window that makes EvaluateLightTimeline return zero.
                rows[4 * 2 + 1] = new Color(0f, -1f, 0f, -1f);
                timelineTexture.SetPixels(rows);
                timelineTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", timelineTexture);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 2f);
                material.SetVector("_ColorA", Color.black);
                material.SetVector("_ColorB", Color.black);

                var lit = RenderGradientPixel(material, 0.5f, 0.25f);
                // 201 rows puts row 100's centre exactly on the strip boundary.
                var column = RenderGradientColumn(material, 0.5f, 201);

                Assert.That(column[50].b, Is.EqualTo(lit.b).Within(0.02f));
                Assert.That(column[150].b, Is.EqualTo(0f).Within(0.02f),
                    "The inactive strip's interior must stay transparent");
                Assert.That(
                    column[100].b,
                    Is.EqualTo(lit.b * 0.5f).Within(0.02f),
                    "The lit-to-inactive boundary must resolve to half the lit contribution");
            }
            finally
            {
                Object.DestroyImmediate(timelineTexture);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void StripBoundaryAntiAliasingBlendsInactiveToLit()
        {
            var shader = Shader.Find("ChroMapper/Object/Basic Gradient");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader)
            {
                enableInstancing = false
            };
            var timelineTexture = new Texture2D(
                2,
                9,
                TextureFormat.RGBAFloat,
                false,
                true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var rows = new Color[18];
                // Column 0 mirrors the production inactive-light payload.
                rows[4 * 2] = new Color(0f, -1f, 0f, -1f);
                // Column 1 is a plain active segment holding authored blue 0.5 over [0,1].
                rows[1] = new Color(0f, 0f, 0.5f, 1f);
                rows[3] = new Color(0f, 0f, 0.5f, 1f);
                rows[4 * 2 + 1] = new Color(0f, 1f, 0f, 1f);
                rows[5 * 2 + 1] = new Color(0f, 0f, 1f, 1f);
                rows[6 * 2 + 1] = new Color(0f, 0f, 1f, 1f);
                rows[7 * 2 + 1] = new Color(0f, 0f, 0f, 2f);
                timelineTexture.SetPixels(rows);
                timelineTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", timelineTexture);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 2f);
                material.SetVector("_ColorA", Color.black);
                material.SetVector("_ColorB", Color.black);

                var lit = RenderGradientPixel(material, 0.5f, 0.75f);
                var column = RenderGradientColumn(material, 0.5f, 201);

                Assert.That(column[50].b, Is.EqualTo(0f).Within(0.02f));
                Assert.That(column[150].b, Is.EqualTo(lit.b).Within(0.02f));
                Assert.That(
                    column[100].b,
                    Is.EqualTo(lit.b * 0.5f).Within(0.02f),
                    "The inactive-to-lit boundary must resolve to half the lit contribution");
            }
            finally
            {
                Object.DestroyImmediate(timelineTexture);
                Object.DestroyImmediate(material);
            }
        }

        // At an oblique angle, a pixel can touch only the previous strip. Do not borrow the next strip's
        // color instead.
        [Test]
        public void NarrowProjectedStripBlendsWithTheTouchedNeighbor()
        {
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"))
            {
                enableInstancing = false
            };
            var texture = new Texture2D(3, 9, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            try
            {
                var pixels = new Color[27];
                var colors = new[] { Color.red, Color.blue, Color.green };
                for (var light = 0; light < 3; light++)
                {
                    pixels[light] = new Color(colors[light].r, colors[light].g,
                        colors[light].b, 1f);
                    pixels[3 + light] = pixels[light];
                    pixels[(4 * 3) + light] = new Color(0f, 1f, 0f, 1f);
                    pixels[(5 * 3) + light] = new Color(0f, 0f, 1f, 1f);
                    pixels[(6 * 3) + light] = new Color(0f, 0f, 1f, 1f);
                    pixels[(7 * 3) + light] = new Color(0f, 0f, 0f, 2f);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", texture);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 3f);
                material.SetVector("_ColorA", Color.black);
                material.SetVector("_ColorB", Color.black);

                var column = RenderGradientColumn(material, 0.5f, 3, 0.15f, 0.95f);
                Assert.That(column[1].r, Is.GreaterThan(column[1].g + 0.01f),
                    $"The center blue pixel touches red on its left, not green on its right: {column[1]}.");

                // With both neighbors off, the blue strip's center covers a
                // whole screen pixel and must not be darkened by color AA.
                pixels[4 * 3] = new Color(0f, -1f, 0f, -1f);
                pixels[(4 * 3) + 2] = new Color(0f, -1f, 0f, -1f);
                texture.SetPixels(pixels);
                texture.Apply(false, false);
                var baseline = RenderGradientPixel(material, 0.5f, 0.5f);
                var againstBackground = RenderGradientColumn(material, 0.5f, 3);
                Assert.That(againstBackground[1].b, Is.EqualTo(baseline.b).Within(0.02f),
                    "Color smoothing darkened a full blue pixel beside black strips.");
                // The beat-6 separated strips can project narrower than one
                // pixel. Both black neighbors then occupy part of the same
                // pixel; coverage must include both sides of the lit strip.
                var subpixelStrip = RenderGradientColumn(material, 0.5f, 3,
                    bottom: 0.2f, top: 0.8f);
                Assert.That(subpixelStrip[1].b,
                    Is.InRange(baseline.b * 0.5f, baseline.b * 0.72f),
                    "A subpixel lit strip must account for black on both sides of the pixel.");
                // A lit strip on the other touched side still owns part of
                // this pixel; do not apply the two-dark-neighbor reduction.
                pixels[(4 * 3) + 2] = new Color(0f, 1f, 0f, 1f);
                texture.SetPixels(pixels);
                texture.Apply(false, false);
                var otherSideLit = RenderGradientColumn(material, 0.5f, 3,
                    bottom: 0.2f, top: 0.8f);
                Assert.That(otherSideLit[1].b,
                    Is.GreaterThan(baseline.b * 0.75f),
                    "An emitting opposite neighbor must not be treated as background.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void DistributedStripBoundaryAntiAliasingBlendsLitToOff()
        {
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"))
            {
                enableInstancing = false
            };
            var endpointTexture = new Texture2D(2, 4, TextureFormat.RGBAHalf, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var colors = new Color[8];
                colors[0] = Color.blue;
                colors[2] = Color.blue;
                endpointTexture.SetPixels(colors);
                endpointTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", endpointTexture);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 2f);
                material.SetVector("_ColorA", Color.black);
                material.SetVector("_ColorB", Color.black);

                var column = RenderGradientColumn(material, 0.5f, 201);
                Assert.That(column[50].b, Is.GreaterThan(0.05f));
                Assert.That(column[150].b, Is.EqualTo(0f).Within(0.01f));
                Assert.That(column[100].b, Is.EqualTo(column[50].b * 0.5f).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(endpointTexture);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void RibbonOuterEdgeAntiAliasingScalesPartialPixels()
        {
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"))
            {
                enableInstancing = false
            };

            try
            {
                material.SetVector("_ColorA", Color.blue);
                material.SetVector("_ColorB", Color.blue);
                var column = RenderGradientColumn(material, 0.5f, 101, 0.25f, 0.75f);
                var interior = column[50].b;

                Assert.That(column[24].b, Is.EqualTo(0f).Within(0.01f));
                Assert.That(column[76].b, Is.EqualTo(0f).Within(0.01f));
                Assert.That(column[26].b, Is.EqualTo(interior).Within(0.02f));
                Assert.That(column[74].b, Is.EqualTo(interior).Within(0.02f));
                Assert.That(column[25].b, Is.EqualTo(interior * 0.75f).Within(0.02f));
                Assert.That(column[75].b, Is.EqualTo(interior * 0.75f).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void RibbonFrontAndBackEdgeAntiAliasingScalesPartialPixels()
        {
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"))
            {
                enableInstancing = false
            };

            try
            {
                material.SetVector("_ColorA", Color.blue);
                material.SetVector("_ColorB", Color.blue);
                var row = RenderGradientRow(material, 0.5f, 101, 0.25f, 0.75f);
                var interior = row[50].b;

                Assert.That(row[24].b, Is.EqualTo(0f).Within(0.01f));
                Assert.That(row[76].b, Is.EqualTo(0f).Within(0.01f));
                Assert.That(row[26].b, Is.EqualTo(interior).Within(0.02f));
                Assert.That(row[74].b, Is.EqualTo(interior).Within(0.02f));
                Assert.That(row[25].b, Is.EqualTo(interior * 0.75f).Within(0.02f));
                Assert.That(row[75].b, Is.EqualTo(interior * 0.75f).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TimelineEdgeAntiAliasingBlendsWithBackground(bool startsAtCenter)
        {
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"))
            {
                enableInstancing = false
            };
            var timelineTexture = new Texture2D(1, 9, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var rows = new Color[9];
                rows[0] = Color.blue;
                rows[1] = Color.blue;
                rows[4] = startsAtCenter
                    ? new Color(0.5f, 1f, 0.5f, 1f)
                    : new Color(0f, 0.5f, 0f, 0.5f);
                rows[5] = new Color(0f, 0f, 1f, 1f);
                rows[6] = new Color(0f, 0f, 1f, 1f);
                rows[7] = new Color(0f, 0f, 0f, 2f);
                timelineTexture.SetPixels(rows);
                timelineTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", timelineTexture);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);

                var row = RenderGradientRow(material, 0.5f, 101);
                var lit = row[startsAtCenter ? 51 : 49].b;
                Assert.That(row[startsAtCenter ? 49 : 51].b, Is.EqualTo(0f).Within(0.01f));
                Assert.That(lit, Is.GreaterThan(0.05f));
                Assert.That(row[50].b, Is.EqualTo(lit * 0.5f).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(timelineTexture);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void DistributedNodeRibbonJoinDoesNotExposeBackground()
        {
            var firstGroup = CreateGroup(1f, 0, 1, filterChunks: 2, brightnessDistribution: 0f);
            var secondGroup = CreateGroup(2f, 0, 1, filterChunks: 2, brightnessDistribution: 0f);
            var thirdGroup = CreateGroup(3f, 0, 1, filterChunks: 2, brightnessDistribution: 0f);
            firstGroup["e"][0]["w"] = 0.25f;
            secondGroup["e"][0]["w"] = 0.25f;
            thirdGroup["e"][0]["w"] = 0.25f;
            var map = LoadMap(CreateDifficultyJson(firstGroup, secondGroup, thirdGroup));
            var first = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var second = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            var third = map.LightColorEventBoxGroups[2].Boxes[0].Events[0];
            foreach (var node in new[] { first, second, third })
            {
                node.CustomColor = Color.blue;
                node.Brightness = 1f;
            }

            var timeline = GLSEventCommon.GetColorTimeline(first, 2);
            Assert.That(timeline.TryGetOutgoing(first, 1, out var firstState), Is.True);
            Assert.That(timeline.TryGetOutgoing(second, 1, out var secondState), Is.True);
            Assert.That(firstState.EndTime, Is.EqualTo(secondState.StartTime).Within(0.001f));
            Assert.That(secondState.StartTime, Is.GreaterThan(second.SongBpmTime + 0.05f));

            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var firstObject = new GameObject("First distributed ribbon");
            var secondObject = new GameObject("Second distributed ribbon");
            Material firstMaterial = null;
            Material secondMaterial = null;
            try
            {
                var firstRibbon = CreateRibbonController(firstObject, out var firstRenderer);
                var secondRibbon = CreateRibbonController(secondObject, out var secondRenderer);
                GLSEventCommon.UpdateColorTransitionRibbon(firstRibbon, first, appearance, _ => false, 2);
                GLSEventCommon.UpdateColorTransitionRibbon(secondRibbon, second, appearance, _ => false, 2);
                Assert.That(firstObject.activeSelf, Is.True);
                Assert.That(secondObject.activeSelf, Is.True);
                var firstProperties = new MaterialPropertyBlock();
                var secondProperties = new MaterialPropertyBlock();
                firstRenderer.GetPropertyBlock(firstProperties);
                secondRenderer.GetPropertyBlock(secondProperties);
                firstMaterial = CreateWaveSampleMaterial(firstProperties);
                secondMaterial = CreateWaveSampleMaterial(secondProperties);

                var join = secondState.StartTime;
                var row = RenderJoinedTimelineRow(
                    firstMaterial, firstRibbon, secondMaterial, secondRibbon,
                    join - 0.05f, join + 0.05f, 0.25f, 101);
                Assert.That(row[49].b, Is.GreaterThan(0.05f));
                Assert.That(row[51].b, Is.GreaterThan(0.05f));
                Assert.That(row[50].b,
                    Is.GreaterThan(Mathf.Min(row[49].b, row[51].b) - 0.02f),
                    $"The shared lit join must not reveal a dark horizontal line between owner ribbons. Pixels {row[49].b}, {row[50].b}, {row[51].b}; spans {firstRibbon.ColorTimelineStart}..{firstRibbon.ColorTimelineStart + firstRibbon.ColorTimelineDuration}, {secondRibbon.ColorTimelineStart}..{secondRibbon.ColorTimelineStart + secondRibbon.ColorTimelineDuration}, join {join}.");
                Assert.That(row[50].b,
                    Is.LessThan(Mathf.Max(row[49].b, row[51].b) + 0.02f),
                    "The shared lit join must not form an overbright line either.");
            }
            finally
            {
                Object.DestroyImmediate(firstMaterial);
                Object.DestroyImmediate(secondMaterial);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // A time-distributed join at brightness 2.1 exposes overlap that a dim ribbon could hide.
        [TestCase(-0.25f)]
        [TestCase(0f)]
        [TestCase(0.25f)]
        public void BrightDistributedNodeRibbonJoinMatchesAdjacentPixels(float subpixelOffset)
        {
            var firstGroup = CreateReportedBrightJoinGroup(202f);
            var secondGroup = CreateReportedBrightJoinGroup(202.75f);
            var map = LoadMap(CreateDifficultyJson(firstGroup, secondGroup));
            var first = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var second = map.LightColorEventBoxGroups[0].Boxes[0].Events[1];
            var timeline = GLSEventCommon.GetColorTimeline(first, 4);
            Assert.That(timeline.TryGetOutgoing(first, 1, out var firstState), Is.True);
            Assert.That(timeline.TryGetOutgoing(second, 1, out var secondState), Is.True);
            Assert.That(firstState.EndTime, Is.EqualTo(secondState.StartTime).Within(0.001f));

            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            appearance.BlueColor = Color.blue;
            var firstObject = new GameObject("Reported first node ribbon");
            var secondObject = new GameObject("Reported second node ribbon");
            Material firstMaterial = null;
            Material secondMaterial = null;
            try
            {
                var firstRibbon = CreateRibbonController(firstObject, out var firstRenderer);
                var secondRibbon = CreateRibbonController(secondObject, out var secondRenderer);
                GLSEventCommon.UpdateColorTransitionRibbon(firstRibbon, first, appearance, _ => false, 4);
                GLSEventCommon.UpdateColorTransitionRibbon(secondRibbon, second, appearance, _ => false, 4);
                var firstProperties = new MaterialPropertyBlock();
                var secondProperties = new MaterialPropertyBlock();
                firstRenderer.GetPropertyBlock(firstProperties);
                secondRenderer.GetPropertyBlock(secondProperties);
                firstMaterial = CreateWaveSampleMaterial(firstProperties);
                secondMaterial = CreateWaveSampleMaterial(secondProperties);

                var join = secondState.StartTime;
                // Move the event join across the pixel grid to cover its camera-dependent seam.
                var viewOffset = subpixelOffset * (0.1f / 101f);
                var row = RenderJoinedTimelineRow(
                    firstMaterial, firstRibbon, secondMaterial, secondRibbon,
                    join - 0.05f + viewOffset, join + 0.05f + viewOffset, 0.625f, 101);
                Assert.That(row[49].b, Is.GreaterThan(0.05f));
                Assert.That(row[51].b, Is.GreaterThan(0.05f));
                Assert.That(row[50].b,
                    Is.EqualTo((row[49].b + row[51].b) * 0.5f).Within(0.02f),
                    $"Shared lit endpoints should match the adjacent ribbon, not form a bright seam: {row[49].b}, {row[50].b}, {row[51].b}.");
            }
            finally
            {
                Object.DestroyImmediate(firstMaterial);
                Object.DestroyImmediate(secondMaterial);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(appearance);
            }
        }

        [Test]
        public void AdjacentOwnedLightStripsDoNotExposeBackground()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 31, 1, filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(1.01f, 31, 1, filterParam: 1,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(3f, 31, 1, filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(3.01f, 31, 1, filterParam: 1,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0)));
            var first = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var second = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            foreach (var group in map.LightColorEventBoxGroups)
            {
                group.Boxes[0].Events[0].CustomColor = Color.green;
                group.Boxes[0].Events[0].Brightness = 2.1f;
            }
            var timeline = GLSEventCommon.GetColorTimeline(first, 2);
            Assert.That(timeline.TryGetOutgoing(first, 0, out _), Is.True);
            Assert.That(timeline.TryGetOutgoing(first, 1, out _), Is.False);
            Assert.That(timeline.TryGetOutgoing(second, 1, out _), Is.True);
            Assert.That(timeline.TryGetOutgoing(second, 0, out _), Is.False);

            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var firstObject = new GameObject("First neighboring light ribbon");
            var secondObject = new GameObject("Second neighboring light ribbon");
            Material firstMaterial = null;
            Material secondMaterial = null;
            try
            {
                var firstRibbon = CreateRibbonController(firstObject, out var firstRenderer);
                var secondRibbon = CreateRibbonController(secondObject, out var secondRenderer);
                // These groups share an outer track lane; inner boxes render in
                // separate lanes and correctly fade their own exposed edges.
                GLSEventCommon.UpdateColorTransitionRibbon(firstRibbon, first, appearance, _ => false, 2,
                    aggregateSameTimeBoxes: true);
                GLSEventCommon.UpdateColorTransitionRibbon(secondRibbon, second, appearance, _ => false, 2,
                    aggregateSameTimeBoxes: true);
                var firstProperties = new MaterialPropertyBlock();
                var secondProperties = new MaterialPropertyBlock();
                firstRenderer.GetPropertyBlock(firstProperties);
                secondRenderer.GetPropertyBlock(secondProperties);
                firstMaterial = CreateWaveSampleMaterial(firstProperties);
                secondMaterial = CreateWaveSampleMaterial(secondProperties);
                var firstProgress = (2f - firstRibbon.ColorTimelineStart) / firstRibbon.ColorTimelineDuration;
                var secondProgress = (2f - secondRibbon.ColorTimelineStart) / secondRibbon.ColorTimelineDuration;
                var column = RenderGradientColumn(
                    firstMaterial, firstProgress, 101,
                    secondMaterial: secondMaterial, secondProgress: secondProgress);
                Assert.That(column[49].g, Is.GreaterThan(0.01f));
                Assert.That(column[51].g, Is.GreaterThan(0.01f));
                var adjacentGreen = (column[49].g + column[51].g) * 0.5f;
                Assert.That(column[50].g,
                    Is.EqualTo(adjacentGreen).Within(adjacentGreen * 0.1f),
                    $"Neighboring lit strips should meet without a dark vertical seam: {column[49].g}, {column[50].g}, {column[51].g}.");
                // Before the neighboring light's first event, this edge really borders darkness and must keep
                // its anti-aliasing.
                var beforeProgress = (1.005f - firstRibbon.ColorTimelineStart)
                    / firstRibbon.ColorTimelineDuration;
                var beforeColumn = RenderGradientColumn(firstMaterial, beforeProgress, 101);
                var beforeInterior = Mathf.Max(beforeColumn[49].g, beforeColumn[51].g);
                Assert.That(beforeInterior, Is.GreaterThan(0.05f));
                Assert.That(beforeColumn[50].g,
                    Is.EqualTo(beforeInterior * 0.5f).Within(beforeInterior * 0.1f));
            }
            finally
            {
                Object.DestroyImmediate(firstMaterial);
                Object.DestroyImmediate(secondMaterial);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(appearance);
            }
        }

        [Test]
        public void RelitAdjacentOwnedLightStripsDoNotExposeBackground()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 31, 1, filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(1.01f, 31, 1, filterParam: 1,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(1.5f, 31, 1, filterParam: 1,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(2f, 31, 1, filterParam: 1,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(3f, 31, 1, filterParam: 1,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(5f, 31, 1, filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0),
                CreateGroup(5.01f, 31, 1, filterParam: 1,
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 0, filterChunks: 0)));
            foreach (var group in map.LightColorEventBoxGroups)
            {
                group.Boxes[0].Events[0].CustomColor = Color.green;
                group.Boxes[0].Events[0].Brightness = 2.1f;
            }
            map.LightColorEventBoxGroups[3].Boxes[0].Events[0].Brightness = 0f;
            var first = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var relit = map.LightColorEventBoxGroups[4].Boxes[0].Events[0];
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var firstObject = new GameObject("Long neighboring light ribbon");
            var relitObject = new GameObject("Relit neighboring light ribbon");
            Material firstMaterial = null;
            Material relitMaterial = null;
            try
            {
                var firstRibbon = CreateRibbonController(firstObject, out var firstRenderer);
                var relitRibbon = CreateRibbonController(relitObject, out var relitRenderer);
                // These groups share an outer track lane; inner boxes render in
                // separate lanes and correctly fade their own exposed edges.
                GLSEventCommon.UpdateColorTransitionRibbon(firstRibbon, first, appearance, _ => false, 2,
                    aggregateSameTimeBoxes: true);
                GLSEventCommon.UpdateColorTransitionRibbon(relitRibbon, relit, appearance, _ => false, 2,
                    aggregateSameTimeBoxes: true);
                var firstProperties = new MaterialPropertyBlock();
                var relitProperties = new MaterialPropertyBlock();
                firstRenderer.GetPropertyBlock(firstProperties);
                relitRenderer.GetPropertyBlock(relitProperties);
                firstMaterial = CreateWaveSampleMaterial(firstProperties);
                relitMaterial = CreateWaveSampleMaterial(relitProperties);
                var firstProgress = (4f - firstRibbon.ColorTimelineStart) / firstRibbon.ColorTimelineDuration;
                var relitProgress = (4f - relitRibbon.ColorTimelineStart) / relitRibbon.ColorTimelineDuration;
                var column = RenderGradientColumn(
                    firstMaterial, firstProgress, 101, 0.002f, 1.002f,
                    secondMaterial: relitMaterial, secondProgress: relitProgress);
                var adjacentGreen = (column[49].g + column[51].g) * 0.5f;
                Assert.That(adjacentGreen, Is.GreaterThan(0.01f));
                Assert.That(column[50].g,
                    Is.EqualTo(adjacentGreen).Within(adjacentGreen * 0.02f),
                    $"A relit neighboring strip should meet without a visible seam: {column[49].g}, {column[50].g}, {column[51].g}.");
            }
            finally
            {
                Object.DestroyImmediate(firstMaterial);
                Object.DestroyImmediate(relitMaterial);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(relitObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // Collider's four-chunk green group must not acquire dark lines between lit strips.
        [Test]
        public void ReportedChunkedGreenRibbonHasNoDarkVerticalBoundary()
        {
            var group = CreateGroup(204.5f, 10, 0, filterParam: 1, filterChunks: 4,
                brightnessDistribution: 0f);
            var box = group["e"][0];
            box["f"]["r"] = 0;
            box["w"] = 1f;
            box["d"] = 1f;
            box["b"] = 0f;
            box["e"][0]["c"] = 0;
            box["e"][0]["s"] = 0.5f;
            box["e"][0]["i"] = 0;
            var firstColor = new JSONArray();
            firstColor.Add(0f);
            firstColor.Add(1f);
            firstColor.Add(0.197f);
            box["e"][0]["customData"] = new JSONObject { ["color"] = firstColor };
            var secondColor = new JSONArray();
            secondColor.Add(0f);
            secondColor.Add(1f);
            secondColor.Add(0.254f);
            ((JSONArray)box["e"]).Add(new JSONObject
            {
                ["b"] = 0.5f,
                ["c"] = 0,
                ["s"] = 0.7f,
                ["i"] = 1,
                ["f"] = 0,
                ["sb"] = 0,
                ["sf"] = 0,
                ["customData"] = new JSONObject { ["color"] = secondColor, ["colorEasing"] = 20 }
            });
            var map = LoadMap(CreateDifficultyJson(group));
            var first = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var second = map.LightColorEventBoxGroups[0].Boxes[0].Events[1];
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var firstObject = new GameObject("Reported first green node ribbon");
            var secondObject = new GameObject("Reported second green node ribbon");
            Material firstMaterial = null;
            Material secondMaterial = null;
            try
            {
                var firstRibbon = CreateRibbonController(firstObject, out var firstRenderer);
                var secondRibbon = CreateRibbonController(secondObject, out var secondRenderer);
                GLSEventCommon.UpdateColorTransitionRibbon(firstRibbon, first, appearance, _ => false, 4);
                GLSEventCommon.UpdateColorTransitionRibbon(secondRibbon, second, appearance, _ => false, 4);
                Assert.That(firstRibbon.ColorTimelineDuration, Is.GreaterThan(0f));
                Assert.That(secondRibbon.ColorTimelineDuration, Is.GreaterThan(0f));
                var firstProperties = new MaterialPropertyBlock();
                var secondProperties = new MaterialPropertyBlock();
                firstRenderer.GetPropertyBlock(firstProperties);
                secondRenderer.GetPropertyBlock(secondProperties);
                firstMaterial = CreateWaveSampleMaterial(firstProperties);
                secondMaterial = CreateWaveSampleMaterial(secondProperties);
                var time = 205.4f;
                var firstProgress = (time - firstRibbon.ColorTimelineStart) / firstRibbon.ColorTimelineDuration;
                var secondProgress = (time - secondRibbon.ColorTimelineStart) / secondRibbon.ColorTimelineDuration;
                // Check both near and distant projected strip widths.
                foreach (var height in new[] { 101, 401 })
                {
                    var column = RenderGradientColumn(firstMaterial, firstProgress, height,
                        secondMaterial: secondMaterial, secondProgress: secondProgress);
                    foreach (var boundary in new[] { height / 4, height / 2, (height * 3) / 4 })
                    {
                        // Every channel at the join stays within the adjacent strip colors;
                        // a black or bright blend artifact violates this even when hues match.
                        for (var channel = 0; channel < 3; channel++)
                        {
                            var left = column[boundary - 5][channel];
                            var right = column[boundary + 5][channel];
                            Assert.That(column[boundary][channel],
                                Is.InRange(Mathf.Min(left, right) - 0.005f,
                                    Mathf.Max(left, right) + 0.005f),
                                $"Height {height}, boundary {boundary}, channel {channel}: {left}, {column[boundary][channel]}, {right}.");
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(firstMaterial);
                Object.DestroyImmediate(secondMaterial);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // Collider's beat-409 boxes use separate inner lanes. Their blue and red ribbons must not blend
        // across lanes.
        [Test]
        public void InnerAlternatingPulseRibbonNeverBorrowsSiblingColor()
        {
            var map = LoadMap(CreateDifficultyJson(CreateAlternatingPulseGroup(409f)));
            var boxes = map.LightColorEventBoxGroups[0].Boxes;
            foreach (var node in boxes[0].Events)
                node.CustomColor = Color.blue;
            foreach (var node in boxes[1].Events)
                node.CustomColor = Color.red;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var blueObject = new GameObject("Beat 409 blue inner ribbon");
            var redObject = new GameObject("Beat 409 red inner ribbon");
            Material blueMaterial = null;
            Material redMaterial = null;
            try
            {
                var blueRibbon = CreateRibbonController(blueObject, out var blueRenderer);
                var redRibbon = CreateRibbonController(redObject, out var redRenderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    blueRibbon, boxes[0].Events[3], appearance, _ => false, 5);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    redRibbon, boxes[1].Events[3], appearance, _ => false, 5);
                var blueProperties = new MaterialPropertyBlock();
                var redProperties = new MaterialPropertyBlock();
                blueRenderer.GetPropertyBlock(blueProperties);
                redRenderer.GetPropertyBlock(redProperties);
                blueMaterial = CreateWaveSampleMaterial(blueProperties);
                redMaterial = CreateWaveSampleMaterial(redProperties);
                var blueProgress = (410.375f - blueRibbon.ColorTimelineStart)
                    / blueRibbon.ColorTimelineDuration;
                var redProgress = (410.375f - redRibbon.ColorTimelineStart)
                    / redRibbon.ColorTimelineDuration;
                var blueColumn = RenderGradientColumn(blueMaterial, blueProgress, 401);
                var redColumn = RenderGradientColumn(redMaterial, redProgress, 401);
                Assert.That(blueColumn.Max(pixel => pixel.b), Is.GreaterThan(0.1f));
                Assert.That(redColumn.Max(pixel => pixel.r), Is.GreaterThan(0.1f));
                Assert.That(blueColumn.Max(pixel => pixel.r), Is.LessThan(0.08f),
                    "The blue inner lane borrowed the other box's red color.");
                Assert.That(redColumn.Max(pixel => pixel.b), Is.LessThan(0.08f),
                    "The red inner lane borrowed the other box's blue color.");
                // The reported hairlines appear in the foreshortened inner
                // lane view, where one screen pixel can touch multiple IDs.
                var blueProjected = RenderRibbonPlane(new[] { blueMaterial },
                    new[] { blueRibbon }, 410.25f, 410.5f, 32, 401,
                    perspective: true);
                var redProjected = RenderRibbonPlane(new[] { redMaterial },
                    new[] { redRibbon }, 410.25f, 410.5f, 32, 401,
                    perspective: true);
                Assert.That(blueProjected.Max(pixel => pixel.r), Is.LessThan(0.08f),
                    "The projected blue inner lane borrowed red at a strip edge.");
                Assert.That(redProjected.Max(pixel => pixel.b), Is.LessThan(0.08f),
                    "The projected red inner lane borrowed blue at a strip edge.");
            }
            finally
            {
                Object.DestroyImmediate(blueMaterial);
                Object.DestroyImmediate(redMaterial);
                Object.DestroyImmediate(blueObject);
                Object.DestroyImmediate(redObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // The same beat-409 boxes share one ribbon in the outer view; their adjoining lit strips must meet
        // without dark seams.
        [Test]
        public void OuterAlternatingPulseRibbonKeepsLitStripJoins()
        {
            var map = LoadMap(CreateDifficultyJson(CreateAlternatingPulseGroup(409f)));
            var boxes = map.LightColorEventBoxGroups[0].Boxes;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            // The test-created appearance has zeroed red/blue fields; supply
            // visible default palette colors so c=0/c=1 exercise the map.
            appearance.RedColor = new Color(1f, 0f, 0.5f);
            appearance.BlueColor = new Color(0.1f, 0.4f, 1f);
            appearance.OffColor = Color.clear;
            var ribbonObjects = new List<GameObject>();
            var ribbons = new List<LightGradientController>();
            var materials = new List<Material>();
            try
            {
                // The outer view draws the primary node and its ghost nodes,
                // so render their full material stack at the reported time.
                foreach (var node in boxes.SelectMany(box => box.Events)
                    .OrderBy(node => node.SongBpmTime))
                {
                    var ribbonObject = new GameObject("Beat 409 outer group ribbon");
                    ribbonObjects.Add(ribbonObject);
                    var ribbon = CreateRibbonController(ribbonObject, out var renderer);
                    GLSEventCommon.UpdateColorTransitionRibbon(ribbon, node,
                        appearance, _ => false, 5, aggregateSameTimeBoxes: true);
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    materials.Add(CreateWaveSampleMaterial(properties));
                    ribbons.Add(ribbon);
                }
                var plane = RenderRibbonPlane(materials.ToArray(), ribbons.ToArray(),
                    410.25f, 410.5f, 128, 401, perspective: true);
                var compared = 0;
                for (var y = 20; y < 380; y++)
                {
                    var across = (y + 0.5f) / 401f;
                    var leftEdge = Mathf.Lerp(0.18f, 0.35f, across) * 128f;
                    var rightEdge = Mathf.Lerp(0.82f, 0.65f, across) * 128f;
                    for (var strip = 1; strip < 5; strip++)
                    {
                        var boundary = Mathf.RoundToInt(Mathf.Lerp(leftEdge, rightEdge, strip / 5f));
                        var left = plane[(y * 128) + boundary - 2];
                        var middle = plane[(y * 128) + boundary];
                        var right = plane[(y * 128) + boundary + 2];
                        var leftEnergy = left.r + left.g + left.b;
                        var rightEnergy = right.r + right.g + right.b;
                        if (Mathf.Min(leftEnergy, rightEnergy) < 0.05f)
                            continue;
                        compared++;
                        var middleEnergy = middle.r + middle.g + middle.b;
                        Assert.That(middleEnergy, Is.GreaterThanOrEqualTo(
                                Mathf.Min(leftEnergy, rightEnergy) * 0.8f),
                            $"Outer beat {410.25f + across * 0.25f}, join {strip}: "
                            + $"{left} / {middle} / {right}.");
                    }
                    // Search every interior pixel too: the five physical
                    // strip boundaries are slanted by this projection, so a
                    // one-pixel dark line can miss a rounded boundary index.
                    for (var x = Mathf.CeilToInt(leftEdge) + 3;
                        x < Mathf.FloorToInt(rightEdge) - 3; x++)
                    {
                        var left = plane[(y * 128) + x - 2];
                        var middle = plane[(y * 128) + x];
                        var right = plane[(y * 128) + x + 2];
                        var leftEnergy = left.r + left.g + left.b;
                        var rightEnergy = right.r + right.g + right.b;
                        if (Mathf.Min(leftEnergy, rightEnergy) < 0.05f)
                            continue;
                        var middleEnergy = middle.r + middle.g + middle.b;
                        Assert.That(middleEnergy, Is.GreaterThanOrEqualTo(
                                Mathf.Min(leftEnergy, rightEnergy) * 0.8f),
                            $"Outer beat {410.25f + across * 0.25f}, x {x}: "
                            + $"{left} / {middle} / {right}.");
                    }
                }
                Assert.That(compared, Is.GreaterThan(0));
            }
            finally
            {
                foreach (var material in materials)
                    Object.DestroyImmediate(material);
                foreach (var ribbonObject in ribbonObjects)
                    Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // Collider's beat-427 alternating pink lights are both emitting during the fade, so their shared
        // strip edges must stay lit.
        [Test]
        public void FadingAlternatingRibbonsHaveNoDarkVerticalBoundary()
        {
            var group = CreateAlternatingPulseGroup(427f);
            var following = CreateGroup(427.875f, 10, 1, filterParam: 1,
                filterChunks: 0, brightnessDistribution: 0f);
            following["e"][0]["w"] = 0f;
            following["e"][0]["b"] = 0;
            following["e"][0]["e"][0]["s"] = 0f;
            var map = LoadMap(CreateDifficultyJson(group, following));
            var first = map.LightColorEventBoxGroups[0].Boxes[0].Events[1];
            var second = map.LightColorEventBoxGroups[0].Boxes[1].Events[1];
            first.CustomColor = new Color(1f, 0f, 0.8f);
            second.CustomColor = first.CustomColor;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var firstObject = new GameObject("Fading pink first box ribbon");
            Material firstMaterial = null;
            try
            {
                var firstRibbon = CreateRibbonController(firstObject, out var firstRenderer);
                // The outer track draws one representative for both same-time
                // boxes. Stacking two inner-lane materials here invents an
                // overlap that never occurs in this reported outer view.
                GLSEventCommon.UpdateColorTransitionRibbon(firstRibbon, first, appearance, _ => false, 4,
                    aggregateSameTimeBoxes: true);
                var firstProperties = new MaterialPropertyBlock();
                firstRenderer.GetPropertyBlock(firstProperties);
                firstMaterial = CreateWaveSampleMaterial(firstProperties);
                var time = 427.5f;
                var firstProgress = (time - firstRibbon.ColorTimelineStart) / firstRibbon.ColorTimelineDuration;
                var column = RenderGradientColumn(firstMaterial, firstProgress, 401);
                foreach (var boundary in new[] { 100, 200, 300 })
                {
                    var left = column[boundary - 5].r;
                    var right = column[boundary + 5].r;
                    Assert.That(Mathf.Min(left, right), Is.GreaterThan(0.02f));
                    Assert.That(column[boundary].r,
                        Is.GreaterThanOrEqualTo(Mathf.Min(left, right) - 0.005f),
                        $"Fading pink boundary {boundary}: {left}, {column[boundary].r}, {right}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(firstMaterial);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // Collider's beat-6 boxes strobe different colors on alternating lights. Pixels crossing a shared
        // edge must blend the two displayed colors.
        [Test]
        public void AlternatingStrobeRibbonsBlendColorsAtSharedStripEdges()
        {
            var group = CreateGroup(6f, 15, 0, filterParam: 2, filterParam1: 0,
                filterChunks: 4, brightnessDistribution: 0f);
            var otherGroup = CreateGroup(6f, 15, 0, filterParam: 2, filterParam1: 1,
                filterChunks: 4, brightnessDistribution: 0f);
            var firstBox = group["e"][0];
            var secondBox = otherGroup["e"][0];
            foreach (var box in new[] { firstBox, secondBox })
            {
                box["w"] = 1f;
                box["d"] = 1;
                box["b"] = 0;
                box["e"][0]["c"] = 0;
                box["e"][0]["s"] = 0.2f;
                box["e"][0]["i"] = 0;
                box["e"][0]["f"] = 2;
                box["e"][0]["sb"] = 0.3f;
                ((JSONArray)box["e"]).Add(new JSONObject
                {
                    ["b"] = 0.5f, ["c"] = 0, ["s"] = 0.7f, ["i"] = 1,
                    ["f"] = 2, ["sb"] = 0.3f, ["sf"] = 0
                });
            }
            ((JSONArray)group["e"]).Add(secondBox);
            var following = CreateGroup(7.5f, 15, 2, filterParam: 1,
                filterChunks: 0, brightnessDistribution: 0f);
            following["e"][0]["w"] = 0f;
            following["e"][0]["b"] = 0;
            following["e"][0]["e"][0]["c"] = 0;
            following["e"][0]["e"][0]["s"] = 0.5f;
            var map = LoadMap(CreateDifficultyJson(group, following));
            var first = map.LightColorEventBoxGroups[0].Boxes[0].Events[1];
            var second = map.LightColorEventBoxGroups[0].Boxes[1].Events[1];
            var firstStart = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var secondStart = map.LightColorEventBoxGroups[0].Boxes[1].Events[0];
            firstStart.CustomColor = new Color(0f, 1f, 0.197f);
            firstStart.StrobeColor = new Color(1f, 0f, 0.999f);
            secondStart.CustomColor = new Color(0.9f, 1f, 0.197f);
            secondStart.StrobeColor = new Color(0f, 1f, 0.999f);
            first.CustomColor = new Color(0f, 1f, 0.254f);
            first.StrobeColor = new Color(1f, 0f, 0.999f);
            first.ChromaColorEasing = 20;
            second.CustomColor = new Color(0.9f, 1f, 0.254f);
            second.StrobeColor = new Color(0f, 1f, 0.999f);
            second.ChromaColorEasing = 20;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var firstObject = new GameObject("Green strobe box ribbon");
            var secondObject = new GameObject("Yellow strobe box ribbon");
            var firstStartObject = new GameObject("Green initial strobe ribbon");
            var secondStartObject = new GameObject("Yellow initial strobe ribbon");
            var groupObject = new GameObject("Combined outer group ribbon");
            var groupLaterObject = new GameObject("Combined later outer preview ribbon");
            Material firstMaterial = null;
            Material secondMaterial = null;
            Material firstStartMaterial = null;
            Material secondStartMaterial = null;
            Material groupMaterial = null;
            Material groupLaterMaterial = null;
            try
            {
                var firstRibbon = CreateRibbonController(firstObject, out var firstRenderer);
                var secondRibbon = CreateRibbonController(secondObject, out var secondRenderer);
                var firstStartRibbon = CreateRibbonController(firstStartObject, out var firstStartRenderer);
                var secondStartRibbon = CreateRibbonController(secondStartObject, out var secondStartRenderer);
                var groupRibbon = CreateRibbonController(groupObject, out var groupRenderer);
                var groupLaterRibbon = CreateRibbonController(groupLaterObject, out var groupLaterRenderer);
                // The photographed join is in the outer view, where the
                // neighboring event boxes share one aggregated group ribbon.
                // Keep the compact four-strip strobe case beside the real
                // ten-light outer render checked below.
                GLSEventCommon.UpdateColorTransitionRibbon(firstRibbon, first, appearance,
                    _ => false, 4, aggregateSameTimeBoxes: true);
                GLSEventCommon.UpdateColorTransitionRibbon(secondRibbon, second, appearance,
                    _ => false, 4, aggregateSameTimeBoxes: true);
                GLSEventCommon.UpdateColorTransitionRibbon(firstStartRibbon, firstStart,
                    appearance, _ => false, 4, aggregateSameTimeBoxes: true);
                GLSEventCommon.UpdateColorTransitionRibbon(secondStartRibbon, secondStart,
                    appearance, _ => false, 4, aggregateSameTimeBoxes: true);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    groupRibbon, firstStart, appearance, _ => false, 10, aggregateSameTimeBoxes: true);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    groupLaterRibbon, first, appearance, _ => false, 10, aggregateSameTimeBoxes: true);
                var firstProperties = new MaterialPropertyBlock();
                var secondProperties = new MaterialPropertyBlock();
                var firstStartProperties = new MaterialPropertyBlock();
                var secondStartProperties = new MaterialPropertyBlock();
                var groupProperties = new MaterialPropertyBlock();
                var groupLaterProperties = new MaterialPropertyBlock();
                firstRenderer.GetPropertyBlock(firstProperties);
                secondRenderer.GetPropertyBlock(secondProperties);
                firstStartRenderer.GetPropertyBlock(firstStartProperties);
                secondStartRenderer.GetPropertyBlock(secondStartProperties);
                groupRenderer.GetPropertyBlock(groupProperties);
                groupLaterRenderer.GetPropertyBlock(groupLaterProperties);
                firstMaterial = CreateWaveSampleMaterial(firstProperties);
                secondMaterial = CreateWaveSampleMaterial(secondProperties);
                firstStartMaterial = CreateWaveSampleMaterial(firstStartProperties);
                secondStartMaterial = CreateWaveSampleMaterial(secondStartProperties);
                groupMaterial = CreateWaveSampleMaterial(groupProperties);
                groupLaterMaterial = CreateWaveSampleMaterial(groupLaterProperties);
                var time = 7.1f;
                var firstProgress = (time - firstRibbon.ColorTimelineStart) / firstRibbon.ColorTimelineDuration;
                var secondProgress = (time - secondRibbon.ColorTimelineStart) / secondRibbon.ColorTimelineDuration;
                var column = RenderGradientColumn(firstMaterial, firstProgress, 401,
                    secondMaterial: secondMaterial, secondProgress: secondProgress);
                var contrastingEdges = 0;
                foreach (var boundary in new[] { 100, 200, 300 })
                {
                    var left = column[boundary - 5].r;
                    var right = column[boundary + 5].r;
                    if (Mathf.Abs(left - right) < 0.08f
                        || column[boundary - 5].g < 0.02f
                        || column[boundary + 5].g < 0.02f)
                        continue;
                    contrastingEdges++;
                    Assert.That(column[boundary].r,
                        Is.InRange(Mathf.Min(left, right) + 0.02f,
                            Mathf.Max(left, right) - 0.02f),
                        $"Contrasting strip edge {boundary} should blend: {left}, {column[boundary].r}, {right}.");
                }
                Assert.That(contrastingEdges, Is.GreaterThan(0));
                // The reported steps lie in 6.5-7.0, where distributed IDs can
                // still use their first event while neighbors use the second.
                var plane = RenderRibbonPlane(
                    new[] { firstStartMaterial, secondStartMaterial, firstMaterial, secondMaterial },
                    new[] { firstStartRibbon, secondStartRibbon, firstRibbon, secondRibbon },
                    6.5f, 7f, 191, 401);
                // The reported staircase crosses this cyan/pink join near 6.75.
                // Require its pixel to contain both neighboring strobe colors.
                var leftStrobe = plane[(200 - 5) * 191 + 96];
                var joinedStrobe = plane[200 * 191 + 96];
                var rightStrobe = plane[(200 + 5) * 191 + 96];
                Assert.That(joinedStrobe.r,
                    Is.InRange(Mathf.Min(leftStrobe.r, rightStrobe.r) + 0.002f,
                        Mathf.Max(leftStrobe.r, rightStrobe.r) - 0.002f));
                Assert.That(joinedStrobe.g,
                    Is.InRange(Mathf.Min(leftStrobe.g, rightStrobe.g) + 0.002f,
                        Mathf.Max(leftStrobe.g, rightStrobe.g) - 0.002f));
                var staggeredEdges = 0;
                var contrastingStrobeEdges = 0;
                for (var x = 5; x < 186; x++)
                {
                    foreach (var boundary in new[] { 100, 200, 300 })
                    {
                        var left = plane[(boundary - 5) * 191 + x];
                        var middle = plane[boundary * 191 + x];
                        var right = plane[(boundary + 5) * 191 + x];
                        var bright = Mathf.Max(left.g, right.g);
                        var dark = Mathf.Min(left.g, right.g);
                        // At the photographed cyan-to-pink join, both lights
                        // emit; the boundary must contain both color channels.
                        var leftEnergy = left.r + left.g + left.b;
                        var rightEnergy = right.r + right.g + right.b;
                        if (leftEnergy > 0.03f && rightEnergy > 0.03f
                            && Mathf.Max(leftEnergy, rightEnergy) < Mathf.Min(leftEnergy, rightEnergy) * 1.3f
                            && Mathf.Abs(left.r - right.r) > 0.03f)
                        {
                            contrastingStrobeEdges++;
                            Assert.That(middle.r,
                                Is.InRange(Mathf.Min(left.r, right.r) + 0.002f,
                                    Mathf.Max(left.r, right.r) - 0.002f),
                                $"Beat {6.5f + ((x + 0.5f) / 191f * 0.5f)} color edge {boundary}: {left}, {middle}, {right}.");
                        }
                        if (bright < 0.03f || dark > 0.005f)
                            continue;
                        staggeredEdges++;
                        Assert.That(middle.g,
                            Is.InRange(dark + 0.002f, bright - 0.002f),
                            $"Beat {6.5f + ((x + 0.5f) / 191f * 0.5f)} edge {boundary}: {left}, {middle}, {right}.");
                    }
                }
                Assert.That(staggeredEdges, Is.GreaterThan(0));
                Assert.That(contrastingStrobeEdges, Is.GreaterThan(0));
                // The outer GLS group uses an aggregated timeline, unlike the
                // four inner node ribbons above. Check that actual display path.
                var groupPlane = RenderRibbonPlane(new[] { groupMaterial, groupLaterMaterial },
                    new[] { groupRibbon, groupLaterRibbon }, 6.5f, 7f, 191, 401);
                var groupColorEdges = 0;
                for (var x = 5; x < 186; x++)
                {
                    // Collider group 15 has ten physical color lights; the
                    // authored chunk count of four is not its strip count.
                    for (var strip = 1; strip < 10; strip++)
                    {
                        var boundary = Mathf.RoundToInt(strip * 401f / 10f);
                        var left = groupPlane[(boundary - 5) * 191 + x];
                        var middle = groupPlane[boundary * 191 + x];
                        var right = groupPlane[(boundary + 5) * 191 + x];
                        var leftEnergy = left.r + left.g + left.b;
                        var rightEnergy = right.r + right.g + right.b;
                        if (leftEnergy < 0.03f || rightEnergy < 0.03f
                            || Mathf.Max(leftEnergy, rightEnergy) >= Mathf.Min(leftEnergy, rightEnergy) * 1.3f
                            || Mathf.Abs(left.r - right.r) < 0.03f)
                            continue;
                        groupColorEdges++;
                        Assert.That(middle.r,
                            Is.InRange(Mathf.Min(left.r, right.r) + 0.002f,
                                Mathf.Max(left.r, right.r) - 0.002f),
                            $"Outer group beat {6.5f + ((x + 0.5f) / 191f * 0.5f)} edge {boundary}: {left}, {middle}, {right}.");
                    }
                }
                Assert.That(groupColorEdges, Is.GreaterThan(0));
                var projected = RenderRibbonPlane(new[] { groupMaterial, groupLaterMaterial },
                    new[] { groupRibbon, groupLaterRibbon }, 6.5f, 7f, 128, 401,
                    perspective: true);
                // At the photographed projected width, adjoining lit strips
                // still need intermediate colors in their shared screen pixels.
                var projectedBlends = 0;
                for (var y = 20; y < 380; y += 3)
                {
                    var row = y * 128;
                    var fraction = (y + 0.5f) / 401f;
                    for (var x = 28; x < 100; x++)
                    {
                        var left = projected[row + x - 3];
                        var right = projected[row + x + 3];
                        var leftEnergy = left.r + left.g + left.b;
                        var rightEnergy = right.r + right.g + right.b;
                        if (leftEnergy < 0.02f || rightEnergy < 0.02f
                            || Mathf.Max(leftEnergy, rightEnergy) >= Mathf.Min(leftEnergy, rightEnergy) * 1.5f
                            || Mathf.Min(left.r / leftEnergy, right.r / rightEnergy) > 0.005f
                            || Mathf.Max(left.r / leftEnergy, right.r / rightEnergy) < 0.49f)
                            continue;
                        var lower = Mathf.Min(left.r, right.r) + 0.005f;
                        var upper = Mathf.Max(left.r, right.r) - 0.005f;
                        var blended = false;
                        for (var dx = -2; dx <= 2; dx++)
                        {
                            var red = projected[row + x + dx].r;
                            blended |= red > lower && red < upper;
                        }
                        // A boundary exactly between pixel footprints need not
                        // invent an intermediate pixel. The supersampled
                        // Monstercat regressions verify every subpixel phase.
                        if (blended)
                            projectedBlends++;
                    }
                }
                Assert.That(projectedBlends, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(firstMaterial);
                Object.DestroyImmediate(secondMaterial);
                Object.DestroyImmediate(firstStartMaterial);
                Object.DestroyImmediate(secondStartMaterial);
                Object.DestroyImmediate(groupMaterial);
                Object.DestroyImmediate(groupLaterMaterial);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(firstStartObject);
                Object.DestroyImmediate(secondStartObject);
                Object.DestroyImmediate(groupObject);
                Object.DestroyImmediate(groupLaterObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // A pixel spanning a hard-strobe switch or cycle wrap must contain both red and blue phases.
        [TestCase(1f)]
        [TestCase(2f)]
        public void StrobePhaseEdgesAntiAliasAcrossTime(float frequency)
        {
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"))
            {
                enableInstancing = false
            };

            try
            {
                material.SetVector("_ColorA", Color.red);
                material.SetVector("_ColorB", Color.red);
                material.SetVector("_StrobeColorA", Color.blue);
                material.SetVector("_StrobeColorB", Color.blue);
                material.SetFloat("_UseStrobeColors", 1f);
                material.SetFloat("_StrobeDuration", 1f);
                material.SetFloat("_StrobeFrequencyA", frequency);
                material.SetFloat("_StrobeFrequencyB", frequency);

                var row = RenderGradientRow(material, 0.5f, 101);
                Assert.That(row[50].r, Is.GreaterThan(0.01f));
                Assert.That(row[50].b, Is.GreaterThan(0.01f));
                Assert.That(row[50].r, Is.LessThan(Mathf.Max(row[49].r, row[51].r) - 0.01f));
                Assert.That(row[50].b, Is.LessThan(Mathf.Max(row[49].b, row[51].b) - 0.01f));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        // Use each light's own strobe clock when blending a pulse edge inside one pixel.
        [TestCase(1f)]
        [TestCase(2f)]
        public void TimelineStrobePhaseEdgesAntiAliasAcrossTime(float frequency)
        {
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"))
            {
                enableInstancing = false
            };
            var timelineTexture = new Texture2D(1, 9, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var rows = new Color[9];
                rows[0] = Color.red;
                rows[1] = Color.red;
                rows[2] = Color.blue;
                rows[3] = Color.blue;
                rows[4] = new Color(0f, 1f, 0f, 1f);
                rows[5] = new Color(frequency, frequency, 1f, 1f);
                rows[6] = Color.white;
                rows[7] = new Color(1f, 1f, 0f, 2f);
                timelineTexture.SetPixels(rows);
                timelineTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", timelineTexture);
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);

                var row = RenderGradientRow(material, 0.5f, 101);
                Assert.That(row[50].r, Is.GreaterThan(0.01f));
                Assert.That(row[50].b, Is.GreaterThan(0.01f));
                Assert.That(row[50].r, Is.LessThan(Mathf.Max(row[49].r, row[51].r) - 0.01f));
                Assert.That(row[50].b, Is.LessThan(Mathf.Max(row[49].b, row[51].b) - 0.01f));
            }
            finally
            {
                Object.DestroyImmediate(timelineTexture);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void BoundaryQueryReturnsOnlySourcesWhoseTransitionsCrossTheBoundary()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(3f, 1, 0)));
            GLSEventCommon.SetColorTransitionLightCount(0, 1);
            GLSEventCommon.SetColorTransitionLightCount(1, 1);
            // End the unrelated lane unlit so it has no held ribbon crossing the query beat.
            map.LightColorEventBoxGroups[3].Boxes[0].Events[0].Brightness = 0f;
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);

            Assert.That(retainedGroups, Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0] }));
        }

        [Test]
        public void InnerBoundaryQueryReturnsOnlyTheActiveGroupSource()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(6f, 1, 1)));
            GLSEventCommon.SetColorTransitionLightCount(0, 1);
            GLSEventCommon.SetColorTransitionLightCount(1, 1);
            var retainedSources = new List<BaseLightColorBase>();

            GLSEventCommon.GetColorTransitionSourcesAt(
                4f,
                map.LightColorEventBoxGroups[0],
                null,
                retainedSources);

            Assert.That(
                retainedSources,
                Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0].Boxes[0].Events[0] }));
        }

        [Test]
        public void BoundaryQueryIncludesTransitionsEndingAtTheBoundary()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1)));
            GLSEventCommon.SetColorTransitionLightCount(0, 1);
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(5f, null, retainedGroups);

            Assert.That(retainedGroups, Does.Contain(map.LightColorEventBoxGroups[0]));
        }

        [Test]
        public void RemovingTransitionTargetRewiresThePreviousSourceToTheNextTarget()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(8f, 0, 1)));
            GLSEventCommon.SetColorTransitionLightCount(0, 1);
            var firstSource = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var removedTarget = map.LightColorEventBoxGroups[1];

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(firstSource, out var initialEnd), Is.True);
            Assert.That(initialEnd, Is.EqualTo(5f));

            GLSEventCommon.RemoveColorTransitionGroup(removedTarget);

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(firstSource, out var rewiredEnd), Is.True);
            Assert.That(rewiredEnd, Is.EqualTo(8f));

            var retainedSources = new List<BaseLightColorBase>();
            GLSEventCommon.GetColorTransitionSourcesAt(6f, map.LightColorEventBoxGroups[0], null, retainedSources);
            Assert.That(retainedSources, Is.EquivalentTo(new[] { firstSource }));
        }

        [Test]
        public void AddingTransitionTargetRewiresThePreviousSourceToTheCloserTarget()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(8f, 0, 1)));
            GLSEventCommon.SetColorTransitionLightCount(0, 1);
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var insertedTarget = new BaseLightColorEventBoxGroup(CreateGroup(5f, 0, 1));
            insertedTarget.SetMap(map);
            insertedTarget.RecomputeSongBpmTime();

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var initialEnd), Is.True);
            Assert.That(initialEnd, Is.EqualTo(8f));

            GLSEventCommon.AddColorTransitionGroup(insertedTarget);

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var rewiredEnd), Is.True);
            Assert.That(rewiredEnd, Is.EqualTo(5f));

            var retainedSources = new List<BaseLightColorBase>();
            GLSEventCommon.GetColorTransitionSourcesAt(4f, map.LightColorEventBoxGroups[0], null, retainedSources);
            Assert.That(retainedSources, Is.EquivalentTo(new[] { source }));
        }

        // Replacing the only group of an ID must stay an incremental timeline edit; tearing down the
        // emptied cache forces a full rebuild of every light's states on the very next add.
        [Test]
        public void SameIdOnlyGroupReplacementKeepsIncrementalTimeline()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(8f, 0, 1),
                CreateGroup(5f, 1, 0)));
            GLSEventCommon.SetColorTransitionLightCount(0, 4);
            GLSEventCommon.SetColorTransitionLightCount(1, 4);
            var replaced = map.LightColorEventBoxGroups[2];
            var baselineTimeline = GLSEventCommon.GetColorTimeline(
                replaced.Boxes[0].Events[0],
                4);
            Assert.That(baselineTimeline, Is.Not.Null);

            var replacement = new BaseLightColorEventBoxGroup(CreateGroup(6f, 1, 1));
            replacement.SetMap(map);
            replacement.RecomputeSongBpmTime();
            GLSEventCommon.RemoveColorTransitionGroup(replaced);
            GLSEventCommon.AddColorTransitionGroup(replacement);

            var replacementNode = replacement.Boxes[0].Events[0];
            var afterTimeline = GLSEventCommon.GetColorTimeline(replacementNode, 4);
            Assert.AreSame(
                baselineTimeline,
                afterTimeline,
                "A same-ID remove+add replacement must reuse the incremental timeline instead of rebuilding it.");
            Assert.That(
                afterTimeline.TryGetOutgoing(replacementNode, 0, out _),
                Is.True,
                "The replacement node must own outgoing segments on the preserved timeline.");
            Assert.That(
                afterTimeline.TryGetOutgoing(replaced.Boxes[0].Events[0], 0, out _),
                Is.False,
                "The retired node's segments must be gone after the incremental replace.");
        }

        // The collection layer refreshes only containers whose outgoing or incoming ribbon rewired, so
        // the cache must report the changed node identities instead of forcing a full fan-out.
        [Test]
        public void ColorMutationCollectsOnlyRewiredNodes()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(8f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(6f, 1, 0)));
            GLSEventCommon.SetColorTransitionLightCount(0, 4);
            GLSEventCommon.SetColorTransitionLightCount(1, 4);
            var previousSource = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var displacedTarget = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            var unrelatedSource = map.LightColorEventBoxGroups[2].Boxes[0].Events[0];
            var unrelatedTarget = map.LightColorEventBoxGroups[3].Boxes[0].Events[0];
            Assert.That(GLSEventCommon.GetColorTimeline(previousSource, 4), Is.Not.Null);
            Assert.That(GLSEventCommon.GetColorTimeline(unrelatedSource, 4), Is.Not.Null);

            var inserted = new BaseLightColorEventBoxGroup(CreateGroup(4f, 0, 1));
            inserted.SetMap(map);
            inserted.RecomputeSongBpmTime();
            GLSEventCommon.AddColorTransitionGroup(inserted);

            var changedNodes = new HashSet<BaseLightColorBase>();
            var changedAggregates = new Dictionary<BaseEventBoxGroup, HashSet<float>>();
            Assert.That(
                GLSEventCommon.TryCollectChangedColorTransitions(changedNodes, changedAggregates),
                Is.True,
                "An incremental insert must report its changed set instead of signalling a full refresh.");
            Assert.That(changedNodes, Does.Contain(inserted.Boxes[0].Events[0]));
            Assert.That(
                changedNodes,
                Does.Contain(previousSource),
                "The rewired predecessor's outgoing ribbon changed and must be refreshed.");
            Assert.That(
                changedNodes,
                Does.Contain(displacedTarget),
                "The displaced target's incoming ribbon changed and must be refreshed.");
            Assert.That(
                changedNodes.Contains(unrelatedSource),
                Is.False,
                "Unrelated group IDs must not be reported as changed.");
            Assert.That(
                changedNodes.Contains(unrelatedTarget),
                Is.False,
                "Unrelated group IDs must not be reported as changed.");
            Assert.That(changedAggregates[inserted], Does.Contain(0f));
            Assert.That(
                changedAggregates[map.LightColorEventBoxGroups[0]],
                Does.Contain(previousSource.RelativeJsonTime),
                "Same-time aggregates key off the owning group's relative beat.");

            // A second collect without new mutations must report an empty set, not replay the last edit.
            changedNodes.Clear();
            changedAggregates.Clear();
            Assert.That(
                GLSEventCommon.TryCollectChangedColorTransitions(changedNodes, changedAggregates),
                Is.True);
            Assert.That(changedNodes, Is.Empty);
        }

        [Test]
        public void BoundaryQueryKeepsEverySameTimestampSourceFromIndependentFilters()
        {
            var groups = new[]
            {
                CreateGroup(1f, 0, 0, filterParam: 0, filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2, filterChunks: 0, brightnessDistribution: 0f),
                CreateGroup(1f, 0, 0, filterParam: 1, filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2, filterChunks: 0, brightnessDistribution: 0f),
                CreateGroup(5f, 0, 1, filterParam: 0, filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2, filterChunks: 0, brightnessDistribution: 0f),
                CreateGroup(5f, 0, 1, filterParam: 1, filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2, filterChunks: 0, brightnessDistribution: 0f)
            };
            foreach (var group in groups)
                group["e"][0]["w"] = 0f;

            var map = LoadMap(CreateDifficultyJson(groups));
            GLSEventCommon.SetColorTransitionLightCount(0, 4);
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);

            Assert.That(
                retainedGroups,
                Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0], map.LightColorEventBoxGroups[1] }));
        }

        [Test]
        public void BoundaryQueryReturnsOnlySourcesMatchingTheActiveTrack()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0, filterParam: 0, track: "selected",
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 2, filterChunks: 0),
                CreateGroup(5f, 0, 1, filterParam: 0, track: "selected",
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 2, filterChunks: 0),
                CreateGroup(2f, 0, 0, filterParam: 1, track: "other",
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 2, filterChunks: 0),
                CreateGroup(6f, 0, 1, filterParam: 1, track: "other",
                    filterType: (int)IndexFilterType.StepAndOffset, filterParam1: 2, filterChunks: 0)));
            GLSEventCommon.SetColorTransitionLightCount(0, 4);
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, "selected", retainedGroups);

            Assert.That(retainedGroups, Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0] }));
        }

        // Removing every group of one ID must clear its intervals and report its retired nodes.
        [Test]
        public void RemovingKnownIdGroupsClearsTheirIntervals()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(6f, 1, 1)));
            GLSEventCommon.SetColorTransitionLightCount(0, 4);
            GLSEventCommon.SetColorTransitionLightCount(1, 1);
            var removedSource = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var removedTarget = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            var otherSource = map.LightColorEventBoxGroups[2].Boxes[0].Events[0];
            Assert.That(GLSEventCommon.GetColorTimeline(removedSource, 4), Is.Not.Null);

            GLSEventCommon.RemoveColorTransitionGroup(map.LightColorEventBoxGroups[1]);
            GLSEventCommon.RemoveColorTransitionGroup(map.LightColorEventBoxGroups[0]);

            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);
            Assert.That(
                retainedGroups,
                Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[2] }),
                "Removed known-ID groups must drop their retention intervals before the emptied cache commits.");
            var retainedSources = new List<BaseLightColorBase>();
            GLSEventCommon.GetColorTransitionSourcesAt(4f, map.LightColorEventBoxGroups[0], null, retainedSources);
            Assert.That(retainedSources, Is.Empty);
            Assert.That(
                GLSEventCommon.TryGetColorTransitionEndTime(removedSource, out _),
                Is.False,
                "A node whose group left the timeline must lose its transition end.");
            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(otherSource, out var otherEnd), Is.True);
            Assert.That(otherEnd, Is.EqualTo(6f));

            var changedNodes = new HashSet<BaseLightColorBase>();
            var changedAggregates = new Dictionary<BaseEventBoxGroup, HashSet<float>>();
            Assert.That(
                GLSEventCommon.TryCollectChangedColorTransitions(changedNodes, changedAggregates),
                Is.True,
                "An emptied known ID still reports its retired nodes incrementally instead of forcing a full refresh.");
            Assert.That(changedNodes, Does.Contain(removedSource));
            Assert.That(changedNodes, Does.Contain(removedTarget));
        }

        // Changing the environment's light count invalidates every per-light state and requires a full ribbon
        // refresh.
        [Test]
        public void LightCountChangeRebuildsTimelineAndReportsFullRefresh()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var target = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            var fourLightTimeline = GLSEventCommon.GetColorTimeline(source, 4);
            Assert.That(fourLightTimeline, Is.Not.Null);

            var eightLightTimeline = GLSEventCommon.GetColorTimeline(source, 8);
            Assert.AreNotSame(fourLightTimeline, eightLightTimeline);
            Assert.That(eightLightTimeline.LightCount, Is.EqualTo(8));
            Assert.That(eightLightTimeline.TryGetOutgoing(source, 7, out var rebuilt), Is.True);
            Assert.That(rebuilt.Next.Base, Is.SameAs(target));
            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime), Is.True);
            Assert.That(endTime, Is.EqualTo(5f));

            var changedNodes = new HashSet<BaseLightColorBase>();
            var changedAggregates = new Dictionary<BaseEventBoxGroup, HashSet<float>>();
            Assert.That(
                GLSEventCommon.TryCollectChangedColorTransitions(changedNodes, changedAggregates),
                Is.False,
                "A rebuilt timeline cannot scope a changed set, so the refresh must stay a full one.");
        }

        [Test]
        public void LoadingDifferentMapClearsTransitionCacheAndLightCounts()
        {
            LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(6f, 1, 1)));
            GLSEventCommon.SetColorTransitionLightCount(0, 1);
            GLSEventCommon.SetColorTransitionLightCount(1, 1);
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);
            Assert.That(retainedGroups, Is.Not.Empty, "The first map must populate its ribbon intervals.");

            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(10f, 0, 0),
                CreateGroup(20f, 0, 1),
                CreateGroup(12f, 1, 0),
                CreateGroup(18f, 1, 1)));

            retainedGroups.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);
            Assert.That(retainedGroups, Is.Empty, "The previous map's intervals must not survive a reload.");
            Assert.That(
                GLSEventCommon.TryGetColorTransitionEndTime(map.LightColorEventBoxGroups[0].Boxes[0].Events[0], out _),
                Is.False,
                "The new map must not inherit registered counts from the previous map.");

            GLSEventCommon.SetColorTransitionLightCount(0, 1);
            GLSEventCommon.SetColorTransitionLightCount(1, 1);
            GLSEventCommon.GetColorTransitionSourceGroupsAt(15f, null, retainedGroups);
            Assert.That(retainedGroups,
                Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0], map.LightColorEventBoxGroups[2] }));
            Assert.That(
                GLSEventCommon.TryGetColorTransitionEndTime(map.LightColorEventBoxGroups[2].Boxes[0].Events[0], out var endTime),
                Is.True);
            Assert.That(endTime, Is.EqualTo(18f));
        }

        [Test]
        public void ResetAfterPhysicalEditRebuildsTimelineFromCurrentGroups()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(8f, 0, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var firstTimeline = GLSEventCommon.GetColorTimeline(source, 1);
            Assert.That(firstTimeline, Is.Not.Null);

            var inserted = new BaseLightColorEventBoxGroup(CreateGroup(4f, 0, 1));
            inserted.SetMap(map);
            inserted.RecomputeSongBpmTime();
            GLSEventCommon.AddColorTransitionGroup(inserted);
            var insertedNode = inserted.Boxes[0].Events[0];

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var rewiredEnd), Is.True);
            Assert.That(rewiredEnd, Is.EqualTo(4f));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(5f, null, retainedGroups);
            Assert.That(retainedGroups, Is.EquivalentTo(new[] { inserted }));

            GLSEventCommon.ResetColorTransitionLightCounts();
            retainedGroups.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(5f, null, retainedGroups);
            Assert.That(retainedGroups, Is.Empty, "A reset must clear the previous environment's ribbon intervals.");

            GLSEventCommon.SetColorTransitionLightCount(0, 1);
            var rebuiltTimeline = GLSEventCommon.GetColorTimeline(source, 1);
            Assert.AreNotSame(firstTimeline, rebuiltTimeline,
                "Re-registering after a reset must rebuild from the current groups.");
            Assert.That(rebuiltTimeline.TryGetOutgoing(source, 0, out var outgoing), Is.True);
            Assert.That(outgoing.Next.Base, Is.SameAs(insertedNode));
            GLSEventCommon.GetColorTransitionSourceGroupsAt(5f, null, retainedGroups);
            Assert.That(retainedGroups, Is.EquivalentTo(new[] { inserted }));
        }

        // A caller passing zero must still use the group's registered count to resolve overlapping filters.
        [Test]
        public void KnownCountZeroWidthRibbonUsesPhysicalTimeline()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(
                    5f,
                    0,
                    1,
                    filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2,
                    filterChunks: 0)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var target = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            GLSEventCommon.SetColorTransitionLightCount(0, 4);
            var timeline = GLSEventCommon.GetColorTimeline(source, 4);
            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.TryGetOutgoing(source, 0, out var overlapped), Is.True);
            Assert.That(overlapped.Next.Base, Is.SameAs(target));
            Assert.That(timeline.TryGetOutgoing(source, 1, out var held), Is.True);
            Assert.That(held.EndTime, Is.EqualTo(float.MaxValue));

            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS zero-width ribbon test");
            var incomingObject = new GameObject("GLS zero-width incoming ribbon test");
            var originalVisualize = Settings.Instance.VisualizeGLSLightTransitions;
            try
            {
                Settings.Instance.VisualizeGLSLightTransitions = true;
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 0);

                Assert.That(
                    ribbonObject.activeSelf,
                    Is.True,
                    "A width-0 update must reuse the registered light count and draw the physical timeline ribbon.");
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(
                    properties.GetFloat(Shader.PropertyToID("_UseLightTimeline")),
                    Is.EqualTo(1f),
                    "The registered count must upload the per-light timeline payload.");

                var incomingController = CreateRibbonController(incomingObject, out var incomingRenderer);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    incomingController, target, appearance, _ => false, 0);
                Assert.That(
                    incomingObject.activeSelf,
                    Is.True,
                    "The incoming ribbon must also resolve the registered light count for a width-0 caller.");
                var incomingProperties = new MaterialPropertyBlock();
                incomingRenderer.GetPropertyBlock(incomingProperties);
                Assert.That(
                    incomingProperties.GetFloat(Shader.PropertyToID("_UseLightTimeline")),
                    Is.EqualTo(1f));
            }
            finally
            {
                Settings.Instance.VisualizeGLSLightTransitions = originalVisualize;
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(incomingObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // This older Wave snapshot has six groups and nine nodes on WeaveEnvironment's eight-light group 1.
        // Even lights reach the filtered beat-11 node before the all-light beat-11.25 node; odd lights skip
        // beat 11. The all-light node cancels the filtered box's pending children. Distribution-only cases
        // move interrupting groups to another ID so those later children can play. GLSColorPlaybackTestBase
        // uses the current three-group version, where sibling boxes divide lights by first-box ownership.
        private const int WaveLightCount = 8;

        // The Wave fixture must retain its group/node counts, filter coverage, raw distribution strings, and
        // custom easing keys.
        [Test]
        public void WaveMapFixtureRebuildsAuthoredGroupsNodesAndColorDistributionPayloads()
        {
            var map = LoadWaveTestingMap();
            var groups = map.LightColorEventBoxGroups;
            Assert.That(groups.Count, Is.EqualTo(6));
            Assert.That(groups.Select(x => x.ID), Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1 }));
            Assert.That(
                groups.Select(x => x.Boxes[0].Events.Length),
                Is.EqualTo(new[] { 1, 1, 4, 1, 1, 1 }));
            Assert.That(
                groups[2].Boxes[0].Events.Select(x => x.JsonTime),
                Is.EqualTo(new[] { 11f, 17f, 24.75f, 34f }));
            Assert.That(WaveNode(map, 0).JsonTime, Is.EqualTo(0f));
            Assert.That(WaveNode(map, 1).JsonTime, Is.EqualTo(1.5f));
            Assert.That(WaveNode(map, 3).JsonTime, Is.EqualTo(11.25f));
            Assert.That(WaveNode(map, 4).JsonTime, Is.EqualTo(22f));
            Assert.That(WaveNode(map, 5).JsonTime, Is.EqualTo(46f));
            Assert.That(
                IndexFilterHelper.Convert(groups[2].Boxes[0].IndexFilter, WaveLightCount)
                    ?.Select(x => x.Element),
                Is.EqualTo(new[] { 0, 2, 4, 6 }),
                "The beat-11 box's step-and-offset filter must select only the four even lights.");
            Assert.That(
                IndexFilterHelper.Convert(groups[0].Boxes[0].IndexFilter, WaveLightCount)
                    ?.Select(x => x.Element),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }));

            var firstColorDistributionNode = WaveNode(map, 2, 2);
            Assert.That(firstColorDistributionNode.ColorDistributions, Is.EqualTo(new[] { "v,1,l,l" }));
            Assert.That(firstColorDistributionNode.StrobeColorDistributions, Is.EqualTo(new[] { "sv,1,l" }));
            var secondColorDistributionNode = WaveNode(map, 2, 3);
            Assert.That(secondColorDistributionNode.ParsedColorDistributions.Count, Is.EqualTo(1));
            Assert.That(secondColorDistributionNode.ParsedStrobeColorDistributions.Count, Is.EqualTo(2));
            AssertColor(
                WaveNode(map, 1).CustomColor.Value,
                new Color(1f, 0.4f, 0.913f, 1f));
            Assert.That(WaveNode(map, 4).ChromaStrobeColorEasing, Is.EqualTo(1));
            Assert.That(WaveNode(map, 4).ChromaStrobeEasing, Is.EqualTo(11));
            Assert.That(WaveNode(map, 5).ChromaColorEasing, Is.EqualTo(11));

            // Same-filter control that already resolves today: the first A node's only follower is A@1.5.
            Assert.That(
                GLSEventCommon.TryGetColorTransitionEndTime(WaveNode(map, 0), out var firstEnd),
                Is.True);
            Assert.That(firstEnd, Is.EqualTo(1.5f));
        }

        // WaveMapAllLightRibbonSplitsAtFilteredInterruption: requested strips send the beat-1.5
        // node's even lanes to the beat-11 filtered child while odd lanes keep running to the
        // beat-11.25 all-light node; one scalar following event cannot express both, so this
        // asserts the per-light endpoint rows instead of a single transition time.
        [Test]
        public void WaveMapAllLightRibbonSplitsAtFilteredInterruption()
        {
            var map = LoadWaveTestingMap();
            var appearance = CreateWaveAppearance();
            try
            {
                AssertWaveRibbonStrips(
                    WaveNode(map, 1),
                    EvenOddWaveTargets(WaveNode(map, 2), WaveNode(map, 3)),
                    appearance);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapFilteredRibbonEndsAtAllLightInterruption: requested strips carry the beat-11
        // filtered node's four controlled lights into the 11.25 all-light node instead of running
        // straight through to the next same-filter child at beat 17.
        [Test]
        public void WaveMapFilteredRibbonEndsAtAllLightInterruption()
        {
            var map = LoadWaveTestingMap();
            var appearance = CreateWaveAppearance();
            try
            {
                var source = WaveNode(map, 2, 0);
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True,
                    "Every light the beat-11 filtered node controls shares the 11.25 all-light node as its next node.");
                Assert.That(endTime, Is.EqualTo(11.25f));
                AssertWaveRibbonStrips(
                    source,
                    EvenOddWaveTargets(WaveNode(map, 3), null),
                    appearance);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // A later group cancels the prior box's pending children; both sets of lights now continue to the beat-22 all-light node.
        [Test]
        public void WaveMapAllLightTakeoverDoesNotResumeFilteredChildren()
        {
            var map = LoadWaveTestingMap();
            var appearance = CreateWaveAppearance();
            try
            {
                AssertWaveRibbonStrips(
                    WaveNode(map, 3),
                    EvenOddWaveTargets(WaveNode(map, 4), WaveNode(map, 4)),
                    appearance);
                Assert.IsFalse(GLSEventCommon.TryGetColorTransitionEndTime(WaveNode(map, 2, 1), out _),
                    "The canceled beat-17 child must not reappear after the all-light takeover.");
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapFilteredRibbonRejoinsAllLightSequenceAtNextNode: requested strips send the
        // beat-17 child's controlled lights to the beat-22 all-light node rather than the
        // same-filter sibling at beat 24.75.
        [Test]
        public void WaveMapFilteredRibbonRejoinsAllLightSequenceAtNextNode()
        {
            var map = LoadWaveTestingMap();
            // Let the beat-17 child exist; the later beat-22 group still takes over and must receive its ribbon.
            map.LightColorEventBoxGroups[3].ID = 2;
            var appearance = CreateWaveAppearance();
            try
            {
                var source = WaveNode(map, 2, 1);
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True,
                    "Every light the beat-17 filtered node controls shares the beat-22 all-light node as its next node.");
                Assert.That(endTime, Is.EqualTo(22f));
                AssertWaveRibbonStrips(
                    source,
                    EvenOddWaveTargets(WaveNode(map, 4), null),
                    appearance);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // Once canceled by a different group, later filtered children cannot interrupt C's continuation to the final all-light node.
        [Test]
        public void WaveMapAllLightContinuationIgnoresCanceledFilteredChildren()
        {
            var map = LoadWaveTestingMap();
            var appearance = CreateWaveAppearance();
            try
            {
                AssertWaveRibbonStrips(
                    WaveNode(map, 4),
                    EvenOddWaveTargets(WaveNode(map, 5), WaveNode(map, 5)),
                    appearance);
                Assert.IsFalse(GLSEventCommon.TryGetColorTransitionEndTime(WaveNode(map, 2, 2), out _),
                    "The canceled beat-24.75 child cannot claim any strip from C.");
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // At beat 34, hs,-0.4,L affects only even lights. Recompute expected hue and saturation with Unity
        // HSV math, leaving odd strip centers dark.
        [Test]
        public void WaveMapMatchingFilterRibbonKeepsAuthoredPerLightColorDistributionStrips()
        {
            // Isolate color-distribution rendering from group takeover; canceled nodes are covered by the interruption regressions.
            var map = LoadWaveTestingMap(interruptFiltered: false);
            var source = WaveNode(map, 2, 2);
            var appearance = CreateWaveAppearance();
            var ribbonObject = new GameObject("GLS wave matching-filter ribbon test");
            Material maskedMaterial = null;
            try
            {
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True);
                Assert.That(endTime, Is.EqualTo(34f));

                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, _ => false, WaveLightCount);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                var dump = DescribeTexture(texture);
                Color.RGBToHSV(new Color(0.179f, 1f, 0f), out var mainHue, out var mainSaturation, out var mainValue);
                Color.RGBToHSV(new Color(0.969f, 0f, 0.941f), out var strobeHue, out _, out var strobeValue);
                for (var order = 0; order < 4; order++)
                {
                    var lightIndex = order * 2;
                    var textureX = WaveLightCount - lightIndex - 1;
                    var progress = order / 3f;
                    // "hs,-0.4,L" rotates hue and drops saturation by the same linear share of
                    // selected-light order; the box's 0.03 wave adds 0.01 alpha per step.
                    var expectedMain = Color.HSVToRGB(
                        Mathf.Repeat(mainHue - (0.4f * progress), 1f),
                        Mathf.Clamp01(mainSaturation - (0.4f * progress)),
                        mainValue,
                        true);
                    expectedMain.a = 0.8f + (0.03f * progress);
                    // Strobe applies "hs,0.4,L" then "v,2,L" cumulatively per light.
                    var expectedStrobe = Color.HSVToRGB(
                        Mathf.Repeat(strobeHue + (0.4f * progress), 1f),
                        1f,
                        strobeValue + (2f * progress),
                        true);
                    expectedStrobe.a = 1f;
                    var label = $"light{lightIndex}";
                    // Endpoint rows are RGBAHalf; literal rows need the existing 0.001 texel tolerance.
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        new Color(0f, 0.5f, 0f, 0.5f + (0.03f * progress)),
                        $"{label} source-main\n{dump}",
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 1),
                        expectedMain,
                        $"{label} transition-main\n{dump}",
                        0.005f);
                    AssertColor(
                        texture.GetPixel(textureX, 2),
                        new Color(0f, 0.7f, 0.7f, 0.8f),
                        $"{label} source-strobe\n{dump}",
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 3),
                        expectedStrobe,
                        $"{label} transition-strobe\n{dump}",
                        0.005f);
                }

                // Odd texels can contain neighboring tween data for edge blending, but odd light centers must
                // stay dark in this even-filtered ribbon.
                maskedMaterial = CreateWaveSampleMaterial(properties);
                for (var lightIndex = 1; lightIndex < WaveLightCount; lightIndex += 2)
                {
                    var maskedPixel = RenderGradientPixel(
                        maskedMaterial, 0.5f, WaveLaneForLight(lightIndex));
                    Assert.That(maskedPixel.r + maskedPixel.g + maskedPixel.b,
                        Is.EqualTo(0f).Within(0.004f), $"light{lightIndex} must stay masked");
                }
            }
            finally
            {
                Object.DestroyImmediate(maskedMaterial);
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // With sf=1, the 24.75-to-34 transition uses a Cubic InOut pulse and frequency 1-to-3 ramp. Progress
        // 0.556 samples the normal phase; 0.581 samples the strobe phase. Render both through Basic Gradient
        // to check per-light coverage.
        [Test]
        public void WaveMapColorDistributionStripsRenderThroughRealShader()
        {
            // Keep the tested color-distribution interval alive instead of expecting a canceled child to emit pixels.
            var map = LoadWaveTestingMap(interruptFiltered: false);
            var appearance = CreateWaveAppearance();
            var ribbonObject = new GameObject("GLS wave strip render test");
            Material sampleMaterial = null;
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller,
                    WaveNode(map, 2, 2),
                    appearance,
                    _ => false,
                    WaveLightCount);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                sampleMaterial = CreateWaveSampleMaterial(properties);

                var evenNormal = RenderGradientPixel(sampleMaterial, 0.556f, WaveLaneForLight(0));
                Assert.That(evenNormal.g, Is.GreaterThan(evenNormal.r), "light0 normal band stays green-dominant");
                Assert.That(evenNormal.g, Is.GreaterThan(evenNormal.b), "light0 normal band\n" + evenNormal);
                var firstStrobe = RenderGradientPixel(sampleMaterial, 0.581f, WaveLaneForLight(0));
                Assert.That(firstStrobe.r, Is.GreaterThan(firstStrobe.g), "light0 strobe band keeps the magenta payload");
                Assert.That(firstStrobe.b, Is.GreaterThan(firstStrobe.g), "light0 strobe band\n" + firstStrobe);
                var lastStrobe = RenderGradientPixel(sampleMaterial, 0.581f, WaveLaneForLight(6));
                Assert.That(
                    lastStrobe.g,
                    Is.GreaterThan(lastStrobe.r),
                    "light6's 'hs,0.4,L'+'v,2,L' strobe endpoint must differ from light0's, proving per-light strips");
                Assert.That(lastStrobe.g, Is.GreaterThan(lastStrobe.b), "light6 strobe band\n" + lastStrobe);
                var oddStrip = RenderGradientPixel(sampleMaterial, 0.581f, WaveLaneForLight(1));
                Assert.That(
                    oddStrip.maxColorComponent,
                    Is.LessThan(0.05f),
                    "An unselected odd lane must stay masked instead of overlaying all eight lights");
            }
            finally
            {
                if (sampleMaterial != null)
                {
                    Object.DestroyImmediate(sampleMaterial);
                }

                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapLastFilteredRibbonReachesFinalAllLightNode: requested strips carry the beat-34
        // shifted child forward to the beat-46 all-light node on even lanes instead of ending the
        // filtered sequence at the box's own last child.
        [Test]
        public void WaveMapLastFilteredRibbonReachesFinalAllLightNode()
        {
            // The final-child link exists only when an earlier different group has not canceled that child.
            var map = LoadWaveTestingMap(interruptFiltered: false);
            var appearance = CreateWaveAppearance();
            try
            {
                var source = WaveNode(map, 2, 3);
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True,
                    "Every light the beat-34 filtered node controls shares the beat-46 all-light node as its next node.");
                Assert.That(endTime, Is.EqualTo(46f));
                AssertWaveRibbonStrips(
                    source,
                    EvenOddWaveTargets(WaveNode(map, 5), null),
                    appearance);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapBoundarySourcesFollowPerLightChronology checks viewport retention against the same
        // per-light model: at beat 12 only the 11.25 node still owns strips (the beat-11 child's
        // evens already ended); at beats 23 and 35 only the beat-22 node continues to the final
        // node because the earlier filtered box's later children were canceled by takeover.
        [Test]
        public void WaveMapBoundarySourcesFollowPerLightChronology()
        {
            var map = LoadWaveTestingMap();
            var groups = map.LightColorEventBoxGroups;
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();
            var retainedSources = new List<BaseLightColorBase>();

            // Control beat that already matches: the beat-1.5 node sources every light's strip.
            GLSEventCommon.GetColorTransitionSourceGroupsAt(5f, null, retainedGroups);
            Assert.That(retainedGroups, Is.EquivalentTo(new[] { groups[1] }));

            GLSEventCommon.GetColorTransitionSourcesAt(12f, groups[2], null, retainedSources);
            Assert.That(
                retainedSources,
                Is.Empty,
                "The beat-11 child's strips end at the 11.25 all-light interruption on its controlled lights.");
            retainedGroups.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(12f, null, retainedGroups);
            Assert.That(
                retainedGroups,
                Is.EquivalentTo(new[] { groups[3] }),
                "Only the 11.25 all-light node still owns strips at beat 12; no straight-through 11->17 link may survive.");

            retainedGroups.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(23f, null, retainedGroups);
            Assert.That(
                retainedGroups,
                Is.EquivalentTo(new[] { groups[4] }),
                "At beat 23 all lights continue 22->46; the canceled filtered children cannot regain ownership.");

            GLSEventCommon.GetColorTransitionSourcesAt(35f, groups[2], null, retainedSources);
            // Retention follows playback ownership too: canceled children must not resurrect offscreen containers.
            Assert.That(retainedSources, Is.Empty,
                "The canceled beat-34 child owns no interval after the all-light takeover.");
            retainedGroups.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(35f, null, retainedGroups);
            Assert.That(retainedGroups, Is.EquivalentTo(new[] { groups[4] }));
        }

        // The map's lowercase easing tokens in v,1,l,l and sv,1,l are invalid. Preserve the strings on save,
        // but do not apply them.
        [Test]
        public void WaveMapAuthoredColorDistributionStringsStayPreservedButParseEmpty()
        {
            var map = LoadWaveTestingMap();
            var node = WaveNode(map, 2, 2);
            Assert.That(node.ColorDistributions, Is.EqualTo(new[] { "v,1,l,l" }));
            Assert.That(node.StrobeColorDistributions, Is.EqualTo(new[] { "sv,1,l" }));
            Assert.That(node.ParsedColorDistributions, Is.Empty);
            Assert.That(node.ParsedStrobeColorDistributions, Is.Empty);

            var appearance = CreateWaveAppearance();
            try
            {
                var mainColors = new Color[WaveLightCount];
                var strobeColors = new Color[WaveLightCount];
                Assert.That(
                    GLSEventCommon.PopulateColorTransitionEndpoint(
                        node, WaveLightCount, false, appearance, mainColors, strobeColors),
                    Is.True);
                for (var order = 0; order < 4; order++)
                {
                    var lightIndex = order * 2;
                    AssertColor(
                        mainColors[lightIndex],
                        new Color(0f, 0.5f, 0f, 0.5f + (0.03f * (order / 3f))),
                        $"light{lightIndex} main keeps only the brightness-distribution gradient");
                    AssertColor(
                        strobeColors[lightIndex],
                        new Color(0f, 0.7f, 0.7f, 0.8f),
                        $"light{lightIndex} strobe stays uniform while 'sv,1,l' fails to parse");
                }

                for (var lightIndex = 1; lightIndex < WaveLightCount; lightIndex += 2)
                {
                    AssertColor(mainColors[lightIndex], Color.black, $"light{lightIndex} unselected");
                    AssertColor(strobeColors[lightIndex], Color.black, $"light{lightIndex} unselected");
                }
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // Using uppercase Linear tokens, v,1,L,l and sv,1,L, applies the intended offsets across the four
        // selected lights.
        [Test]
        public void WaveMapCorrectedColorDistributionInstructionsDistributeAcrossSelectedLights()
        {
            // Compare corrected color-distribution parsing and pixels on a live interval, not one canceled by the older takeover fixture.
            var map = LoadWaveTestingMap(new[] { "v,1,L,l" }, new[] { "sv,1,L" }, interruptFiltered: false);
            var node = WaveNode(map, 2, 2);
            Assert.That(node.ParsedColorDistributions.Count, Is.EqualTo(1));
            Assert.That(node.ParsedColorDistributions[0].Targets, Is.EqualTo(GLSColorDistributionTargets.Value));
            Assert.That(node.ParsedColorDistributions[0].Offset, Is.EqualTo(1f));
            Assert.That(node.ParsedColorDistributions[0].UsesAffectedLightProgress, Is.True);
            Assert.That(node.ParsedStrobeColorDistributions.Count, Is.EqualTo(1));
            Assert.That(
                node.ParsedStrobeColorDistributions[0].Targets,
                Is.EqualTo(GLSColorDistributionTargets.Saturation | GLSColorDistributionTargets.Value));
            Assert.That(node.ParsedStrobeColorDistributions[0].UsesAffectedLightProgress, Is.False);

            var appearance = CreateWaveAppearance();
            var ribbonObject = new GameObject("GLS corrected color-distribution ribbon test");
            try
            {
                var mainColors = new Color[WaveLightCount];
                var strobeColors = new Color[WaveLightCount];
                Assert.That(
                    GLSEventCommon.PopulateColorTransitionEndpoint(
                        node, WaveLightCount, false, appearance, mainColors, strobeColors),
                    Is.True);

                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, node, appearance, _ => false, WaveLightCount);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                var dump = DescribeTexture(texture);
                for (var order = 0; order < 4; order++)
                {
                    var lightIndex = order * 2;
                    var progress = order / 3f;
                    var textureX = WaveLightCount - lightIndex - 1;
                    // "v,1,L,l" lifts HSV value by the per-light share; "sv,1,L" pushes the cyan
                    // strobe's value identically through the chunk coordinate.
                    var expectedMain = new Color(0f, 0.5f + progress, 0f, 0.5f + (0.03f * progress));
                    var expectedStrobe =
                        new Color(0f, 0.7f + progress, 0.7f + progress, 0.8f);
                    var label = $"light{lightIndex}";
                    AssertColor(mainColors[lightIndex], expectedMain, $"{label} preview main");
                    AssertColor(strobeColors[lightIndex], expectedStrobe, $"{label} preview strobe");
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        expectedMain,
                        $"{label} ribbon source-main\n{dump}",
                        0.005f);
                    AssertColor(
                        texture.GetPixel(textureX, 2),
                        expectedStrobe,
                        $"{label} ribbon source-strobe\n{dump}",
                        0.005f);
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // InstantColorTargetOptionallyDrawsSolidStepRibbon retains the requested step-strip option: an Instant
        // (i=0) follower holds the preceding state rather than suppressing its ribbon. Strobing sources
        // continue to depict their active pulse, while non-strobing sources remain solid until the step.
        // Keep this separate from the authored wave-map assertions, whose destinations all transition.
        [Test]
        public void InstantColorTargetOptionallyDrawsSolidStepRibbon()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 1),
                CreateGroup(5f, 0, 0)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            source.CustomColor = new Color(0.9f, 0.1f, 0.2f, 1f);
            source.Brightness = 1f;
            // The step-strip test uses the physical count registered for the group.
            GLSEventCommon.SetColorTransitionLightCount(0, WaveLightCount);
            var instantTarget = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            instantTarget.CustomColor = new Color(0.1f, 0.2f, 0.9f, 1f);
            instantTarget.Brightness = 1f;
            var appearance = CreateWaveAppearance();
            var ribbonObject = new GameObject("GLS step ribbon test");
            try
            {
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True,
                    "Desired option: the interval before an Instant endpoint still owns a strip.");
                Assert.That(endTime, Is.EqualTo(5f));
                var controller = CreateRibbonController(ribbonObject, out var _);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, _ => false, WaveLightCount);
                Assert.That(
                    ribbonObject.activeSelf,
                    Is.True,
                    "Desired option: a solid step strip renders instead of hiding the ribbon.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // Use a minimal renderer to check the production controller's property block without unrelated prefab
        // state.
        internal static LightGradientController CreateRibbonController(
            GameObject ribbonObject,
            out MeshRenderer renderer)
        {
            renderer = ribbonObject.AddComponent<MeshRenderer>();
            var controller = ribbonObject.AddComponent<LightGradientController>();
            var rendererField = typeof(LightGradientController).GetField(
                "meshRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rendererField, Is.Not.Null);
            rendererField.SetValue(controller, renderer);
            return controller;
        }

        // Render one interpolated UV to isolate the fragment shader's output.
        internal static Color RenderGradientPixel(Material material, float progress, float lane)
        {
            // Dark linear colors fall below an 8-bit render target's precision. Use ARGBFloat so converting
            // the result back to sRGB preserves those channels.
            var renderTexture = new RenderTexture(
                1,
                1,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0, 0),
                    new Vector3(1, 0),
                    new Vector3(1, 1),
                    new Vector3(0, 1)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                uv = new[]
                {
                    new Vector2(progress, lane),
                    new Vector2(progress, lane),
                    new Vector2(progress, lane),
                    new Vector2(progress, lane)
                }
            };
            var readTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var previousRenderTexture = RenderTexture.active;
            try
            {
                renderTexture.Create();
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadOrtho();
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                GL.PopMatrix();
                readTexture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                readTexture.Apply(false, false);
                return readTexture.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                Object.DestroyImmediate(readTexture);
                Object.DestroyImmediate(mesh);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }

        // Render several rows so screen-space derivatives cover real strip edges. Inset the quad when testing
        // its outer edges.
        internal static Color[] RenderGradientColumn(
            Material material, float progress, int height, float bottom = 0f, float top = 1f,
            Material secondMaterial = null, float secondProgress = 0f)
        {
            var renderTexture = new RenderTexture(
                1,
                height,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0, bottom),
                    new Vector3(1, bottom),
                    new Vector3(1, top),
                    new Vector3(0, top)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                uv = new[]
                {
                    new Vector2(progress, 0f),
                    new Vector2(progress, 0f),
                    new Vector2(progress, 1f),
                    new Vector2(progress, 1f)
                }
            };
            var readTexture = new Texture2D(1, height, TextureFormat.RGBAFloat, false, true);
            var previousRenderTexture = RenderTexture.active;
            try
            {
                renderTexture.Create();
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadOrtho();
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                // AdjacentOwnedLightStripsDoNotExposeBackground composites the second
                // owner in this same strip-boundary render target.
                if (secondMaterial != null)
                {
                    mesh.uv = new[]
                    {
                        new Vector2(secondProgress, 0f),
                        new Vector2(secondProgress, 0f),
                        new Vector2(secondProgress, 1f),
                        new Vector2(secondProgress, 1f)
                    };
                    secondMaterial.SetPass(0);
                    Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                }
                GL.PopMatrix();
                readTexture.ReadPixels(new Rect(0, 0, 1, height), 0, 0);
                readTexture.Apply(false, false);
                return readTexture.GetPixels(0, 0, 1, height);
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                Object.DestroyImmediate(readTexture);
                Object.DestroyImmediate(mesh);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }

        // Render all contributing ribbons together so pixel derivatives cover both timeline and strip
        // boundaries.
        private static Color[] RenderRibbonPlane(
            Material[] materials, LightGradientController[] ribbons,
            float timeStart, float timeEnd, int width, int height,
            bool perspective = false)
        {
            var target = new RenderTexture(width, height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(perspective ? 0.18f : 0f, 0),
                    new Vector3(perspective ? 0.82f : 1f, 0),
                    new Vector3(perspective ? 0.65f : 1f, 1),
                    new Vector3(perspective ? 0.35f : 0f, 1)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            var previousTarget = RenderTexture.active;
            try
            {
                // Use a perspective plane; an affine trapezoid changes pixel derivatives at its triangle
                // diagonal.
                if (perspective)
                {
                    const float depth = 0.64f / 0.30f;
                    mesh.vertices = new[]
                    {
                        new Vector3(0.18f, 0f, -1f), new Vector3(0.82f, 0f, -1f),
                        new Vector3(0.65f * depth, depth, -depth),
                        new Vector3(0.35f * depth, depth, -depth)
                    };
                }
                target.Create();
                RenderTexture.active = target;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                if (perspective)
                {
                    GL.LoadProjectionMatrix(Matrix4x4.Frustum(0f, 1f, 0f, 1f, 1f, 10f));
                    GL.modelview = Matrix4x4.identity;
                }
                else
                {
                    GL.LoadOrtho();
                }
                for (var n = 0; n < materials.Length; n++)
                {
                    var start = (timeStart - ribbons[n].ColorTimelineStart) / ribbons[n].ColorTimelineDuration;
                    var end = (timeEnd - ribbons[n].ColorTimelineStart) / ribbons[n].ColorTimelineDuration;
                    mesh.uv = perspective
                        ? new[]
                        {
                            new Vector2(start, 0), new Vector2(start, 1),
                            new Vector2(end, 1), new Vector2(end, 0)
                        }
                        : new[]
                        {
                            new Vector2(start, 0), new Vector2(end, 0),
                            new Vector2(end, 1), new Vector2(start, 1)
                        };
                    materials[n].SetPass(0);
                    Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                }
                GL.PopMatrix();
                readTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readTexture.Apply(false, false);
                return readTexture.GetPixels();
            }
            finally
            {
                RenderTexture.active = previousTarget;
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(readTexture);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        // Vary timeline progress across the render target. Inset the quad when testing its front and back
        // edges.
        internal static Color[] RenderGradientRow(
            Material material, float lane, int width, float left = 0f, float right = 1f)
        {
            var renderTexture = new RenderTexture(
                width, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(left, 0), new Vector3(right, 0),
                    new Vector3(right, 1), new Vector3(left, 1)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                uv = new[]
                {
                    new Vector2(0f, lane), new Vector2(1f, lane),
                    new Vector2(1f, lane), new Vector2(0f, lane)
                }
            };
            var readTexture = new Texture2D(width, 1, TextureFormat.RGBAFloat, false, true);
            var previousRenderTexture = RenderTexture.active;
            try
            {
                renderTexture.Create();
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadOrtho();
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                GL.PopMatrix();
                readTexture.ReadPixels(new Rect(0, 0, width, 1), 0, 0);
                readTexture.Apply(false, false);
                return readTexture.GetPixels(0, 0, width, 1);
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                Object.DestroyImmediate(readTexture);
                Object.DestroyImmediate(mesh);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }

        // Render both node ribbons at their real timeline spans, including mesh overlap.
        private static Color[] RenderJoinedTimelineRow(
            Material firstMaterial, LightGradientController firstRibbon,
            Material secondMaterial, LightGradientController secondRibbon,
            float viewStart, float viewEnd, float lane, int width)
        {
            var target = new RenderTexture(width, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readTexture = new Texture2D(width, 1, TextureFormat.RGBAFloat, false, true);
            var mesh = new Mesh();
            var previousTarget = RenderTexture.active;
            try
            {
                target.Create();
                RenderTexture.active = target;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadOrtho();
                DrawRibbon(firstMaterial, firstRibbon);
                DrawRibbon(secondMaterial, secondRibbon);
                GL.PopMatrix();
                readTexture.ReadPixels(new Rect(0, 0, width, 1), 0, 0);
                readTexture.Apply(false, false);
                return readTexture.GetPixels(0, 0, width, 1);
            }
            finally
            {
                RenderTexture.active = previousTarget;
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(readTexture);
                target.Release();
                Object.DestroyImmediate(target);
            }

            void DrawRibbon(Material material, LightGradientController ribbon)
            {
                var left = (ribbon.ColorTimelineStart - viewStart) / (viewEnd - viewStart);
                var right = (ribbon.ColorTimelineStart + ribbon.ColorTimelineDuration - viewStart)
                    / (viewEnd - viewStart);
                mesh.vertices = new[]
                {
                    new Vector3(left, 0f), new Vector3(right, 0f),
                    new Vector3(right, 1f), new Vector3(left, 1f)
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.uv = new[]
                {
                    new Vector2(0f, lane), new Vector2(1f, lane),
                    new Vector2(1f, lane), new Vector2(0f, lane)
                };
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
            }
        }

        internal static Color ApplyExpectedRibbonOpacity(Color lightColor)
        {
            const float alphaAtLightLevel100 = 0.6f;
            var lightLevel = Mathf.Max(lightColor.a, 0f);
            var scale = (1f - alphaAtLightLevel100) / alphaAtLightLevel100;
            var opacity = lightLevel / (lightLevel + scale);
            lightColor.a = opacity;
            return lightColor;
        }

        // Calculate expected opacity and white boost from the light color without reusing the ribbon shader.
        internal static Color CalculateExpectedRibbonPixel(Color lightColor)
        {
            var composed = ApplyExpectedRibbonOpacity(lightColor);
            var ribbonAlpha = composed.a;
            var lightLevel = Mathf.Max(lightColor.a, 0f);
            var colorPeak = Mathf.Max(lightColor.r, Mathf.Max(lightColor.g, lightColor.b));
            var normalizedColor = lightColor / Mathf.Max(colorPeak, 1f);
            // Clamp negative Back/Elastic overshoot before display conversion, as the light shader does.
            normalizedColor.r = Mathf.Max(normalizedColor.r, 0f);
            normalizedColor.g = Mathf.Max(normalizedColor.g, 0f);
            normalizedColor.b = Mathf.Max(normalizedColor.b, 0f);
            var overbright = Mathf.Max(lightLevel - 1f, 0f);
            const float halfWhiteOverbright = 3f;
            const float maximumWhiteBlend = 0.85f;
            var whiteCurveScaleSquared = (halfWhiteOverbright * halfWhiteOverbright)
                * ((maximumWhiteBlend / 0.5f) - 1f);
            var whiteMix = maximumWhiteBlend * (overbright * overbright)
                / ((overbright * overbright) + whiteCurveScaleSquared);
            var surfaceColor = Color.LerpUnclamped(normalizedColor, Color.white, whiteMix) * ribbonAlpha;
            return new Color(
                Mathf.Clamp01(surfaceColor.r),
                Mathf.Clamp01(surfaceColor.g),
                Mathf.Clamp01(surfaceColor.b),
                0f);
        }

        // Timeline texture rows must retain the same map colors consumed directly by the light shader, before
        // ribbon opacity and display compensation.
        [Test]
        public void TimelineStripPreservesSampledColorSpace()
        {
            var texture = new Texture2D(1, 9, TextureFormat.RGBAFloat, false, true);
            var rows = new Color[9];
            rows[0] = new Color(0f, 0f, 0.5f, 1f);
            rows[1] = new Color(0f, 0f, 0.5f, 1f);
            rows[4] = new Color(0f, 1f, 0f, 1f);
            rows[6] = new Color(0f, 0f, 1f, 1f);
            rows[7] = new Color(0f, 0f, 0f, 2f);
            texture.SetPixels(rows);
            texture.Apply(false, false);
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient")) { enableInstancing = false };
            // Disable camera brightness effects so expected output is the map's RGB multiplied once by
            // opacity 0.6.
            var originalBaseColorBoost = Shader.GetGlobalFloat("_BaseColorBoost");
            var originalBaseColorBoostThreshold = Shader.GetGlobalFloat("_BaseColorBoostThreshold");
            Shader.SetGlobalFloat("_BaseColorBoost", 0f);
            Shader.SetGlobalFloat("_BaseColorBoostThreshold", 0f);
            try
            {
                material.SetFloat("_UseLightTimeline", 1f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 1f);
                material.SetTexture("_LightDistributionTex", texture);
                var pixel = RenderGradientPixel(material, 0.5f, 0.5f);
                Debug.Log($"[TimelineLinearProbe] pixel={pixel}");
                // At brightness 100 and opacity 0.6, blue 0.5 must be premultiplied exactly once.
                Assert.That(
                    pixel.gamma.b,
                    Is.EqualTo(0.3f).Within(0.02f),
                    "Authored blue 0.5 at level 100 must be multiplied once by ribbon alpha 0.6");
            }
            finally
            {
                Shader.SetGlobalFloat("_BaseColorBoost", originalBaseColorBoost);
                Shader.SetGlobalFloat("_BaseColorBoostThreshold", originalBaseColorBoostThreshold);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        // Component assertions keep the ribbon regression sensitive to every HDR/alpha channel instead of Color's aggregate equality.
        private static void AssertColor(
            Color actual,
            Color expected,
            string context = null,
            float tolerance = 0.0001f)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), context);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), context);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), context);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), context);
        }

        private static string DescribeTexture(Texture2D texture)
        {
            var result = new StringBuilder();
            for (var y = 0; y < texture.height; y++)
            {
                result.Append($"row{y}:");
                for (var x = 0; x < texture.width; x++)
                {
                    var color = texture.GetPixel(x, y);
                    result.Append(
                        $" [{x}]=({color.r:0.###},{color.g:0.###},{color.b:0.###},{color.a:0.###})");
                }
            }

            return result.ToString();
        }

        // Create a map-scoped cache input without involving editor prefabs or viewport state.
        private static BaseDifficulty LoadMap(JSONNode json)
        {
            // Each synthetic map registers its own light counts instead of inheriting another test's environment.
            GLSEventCommon.ResetColorTransitionLightCounts();
            var map = BeatmapFactory.GetDifficultyFromJson(
                json,
                "testmap",
                BeatSaberSongContainer.Instance.Info,
                BeatSaberSongContainer.Instance.MapDifficultyInfo);
            BeatSaberSongContainer.Instance.Map = map;
            return map;
        }

        private static JSONNode CreateDifficultyJson(params JSONNode[] groups)
        {
            var groupArray = new JSONArray();
            foreach (var group in groups)
            {
                groupArray.Add(group);
            }

            return new JSONObject
            {
                ["version"] = "3.2.0",
                ["lightColorEventBoxGroups"] = groupArray,
            };
        }

        // Keep both event boxes' reverse filters and the delayed brightness-2.1 node, which makes overlap
        // visible.
        private static JSONNode CreateReportedBrightJoinGroup(float beat)
        {
            var group = CreateGroup(beat, 19, 1, brightnessDistribution: 0f);
            var box = group["e"][0];
            box["f"]["r"] = 1;
            box["f"]["c"] = 0;
            box["w"] = 0.05f;
            box["d"] = 2;
            box["b"] = 0;
            box["e"][0]["c"] = 0;
            box["e"][0]["s"] = 0;
            ((JSONArray)box["e"]).Add(new JSONObject
            {
                ["b"] = 0.125f,
                ["c"] = 1,
                ["s"] = 2.1f,
                ["i"] = 1,
                ["f"] = 0,
                ["sb"] = 0,
                ["sf"] = 0,
            });
            return group;
        }

        private static JSONObject CreateColorDistributionCustomData(string[] colorDistributions, string[] strobeColorDistributions)
        {
            var result = new JSONObject();
            var colorDistributionArray = new JSONArray();
            foreach (var colorDistribution in colorDistributions)
            {
                colorDistributionArray.Add(colorDistribution);
            }

            var strobeColorDistributionArray = new JSONArray();
            foreach (var strobeColorDistribution in strobeColorDistributions)
            {
                strobeColorDistributionArray.Add(strobeColorDistribution);
            }

            result[GLSColorDistribution.ColorDistributionsKey] = colorDistributionArray;
            result[GLSColorDistribution.StrobeColorDistributionsKey] = strobeColorDistributionArray;
            return result;
        }

        // InnerAlternatingPulseRibbonNeverBorrowsSiblingColor and the reported 427
        // fade share the same opposed filters and event sequence from Collider.
        private static JSONNode CreateAlternatingPulseGroup(float beat)
        {
            var group = CreateGroup(beat, 10, 1, filterParam: 2, filterParam1: 1,
                filterChunks: 0, brightnessDistribution: 0f);
            var otherGroup = CreateGroup(beat, 10, 1, filterParam: 2, filterParam1: 0,
                filterChunks: 0, brightnessDistribution: 0f);
            var firstBox = group["e"][0];
            var secondBox = otherGroup["e"][0];
            firstBox["w"] = 0.05f;
            firstBox["d"] = 2;
            firstBox["b"] = 0;
            secondBox["w"] = 0f;
            secondBox["d"] = 1;
            secondBox["b"] = 0;
            foreach (var box in new[] { firstBox, secondBox })
            {
                box["e"][0]["c"] = 0;
                box["e"][0]["s"] = 0f;
                ((JSONArray)box["e"]).Add(new JSONObject
                {
                    ["b"] = 0.25f, ["c"] = 0, ["s"] = 1.6f, ["i"] = 1,
                    ["f"] = 0, ["sb"] = 0, ["sf"] = 0
                });
                ((JSONArray)box["e"]).Add(new JSONObject
                {
                    ["b"] = 1f, ["c"] = 1, ["s"] = 0f, ["i"] = 1,
                    ["f"] = 0, ["sb"] = 0, ["sf"] = 0
                });
                ((JSONArray)box["e"]).Add(new JSONObject
                {
                    ["b"] = 1.25f, ["c"] = 1, ["s"] = 1.5f, ["i"] = 1,
                    ["f"] = 0, ["sb"] = 0, ["sf"] = 0
                });
                ((JSONArray)box["e"]).Add(new JSONObject
                {
                    ["b"] = 1.5f, ["c"] = 1, ["s"] = 0f, ["i"] = 1,
                    ["f"] = 0, ["sb"] = 0, ["sf"] = 0
                });
            }
            ((JSONArray)group["e"]).Add(secondBox);
            return group;
        }

        private static JSONNode CreateGroup(
            float time,
            int id,
            int transitionType,
            int filterParam = 1,
            string track = null,
            JSONNode boxCustomData = null,
            int filterType = 1,
            int filterParam1 = 0,
            int filterChunks = 1,
            float brightnessDistribution = 1f)
        {
            var eventArray = new JSONArray();
            eventArray.Add(new JSONObject
            {
                ["b"] = 0,
                ["i"] = transitionType,
                ["c"] = 1,
                ["s"] = 1,
                ["f"] = 0,
                ["sb"] = 0,
                ["sf"] = 0,
            });

            var box = new JSONObject
            {
                ["f"] = new JSONObject
                {
                    ["c"] = filterChunks,
                    ["f"] = filterType,
                    ["p"] = filterParam,
                    ["t"] = filterParam1,
                    ["r"] = 0,
                    ["n"] = 0,
                    ["s"] = 0,
                    ["l"] = 0,
                    ["d"] = 0,
                },
                ["w"] = 1,
                ["d"] = 1,
                ["r"] = brightnessDistribution,
                ["t"] = 1,
                ["b"] = 1,
                ["i"] = 0,
                ["e"] = eventArray,
            };
            if (boxCustomData != null)
            {
                box["customData"] = boxCustomData;
            }

            var boxArray = new JSONArray();
            boxArray.Add(box);

            var group = new JSONObject
            {
                ["b"] = time,
                ["g"] = id,
                ["e"] = boxArray,
            };

            if (track != null)
            {
                // Exercise the same group-level track predicate used by viewport retention.
                group["customData"] = new JSONObject { ["unusedKeyTrack"] = track };
            }

            return group;
        }

        // WaveNode indexes the reconstructed six-group topology: groups 0/1/3/4/5 hold one all-light
        // node each; group 2 holds the four step-and-offset children.
        private static BaseLightColorBase WaveNode(BaseDifficulty map, int groupIndex, int eventIndex = 0) =>
            map.LightColorEventBoxGroups[groupIndex].Boxes[0].Events[eventIndex];

        // Wave routing tests explicitly know the physical width; color-distribution-only cases move interrupting groups to another light ID.
        private static BaseDifficulty LoadWaveTestingMap(
            string[] firstColorDistributionNodeColorDistributions = null,
            string[] firstColorDistributionNodeStrobeColorDistributions = null,
            bool interruptFiltered = true)
        {
            var map = LoadMap(CreateWaveTestingDifficultyJson(firstColorDistributionNodeColorDistributions, firstColorDistributionNodeStrobeColorDistributions));
            if (!interruptFiltered)
            {
                map.LightColorEventBoxGroups[3].ID = 2;
                map.LightColorEventBoxGroups[4].ID = 2;
            }
            GLSEventCommon.SetColorTransitionLightCount(1, WaveLightCount);
            return map;
        }

        // Keep all six groups from Wave's ExpertPlusStandard.dat. Optional overrides correct the first
        // distribution node's malformed easing tokens without changing its filters or event times.
        private static JSONNode CreateWaveTestingDifficultyJson(
            string[] firstColorDistributionNodeColorDistributions = null,
            string[] firstColorDistributionNodeStrobeColorDistributions = null)
        {
            var groups = new JSONArray();
            groups.Add(CreateWaveGroup(0f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 0f, 1, 0f,
                    new JSONObject { ["color"] = WaveColor(1f, 0.4f, 0.913f) }))));
            groups.Add(CreateWaveGroup(1.5f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 0f, 1, 0f,
                    new JSONObject { ["color"] = WaveColor(1f, 0.4f, 0.913f) }))));
            groups.Add(CreateWaveGroup(11f, CreateWaveBox(EvenStepWaveFilter(), 0.03f,
                WaveColorNode(0f, 1f, 5, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.179f, 1f, 0f),
                        ["strobeColor"] = WaveColor(0.969f, 0f, 0.942f),
                    }),
                WaveColorNode(6f, 0.6f, 0, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.179f, 1f, 0f),
                        ["strobeColor"] = WaveColor(0.969f, 0f, 0.942f),
                    }),
                WaveColorNode(13.75f, 0.5f, 1, 0.8f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0f, 0.5f, 0f),
                        ["strobeColor"] = WaveColor(0f, 0.7f, 0.7f),
                        ["strobeColorDistributions"] = WaveStrings(firstColorDistributionNodeStrobeColorDistributions ?? new[] { "sv,1,l" }),
                        ["colorDistributions"] = WaveStrings(firstColorDistributionNodeColorDistributions ?? new[] { "v,1,l,l" }),
                    }),
                WaveColorNode(23f, 0.8f, 3, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.179f, 1f, 0f),
                        ["strobeColor"] = WaveColor(0.969f, 0f, 0.941f),
                        ["colorDistributions"] = WaveStrings("hs,-0.4,L"),
                        ["strobeColorDistributions"] = WaveStrings("hs,0.4,L", "v,2,L"),
                    }))));
            groups.Add(CreateWaveGroup(11.25f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 0.5f, 1, 0f,
                    new JSONObject { ["color"] = WaveColor(1f, 0.4f, 0.913f) }))));
            groups.Add(CreateWaveGroup(22f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 0.2f, 1, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.5f, 0f, 0.913f),
                        ["strobeColorEasing"] = 1,
                        ["strobeEasing"] = 11,
                    }))));
            groups.Add(CreateWaveGroup(46f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 1f, 0, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.179f, 1f, 0f),
                        ["strobeColor"] = WaveColor(0.969f, 0f, 0.941f),
                        ["colorEasing"] = 11,
                    }))));
            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["lightColorEventBoxGroups"] = groups,
            };
        }

        private static JSONObject CreateWaveGroup(float beat, params JSONNode[] boxes)
        {
            var boxArray = new JSONArray();
            foreach (var box in boxes)
            {
                boxArray.Add(box);
            }

            return new JSONObject
            {
                ["b"] = beat,
                ["g"] = 1,
                ["e"] = boxArray,
            };
        }

        private static JSONObject CreateWaveBox(
            JSONNode filter,
            float brightnessDistribution,
            params JSONNode[] events)
        {
            var eventArray = new JSONArray();
            foreach (var evt in events)
            {
                eventArray.Add(evt);
            }

            return new JSONObject
            {
                ["f"] = filter,
                ["w"] = 0,
                ["d"] = 1,
                ["r"] = brightnessDistribution,
                ["t"] = 1,
                ["b"] = 0,
                ["i"] = 0,
                ["e"] = eventArray,
            };
        }

        private static JSONObject AllLightWaveFilter() =>
            new()
            {
                ["f"] = 1,
                ["p"] = 1,
                ["t"] = 0,
                ["r"] = 0,
                ["c"] = 0,
                ["n"] = 0,
                ["s"] = 0,
                ["l"] = 0,
                ["d"] = 0,
            };

        private static JSONObject EvenStepWaveFilter() =>
            new()
            {
                ["f"] = 2,
                ["p"] = 0,
                ["t"] = 2,
                ["r"] = 0,
                ["c"] = 8,
                ["n"] = 0,
                ["s"] = 0,
                ["l"] = 0,
                ["d"] = 0,
            };

        private static JSONObject WaveColorNode(
            float relativeBeat,
            float brightness,
            int frequency,
            float strobeBrightness,
            JSONNode customData) =>
            new()
            {
                ["b"] = relativeBeat,
                ["c"] = 0,
                ["s"] = brightness,
                ["i"] = 1,
                ["f"] = frequency,
                ["sb"] = strobeBrightness,
                ["sf"] = 1,
                ["customData"] = customData,
            };

        private static JSONArray WaveColor(params float[] channels)
        {
            var color = new JSONArray();
            foreach (var channel in channels)
            {
                color.Add(channel);
            }

            return color;
        }

        private static JSONArray WaveStrings(params string[] values)
        {
            var strings = new JSONArray();
            foreach (var value in values)
            {
                strings.Add(value);
            }

            return strings;
        }

        private static EventAppearanceSO CreateWaveAppearance()
        {
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            return appearance;
        }

        // EvenOddWaveTargets returns the requested per-light follower list: the filtered node on
        // even lanes and the all-light node on odd lanes (or nothing when a lane keeps no strip).
        private static BaseLightColorBase[] EvenOddWaveTargets(
            BaseLightColorBase evenTarget,
            BaseLightColorBase oddTarget) =>
            Enumerable.Range(0, WaveLightCount)
                .Select(lightIndex => lightIndex % 2 == 0 ? evenTarget : oddTarget)
                .ToArray();

        private static bool[] WaveCoveredLights(BaseLightColorBase evt)
        {
            var covered = new bool[WaveLightCount];
            if (evt.EventBoxData is BaseLightColorEventBox box
                && IndexFilterHelper.Convert(box.IndexFilter, WaveLightCount) is { } filter)
            {
                foreach (var entry in filter)
                {
                    covered[entry.Element] = true;
                }
            }

            return covered;
        }

        private static (Color[] main, Color[] strobe) PopulateWaveEndpoints(
            BaseLightColorBase evt,
            EventAppearanceSO appearance)
        {
            var main = new Color[WaveLightCount];
            var strobe = new Color[WaveLightCount];
            for (var lightIndex = 0; lightIndex < WaveLightCount; lightIndex++)
            {
                main[lightIndex] = Color.black;
                strobe[lightIndex] = Color.black;
            }

            if (evt != null)
            {
                GLSEventCommon.PopulateColorTransitionEndpoint(
                    evt, WaveLightCount, false, appearance, main, strobe);
            }

            return (main, strobe);
        }

        // Each strip ends at that physical light's next node, including its distributed color. Lights outside
        // the source filter stay dark. Texture columns run in reverse light order: width - light - 1.
        private static void AssertWaveRibbonStrips(
            BaseLightColorBase source,
            BaseLightColorBase[] nextPerLight,
            EventAppearanceSO appearance)
        {
            var ribbonObject = new GameObject("GLS wave ribbon test");
            Material maskedSample = null;
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, _ => false, WaveLightCount);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(
                    properties.GetFloat(useLightDistributionId),
                    Is.EqualTo(1f),
                    $"beat {source.JsonTime:R}: per-light targets need the distributed endpoint payload");
                Assert.That(properties.GetFloat(lightDistributionWidthId), Is.EqualTo(WaveLightCount));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null, $"beat {source.JsonTime:R}: missing endpoint texture");
                var covered = WaveCoveredLights(source);
                var (sourceMain, sourceStrobe) = PopulateWaveEndpoints(source, appearance);
                var endpointCache = new Dictionary<BaseLightColorBase, (Color[] main, Color[] strobe)>();
                var dump = DescribeTexture(texture);
                for (var lightIndex = 0; lightIndex < WaveLightCount; lightIndex++)
                {
                    var textureX = WaveLightCount - lightIndex - 1;
                    var next = covered[lightIndex] ? nextPerLight[lightIndex] : null;
                    // A masked light has no target; initialize the tuple so the guarded endpoint assertions are definitely assigned.
                    (Color[] main, Color[] strobe) rows = default;
                    if (next != null && !endpointCache.TryGetValue(next, out rows))
                    {
                        rows = PopulateWaveEndpoints(next, appearance);
                        endpointCache.Add(next, rows);
                    }

                    var expectedSourceMain = covered[lightIndex] ? sourceMain[lightIndex] : Color.black;
                    // Unused strobe channels collapse to primary at stopped endpoints, independently of the phase at arrival.
                    var expectedSourceStrobe = covered[lightIndex]
                        ? GLSEventCommon.GetStrobeFrequency(source) > 0f ? sourceStrobe[lightIndex] : sourceMain[lightIndex]
                        : Color.black;
                    var expectedNextMain = next != null ? rows.main[lightIndex] : Color.black;
                    var expectedNextStrobe = next != null
                        ? GLSEventCommon.GetStrobeFrequency(next) > 0f ? rows.strobe[lightIndex] : rows.main[lightIndex]
                        : Color.black;
                    var label = $"beat {source.JsonTime:R} light{lightIndex}";
                    var targetLabel = next == null ? "none" : $"beat {next.JsonTime:R}";
                    // An unowned texel may cache its neighbor's tween for edge blending, but its light center
                    // must still render dark.
                    if (!covered[lightIndex])
                    {
                        if (maskedSample == null)
                            maskedSample = CreateWaveSampleMaterial(properties);
                        var maskedPixel = RenderGradientPixel(
                            maskedSample, 0.5f, WaveLaneForLight(lightIndex));
                        Assert.That(maskedPixel.r + maskedPixel.g + maskedPixel.b,
                            Is.EqualTo(0f).Within(0.004f), $"{label} must stay masked");
                        continue;
                    }
                    // Endpoint rows are RGBAHalf: HDR strobe values near 3 carry ~0.002 quantization.
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        expectedSourceMain,
                        $"{label} source-main {GLSRibbonTestDiagnostics.FormatColor(expectedSourceMain)}\n{dump}",
                        0.004f);
                    AssertColor(
                        texture.GetPixel(textureX, 1),
                        expectedNextMain,
                        $"{label} transition-main -> {targetLabel} {GLSRibbonTestDiagnostics.FormatColor(expectedNextMain)}\n{dump}",
                        0.004f);
                    AssertColor(
                        texture.GetPixel(textureX, 2),
                        expectedSourceStrobe,
                        $"{label} source-strobe {GLSRibbonTestDiagnostics.FormatColor(expectedSourceStrobe)}\n{dump}",
                        0.004f);
                    AssertColor(
                        texture.GetPixel(textureX, 3),
                        expectedNextStrobe,
                        $"{label} transition-strobe -> {targetLabel} {GLSRibbonTestDiagnostics.FormatColor(expectedNextStrobe)}\n{dump}",
                        0.004f);
                }
            }
            finally
            {
                Object.DestroyImmediate(maskedSample);
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // WaveLaneForLight mirrors the shader: floor(uv.y * width) selects a column and the texture
        // stores light lightIndex at column width - lightIndex - 1, so lane centers sit at
        // (width - 0.5 - lightIndex) / width.
        private static float WaveLaneForLight(int lightIndex) =>
            (WaveLightCount - 0.5f - lightIndex) / WaveLightCount;

        // Compare ribbon pixels with the parametric light shader rendering the same live tween. A second
        // ribbon render would share any color-space bug.
        internal static Material CreateLightSampleMaterial()
        {
            var shader = Shader.Find("ChroMapper/Parametric Box Transparent");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader) { enableInstancing = false };
            // The production material asset supplies these; pin them so the test quad returns the
            // shader's albedo alone (unit alpha ramp, overwrite blend, depth/cull off).
            material.SetFloat("_CullMode", 0f);
            material.SetFloat("_ZTest", 8f);
            material.SetVector("_AlphaWidth", Vector4.one);
            material.SetFloat("_BlendModeSrc", 1f);
            material.SetFloat("_BlendModeDst", 0f);
            return material;
        }

        // Copy the production property block so pixel tests use the real ribbon shader and its uploaded data.
        internal static Material CreateWaveSampleMaterial(MaterialPropertyBlock properties)
        {
            var shader = Shader.Find("ChroMapper/Object/Basic Gradient");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader) { enableInstancing = false };
            // Copy endpoints with SetVector, matching production's unconverted sRGB upload.
            material.SetVector("_ColorA", properties.GetColor(colorAId));
            material.SetVector("_ColorB", properties.GetColor(colorBId));
            material.SetVector("_StrobeColorA", properties.GetColor(strobeColorAId));
            material.SetVector("_StrobeColorB", properties.GetColor(strobeColorBId));
            material.SetFloat("_StrobeDuration", properties.GetFloat(strobeDurationId));
            material.SetFloat("_StrobeFade", properties.GetFloat(strobeFadeId));
            material.SetFloat("_StrobeFrequencyA", properties.GetFloat(strobeFrequencyAId));
            material.SetFloat("_StrobeFrequencyB", properties.GetFloat(strobeFrequencyBId));
            material.SetFloat("_UseStrobeColors", properties.GetFloat(useStrobeColorsId));
            material.SetInt("_EasingID", properties.GetInt(easingId));
            material.SetInt("_UseHSV", properties.GetInt(useHsvId));
            material.SetFloat("_UseLightDistribution", properties.GetFloat(useLightDistributionId));
            material.SetFloat("_LightDistributionWidth", properties.GetFloat(lightDistributionWidthId));
            // GLS playback/ribbon pixel tests must exercise the production per-light clocks, not fall back to the old four-row shader branch.
            material.SetFloat("_UseLightTimeline", properties.GetFloat(Shader.PropertyToID("_UseLightTimeline")));
            material.SetFloat("_LightTimelineDuration", properties.GetFloat(Shader.PropertyToID("_LightTimelineDuration")));
            // Preserve the shared absolute timeline clock in rendered fixtures.
            material.SetFloat("_LightTimelineStart", properties.GetFloat(Shader.PropertyToID("_LightTimelineStart")));
            material.SetVector("_LightBoostData", properties.GetVector(Shader.PropertyToID("_LightBoostData")));
            material.SetFloat("_LightBoostCount", properties.GetFloat(Shader.PropertyToID("_LightBoostCount")));
            material.SetFloat("_LightBoostInitial", properties.GetFloat(Shader.PropertyToID("_LightBoostInitial")));
            if (properties.GetTexture(lightDistributionTextureId) is Texture texture)
            {
                material.SetTexture("_LightDistributionTex", texture);
            }

            return material;
        }
    }
}

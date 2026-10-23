using System.Collections;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaGlowingKeywordRenderingParityTest : TestBase
    {
        private bool previousAnimations;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator AuthoredMainEffectGateOverridesDisabledRendererGate()
        {
            var authoredColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            var uniformColor = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? authoredColor.linear
                : authoredColor;
            var environment = new JSONArray();
            foreach (var authoredGate in new[] { false, true })
            {
                var keywords = JSON.Parse("[\"_WHITEBOOSTTYPE_MAINEFFECT\"]");
                if (authoredGate)
                    keywords.Add("MAIN_EFFECT_ENABLED");

                environment.Add(
                    new JSONObject
                    {
                        ["track"] = authoredGate
                            ? "authored-gate"
                            : "boost-control",
                        ["geometry"] = new JSONObject
                        {
                            ["type"] = "Cube",
                            ["material"] = new JSONObject
                            {
                                ["shader"] = "Glowing", ["color"] = authoredColor, ["shaderKeywords"] = keywords
                            }
                        }
                    });
            }

            yield return TestUtils.ReloadMap(
                3,
                new JSONObject
                {
                    ["version"] = "3.3.0", ["customData"] = new JSONObject { ["environment"] = environment }
                },
                environmentName: "GagaEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            yield return null;

            var containers = Object.FindObjectsByType<GeometryContainer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement.Track is "authored-gate" or "boost-control")
                .ToArray();
            Assert.That(containers.Length, Is.EqualTo(2));
            var cameraObject = new GameObject("GlowingAuthoredGateCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31;
            camera.allowHDR = true;

            var previousPostBloom = Shader.IsKeywordEnabled("POST_BLOOM");
            var previousBloomFog = Shader.IsKeywordEnabled("BLOOM_FOG");
            var previousBoost = Shader.GetGlobalFloat("_BaseColorBoost");
            var previousThreshold = Shader.GetGlobalFloat("_BaseColorBoostThreshold");
            try
            {
                Shader.DisableKeyword("POST_BLOOM");
                Shader.DisableKeyword("BLOOM_FOG");
                Shader.SetGlobalFloat("_BaseColorBoost", 1f);
                Shader.SetGlobalFloat("_BaseColorBoostThreshold", 0f);
                Color actual = Color.clear;
                foreach (var container in containers)
                {
                    var renderer = container.MpbController.Renderers.Single();
                    var previousLayer = renderer.gameObject.layer;
                    var previousPosition = renderer.transform.position;
                    var previousScale = renderer.transform.localScale;
                    var material = renderer.sharedMaterial;
                    var reference = new Material(material);
                    try
                    {
                        renderer.gameObject.layer = 31;
                        renderer.transform.position = new Vector3(0f, 0f, 10f);
                        renderer.transform.localScale = new Vector3(4f, 4f, 0.1f);
                        var pixel = ChromaBaseWaterRenderingParityTest.CaptureCenter(camera);
                        if (container.EnvironmentEnhancement.Track == "boost-control")
                            Assert.That(
                                Vector4.Distance(pixel, Color.white),
                                Is.LessThan(0.003f),
                                "The native no-gate positive control must apply saturated alpha-squared white boost");
                        else
                        {
                            actual = pixel;
                            var gate = new LocalKeyword(reference.shader, "POST_BLOOM");
                            Assert.That(
                                gate.isValid && gate.isOverridable,
                                Is.True,
                                "The compiled adapter must retain the native global-scope gate");
                            reference.EnableKeyword(gate);
                            renderer.sharedMaterial = reference;
                            var referencePixel = ChromaBaseWaterRenderingParityTest.CaptureCenter(camera);
                            // Native 1.44.1 Glowing fragment10 with MAIN_EFFECT_ENABLED and
                            // _WHITEBOOSTTYPE_MAINEFFECT only returns _Color. Global-disabled uses the material's gate.
                            Assert.That(
                                Vector4.Distance(referencePixel, uniformColor),
                                Is.LessThan(0.003f),
                                "The material-enabled gate control must match the independent native constant-color shader");
                            Assert.That(
                                Shader.IsKeywordEnabled("POST_BLOOM"),
                                Is.False,
                                "The reference must preserve the renderer's disabled global gate");
                        }

                    }

                    finally
                    {
                        renderer.sharedMaterial = material;
                        renderer.gameObject.layer = previousLayer;
                        renderer.transform.position = previousPosition;
                        renderer.transform.localScale = previousScale;
                        Object.DestroyImmediate(reference);
                    }

                }

                Assert.That(
                    Vector4.Distance(actual, uniformColor),
                    Is.LessThan(0.003f),
                    "The accepted MAIN_EFFECT_ENABLED material request was dropped by preset dispatch");
            }

            finally
            {
                if (previousPostBloom)
                    Shader.EnableKeyword("POST_BLOOM");

                else
                    Shader.DisableKeyword("POST_BLOOM");

                if (previousBloomFog)
                    Shader.EnableKeyword("BLOOM_FOG");

                else
                    Shader.DisableKeyword("BLOOM_FOG");

                Shader.SetGlobalFloat("_BaseColorBoost", previousBoost);
                Shader.SetGlobalFloat("_BaseColorBoostThreshold", previousThreshold);
                Object.DestroyImmediate(cameraObject);
            }

        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

    }
}

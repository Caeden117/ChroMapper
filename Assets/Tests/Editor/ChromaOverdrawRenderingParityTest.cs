using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaOverdrawRenderingParityTest : TestBase
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
        public IEnumerator BaseWaterAuthoredOverdrawUsesNativeOpaqueGate() => CheckResponse("BaseWater");

        [UnityTest]
        public IEnumerator OpaqueLightAuthoredOverdrawUsesNativeTransparentGate() => CheckResponse("OpaqueLight");

        [UnityTest]
        public IEnumerator TransparentLightAuthoredOverdrawUsesNativeTransparentGate() => CheckResponse("TransparentLight");

        [UnityTest]
        public IEnumerator MirrorAuthoredOverdrawAddsNativeReflection() => CheckResponse("WaterfallMirror");

        private static IEnumerator CheckResponse(string preset)
        {
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject
                {
                    ["version"] = "3.3.0",
                    ["customData"] = new JSONObject
                    {
                        ["environment"] = new JSONArray
                        {
                            [0] = new JSONObject
                            {
                                ["track"] = "overdraw",
                                ["geometry"] = new JSONObject
                                {
                                    // Native transparent blending adds both cube faces. A Quad isolates the per-fragment response.
                                    ["type"] = preset == "TransparentLight"
                                        ? "Quad"
                                        : "Cube",
                                    ["material"] = new JSONObject
                                    {
                                        ["shader"] = preset,
                                        ["color"] = Color.white,
                                        ["shaderKeywords"] = new JSONArray { [0] = "OVERDRAW_VIEW" }
                                    }
                                }
                            }
                        }
                    }
                },
                environmentName: "GagaEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            yield return null;

            var container = Object.FindObjectsByType<GeometryContainer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
                .Single(candidate => candidate.EnvironmentEnhancement.Track == "overdraw");
            var renderer = container.MpbController.Renderers.Single();
            var actualMaterial = renderer.sharedMaterial;
            var previousBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(previousBlock);
            var previousLayer = renderer.gameObject.layer;
            var previousPosition = renderer.transform.position;
            var previousRotation = renderer.transform.rotation;
            var previousScale = renderer.transform.localScale;
            var cameraObject = new GameObject("NativeOverdrawResponseCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31;
            camera.allowHDR = true;

            var controlShader = Shader.Find("ChroMapper/Lit");
            Assert.That(controlShader, Is.Not.Null, "Missing compiled overdraw positive control");
            var control = new Material(controlShader) { shaderKeywords = new[] { "OVERDRAW_VIEW" } };
            var volumeControl = new Material(actualMaterial) { shaderKeywords = new string[0] };
            var neutralNoise = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            neutralNoise.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            neutralNoise.Apply();
            var floatNames = new[] { "_TrueOverdrawOn", "_OpaqueOverdrawOn", "_TransparentOverdrawOn" };
            var previousFloats = floatNames.Select(Shader.GetGlobalFloat).ToArray();
            var previousOverdrawColor = Shader.GetGlobalVector("_OverdrawColor");
            var previousNoise = Shader.GetGlobalTexture("_GlobalBlueNoiseTex");
            var globalKeywords = new[] { "BLOOM_FOG", "POST_BLOOM" };
            var previousKeywords = globalKeywords.Select(Shader.IsKeywordEnabled).ToArray();
            var overdrawColor = new Vector4(0.2f, 0.4f, 0.6f, 1f);
            Camera.CameraCallback publishNoise = renderingCamera =>
            {
                if (renderingCamera == camera)
                    Shader.SetGlobalTexture("_GlobalBlueNoiseTex", neutralNoise);

            };
            Camera.onPreRender += publishNoise;
            try
            {
                renderer.gameObject.layer = 31;
                renderer.transform.position = new Vector3(0f, 0f, 10f);
                renderer.transform.rotation = Quaternion.identity;
                renderer.transform.localScale = new Vector3(4f, 4f, 0.1f);
                var block = new MaterialPropertyBlock();
                block.SetColor("_Color", Color.white);
                // Native Transparent's vertex shader scales X/Z by the box width channels before any fragment runs.
                block.SetVector("_AlphaWidth", Vector4.one);
                block.SetTexture("_ReflectionTex", Texture2D.whiteTexture);
                renderer.SetPropertyBlock(block);
                foreach (var keyword in globalKeywords)
                    Shader.DisableKeyword(keyword);

                if (preset == "TransparentLight")
                {
                    renderer.sharedMaterial = volumeControl;
                    var volumePixel = ChromaBaseWaterRenderingParityTest.CaptureCenter(camera);
                    Assert.That(
                        new Vector3(volumePixel.r, volumePixel.g, volumePixel.b).sqrMagnitude,
                        Is.GreaterThan(0.1f),
                        "The transparent surface must cover the sample before its debug output is compared");
                }

                Shader.SetGlobalVector("_OverdrawColor", overdrawColor);
                Shader.SetGlobalFloat("_TrueOverdrawOn", 1f);
                Shader.SetGlobalFloat("_OpaqueOverdrawOn", 2f);
                Shader.SetGlobalFloat("_TransparentOverdrawOn", 3f);
                renderer.sharedMaterial = control;
                var positive = ChromaBaseWaterRenderingParityTest.CaptureCenter(camera);
                AssertRgb(positive, new Vector3(0.04f, 0.08f, 0.12f), "Compiled native opaque overdraw positive control");
                renderer.sharedMaterial = actualMaterial;

                var failures = new List<string>();
                foreach (var trueOverdraw in new[] { 0f, 1f })
                {
                    Shader.SetGlobalFloat("_TrueOverdrawOn", trueOverdraw);
                    var actual = ChromaBaseWaterRenderingParityTest.CaptureCenter(camera);
                    // Native D3D11 opaque and transparent lights both use TransparentOverdrawOn alone.
                    // WaterLit uses TrueOverdrawOn*OpaqueOverdrawOn. Mirror adds that term to its squared-intensity reflection.
                    var factor = preset is "OpaqueLight" or "TransparentLight"
                        ? 0.3f
                        : 0.2f * trueOverdraw;
                    var expected = new Vector3(overdrawColor.x, overdrawColor.y, overdrawColor.z) * factor;
                    if (preset == "WaterfallMirror")
                    {
                        var intensity = actualMaterial.GetFloat("_ReflectionIntensity");
                        expected += Vector3.one * (intensity * intensity);
                    }

                    var actualRgb = new Vector3(actual.r, actual.g, actual.b);
                    if (Vector3.Distance(actualRgb, expected) >= 0.003f)
                        failures.Add(preset + " trueOverdraw=" + trueOverdraw + " expected=" + expected + " actual=" + actualRgb);

                }

                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }

            finally
            {
                Camera.onPreRender -= publishNoise;
                renderer.sharedMaterial = actualMaterial;
                renderer.SetPropertyBlock(previousBlock);
                renderer.gameObject.layer = previousLayer;
                renderer.transform.position = previousPosition;
                renderer.transform.rotation = previousRotation;
                renderer.transform.localScale = previousScale;
                for (var index = 0; index < floatNames.Length; index++)
                    Shader.SetGlobalFloat(floatNames[index], previousFloats[index]);

                Shader.SetGlobalVector("_OverdrawColor", previousOverdrawColor);
                Shader.SetGlobalTexture("_GlobalBlueNoiseTex", previousNoise);
                for (var index = 0; index < globalKeywords.Length; index++)
                {
                    if (previousKeywords[index])
                        Shader.EnableKeyword(globalKeywords[index]);

                    else
                        Shader.DisableKeyword(globalKeywords[index]);

                }

                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(control);
                Object.DestroyImmediate(volumeControl);
                Object.DestroyImmediate(neutralNoise);
            }

        }

        private static void AssertRgb(Color actual, Vector3 expected, string message)
        {
            Assert.That(
                Vector3.Distance(new Vector3(actual.r, actual.g, actual.b), expected),
                Is.LessThan(0.003f),
                message);
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

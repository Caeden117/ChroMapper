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
    public class ChromaBaseWaterRenderingParityTest : TestBase
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
        public IEnumerator GeneratedBaseWaterUsesNativePackedReflectionResponse()
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
                                ["track"] = "base-water",
                                ["geometry"] = new JSONObject
                                {
                                    ["type"] = "Cube",
                                    ["material"] = new JSONObject { ["shader"] = "BaseWater", ["color"] = Color.white }
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
                .Single(candidate => candidate.EnvironmentEnhancement.Track == "base-water");
            var renderer = container.MpbController.Renderers.Single();
            var actualMaterial = renderer.sharedMaterial;
            var previousLayer = renderer.gameObject.layer;
            var previousPosition = renderer.transform.position;
            var previousRotation = renderer.transform.rotation;
            var previousScale = renderer.transform.localScale;
            var cameraObject = new GameObject("BaseWaterNativeResponseCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31;
            camera.allowHDR = true;

            var referenceShader = Shader.Find("ChroMapper/Water Lit");
            Assert.That(referenceShader, Is.Not.Null, "Missing compiled WaterLit reference shader");
            var referenceMaterial = new Material(referenceShader)
            {
                color = Color.white,
                shaderKeywords = new[]
                {
                    "FOG", "HEIGHT_FOG", "INVERT_RIMLIGHT", "MASK_RED_IS_ALPHA", "NOISE_DITHERING",
                    "NORMAL_MAP", "REFLECTION_PROBE", "REFLECTION_PROBE_BOX_PROJECTION",
                    "_DECALBLEND_ALPHABLEND", "_DISSOLVEAXIS_LOCALX", "_EMISSIONCOLORTYPE_FLAT",
                    "_EMISSIONTEXTURE_NONE", "_RIMLIGHT_NONE", "_ROTATE_UV_NONE", "_VERTEXMODE_NONE",
                    "_WHITEBOOSTTYPE_NONE", "_ZWRITE_ON"
                }
            };
            var probe = new Cubemap(1, TextureFormat.RGBAFloat, false);
            var emptyProbe = new Cubemap(1, TextureFormat.RGBAFloat, false);
            var noise = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            noise.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            noise.Apply();
            for (var face = 0; face < 6; face++)
                emptyProbe.SetPixels(new[] { Color.clear }, (CubemapFace)face);

            emptyProbe.Apply();

            var textureNames = new[] { "_ReflectionProbeTexture1", "_ReflectionProbeTexture2", "_GlobalBlueNoiseTex" };
            var floatNames = new[] { "_HasBakedReflectionProbeData", "_CustomFogHeightFogStartY", "_CustomFogHeightFogHeight" };
            var vectorNames = new[]
            {
                "_ReflectionProbePosition", "_ReflectionProbeBoundsMin", "_ReflectionProbeBoundsMax",
                "_LightProbeLightBakeIdA", "_LightProbeLightBakeIdB", "_LightProbeLightBakeIdC",
                "_LightProbeLightBakeIdD", "_LightProbeLightBakeIdE", "_LightProbeLightBakeIdF"
            };
            var previousTextures = textureNames.Select(Shader.GetGlobalTexture).ToArray();
            var previousFloats = floatNames.Select(Shader.GetGlobalFloat).ToArray();
            var previousVectors = vectorNames.Select(Shader.GetGlobalVector).ToArray();
            var globalKeywords = new[] { "BLOOM_FOG", "ENABLE_BLOOM_FOG", "ACES_TONE_MAPPING" };
            var previousKeywords = globalKeywords.Select(Shader.IsKeywordEnabled).ToArray();
            Camera.CameraCallback publishConstantNoise = renderingCamera =>
            {
                if (renderingCamera == camera)
                    Shader.SetGlobalTexture("_GlobalBlueNoiseTex", noise);

            };
            // The normal camera callback republishes the scene texture. Apply this deterministic input after that producer.
            Camera.onPreRender += publishConstantNoise;
            try
            {
                renderer.gameObject.layer = 31;
                renderer.transform.position = new Vector3(0f, 0f, 10f);
                renderer.transform.rotation = Quaternion.identity;
                renderer.transform.localScale = new Vector3(4f, 4f, 0.1f);
                foreach (var keyword in globalKeywords)
                    Shader.DisableKeyword(keyword);

                Shader.SetGlobalTexture(textureNames[0], probe);
                Shader.SetGlobalTexture(textureNames[1], emptyProbe);
                Shader.SetGlobalTexture(textureNames[2], noise);
                Shader.SetGlobalFloat(floatNames[0], 1f);
                Shader.SetGlobalFloat(floatNames[1], -100f);
                Shader.SetGlobalFloat(floatNames[2], 1f);
                Shader.SetGlobalVector(vectorNames[0], Vector4.zero);
                Shader.SetGlobalVector(vectorNames[1], new Vector4(-100f, -100f, -100f, 0f));
                Shader.SetGlobalVector(vectorNames[2], new Vector4(100f, 100f, 100f, 0f));
                Shader.SetGlobalVector(vectorNames[3], new Vector4(1f, 1f, 1f, 0f));
                for (var index = 4; index < vectorNames.Length; index++)
                    Shader.SetGlobalVector(vectorNames[index], Vector4.zero);

                var failures = new List<string>();
                foreach (var packedRed in new[] { 0.25f, 0.4f })
                {
                    for (var face = 0; face < 6; face++)
                        probe.SetPixels(new[] { new Color(packedRed, 0f, 0f, 0f) }, (CubemapFace)face);

                    probe.Apply();
                    renderer.sharedMaterial = referenceMaterial;
                    var reference = CaptureCenter(camera);
                    renderer.sharedMaterial = actualMaterial;
                    var actual = CaptureCenter(camera);
                    // The native 1.44.1 BaseWater DXBC decodes 2*packedRed and multiplies by
                    // 2*(0.8*metallic+0.2)*smoothness. Fresh shader defaults are metallic=0 and smoothness=0.5.
                    var expected = Vector3.one * (packedRed * 0.4f);
                    Assert.That(
                        Vector3.Distance(new Vector3(reference.r, reference.g, reference.b), expected),
                        Is.LessThan(0.003f),
                        "Compiled WaterLit control does not match the independent native response");
                    if (Vector3.Distance(new Vector3(actual.r, actual.g, actual.b), expected) >= 0.003f)
                        failures.Add("BaseWater packed=" + packedRed + " expected=" + expected + " actual=" + actual);

                }

                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }

            finally
            {
                Camera.onPreRender -= publishConstantNoise;
                renderer.sharedMaterial = actualMaterial;
                renderer.gameObject.layer = previousLayer;
                renderer.transform.position = previousPosition;
                renderer.transform.rotation = previousRotation;
                renderer.transform.localScale = previousScale;
                for (var index = 0; index < textureNames.Length; index++)
                    Shader.SetGlobalTexture(textureNames[index], previousTextures[index]);

                for (var index = 0; index < floatNames.Length; index++)
                    Shader.SetGlobalFloat(floatNames[index], previousFloats[index]);

                for (var index = 0; index < vectorNames.Length; index++)
                    Shader.SetGlobalVector(vectorNames[index], previousVectors[index]);

                for (var index = 0; index < globalKeywords.Length; index++)
                {
                    if (previousKeywords[index])
                        Shader.EnableKeyword(globalKeywords[index]);

                    else
                        Shader.DisableKeyword(globalKeywords[index]);

                }

                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(referenceMaterial);
                Object.DestroyImmediate(probe);
                Object.DestroyImmediate(emptyProbe);
                Object.DestroyImmediate(noise);
            }

        }

        internal static Color CaptureCenter(Camera camera)
        {
            var target = new RenderTexture(32, 32, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(16, 16, 1, 1), 0, 0);
                pixels.Apply();
                return pixels.GetPixel(0, 0);
            }

            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
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

using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class NativeEnvironmentLightmapParityTest : TestBase
    {
        private Scene loadedScene;

        private static JSONArray Samples => JSON.Parse(File.ReadAllText(PathUtils.Combine(
            Application.dataPath, "Tests", "Fixtures", "NativeEnvironmentLightmapSamples.json"))).AsArray;

        private static LightmapDataSO GetData(string environment)
        {
            return AssetDatabase.LoadAssetAtPath<LightmapDataSO>(PathUtils.Combine(
                "Assets", "_Graphics", "Textures", "Environments", "BakedLighting", environment + "LightmapData.asset"));
        }

        [Test]
        public void ImportedLightmapMipsMatchNativeGpuSamples()
        {
            var shader = Shader.Find("Hidden/Tests/NativeLightmapSample");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var row in Samples.Children)
                {
                    var environment = row["environment"].Value;
                    var data = GetData(environment);
                    Assert.That(data, Is.Not.Null, environment);
                    var textures = new[] { data.Lightmap1, data.Lightmap2 };
                    for (var index = 0; index < textures.Length; index++)
                    {
                        var texture = textures[index];
                        var native = row["textures"][index];
                        Assert.That(texture, Is.Not.Null, environment);
                        Assert.That(texture.width, Is.EqualTo(native["width"].AsInt), environment);
                        Assert.That(texture.height, Is.EqualTo(native["height"].AsInt), environment);
                        Assert.That(texture.mipmapCount, Is.EqualTo(native["mips"].AsInt), environment);
                        Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Trilinear), environment);
                        Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), environment);
                        Assert.That(texture.anisoLevel, Is.EqualTo(2), environment);
                        material.SetTexture("_Lightmap", texture);
                        foreach (var sample in native["samples"].Children)
                        {
                            material.SetVector("_Sample", new Vector4(sample["uv"][0].AsFloat,
                                sample["uv"][1].AsFloat, 0f, sample["mip"].AsInt));
                            Graphics.Blit(Texture2D.blackTexture, target, material);
                            RenderTexture.active = target;
                            image.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0);
                            image.Apply();
                            var actual = image.GetPixel(0, 0);
                            var label = $"{environment} lightmap {index + 1}, mip {sample["mip"].AsInt}, uv {sample["uv"]}";
                            Assert.That(actual.r, Is.EqualTo(sample["rgb"][0].AsFloat).Within(0.005f), label);
                            Assert.That(actual.g, Is.EqualTo(sample["rgb"][1].AsFloat).Within(0.005f), label);
                            Assert.That(actual.b, Is.EqualTo(sample["rgb"][2].AsFloat).Within(0.005f), label);
                        }
                    }
                }
            }
            finally
            {
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(material);
            }
        }

        [UnityTest]
        public IEnumerator ScenesPublishNativeTexturesAndRendererAtlasRectangles()
        {
            foreach (var row in Samples.Children)
            {
                var environment = row["environment"].Value;
                yield return SceneManager.LoadSceneAsync(environment, LoadSceneMode.Additive);
                loadedScene = SceneManager.GetSceneByName(environment);
                var roots = loadedScene.GetRootGameObjects();
                var loader = roots.SelectMany(root => root.GetComponentsInChildren<BakedLightDataLoader>(true)).Single();
                var data = GetData(environment);
                Assert.That(loader.LightmapData, Is.SameAs(data), environment);
                loader.SendDataToShaders();
                Assert.That(Shader.GetGlobalTexture("_LightMap1"), Is.SameAs(data.Lightmap1), environment);
                Assert.That(Shader.GetGlobalTexture("_LightMap2"), Is.SameAs(data.Lightmap2), environment);
                var objects = roots.SelectMany(root => root.GetComponentsInChildren<ChromaIDMarker>(true))
                    .ToDictionary(marker => marker.ChromaID, marker => marker.gameObject);
                foreach (var expected in row["renderers"].Children)
                {
                    if (!expected["inScene"].AsBool)
                    {
                        continue;
                    }
                    var id = expected["chromaId"].Value;
                    var renderer = objects[id].GetComponent<MeshRenderer>();
                    // Inactive authored geometry runs Awake when Chroma activates or clones it.
                    objects[id].SetActive(true);
                    CheckAtlas(renderer, expected);
                }
                yield return SceneManager.UnloadSceneAsync(loadedScene);
                loadedScene = default;
            }
        }

        [Test]
        public void RegenerationAndInactiveClonesRetainNativeAtlasRectangles()
        {
            var temporary = new GameObject("RegeneratedNativeAtlas");
            temporary.SetActive(false);
            var renderer = temporary.AddComponent<MeshRenderer>();
            try
            {
                foreach (var row in Samples.Children)
                {
                    var environment = row["environment"].Value;
                    EnvironmentBakedLightingAssets.BindLightmapData(temporary, environment);
                    Assert.That(temporary.GetComponent<BakedLightDataLoader>().LightmapData, Is.SameAs(GetData(environment)), environment);
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PathUtils.Combine(
                        "Assets", "__Scenes", "Environments", "Data", environment + ".json"));
                    var export = CreateUtils.JsonToEnvironmentData(asset);
                    var records = export.Objects.ToDictionary(item => item.ChromaID);
                    foreach (var expected in row["renderers"].Children)
                    {
                        var data = records[expected["chromaId"].Value].Components.MeshRenderer[0];
                        data.FillComponents(temporary, renderer, new CreateContainer());
                        CheckAtlas(renderer, expected);
                        var clone = Object.Instantiate(temporary);
                        try
                        {
                            var cloneRenderer = clone.GetComponent<MeshRenderer>();
                            cloneRenderer.lightmapIndex = -1;
                            cloneRenderer.lightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
                            clone.SetActive(true);
                            CheckAtlas(cloneRenderer, expected);
                        }
                        finally
                        {
                            Object.DestroyImmediate(clone);
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(temporary);
            }
        }

        private static void CheckAtlas(MeshRenderer renderer, JSONNode expected)
        {
            var label = expected["chromaId"].Value;
            Assert.That(renderer.lightmapIndex, Is.EqualTo(expected["index"].AsInt), label);
            var actual = renderer.lightmapScaleOffset;
            for (var index = 0; index < 4; index++)
            {
                Assert.That(actual[index], Is.EqualTo(expected["scaleOffset"][index].AsFloat).Within(0.000001f), label);
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreMapper()
        {
            if (loadedScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(loadedScene);
            }
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

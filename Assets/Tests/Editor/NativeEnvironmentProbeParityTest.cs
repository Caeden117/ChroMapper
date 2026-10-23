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
    public class NativeEnvironmentProbeParityTest : TestBase
    {
        private Scene loadedScene;
        private static readonly Vector3[] FaceDirections =
        {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
        };

        private static JSONArray Samples => JSON.Parse(File.ReadAllText(PathUtils.Combine(
            Application.dataPath, "Tests", "Fixtures", "NativeEnvironmentProbeSamples.json"))).AsArray;

        private static ReflectionProbeDataSO GetData(string environment)
        {
            return AssetDatabase.LoadAssetAtPath<ReflectionProbeDataSO>(PathUtils.Combine(
                "Assets", "_Graphics", "Textures", "Environments", "BakedLighting",
                environment + "ReflectionProbeData.asset"));
        }

        [Test]
        public void ImportedCubemapFacesAndMipsMatchShippedLighting()
        {
            var shader = Shader.Find("Hidden/Tests/NativeCubemapSample");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            try
            {
                foreach (var row in Samples.Children)
                {
                    var environment = row["environment"].Value;
                    var data = GetData(environment);
                    Assert.That(data, Is.Not.Null, environment);
                    var textures = new[] { data.ReflectionProbeCubemap1, data.ReflectionProbeCubemap2 };
                    for (var probeIndex = 0; probeIndex < 2; probeIndex++)
                    {
                        var expected = row["probes"][probeIndex];
                        var texture = textures[probeIndex];
                        Assert.That(texture, Is.Not.Null, environment);
                        Assert.That(texture.width, Is.EqualTo(expected["width"].AsInt), environment);
                        Assert.That(texture.mipmapCount, Is.EqualTo(expected["mips"].AsInt), environment);
                        material.SetTexture("_Probe", texture);
                        for (var face = 0; face < 6; face++)
                        {
                            material.SetVector("_Direction", FaceDirections[face]);
                            for (var mip = 0; mip < texture.mipmapCount; mip++)
                            {
                                material.SetFloat("_Mip", mip);
                                Graphics.Blit(Texture2D.blackTexture, target, material);
                                RenderTexture.active = target;
                                image.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                                image.Apply();
                                var actual = image.GetPixel(0, 0);
                                var native = expected["samples"][face][mip];
                                var label = $"{environment} probe {probeIndex + 1} face {face} mip {mip}";
                                Assert.That(actual.r, Is.EqualTo(native[0].AsFloat).Within(0.005f), label);
                                Assert.That(actual.g, Is.EqualTo(native[1].AsFloat).Within(0.005f), label);
                                Assert.That(actual.b, Is.EqualTo(native[2].AsFloat).Within(0.005f), label);
                            }
                        }
                    }
                }
            }
            finally
            {
                RenderTexture.active = previousTarget;
                target.Release();
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }

        [UnityTest]
        public IEnumerator EveryBakedEnvironmentSceneBindsItsOwnNativePair()
        {
            foreach (var row in Samples.Children)
            {
                var environment = row["environment"].Value;
                yield return SceneManager.LoadSceneAsync(environment, LoadSceneMode.Additive);
                loadedScene = SceneManager.GetSceneByName(environment);
                var probes = loadedScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<BakedReflectionProbe>(true)).ToArray();
                Assert.That(probes, Has.Length.EqualTo(1), environment);
                var data = GetData(environment);
                Assert.That(probes[0].ReflectionProbeData, Is.Not.Null, environment);
                Assert.That(probes[0].ReflectionProbeData.ReflectionProbeCubemap1,
                    Is.SameAs(data.ReflectionProbeCubemap1), environment);
                Assert.That(probes[0].ReflectionProbeData.ReflectionProbeCubemap2,
                    Is.SameAs(data.ReflectionProbeCubemap2), environment);
                probes[0].SendDataToShaders();
                Assert.That(Shader.GetGlobalFloat("_HasBakedReflectionProbeData"), Is.EqualTo(1f), environment);
                Assert.That(Shader.GetGlobalTexture("_ReflectionProbeTexture1"), Is.SameAs(data.ReflectionProbeCubemap1), environment);
                Assert.That(Shader.GetGlobalTexture("_ReflectionProbeTexture2"), Is.SameAs(data.ReflectionProbeCubemap2), environment);
                yield return SceneManager.UnloadSceneAsync(loadedScene);
                loadedScene = default;
            }
        }

        [Test]
        public void SceneRegenerationRestoresNativeProbeAssets()
        {
            var temporary = new GameObject("RegeneratedProbe");
            try
            {
                var probe = temporary.AddComponent<BakedReflectionProbe>();
                foreach (var row in Samples.Children)
                {
                    var environment = row["environment"].Value;
                    var container = new CreateContainer
                    {
                        Data = new EnvironmentData { Data = new EnvironmentDataInfo { ID = environment } }
                    };
                    var componentData = new BakedReflectionProbeData { Size = Vector3.one * 10f };
                    componentData.FillComponents(temporary, probe, container);
                    Assert.That(probe.ReflectionProbeData, Is.SameAs(GetData(environment)), environment);
                }
            }
            finally
            {
                Object.DestroyImmediate(temporary);
            }
        }

        [UnityTearDown]
        public IEnumerator UnloadEnvironment()
        {
            if (loadedScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(loadedScene);
            }
            // Loading an environment assigns shared lightshow references. A fresh mapper scene releases those references before the next test seeks.
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

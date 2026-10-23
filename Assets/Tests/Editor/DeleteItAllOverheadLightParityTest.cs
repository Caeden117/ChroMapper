using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // DELETE IT ALL (4cf62, Mawntee) places reflective NeonTop glass above the player at 405.484.
    // Full and reduced fixtures reproduced the missing layer before binding the native probe pair.
    public class DeleteItAllOverheadLightParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChromaLite;
        private bool previousClockEnabled;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private AudioTimeSyncController clock;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Requires the ignored original DELETE IT ALL difficulty.")]
        public IEnumerator FullMapOverheadGlassRetainsItsReflectionAt405484()
        {
            yield return CheckGlass(PathUtils.Combine(Application.dataPath, "..", "TestResults", "DeleteItAll", "FullMap.json"));
        }

        [UnityTest]
        public IEnumerator ReducedOverheadGlassRetainsItsReflectionAt405484()
        {
            yield return CheckGlass(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "DeleteItAllOverheadLightFixture.json"));
        }

        private IEnumerator CheckGlass(string path)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 18,
                NoteStartBeatOffset = -0.5f,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(path)), beatsPerMinute: 155,
                environmentName: "InterscopeEnvironment", songLengthSeconds: 300, forceSceneReload: true,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(405.484f);
            yield return null;
            yield return null;
            var probe = Object.FindAnyObjectByType<BakedReflectionProbe>();
            var lights = Object.FindObjectsByType<ParametricBoxLight>(FindObjectsSortMode.None)
                .Where(light => light.Renderer.sharedMaterial.name == "EnvLightGlass").ToArray();
            var target = lights.Single(light => light.GetComponent<ChromaIDMarker>().ChromaID
                == "InterscopeEnvironment.[0]Environment.[50]NeonTop (4).[0]BoxLight");
            Assert.That(target.Renderer.sharedMaterial.IsKeywordEnabled("REFLECTION_PROBE"), Is.True);
            foreach (var beat in new[] { 405.482f, 405.484f, 405.5f, 405.484f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                CompareNativeReflection(target.Renderer, probe);
            }
            Assert.That(probe.ReflectionProbeData, Is.Not.Null,
                "Interscope glass requests REFLECTION_PROBE but its packed lighting data is absent, leaving only the emissive rectangle.");
            Assert.That(probe.ReflectionProbeData.ReflectionProbeCubemap1, Is.Not.Null);
            Assert.That(probe.ReflectionProbeData.ReflectionProbeCubemap2, Is.Not.Null);
            Assert.That(Shader.GetGlobalFloat("_HasBakedReflectionProbeData"), Is.EqualTo(1f));
        }

        private static void CompareNativeReflection(Renderer renderer, BakedReflectionProbe probe)
        {
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "DeleteItAll");
            Directory.CreateDirectory(directory);
            var reference = AssetDatabase.LoadAssetAtPath<ReflectionProbeDataSO>(PathUtils.Combine(
                "Assets", "_Graphics", "Textures", "Environments", "BakedLighting",
                "InterscopeEnvironmentReflectionProbeData.asset"));
            Assert.That(reference, Is.Not.Null);
            Assert.That(reference.ReflectionProbeCubemap1, Is.Not.Null);
            Assert.That(reference.ReflectionProbeCubemap2, Is.Not.Null);
            var previousData = probe.ReflectionProbeData;
            var previousLayer = renderer.gameObject.layer;
            var cameraObject = new GameObject("OverheadGlassResponseCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = renderer.bounds.center + new Vector3(4f, -4f, -10f);
            camera.transform.LookAt(renderer.bounds.center);
            var run = System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
            try
            {
                renderer.gameObject.layer = 31;
                var actual = Capture(camera, PathUtils.Combine(directory, run + "-actual.png"));
                probe.ReflectionProbeData = reference;
                probe.SendDataToShaders();
                var expected = Capture(camera, PathUtils.Combine(directory, run + "-native.png"));
                var total = 0f;
                var changed = 0;
                for (var i = 0; i < actual.Length; i++)
                {
                    var difference = Mathf.Abs(actual[i].r - expected[i].r)
                        + Mathf.Abs(actual[i].g - expected[i].g) + Mathf.Abs(actual[i].b - expected[i].b);
                    total += difference;
                    if (difference > 0.03f)
                    {
                        changed++;
                    }
                }
                Assert.That(total / (actual.Length * 3), Is.LessThan(0.0001f),
                    $"The source light loses its reflective glass surface compared with the shipped Interscope probe inputs ({changed} changed pixels).");
            }
            finally
            {
                renderer.gameObject.layer = previousLayer;
                probe.ReflectionProbeData = previousData;
                probe.SendDataToShaders();
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static Color[] Capture(Camera camera, string path)
        {
            var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGBFloat);
            var image = new Texture2D(256, 256, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                return image.GetPixels();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }
            if (clock != null)
            {
                clock.enabled = previousClockEnabled;
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChromaLite;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

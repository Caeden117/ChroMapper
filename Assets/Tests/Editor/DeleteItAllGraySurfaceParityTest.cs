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
    public class DeleteItAllGraySurfaceParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChromaLite;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode uiMode;
        private CameraManager cameraManager;

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
        [Explicit("Requires the local original DELETE IT ALL map.")]
        public IEnumerator FullMapGrayLightSurroundsReceiveShippedBakedLighting()
        {
            yield return CheckMap(PathUtils.Combine(Application.dataPath, "..", "TestResults", "DeleteItAll", "FullMap.json"));
        }

        [UnityTest]
        public IEnumerator ReducedGrayLightSurroundsReceiveShippedBakedLighting()
        {
            yield return CheckMap(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "DeleteItAllGraySurfaceFixture.json"));
        }

        private IEnumerator CheckMap(string path)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus", NoteJumpSpeed = 18, NoteStartBeatOffset = -0.5f,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(path)), beatsPerMinute: 155,
                environmentName: "InterscopeEnvironment", songLengthSeconds: 300, forceSceneReload: true,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(405.484f);
            yield return null;
            yield return null;
            CheckSurroundAtlasCoordinates(!path.EndsWith("FullMap.json"));
        }

        private void CheckSurroundAtlasCoordinates(bool reduced)
        {
            var root = PathUtils.Combine(Application.dataPath, "..", "TestResults", "DeleteItAll");
            Directory.CreateDirectory(root);
            var nativeData = AssetDatabase.LoadAssetAtPath<LightmapDataSO>(PathUtils.Combine(
                "Assets", "_Graphics", "Textures", "Environments", "BakedLighting", "InterscopeEnvironmentLightmapData.asset"));
            Assert.That(Shader.GetGlobalTexture("_LightMap1"), Is.SameAs(nativeData.Lightmap1));
            Assert.That(Shader.GetGlobalTexture("_LightMap2"), Is.SameAs(nativeData.Lightmap2));
            var camera = cameraManager.CameraControllers[1].Camera;
            var previousRotation = camera.transform.rotation;
            var previousFov = camera.fieldOfView;
            var suffixes = new[] { "[17]Pillar (3).[0]PillarL", "[17]Pillar (3).[1]PillarR" };
            var nativeOffsets = new[]
            {
                new Vector4(0.109779514f, 0.109779514f, 0.879595637f, -0.002144131f),
                new Vector4(0.182965860f, 0.182965860f, 0.711270213f, -0.003573552f)
            };
            var renderers = suffixes.Select(suffix => Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Single(value => value.GetComponent<ChromaIDMarker>() != null
                    && value.GetComponent<ChromaIDMarker>().ChromaID.EndsWith(suffix))).ToArray();
            var originalBlocks = renderers.Select(_ => new MaterialPropertyBlock()).ToArray();
            var previousOrthographic = camera.orthographic;
            var previousSize = camera.orthographicSize;
            var previousMask = camera.cullingMask;
            var previousLayers = renderers.Select(value => value.gameObject.layer).ToArray();
            var previousClearFlags = camera.clearFlags;
            var previousBackground = camera.backgroundColor;
            var originalPosition = camera.transform.position;
            if (reduced)
            {
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                foreach (var renderer in renderers)
                {
                    renderer.gameObject.layer = 31;
                }
                camera.orthographic = true;
                camera.orthographicSize = renderers[0].bounds.extents.magnitude;
                camera.transform.position = renderers[0].bounds.center + Vector3.left * renderers[0].bounds.extents.magnitude * 3f;
            }
            var run = System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + (reduced ? "-reduced" : "-full");
            try
            {
                camera.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                camera.fieldOfView = 90f;
                var actual = Capture(camera, PathUtils.Combine(root, run + "-surround-actual.png"));
                for (var index = 0; index < renderers.Length; index++)
                {
                    renderers[index].GetPropertyBlock(originalBlocks[index]);
                    var block = new MaterialPropertyBlock();
                    renderers[index].GetPropertyBlock(block);
                    block.SetVector("unity_LightmapST", nativeOffsets[index]);
                    renderers[index].SetPropertyBlock(block);
                }
                var expected = Capture(camera, PathUtils.Combine(root, run + "-surround-native.png"));
                var difference = 0f;
                var changed = 0;
                for (var y = 0; y < 576; y++)
                {
                    for (var x = 0; x < 1024; x++)
                    {
                        var index = y * 1024 + x;
                        var delta = Mathf.Abs(actual[index].r - expected[index].r)
                            + Mathf.Abs(actual[index].g - expected[index].g)
                            + Mathf.Abs(actual[index].b - expected[index].b);
                        difference += delta;
                        if (delta > 0.03f)
                        {
                            changed++;
                        }
                    }
                }
                Assert.That(changed, Is.LessThanOrEqualTo(4), "The gray light surrounds differ from the native atlas control.");
                Assert.That(difference / (576f * 1024f * 3f), Is.LessThan(0.00001f),
                    "The gray frames around the two yellow lights must sample their native atlas rectangles.");
            }
            finally
            {
                camera.transform.rotation = previousRotation;
                camera.fieldOfView = previousFov;
                camera.orthographic = previousOrthographic;
                camera.orthographicSize = previousSize;
                camera.cullingMask = previousMask;
                camera.clearFlags = previousClearFlags;
                camera.backgroundColor = previousBackground;
                camera.transform.position = originalPosition;
                for (var index = 0; index < renderers.Length; index++)
                {
                    renderers[index].SetPropertyBlock(originalBlocks[index]);
                    renderers[index].gameObject.layer = previousLayers[index];
                }
            }
        }

        private static Color[] Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1024, 576, 24, RenderTextureFormat.ARGBFloat);
            var image = new Texture2D(1024, 576, TextureFormat.RGBAFloat, false, true);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                image.Apply();
                var pixels = image.GetPixels();
                var displayPixels = new Color[pixels.Length];
                for (var index = 0; index < pixels.Length; index++)
                {
                    displayPixels[index] = pixels[index].gamma;
                }
                var display = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                try
                {
                    display.SetPixels(displayPixels);
                    display.Apply();
                    File.WriteAllBytes(path, display.EncodeToPNG());
                }
                finally
                {
                    Object.DestroyImmediate(display);
                }
                return pixels;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
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


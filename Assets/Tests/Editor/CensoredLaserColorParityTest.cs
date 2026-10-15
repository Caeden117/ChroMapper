using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "CENSORED!!", mapped by Saltyfish (BeatSaver ID: 4b3da).
    public class CensoredLaserColorParityTest : TestBase
    {
        private const float NativeNormalAlpha = 0.7490196f;
        private bool previousAnimations;
        private float previousFov;
        private float previousOffset;
        private bool previousBloom;
        private UIMode uiMode;
        private CameraManager cameraManager;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            previousBloom = Settings.Instance.Bloom;
            yield break;
        }

        [UnityTest]
        public IEnumerator FullMapSteadyLasersUseNativeAlpha()
        {
            yield return CheckColor("CensoredFullMapFixture.json", true);
        }

        [UnityTest]
        public IEnumerator ReducedSteadyLasersUseNativeAlpha()
        {
            yield return CheckColor("CensoredLaserColorFixture.json", false);
        }

        [UnityTest]
        public IEnumerator TubeEventsKeepNativeEndpointsAndBoostAcrossReverseSeeks()
        {
            var events = new JSONArray();
            foreach (var (beat, value) in new (float Beat, int Value)[]
            {
                (0, 1), (4, 2), (8, 3), (12, 0), (16, 9), (21, 1), (24, 2), (28, 9)
            })
            {
                var evt = new JSONObject { ["b"] = beat, ["et"] = 2, ["i"] = value, ["f"] = 2 };
                if (value != 9)
                    evt["customData"] = new JSONObject { ["color"] = JSON.Parse("[0.556,0.641,1,0.5]") };

                events.Add(evt);
            }

            var boosts = new JSONArray();
            boosts.Add(new JSONObject { ["b"] = 20, ["o"] = true });
            var data = new JSONObject
            {
                ["version"] = "3.3.0", ["basicBeatmapEvents"] = events,
                ["colorBoostBeatmapEvents"] = boosts
            };
            yield return TestUtils.ReloadMap(3, data, beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment", forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            var lasers = Object.FindObjectsByType<ParametricBloomFogLightController>(FindObjectsSortMode.None)
                .Where(light => light.Type == 2).ToArray();
            Assert.That(lasers.Length, Is.EqualTo(8));
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var flashEase = 1f - Mathf.Pow(1f - 0.5f / 1.95f, 3f);
            foreach (var (beat, alpha) in new (float Beat, float Alpha)[]
            {
                (1, NativeNormalAlpha), (4, 1), (4.5f, Mathf.Lerp(1, NativeNormalAlpha, flashEase)),
                (6, NativeNormalAlpha), (8, 1), (9, Mathf.Pow(2, -10f / 4.875f)), (12, 0),
                (16, 2), (21, 0.8f), (24, 1), (24.5f, Mathf.Lerp(1, 0.8f, flashEase)),
                (26, 0.8f), (28, 2), (1, NativeNormalAlpha), (4.5f, Mathf.Lerp(1, NativeNormalAlpha, flashEase))
            })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                foreach (var light in lasers)
                {
                    Assert.That(light.Color.a, Is.EqualTo(alpha).Within(0.0001f),
                        $"Tube event at beat {beat} has the wrong native ColorSO endpoint.");
                    var block = new MaterialPropertyBlock();
                    light.BoxLight.Renderer.GetPropertyBlock(block);
                    Assert.That(block.GetColor("_Color").a,
                        Is.EqualTo(alpha * light.ColorAlphaMultiplier).Within(0.0001f));
                }
            }
        }

        private IEnumerator CheckColor(string fixture, bool capture)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90;
            Settings.Instance.PlayerCameraOffsetZ = 0;
            Settings.Instance.Bloom = true;
            var data = JSON.Parse(File.ReadAllText(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", fixture)));
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus", NoteJumpSpeed = 16, NoteStartBeatOffset = 0,
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(3, data, beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment", songLengthSeconds: 110, forceSceneReload: true,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(164.031f);
            yield return null;
            yield return null;

            var lasers = Object.FindObjectsByType<ParametricBloomFogLightController>(FindObjectsSortMode.None)
                .Where(light => light.Type == 2 || light.Type == 3).ToArray();
            Assert.That(lasers.Length, Is.EqualTo(16));
            var actualColors = lasers.Select(light => light.Color).ToArray();
            if (capture)
            {
                var camera = cameraManager.SelectedCameraController.Camera;
                var actualPixels = Capture(camera, "actual");
                Color32[] normalizedPixels;
                try
                {
                    // Recreate the old normalized input while keeping camera, map, materials, and bloom fixed.
                    foreach (var laser in lasers)
                    {
                        laser.SetColor(new Color(0.556f, 0.641f, 1f, 1f));
                    }

                    normalizedPixels = Capture(camera, "normalized-input");
                }
                finally
                {
                    for (var i = 0; i < lasers.Length; i++)
                    {
                        lasers[i].SetColor(actualColors[i]);
                    }
                }

                var actualSaturation = 0f;
                var normalizedSaturation = 0f;
                var samples = 0;
                var whiteBodyPixels = 0;
                for (var i = 0; i < actualPixels.Length; i++)
                {
                    var actual = actualPixels[i];
                    var normalized = normalizedPixels[i];
                    var x = i % 1024;
                    if ((x < 450 || x > 574) && actual.r > 245 && actual.g > 245 && actual.b > 245)
                        whiteBodyPixels++;

                    if (actual.b < 80 || normalized.b < 80 || normalized.r - actual.r < 8)
                        continue;

                    actualSaturation += (actual.b - actual.r) / (float)actual.b;
                    normalizedSaturation += (normalized.b - normalized.r) / (float)normalized.b;
                    samples++;
                }

                Debug.Log($"[CensoredLaserColor] changed blue pixels={samples} " +
                    $"native saturation={actualSaturation / Mathf.Max(samples, 1)} " +
                    $"normalized saturation={normalizedSaturation / Mathf.Max(samples, 1)} " +
                    $"white body pixels={whiteBodyPixels} " +
                    $"baked glow controllers={lasers.Count(light => light.SpriteLight != null)}");
                Assert.That(whiteBodyPixels, Is.GreaterThan(1000), "Nearby laser bodies must retain white cores.");
                Assert.That(samples, Is.GreaterThan(100), "The alpha correction must affect rendered laser pixels.");
                Assert.That(actualSaturation / samples, Is.GreaterThan(normalizedSaturation / samples + 0.01f),
                    "Native alpha must preserve more blue in the far bodies and bloom than normalized alpha.");
            }

            for (var i = 0; i < lasers.Length; i++)
            {
                var light = lasers[i];
                Assert.That(actualColors[i].r, Is.EqualTo(0.556f).Within(0.0001f));
                Assert.That(actualColors[i].g, Is.EqualTo(0.641f).Within(0.0001f));
                Assert.That(actualColors[i].b, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(actualColors[i].a, Is.EqualTo(NativeNormalAlpha).Within(0.0001f),
                    $"{fixture}: {light.name} sends normalized brightness into the native parametric shader.");
                var block = new MaterialPropertyBlock();
                light.BoxLight.Renderer.GetPropertyBlock(block);
                Assert.That(block.GetColor("_Color").a,
                    Is.EqualTo(NativeNormalAlpha * light.ColorAlphaMultiplier).Within(0.0001f));
            }

            atsc.MoveToJsonTime(147.906f);
            yield return null;
            atsc.MoveToJsonTime(164.031f);
            yield return null;
            Assert.That(lasers[0].Color.a, Is.EqualTo(NativeNormalAlpha).Within(0.0001f));
        }

        private static Color32[] Capture(Camera camera, string suffix)
        {
            var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;
            var target = new RenderTexture(1024, 576, 24, format);
            var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "CensoredLaserColorParityTest");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(PathUtils.Combine(directory, $"full-map-164.031-{suffix}.png"), texture.EncodeToPNG());
                return texture.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        [UnityTearDown]
        public IEnumerator RestorePreview()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            Settings.Instance.Bloom = previousBloom;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
        }
    }
}

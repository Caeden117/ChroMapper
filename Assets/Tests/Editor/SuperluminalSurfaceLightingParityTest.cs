using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Superluminal by modus, mapped by nasafrasa (358a7), uses four enhanced Gaga directional lights.
    public class SuperluminalSurfaceLightingParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChromaLite;
        private float previousFov;
        private float previousOffset;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private AudioTimeSyncController playbackClock;
        private bool previousClockEnabled;

        protected override void CleanupTestObjects()
        {
        }

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        [UnityTest]
        [Explicit("Local full-map reproduction requires the original Superluminal difficulty in TestResults/Superluminal.")]
        public IEnumerator FullMapBeat132DirectionalLightsIlluminateTunnel()
        {
            var fixturePath = PathUtils.Combine(
                Application.dataPath,
                "..",
                "TestResults",
                "Superluminal",
                "SuperluminalFullMapFixture.json");
            if (!File.Exists(fixturePath))
                Assert.Ignore($"Local full-map reproduction requires the ignored fixture at {fixturePath}.");

            yield return CheckLighting("SuperluminalFullMapFixture.json", true);
        }

        [UnityTest]
        public IEnumerator ReducedDirectionalLightsIlluminateSurfacesAtReportedCuts()
        {
            yield return CheckLighting("SuperluminalSurfaceLightingFixture.json", false);
        }

        private IEnumerator CheckLighting(string fixture, bool fullMap)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.PlayerCameraFOV = 90;
            Settings.Instance.PlayerCameraOffsetZ = 0;
            var path = fullMap
                ? PathUtils.Combine(Application.dataPath, "..", "TestResults", "Superluminal", fixture)
                : PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", fixture);
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 16,
                NoteStartBeatOffset = 0,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                CustomColorLeft = new Color(0.6039216f, 0.254902f, 0.9960784f),
                CustomColorRight = new Color(0.1294118f, 0.2156863f, 0.827451f),
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(path)),
                beatsPerMinute: 150,
                environmentName: "GagaEnvironment",
                songLengthSeconds: 190,
                forceSceneReload: true,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            playbackClock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = playbackClock.enabled;
            playbackClock.enabled = false;
            playbackClock.SongAudioSource.Stop();
            var lights = Object.FindObjectsByType<DirectionalLightsController>(FindObjectsSortMode.None)
                .Where(light => light.name.StartsWith("DirectionalLightFront")).ToArray();
            Assert.That(lights.Length, Is.EqualTo(4), "The full source map must retain its four authored directional lights.");
            var trackLights = new Dictionary<string, DirectionalLightsController>();
            var containers = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None);
            foreach (var track in new[] { "DirectionalLight2Track", "DirectionalLight3Track", "DirectionalLight4Track" })
            {
                var container = containers.Single(item => item.EnvironmentEnhancement?.Track == track);
                var target = container.GetComponents<ObjectAnimator>()
                    .Select(animator => animator.LocalTarget)
                    .Single(item => item != null && item.GetComponent<DirectionalLightsController>() != null);
                trackLights.Add(track, target.GetComponent<DirectionalLightsController>());
            }

            var failures = new List<string>();
            var renderedCuts = new HashSet<float>();
            var sampleIndex = 0;
            foreach (var beat in new[]
            {
                132f, 34.937f, 76.5f, 108f,
                34.499f, 34.5f, 35.4f, 35.401f,
                76.499f, 76.501f, 107.999f, 108.001f,
                131.999f, 132.001f, 108f, 34.937f, 132f
            })
            {
                playbackClock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                yield return null;
                var expectedInputs = ExpectedInputs(beat);
                if (expectedInputs != null)
                {
                    foreach (var pair in expectedInputs)
                    {
                        var light = trackLights[pair.Key];
                        Assert.That(
                            light.LightIntensityData[0].Color,
                            Is.EqualTo(pair.Value).Within(0.0001f),
                            $"{pair.Key} must receive the authored beat-{beat} color before testing surface output.");
                    }

                }

                foreach (var light in lights)
                {
                    var received = light.LightIntensityData[0].Color;
                    var expected = NativeMixedColor(light);
                    var actual = light.Light.Color;
                    var actualRgb = new Vector3(actual.r, actual.g, actual.b);
                    var expectedRgb = new Vector3(expected.r, expected.g, expected.b);
                    if (Vector3.Distance(actualRgb, expectedRgb) > 0.001f)
                        failures.Add($"beat {beat}: {light.name} received {received} but its directional light stayed {actual}, expected {expected}");

                }

                var captureName = fixture + $"-{sampleIndex++:D2}-{beat}";
                var pixels = Capture(cameraManager.SelectedCameraController.Camera, captureName + "-actual");
                if (fullMap && expectedInputs != null && renderedCuts.Add(beat))
                {
                    var clones = trackLights.Values.ToArray();
                    var intensities = clones.Select(light => light.Light.Intensity).ToArray();
                    try
                    {
                        foreach (var light in clones)
                            light.Light.Intensity = 0;

                        yield return null;
                        yield return null;
                        var unlit = Capture(cameraManager.SelectedCameraController.Camera, captureName + "-clones-disabled");
                        var changedPixels = CountChangedPixels(pixels, unlit);
                        if (changedPixels < 1000)
                            failures.Add($"beat {beat}: enhanced directional lights affected only {changedPixels} rendered pixels");

                    }

                    finally
                    {
                        for (var i = 0; i < clones.Length; i++)
                            clones[i].Light.Intensity = intensities[i];

                    }

                }

            }

            playbackClock.MoveToJsonTime(132.062f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(playbackClock, 132.062f);
            yield return null;
            yield return null;
            yield return null;
            foreach (var light in lights)
                Assert.That(
                    light.Light.Color,
                    Is.EqualTo(NativeMixedColor(light)).Within(0.001f),
                    "The cloned mixers must keep refreshing during deterministic playback without native audio.");

            Assert.That(failures, Is.Empty, "Enhanced directional light colors must reach tunnel surfaces:\n" + string.Join("\n", failures));
        }

        private static Dictionary<string, Color> ExpectedInputs(float beat)
        {
            const float nativeNormalAlpha = 0.7490196f;
            var blue = new Color(0.209f, 0.086f, 1, 0.576f * nativeNormalAlpha);
            return beat switch
            {
                132 => new Dictionary<string, Color>
                {
                    ["DirectionalLight2Track"] = Color.white,
                    ["DirectionalLight3Track"] = new Color(0.209f, 0.086f, 1, 1),
                    ["DirectionalLight4Track"] = new Color(1, 0.082f, 0.529f, 1)
                },
                34.937f => new Dictionary<string, Color>
                {
                    ["DirectionalLight2Track"] = new Color(0.5f, 0, 1, 0.438f * nativeNormalAlpha),
                    ["DirectionalLight3Track"] = new Color(0, 0, 1, 0.438f * nativeNormalAlpha),
                    ["DirectionalLight4Track"] = new Color(1, 1, 1, 0.438f * nativeNormalAlpha)
                },
                76.5f => new Dictionary<string, Color>
                {
                    ["DirectionalLight2Track"] = blue,
                    ["DirectionalLight3Track"] = blue,
                    ["DirectionalLight4Track"] = blue
                },
                108 => new Dictionary<string, Color>
                {
                    ["DirectionalLight2Track"] = blue,
                    ["DirectionalLight3Track"] = Color.white,
                    ["DirectionalLight4Track"] = Color.white
                },
                _ => null
            };
        }

        internal static Color NativeMixedColor(DirectionalLightsController light)
        {
            // Gaga's shipped DirectionalLightWithIds has these five weights and Maximum mixing.
            var weights = new[] { 0.3f, 0.3f, 0f, 0f, 1f };
            Assert.That(light.LightIntensityData.Length, Is.EqualTo(weights.Length));
            Assert.That(light.Intensity, Is.EqualTo(1));
            Assert.That(light.MaxIntensity, Is.EqualTo(1));
            Assert.That(light.MultiplyColorByAlpha, Is.True);
            Assert.That(light.MixType, Is.EqualTo(ColorMixAndWeightingApproach.Maximum));
            var mixed = Color.clear;
            for (var i = 0; i < weights.Length; i++)
            {
                Assert.That(light.LightIntensityData[i].Intensity, Is.EqualTo(weights[i]));
                var input = light.LightIntensityData[i].Color;
                var factor = Mathf.Sqrt(input.a * weights[i]);
                mixed.r = Mathf.Max(mixed.r, input.r * factor);
                mixed.g = Mathf.Max(mixed.g, input.g * factor);
                mixed.b = Mathf.Max(mixed.b, input.b * factor);
                mixed.a = Mathf.Max(mixed.a, factor);
            }

            if (mixed.grayscale > 1)
                mixed /= mixed.grayscale;

            return mixed;
        }

        private static int CountChangedPixels(Color32[] lit, Color32[] unlit)
        {
            var changed = 0;
            for (var i = 0; i < lit.Length; i++)
            {
                if (Mathf.Abs(lit[i].r - unlit[i].r) > 16
                    || Mathf.Abs(lit[i].g - unlit[i].g) > 16
                    || Mathf.Abs(lit[i].b - unlit[i].b) > 16)
                {
                    changed++;
                }

            }

            return changed;
        }

        private static Color32[] Capture(Camera camera, string name)
        {
            var target = new RenderTexture(
                1024,
                576,
                24,
                camera.allowHDR
                    ? RenderTextureFormat.DefaultHDR
                    : RenderTextureFormat.ARGB32);
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
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "Superluminal", "SurfaceLighting");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(PathUtils.Combine(directory, name + ".png"), texture.EncodeToPNG());
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
            if (playbackClock != null)
            {
                if (playbackClock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(playbackClock);

                playbackClock.enabled = previousClockEnabled;
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChromaLite;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" }, forceSceneReload: true);
        }

    }
}

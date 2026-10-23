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
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Superluminal's rocket activates at184 with 109 ship2 cubes and five cyl2 cylinders using InterscopeCar.
    public class SuperluminalRocketMaterialParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChromaLite;
        private float previousFov;
        private float previousOffset;
        private bool previousClockEnabled;
        private bool clockCaptured;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private AudioTimeSyncController clock;
        private string captureRun;
        private static readonly int colorId = Shader.PropertyToID("_Color");

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            clockCaptured = false;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Local rocket reproduction requires the complete Superluminal difficulty.")]
        public IEnumerator FullMapRocketUsesNativeCarReflectionBeforeAndDuringStrobes()
        {
            var path = PathUtils.Combine(
                Application.dataPath,
                "..",
                "TestResults",
                "Superluminal",
                "SuperluminalFullMapFixture.json");
            if (!File.Exists(path))
                Assert.Ignore($"Local full-map reproduction requires the ignored fixture at {path}.");

            yield return CheckRocket(path, true);
        }

        [UnityTest]
        public IEnumerator ReducedRocketUsesNativeCarReflectionBeforeAndDuringStrobes()
        {
            var path = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "SuperluminalRocketMaterialFixture.json");
            yield return CheckRocket(path, false);
        }

        private IEnumerator CheckRocket(string path, bool fullMap)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            captureRun = (fullMap
                ? "full-"
                : "reduced-")
                + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
            var data = JSON.Parse(File.ReadAllText(path));
            var sourceTracks = data["customData"]["environment"].Children
                .Select(entry => entry["track"].Value)
                .Where(
                    track => track.StartsWith("modelScene0_ship2_")
                    || track.StartsWith("modelScene0_cyl2_")).ToArray();
            Assert.That(
                sourceTracks.Length,
                Is.EqualTo(
                    fullMap
                        ? 114
                        : 2));
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
                data,
                beatsPerMinute: 150,
                environmentName: "GagaEnvironment",
                songLengthSeconds: 270,
                forceSceneReload: true,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clockCaptured = true;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);

            var nativeCar = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Graphics/Materials/Environments/Interscope/Car.mat");
            AssertNativeCar(nativeCar);
            var probe = Object.FindAnyObjectByType<BakedReflectionProbe>();
            Assert.That(probe, Is.Not.Null);
            Assert.That(probe.ReflectionProbeData, Is.Not.Null);
            Assert.That(probe.ReflectionProbeData.ReflectionProbeCubemap1, Is.Not.Null);
            Assert.That(probe.ReflectionProbeData.ReflectionProbeCubemap2, Is.Not.Null);
            Assert.That(Shader.GetGlobalFloat("_HasBakedReflectionProbeData"), Is.EqualTo(1f));

            var containers = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None);
            var rocket = sourceTracks.Select(
                track => containers.Single(
                    container =>
                container.EnvironmentEnhancement.Track == track)).ToArray();
            var renderers = rocket.SelectMany(container => container.MpbController.Renderers).ToArray();
            Assert.That(renderers.Length, Is.EqualTo(sourceTracks.Length));
            var lights = Object.FindObjectsByType<DirectionalLightsController>(FindObjectsSortMode.None)
                .Where(light => light.name.StartsWith("DirectionalLightFront")).ToArray();
            Assert.That(lights.Length, Is.EqualTo(4));
            var failures = new List<string>();
            var records = new JSONArray();
            var sampleIndex = 0;
            foreach (var beat in new[]
            {
                183.999f, 184f, 184.061f, 184.062f, 184.063f,
                187.999f, 188f, 188.001f, 188.374f, 188.375f,
                188.438f, 188.484f, 188.499f, 188.5f, 188.501f,
                188.484f, 184.062f
            })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                yield return null;
                Sample(beat, "seek", sampleIndex++, renderers, lights, nativeCar, failures, records);
            }

            clock.MoveToJsonTime(187.999f);
            yield return null;
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 187.999f);
            var currentSeconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
            foreach (var beat in new[] { 188f, 188.438f, 188.484f, 188.5f, 188.562f })
            {
                currentSeconds.SetValue(clock, clock.GetSecondsFromBeat(beat));
                yield return null;
                yield return null;
                yield return null;
                Assert.That(clock.SongAudioSource.isPlaying, Is.False);
                Sample(beat, "playback", sampleIndex++, renderers, lights, nativeCar, failures, records);
            }

            TestUtils.PauseDeterministicPlayback(clock);
            var directory = CaptureDirectory();
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                PathUtils.Combine(
                    directory,
                    fullMap
                        ? "full-runtime.json"
                        : "reduced-runtime.json"),
                records.ToString(2));
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void AssertNativeCar(Material material)
        {
            Assert.That(material, Is.Not.Null);
            // Chroma resolves Car. These flags/values are the selected game's serialized Car inputs.
            Assert.That(material.IsKeywordEnabled("REFLECTION_PROBE"), Is.True);
            Assert.That(material.IsKeywordEnabled("MULTIPLY_REFLECTIONS"), Is.True);
            Assert.That(material.IsKeywordEnabled("DIFFUSE"), Is.False);
            Assert.That(material.IsKeywordEnabled("SPECULAR"), Is.False);
            Assert.That(material.GetFloat("_ReflectionProbeIntensity"), Is.EqualTo(1f));
            Assert.That(material.GetFloat("_Smoothness"), Is.EqualTo(1f));
            Assert.That(material.GetFloat("_VertexMode"), Is.EqualTo(3f));
            Assert.That(material.GetFloat("_RimScale"), Is.EqualTo(0.1f));
            Assert.That(material.GetFloat("_RimOffset"), Is.EqualTo(0.18f));
        }

        private void Sample(
            float beat,
            string phase,
            int index,
            Renderer[] renderers,
            DirectionalLightsController[] lights,
            Material nativeCar,
            List<string> failures,
            JSONArray records)
        {
            foreach (var light in lights)
                Assert.That(
                    light.Light.Color,
                    Is.EqualTo(SuperluminalSurfaceLightingParityTest.NativeMixedColor(light)).Within(0.001f),
                    $"Native directional mixing must reach the actual lights at {phase} b{beat}.");

            var label = phase + "-" + index.ToString("D2") + "-" + beat.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            var camera = cameraManager.SelectedCameraController.Camera;
            var actual = Capture(camera, label + "-actual");
            var materials = renderers.Select(renderer => renderer.sharedMaterial).ToArray();
            var enabled = renderers.Select(renderer => renderer.enabled).ToArray();
            var block = new MaterialPropertyBlock();
            renderers[0].GetPropertyBlock(block);
            Assert.That(
                Vector4.Distance(block.GetColor(colorId), new Color(10f, 10f, 10f, 1f)),
                Is.LessThan(0.001f),
                "The authored HDR rocket color must survive material binding.");
            var bindingMatches = materials.All(
                material =>
                material.shader == nativeCar.shader && material.IsKeywordEnabled("REFLECTION_PROBE")
                && material.IsKeywordEnabled("MULTIPLY_REFLECTIONS")
                && !material.IsKeywordEnabled("DIFFUSE") && !material.IsKeywordEnabled("SPECULAR"));
            if (!bindingMatches)
                failures.Add($"{phase} b{beat}: rocket uses {materials[0].name} [{string.Join(",", materials[0].shaderKeywords)}], not native Car reflection inputs.");

            try
            {
                foreach (var renderer in renderers)
                    renderer.enabled = false;

                var withoutRocket = Capture(camera, label + "-without-rocket");
                for (var i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = enabled[i];
                    renderers[i].sharedMaterial = nativeCar;
                }

                var reference = Capture(camera, label + "-native-car");
                var difference = CountDifferent(actual, reference, 3);
                var contribution = CountDifferent(reference, withoutRocket, 3);
                var actualResponse = MeasureBrightRocketResponse(actual, withoutRocket);
                var nativeResponse = MeasureBrightRocketResponse(reference, withoutRocket);
                if (beat >= 184f && difference > 16)
                    failures.Add($"{phase} b{beat}: rocket differs from the native-Car-only control in {difference} pixels.");

                if (beat == 188f || beat == 188.5f)
                {
                    Assert.That(
                        contribution,
                        Is.GreaterThan(500),
                        $"The native material control must retain a visible rocket contribution at {phase} b{beat}.");
                    Assert.That(
                        nativeResponse.BrightPixels,
                        Is.GreaterThan(100),
                        $"Native Car must contribute a broad lit surface at the source type3 pulse, {phase} b{beat}.");
                    Assert.That(
                        nativeResponse.TextureBins,
                        Is.GreaterThan(2),
                        $"Native Car must retain the packed probe's varying brightness at {phase} b{beat}.");
                    if (actualResponse.BrightPixels < nativeResponse.BrightPixels * 0.9f)
                        failures.Add($"{phase} b{beat}: rocket has only {actualResponse.BrightPixels} bright surface pixels, native Car has {nativeResponse.BrightPixels}.");

                }

                else if (beat == 184.062f && actualResponse.BrightPixels > nativeResponse.BrightPixels + 16)
                    failures.Add($"{phase} b{beat}: rocket adds {actualResponse.BrightPixels - nativeResponse.BrightPixels} bright outline pixels absent from native Car's off phase.");

                records.Add(
                    new JSONObject
                    {
                        ["phase"] = phase,
                        ["beat"] = beat,
                        ["cameraZ"] = camera.transform.position.z,
                        ["material"] = materials[0].name,
                        ["materialBindingMatches"] = bindingMatches,
                        ["nativeMaterialDifferencePixels"] = difference,
                        ["nativeRocketContributionPixels"] = contribution,
                        ["actualBrightRocketPixels"] = actualResponse.BrightPixels,
                        ["nativeBrightRocketPixels"] = nativeResponse.BrightPixels,
                        ["actualTextureBins"] = actualResponse.TextureBins,
                        ["nativeTextureBins"] = nativeResponse.TextureBins,
                        ["probePosition"] = (Vector3)Shader.GetGlobalVector("_ReflectionProbePosition")
                    });
            }

            finally
            {
                for (var i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = enabled[i];
                    renderers[i].sharedMaterial = materials[i];
                }

            }

        }

        private static (int BrightPixels, int TextureBins) MeasureBrightRocketResponse(
            Color32[] rendered,
            Color32[] withoutRocket)
        {
            var count = 0;
            var bins = new HashSet<int>();
            for (var i = 0; i < rendered.Length; i++)
            {
                var brightness = (rendered[i].r + rendered[i].g + rendered[i].b) / 3;
                var background = (withoutRocket[i].r + withoutRocket[i].g + withoutRocket[i].b) / 3;
                if (brightness > 64 && brightness - background > 12)
                {
                    count++;
                    bins.Add(brightness / 16);
                }

            }

            return (count, bins.Count);
        }

        private static int CountDifferent(Color32[] first, Color32[] second, int threshold)
        {
            var count = 0;
            for (var i = 0; i < first.Length; i++)
            {
                if (Mathf.Abs(first[i].r - second[i].r) > threshold
                    || Mathf.Abs(first[i].g - second[i].g) > threshold
                    || Mathf.Abs(first[i].b - second[i].b) > threshold)
                {
                    count++;
                }

            }

            return count;
        }

        private string CaptureDirectory() => PathUtils.Combine(
            Application.dataPath,
            "..",
            "TestResults",
            "Superluminal",
            "Rocket",
            captureRun);

        private Color32[] Capture(Camera camera, string name)
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
                var directory = CaptureDirectory();
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
            if (clockCaptured && clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
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

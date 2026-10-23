using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    // Superluminal (358a7), modus/nasafrasa: the beat-196 tunnel uses BillieWater cubes.
    public class SuperluminalTunnelParityTest : TestBase
    {
        private bool previousAnimations;
        private float previousFov;
        private float previousOffset;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private AudioTimeSyncController playbackClock;
        private bool previousClockEnabled;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [Test]
        public void ImportedGagaProbesKeepNativeMipChainAndBindings()
        {
            var data = AssetDatabase.LoadAssetAtPath<ReflectionProbeDataSO>(
                "Assets/_Graphics/Textures/Environments/Gaga/GagaReflectionProbeData.asset");
            var index = 0;
            foreach (var name in new[] { "GagaReflectionProbe1", "GagaReflectionProbe2" })
            {
                var path = "Assets/_Graphics/Textures/Environments/Gaga/" + name + ".cmprobe";
                var texture = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(512));
                Assert.That(texture.mipmapCount, Is.EqualTo(10));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
                Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(index++ == 0 ? data.ReflectionProbeCubemap1 : data.ReflectionProbeCubemap2, Is.SameAs(texture));
            }
        }

        [UnityTest]
        [Explicit("Local reproduction requires the original Superluminal difficulty in TestResults/Superluminal.")]
        public IEnumerator FullMapTunnelUsesNativeWaterMaterial()
        {
            // Unity can include Explicit tests in an unfiltered run, where the local source fixture is absent.
            if (!File.Exists(GetFixturePath("SuperluminalFullMapFixture", true)))
                Assert.Ignore("The optional full-map reproduction needs the original Superluminal difficulty in TestResults/Superluminal.");

            yield return CheckTunnel("SuperluminalFullMapFixture", true);
        }

        [UnityTest]
        public IEnumerator ReducedTunnelUsesNativeWaterMaterial()
        {
            yield return CheckTunnel("SuperluminalTunnelFixture", false);
        }

        [UnityTest]
        public IEnumerator ReducedTunnelRetainsNativeProbeFiltering()
        {
            yield return CheckTunnel("SuperluminalTunnelPatternFixture", false, true);
        }

        private static string GetFixturePath(string fixtureName, bool fullMap) => fullMap
            ? PathUtils.Combine(Application.dataPath, "..", "TestResults", "Superluminal", fixtureName + ".json")
            : PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", fixtureName + ".json");

        private IEnumerator CheckTunnel(string fixtureName, bool fullMap, bool checkPattern = false)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90;
            Settings.Instance.PlayerCameraOffsetZ = 0;
            var path = GetFixturePath(fixtureName, fullMap);
            var data = JSON.Parse(File.ReadAllText(path));
            var activationPositions = new Dictionary<string, Vector3>();
            foreach (var evt in data["customData"]["customEvents"].Children)
            {
                var properties = evt["d"];
                if (evt["b"].AsFloat != 196f || evt["t"].Value != "AnimateTrack"
                    || !properties["track"].Value.StartsWith("modelScene1_waterTunnel_"))
                {
                    continue;
                }

                var position = properties["position"];
                activationPositions.Add(
                    properties["track"].Value,
                    new Vector3(position[0].AsFloat, position[1].AsFloat, position[2].AsFloat));
            }

            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 16,
                NoteStartBeatOffset = 0,
                CustomColorLeft = new Color(0.6039216f, 0.254902f, 0.9960784f),
                CustomColorRight = new Color(0.1294118f, 0.2156863f, 0.827451f),
                LightshowFileName = "MissingTestLightshow.dat",
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" }
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
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            playbackClock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = playbackClock.enabled;
            playbackClock.enabled = false;
            playbackClock.SongAudioSource.Stop();
            var tunnels = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement.Track?.StartsWith("modelScene1_waterTunnel_") == true)
                .OrderBy(container => int.Parse(container.EnvironmentEnhancement.Track.Split('_').Last())).ToArray();
            Assert.That(tunnels.Length, Is.EqualTo(activationPositions.Count));
            var renderers = tunnels.SelectMany(container => container.MpbController.Renderers).ToArray();
            var pulseRenderers = fullMap
                ? Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None)
                    .Where(container => container.EnvironmentEnhancement.Track == "modelScene0_laser1_0")
                    .SelectMany(container => container.MpbController.Renderers).ToArray()
                : System.Array.Empty<Renderer>();
            if (fullMap)
                Assert.That(pulseRenderers.Length, Is.EqualTo(1));

            var nativeMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Graphics/Materials/Environments/Billie/WaterfallFalling.mat");
            Assert.That(nativeMaterial, Is.Not.Null);
            var errors = new List<string>();
            var probe = Object.FindAnyObjectByType<BakedReflectionProbe>();
            Assert.That(probe, Is.Not.Null);
            if (probe.ReflectionProbeData == null
                || probe.ReflectionProbeData.ReflectionProbeCubemap1 == null
                || probe.ReflectionProbeData.ReflectionProbeCubemap2 == null
                || Shader.GetGlobalFloat("_HasBakedReflectionProbeData") != 1f)
            {
                errors.Add("Gaga's native packed reflection pair must be published for BillieWater geometry.");
            }

            var seekIndex = 0;
            foreach (var beat in new[] { 195.999f, 196f, 196.015f, 196.062f, 196.375f, 196.750f, 196.875f, 195.999f, 196.015f, 196f })
            {
                playbackClock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                for (var i = 0; i < tunnels.Length; i++)
                {
                    var target = tunnels[i].Animator.LocalTarget;
                    if (beat == 196f)
                    {
                        var expectedPosition = activationPositions[tunnels[i].EnvironmentEnhancement.Track];
                        if (Vector3.Distance(target.position, expectedPosition) > 0.05f)
                            errors.Add($"Beat196 tunnel{i} remains at {target.position}; activation must immediately use {expectedPosition}.");

                    }
                }

                if (!fullMap && !checkPattern)
                    continue;

                var camera = cameraManager.SelectedCameraController.Camera;
                var label = fixtureName + "-seek" + seekIndex++ + "-"
                    + beat.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
                var actual = Capture(camera, label + "-actual");
                CaptureWithout(camera, label + "-without-pulse", pulseRenderers);
                var removed = CaptureWithout(camera, label + "-without-tunnel", renderers);
                if (beat == 196.750f)
                {
                    CheckNativeProbeFiltering(camera, label, actual, errors);
                    yield return CheckMovedPreviewCameras(label, errors);
                }

                if (beat == 196.015f && fullMap)
                {
                    if (CountDifferences(actual, removed) < 1000)
                        errors.Add("The authored tunnel did not contribute enough rendered pixels to reproduce the report.");

                    // Subtraction excludes blue background gaps so black strips cannot satisfy the color check.
                    var coloredPixels = CountColoredTunnelPixels(actual, removed);
                    if (coloredPixels < 200000)
                        errors.Add($"The tunnel must render broad blue/purple strips; only {coloredPixels} colored tunnel pixels were present.");

                    var withoutProbe = CaptureWithoutPackedProbe(camera, label + "-without-packed-probe");
                    var missingProbePixels = CountColoredTunnelPixels(withoutProbe, removed);
                    if (missingProbePixels >= 200000)
                        errors.Add($"Disabling the packed probe must reproduce the missing strip colors; {missingProbePixels} colored pixels remained.");

                }
            }

            // Chroma's EnvironmentMaterialsManager resolves BillieWater to WaterfallFalling, not WaterfallMirror.
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial.shader != nativeMaterial.shader)
                    errors.Add($"BillieWater tunnel uses {renderer.sharedMaterial.shader.name}; native uses {nativeMaterial.shader.name}.");

            }

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        private static int Difference(Color32 left, Color32 right) =>
            Mathf.Abs(left.r - right.r) + Mathf.Abs(left.g - right.g) + Mathf.Abs(left.b - right.b);

        private static int CountDifferences(Color32[] left, Color32[] right)
        {
            var changed = 0;
            for (var i = 0; i < left.Length; i++)
            {
                if (Difference(left[i], right[i]) > 15)
                    changed++;

            }

            return changed;
        }

        private static int CountColoredTunnelPixels(Color32[] rendered, Color32[] withoutTunnel)
        {
            var colored = 0;
            for (var i = 0; i < rendered.Length; i++)
            {
                var color = rendered[i];
                if (Difference(color, withoutTunnel[i]) > 15
                    && color.b >= 48 && color.b >= color.g * 1.5f
                    && color.b >= color.r * 0.75f && Mathf.Max(color.r, color.b) - color.g >= 32)
                {
                    colored++;
                }
            }

            return colored;
        }

        private static Color32[] CaptureWithout(Camera camera, string label, Renderer[] renderers)
        {
            var enabled = renderers.Select(renderer => renderer.enabled).ToArray();
            try
            {
                foreach (var renderer in renderers)
                    renderer.enabled = false;

                return Capture(camera, label);
            }

            finally
            {
                for (var i = 0; i < renderers.Length; i++)
                    renderers[i].enabled = enabled[i];

            }
        }

        private static Color32[] CaptureWithoutPackedProbe(Camera camera, string label)
        {
            var previousAvailability = Shader.GetGlobalFloat("_HasBakedReflectionProbeData");
            try
            {
                Shader.SetGlobalFloat("_HasBakedReflectionProbeData", 0f);
                return Capture(camera, label);
            }

            finally
            {
                Shader.SetGlobalFloat("_HasBakedReflectionProbeData", previousAvailability);
            }
        }

        private static void CheckNativeProbeFiltering(Camera camera, string label, Color32[] actual, List<string> errors)
        {
            var native = CaptureNativeProbeControl(camera, label + "-native-probe-control");
            var difference = LowerTunnelDifference(actual, native, out var comparedPixels);
            if (comparedPixels < 128)
                errors.Add(label + ": the low-strobe control must render the lower tunnel region: " + comparedPixels);

            else if (difference > 0.015f)
                errors.Add(label + ": the lower tunnel differs from the native mip-filtered control: mean RGB difference=" + difference);
        }

        private IEnumerator CheckMovedPreviewCameras(string label, List<string> errors)
        {
            var player = cameraManager.SelectedCameraController.transform;
            var playerPosition = player.position;
            var playerRotation = player.rotation;
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            var editor = cameraManager.SelectedCameraController;
            var previousPosition = editor.transform.position;
            var previousRotation = editor.transform.rotation;
            try
            {
                foreach (var offset in new[] { new Vector3(-3f, 0f, -2f), new Vector3(3f, 0f, -2f) })
                {
                    editor.transform.SetPositionAndRotation(playerPosition + offset, playerRotation);
                    yield return null;
                    yield return null;
                    var movedLabel = label + "-preview-x" + offset.x;
                    var actual = Capture(editor.Camera, movedLabel + "-actual");
                    CheckNativeProbeFiltering(editor.Camera, movedLabel, actual, errors);
                }
            }
            finally
            {
                editor.transform.SetPositionAndRotation(previousPosition, previousRotation);
                uiMode.SetUIMode(UIModeType.Playing, false);
                cameraManager.SelectCamera(CameraType.Playing);
            }
        }

        private static Color32[] CaptureNativeProbeControl(Camera camera, string label)
        {
            var native1 = ReadNativeProbe(0);
            var native2 = ReadNativeProbe(1);
            var previous1 = Shader.GetGlobalTexture("_ReflectionProbeTexture1");
            var previous2 = Shader.GetGlobalTexture("_ReflectionProbeTexture2");
            try
            {
                Shader.SetGlobalTexture("_ReflectionProbeTexture1", native1);
                Shader.SetGlobalTexture("_ReflectionProbeTexture2", native2);
                return Capture(camera, label);
            }
            finally
            {
                Shader.SetGlobalTexture("_ReflectionProbeTexture1", previous1);
                Shader.SetGlobalTexture("_ReflectionProbeTexture2", previous2);
                Object.DestroyImmediate(native1);
                Object.DestroyImmediate(native2);
            }
        }

        private static Cubemap ReadNativeProbe(int probeIndex)
        {
            var path = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "GagaWaterProbeMipReference.bytes.gz");
            byte[] data;
            using (var file = File.OpenRead(path))
            using (var compressed = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                compressed.CopyTo(output);
                data = output.ToArray();
            }
            var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB;
            var texture = new Cubemap(512, format, UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain)
            {
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 1
            };
            var offset = probeIndex * (data.Length / 2);
            for (var face = 0; face < 6; face++)
            {
                for (var mip = 2; mip <= 3; mip++)
                {
                    var size = 512 >> mip;
                    var pixels = new Color32[size * size];
                    for (var i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(data[offset + i * 3], data[offset + i * 3 + 1], data[offset + i * 3 + 2], 255);

                    texture.SetPixelData(pixels, mip, (CubemapFace)face);
                    offset += size * size * 3;
                }
            }
            Assert.That(offset, Is.EqualTo((probeIndex + 1) * (data.Length / 2)));
            texture.Apply(false, false);
            return texture;
        }

        private static float LowerTunnelDifference(Color32[] actual, Color32[] reference, out int comparedPixels)
        {
            // GetPixels32 starts at the bottom. This excludes foreground notes and the central bloom while crossing the reported ring.
            var difference = 0f;
            comparedPixels = 0;
            for (var y = 11; y < 219; y++)
            {
                for (var x = 368; x < 655; x++)
                {
                    var index = y * 1024 + x;
                    var expected = reference[index];
                    if (expected.b < 50 || expected.b <= expected.r * 1.5f || expected.b <= expected.g * 1.5f)
                        continue;

                    difference += Difference(actual[index], expected) / (255f * 3f);
                    comparedPixels++;
                }
            }
            return comparedPixels == 0 ? 0f : difference / comparedPixels;
        }

        private static Color32[] Capture(Camera camera, string label)
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
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "Superluminal", "Tunnel");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(PathUtils.Combine(directory, label + ".png"), texture.EncodeToPNG());
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
                playbackClock.enabled = previousClockEnabled;

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
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

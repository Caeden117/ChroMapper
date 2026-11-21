using System.Collections;
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
    public class FireStarterMetallicaParityTest : TestBase
    {
        private const int CaptureWidth = 1024;
        private const int CaptureHeight = 576;

        private bool previousAnimations;
        private float previousCameraOffset;
        private float previousCameraFov;
        private UIMode uiMode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousCameraOffset = Settings.Instance.PlayerCameraOffsetZ;
            previousCameraFov = Settings.Instance.PlayerCameraFOV;
            ConfigurePreview();
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 16f,
                NoteStartBeatOffset = -0.75f,
                LightshowFileName = "MissingTestLightshow.dat",
                CustomColorLeft = new Color(0.785f, 0.078f, 0.078f),
                CustomColorRight = new Color(0.15f, 0.557f, 0.824f),
                CustomEnvColorLeft = new Color(0.839f, 0.392f, 0.102f),
                CustomEnvColorRight = new Color(0.69f, 0.102f, 0.839f),
                CustomEnvColorWhite = new Color(0.775f, 0.869f, 0.784f)
            };

            // Keep the surrounding groups and each group's next event so interpolation and occlusion match the full map.
            var fixturePath = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "FireStarterMetallicaFixture.json");
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(fixturePath)),
                beatsPerMinute: 131f,
                environmentName: "MetallicaEnvironment",
                songLengthSeconds: 200,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
        }

        [SetUp]
        public void ConfigurePreview()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.PlayerCameraFOV = 90f;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator InfiniteLasersContributeAtReportedBeat()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(100.578f);
            yield return null;
            yield return null;

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var lasers = context.Descriptor.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                .Where(light => light.Kind == LightController.LightKind.Group && (light.Type == 29 || light.Type == 30))
                .OrderBy(light => light.Type).ThenBy(light => light.ID).ToArray();
            Assert.AreEqual(24, lasers.Length);
            var colors = lasers.Select(light => light.Color).ToArray();
            var camera = cameras.SelectedCameraController.Camera;
            var actual = Capture(camera, "actual");
            Color32[] withoutLasers;
            try
            {
                foreach (var laser in lasers)
                    laser.SetColor(Color.clear);

                withoutLasers = Capture(camera, "without-lasers");
            }
            finally
            {
                for (var index = 0; index < lasers.Length; index++)
                    lasers[index].SetColor(colors[index]);
            }

            var changedPixels = 0;
            for (var y = 0; y < CaptureHeight; y++)
            {
                for (var x = 0; x < CaptureWidth; x++)
                {
                    // Exclude the wall corridor so the beams must contribute visible pixels in the surrounding environment.
                    if (x >= 341 && x < 683 && y < 461)
                        continue;

                    var index = (y * CaptureWidth) + x;
                    var difference = Mathf.Abs(actual[index].r - withoutLasers[index].r)
                        + Mathf.Abs(actual[index].g - withoutLasers[index].g)
                        + Mathf.Abs(actual[index].b - withoutLasers[index].b);
                    if (difference >= 24)
                        changedPixels++;
                }
            }

            Assert.That(
                changedPixels,
                Is.GreaterThan(100),
                "Metallica's groups 29 and 30 should draw visible beams around the pink walls at beat 100.578.");
        }

        [UnityTest]
        public IEnumerator StarsMatchNativePixelsThroughoutFade() => CheckNativeStarFade(false);

        [UnityTest]
        public IEnumerator ImportedStarsMatchNativePixelsThroughoutFade() => CheckNativeStarFade(true);

        private IEnumerator CheckNativeStarFade(bool importRendererSettings)
        {
            var fixture = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "MetallicaNativeStarFadeFixture.json")));
            var width = fixture["width"].AsInt;
            var height = fixture["height"].AsInt;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var sources = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor
                .GetComponentsInChildren<ParticleSystem>(true)
                .Where(system => system.name.StartsWith("StarsParticles")).ToArray();
            Assert.That(sources.Length, Is.EqualTo(4));
            var renderers = sources.Select(system => system.GetComponent<ParticleSystemRenderer>()).ToArray();
            var previousColorSpaces = renderers.Select(renderer => renderer.applyActiveColorSpace).ToArray();
            var previousMaterials = renderers.Select(renderer => renderer.sharedMaterial).ToArray();
            var previousLifetimeColors = sources.Select(system => system.colorOverLifetime.enabled).ToArray();
            try
            {
                if (importRendererSettings)
                {
                    var environmentData = CreateUtils.JsonToEnvironmentData(AssetDatabase.LoadAssetAtPath<TextAsset>(
                        "Assets/__Scenes/Environments/Data/MetallicaEnvironment.json"));
                    var library = AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(
                        "Assets/Editor/Environments/EnvironmentLibrarySO.asset");
                    library.Materials.RebuildLookup();
                    var container = new CreateContainer { Data = environmentData, Library = library };
                    foreach (var system in sources)
                    {
                        var renderer = system.GetComponent<ParticleSystemRenderer>();
                        // New Unity 6.3 renderers enable this conversion before environment data is applied.
                        renderer.applyActiveColorSpace = true;
                        environmentData.Objects.First(data => data.GameObjectName == system.name)
                            .Components.ParticleSystem[0].Renderer.CopyTo(renderer, container);
                    }
                }

                foreach (var frame in fixture["frames"].AsArray.Children)
                {
                    clock.MoveToJsonTime(frame["beat"].AsFloat);
                    yield return null;
                    yield return null;
                    var particles = frame["particles"].AsArray.Children.Select(particle => new ParticleSystem.Particle
                    {
                        position = ReadVector(particle["position"]),
                        rotation3D = ReadVector(particle["rotation"]),
                        startSize = particle["size"],
                        startColor = ReadColor(particle["color"]),
                        startLifetime = 10f,
                        remainingLifetime = 10f
                    }).ToArray();
                    foreach (var system in sources)
                    {
                        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var lifetime = system.colorOverLifetime;
                        lifetime.enabled = false;
                        // Native stars share a seed. Keep the full particle field so overlapping sprites and bloom match.
                        system.SetParticles(particles);
                        system.Pause();
                        var block = new MaterialPropertyBlock();
                        block.SetColor("_Color", ReadColor(frame["tint"]));
                        system.GetComponent<ParticleSystemRenderer>().SetPropertyBlock(block);
                    }

                    var camera = cameras.SelectedCameraController.Camera;
                    camera.transform.SetPositionAndRotation(ReadVector(fixture["cameraPosition"]), Quaternion.identity);
                    camera.fieldOfView = fixture["cameraVerticalFov"];
                    var pixels = Capture(camera, frame["capture"].Value + "-star-regression", width, height);
                    var peak = MeasureStarPeak(
                        pixels,
                        width,
                        height,
                        frame["pixel"][0].AsInt,
                        frame["pixel"][1].AsInt,
                        frame["backgroundRadius"].AsInt);
                    var expected = ReadVector(frame["peak"]);
                    Assert.That(
                        Vector3.Distance(peak, expected),
                        Is.LessThanOrEqualTo(8f),
                        $"Native capture {frame["capture"]}, beat {frame["beat"]}: peak {peak}, expected {expected}.");
                }
            }
            finally
            {
                for (var index = 0; index < sources.Length; index++)
                {
                    renderers[index].applyActiveColorSpace = previousColorSpaces[index];
                    renderers[index].sharedMaterial = previousMaterials[index];
                    var lifetime = sources[index].colorOverLifetime;
                    lifetime.enabled = previousLifetimeColors[index];
                    sources[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    sources[index].Play();
                }
            }
        }

        private static Vector3 ReadVector(JSONNode node) => new(node[0], node[1], node[2]);

        private static Color ReadColor(JSONNode node) => new(node[0], node[1], node[2], node[3]);

        private static Vector3 MeasureStarPeak(
            Color32[] pixels,
            int width,
            int height,
            int x,
            int topY,
            int radius)
        {
            var y = height - 1 - topY;
            var size = (radius * 2) + 1;
            var red = new byte[size * size];
            var green = new byte[red.Length];
            var blue = new byte[red.Length];
            var index = 0;
            var peak = Vector3.zero;
            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var pixel = pixels[((y + dy) * width) + x + dx];
                    red[index] = pixel.r;
                    green[index] = pixel.g;
                    blue[index++] = pixel.b;
                    if (Mathf.Abs(dx) <= 2 && Mathf.Abs(dy) <= 2)
                    {
                        peak.x = Mathf.Max(peak.x, pixel.r);
                        peak.y = Mathf.Max(peak.y, pixel.g);
                        peak.z = Mathf.Max(peak.z, pixel.b);
                    }
                }
            }

            System.Array.Sort(red);
            System.Array.Sort(green);
            System.Array.Sort(blue);
            var middle = red.Length / 2;
            return peak - new Vector3(red[middle], green[middle], blue[middle]);
        }

        private static Color32[] Capture(
            Camera camera,
            string name,
            int width = CaptureWidth,
            int height = CaptureHeight)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var directory = PathUtils.Combine(
                    Application.dataPath,
                    "..",
                    "..",
                    "Scratch",
                    "FireStarterMetallica",
                    "captures");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(PathUtils.Combine(directory, name + ".png"), texture.EncodeToPNG());
                return texture.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(target);
            }
        }

        [UnityTearDown]
        public IEnumerator RestorePreview()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraOffsetZ = previousCameraOffset;
            Settings.Instance.PlayerCameraFOV = previousCameraFov;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Source: Array by VoltageO and CMT (50222), from the Kamikazi ending-cube repro.
    // Keep one enclosing cube, its three bars, their original animations, and the inherited light events.
    public class KamikaziEndingCubeParityTest : InputTestBase
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly System.Reflection.PropertyInfo CurrentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        private bool previousAnimations;
        private bool previousBloom;
        private float previousFov;
        private float previousOffset;
        private UIMode uiMode;
        private CameraManager cameraManager;

        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousBloom = Settings.Instance.Bloom;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.ApplyOptionByName("Bloom", true);

            var path = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "KamikaziEndingCubeFixture.json");
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(path)),
                beatsPerMinute: 202,
                environmentName: "DefaultEnvironment",
                songLengthSeconds: 100);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator EnclosedPlusStaysVisibleAfterSeeksAndDuringPlayback()
        {
            InitializeVirtualInput(includeKeyboard: true);
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previousPosition = camera.transform.position;
            var previousRotation = camera.transform.rotation;
            var previousCameraFov = camera.fieldOfView;
            var previousTarget = camera.targetTexture;
            var targetFormat = camera.allowHDR
                ? RenderTextureFormat.DefaultHDR
                : RenderTextureFormat.ARGB32;
            var target = new RenderTexture(1024, 576, 24, targetFormat);
            var image = new Texture2D(1024, 576, TextureFormat.RGBA32, false);

            try
            {
                camera.transform.SetPositionAndRotation(new Vector3(0f, 1.65f, 0f), Quaternion.identity);
                camera.fieldOfView = 90f;
                camera.targetTexture = target;

                foreach (var beat in new[] { 214.125f, 216f, 214.125f })
                {
                    clock.MoveToJsonTime(beat);
                    yield return null;
                    yield return null;
                    AssertVisiblePlus(camera, target, image);
                }

                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 213.75f);
                for (var beat = 213.75f; beat <= 214.125f; beat += 1f / 64f)
                {
                    var songBeat = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(beat);
                    CurrentSecondsProperty.SetValue(clock, clock.GetSecondsFromBeat(songBeat));
                    yield return null;
                }

                Assert.That(clock.IsPlaying, Is.True);
                Assert.That(clock.SongAudioSource.isPlaying, Is.False);
                AssertVisiblePlus(camera, target, image);
            }
            finally
            {
                clock.StopScheduled = false;
                if (clock.IsPlaying)
                    clock.CancelPlaying();

                camera.targetTexture = previousTarget;
                camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
                camera.fieldOfView = previousCameraFov;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
                TearDownVirtualInput();
            }
        }

        private static void AssertVisiblePlus(Camera camera, RenderTexture target, Texture2D image)
        {
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>()
                .LoadedContainers.Values
                .OfType<GeometryContainer>()
                .Where(container => container.EnvironmentEnhancement.Geometry != null)
                .ToArray();
            var cube = geometry.Single(container =>
                container.EnvironmentEnhancement.Scale == new Vector3(1.2f, 1.2f, 1.2f));
            var cubeRenderer = cube.GetComponentsInChildren<ParametricBloomFogLightController>(true).Single().BoxLight.Renderer;
            var bars = geometry.Where(container => container != cube)
                .Select(container => container.GetComponentsInChildren<ParametricBloomFogLightController>(true).Single().BoxLight.Renderer)
                .ToArray();
            Assert.That(bars, Has.Length.EqualTo(3));
            Assert.That(cubeRenderer.enabled, Is.True);
            foreach (var bar in bars)
            {
                Assert.That(bar.enabled && bar.gameObject.activeInHierarchy, Is.True);
                Assert.That(Vector3.Distance(bar.bounds.center, cubeRenderer.bounds.center), Is.LessThan(0.05f));
            }

            var properties = new MaterialPropertyBlock();
            cubeRenderer.GetPropertyBlock(properties);
            Assert.That(properties.GetColor(ColorId).a, Is.EqualTo(0.7490196f).Within(0.0001f));

            var bounds = cubeRenderer.bounds;
            var minimum = new Vector2(float.MaxValue, float.MaxValue);
            var maximum = new Vector2(float.MinValue, float.MinValue);
            for (var corner = 0; corner < 8; corner++)
            {
                var viewport = camera.WorldToViewportPoint(new Vector3(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z));
                minimum = Vector2.Min(minimum, viewport);
                maximum = Vector2.Max(maximum, viewport);
            }

            var on = RenderPixels(camera, target, image);
            Color32[] off;
            try
            {
                foreach (var bar in bars)
                    bar.enabled = false;

                off = RenderPixels(camera, target, image);
            }
            finally
            {
                foreach (var bar in bars)
                    bar.enabled = true;
            }

            var margin = (maximum - minimum) * 0.2f;
            var x0 = Mathf.Clamp(Mathf.CeilToInt((minimum.x + margin.x) * target.width), 0, target.width - 1);
            var x1 = Mathf.Clamp(Mathf.FloorToInt((maximum.x - margin.x) * target.width), 0, target.width - 1);
            var y0 = Mathf.Clamp(Mathf.CeilToInt((minimum.y + margin.y) * target.height), 0, target.height - 1);
            var y1 = Mathf.Clamp(Mathf.FloorToInt((maximum.y - margin.y) * target.height), 0, target.height - 1);
            Assert.That((x1 - x0 + 1) * (y1 - y0 + 1), Is.GreaterThanOrEqualTo(20));
            var changedPixels = 0;
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var index = y * target.width + x;
                    var delta = Mathf.Max(
                        Mathf.Abs(on[index].r - off[index].r),
                        Mathf.Max(Mathf.Abs(on[index].g - off[index].g), Mathf.Abs(on[index].b - off[index].b)));
                    if (delta >= 8)
                        changedPixels++;
                }
            }

            Assert.That(changedPixels, Is.GreaterThanOrEqualTo(10), "The cube surface hid its enclosed plus.");
        }

        private static Color32[] RenderPixels(Camera camera, RenderTexture target, Texture2D image)
        {
            var previousActive = RenderTexture.active;
            try
            {
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (clock != null)
            {
                clock.StopScheduled = false;
                if (clock.IsPlaying)
                    clock.CancelPlaying();
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            TearDownVirtualInput();
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            Settings.ApplyOptionByName("Bloom", previousBloom);
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

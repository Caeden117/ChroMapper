using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // Salty's beat-2.594 et0 lightID[1] event maps via the Chroma Panic type1 table (key1 ->
    // controller index 6) to the registered Panic laser; the user confirms live CM now matches
    // the game's side after the ObjectAnimator lifecycle fix.
    // Fixture keeps the authored customEvents/environment relative order, no flattening.
    public class SaltyOpeningLaserParityTest : TestBase
    {
        private const int CaptureWidth = 1024;
        private const int CaptureHeight = 576;

        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "SaltyOpeningLaserFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            previousPlayerCameraFOV = Settings.Instance.PlayerCameraFOV;
            previousPlayerCameraOffsetZ = Settings.Instance.PlayerCameraOffsetZ;
            previousCameraFOV = Settings.Instance.CameraFOV;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 215,
                environmentName: "PanicEnvironment",
                songLengthSeconds: 250);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // The authored lightID remap must light controller 6 and leave controller 7 dark.
        [UnityTest]
        public IEnumerator Beat2625LightIdOneRegistersActiveController()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.SelectedCameraController.Camera;
            yield return SeekTo(2.625f);

            var pair = Object.FindObjectsByType<ParametricBloomFogLightController>(
                    FindObjectsSortMode.None)
                .Where(c => c.Type == 0 && (c.ID == 6 || c.ID == 7))
                .OrderBy(c => c.ID)
                .ToList();
            var id6 = pair.SingleOrDefault(c => c.ID == 6);
            var id7 = pair.SingleOrDefault(c => c.ID == 7);
            Assert.That(id6, Is.Not.Null, "Type0 ID6 controller missing; fixture lost the remap target.");
            Assert.That(id7, Is.Not.Null, "Type0 ID7 controller missing; fixture lost the remap target.");
            Assert.That(id6.Type, Is.EqualTo(0));
            Assert.That(id6.Color.a, Is.GreaterThan(0.01f),
                "Type0 ID6 carried no color at beat 2.625; fixture lost the lightID[1] routing.");
            Assert.That(id7.Type, Is.EqualTo(0));
            Assert.That(id7.Color.a, Is.LessThanOrEqualTo(0.01f),
                "Type0 ID7 lit at beat 2.625; lightID[1] should only drive ID6.");

            var litPixels = RenderPixels(camera);
            var color = id6.Color;
            Color32[] unlitPixels;
            try
            {
                id6.SetColor(Color.clear);
                id6.Refresh();
                unlitPixels = RenderPixels(camera);
            }
            finally
            {
                id6.SetColor(color);
                id6.Refresh();
            }

            var left = CountAddedRedLaserPixels(litPixels, unlitPixels, 260, 460);
            var right = CountAddedRedLaserPixels(litPixels, unlitPixels, 564, 764);
            Assert.That(left + right, Is.GreaterThan(0),
                $"ID6 added no visible red laser pixels at beat 2.625; left={left} right={right}.");
        }

        private static int CountAddedRedLaserPixels(Color32[] litPixels, Color32[] unlitPixels, int xMin, int xMax)
        {
            var count = 0;
            for (var topY = 350; topY < CaptureHeight; topY++)
            {
                var row = CaptureHeight - 1 - topY;
                for (var x = xMin; x <= xMax; x++)
                {
                    var index = (row * CaptureWidth) + x;
                    if (IsRedLaserPixel(litPixels[index]) && !IsRedLaserPixel(unlitPixels[index]))
                        count++;
                }
            }
            return count;
        }

        private static bool IsRedLaserPixel(Color32 pixel) =>
            pixel.r >= 140 && pixel.r >= 2 * pixel.g && pixel.r >= 2 * pixel.b;

        private static Color32[] RenderPixels(Camera camera)
        {
            // The red-pixel thresholds describe the playing camera's sRGB display output.
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
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

        private void ApplyDeployedCameraSettings()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.ApplyOptionByName(nameof(Settings.PlayerCameraOffsetZ), 0f);
            Settings.Instance.CameraFOV = 90f;
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        // A failed assertion must still leave the editor camera and preview mode safe for reload.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = previousPlayerCameraFOV;
            Settings.ApplyOptionByName(nameof(Settings.PlayerCameraOffsetZ), previousPlayerCameraOffsetZ);
            Settings.Instance.CameraFOV = previousCameraFOV;
            yield break;
        }
    }
}

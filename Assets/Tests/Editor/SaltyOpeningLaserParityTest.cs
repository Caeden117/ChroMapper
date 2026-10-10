using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // Salty's beat-2.594 et0 lightID[1] event maps via the Chroma Panic type1 table (key1 ->
    // controller index 6) to the registered Panic laser; the user confirms live CM now matches
    // the game's side after the ObjectAnimator lifecycle fix.
    // Fixture keeps the authored customEvents/environment relative order, no flattening.
    // Explicit because the HDR screenshot path is machine/GPU-specific; run on demand with
    // -TestFilter 'Tests.Editor.SaltyOpeningLaserParityTest'.
    [Explicit]
    public class SaltyOpeningLaserParityTest : TestBase
    {
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

        // Runtime light-routing check: identical red-pixel windows to the full-map diagnostic
        // (left x260..460, right x564..764, top-down y350..575, r>=140 && r>=2g && r>=2b) are
        // still captured, but the batch camera cannot assert live screen orientation - it only
        // proves the remapped ID6 controller is lit and ID7 stays dark.
        [UnityTest]
        public IEnumerator Beat2625LightIdOneRegistersActiveController()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);
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
            foreach (var controller in pair)
            {
                var marker = controller.GetComponent<ChromaIDMarker>();
                Debug.Log($"[SaltyDiag] fixture type0 id={controller.ID} chromaID=" +
                    $"{(marker == null ? "<none>" : marker.ChromaID)} " +
                    $"pos={controller.transform.position} color={controller.Color}");
            }
            Assert.That(id6.Type, Is.EqualTo(0));
            Assert.That(id6.Color.a, Is.GreaterThan(0.01f),
                "Type0 ID6 carried no color at beat 2.625; fixture lost the lightID[1] routing.");
            Assert.That(id7.Type, Is.EqualTo(0));
            Assert.That(id7.Color.a, Is.LessThanOrEqualTo(0.01f),
                "Type0 ID7 lit at beat 2.625; lightID[1] should only drive ID6.");

            var pixels = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-fixture-laser2625.png"));
            var left = CountRedLaserPixels(pixels, 260, 460);
            var right = CountRedLaserPixels(pixels, 564, 764);
            Object.DestroyImmediate(pixels);
            Debug.Log($"[SaltyDiag] fixture laser2625 red-px L={left} R={right}");
            Assert.That(left + right, Is.GreaterThan(0),
                $"No visible red laser pixels at beat 2.625; left={left} right={right}.");
        }

        private static int CountRedLaserPixels(Texture2D texture, int xMin, int xMax)
        {
            var pixels = texture.GetPixels32();
            var count = 0;
            for (var topY = 350; topY <= 575 && topY < 576; topY++)
            {
                var row = 575 - topY;
                for (var x = xMin; x <= xMax && x < 1024; x++)
                {
                    var p = pixels[row * 1024 + x];
                    if (p.r >= 140 && p.r >= 2 * p.g && p.r >= 2 * p.b) count++;
                }
            }
            return count;
        }

        private Texture2D RenderPixelsAndSave(Camera camera, string path)
        {
            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        private void ApplyDeployedCameraSettings()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
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
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerCameraOffsetZ;
            Settings.Instance.CameraFOV = previousCameraFOV;
            yield break;
        }
    }
}

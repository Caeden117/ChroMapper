using System.Collections;
using System.Collections.Generic;
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
    // Salty's beat-221 fake walls keep the authored relative order of the source map's
    // customData.fakeObstacles[6..14]: eight red bars on beat0..beat7 tracks and the white
    // [1/4, pee2] wall that the game draws while CM emits zero pixels.
    public class SaltyBeat222WallParityTest : TestBase
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
            "SaltyBeat222FakeWallFixture.json");

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

        // Deployed-vs-game discriminator: the game shows the white wall crossing the red bars, but the
        // full-map capture's white-off A/B was byte-identical. The same wall at frame edgeSize .05
        // renders hundreds of pixels, so the regression is the collapsed thin geometry.
        [UnityTest]
        public IEnumerator WhiteFakeWallRemainsVisibleWhileCrossingRedBars()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var shotDir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(shotDir);

            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(walls, Is.Not.Null, "No ObstacleGridContainer in the loaded scene.");

            var failures = new List<string>();
            List<Beatmap.Containers.ObstacleContainer> redWalls = null;
            foreach (var beat in new[] { 221.1f, 222.25f, 223f })
            {
                yield return SeekTo(beat);
                redWalls ??= walls.LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData.CustomFake
                        && c.ObstacleData.CustomTrack != null
                        && c.ObstacleData.CustomTrack.IsString
                        && c.ObstacleData.CustomTrack.Value.StartsWith("beat"))
                    .ToList();
                var wall = FindWhiteFakeWall(walls);
                var normalPixels = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, $"salty-fixture-beat{beat}-white-normal.png"));

                var disabled = new List<Renderer>();
                Texture2D offPixels = null;
                try
                {
                    foreach (var renderer in wall.GetComponentsInChildren<Renderer>(true)
                        .Where(r => r.enabled && (r.name == "Core" || r.name == "Outline")))
                    {
                        renderer.enabled = false;
                        disabled.Add(renderer);
                    }
                    offPixels = RenderPixelsAndSave(camera,
                        PathUtils.Combine(shotDir, $"salty-fixture-beat{beat}-white-off.png"));
                }
                finally
                {
                    foreach (var renderer in disabled) renderer.enabled = true;
                }

                var delta = CountPixelDelta(normalPixels.GetPixels32(), offPixels.GetPixels32());
                Object.DestroyImmediate(normalPixels);
                Object.DestroyImmediate(offPixels);
                if (delta == 0)
                    failures.Add($"beat {beat}: disabling the white wall changed 0 pixels; " +
                        "the game draws it visibly while crossing the red bars.");
            }

            Assert.That(redWalls, Has.Count.EqualTo(8),
                "The fixture should carry all eight authored red bars.");

            // The fixture only means something if the red bars still render next to the white wall.
            yield return SeekTo(222.25f);
            var redNormal = RenderPixelsAndSave(camera,
                PathUtils.Combine(shotDir, "salty-fixture-beat222.25-red-normal.png"));
            var redDisabled = new List<Renderer>();
            Texture2D redOff = null;
            try
            {
                foreach (var renderer in redWalls
                    .SelectMany(w => w.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r.enabled))
                {
                    renderer.enabled = false;
                    redDisabled.Add(renderer);
                }
                redOff = RenderPixelsAndSave(camera,
                    PathUtils.Combine(shotDir, "salty-fixture-beat222.25-red-off.png"));
            }
            finally
            {
                foreach (var renderer in redDisabled) renderer.enabled = true;
            }
            var redDelta = CountPixelDelta(redNormal.GetPixels32(), redOff.GetPixels32());
            Object.DestroyImmediate(redNormal);
            Object.DestroyImmediate(redOff);
            if (redDelta == 0)
                failures.Add("beat 222.25: disabling the eight red walls changed 0 pixels; " +
                    "the fixture does not reproduce the deployed wall stack.");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static Beatmap.Containers.ObstacleContainer FindWhiteFakeWall(ObstacleGridContainer walls)
        {
            var wall = walls.LoadedContainers.Values
                .OfType<Beatmap.Containers.ObstacleContainer>()
                .SingleOrDefault(c => c.ObstacleData.CustomFake
                    && Mathf.Approximately(c.ObstacleData.JsonTime, 221f)
                    && c.ObstacleData.CustomTrack != null
                    && c.ObstacleData.CustomTrack.IsArray
                    && c.ObstacleData.CustomTrack.Children.Any(child =>
                        child.IsString && child.Value == "1/4")
                    && c.ObstacleData.CustomTrack.Children.Any(child =>
                        child.IsString && child.Value == "pee2"));
            Assert.That(wall, Is.Not.Null,
                "No b=221 fakeObstacle with tracks [1/4, pee2] in the loaded containers.");
            return wall;
        }

        private static int CountPixelDelta(Color32[] a, Color32[] b)
        {
            var count = 0;
            for (var i = 0; i < a.Length; i++)
            {
                var delta = Mathf.Abs(a[i].r - b[i].r)
                    + Mathf.Abs(a[i].g - b[i].g)
                    + Mathf.Abs(a[i].b - b[i].b);
                if (delta >= 45) count++;
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

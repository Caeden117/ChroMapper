using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "Spells", mapped by Joetastic & Swifter (BeatSaver ID: 35a0b).
    // Portable reduction of the Spells b275 left-quad regression: the fixture already
    // retains the exact `light` material ({shader:TransparentLight}), the authored
    // modelScene0_solid1_0 Quad/lightID-2000 enhancement, and the b275 white event.
    // Same assertions as the full-map Beat275LeftSolidQuadRendersPhysicalSurfaceAndFog.
    public class SpellsBeat275BeamParityTest : TestBase
    {
        private static string OutputDir => PathUtils.Combine(
            Application.dataPath, "..", "TestResults", "SpellsBeat275");

        private bool animationsBeforeTest;
        private float playerFovBeforeTest;
        private float playerOffsetBeforeTest;
        private float cameraFovBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private static string FixturePath => Path.Combine(
            Application.dataPath, "Tests", "Fixtures", "SpellsLaserWallFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            playerFovBeforeTest = Settings.Instance.PlayerCameraFOV;
            playerOffsetBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            cameraFovBeforeTest = Settings.Instance.CameraFOV;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 150,
                environmentName: "BillieEnvironment",
                songLengthSeconds: 215);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator Beat275LeftSolidQuadRendersPhysicalSurfaceAndFog()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var camera = cameraManager.CameraControllers[1].Camera;
            var report = new StringBuilder();
            var run = System.DateTime.UtcNow.Ticks;
            Directory.CreateDirectory(OutputDir);

            atsc.MoveToJsonTime(274.75f);
            yield return null;
            yield return null;

            var target = Object.FindAnyObjectByType<GeometryGridContainer>()
                .LoadedContainers.Values
                .OfType<GeometryContainer>()
                .FirstOrDefault(c => c.EnvironmentEnhancement != null
                    && c.EnvironmentEnhancement.Track == "modelScene0_solid1_0");
            Assert.That(target, Is.Not.Null,
                "modelScene0_solid1_0 geometry was not generated — setup ambiguity.");
            var controller = target.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                .FirstOrDefault();
            Assert.That(controller, Is.Not.Null, "no controller under target geometry.");
            Assert.That(controller.BoxLight, Is.Not.Null, "target has no BoxLight.");
            var renderer = controller.BoxLight.Renderer;
            var fog = controller.BloomFog;
            Assert.That(renderer, Is.Not.Null, "target BoxLight has no Renderer.");
            Assert.That(fog, Is.Not.Null, "target has no BloomFog object.");

            Assert.That(controller.Color.a, Is.LessThanOrEqualTo(0.02f),
                $"light off before b275 expected alpha<=0.02, got {controller.Color.a}");

            atsc.MoveToJsonTime(275.05f);
            yield return null;
            yield return null;

            Assert.That(renderer.enabled && renderer.gameObject.activeInHierarchy, Is.True,
                "BoxLight renderer should be enabled and active at b275.05.");
            Assert.That(fog.enabled, Is.True, "BloomFog should be enabled at b275.05.");
            Assert.That(controller.Color.a, Is.GreaterThanOrEqualTo(0.4f),
                $"b275 flash should drive color alpha >=0.4, got {controller.Color.a}");

            var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;
            var rt = new RenderTexture(1024, 576, 24, format);
            var previousTarget = camera.targetTexture;
            var previousBoxEnabled = renderer.enabled;
            var previousFogEnabled = fog.enabled;
            Texture2D normal = null, boxOff = null, fogOff = null;
            var failures = new List<string>();
            try
            {
                camera.targetTexture = rt;
                normal = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                boxOff = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                fogOff = new Texture2D(1024, 576, TextureFormat.RGBA32, false);

                camera.Render();
                ReadRenderTexture(rt, normal);
                File.WriteAllBytes(
                    Path.Combine(OutputDir, $"spells-fixture275-parity-{run}-normal.png"),
                    normal.EncodeToPNG());

                renderer.enabled = false;
                camera.Render();
                ReadRenderTexture(rt, boxOff);
                File.WriteAllBytes(
                    Path.Combine(OutputDir, $"spells-fixture275-parity-{run}-boxoff.png"),
                    boxOff.EncodeToPNG());
                renderer.enabled = true;

                fog.enabled = false;
                camera.Render();
                ReadRenderTexture(rt, fogOff);
                File.WriteAllBytes(
                    Path.Combine(OutputDir, $"spells-fixture275-parity-{run}-fogoff.png"),
                    fogOff.EncodeToPNG());
                fog.enabled = previousFogEnabled;

                // The four region scans each re-walked every texel through GetPixel; snapshot each
                // capture once and fold the left-half + full-frame tallies into one pass per pair.
                var normalPixels = normal.GetPixels();
                var boxOffPixels = boxOff.GetPixels();
                var fogOffPixels = fogOff.GetPixels();
                CompareRegions(normalPixels, boxOffPixels, normal.width, normal.height,
                    out var boxFull, out var boxLeft);
                AppendRegion(report, "box-on vs box-off", boxLeft, "left-half");
                AppendRegion(report, "box-on vs box-off", boxFull, "full");
                var boxChanged = boxLeft.Count;
                CompareRegions(normalPixels, fogOffPixels, normal.width, normal.height,
                    out var fogFull, out var fogLeft);
                AppendRegion(report, "normal vs fog-off", fogFull, "full");
                AppendRegion(report, "normal vs fog-off", fogLeft, "left-half");
                var fogChanged = fogFull.Count;

                if (renderer.sharedMaterial.HasProperty("_CullMode"))
                    report.AppendLine($"material _CullMode={renderer.sharedMaterial.GetFloat("_CullMode")}");
                report.AppendLine($"camera pos={camera.transform.position} rot={camera.transform.eulerAngles} " +
                    $"fov={camera.fieldOfView} aspect={camera.aspect} allowHDR={camera.allowHDR}");

                if (boxChanged < 200)
                    failures.Add(
                        $"physical beam invisible in left half: box on/off changed {boxChanged} pixels, " +
                        "expected >=200 (back-facing Quad is culled).");
                if (fogChanged < 1000)
                    failures.Add(
                        $"fog bloom missing: fog on/off changed {fogChanged} pixels, expected >=1000.");
            }
            finally
            {
                renderer.enabled = previousBoxEnabled;
                fog.enabled = previousFogEnabled;
                camera.targetTexture = previousTarget;
                Object.Destroy(rt);
                if (normal != null) Object.Destroy(normal);
                if (boxOff != null) Object.Destroy(boxOff);
                if (fogOff != null) Object.Destroy(fogOff);
            }

            var reportPath = Path.Combine(OutputDir, $"spells-fixture275-parity-{run}-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[Spells275Fixture] parity report: {reportPath}");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private struct RegionStats
        {
            public int Count;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;

            public static RegionStats Create() => new()
            {
                MinX = int.MaxValue,
                MaxX = int.MinValue,
                MinY = int.MaxValue,
                MaxY = int.MinValue
            };

            public void Add(int x, int y)
            {
                Count++;
                if (x < MinX) MinX = x;
                if (x > MaxX) MaxX = x;
                if (y < MinY) MinY = y;
                if (y > MaxY) MaxY = y;
            }
        }

        // One linear pass over the flattened captures: the same delta predicate feeds both the
        // full-frame and left-half tallies, replacing the per-region GetPixel walks.
        private static void CompareRegions(
            Color[] a, Color[] b, int width, int height,
            out RegionStats full, out RegionStats left)
        {
            full = RegionStats.Create();
            left = RegionStats.Create();
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var ca = a[y * width + x];
                var cb = b[y * width + x];
                var delta = Mathf.RoundToInt(255f *
                    (Mathf.Abs(ca.r - cb.r) + Mathf.Abs(ca.g - cb.g) + Mathf.Abs(ca.b - cb.b)));
                if (delta < 24) continue;
                full.Add(x, y);
                if (x < width / 2)
                {
                    left.Add(x, y);
                }
            }
        }

        private static void AppendRegion(StringBuilder sb, string label, RegionStats stats, string region)
        {
            sb.AppendLine($"  delta {label} {region}: pixels>=24: {stats.Count} " +
                (stats.Count > 0
                    ? $"bbox x[{stats.MinX}..{stats.MaxX}] y[{stats.MinY}..{stats.MaxY}]"
                    : "bbox none"));
        }

        private static void ReadRenderTexture(RenderTexture source, Texture2D destination)
        {
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                destination.Apply();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = playerFovBeforeTest;
            Settings.Instance.PlayerCameraOffsetZ = playerOffsetBeforeTest;
            Settings.Instance.CameraFOV = cameraFovBeforeTest;
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

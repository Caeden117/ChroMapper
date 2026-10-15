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
    // TEMP machine-specific full-map diagnostic for the Spells beat-275 left white laser:
    // the authored b275 type-1 event drives lightID 2000 on the generated Quad
    // "modelScene0_solid1_0" (env idx49). Game shows the physical beam + fog bloom;
    // CM Playing reportedly shows only the fog. Diagnostic only — no parity assertions
    // beyond "the exact target exists"; A/B pixel counts are logged for lead review.
    [Explicit]
    public class SpellsBeat275FullMapParityTest : TestBase
    {
        private const string SourceMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/" +
            "35a0b (Spells - Joetastic & Swifter)/ExpertPlusStandard.dat";
        private const string OutputDir = "C:/Users/tdrak/AppData/Local/Temp/devin-spells275";

        private bool animationsBeforeTest;
        private float playerFovBeforeTest;
        private float playerOffsetBeforeTest;
        private float cameraFovBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

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
                JSON.Parse(File.ReadAllText(SourceMapPath)),
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

        // Behavioral regression for the user's report: the authored b275 white flash on
        // lightID 2000 should render a physical beam surface (left half) AND its fog bloom.
        // Was RED: the Quad's face was culled (shared material _CullMode=2, authored rotation
        // leaves the front normal away from camera) so box on/off changed 0 pixels while fog
        // toggling changed ~95k; fixed by GeometryAppearanceSO's cull-off material variant for
        // TransparentLight Quad geometry. Fog leg stays GREEN either way.
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

            // Before the b275 flash the authored light is off.
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
                    Path.Combine(OutputDir, $"spells-b275-parity-{run}-normal.png"),
                    normal.EncodeToPNG());

                renderer.enabled = false;
                camera.Render();
                ReadRenderTexture(rt, boxOff);
                File.WriteAllBytes(
                    Path.Combine(OutputDir, $"spells-b275-parity-{run}-boxoff.png"),
                    boxOff.EncodeToPNG());
                renderer.enabled = true;

                fog.enabled = false;
                camera.Render();
                ReadRenderTexture(rt, fogOff);
                File.WriteAllBytes(
                    Path.Combine(OutputDir, $"spells-b275-parity-{run}-fogoff.png"),
                    fogOff.EncodeToPNG());
                fog.enabled = previousFogEnabled;

                var boxChanged = CountRegion(report, "box-on vs box-off", normal, boxOff, 0, 512, "left-half");
                CountRegion(report, "box-on vs box-off", normal, boxOff, 0, 1024, "full");
                var fogChanged = CountRegion(report, "normal vs fog-off", normal, fogOff, 0, 1024, "full");
                CountRegion(report, "normal vs fog-off", normal, fogOff, 0, 512, "left-half");

                if (controller.BoxLight.Renderer.sharedMaterial.HasProperty("_CullMode"))
                    report.AppendLine($"material _CullMode={controller.BoxLight.Renderer.sharedMaterial.GetFloat("_CullMode")}");
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

            var reportPath = Path.Combine(OutputDir, $"spells-b275-parity-{run}-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[Spells275] parity report: {reportPath}");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // TEMP orientation check: the culling fix assumes the Quad's authored rotation reaches
        // the generated mesh unchanged (the single face is legitimately back-facing). Verify the
        // authored euler survives load and that the face really points away — rules a rotation
        // bug coexisting with the culled material.
        [UnityTest]
        public IEnumerator Beat275GeneratedQuadKeepsAuthoredRotation()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var camera = cameraManager.CameraControllers[1].Camera;
            var report = new StringBuilder();
            Directory.CreateDirectory(OutputDir);
            var run = System.DateTime.UtcNow.Ticks;

            var literal = new Vector3(-11.009871f, 62.107605f, -178.364471f);
            var target = Object.FindAnyObjectByType<GeometryGridContainer>()
                .LoadedContainers.Values
                .OfType<GeometryContainer>()
                .FirstOrDefault(c => c.EnvironmentEnhancement != null
                    && c.EnvironmentEnhancement.Track == "modelScene0_solid1_0");
            Assert.That(target, Is.Not.Null, "modelScene0_solid1_0 not generated.");
            var controller = target.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                .FirstOrDefault();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.BoxLight, Is.Not.Null);
            var renderer = controller.BoxLight.Renderer;
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh, Is.Not.Null);
            Assert.That(target.EnvironmentEnhancement.Rotation, Is.Not.Null,
                "enhancement has no authored Rotation.");

            foreach (var beat in new[] { 274.75f, 275.05f, 275.4f, 274.75f })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var authored = target.EnvironmentEnhancement.Rotation.Value;
                var worldRot = renderer.transform.rotation;
                var angle = Quaternion.Angle(worldRot, Quaternion.Euler(literal));
                var dot = Vector3.Dot(
                    renderer.transform.TransformDirection(mesh.normals[0]),
                    camera.transform.position - renderer.bounds.center);
                report.AppendLine($"beat {beat}: authoredEuler={authored} literal={literal} " +
                    $"actualWorldEuler={worldRot.eulerAngles} angleVsAuthored={angle:F3}deg " +
                    $"cameraPos={camera.transform.position} faceDotToCamera={dot:F3}");
                Assert.That(
                    Vector3.Distance(authored, literal), Is.LessThanOrEqualTo(0.01f),
                    $"beat {beat}: parsed authored rotation {authored} differs from source literal.");
                Assert.That(angle, Is.LessThanOrEqualTo(0.2f),
                    $"beat {beat}: generated Quad world rotation differs from authored euler by " +
                    $"{angle}deg — a parent/rotation transform bug may coexist with the cull fix.");
                Assert.That(dot, Is.LessThan(0f),
                    $"beat {beat}: expected the Quad face to be back-facing (dot<0), got {dot}.");
            }

            var reportPath = Path.Combine(OutputDir, $"spells-b275-orientation-{run}.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[Spells275] orientation report: {reportPath}");
        }

        private static void ReportDelta(StringBuilder sb, string label, Texture2D a, Texture2D b)
        {
            CountRegion(sb, label, a, b, 0, 1024, "full");
            CountRegion(sb, label, a, b, 0, 512, "left-half");
        }

        private static int CountRegion(
            StringBuilder sb, string label, Texture2D a, Texture2D b,
            int x0, int x1, string region)
        {
            var count = 0;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (var y = 0; y < 576; y++)
            for (var x = x0; x < x1; x++)
            {
                var ca = a.GetPixel(x, y);
                var cb = b.GetPixel(x, y);
                var delta = Mathf.RoundToInt(255f *
                    (Mathf.Abs(ca.r - cb.r) + Mathf.Abs(ca.g - cb.g) + Mathf.Abs(ca.b - cb.b)));
                if (delta < 24) continue;
                count++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            sb.AppendLine($"  delta {label} {region}: pixels>=24: {count} " +
                (count > 0 ? $"bbox x[{minX}..{maxX}] y[{minY}..{maxY}]" : "bbox none"));
            return count;
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

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
    // Fixture source: "Array", mapped by VoltageO, CMT (BeatSaver ID: 50222); fixture reduced from the local Kamikazi repro.
       // KamikaziLightArrayTest reproduces the reported "Kamikazi Light Level Repro 2" map: 4070 generated
    // ILightWithId geometry lights parked at z -10000 that fly in on per-track AnimateTrack events starting at
    // beat 14, inside a DefaultEnvironment whose vanilla Environment and GameCore are hidden by the map's own
    // enhancement entries. The array must spawn, stay parked before beat 14, and become visible after it.
    public class KamikaziLightArrayTest : TestBase
    {
        private const int ExpectedLightCount = 4070;

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int litId = Shader.PropertyToID("_AnimationSpawned");
        private bool? animationsBeforeTest;
        private float? playerFovBeforeTest;
        private float? playerOffsetBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "KamikaziLightArrayFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return LoadFixture();
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The 4070 generated lights and the fixture's map objects must survive between tests; the default
        // cleanup would delete every custom event and leave later seeks with nothing to drive.
        protected override void CleanupTestObjects()
        {
        }

        private static IEnumerator LoadFixture()
        {
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 202,
                environmentName: "DefaultEnvironment",
                songLengthSeconds: 80);
        }

        // GeneratedLightArraySpawnsParksAndFliesInAtBeat14 constrains the whole reported behavior: the array
        // exists, it is parked before its first event, and it leaves the parking position once the beat 14
        // AnimateTrack events run. The reported regression left the array not displaying at all.
        [UnityTest]
        public IEnumerator GeneratedLightArraySpawnsParksAndFliesInAtBeat14()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var lights = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Geometry != null)
                .ToList();

            Assert.That(
                lights,
                Has.Count.EqualTo(ExpectedLightCount),
                "The map's generated ILightWithId geometry did not all spawn.");

            var targets = lights
                .Select(container => container.Animator.LocalTarget)
                .Where(target => target != null)
                .ToList();
            Assert.That(
                targets,
                Has.Count.EqualTo(ExpectedLightCount),
                "Every generated light needs an animation target to fly in on.");

            atsc.MoveToJsonTime(0);
            yield return null;
            yield return null;
            // The map authors two groups: 3996 event-driven lights parked at z -10000 before beat 14, plus 74
            // static lights on one shared track (66 at their authored z 40 positions and 8 transform-less
            // inline-material triangles at the origin) that are always visible.
            var parkedAtStart = targets.Count(target => target.position.z < -9999f);
            Assert.That(
                parkedAtStart,
                Is.EqualTo(3996),
                "The event-driven generated lights were not parked at z -10000 before their beat-14 events.");
            var staticVisible = targets.Count(target => target.position.z > -9999f);
            Assert.That(
                staticVisible,
                Is.EqualTo(74),
                "The static generated lights were not at their authored visible positions.");

            // The map flies the parked lights in four waves and each wave's own duration parks it back at
            // (0, 0, -10000) before the next one: 1024 lights at beat 14 for 64 beats, 612 at beat 98 for 4
            // beats, 800 at beat 150 for 32 beats, and 1560 at beat 214 for 64 beats.
            // The production renderer list (MpbController.Renderers) holds each light's actual shape renderer,
            // not the prefab's disabled selection outline mesh.
            var brokenRenderers = lights
                .Where(container => container.MpbController.Renderers.Count == 0
                    || container.MpbController.Renderers.Any(renderer =>
                        renderer == null || !renderer.enabled || renderer.sharedMaterial == null))
                .ToList();
            Assert.That(
                brokenRenderers,
                Is.Empty,
                "A generated light had no enabled renderer with an assigned material. Samples: " +
                string.Join("; ", brokenRenderers.Take(5).Select(container =>
                    $"track='{container.EnvironmentEnhancement?.Track}' " +
                    $"material={container.EnvironmentEnhancement?.Geometry?["material"]}")));

            foreach (var (beat, visible) in new[]
                     {
                         (20f, 1024),
                         (100f, 612),
                         (152f, 800),
                         (216f, 1560),
                     })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var flownIn = targets.Count(target => target.position.z > -1000f);
                Assert.That(
                    flownIn,
                    Is.EqualTo(visible + 74),
                    $"Seeking to beat {beat} left {flownIn} generated lights visible instead of the expected " +
                    $"{visible + 74} ({visible} in active waves plus the 74 static lights); the light array is " +
                    "not displaying.");

                // The map's normal light events must actually light the flown-in lights: the TransparentLight
                // material renders nothing at zero alpha, so an unlit-but-positioned array is invisible to the
                // user even though every position and renderer flag checks out.
                var litLights = lights.Count(container =>
                {
                    var color = container.MpbController.Mpb.GetColor(colorId);
                    return color.a > 0.001f || container.MpbController.Mpb.GetFloat(litId) > 0.5f;
                });
                Assert.That(
                    litLights,
                    Is.GreaterThan(0),
                    $"No generated lights are lit at beat {beat}; the light events are not reaching the array.");
            }
        }

        // PlayingThroughTheMapRendersTheFirstWave reproduces the deployed-build report: running through the map
        // left none of the generated lights rendering even though large seeks worked. Real playback advances the
        // audio time continuously and pushes each track's animation once per frame; Jenkins tests cannot depend
        // on AudioSource playback, so this steps the time one frame at a time through the wave start, which
        // exercises the exact per-frame TrackAnimator.Update push and ObjectAnimator.LateUpdate apply streaming
        // playback uses.
        [UnityTest]
        public IEnumerator PlayingThroughTheMapRendersTheFirstWave()
        {
            // The teardown restores these because any failed assertion must not leave Playing mode active
            // across the next test's seek.
            animationsBeforeTest = Settings.Instance.Animations;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            Settings.Instance.Animations = true;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var targets = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Geometry != null)
                .Select(container => container.Animator.LocalTarget)
                .Where(target => target != null)
                .ToList();
            Assert.That(targets, Has.Count.EqualTo(ExpectedLightCount));

            // Run through beats 13.5 to 16 one frame per step, exactly like continuous playback time.
            for (var beat = 13.5f; beat <= 16f; beat += 0.05f)
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
            }

            var flownIn = targets.Count(target => target.position.z > -1000f);
            Assert.That(
                flownIn,
                Is.EqualTo(1024 + 74),
                $"Running through the start of the map left {flownIn} generated lights visible instead of the " +
                "expected 1098 (the beat-14 wave plus the static lights); the light array is not rendering " +
                "during playback.");
        }

        // LoadingAMapWhileInPlayingModeKeepsTheTimeControllerAlive pins the deployed-build root cause behind
        // "none of them are rendering": with a preview mode active, the 03_Mapper scene Start fired
        // OnTimeChanged into ObstacleGridContainer before any map data existed and RefreshWalls dereferenced
        // null sorted arrays; the exception aborted AudioTimeSyncController.Start after ResetTime, skipping
        // the audio clip assignment, the OnLevelLoaded subscription (so time never advanced again),
        // Initialized, and editor-state registration. Loading maps in playing mode must keep the time
        // controller alive and the light array animating.
        [UnityTest]
        public IEnumerator LoadingAMapWhileInPlayingModeKeepsTheTimeControllerAlive()
        {
            // Enter playing mode before the load, exactly like testing several maps without leaving preview.
            animationsBeforeTest = Settings.Instance.Animations;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            Settings.Instance.Animations = true;
            uiMode.SetUIMode(UIModeType.Playing, false);

            yield return LoadFixture();
            // The reload replaced the scene's map, so the shared baseline must point at this fixture instance
            // for the remaining tests' ResetSharedMapState to stay coherent.
            TestUtils.CaptureCurrentMapAsSharedBaseline();

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(
                atsc.Initialized,
                Is.True,
                "AudioTimeSyncController.Start aborted while loading in playing mode; without it the song time " +
                "never advances again and nothing renders during run-through.");

            atsc.MoveToJsonTime(20);
            yield return null;
            yield return null;
            var flownIn = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Geometry != null)
                .Select(container => container.Animator.LocalTarget)
                .Where(target => target != null)
                .Count(target => target.position.z > -1000f);
            Assert.That(
                flownIn,
                Is.EqualTo(1024 + 74),
                $"After loading in playing mode, seeking to beat 20 left {flownIn} generated lights visible " +
                "instead of the expected 1098.");
        }

        // BackFacingTransparentTriangleRendersLitSurface: the fixture's first wave contains lit
        // TransparentLight Triangles whose single face points away from the camera (s934 at b20).
        // The shared transparent-light material's back-face culling dropped them entirely, so a
        // lit, in-frustum beam drew nothing; fixed by GeometryAppearanceSO's cached cull-off
        // variant for TransparentLight Quad/Triangle, matching the game's single-sided planar
        // light surfaces. Same mechanism as
        // SpellsBeat275BeamParityTest.Beat275LeftSolidQuadRendersPhysicalSurfaceAndFog.
        [UnityTest]
        public IEnumerator BackFacingTransparentTriangleRendersLitSurface()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            playerFovBeforeTest = Settings.Instance.PlayerCameraFOV;
            playerOffsetBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 60f;
            Settings.Instance.PlayerCameraOffsetZ = -3.6f;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return null;

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var camera = cameraManager.CameraControllers[1].Camera;
            var outDir = PathUtils.Combine(
                Application.dataPath, "..", "TestResults", "KamikaziPlanar");
            Directory.CreateDirectory(outDir);
            var report = new StringBuilder();
            var run = System.DateTime.UtcNow.Ticks;

            atsc.MoveToJsonTime(0f);
            yield return null;
            yield return null;

            var target = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.EnvironmentEnhancement != null
                    && c.EnvironmentEnhancement.Track == "s934"
                    && c.EnvironmentEnhancement.Geometry != null
                    && c.EnvironmentEnhancement.Geometry["type"].Value == "Triangle"
                    && c.EnvironmentEnhancement.Geometry["material"].Value == "tp");
            Assert.That(target, Is.Not.Null,
                "generated Triangle track=s934 was not spawned — fixture/setup ambiguity.");
            var controller = target.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                .FirstOrDefault();
            Assert.That(controller, Is.Not.Null, "s934 has no light controller.");
            Assert.That(controller.BoxLight, Is.Not.Null, "s934 has no BoxLight.");
            var renderer = controller.BoxLight.Renderer;
            var fog = controller.BloomFog;
            Assert.That(renderer, Is.Not.Null, "s934 BoxLight has no Renderer.");
            Assert.That(fog, Is.Not.Null, "s934 has no BloomFog object.");
            Assert.That(controller.Color.a, Is.LessThanOrEqualTo(0.02f),
                $"s934 should be unlit at b0, got alpha {controller.Color.a}");

            atsc.MoveToJsonTime(20f);
            yield return null;
            yield return null;

            Assert.That(controller.Color.a, Is.GreaterThanOrEqualTo(0.05f),
                $"s934 should be lit at b20, got alpha {controller.Color.a}");
            Assert.That(renderer.enabled && renderer.gameObject.activeInHierarchy, Is.True,
                "s934 renderer should be enabled and active at b20.");
            Assert.That(fog.enabled, Is.True, "s934 BloomFog should be enabled at b20.");
            Assert.That(
                GeometryUtility.TestPlanesAABB(
                    GeometryUtility.CalculateFrustumPlanes(camera), renderer.bounds),
                Is.True, "s934 bounds should intersect the camera frustum at b20.");
            var viewportCenter = camera.WorldToViewportPoint(renderer.bounds.center);
            Assert.That(viewportCenter.x, Is.InRange(0f, 1f));
            Assert.That(viewportCenter.y, Is.InRange(0f, 1f));
            // Setup evidence: the single triangle face points away from the camera, so the
            // shared material's back-face culling discards the whole lit surface.
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh, Is.Not.Null);
            var worldNormal = renderer.transform.TransformDirection(mesh.normals[0]);
            Assert.That(
                Vector3.Dot(worldNormal, camera.transform.position - renderer.bounds.center),
                Is.LessThan(0f), "s934 should be back-facing at b20 (known culled case).");

            var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;
            var rt = new RenderTexture(1024, 576, 24, format);
            var previousTarget = camera.targetTexture;
            var previousEnabled = renderer.enabled;
            Texture2D on = null, off = null;
            var failures = new List<string>();
            try
            {
                camera.targetTexture = rt;
                on = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                off = new Texture2D(1024, 576, TextureFormat.RGBA32, false);

                camera.Render();
                ReadTexture(rt, on);
                File.WriteAllBytes(
                    Path.Combine(outDir, $"kamikazi-s934-{run}-on.png"), on.EncodeToPNG());
                renderer.enabled = false;
                camera.Render();
                ReadTexture(rt, off);
                File.WriteAllBytes(
                    Path.Combine(outDir, $"kamikazi-s934-{run}-off.png"), off.EncodeToPNG());

                var changed = CountDelta(report, "renderer on vs off", on, off, 0, 1024, 0, 576, "full");
                if (changed < 40)
                    failures.Add(
                        $"lit back-facing Triangle s934 rendered {changed} pixels (expected >=40); " +
                        "the shared TransparentLight material culls its only face.");
            }
            finally
            {
                renderer.enabled = previousEnabled;
                camera.targetTexture = previousTarget;
                Object.Destroy(rt);
                if (on != null) Object.Destroy(on);
                if (off != null) Object.Destroy(off);
            }

            var reportPath = Path.Combine(outDir, $"kamikazi-s934-{run}-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[KamikaziPlanar] report: {reportPath}");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void ReadTexture(RenderTexture source, Texture2D destination)
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

        private static int CountDelta(
            StringBuilder sb, string label, Texture2D a, Texture2D b,
            int x0, int x1, int y0, int y1, string region)
        {
            var count = 0;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (var y = y0; y <= y1 && y < 576; y++)
            for (var x = x0; x <= x1 && x < 1024; x++)
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
            sb.AppendLine($"  delta {label} {region} x[{x0}..{x1}] y[{y0}..{y1}]: " +
                $"pixels>=24: {count} " +
                (count > 0 ? $"bbox x[{minX}..{maxX}] y[{minY}..{maxY}]" : "bbox none"));
            return count;
        }

        // Restore the editing mode per test so a failed assertion cannot leave Playing mode active across the
        // next case (KamikaziLightArrayTest's original run hung the whole runner that way); the fixture map
        // itself stays loaded and the empty-map restore happens once in the class teardown.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            // Re-find the mode objects: the playing-mode load test reloads mid-test, so the captured refs can
            // be destroyed scene objects whose fake-null check would silently skip this restore.
            var currentUiMode = uiMode != null ? uiMode : Object.FindAnyObjectByType<UIMode>();
            if (currentUiMode != null)
            {
                currentUiMode.SetUIMode(UIModeType.Normal, false);
            }

            var currentCameraManager = cameraManager != null
                ? cameraManager
                : Object.FindAnyObjectByType<CameraManager>();
            if (currentCameraManager != null)
            {
                currentCameraManager.SelectCamera(CameraType.Editing);
            }

            if (animationsBeforeTest.HasValue)
            {
                Settings.Instance.Animations = animationsBeforeTest.Value;
                animationsBeforeTest = null;
            }
            if (playerFovBeforeTest.HasValue)
            {
                Settings.Instance.PlayerCameraFOV = playerFovBeforeTest.Value;
                playerFovBeforeTest = null;
            }
            if (playerOffsetBeforeTest.HasValue)
            {
                Settings.Instance.PlayerCameraOffsetZ = playerOffsetBeforeTest.Value;
                playerOffsetBeforeTest = null;
            }

            yield break;
        }

        // Restore the canonical empty shared map once per class so later fixtures do not inherit the
        // 4070-light fixture.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

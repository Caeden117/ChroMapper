using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
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

        // CompleteMapEndingCubesKeepInternalPlusVisible: the user's paired screenshot shows the
        // beat-214 end cube wave washing to white in CM and hiding the enclosed white 3D plus
        // (three thin 'tp' bars at the same center) that stays visible in game. Full-map only:
        // the shipped file's real event stream drives the red type-1 color inheritance and the
        // b214.031 white flip, which a trimmed fixture would risk losing. Explicit because it
        // reloads the complete map and captures renders.
        [Explicit]
        [UnityTest]
        public IEnumerator CompleteMapEndingCubesKeepInternalPlusVisible()
        {
            const string fullMapPath =
                "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomWIPLevels/" +
                "Kamikazi Light Level Repro 2/ExpertPlusStandard.dat";
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(fullMapPath)),
                beatsPerMinute: 202,
                environmentName: "DefaultEnvironment",
                songLengthSeconds: 100);
            animationsBeforeTest = Settings.Instance.Animations;
            playerFovBeforeTest = Settings.Instance.PlayerCameraFOV;
            playerOffsetBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var previousBloom = Settings.Instance.Bloom;
            var previousBloomFog = Settings.Instance.BloomFog;
            var shotDir = Path.Combine(Application.temporaryCachePath, "KamikaziEnding",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            Directory.CreateDirectory(shotDir);
            var failures = new List<string>();

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var camera = cameraManager.CameraControllers[1].Camera;
            var previousPosition = camera.transform.position;
            var previousRotation = camera.transform.rotation;
            var previousFov = camera.fieldOfView;
            // The capture format must match the PLAYING camera, so the target is created after
            // the mode switch below rather than from the editing camera's allowHDR state.
            RenderTexture rt = null;
            var previousTarget = camera.targetTexture;
            var renderTextures = new List<Texture2D>();
            var materialClones = new List<Material>();
            Camera.CameraCallback preRender = null;

            Texture2D Capture(string name)
            {
                camera.Render();
                var tex = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                renderTextures.Add(tex);
                ReadTexture(rt, tex);
                File.WriteAllBytes(Path.Combine(shotDir, name), tex.EncodeToPNG());
                return tex;
            }

            try
            {
                Settings.Instance.Animations = true;
                Settings.Instance.PlayerCameraFOV = 90f;
                Settings.Instance.PlayerCameraOffsetZ = 0f;
                Settings.ApplyOptionByName("Bloom", true);
                Settings.ApplyOptionByName("BloomFog", true);
                uiMode.SetUIMode(UIModeType.Playing, false);
                cameraManager.SelectCamera(CameraType.Playing);
                var cameraFormat = camera.allowHDR
                    ? RenderTextureFormat.DefaultHDR
                    : RenderTextureFormat.ARGB32;
                rt = new RenderTexture(1024, 576, 24, cameraFormat);
                Debug.Log($"[KamikaziEnding] playing camera allowHDR={camera.allowHDR} " +
                    $"captureFormat={cameraFormat}");
                camera.fieldOfView = 90f;
                camera.transform.position = new Vector3(0f, 1.65f, 0f);
                camera.transform.rotation = Quaternion.identity;
                // Keep the capture target assigned while any world-to-viewport math runs so
                // projections use the 1024x576 capture aspect instead of the GameView's.
                camera.targetTexture = rt;

                // Record the live color pipeline while this camera actually draws: after-Render
                // reads can miss the BloomFog keyword the pass resets.
                preRender = cam =>
                {
                    if (cam != camera) return;
                    Debug.Log("[KamikaziEnding] onPreRender: " +
                        $"BLOOM_FOG={Shader.IsKeywordEnabled("BLOOM_FOG")} " +
                        $"POST_BLOOM={Shader.IsKeywordEnabled("POST_BLOOM")} " +
                        $"fogAtten={Shader.GetGlobalFloat("_CustomFogAttenuation")} " +
                        $"fogOffset={Shader.GetGlobalFloat("_CustomFogOffset")} " +
                        $"heightStartY={Shader.GetGlobalFloat("_CustomFogHeightFogStartY")} " +
                        $"heightH={Shader.GetGlobalFloat("_CustomFogHeightFogHeight")}");
                };
                Camera.onPreRender += preRender;

                var alphaWidthId = Shader.PropertyToID("_AlphaWidth");
                var mpb = new MaterialPropertyBlock();
                var firstBeat = true;
                // One capture/assert pass per sample: stopped seeks call this with phase null
                // (original filenames); the playback sample labels its PNGs with "playback".
                void SampleCandidate(float beat, string phase)
                {
                    camera.transform.position = new Vector3(0f, 1.65f, 0f);
                    camera.transform.rotation = Quaternion.identity;
                    camera.fieldOfView = 90f;
                    var pngPrefix = string.IsNullOrEmpty(phase) ? "" : phase + "-";

                    var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                    var geometryContainers = Object.FindObjectsByType<GeometryContainer>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(c => c.EnvironmentEnhancement?.Geometry != null
                            && c.EnvironmentEnhancement.Geometry["type"].Value == "Cube"
                            && c.EnvironmentEnhancement.Geometry["material"].Value == "tp")
                        .ToList();
                    Renderer ShapeRenderer(GeometryContainer c) =>
                        c.MpbController.Renderers.FirstOrDefault(r => r != null);

                    var candidates = new List<(GeometryContainer Cube, Renderer Renderer,
                        List<(GeometryContainer Bar, Renderer Renderer)> Bars, Rect Pixels)>();
                    foreach (var cube in geometryContainers)
                    {
                        var scale = cube.EnvironmentEnhancement.Scale;
                        if (!scale.HasValue
                            || Mathf.Abs(scale.Value.x - 1.2f) > 0.01f
                            || Mathf.Abs(scale.Value.y - 1.2f) > 0.01f
                            || Mathf.Abs(scale.Value.z - 1.2f) > 0.01f) continue;
                        var renderer = ShapeRenderer(cube);
                        if (renderer == null || !renderer.enabled
                            || !renderer.gameObject.activeInHierarchy) continue;
                        if (!GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) continue;
                        var center = camera.WorldToViewportPoint(renderer.bounds.center);
                        if (center.z <= camera.nearClipPlane
                            || center.x < 0.12f || center.x > 0.88f
                            || center.y < 0.12f || center.y > 0.88f) continue;

                        var bars = new List<(GeometryContainer, Renderer)>();
                        foreach (var bar in geometryContainers)
                        {
                            if (ReferenceEquals(bar, cube)) continue;
                            var bs = bar.EnvironmentEnhancement.Scale;
                            if (!bs.HasValue || !IsThinBarScale(bs.Value)) continue;
                            var barRenderer = ShapeRenderer(bar);
                            if (barRenderer == null) continue;
                            if (Vector3.Distance(barRenderer.bounds.center,
                                    renderer.bounds.center) >= 0.05f) continue;
                            bars.Add((bar, barRenderer));
                        }
                        if (bars.Count < 3) continue;

                        var min = new Vector2(float.MaxValue, float.MaxValue);
                        var max = new Vector2(float.MinValue, float.MinValue);
                        var b = renderer.bounds;
                        for (var i = 0; i < 8; i++)
                        {
                            var corner = camera.WorldToViewportPoint(new Vector3(
                                (i & 1) == 0 ? b.min.x : b.max.x,
                                (i & 2) == 0 ? b.min.y : b.max.y,
                                (i & 4) == 0 ? b.min.z : b.max.z));
                            min = Vector2.Min(min, corner);
                            max = Vector2.Max(max, corner);
                        }
                        var rect = new Rect(min.x * 1024f, min.y * 576f,
                            (max.x - min.x) * 1024f, (max.y - min.y) * 576f);
                        if (rect.width < 12f || rect.height < 12f) continue;
                        candidates.Add((cube, renderer, bars, rect));
                    }

                    var chosen = candidates
                        .OrderByDescending(c => c.Pixels.width * c.Pixels.height)
                        .FirstOrDefault();
                    if (chosen.Cube == null)
                    {
                        failures.Add($"beat {beat}: no enclosing 1.2-scale 'tp' cube with three " +
                            "co-centered thin bars on screen (precondition, not proof).");
                        return;
                    }
                    var chosenCube = chosen.Cube;
                    var cubeRenderer = chosen.Renderer;
                    var chosenBars = chosen.Bars;
                    var roi = chosen.Pixels;

                    // The report describes a trio of bars at the cube center; a different count
                    // or a disabled bar is a precondition problem, not proof the plus is hidden.
                    if (chosenBars.Count != 3)
                        failures.Add($"beat {beat}: {chosenBars.Count} thin bars share the " +
                            "cube center, expected exactly 3 (precondition).");
                    foreach (var (bar, barRenderer) in chosenBars)
                        if (!barRenderer.enabled || !barRenderer.gameObject.activeInHierarchy)
                            failures.Add($"beat {beat}: bar {bar.EnvironmentEnhancement.Track} " +
                                "renderer is not active/enabled (precondition).");

                    var controller = chosenCube.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                        .FirstOrDefault(l => l.BoxLight != null);
                    var mat = cubeRenderer.sharedMaterial;
                    cubeRenderer.GetPropertyBlock(mpb);
                    var cubeMpbAlpha = mpb.GetColor(colorId).a;
                    Debug.Log($"[KamikaziEnding] beat={beat} cube track={chosenCube.EnvironmentEnhancement.Track} " +
                        $"lightID={(controller == null ? "none" : controller.ID.ToString())} " +
                        $"worldPos={cubeRenderer.bounds.center} roi={roi} " +
                        $"material={mat.name} shader={mat.shader.name} " +
                        $"keywords={string.Join(",", mat.shaderKeywords)} " +
                        $"cull={MaterialFloat(mat, "_CullMode")} " +
                        $"blendSrc={MaterialFloat(mat, "_BlendModeSrc")} " +
                        $"blendDst={MaterialFloat(mat, "_BlendModeDst")} " +
                        $"blendSrcA={MaterialFloat(mat, "_BlendModeSrcA")} " +
                        $"colorAlphaMul={(controller == null ? "none" : controller.ColorAlphaMultiplier.ToString())} " +
                        $"bloomFogMul={(controller == null ? "none" : controller.BloomFogIntensityMultiplier.ToString())} " +
                        $"boxAlphaMul={(controller == null || controller.BoxLight == null ? "none" : controller.BoxLight.AlphaMultiplier.ToString())} " +
                        $"mpbColor={mpb.GetColor(colorId)} mpbAlphaWidth={mpb.GetVector(alphaWidthId)}");
                    foreach (var (bar, barRenderer) in chosenBars.Take(3))
                    {
                        var barController = bar
                            .GetComponentsInChildren<ParametricBloomFogLightController>(true)
                            .FirstOrDefault();
                        barRenderer.GetPropertyBlock(mpb);
                        Debug.Log($"[KamikaziEnding] beat={beat} bar track={bar.EnvironmentEnhancement.Track} " +
                            $"lightID={(barController == null ? "none" : barController.ID.ToString())} " +
                            $"center={barRenderer.bounds.center} enabled={barRenderer.enabled} " +
                            $"material={barRenderer.sharedMaterial.name} " +
                            $"mpbColor={mpb.GetColor(colorId)}");
                    }

                    var baseline = Capture($"kamikazi-ending-{pngPrefix}b{beat}-baseline.png");
                    var barFlags = chosenBars.Select(entry => entry.Renderer.enabled).ToArray();
                    Texture2D plusOff;
                    try
                    {
                        foreach (var entry in chosenBars)
                            entry.Renderer.enabled = false;
                        plusOff = Capture($"kamikazi-ending-{pngPrefix}b{beat}-plusoff.png");
                    }
                    finally
                    {
                        for (var i = 0; i < chosenBars.Count; i++)
                            chosenBars[i].Renderer.enabled = barFlags[i];
                    }

                    // Central 70% of the projected cube: the plus sits inside it, so disabling
                    // the three bar renderers must change pixels there if the plus is visible.
                    var x0 = Mathf.Max(0, (int)(roi.x + roi.width * 0.15f));
                    var x1 = Mathf.Min(1023, (int)(roi.x + roi.width * 0.85f));
                    var y0 = Mathf.Max(0, (int)(roi.y + roi.height * 0.15f));
                    var y1 = Mathf.Min(575, (int)(roi.y + roi.height * 0.85f));
                    var changed = 0;
                    var nearWhite = 0;
                    var maskPixels = 0;
                    for (var y = y0; y <= y1; y++)
                    for (var x = x0; x <= x1; x++)
                    {
                        maskPixels++;
                        var on = baseline.GetPixel(x, y);
                        var off = plusOff.GetPixel(x, y);
                        var diff = Mathf.Max(
                            Mathf.Abs(on.r - off.r),
                            Mathf.Max(Mathf.Abs(on.g - off.g), Mathf.Abs(on.b - off.b)));
                        if (diff * 255f >= 8f) changed++;
                        if (on.r >= 0.95f && on.g >= 0.95f && on.b >= 0.95f) nearWhite++;
                    }
                    Debug.Log($"[KamikaziEnding] beat={beat} roi=[{x0}..{x1}]x[{y0}..{y1}] " +
                        $"maskPixels={maskPixels} changed={changed} nearWhite={nearWhite} " +
                        $"captures={shotDir}");

                    // Native input: DefaultEnvironment's type-1 event value 5 reads the scene
                    // EnvLightColor1Normal asset multiplierColor (1,1,1,0.7490196), so the cubes
                    // should render red at alpha .7490196 — not the steady alpha 1 that CM's
                    // preview color path preserves. The plus bars deliberately bypass the
                    // colored multiplier (value 9) and stay authored-white.
                    if (Mathf.Abs(cubeMpbAlpha - 0.7490196f) > 0.0001f)
                        failures.Add($"beat {beat}: cube MPB _Color.a=" +
                            $"{cubeMpbAlpha}, expected 0.7490196 from the " +
                            "scene's EnvLightColor1Normal multiplierColor.");

                    if (maskPixels < 20)
                        failures.Add($"beat {beat}: projected cube ROI only {maskPixels} px " +
                            "(<20), too small to prove the plus.");
                    if (changed < 10)
                        failures.Add($"beat {beat}: disabling the three enclosed bars changed " +
                            $"{changed} pixels inside the cube's projected center (<10); the " +
                            "white plus is not visible through the cube surface.");

                    if (firstBeat)
                    {
                        firstBeat = false;
                        // MPB-only control: the same frame with the cube's alpha set to the
                        // native .7490196 isolates whether the authored-asset alpha alone
                        // restores the plus; this is a contrast render, not a proposed fix.
                        var savedMpb = new MaterialPropertyBlock();
                        cubeRenderer.GetPropertyBlock(savedMpb);
                        cubeRenderer.GetPropertyBlock(mpb); // refill: the bar loop reused mpb
                        var nativeColor = mpb.GetColor(colorId);
                        nativeColor.a = 0.7490196f;
                        var savedBarFlags = chosenBars.Select(entry => entry.Renderer.enabled).ToArray();
                        try
                        {
                            mpb.SetColor(colorId, nativeColor);
                            cubeRenderer.SetPropertyBlock(mpb);
                            var nativeOn = Capture($"kamikazi-ending-{pngPrefix}b{beat}-native-alpha.png");
                            foreach (var entry in chosenBars)
                                entry.Renderer.enabled = false;
                            var nativeOff = Capture($"kamikazi-ending-{pngPrefix}b{beat}-native-alpha-plusoff.png");
                            var nativeChanged = 0;
                            var nativeNearWhite = 0;
                            for (var y = y0; y <= y1; y++)
                            for (var x = x0; x <= x1; x++)
                            {
                                var on = nativeOn.GetPixel(x, y);
                                var off = nativeOff.GetPixel(x, y);
                                var diff = Mathf.Max(
                                    Mathf.Abs(on.r - off.r),
                                    Mathf.Max(Mathf.Abs(on.g - off.g), Mathf.Abs(on.b - off.b)));
                                if (diff * 255f >= 8f) nativeChanged++;
                                if (on.r >= 0.95f && on.g >= 0.95f && on.b >= 0.95f) nativeNearWhite++;
                            }
                            Debug.Log($"[KamikaziEnding] beat={beat} native-alpha ROI: " +
                                $"changed={nativeChanged} nearWhite={nativeNearWhite} " +
                                $"(baseline changed={changed} nearWhite={nearWhite})");
                        }
                        finally
                        {
                            for (var i = 0; i < chosenBars.Count; i++)
                                chosenBars[i].Renderer.enabled = savedBarFlags[i];
                            cubeRenderer.SetPropertyBlock(savedMpb);
                        }

                        // Diagnostic probes only: alpha-source blend and back-face cull are
                        // contrast renders to localize the wash-out, not proposed fixes.
                        var original = cubeRenderer.sharedMaterial;
                        var zeroSrcAlpha = new Material(original);
                        materialClones.Add(zeroSrcAlpha);
                        zeroSrcAlpha.SetFloat("_BlendModeSrcA", (float)UnityEngine.Rendering.BlendMode.Zero);
                        try
                        {
                            cubeRenderer.sharedMaterial = zeroSrcAlpha;
                            Capture($"kamikazi-ending-{pngPrefix}b{beat}-blendSrcA-zero.png");
                        }
                        finally
                        {
                            cubeRenderer.sharedMaterial = original;
                        }

                        var cullBack = new Material(original);
                        materialClones.Add(cullBack);
                        cullBack.SetFloat("_CullMode", (float)UnityEngine.Rendering.CullMode.Back);
                        try
                        {
                            cubeRenderer.sharedMaterial = cullBack;
                            Capture($"kamikazi-ending-{pngPrefix}b{beat}-cull-back.png");
                        }
                        finally
                        {
                            cubeRenderer.sharedMaterial = original;
                        }
                    }
                }

                foreach (var beat in new[] { 214.125f, 216f, 214.125f })
                {
                    atsc.MoveToJsonTime(beat);
                    yield return null;
                    yield return null;
                    SampleCandidate(beat, null);
                }

                // Fourth sample under real playback: the isPlaying event path differs from
                // stopped seeks, so enter Playing with the audio source stopped and step the
                // pinned clock through the same window. The authored cube alpha is constant
                // .7490196 across 213.75-214.125 (red b151, bars flip white at 214.031), so the
                // same candidate/alpha/plus assertions apply while IsPlaying. The dedicated
                // virtual mouse keeps the toggle off physical input.
                InputTestFixture inputFixture = null;
                Mouse virtualMouse = null;
                var currentSecondsProperty = typeof(AudioTimeSyncController)
                    .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
                Assert.That(currentSecondsProperty, Is.Not.Null,
                    "AudioTimeSyncController.CurrentSeconds property missing.");
                try
                {
                    inputFixture = new InputTestFixture();
                    inputFixture.Setup();
                    virtualMouse = InputSystem.AddDevice<Mouse>();

                    atsc.MoveToJsonTime(213.75f);
                    atsc.TogglePlaying();
                    atsc.SongAudioSource.Stop();
                    atsc.StopScheduled = true;
                    yield return null;
                    yield return null;

                    for (var beat = 213.75f; beat <= 214.125f; beat += 1f / 64f)
                    {
                        PinClock(atsc, currentSecondsProperty, beat);
                        yield return null;
                    }
                    Assert.That(atsc.IsPlaying, Is.True,
                        "The playback sample must run while IsPlaying is true.");
                    Assert.That(atsc.SongAudioSource.isPlaying, Is.False,
                        "The playback sample relies on the pinned clock, not the audio source.");
                    Debug.Log($"[KamikaziEnding] playback sample at " +
                        $"CurrentJsonTime={atsc.CurrentJsonTime} IsPlaying={atsc.IsPlaying}");
                    SampleCandidate(atsc.CurrentJsonTime, "playback");
                }
                finally
                {
                    atsc.StopScheduled = false;
                    if (atsc.IsPlaying) atsc.CancelPlaying();
                    virtualMouse = null;
                    if (inputFixture != null) inputFixture.TearDown();
                }
            }
            finally
            {
                if (preRender != null) Camera.onPreRender -= preRender;
                camera.targetTexture = previousTarget;
                camera.transform.position = previousPosition;
                camera.transform.rotation = previousRotation;
                camera.fieldOfView = previousFov;
                Settings.ApplyOptionByName("Bloom", previousBloom);
                Settings.ApplyOptionByName("BloomFog", previousBloomFog);
                Object.Destroy(rt);
                foreach (var tex in renderTextures) Object.Destroy(tex);
                foreach (var clone in materialClones) Object.Destroy(clone);
            }

            Debug.Log($"[KamikaziEnding] captures in {shotDir}");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static bool IsThinBarScale(Vector3 scale)
        {
            var axes = new[] { Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z) };
            var wide = axes.Count(a => Mathf.Abs(a - 1f) < 0.01f);
            var thin = axes.Count(a => Mathf.Abs(a - 0.05f) < 0.01f);
            return wide == 1 && thin == 2;
        }

        private static string MaterialFloat(Material material, string property) =>
            material.HasProperty(property) ? material.GetFloat(property).ToString("0.###") : "n/a";

        // Pin the song clock deterministically while playing: the audio source is stopped, so
        // CurrentSeconds is set directly (the same idiom SaltyFullMapParityTest uses).
        private static void PinClock(
            AudioTimeSyncController atsc, PropertyInfo currentSecondsProperty, float jsonBeat)
        {
            var songBeat = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonBeat);
            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat));
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

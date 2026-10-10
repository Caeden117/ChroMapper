using System.Collections;
using System.Collections.Generic;
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
    // Salty opening spheres reduced to fixture size: the `hehe`/`hehe2` generated spheres are
    // parented to `die` and banished to scale0 at (0,-6969,-6969) by the b0 custom events.
    // The user's Info.dat keeps EditingMode.BasicEvent; entering Playing from that workspace
    // deactivates Gameplay Container Tracks and the full-map test showed both sphere animators
    // permanently detached from their tracks. Fixture preserves authored relative order.
    public class SaltyOpeningSphereParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;
        private EditingMode previousEditingMode;
        private EditModeContext editMode;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "SaltyOpeningSpheresFixture.json");

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
            editMode = Object.FindAnyObjectByType<EditModeContext>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // Deployed-workspace reproduction of the detachment: the live log shows hehe2 scale16
        // with track=<none> under AnimationMode=True. The spheres must stay attached to their
        // tracks so the die banish keeps them out of the playing frustum at every beat.
        [UnityTest]
        public IEnumerator BasicEventToPlayingKeepsBanishedSpheresAttached()
        {
            ApplyDeployedCameraSettings();
            Assert.That(editMode, Is.Not.Null, "No EditModeContext in the loaded scene.");
            previousEditingMode = editMode.EditingMode;
            var failures = new List<string>();
            try
            {
                editMode.EditingMode = EditingMode.BasicEvent;
                yield return null; // let UpdateGrid deactivate the gameplay track container
                uiMode.SetUIMode(UIModeType.Playing, false);
                cameraManager.SelectCamera(CameraType.Playing);
                var camera = cameraManager.CameraControllers[1].Camera;
                if (!UIMode.AnimationMode)
                    failures.Add("UIMode.AnimationMode was false in Playing; the test did not " +
                        "match the deployed preview state.");
                foreach (var beat in new[] { 0.1f, 6f, 0.1f })
                {
                    yield return SeekTo(beat);
                    InspectSphereState(camera, beat, failures);
                }
            }
            finally
            {
                editMode.EditingMode = previousEditingMode;
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Same workspace path, but seeded from the LIVE-observed state: the deployed log showed
        // each sphere's immediate Parent proxy at localScale1 with hehe2 at worldScale16 when
        // entering Playing. AssignTrackParent pushes the `die` animation onto the CHILD tracks'
        // ObjectParentTransform proxies (hehe/hehe2's Parent nodes) — die.Track has no animator
        // of its own — so the banish to assert lives on those proxies, not dieParent.
        [UnityTest]
        public IEnumerator BasicEventToPlayingAppliesDieBanishFromVisibleStart()
        {
            ApplyDeployedCameraSettings();
            Assert.That(editMode, Is.Not.Null, "No EditModeContext in the loaded scene.");
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(tracks, Is.Not.Null, "No TracksManager in the loaded scene.");
            previousEditingMode = editMode.EditingMode;
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            Assert.That(geometry, Is.Not.Null, "No GeometryGridContainer in the loaded scene.");
            var sphereContainers = geometry.LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>()
                .Where(c => c.EnvironmentEnhancement.Track == "hehe"
                    || c.EnvironmentEnhancement.Track == "hehe2")
                .ToList();
            var authored = new Dictionary<string, (Vector3 position, Vector3 scale)>
            {
                ["hehe"] = (new Vector3(0f, 1f, 10f), Vector3.one * 7f),
                ["hehe2"] = (new Vector3(0f, 0.37f, 20f), Vector3.one * 16f),
            };
            var originals = sphereContainers
                .SelectMany(c => new[] { c.transform, c.transform.parent }.Where(t => t != null))
                .Distinct()
                .Select(t => (t, t.localPosition, t.localScale)).ToList();
            var failures = new List<string>();
            try
            {
                // Baseline: before the workspace switch the proxies must already carry the
                // banish, or the seeded visible state below isn't discriminating.
                foreach (var container in sphereContainers)
                {
                    var proxy = container.transform.parent;
                    var proxyText = proxy == null ? "<none>" : $"pos={proxy.position} scale={proxy.lossyScale}";
                    Debug.Log($"[SaltyDiag] baseline {container.EnvironmentEnhancement.Track} " +
                        $"proxy {proxyText}");
                    if (proxy == null || proxy.lossyScale.magnitude > 0.5f
                        || proxy.position.magnitude < 6000f)
                        failures.Add($"baseline: {container.EnvironmentEnhancement.Track} parent " +
                            $"proxy was not banished before the workspace switch ({proxyText}).");
                }

                editMode.EditingMode = EditingMode.BasicEvent;
                yield return null; // let UpdateGrid deactivate the gameplay track container
                foreach (var container in sphereContainers)
                {
                    var (position, scale) = authored[container.EnvironmentEnhancement.Track];
                    container.transform.localPosition = position;
                    container.transform.localScale = scale;
                    if (container.transform.parent != null)
                    {
                        container.transform.parent.localPosition = Vector3.zero;
                        container.transform.parent.localScale = Vector3.one;
                    }
                }
                var camera = cameraManager.CameraControllers[1].Camera;
                // Seed check BEFORE entering Playing: hehe2 must already sit at the live-visible
                // scale16/in-frustum state so the RED below is a genuine repro.
                var hehe2 = sphereContainers
                    .SingleOrDefault(c => c.EnvironmentEnhancement.Track == "hehe2");
                Assert.That(hehe2, Is.Not.Null, "No hehe2 container in the loaded scene.");
                var seedPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
                var seedRenderer = hehe2.GetComponentInChildren<MeshRenderer>();
                var seedVisible = seedRenderer != null
                    && GeometryUtility.TestPlanesAABB(seedPlanes, seedRenderer.bounds);
                Debug.Log($"[SaltyDiag] pre-Playing seeded hehe2 lossyScale={hehe2.transform.lossyScale} " +
                    $"rendererInFrustum={seedVisible}");
                if (hehe2.transform.lossyScale.magnitude < 1f || !seedVisible)
                    failures.Add("setup: hehe2 was still banished/out of frustum before Playing; " +
                        "the seed did not reproduce the live visible state.");

                uiMode.SetUIMode(UIModeType.Playing, false);
                cameraManager.SelectCamera(CameraType.Playing);
                if (!UIMode.AnimationMode)
                    failures.Add("UIMode.AnimationMode was false in Playing; the test did not " +
                        "match the deployed preview state.");

                // The user's circle is on the very first Playing frame at beat0; require the
                // banish to hold before any seek drives the animation system.
                yield return null;
                InspectBanishState(camera, 0f, sphereContainers, failures);

                foreach (var beat in new[] { 0.1f, 6f, 0.1f })
                {
                    yield return SeekTo(beat);
                    InspectBanishState(camera, beat, sphereContainers, failures);
                }
            }
            finally
            {
                foreach (var (t, position, scale) in originals)
                {
                    t.localPosition = position;
                    t.localScale = scale;
                }
                editMode.EditingMode = previousEditingMode;
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Named-material rollback guard: BLACK authors shader 'Standard' with the shaderKeywords
        // key omitted entirely (validated against the fixture JSON below). Chroma's Standard->Glowing
        // upgrade only applies to an explicitly empty keyword list, so hehe must keep the lit
        // ChroMapper/Lit shader and the authored black MPB colour; an omitted-key-is-empty
        // regression would silently switch it to the Glowing shader. hehe2's GLOW (TransparentLight)
        // stays a different shader.
        [UnityTest]
        public IEnumerator NamedStandardWithoutKeywordsKeepsBlackSphereLit()
        {
            var fixture = JSON.Parse(File.ReadAllText(FixturePath));
            var black = fixture["customData"]["materials"]["BLACK"];
            Assert.That(black["shader"].Value, Is.EqualTo("Standard"),
                "Fixture BLACK should author shader 'Standard'.");
            Assert.That(black.HasKey("shaderKeywords"), Is.False,
                "Fixture BLACK should omit shaderKeywords; this guard covers the omitted-key path.");

            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            Assert.That(geometry, Is.Not.Null, "No GeometryGridContainer in the loaded scene.");
            var spheres = geometry.LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>()
                .Where(c => c.EnvironmentEnhancement.Track == "hehe"
                    || c.EnvironmentEnhancement.Track == "hehe2")
                .ToDictionary(c => c.EnvironmentEnhancement.Track);
            Assert.That(spheres.Count, Is.EqualTo(2),
                "Expected hehe and hehe2 geometry containers in the loaded scene.");

            // The authored shape renderer is the one the appearance controller drives;
            // GetComponentInChildren can pick up the editor selection-highlight mesh instead.
            var heheRenderer = spheres["hehe"].MpbController.Renderers.FirstOrDefault();
            Assert.That(heheRenderer, Is.Not.Null, "No shape renderer on the hehe container.");
            Assert.That(heheRenderer.sharedMaterial.shader.name, Is.EqualTo("ChroMapper/Lit"),
                "hehe's named Standard material lost the lit shader (keywords omitted treated as " +
                "empty => Glowing): " + heheRenderer.sharedMaterial.shader.name);
            var heheColor = spheres["hehe"].MpbController.Mpb
                .GetColor(Shader.PropertyToID("_Color"));
            Assert.That(heheColor.r, Is.EqualTo(0f).Within(0.01f),
                $"hehe MPB _Color.r should be BLACK (0), got {heheColor}.");
            Assert.That(heheColor.g, Is.EqualTo(0f).Within(0.01f),
                $"hehe MPB _Color.g should be BLACK (0), got {heheColor}.");
            Assert.That(heheColor.b, Is.EqualTo(0f).Within(0.01f),
                $"hehe MPB _Color.b should be BLACK (0), got {heheColor}.");

            var hehe2Renderer = spheres["hehe2"].MpbController.Renderers.FirstOrDefault();
            Assert.That(hehe2Renderer, Is.Not.Null, "No shape renderer on the hehe2 container.");
            Assert.That(hehe2Renderer.sharedMaterial.shader.name, Is.Not.EqualTo("ChroMapper/Lit"),
                "hehe2's GLOW should resolve to a light shader, not ChroMapper/Lit.");
            yield break;
        }

        private void InspectBanishState(
            Camera camera, float beat, List<Beatmap.Containers.GeometryContainer> sphereContainers,
            List<string> failures)
        {
            InspectSphereState(camera, beat, failures);
            // The die banish lands on each child track's ObjectParentTransform proxy (the sphere's
            // immediate Parent node), so assert those proxies carry scale0 at the -6969 offset.
            foreach (var container in sphereContainers)
            {
                var proxy = container.transform.parent;
                var proxyText = proxy == null ? "<none>" : $"pos={proxy.position} scale={proxy.lossyScale}";
                Debug.Log($"[SaltyDiag] fixture beat={beat} {container.EnvironmentEnhancement.Track} " +
                    $"proxy {proxyText}");
                if (proxy == null || proxy.lossyScale.magnitude > 0.5f
                    || proxy.position.magnitude < 6000f)
                    failures.Add($"beat {beat}: {container.EnvironmentEnhancement.Track} parent " +
                        $"proxy not banished ({proxyText}).");
            }
        }

        private void InspectSphereState(Camera camera, float beat, List<string> failures)
        {
            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(geometry, Is.Not.Null, "No GeometryGridContainer in the loaded scene.");
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var spheres = geometry.LoadedContainers.Values
                .OfType<Beatmap.Containers.GeometryContainer>()
                .Where(c => c.EnvironmentEnhancement.Track == "hehe"
                    || c.EnvironmentEnhancement.Track == "hehe2")
                .ToList();
            if (spheres.Count != 2)
                failures.Add($"beat {beat}: expected 2 hehe sphere containers, found {spheres.Count}.");
            foreach (var container in spheres)
            {
                var track = container.EnvironmentEnhancement.Track;
                var scale = container.transform.lossyScale;
                var renderer = container.GetComponentInChildren<MeshRenderer>();
                var inFrustum = renderer != null
                    && renderer.enabled
                    && GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
                var animTrack = tracks == null ? null : tracks.GetAnimationTrack(track);
                var attached = animTrack != null
                    && animTrack.Children.Contains(container.Animator);
                Debug.Log($"[SaltyDiag] fixture beat={beat} sphere={track} lossyScale={scale} " +
                    $"bounds={(renderer == null ? "none" : renderer.bounds.ToString())} " +
                    $"inFrustum={inFrustum} trackAttached={attached}");
                if (!attached)
                    failures.Add($"beat {beat}: {track} animator detached from its track " +
                        $"(trackChildren={(animTrack == null ? "no track" : animTrack.Children.Count.ToString())}).");
                if (scale.magnitude > 0.5f)
                    failures.Add($"beat {beat}: {track} lossyScale={scale} - the die banish is lost.");
                if (inFrustum)
                    failures.Add($"beat {beat}: {track} renderer inside the playing frustum.");
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

        // A failed assertion must still leave the workspace, camera and preview mode safe for reload.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);
            if (editMode != null)
                editMode.EditingMode = EditingMode.Gameplay;
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = previousPlayerCameraFOV;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerCameraOffsetZ;
            Settings.Instance.CameraFOV = previousCameraFOV;
            yield break;
        }
    }
}

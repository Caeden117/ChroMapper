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
    // Fixture source: "Heliov", mapped by Swifter (BeatSaver ID: 27ade).
    // HeliovMapParityTest keeps Swifter's complete 1,098-entry BTS environment, its preceding
    // custom events through beat 525, the real lighting events, and the opening notes and rain walls.
    // The tests isolate the reported rain, first cliff, beat-102 valley/beat-457 mountain,
    // and beat-518 powerline views at the map's 150 BPM. The map uses legacy AssignFogTrack plus
    // AnimateTrack fog fields, whereas Spells uses AnimateComponent.
    public class HeliovMapParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private bool colorFakeWallsBeforeTest;
        private float playerCameraOffsetZBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "HeliovEnvironmentFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            colorFakeWallsBeforeTest = Settings.Instance.ColorFakeWalls;
            playerCameraOffsetZBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            Settings.Instance.Animations = true;
            Settings.Instance.ColorFakeWalls = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;

            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 150,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 215);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [SetUp]
        public void EnableReportedPreviewSettings()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.ColorFakeWalls = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
        }

        // RainWallsUseAuthoredColorInPlayingMode: the beat-7.5 rain obstacles have negative
        // duration but carry an explicit gray/transparent custom color. The editor's fake-wall
        // diagnostic tint must not replace that gameplay color when Playing renders the rain.
        [UnityTest]
        public IEnumerator RainWallsUseAuthoredColorInPlayingMode()
        {
            EnterPlayingMode();
            yield return SeekTo(7.5f);

            var rain = Object.FindObjectsByType<ObstacleContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(wall => wall.ObstacleData != null
                    && Mathf.Abs(wall.ObstacleData.JsonTime - 7.5f) < 0.0001f);
            Assert.That(rain, Is.Not.Null, "The first authored rain wall did not load.");
            Assert.That(rain.ObstacleData.Duration, Is.LessThan(0f));
            Assert.That(rain.ObstacleData.CustomColor, Is.Not.Null);
            var actual = rain.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
            var authored = rain.ObstacleData.CustomColor.Value;
            Assert.That(actual.r, Is.EqualTo(authored.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(authored.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(authored.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(authored.a).Within(0.0001f));
        }

        // CliffSceneAtFirstNotesUsesFogTrackAndEnvironment: at beat 6 the camera rides the
        // player track into the 1,049 duplicated pillar meshes. The map sets the fog height and
        // start Y here; without them the fog blacks out the authored cliff and distant objects.
        [UnityTest]
        public IEnumerator CliffSceneAtFirstNotesUsesFogTrackAndEnvironment()
        {
            EnterPlayingMode();
            yield return SeekTo(6.1f);

            var camera = cameraManager.CameraControllers[1].transform;
            Assert.That(camera.position.y, Is.GreaterThan(500f),
                "AssignPlayerToTrack did not move the camera to the elevated first-note cliff.");
            var constructed = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(container => container.EnvironmentEnhancement?.Track?.StartsWith("blenderEnv0_") == true);
            Assert.That(constructed, Is.GreaterThanOrEqualTo(1000),
                "The map's duplicated pillar scenery was not constructed from environment enhancements.");
            AssertFog(3e-06f, 200f, 277.73256f, "first-note cliff");
        }

        // ValleyAndMountainFogFollowAuthoredTrack covers both reported distant views. The beat-102
        // values place a 200-unit fog layer below the new valley camera; beat 454 moves the 300-unit
        // layer for the mountain visible at beat 457. A stale fog state hides distant scenery.
        [UnityTest]
        public IEnumerator ValleyAndMountainFogFollowAuthoredTrack()
        {
            EnterPlayingMode();
            yield return SeekTo(103f);
            AssertFog(1e-05f, 200f, -32.931479f, "beat-102 valley");

            yield return SeekTo(457f);
            AssertFog(3e-06f, 300f, 49.320374f, "beat-457 mountain");
        }

        // PowerlineWaterFogUsesBeat518Values: the water scene intentionally sets height to zero
        // and moves the fog start far below the camera while increasing attenuation. Dropping the
        // height/startY animation leaves an opaque fog plane in front of the powerlines.
        [UnityTest]
        public IEnumerator PowerlineWaterFogUsesBeat518Values()
        {
            EnterPlayingMode();
            yield return SeekTo(518.1f);
            AssertFog(1e-04f, 0f, -69765.12657f, "beat-518 powerline water");

            // The shared Heliov fixture must also restore the earlier cliff's fog on a reverse
            // seek; retaining the powerline layer would black out the opening after scrubbing.
            yield return SeekTo(6.1f);
            AssertFog(3e-06f, 200f, 277.73256f, "first-note cliff after reverse seek");
        }

        // Beat41LaserReachesLeftSideOfPlayingView: the flash is visible from the left rock
        // in game. Compare the same Playing frame with and without its physical box so a
        // bright fog background cannot make a missing left segment pass the regression.
        [UnityTest]
        public IEnumerator Beat41LaserReachesLeftSideOfPlayingView()
        {
            EnterPlayingMode();
            yield return SeekTo(6.1f);
            yield return SeekTo(41.5f);

            var camera = cameraManager.CameraControllers[1].Camera;
            var flash = Object.FindObjectsByType<ParametricBloomFogLightController>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .First(controller => controller.BoxLight != null && controller.ID == 30 &&
                    controller.transform.position.x > 4000f);
            // Beat41LaserReachesLeftSideOfPlayingView: keep the beam mesh dimensions in the
            // failing render log while distinguishing a pose regression from capture variance.
            Debug.Log($"[HeliovBoxDiag] parent={flash.transform.position} boxLocalPos={flash.BoxLight.transform.localPosition} " +
                $"boxLocalScale={flash.BoxLight.transform.localScale} width={flash.BoxLight.Width} " +
                $"height={flash.BoxLight.Height} length={flash.BoxLight.Length} update={flash.BoxLight.UpdateTransform}");
            Assert.That(flash.BoxLight.Renderer, Is.Not.Null, "The authored flash has no physical beam.");
            Assert.That(flash.IsPhysical, Is.True,
                "The authored flash controller must classify its live BoxLight as physical — " +
                "a copied HasInitialized flag leaves hasBoxLight latched false and the beam dark.");
            var previousTarget = camera.targetTexture;
            var sceneTexture = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            var withFlash = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var withoutFlash = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = sceneTexture;
                camera.Render();
                ReadRenderTexture(sceneTexture, withFlash);

                // Keep fog and every other light unchanged while measuring the box itself.
                flash.BoxLight.Renderer.enabled = false;
                try
                {
                    camera.Render();
                    ReadRenderTexture(sceneTexture, withoutFlash);
                }
                finally
                {
                    flash.BoxLight.Renderer.enabled = true;
                }

                var leftBeamMaxDifference = 0;
                var leftBeamPixelCount = 0;
                for (var y = 256; y < 450; y++)
                {
                    for (var x = 256; x < 512; x++)
                    {
                        var withColor = withFlash.GetPixel(x, y);
                        var withoutColor = withoutFlash.GetPixel(x, y);
                        var difference = Mathf.RoundToInt(255f * Mathf.Max(
                            withColor.r - withoutColor.r,
                            Mathf.Max(withColor.g - withoutColor.g, withColor.b - withoutColor.b)));
                        leftBeamMaxDifference = Mathf.Max(leftBeamMaxDifference, difference);
                        if (difference >= 8) leftBeamPixelCount++;
                    }
                }

                Assert.That(leftBeamMaxDifference, Is.GreaterThanOrEqualTo(8),
                    "The beat-41.5 physical flash should contribute visible pixels over the left rock.");
                Assert.That(leftBeamPixelCount, Is.GreaterThan(12),
                    "The beat-41.5 beam should continue across the left sky, not stop in the upper right.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Object.Destroy(sceneTexture);
                Object.Destroy(withFlash);
                Object.Destroy(withoutFlash);
            }
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

        private static void AssertFog(float attenuation, float height, float startY, string view)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null);
            Assert.That(context.Descriptor, Is.Not.Null);
            var fog = context.Descriptor.BloomFogParams;
            Assert.That(fog.Attenuation, Is.EqualTo(attenuation).Within(1e-09f),
                $"{view}: authored fog attenuation did not reach the descriptor.");
            Assert.That(fog.Height, Is.EqualTo(height).Within(0.001f),
                $"{view}: authored fog height did not reach the descriptor.");
            Assert.That(fog.StartY, Is.EqualTo(startY).Within(0.1f),
                $"{view}: authored fog start Y did not reach the descriptor.");
            Assert.That(Shader.GetGlobalFloat("_CustomFogAttenuation"),
                Is.EqualTo(attenuation).Within(1e-09f),
                $"{view}: fog attenuation did not reach the renderer.");
            Assert.That(Shader.GetGlobalFloat("_CustomFogHeightFogHeight"),
                Is.EqualTo(height).Within(0.001f),
                $"{view}: fog height did not reach the renderer.");
            Assert.That(Shader.GetGlobalFloat("_CustomFogHeightFogStartY"),
                Is.EqualTo(startY).Within(0.1f),
                $"{view}: fog start Y did not reach the renderer.");
        }

        private void EnterPlayingMode()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        // Restore preview mode and settings even after a failed assertion; otherwise the next
        // mapper scene Start runs with a stale Playing camera and can hang the Unity test runner.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.ColorFakeWalls = colorFakeWallsBeforeTest;
            Settings.Instance.PlayerCameraOffsetZ = playerCameraOffsetZBeforeTest;
            yield break;
        }

        // Clear the full Heliov fixture after the class so later tests get their ordinary shared map.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }

    // HeliovFullMapLaserParityTest runs the same beat-41.5 physical-flash A/B as
    // Beat41LaserReachesLeftSideOfPlayingView but against the COMPLETE shipped
    // ExpertStandard.dat (all 1,098 BTS enhancements and real lighting/custom events in source
    // order) instead of the reduced fixture — the user's "dark" report is on the full map, so this
    // pins whether the beam renders there. Explicit because it reloads the entire environment.
    public class HeliovFullMapLaserParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private bool colorFakeWallsBeforeTest;
        private float playerCameraOffsetZBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private const string FullMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/" +
            "27ade (Heliov - Swifter)/ExpertStandard.dat";

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            colorFakeWallsBeforeTest = Settings.Instance.ColorFakeWalls;
            playerCameraOffsetZBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            Settings.Instance.Animations = true;
            Settings.Instance.ColorFakeWalls = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;

            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(FullMapPath)),
                beatsPerMinute: 150,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 215);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // Same contract as Beat41LaserReachesLeftSideOfPlayingView: with only the authored flash's
        // physical box toggled, the left-side region must gain >=8 max channel delta and >12
        // qualifying pixels. The diagnostic block logs camera pose, frustum/bounds, beam endpoint
        // viewport, controller/box color state, and fog counts BEFORE the assertions so a red run
        // still identifies which link broke.
        [UnityTest]
        [Explicit]
        public IEnumerator Beat41LaserBeamRendersOnCompleteMap()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(6.1f);
            yield return null;
            yield return null;
            atsc.MoveToJsonTime(41.5f);
            yield return null;
            yield return null;

            var camera = cameraManager.CameraControllers[1].Camera;
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var flash = Object.FindObjectsByType<ParametricBloomFogLightController>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .First(controller => controller.BoxLight != null && controller.ID == 30 &&
                    controller.transform.position.x > 4000f);

            var boxRenderer = flash.BoxLight.Renderer;
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            var inFrustum = boxRenderer != null && GeometryUtility.TestPlanesAABB(frustumPlanes, boxRenderer.bounds);
            var viewportMin = boxRenderer == null
                ? Vector3.negativeInfinity
                : camera.WorldToViewportPoint(boxRenderer.bounds.min);
            var viewportMax = boxRenderer == null
                ? Vector3.negativeInfinity
                : camera.WorldToViewportPoint(boxRenderer.bounds.max);
            var boxMpb = new MaterialPropertyBlock();
            var mpbColor = "no-renderer";
            if (boxRenderer != null)
            {
                boxRenderer.GetPropertyBlock(boxMpb);
                mpbColor = boxMpb.GetColor(Shader.PropertyToID("_Color")).ToString();
            }
            // HeliovFullMapLaserParityTest.Beat41LaserBeamRendersOnCompleteMap: record the controller
            // and renderer colors before the A/B capture so a future dark-beam failure distinguishes
            // missing controller initialization from a geometry property-block overwrite.
            var owner = flash.GetComponentInParent<GeometryContainer>();
            var ownerMpbColor = owner == null || owner.MpbController == null
                ? (Color?)null
                : owner.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
            var ownerAnimator = owner == null ? null : owner.Animator;
            var fog = context.Descriptor.BloomFogParams;
            Debug.Log(
                $"[HeliovFullMapDiag] cameraPos={camera.transform.position} cameraRot={camera.transform.eulerAngles} " +
                $"fov={camera.fieldOfView} | boxBounds={(boxRenderer == null ? "null" : boxRenderer.bounds.ToString())} " +
                $"inFrustum={inFrustum} viewportMin={viewportMin} viewportMax={viewportMax} | " +
                $"controllerActive={flash.isActiveAndEnabled} controllerColor={flash.Color} " +
                $"hasInitialized={flash.HasInitialized} shouldRefresh={flash.ShouldRefresh} " +
                $"updateAlways={flash.UpdateAlways} | " +
                $"boxRendererEnabled={(boxRenderer == null ? "null" : boxRenderer.enabled.ToString())} " +
                $"boxMpbColor={mpbColor} boxAlphaMultiplier={flash.BoxLight.AlphaMultiplier} | " +
                $"owner={(owner == null ? "null" : owner.name)} ownerMpbColor={(ownerMpbColor?.ToString() ?? "null")} " +
                $"ownerAnimatorTargetType={(ownerAnimator == null ? "none" : ownerAnimator.TargetType.ToString())} " +
                $"ownerColorsCount={(ownerAnimator == null ? "-" : ownerAnimator.Colors.Count.ToString())} " +
                $"ownerOpacityCount={(ownerAnimator == null ? "-" : ownerAnimator.Opacity.Count.ToString())} | " +
                $"fog attenuation={fog.Attenuation} height={fog.Height} startY={fog.StartY} | " +
                $"allBloomFogLights={BloomFogObject.AllBloomFogLights.Count}");

            Assert.That(boxRenderer, Is.Not.Null, "The authored flash has no physical beam.");
            var previousTarget = camera.targetTexture;
            var sceneTexture = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            var withFlash = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var withoutFlash = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = sceneTexture;
                camera.Render();
                ReadRenderTexture(sceneTexture, withFlash);

                boxRenderer.enabled = false;
                try
                {
                    camera.Render();
                    ReadRenderTexture(sceneTexture, withoutFlash);
                }
                finally
                {
                    boxRenderer.enabled = true;
                }

                var leftBeamMaxDifference = 0;
                var leftBeamPixelCount = 0;
                for (var y = 256; y < 450; y++)
                {
                    for (var x = 256; x < 512; x++)
                    {
                        var withColor = withFlash.GetPixel(x, y);
                        var withoutColor = withoutFlash.GetPixel(x, y);
                        var difference = Mathf.RoundToInt(255f * Mathf.Max(
                            withColor.r - withoutColor.r,
                            Mathf.Max(withColor.g - withoutColor.g, withColor.b - withoutColor.b)));
                        leftBeamMaxDifference = Mathf.Max(leftBeamMaxDifference, difference);
                        if (difference >= 8) leftBeamPixelCount++;
                    }
                }

                Debug.Log($"[HeliovFullMapDiag] baseline maxDelta={leftBeamMaxDifference} " +
                    $"pixelCount={leftBeamPixelCount}");

                Assert.That(flash.IsPhysical, Is.True,
                    "The authored flash controller must classify its live BoxLight as physical — " +
                    "a copied HasInitialized flag leaves hasBoxLight latched false and the beam dark.");
                Assert.That(leftBeamMaxDifference, Is.GreaterThanOrEqualTo(8),
                    "The beat-41.5 physical flash should contribute visible pixels over the left rock on the complete map.");
                Assert.That(leftBeamPixelCount, Is.GreaterThan(12),
                    "The beat-41.5 beam should continue across the left sky on the complete map.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Object.Destroy(sceneTexture);
                Object.Destroy(withFlash);
                Object.Destroy(withoutFlash);
            }
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
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.ColorFakeWalls = colorFakeWallsBeforeTest;
            Settings.Instance.PlayerCameraOffsetZ = playerCameraOffsetZBeforeTest;
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

using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "The 15 Sublimit", mapped by Mawntee (ft. Lonely) (BeatSaver ID: 1c8c0).
    // The15SublimitMapParityTest keeps the reported ExpertPlus map's complete BTS enhancements,
    // point definitions, and all notes, walls, lighting, and custom events through beat 156.
    // Beat 155.3 is the 1:17.65 water view at 120 BPM, after the player moves to stage five.
    public class The15SublimitMapParityTest : TestBase
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
            "The15SublimitBeat155Fixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            colorFakeWallsBeforeTest = Settings.Instance.ColorFakeWalls;
            playerCameraOffsetZBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            Settings.Instance.Animations = true;
            Settings.Instance.ColorFakeWalls = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;

            // The source map's beat-0 juan5 position contains a literal null. Heck's
            // Convert.ToSingle reads it as the numeric zero component, so the point parses cleanly
            // during load; the fixture keeps the source event intact.
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 120,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 100);
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

        // Beat155FogTrackReachesRenderer distinguishes a stale fog track from the side-array spacing
        // regression: the map finishes its last fog event at beat 68 and holds these values at 155.3.
        [UnityTest]
        public IEnumerator Beat155FogTrackReachesRenderer()
        {
            EnterPlayingMode();
            yield return SeekTo(155.3f);

            var camera = cameraManager.CameraControllers[1].transform;
            Assert.That(camera.position.x, Is.GreaterThan(1_000_000f),
                "AssignPlayerToTrack did not move the camera to the stage-five water scene.");
            var fog = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.BloomFogParams;
            Assert.That(fog.Attenuation, Is.EqualTo(0.003f).Within(1e-06f));
            Assert.That(fog.StartY, Is.EqualTo(-45f).Within(0.01f));
            Assert.That(fog.Height, Is.EqualTo(12.5f).Within(0.01f));
            Assert.That(Shader.GetGlobalFloat("_CustomFogAttenuation"), Is.EqualTo(0.003f).Within(1e-06f));
            Assert.That(Shader.GetGlobalFloat("_CustomFogHeightFogStartY"), Is.EqualTo(-45f).Within(0.01f));
            Assert.That(Shader.GetGlobalFloat("_CustomFogHeightFogHeight"), Is.EqualTo(12.5f).Within(0.01f));
        }

        // Beat155RingArraysKeepVisibleSegmentGaps checks the game screenshot's separated red/black
        // stripes. Each BTS ring's cube is five local units deep, so adjacent ring centers must
        // remain more than five local units apart to leave a visible gap after parent scaling.
        [UnityTest]
        public IEnumerator Beat155RingArraysKeepVisibleSegmentGaps()
        {
            EnterPlayingMode();
            yield return SeekTo(155.3f);

            foreach (var track in new[] { "SpinnyBoiRightStep5", "SpinnyBoiLeftStep5" })
            {
                var container = Object.FindObjectsByType<GeometryContainer>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(item => item.EnvironmentEnhancement?.Track == track);
                // GeometryContainer also owns a material animator; select the animator attached
                // to the native ring manager so this measures the enhanced scene object itself.
                var target = container.GetComponents<ObjectAnimator>()
                    .Single(animator => animator.LocalTarget != null
                        && animator.LocalTarget.GetComponent<TrackLaneRingsManager>() != null)
                    .LocalTarget;
                var positionSpawner = target.GetComponent<TrackLaneRingsPositionSpawner>();
                Assert.That(positionSpawner, Is.Not.Null, $"{track} lost its BTS ring-position component.");
                Assert.That(positionSpawner.enabled, Is.False,
                    $"{track} must preserve BTS's disabled ring-position event spawner.");
                // BTS names children lexically (0, 1, 10, ...) in the imported hierarchy;
                // the manager list retains the authoritative segment sequence (0..29).
                var rings = target.GetComponent<TrackLaneRingsManager>().Rings.ToArray();
                Assert.That(rings, Has.Length.EqualTo(30), $"{track} lost BTS's 30 ring segments.");
                Assert.That(target.localScale.z, Is.EqualTo(3f).Within(0.001f),
                    $"{track} lost its authored beat-0 parent depth scale.");
                var depth = rings[0].transform.GetChild(0).localScale.x;
                var spacing = rings[1].transform.localPosition.z - rings[0].transform.localPosition.z;
                Assert.That(spacing, Is.GreaterThan(depth + 0.5f),
                    $"{track} ring centers are {spacing:F2} apart while each cube is {depth:F2} deep; " +
                    "the game view has gaps between adjacent stripes, not overlapping slabs.");
                Assert.That(spacing, Is.EqualTo(8f).Within(0.05f),
                    $"{track} should keep BTS's initial eight-unit spacing when its spawner is disabled.");
                Assert.That(rings[29].transform.localPosition.z - rings[0].transform.localPosition.z,
                    Is.EqualTo(232f).Within(0.5f),
                    $"{track} must span the game's full depth so fog hides later segments at the right distance.");
            }
        }

        // Beat155WaterWallIsRedWithoutEditorOutline isolates the water obstacle from the fog and
        // ring array: Playing mode should use its authored red color without a selection outline.
        [UnityTest]
        public IEnumerator Beat155WaterWallIsRedWithoutEditorOutline()
        {
            EnterPlayingMode();
            yield return SeekTo(155.3f);

            var water = Object.FindObjectsByType<ObstacleContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(wall => wall.ObstacleData != null
                    && Mathf.Abs(wall.ObstacleData.JsonTime - 69f) < 0.001f
                    && Mathf.Abs(wall.ObstacleData.Duration - 151f) < 0.001f);
            var color = water.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
            Assert.That(color.r, Is.EqualTo(0.069f).Within(0.001f));
            Assert.That(color.g, Is.EqualTo(0f).Within(0.001f));
            Assert.That(color.b, Is.EqualTo(0f).Within(0.001f));
            Assert.That(water.SelectionMpbController.Renderers.All(renderer => !renderer.enabled), Is.True,
                "The water wall's white editor selection outline must not render in Playing mode.");
        }

        // The authored water's negative bloom alpha must not leave the obstacle-frame
        // mesh visible as a bright white line along the distant water boundaries.
        [UnityTest]
        public IEnumerator Beat155WaterSuppressesNegativeAlphaOutline()
        {
            EnterPlayingMode();
            yield return SeekTo(155.3f);
            var water = Object.FindObjectsByType<ObstacleContainer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(wall => wall.ObstacleData != null
                    && Mathf.Abs(wall.ObstacleData.JsonTime - 69f) < 0.001f
                    && Mathf.Abs(wall.ObstacleData.Duration - 151f) < 0.001f);
            var outline = water.OutlineTransform.GetComponentInChildren<MeshRenderer>();
            var color = water.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
            Assert.That(color.a, Is.LessThan(-60f));
            Assert.That(outline.enabled, Is.False,
                "The water's negative-alpha obstacle frame should not draw over the red core.");
            Assert.That(water.CoreRenderer.enabled, Is.True,
                "Suppressing the white frame must leave the red water surface visible.");
            try
            {
                water.SetColor(new Color(color.r, color.g, color.b, 1f));
                Assert.That(outline.enabled, Is.True,
                    "A pooled wall with ordinary alpha must regain its obstacle frame.");
            }
            finally
            {
                water.SetColor(color);
            }
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

        // Restore preview state after failed assertions so the next mapper scene can initialize.
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

        // Clear the map's shared fixture after this class so later tests get their usual map.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

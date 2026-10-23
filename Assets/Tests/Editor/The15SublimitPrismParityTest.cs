using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // The 15 Sublimit (Mawntee ft. Lonely, 1c8c0) positions its center BTS prism with environment entries 11–12.
    // The reduced fixture keeps those entries in order and has no Event 9, like the source map.
    public class The15SublimitPrismParityTest : BasicEventChunkingTestBase
    {
        private const string CenterId = "BTSEnvironment.[0]Environment.[17]SmallPillarPair.[0]PillarL";
        private const string SourceMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/" +
            "1c8c0 (The 15 Sublimit - Mawntee (ft. Lonely))/ExpertPlusStandard.dat";
        private static readonly Vector3 authoredLocalPosition = new Vector3(0f, -8.952f, 0.624f) * 0.6f;
        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        private bool previousAnimations;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private AudioTimeSyncController playbackClock;
        private bool previousClockEnabled;

        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield break;
        }

        private IEnumerator LoadFixture(bool fullMap = false, bool includeMovement = false)
        {
            Settings.Instance.Animations = true;
            var path = fullMap
                ? SourceMapPath
                : PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "The15SublimitCenterPrismFixture.json");
            var map = JSON.Parse(File.ReadAllText(path));
            if (includeMovement)
                map["_events"].Add(JSON.Parse("{\"_time\":282.03125,\"_type\":9,\"_value\":1,\"_floatValue\":1}"));

            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 20,
                NoteStartBeatOffset = 0,
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(
                2,
                map,
                beatsPerMinute: 120,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 320,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator CenterPrismKeepsAuthoredPositionAtBeat284()
        {
            yield return LoadFixture();
            yield return VerifyPrismPositions();
        }

        [UnityTest, Explicit]
        public IEnumerator FullMapCenterPrismKeepsAuthoredPositionAtBeat284()
        {
            yield return LoadFixture(fullMap: true);
            yield return VerifyPrismPositions();
        }

        private IEnumerator VerifyPrismPositions()
        {
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var center = descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == CenterId).transform;
            var expectedParent = Matrix4x4.TRS(
                new Vector3(0f, 34f, 153.600006f) * 0.6f,
                Quaternion.Euler(10f, 0f, 180f),
                new Vector3(2f, 2f, 2f));
            var expectedWorldPosition = expectedParent.MultiplyPoint3x4(authoredLocalPosition);
            var expectedPillar = expectedParent * Matrix4x4.TRS(
                authoredLocalPosition,
                Quaternion.Euler(7.52f, 313.399994f, 171.800003f),
                Vector3.one) * Matrix4x4.Scale(new Vector3(1f, 0.032f, 1f));
            // Chroodle's BTS capture supplies the native pillar mesh pivot, independently of CM's renderer bounds.
            var expectedMeshCenter = expectedPillar.MultiplyPoint3x4(new Vector3(0f, -56.3724976f, 0f));
            var pillarRenderer = center.Find("Pillar").GetComponent<Renderer>();
            var authoredPositions = new Dictionary<Transform, Vector3>();
            foreach (var enhancement in BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements)
            {
                if (!enhancement.ID.Contains("SmallPillarPair") || !enhancement.LocalPosition.HasValue)
                    continue;

                foreach (var marker in descriptor.ChromaIDMarkers)
                {
                    if (Regex.IsMatch(marker.ChromaID, enhancement.ID))
                        authoredPositions[marker.transform] = enhancement.LocalPosition.Value * 0.6f;
                }
            }

            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            void AssertPose(string phase)
            {
                Assert.That(
                    Vector3.Distance(center.position, expectedWorldPosition),
                    Is.LessThan(0.01f),
                    $"{phase}: center prism at {center.position:F5}, expected {expectedWorldPosition:F5}.");
                Assert.That(
                    Vector3.Distance(pillarRenderer.bounds.center, expectedMeshCenter),
                    Is.LessThan(0.01f),
                    $"{phase}: center prism mesh at {pillarRenderer.bounds.center:F5}, expected {expectedMeshCenter:F5}.");
                foreach (var position in authoredPositions)
                {
                    Assert.That(
                        Vector3.Distance(position.Key.localPosition, position.Value),
                        Is.LessThan(0.01f),
                        $"{phase}: {position.Key.name} lost its authored position {position.Value:F5}.");
                }
            }

            foreach (var mode in new[] { UIModeType.Playing, UIModeType.Preview, UIModeType.Playing })
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
                yield return null;
                uiMode.SetUIMode(mode, false);
                cameraManager.SelectCamera(mode == UIModeType.Playing ? CameraType.Playing : CameraType.Editing);
                foreach (var beat in new[] { 0f, 280f, 284.4f, 288f, 280f, 284.4f })
                {
                    clock.MoveToJsonTime(beat);
                    yield return null;
                    yield return null;
                    AssertPose($"{mode} beat {beat}");
                }
            }

            InitializeVirtualInput(includeKeyboard: false);
            playbackClock = clock;
            previousClockEnabled = clock.enabled;
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 280f);
            clock.enabled = false;
            foreach (var beat in new[] { 280f, 282f, 284.4f, 288f })
            {
                currentSecondsProperty.SetValue(clock, clock.GetSecondsFromBeat(beat));
                yield return null;
                yield return null;
                AssertPose($"Playback beat {beat}");
            }
        }

        [UnityTest]
        public IEnumerator AuthoredPositionReturnsAfterRewindingActualPillarMovement()
        {
            yield return LoadFixture(includeMovement: true);
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var center = descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == CenterId).transform;
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(BeatSaberSongContainer.Instance.Map.Events.Count, Is.EqualTo(1));
            Object.FindAnyObjectByType<BasicEventManager>().Refresh();
            clock.MoveToJsonTime(284.4f);
            yield return null;
            yield return null;
            var movedPosition = center.localPosition;
            Assert.That(movedPosition.x, Is.EqualTo(-8f).Within(0.001f));
            Assert.That(movedPosition.y, Is.InRange(5.8f, 6.01f));
            Assert.That(movedPosition.z, Is.EqualTo(0f).Within(0.001f));

            foreach (var refresh in new[] { false, true })
            {
                if (refresh)
                    Object.FindAnyObjectByType<BasicEventManager>().Refresh();

                clock.MoveToJsonTime(280f);
                yield return null;
                yield return null;
                Assert.That(
                    Vector3.Distance(center.localPosition, authoredLocalPosition),
                    Is.LessThan(0.001f),
                    "Rewinding before Event 9 must restore the enhanced position, not the cached native start pose.");
                clock.MoveToJsonTime(284.4f);
                yield return null;
                yield return null;
                Assert.That(Vector3.Distance(center.localPosition, movedPosition), Is.LessThan(0.001f));
            }
        }

        [UnityTearDown]
        public IEnumerator RestorePreviewState()
        {
            if (playbackClock != null)
            {
                if (playbackClock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(playbackClock);

                playbackClock.enabled = previousClockEnabled;
                playbackClock = null;
            }

            TearDownVirtualInput();
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptyMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

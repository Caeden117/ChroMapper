using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class SuperluminalWallSeekTest : BasicEventChunkingTestBase
    {
        // Source obstacle 25 forms a zooming frame. Fake obstacle 2012 supplies a path rotation and scale.
        // The reduced fixture keeps their requests, simplifying the fake path to its first and last points.
        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode uiMode;
        private CameraManager cameras;
        private int previousSnapping;
        private InputAction moveForward;
        private InputAction moveBackward;
        private int fineScrubTimeChanges;

        private void CountFineScrubTimeChanges() => fineScrubTimeChanges++;

        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousSnapping = AudioTimeSyncController.Instance.GridMeasureSnapping;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Requires the ignored original Superluminal difficulty.")]
        public IEnumerator FullMapWallsReachTheirSeekPoseBeforeRendering()
        {
            yield return CheckSeek(PathUtils.Combine(Application.dataPath, "..", "TestResults",
                "Superluminal", "SuperluminalFullMapFixture.json"));
        }

        [UnityTest]
        public IEnumerator ReducedWallsReachTheirSeekPoseBeforeRendering()
        {
            yield return CheckSeek(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures",
                "SuperluminalWallSeekFixture.json"));
        }

        [UnityTest]
        [Explicit("Requires the ignored original Superluminal difficulty.")]
        public IEnumerator FullMapWallsStayStableDuringSixtyFourthBeatScrubs()
        {
            yield return CheckFineScrubs(PathUtils.Combine(Application.dataPath, "..", "TestResults",
                "Superluminal", "SuperluminalFullMapFixture.json"));
        }

        [UnityTest]
        public IEnumerator ReducedWallsStayStableDuringSixtyFourthBeatScrubs()
        {
            yield return CheckFineScrubs(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures",
                "SuperluminalWallSeekFixture.json"));
        }

        private IEnumerator CheckSeek(string path)
        {
            yield return LoadSeekMap(path);
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            var failures = new List<string>();
            foreach (var mode in new[] { UIModeType.Playing, UIModeType.Preview, UIModeType.Normal, UIModeType.Playing })
            {
                uiMode.SetUIMode(mode, false);
                cameras.SelectCamera(mode == UIModeType.Playing ? CameraType.Playing : CameraType.Editing);
                if (mode == UIModeType.Normal)
                    continue;
                foreach (var beat in new[] { 263f, 264f, 266f, 263.5f, 267f, 264.25f, 268f,
                             263.75f, 269f, 264.5f, 270f, 264f })
                {
                    clock.MoveToJsonTime(beat);
                    var immediate = CapturePoses(walls);
                    if (beat == 264f)
                    {
                        var frame = walls.LoadedContainers.Values.OfType<ObstacleContainer>()
                            .Single(wall => Mathf.Abs(wall.ObstacleData.JsonTime - 264.02f) < 0.0001f
                                && wall.ObstacleData.CustomLocalRotation[2].AsFloat == 90f);
                        var bar = walls.LoadedContainers.Values.OfType<ObstacleContainer>()
                            .Single(wall => Mathf.Abs(wall.ObstacleData.JsonTime - 264.3793103f) < 0.0001f);
                        Assert.That(frame.CoreTransform.lossyScale.x, Is.GreaterThan(20f));
                        Assert.That(bar.Animator.LocalTarget.localScale.x, Is.GreaterThan(20f));
                    }
                    yield return null;
                    ComparePoses(walls, immediate, beat, mode + " first frame", failures);
                    yield return null;
                    ComparePoses(walls, immediate, beat, mode + " second frame", failures);
                }
            }
            Assert.That(failures, Is.Empty,
                "A stopped seek must apply its final wall pose synchronously.\n" + string.Join("\n", failures));
        }

        private IEnumerator LoadSeekMap(string path)
        {
            Assert.That(File.Exists(path), Is.True);
            Settings.Instance.Animations = true;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus", NoteJumpSpeed = 16, NoteStartBeatOffset = 0,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(path)),
                beatsPerMinute: 150, environmentName: "GagaEnvironment", songLengthSeconds: 270,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
        }

        private static Dictionary<BaseObstacle, Matrix4x4> CapturePoses(ObstacleGridContainer walls) =>
            walls.LoadedContainers.Values.OfType<ObstacleContainer>()
                .Where(wall => wall.ObstacleData.CustomAnimation != null)
                .ToDictionary(wall => wall.ObstacleData, wall => wall.CoreTransform.localToWorldMatrix);

        private IEnumerator CheckFineScrubs(string path)
        {
            yield return LoadSeekMap(path);
            InitializeVirtualInput(true);
            moveForward = new InputAction("Forward", InputActionType.Button, "<Keyboard>/f");
            moveBackward = new InputAction("Backward", InputActionType.Button, "<Keyboard>/b");
            moveForward.performed += clock.OnMoveCursorForward;
            moveBackward.performed += clock.OnMoveCursorBackward;
            moveForward.Enable();
            moveBackward.Enable();
            clock.GridMeasureSnapping = 64;
            clock.OnTimeChanged += CountFineScrubTimeChanges;
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            var failures = new List<string>();
            foreach (var anchor in new[] { 264.25f, 267.25f, 269.25f })
            {
                clock.MoveToJsonTime(anchor);
                yield return null;
                yield return null;
                for (var sample = 0; sample < 32; sample++)
                {
                    var direction = sample < 16 ? 1 : -1;
                    var key = direction == 1 ? Key.F : Key.B;
                    var expected = clock.CurrentJsonTime + (direction / 64f);
                    fineScrubTimeChanges = 0;
                    PressVirtualKeys(key);
                    ReleaseVirtualKeys(key);
                    Assert.That(clock.CurrentJsonTime, Is.EqualTo(expected).Within(0.00001f));
                    if (fineScrubTimeChanges != 1)
                        failures.Add($"beat {expected}: one input nudge raised {fineScrubTimeChanges} timeline changes.");
                    var immediate = CapturePoses(walls);
                    Assert.That(immediate.Count, Is.GreaterThan(0));
                    yield return null;
                    ComparePoses(walls, immediate, expected, "tiny scrub first frame", failures);
                    yield return null;
                    ComparePoses(walls, immediate, expected, "tiny scrub second frame", failures);
                }
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void ComparePoses(ObstacleGridContainer walls,
            Dictionary<BaseObstacle, Matrix4x4> immediate, float beat, string phase, List<string> failures)
        {
            var largest = 0f;
            BaseObstacle worst = null;
            foreach (var pair in immediate)
            {
                if (!walls.LoadedContainers.TryGetValue(pair.Key, out var container))
                    continue;
                var settled = ((ObstacleContainer)container).CoreTransform.localToWorldMatrix;
                var difference = Vector3.Distance(pair.Value.MultiplyPoint3x4(Vector3.zero),
                    settled.MultiplyPoint3x4(Vector3.zero));
                for (var axis = 0; axis < 3; axis++)
                    difference = Mathf.Max(difference, Vector4.Distance(pair.Value.GetColumn(axis), settled.GetColumn(axis)));
                if (difference > largest)
                {
                    largest = difference;
                    worst = pair.Key;
                }
            }
            if (largest > 0.001f)
                failures.Add($"beat {beat} {phase}: wall b{worst.JsonTime} pose changed by {largest} after seek returned.");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            moveForward?.Dispose();
            moveBackward?.Dispose();
            moveForward = null;
            moveBackward = null;
            TearDownVirtualInput();
            if (clock != null)
            {
                clock.OnTimeChanged -= CountFineScrubTimeChanges;
                clock.enabled = previousClockEnabled;
                clock.GridMeasureSnapping = previousSnapping;
            }
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreFreshMapper()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

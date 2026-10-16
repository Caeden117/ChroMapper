using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class CensoredRingShakeParityTest : PreviewWorkflowTestBase
    {
        private static readonly bool[] PlaybackCases = { false, true };
        private const float PositionTolerance = 0.002f;
        private const float ClockTolerance = 0.00002f;
        private static readonly PropertyInfo PlaybackSeconds = typeof(AudioTimeSyncController)
            .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
        private AudioTimeSyncController clock;
        private bool clockFrozen;
        private bool previousClockEnabled;

        // Imported from the 1.40.8 installation: SHA256 31D463B0028BC21D7C438B3A2C50F42F709F85F5977A5577CCC906C4CAEA3FE8.
        [UnityTest]
        public IEnumerator FullMapRingsAndAttachedLasersFollowEnvironmentShake(
            [ValueSource(nameof(PlaybackCases))] bool playback)
        {
            yield return CheckShake("CensoredFullMapFixture.json", playback);
        }

        [UnityTest]
        public IEnumerator PlaybackShakeToleratesAdjacentClockFloats([Values(-1, 1)] int clockFloatSteps)
        {
            yield return CheckShake("CensoredFullMapFixture.json", true, clockFloatSteps);
        }

        private IEnumerator CheckShake(string fixture, bool playback, int clockFloatSteps = 0)
        {
            Settings.Instance.Animations = true;
            var data = JSON.Parse(File.ReadAllText(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", fixture)));
            var oracle = data.Clone();
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus", NoteJumpSpeed = 16, NoteStartBeatOffset = 0,
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(3, data, beatsPerMinute: 195, environmentName: "FitBeatEnvironment",
                songLengthSeconds: 210, forceSceneReload: true, difficultyInfo: difficulty);
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock = atsc;
            previousClockEnabled = atsc.enabled;
            clockFrozen = true;
            atsc.enabled = false;
            var rings = Object.FindObjectsByType<TrackLaneRing>(FindObjectsSortMode.None)
                .Where(ring => ring.name is "PanelLightTrackLaneRing(Clone)" or "BigCenterLightTrackLaneRing(Clone)")
                .ToArray();
            Assert.That(rings.Length, Is.EqualTo(30));
            var runway = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Single(item => item.name == "TrackConstruction");
            const float baselineBeat = 68f;
            // The first shake keyframe is zero. Freeze the clock before capturing it so setup and
            // playback use the same authored pose, including round trips through seconds.
            atsc.MoveToJsonTime(baselineBeat);
            yield return null;
            yield return null;
            var pausedRunwayX = runway.position.x;
            var pausedRingX = rings.Select(ring => ring.transform.position.x).ToArray();
            if (playback)
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(
                    atsc,
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(baselineBeat));
                PlaybackSeconds.SetValue(atsc, GetSampleSeconds(atsc, baselineBeat, clockFloatSteps));
            }
            yield return null;
            yield return null;
            var baselineTime = atsc.CurrentJsonTime;
            Assert.That(baselineTime, Is.EqualTo(baselineBeat).Within(ClockTolerance));
            var baselineShake = GetShakeX(oracle, baselineTime);
            var initialX = rings.Select(ring => ring.transform.position.x).ToArray();
            var attachedLasers = rings.Select(ring => ring.GetComponentsInChildren<ParametricBoxLight>(true)).ToArray();
            Assert.That(attachedLasers.All(lasers => lasers.Length == 2), Is.True);
            var runwayX = runway.position.x;
            Assert.That(
                runwayX,
                Is.EqualTo(pausedRunwayX).Within(PositionTolerance),
                "Entering playback at the same beat must not move the control runway.");
            for (var i = 0; i < rings.Length; ++i)
                Assert.That(
                    initialX[i],
                    Is.EqualTo(pausedRingX[i]).Within(PositionTolerance),
                    $"Entering playback at the same beat moved {rings[i].name}.");

            foreach (var beat in new[] { 68.1f, 68.2f, 68.5f, 69.1f, 70.2f, 72.9f, 73.1f, 74.5f, 75.1f,
                84.1f, 85.2f, 89.1f, 91.1f, 68.1f })
            {
                if (playback)
                    PlaybackSeconds.SetValue(atsc, GetSampleSeconds(atsc, beat, clockFloatSteps));
                else
                    atsc.MoveToJsonTime(beat);

                yield return null;
                yield return null;
                if (playback)
                {
                    Assert.That(atsc.IsPlaying, Is.True);
                    Assert.That(atsc.SongAudioSource.isPlaying, Is.False);
                }

                Assert.That(atsc.CurrentJsonTime, Is.EqualTo(beat).Within(ClockTolerance));
                var shake = GetShakeX(oracle, atsc.CurrentJsonTime) - baselineShake;
                Assert.That(
                    runway.position.x - runwayX,
                    Is.EqualTo(shake).Within(PositionTolerance),
                    $"The runway must reproduce the shake before comparing rings. Baseline beat={baselineTime:R}, "
                        + $"X={runwayX:R}, shake={baselineShake:R}. Sample beat={atsc.CurrentJsonTime:R}, "
                        + $"X={runway.position.x:R}, expected delta={shake:R}.");
                var failures = new List<string>();
                for (var i = 0; i < rings.Length; ++i)
                {
                    var ring = rings[i];
                    var expectedX = initialX[i] + shake;
                    var error = expectedX - ring.transform.position.x;
                    var lasers = attachedLasers[i];
                    var parentName = ring.transform.parent != null ? ring.transform.parent.name : "<scene root>";
                    Debug.Log($"[CensoredRingShake] beat={atsc.CurrentJsonTime} ring={ring.name} "
                        + $"parent={parentName} x={ring.transform.position.x} expected={expectedX} "
                        + $"animator={ring.GetComponent<Beatmap.Animations.ObjectAnimator>() != null} lasers={lasers.Length}");
                    if (Mathf.Abs(error) > PositionTolerance)
                        failures.Add($"{ring.name} and its {lasers.Length} lasers missed shake X={shake}: "
                            + $"expected {expectedX}, actual {ring.transform.position.x}.");

                    foreach (var laser in lasers)
                    {
                        var localCenter = ring.transform.InverseTransformPoint(laser.Renderer.bounds.center);
                        var expectedCenter = Matrix4x4.TRS(new Vector3(expectedX, ring.transform.position.y,
                            ring.transform.position.z), ring.transform.rotation, ring.transform.lossyScale)
                            .MultiplyPoint3x4(localCenter);
                        if (Mathf.Abs(expectedCenter.x - laser.Renderer.bounds.center.x) > PositionTolerance)
                            failures.Add($"{ring.name}/{laser.name}: laser bounds X={laser.Renderer.bounds.center.x}, "
                                + $"expected {expectedCenter.x} after the authored shake.");
                    }
                }
                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }
        }

        private static float GetSampleSeconds(AudioTimeSyncController atsc, float jsonTime, int floatSteps)
        {
            var songBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);
            var seconds = atsc.GetSecondsFromBeat(songBpmTime);
            // Adjacent positive floats exercise the clock rounding that can differ across CPU architectures.
            return BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(seconds) + floatSteps);
        }

        private static float GetShakeX(JSONNode source, float beat)
        {
            JSONNode selected = null;
            foreach (var ev in source["customData"]["customEvents"].Children)
            {
                if (ev["t"].Value == "AnimateTrack" && ev["d"]["track"].Value == "bruhaa"
                    && ev["d"].HasKey("position") && ev["b"].AsFloat <= beat)
                {
                    selected = ev;
                }
            }
            Assert.That(selected, Is.Not.Null);
            var data = selected["d"];
            var progress = (beat - selected["b"].AsFloat) / data["duration"].AsFloat;
            progress = data.HasKey("repeat") ? progress % 1f : Mathf.Clamp01(progress);
            var points = data["position"].AsArray;
            for (var i = 1; i < points.Count; ++i)
            {
                if (progress <= points[i][3].AsFloat)
                {
                    var start = points[i - 1];
                    var end = points[i];
                    var t = (progress - start[3].AsFloat) / (end[3].AsFloat - start[3].AsFloat);
                    t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                    return Mathf.LerpUnclamped(start[0].AsFloat, end[0].AsFloat, t);
                }
            }
            return points[points.Count - 1][0].AsFloat;
        }

        [UnityTearDown]
        public IEnumerator RestoreEmptyMap()
        {
            if (clock != null && clock.IsPlaying)
                clock.CancelPlaying();

            if (clockFrozen && clock != null)
                clock.enabled = previousClockEnabled;

            clockFrozen = false;
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
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
    // Fixture source: "Paradigm", mapped by Elecast (BeatSaver ID: 3bbb0).
    public class ParadigmMapParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        protected virtual string FixtureName => "ParadigmFullMapFixture.json";
        protected virtual bool IncludeDoor => true;
        protected virtual bool IncludeStreak => true;
        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(PathUtils.Combine(
                    Application.dataPath, "Tests", "Fixtures", FixtureName))),
                beatsPerMinute: 130,
                environmentName: "BillieEnvironment",
                songLengthSeconds: 450);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // Paradigm's door slides in world space, then its city parent moves away at beat 512.
        // The boss streaks likewise move with their parent until individual animations begin at 804.
        [UnityTest]
        public IEnumerator DoorAndStreaksFollowParentMotionAcrossCutsAndReverseSeeks()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var geometry = Object.FindObjectsByType<GeometryContainer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var failures = new List<string>();
            if (IncludeDoor)
            {
                var door = geometry.Single(c => c.EnvironmentEnhancement.Track == "s_bc478");
                yield return CheckDoor(door, failures);
                yield return CheckPlayback(door, failures, door: true);
            }

            if (IncludeStreak)
            {
                var streak = geometry.Single(c => c.EnvironmentEnhancement.Track == "s_boss39");
                yield return CheckStreak(streak, failures);
                yield return CheckPlayback(streak, failures, door: false);
            }

            if (IncludeDoor && IncludeStreak)
            {
                yield return CheckFullMapRendering(geometry, failures);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static IEnumerator CheckDoor(GeometryContainer door, List<string> failures)
        {
            yield return SeekTo(511.9f);
            var slidePosition = Vector3.Lerp(
                new Vector3(59.01f, -13.39f, 111.1504f),
                new Vector3(59.91f, -9.13f, 110.02f),
                11.9f / 12f);
            CheckPosition(failures, door, slidePosition, "door before cut");

            foreach (var beat in new[] { 512.1f, 722.562f, 869.312f, 511.9f, 512.1f })
            {
                yield return SeekTo(beat);
                var expected = beat < 512f
                    ? slidePosition
                    : new Vector3(59.91f, -1000009.13f, -1009889.98f);
                CheckPosition(failures, door, expected, $"door at beat {beat}");
            }
        }

        private static IEnumerator CheckStreak(GeometryContainer streak, List<string> failures)
        {
            yield return SeekTo(719f);
            CheckPosition(failures, streak, new Vector3(-7.7f, 0f, 46.48f), "streak before travel");
            foreach (var beat in new[] { 722.562f, 795f, 797f, 803f, 722.562f, 797f })
            {
                yield return SeekTo(beat);
                var parentZ = beat >= 796f
                    ? Mathf.Lerp(9400f, 10000f, (beat - 796f) / 8f)
                    : beat >= 795f
                        ? 9400f
                        : Mathf.Lerp(10000f, 9463.58f, (beat - 719.5f) / (75.5f * 0.894f));
                CheckPosition(failures, streak, new Vector3(-7.7f, 0f, -9953.52f + parentZ),
                    $"streak at beat {beat}");
            }

            yield return SeekTo(804.5f);
            CheckPosition(failures, streak, new Vector3(-1.7343489f, -2.1319988f, 24.25f),
                "individual streak animation after 804");
        }

        private static void CheckPosition(
            List<string> failures, GeometryContainer target, Vector3 expected, string label)
        {
            var actual = target.Animator.LocalTarget.position;
            Debug.Log($"[ParadigmParity] {label}: expected={expected:F3}, actual={actual:F3}");
            if (Vector3.Distance(actual, expected) > 0.2f)
            {
                failures.Add($"{label}: expected {expected:F3}, got {actual:F3}");
            }
        }

        private static IEnumerator CheckPlayback(GeometryContainer target, List<string> failures, bool door)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var seconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
            var wasEnabled = atsc.enabled;
            var firstBeat = door ? 511.5f : 719.5f;
            yield return SeekTo(firstBeat);
            try
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(atsc, firstBeat);
                atsc.enabled = false;
                var beats = door
                    ? new[] { 511.5f, 511.75f, 512f, 512.25f, 512.5f }
                    : new[] { 719.5f, 720f, 721f, 722.562f, 795f, 796f, 797f, 798f, 803f, 804.5f };
                foreach (var beat in beats)
                {
                    seconds.SetValue(atsc, atsc.GetSecondsFromBeat(beat));
                    // Playback updates track properties in Update and applies their hierarchy in LateUpdate.
                    // Let both settle after a deliberate clock jump before comparing the steady pose.
                    yield return null;
                    yield return null;
                    yield return null;
                    yield return null;
                    Vector3 expected;
                    if (door)
                    {
                        expected = beat < 512f
                            ? Vector3.Lerp(new Vector3(59.01f, -13.39f, 111.1504f),
                                new Vector3(59.91f, -9.13f, 110.02f), (beat - 500f) / 12f)
                            : new Vector3(59.91f, -1000009.13f, -1009889.98f);
                    }
                    else if (beat >= 804f)
                    {
                        expected = new Vector3(-1.7343489f, -2.1319988f, 24.25f);
                    }
                    else
                    {
                        var parentZ = beat >= 796f
                            ? Mathf.Lerp(9400f, 10000f, (beat - 796f) / 8f)
                            : beat >= 795f
                                ? 9400f
                                : Mathf.Lerp(10000f, 9463.58f, (beat - 719.5f) / (75.5f * 0.894f));
                        expected = new Vector3(-7.7f, 0f, -9953.52f + parentZ);
                    }

                    CheckPosition(failures, target, expected, $"{target.EnvironmentEnhancement.Track} playback at {beat}");
                    if (!door && beat >= 804f)
                    {
                        var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack("s_boss39");
                        var property = (Beatmap.Animations.AnimateProperty<Vector3>)track.AnimatedProperties["position"];
                        Debug.Log($"[ParadigmParity] playback detail: json={atsc.CurrentJsonTime:F5}, " +
                            $"trackEnabled={track.enabled}, children={track.CachedChildren.Length}, " +
                            $"animatorEnabled={target.Animator.enabled}, worldCount={target.Animator.WorldPosition.Count}, " +
                            $"property={property.GetLerpedValue(atsc.CurrentJsonTime):F3}, " +
                            $"local={target.transform.localPosition:F3}, parent={target.transform.parent.position:F3}");
                    }
                }
            }
            finally
            {
                if (atsc.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(atsc);
                }

                atsc.enabled = wasEnabled;
            }
        }

        private IEnumerator CheckFullMapRendering(GeometryContainer[] geometry, List<string> failures)
        {
            var streaks = geometry.Where(c => c.EnvironmentEnhancement.Track != null
                && c.EnvironmentEnhancement.Track.StartsWith("s_boss")
                && c.EnvironmentEnhancement.Components != null).ToArray();
            Assert.That(streaks.Length, Is.EqualTo(99));
            var renderers = streaks.SelectMany(c => c.MpbController.Renderers).ToArray();
            var camera = cameraManager.CameraControllers[1].Camera;
            var output = PathUtils.Combine(Application.dataPath, "..", "TestResults",
                "Paradigm-" + System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            Directory.CreateDirectory(output);
            foreach (var beat in new[] { 722.562f, 797f, 804.5f, 869.312f })
            {
                yield return SeekTo(beat);
                var normal = RenderPixels(camera, PathUtils.Combine(output, $"beat-{beat}.png"));
                if (beat < 804f)
                {
                    var parentZ = beat < 796f
                        ? Mathf.Lerp(10000f, 9463.58f, (beat - 719.5f) / (75.5f * 0.894f))
                        : Mathf.Lerp(9400f, 10000f, (beat - 796f) / 8f);
                    foreach (var streak in streaks)
                    {
                        CheckPosition(failures, streak,
                            streak.EnvironmentEnhancement.Position.Value + new Vector3(0f, 0f, parentZ),
                            $"{streak.EnvironmentEnhancement.Track} at beat {beat}");
                    }

                    var enabled = renderers.Select(r => r.enabled).ToArray();
                    Color32[] withoutStreaks;
                    try
                    {
                        foreach (var renderer in renderers)
                        {
                            renderer.enabled = false;
                        }

                        withoutStreaks = RenderPixels(camera, PathUtils.Combine(output, $"beat-{beat}-streaks-off.png"));
                    }
                    finally
                    {
                        for (var i = 0; i < renderers.Length; i++)
                        {
                            renderers[i].enabled = enabled[i];
                        }
                    }

                    var changedPixels = 0;
                    for (var i = 0; i < normal.Length; i++)
                    {
                        if (normal[i].r != withoutStreaks[i].r || normal[i].g != withoutStreaks[i].g
                            || normal[i].b != withoutStreaks[i].b)
                        {
                            changedPixels++;
                        }
                    }

                    Debug.Log($"[ParadigmParity] beat {beat}: streaks changed {changedPixels} pixels. Captures: {output}");
                    if (changedPixels == 0)
                    {
                        failures.Add($"No streak pixels rendered at beat {beat}.");
                    }
                }

                var door = geometry.Single(c => c.EnvironmentEnhancement.Track == "s_bc478");
                if (GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),
                    door.MpbController.Renderers[0].bounds))
                {
                    failures.Add($"Factory door still intersects the camera frustum at beat {beat}.");
                }
            }
        }

        private static Color32[] RenderPixels(Camera camera, string path)
        {
            var target = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return texture.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null && atsc.IsPlaying)
            {
                TestUtils.PauseDeterministicPlayback(atsc);
            }

            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = animationsBeforeTest;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(
                3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }

    public class ParadigmDoorParityTest : ParadigmMapParityTest
    {
        protected override string FixtureName => "ParadigmDoorFixture.json";
        protected override bool IncludeStreak => false;
    }

    public class ParadigmStreakParityTest : ParadigmMapParityTest
    {
        protected override string FixtureName => "ParadigmStreakFixture.json";
        protected override bool IncludeDoor => false;
    }
}

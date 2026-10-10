using System.Collections;
using System.Collections.Generic;
using System.IO;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // Portable Salty b537 Head-camera parity. Fixture SaltyBeat537HeadCameraFixture.json carries the
    // authored customEvents (original indices 440-446, 463, 467) in source order: b537 assigns
    // LeftSaber/RightSaber/asdkm to LeftHand/RightHand/Head and starts the asdkm AnimateTrack
    // position z0 -> -3 over 8 beats with easeOutSine; b573/b577 are the authored follow-ups. No
    // authored Head event exists near b532. These cases were RED before the shared track-parent
    // bind seam (full-map SaltyFullMapPlacementParityTest equivalents demonstrate the same
    // failures); they are green on fixed source and run in the default suite.
    public class SaltyBeat537HeadCameraParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;
        private EditModeContext editMode;
        private EditingMode previousEditingMode;
        private bool editingModeChanged;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "SaltyBeat537HeadCameraFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            previousPlayerCameraFOV = Settings.Instance.PlayerCameraFOV;
            previousPlayerCameraOffsetZ = Settings.Instance.PlayerCameraOffsetZ;
            previousCameraFOV = Settings.Instance.CameraFOV;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 215,
                environmentName: "PanicEnvironment",
                songLengthSeconds: 250);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // Portable version of Beat537HeadCameraSeeksToAuthoredEaseWithoutLag: the authored asdkm
        // (Head) AnimateTrack eases camera z0 -> -3 over 8 beats with easeOutSine. The seek must
        // land on the authored curve synchronously (currently one beat behind) and binding must
        // not lift the camera (previously world y jumped 1.65 -> 2.25 and seeks lagged one beat).
        [UnityTest]
        public IEnumerator Beat537HeadCameraSeeksToAuthoredEaseWithoutLag()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null; // let CameraController.Update apply the FOV before sampling
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            void Sample(float beat, string phase)
            {
                var expectedZ = -3f * Mathf.Sin(Mathf.Clamp01((beat - 537f) / 8f) * Mathf.PI / 2f);
                var pos = camera.transform.position;
                if (Mathf.Abs(pos.z - expectedZ) > 0.07f)
                    failures.Add($"beat {beat} {phase}: camera z={pos.z:F3}, expected {expectedZ:F3} " +
                        "(authored easeOutSine z0->-3 over 8 beats).");
                if (Mathf.Abs(pos.x) > 0.02f)
                    failures.Add($"beat {beat} {phase}: camera x={pos.x:F3}, expected ~0.");
                if (Mathf.Abs(pos.y - 1.65f) > 0.05f)
                    failures.Add($"beat {beat} {phase}: camera y={pos.y:F3}, expected ~1.65 " +
                        "(map authors no y motion; binding must not lift the camera).");
                if (Mathf.Abs(camera.fieldOfView - 90f) > 0.5f)
                    failures.Add($"beat {beat} {phase}: camera fov={camera.fieldOfView}, expected 90.");
            }

            foreach (var beat in new[] { 537f, 537.25f, 538f, 539f, 541f, 543f, 545f, 541f, 537.5f })
            {
                atsc.MoveToJsonTime(beat);
                Sample(beat, "immediate");
                yield return null;
                Sample(beat, "post-frame");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Deterministic PLAYBACK coverage for the same authored ease: the stopped-seek cases above
        // prove the pose is synchronous, but the user's report was motion during play. Jenkins'
        // native AudioSource clock is unreliable, so this drives the production IsPlaying state on a
        // virtual input fixture (CameraController.SetLockState reads Mouse.current on toggle) and
        // steps CurrentSeconds directly — each yielded frame still lets production
        // AudioTimeSyncController.Update/TrackAnimator.Update/CameraController.Update run, then the
        // camera's own ObjectAnimator.LateUpdate is invoked once for an end-of-frame sample.
        [UnityTest]
        public IEnumerator Beat537HeadCameraEasesAcrossDeterministicPlaybackFrames()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null; // let CameraController.Update apply the FOV before sampling
            var cameraController = cameraManager.CameraControllers[1];
            var camera = cameraController.Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            var baselinePos = camera.transform.position;
            if (Vector3.Distance(baselinePos, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                failures.Add($"setup: playing camera world={baselinePos}, expected ~(0,1.65,0).");

            var animatorField = typeof(CameraController).GetField("cameraAnimator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cameraAnimator = animatorField?.GetValue(cameraController) as ObjectAnimator;
            Assert.That(cameraAnimator, Is.Not.Null, "cameraAnimator field missing on CameraController.");

            var currentSecondsProperty = typeof(AudioTimeSyncController)
                .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
            Assert.That(currentSecondsProperty, Is.Not.Null,
                "AudioTimeSyncController.CurrentSeconds property missing.");

            InputTestFixture inputFixture = null;
            Mouse virtualMouse = null;
            try
            {
                inputFixture = new InputTestFixture();
                inputFixture.Setup();
                virtualMouse = InputSystem.AddDevice<Mouse>();

                atsc.MoveToJsonTime(536.875f);
                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                if (!atsc.IsPlaying)
                    failures.Add("setup: TogglePlaying did not enter IsPlaying.");
                if (atsc.SongAudioSource.isPlaying)
                    failures.Add("setup: native SongAudioSource still playing after Stop().");

                float? previousZ = null;
                foreach (var beat in new[] { 536.875f, 537f, 537.25f, 538f, 539f, 541f, 543f, 545f })
                {
                    var songBeat = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(beat);
                    currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat));
                    yield return null; // production Update ran once on the stepped clock
                    cameraAnimator.LateUpdate(); // sample the real render-phase pose

                    var pos = camera.transform.position;
                    var expectedZ = -3f * Mathf.Sin(Mathf.Clamp01((beat - 537f) / 8f) * Mathf.PI / 2f);
                    if (!float.IsFinite(pos.z))
                        failures.Add($"beat {beat}: camera z={pos.z} is not finite.");
                    else
                    {
                        if (Mathf.Abs(pos.z - expectedZ) > 0.07f)
                            failures.Add($"beat {beat}: camera z={pos.z:F3}, expected {expectedZ:F3} " +
                                "(authored easeOutSine z0->-3 over 8 beats).");
                        if (previousZ.HasValue && pos.z > previousZ.Value + 0.07f)
                            failures.Add($"beat {beat}: camera z={pos.z:F3} lurched forward past " +
                                $"previous sample {previousZ.Value:F3}.");
                        previousZ = pos.z;
                    }
                    if (Mathf.Abs(pos.x) > 0.02f)
                        failures.Add($"beat {beat}: camera x={pos.x:F3}, expected ~0.");
                    if (Mathf.Abs(pos.y - 1.65f) > 0.05f)
                        failures.Add($"beat {beat}: camera y={pos.y:F3}, expected ~1.65.");
                    if (Mathf.Abs(camera.fieldOfView - 90f) > 0.5f)
                        failures.Add($"beat {beat}: camera fov={camera.fieldOfView}, expected 90.");
                    if (!atsc.IsPlaying)
                        failures.Add($"beat {beat}: AudioTimeSyncController left IsPlaying mid-run.");
                }
            }
            finally
            {
                if (atsc != null)
                {
                    atsc.StopScheduled = false;
                    if (atsc.IsPlaying) atsc.CancelPlaying();
                }
                if (inputFixture != null)
                {
                    virtualMouse = null;
                    inputFixture.TearDown();
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private IEnumerator EnterPlayingFromBasicEventWorkspace()
        {
            editMode = Object.FindAnyObjectByType<EditModeContext>();
            Assert.That(editMode, Is.Not.Null, "No EditModeContext in the loaded scene.");
            previousEditingMode = editMode.EditingMode;
            editingModeChanged = true;
            editMode.EditingMode = EditingMode.BasicEvent;
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = previousPlayerCameraFOV;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerCameraOffsetZ;
            Settings.Instance.CameraFOV = previousCameraFOV;
            if (editingModeChanged && editMode != null)
            {
                editMode.EditingMode = previousEditingMode;
                editingModeChanged = false;
            }
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

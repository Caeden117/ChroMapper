using System.Collections;
using System.Collections.Generic;
using System.IO;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // Portable Salty b532-rewind camera parity on the same SaltyBeat537HeadCameraFixture.json
    // (authored customEvents indices 440-446, 463, 467). Kept in its own fixture class so the
    // per-class map load gives this test a pristine camera rig: on broken source the ease test's
    // leftover asdkm bind would contaminate any same-fixture baseline. These cases were RED before
    // the camera disconnect/home-restore and mode-switch fixes; green now and run in the default
    // suite.
    public class SaltyBeat532CameraRewindParityTest : TestBase
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

        // Portable version of Beat532CameraStaysHomeBeforeAndAfterHeadTrackVisit: no authored Head
        // event exists before b537, so seeks around b532 must leave the camera at its pristine
        // pose; after a b541 visit every rewind must restore the pose AND the rig's original
        // parent (previously stuck at (0,2.25,-2.12) under the asdkm track, surviving a mode cycle).
        [UnityTest]
        public IEnumerator Beat532CameraStaysHomeBeforeAndAfterHeadTrackVisit()
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

            var animatorField = typeof(CameraController).GetField("cameraAnimator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cameraAnimator = animatorField.GetValue(cameraController) as ObjectAnimator;
            Assert.That(cameraAnimator, Is.Not.Null, "cameraAnimator field missing on CameraController.");

            var pristinePos = camera.transform.position;
            var pristineRigParent = cameraAnimator.transform.parent;
            if (Vector3.Distance(pristinePos, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                failures.Add($"setup: pristine playing camera world={pristinePos}, expected ~(0,1.65,0).");

            string HierarchyName(Transform t) => t == null ? "null" : t.name;

            void RequirePristine(string phase)
            {
                var pos = camera.transform.position;
                if (Vector3.Distance(pos, pristinePos) > 0.05f)
                    failures.Add($"{phase}: camera world={pos}, expected pristine {pristinePos}.");
                var parent = cameraAnimator.transform.parent;
                if (!ReferenceEquals(parent, pristineRigParent))
                    failures.Add($"{phase}: camera rig parent={HierarchyName(parent)}, expected " +
                        $"{HierarchyName(pristineRigParent)} (no Head track is authored yet).");
            }

            // No authored Head camera event before b537: the camera must not move at all.
            foreach (var beat in new[] { 531.75f, 532f, 532.25f, 534f, 536.5f })
            {
                atsc.MoveToJsonTime(beat);
                RequirePristine($"beat {beat} immediate");
                yield return null;
                yield return null;
                RequirePristine($"beat {beat} post-frames");
            }

            // Visit past the bind so the b537 Head track animation actually runs.
            atsc.MoveToJsonTime(541f);
            yield return null;
            yield return null;
            yield return null;
            var z541 = camera.transform.position.z;
            if (Mathf.Abs(z541 - -2.121f) > 0.08f)
                failures.Add($"beat 541: camera z={z541:F3}, expected ~-2.121 on the authored ease " +
                    "(b537 Head event must have executed).");

            // Every rewind to a pre-b537 beat must restore the pristine pose and rig parent.
            foreach (var beat in new[] { 536.5f, 534f, 532.25f, 532f, 531.75f, 532f, 222.25f, 532f })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                RequirePristine($"rewind to {beat}");
            }

            // Mode cycle without map reload, still sitting at a pre-b537 beat.
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            atsc.MoveToJsonTime(532f);
            yield return null;
            yield return null;
            RequirePristine("mode cycle at 532");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Portable version of SaltyFullMapPlacementParityTest.LeavingPlayingAtBeat541UnbindsHeadCamera
        // WithoutSeek: leaving Playing must detach the Head track synchronously, not only on a rewind
        // seek — SyncPlayerTrack's UI-mode guard used to run after the AnimationMode early return, so
        // Playing→Normal at a bound beat left the rig under asdkm at its animated pose.
        [UnityTest]
        public IEnumerator LeavingPlayingAtBeat541UnbindsHeadCameraWithoutSeek()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null;
            var cameraController = cameraManager.CameraControllers[1];
            var camera = cameraController.Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            var animatorField = typeof(CameraController).GetField("cameraAnimator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cameraAnimator = animatorField.GetValue(cameraController) as ObjectAnimator;
            Assert.That(cameraAnimator, Is.Not.Null, "cameraAnimator field missing on CameraController.");

            var pristinePos = camera.transform.position;
            var pristineRigParent = cameraAnimator.transform.parent;
            if (Vector3.Distance(pristinePos, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                failures.Add($"setup: pristine playing camera world={pristinePos}, expected ~(0,1.65,0).");

            string HierarchyName(Transform t) => t == null ? "null" : t.name;

            void RequirePristine(string phase)
            {
                var pos = camera.transform.position;
                if (Vector3.Distance(pos, pristinePos) > 0.05f)
                    failures.Add($"{phase}: camera world={pos}, expected pristine {pristinePos}.");
                var parent = cameraAnimator.transform.parent;
                if (!ReferenceEquals(parent, pristineRigParent))
                    failures.Add($"{phase}: camera rig parent={HierarchyName(parent)}, expected " +
                        $"{HierarchyName(pristineRigParent)} (mode switch must detach the Head track).");
            }

            void RequireBound541(string phase)
            {
                var pos = camera.transform.position;
                if (Mathf.Abs(pos.z - -2.121f) > 0.08f)
                    failures.Add($"{phase}: camera z={pos.z:F3}, expected ~-2.121 on the authored ease.");
                if (Mathf.Abs(pos.y - 1.65f) > 0.05f)
                    failures.Add($"{phase}: camera y={pos.y:F3}, expected ~1.65.");
            }

            // Bind the Head track: b537 assigns asdkm to Head, z eases 0→-3 over 8 beats.
            atsc.MoveToJsonTime(541f);
            RequireBound541("beat 541 immediate");
            yield return null;
            RequireBound541("beat 541 post-frame");

            // Leaving Playing without seeking must already detach and restore the home pose.
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            RequirePristine("Normal mode exit immediate");
            yield return null;
            RequirePristine("Normal mode exit post-frame");

            // Preview keeps AnimationMode true, so the detach must come from the UI-mode guard.
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            RequirePristine("Preview mode at 541");

            // Returning to Playing while still at 541 must synchronously rebind asdkm.
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            RequireBound541("re-enter Playing at 541 immediate");
            yield return null;
            RequireBound541("re-enter Playing at 541 post-frame");

            // Rewind to a pre-b537 beat restores the baseline, and a mode exit there is a no-op.
            atsc.MoveToJsonTime(532f);
            yield return null;
            RequirePristine("rewind to 532");
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            RequirePristine("Normal mode exit at 532");
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return null;
            RequirePristine("Playing re-entry at 532");

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

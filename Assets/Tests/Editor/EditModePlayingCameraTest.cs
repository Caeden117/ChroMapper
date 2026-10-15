using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class EditModePlayingCameraTest : PreviewWorkflowTestBase
    {
        [UnityTest]
        public IEnumerator ReturningFromPlayingPreservesGlsCursorAndSnapping()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previousSnapping = atsc.GridMeasureSnapping;
            try
            {
                editMode.EditingMode = EditingMode.GLS;
                yield return null;
                atsc.GridMeasureSnapping = 8;
                atsc.MoveToJsonTime(12.375f);
                Assert.That(atsc.IsSnapped, Is.True);
                uiMode.SetUIMode(UIModeType.Playing, false);
                yield return null;
                uiMode.TryExitPreviewMode();
                yield return null;
                Assert.That(editMode.EditingMode, Is.EqualTo(EditingMode.GLS));
                Assert.That(atsc.CurrentJsonTime, Is.EqualTo(12.375f), "Leaving Playing reset the GLS cursor.");
                Assert.That(atsc.GridMeasureSnapping, Is.EqualTo(8));
                Assert.That(atsc.IsSnapped, Is.True);
            }
            finally
            {
                atsc.GridMeasureSnapping = previousSnapping;
            }
        }

        [UnityTest]
        public IEnumerator ReturningFromPlayingPreservesGlsNodeGridOrigin()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previousSnapping = atsc.GridMeasureSnapping;
            try
            {
                editMode.EditingMode = EditingMode.EventBox;
                atsc.GridMeasureSnapping = 8;
                atsc.VisualBeatOrigin = 22.07f;
                atsc.MoveToJsonTime(23.07f);
                Assert.That(atsc.IsSnapped, Is.True);
                uiMode.SetUIMode(UIModeType.Playing, false);
                yield return null;
                uiMode.TryExitPreviewMode();
                yield return null;
                Assert.That(editMode.EditingMode, Is.EqualTo(EditingMode.EventBox));
                Assert.That(atsc.VisualBeatOrigin, Is.EqualTo(22.07f),
                    "Leaving Playing reset the node grid's relative beat labels and snap origin.");
                Assert.That(atsc.CurrentJsonTime, Is.EqualTo(23.07f));
                Assert.That(atsc.IsSnapped, Is.True);
            }
            finally
            {
                atsc.VisualBeatOrigin = 0f;
                atsc.GridMeasureSnapping = previousSnapping;
            }
        }

        // SwitchingToGlsWhilePlayingKeepsTheGameplayWorkspace reproduces issue 51a19: F2 must not disable the
        // gameplay tracks that drive the playing camera while the editor is in a preview UI mode.
        [Test]
        public void SwitchingToGlsWhilePlayingKeepsTheGameplayWorkspace()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var inputFixture = new InputTestFixture();
            InputAction glsShortcut = null;

            try
            {
                uiMode.SetUIMode(UIModeType.Playing, false);
                editMode.EditingMode = EditingMode.Gameplay;

                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                glsShortcut = new InputAction(binding: "<Keyboard>/f2");
                glsShortcut.performed += editMode.OnGLSEdit;
                glsShortcut.Enable();

                inputFixture.Press(keyboard.f2Key);

                Assert.That(
                    editMode.EditingMode,
                    Is.EqualTo(EditingMode.Gameplay),
                    "F2 changed the editing workspace while Playing mode still depended on gameplay camera tracks.");
            }
            finally
            {
                glsShortcut?.Dispose();
                inputFixture.TearDown();
                uiMode.SetUIMode(UIModeType.Normal, false);
                editMode.EditingMode = EditingMode.Gameplay;
            }
        }

        // SwitchingToBasicEventsWhilePlayingKeepsTheGameplayWorkspace checks the unguarded F3 sibling of issue 51a19.
        [Test]
        public void SwitchingToBasicEventsWhilePlayingKeepsTheGameplayWorkspace()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var inputFixture = new InputTestFixture();
            InputAction basicEventShortcut = null;

            try
            {
                uiMode.SetUIMode(UIModeType.Playing, false);
                editMode.EditingMode = EditingMode.Gameplay;

                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                basicEventShortcut = new InputAction(binding: "<Keyboard>/f3");
                basicEventShortcut.performed += editMode.OnBasicEventEdit;
                basicEventShortcut.Enable();

                inputFixture.Press(keyboard.f3Key);

                Assert.That(
                    editMode.EditingMode,
                    Is.EqualTo(EditingMode.Gameplay),
                    "F3 changed the editing workspace while Playing mode still depended on gameplay camera tracks.");
            }
            finally
            {
                basicEventShortcut?.Dispose();
                inputFixture.TearDown();
                uiMode.SetUIMode(UIModeType.Normal, false);
                editMode.EditingMode = EditingMode.Gameplay;
            }
        }

        // EnteringPlayingFromAnotherWorkspaceKeepsGameplayCameraTracksActive checks the alternate issue 51a19
        // sequence where the workspace changes before the transient playing camera is selected.
        [TestCase(EditingMode.GLS)]
        [TestCase(EditingMode.BasicEvent)]
        public void EnteringPlayingFromAnotherWorkspaceKeepsGameplayCameraTracksActive(EditingMode initialMode)
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var editMode = Object.FindAnyObjectByType<EditModeContext>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var gameplayTracks = FindGameplayTracks();
            var inputFixture = new InputTestFixture();
            InputAction playingShortcut = null;

            try
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
                editMode.EditingMode = initialMode;

                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                playingShortcut = new InputAction(binding: "<Keyboard>/5");
                playingShortcut.performed +=
                    ((CMInput.IUIModeActions)uiMode).OnToggleUIModePlaying;
                playingShortcut.Enable();

                inputFixture.Press(keyboard.digit5Key);

                Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.Playing));
                Assert.That(
                    cameraManager.SelectedCameraController,
                    Is.SameAs(cameraManager.CameraControllers[1]),
                    "Playing mode did not select the transient playing camera.");
                Assert.That(
                    gameplayTracks.activeInHierarchy,
                    Is.True,
                    $"Entering Playing from {initialMode} left its camera-driving gameplay tracks inactive.");
                uiMode.SetUIMode(UIModeType.Normal, false);
                Assert.That(
                    editMode.EditingMode,
                    Is.EqualTo(initialMode),
                    "Leaving Playing did not restore the editing workspace that was active before playback preview.");
            }
            finally
            {
                playingShortcut?.Dispose();
                inputFixture.TearDown();
                cameraManager.SelectCamera(CameraType.Editing);
                uiMode.SetUIMode(UIModeType.Normal, false);
                editMode.EditingMode = EditingMode.Gameplay;
            }
        }

        // EscapingPlayingBeforePauseRestoresEditingCamera checks that mode 5 exits to its prior mode and editing
        // camera before a fresh second Escape opens pause.
        [Test]
        public void EscapingPlayingBeforePauseRestoresEditingCamera()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var pauseManager = Object.FindAnyObjectByType<PauseManager>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var inputFixture = new InputTestFixture();
            InputAction playingAction = null;
            InputAction escapeAction = null;

            try
            {
                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                playingAction = new InputAction(binding: "<Keyboard>/5");
                playingAction.performed +=
                    ((CMInput.IUIModeActions)uiMode).OnToggleUIModePlaying;
                escapeAction = new InputAction(binding: "<Keyboard>/escape");
                escapeAction.performed += pauseManager.OnPauseEditor;
                playingAction.Enable();
                escapeAction.Enable();

                inputFixture.Press(keyboard.digit5Key);
                Assert.That(cameraManager.SelectedCameraController, Is.SameAs(cameraManager.CameraControllers[1]));
                inputFixture.Press(keyboard.escapeKey);

                Assert.That(PauseManager.IsPaused, Is.False, "The first Escape opened pause instead of exiting Playing.");
                Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.Normal));
                Assert.That(
                    cameraManager.SelectedCameraController,
                    Is.SameAs(cameraManager.CameraControllers[0]),
                    "Exiting Playing restored the Normal enum without restoring its editing camera.");

                inputFixture.Release(keyboard.escapeKey);
                inputFixture.Press(keyboard.escapeKey);
                Assert.That(PauseManager.IsPaused, Is.True, "A fresh second Escape did not open pause normally.");
            }
            finally
            {
                playingAction?.Dispose();
                escapeAction?.Dispose();
                if (PauseManager.IsPaused)
                    pauseManager.TogglePause();
                inputFixture.TearDown();
                cameraManager.SelectCamera(CameraType.Editing);
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
        }

        // SwitchingPreviewModesKeepsOriginalEditorReturnMode ensures Preview and Playing share one editor-mode
        // return target instead of treating either transient mode as the other's previous mode.
        [TestCase(UIModeType.Playing, UIModeType.Preview)]
        [TestCase(UIModeType.Preview, UIModeType.Playing)]
        public void SwitchingPreviewModesKeepsOriginalEditorReturnMode(
            UIModeType firstPreviewMode,
            UIModeType secondPreviewMode)
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var pauseManager = Object.FindAnyObjectByType<PauseManager>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var inputFixture = new InputTestFixture();
            InputAction playingAction = null;
            InputAction previewAction = null;
            InputAction escapeAction = null;

            try
            {
                uiMode.SetUIMode(UIModeType.HideGrids, false);
                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                playingAction = new InputAction(binding: "<Keyboard>/5");
                playingAction.performed +=
                    ((CMInput.IUIModeActions)uiMode).OnToggleUIModePlaying;
                previewAction = new InputAction(binding: "<Keyboard>/4");
                previewAction.performed +=
                    ((CMInput.IUIModeActions)uiMode).OnToggleUIModePreview;
                escapeAction = new InputAction(binding: "<Keyboard>/escape");
                escapeAction.performed += pauseManager.OnPauseEditor;
                playingAction.Enable();
                previewAction.Enable();
                escapeAction.Enable();

                var firstKey = firstPreviewMode == UIModeType.Playing
                    ? keyboard.digit5Key
                    : keyboard.digit4Key;
                var secondKey = secondPreviewMode == UIModeType.Playing
                    ? keyboard.digit5Key
                    : keyboard.digit4Key;
                inputFixture.Press(firstKey);
                inputFixture.Release(firstKey);
                inputFixture.Press(secondKey);
                inputFixture.Press(keyboard.escapeKey);

                Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.HideGrids));
                Assert.That(PauseManager.IsPaused, Is.False, "Escape opened pause instead of restoring Hide Grids.");
                Assert.That(
                    cameraManager.SelectedCameraController,
                    Is.SameAs(cameraManager.CameraControllers[0]),
                    "Escape did not restore the editing camera after switching transient preview modes.");
            }
            finally
            {
                playingAction?.Dispose();
                previewAction?.Dispose();
                escapeAction?.Dispose();
                if (PauseManager.IsPaused)
                    pauseManager.TogglePause();
                inputFixture.TearDown();
                cameraManager.SelectCamera(CameraType.Editing);
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
        }

        // Regression source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
        // This case reproduces its camera-disable sequence without loading the original map.
        // DisabledPlayingCameraDoesNotBreakEditingCameraCursorLock exercises the deployed Salty-load NRE
        // hazard: CameraController.OnDisable unconditionally clears the static `instance` even on the
        // playing camera, although only the editing camera's Start owns it, so disabling the playing
        // controller must not break the surviving editing controller's cursor-lock path.
        [Test]
        public void DisabledPlayingCameraDoesNotBreakEditingCameraCursorLock()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var inputFixture = new InputTestFixture();

            try
            {
                inputFixture.Setup();
                InputSystem.AddDevice<Mouse>();
                InputSystem.AddDevice<Keyboard>();

                uiMode.SetUIMode(UIModeType.Preview, false);
                cameraManager.SelectCamera(CameraType.Editing);
                var editingController = cameraManager.CameraControllers[0];
                var playingController = cameraManager.CameraControllers[1];
                var wasPlayingEnabled = playingController.enabled;
                var previousLockState = Cursor.lockState;

                try
                {
                    playingController.enabled = false;
                    editingController.SetLockState(true);
                    var instance = typeof(CameraController)
                        .GetField("instance", BindingFlags.Static | BindingFlags.NonPublic)
                        .GetValue(null);
                    Assert.That(
                        instance,
                        Is.SameAs(editingController),
                        "Disabling the playing camera cleared the editing camera's static owner.");
                    // Batch mode never applies an OS cursor lock, so only verify it where it can take.
                    if (!Application.isBatchMode)
                    {
                        Assert.That(
                            Cursor.lockState,
                            Is.EqualTo(CursorLockMode.Locked),
                            "Disabling the playing camera broke the editing camera's cursor lock.");
                    }
                }
                finally
                {
                    playingController.enabled = wasPlayingEnabled;
                    // SetLockState tracks a logical lock owner even when batch mode never applies the
                    // native lock, so release editing's claim here before restoring the raw lock state —
                    // otherwise the leaked owner blocks a later test's unlock.
                    editingController.SetLockState(false);
                    Cursor.lockState = previousLockState;
                }
            }
            finally
            {
                inputFixture.TearDown();
                cameraManager.SelectCamera(CameraType.Editing);
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
        }

        // Camera softlock tests inspect the authoritative scene object even after a workspace deactivates it.
        private static GameObject FindGameplayTracks() => Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(transform => transform.name == "Gameplay Container Tracks")
            .gameObject;
    }

    // ResumingPlayingDoesNotRestoreEditingCameraMousePosition reproduces the reported resume bug directly:
    // a right-click camera move in Normal leaves the editing CameraController enabled with a stale
    // savedMousePos. Playing deliberately leaves the cursor free — the historical UIMode.OnPlayToggle
    // lock through the playing controller never reliably held, and the idle editing controller's Update
    // could steal it and warp the cursor back to that stale spot, so resume must not recreate the warp.
    // The cursor calls are recorded through the CameraController.CursorState seam because batch mode owns
    // no OS cursor, and the test invokes the real OnPlayToggle subscriber instead of starting native
    // audio, so the case has no audio-backend or window-focus dependency.
    public class CameraCursorLockTest : TestBase
    {
        private InputTestFixture input;
        private Mouse mouse;
        private CameraManager cameras;
        private UIMode uiMode;
        private CameraController.ICursorState previousCursor;
        private RecordingCursor cursor;
        private bool previousAnimations;

        [SetUp]
        public void SetUpCursor()
        {
            previousAnimations = Settings.Instance.Animations;
            cameras = Object.FindAnyObjectByType<CameraManager>();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            previousCursor = CameraController.CursorState;
            cursor = new RecordingCursor();
            CameraController.CursorState = cursor;
            input = new InputTestFixture();
            input.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Keyboard>();
        }

        // Entering Playing via SetUIMode plus a mid-test failure can leave preview mode and the playing
        // camera selected across the teardown map reload, which crashes the runner — restore Normal and
        // the editing camera here rather than inline after the assertions.
        [UnityTearDown]
        public IEnumerator RestoreCursor()
        {
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameras.SelectCamera(CameraType.Editing);
            cameras.CameraControllers[0].SetLockState(false);
            cameras.CameraControllers[1].SetLockState(false);
            Settings.Instance.Animations = previousAnimations;
            CameraController.CursorState = previousCursor;
            input.TearDown();
            TestUtils.ResetSharedInputState();
            yield break;
        }

        [Test]
        public void ResumingPlayingDoesNotRestoreEditingCameraMousePosition()
        {
            var editing = cameras.CameraControllers[0];
            var editingPosition = new Vector2(123, 234);
            var resumePosition = new Vector2(456, 345);

            input.Set(mouse.position, editingPosition);
            editing.SetLockState(true);
            editing.SetLockState(false);
            Assert.That(cursor.Warps, Is.EqualTo(new[] { editingPosition }));

            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            Assert.That(editing.enabled, Is.True);
            Assert.That(editing.Camera.enabled, Is.False);

            input.Set(mouse.position, resumePosition);
            cursor.Warps.Clear();
            TogglePlayingCursor(true);
            // Playing deliberately leaves the cursor free: playback never reliably held a lock anyway,
            // and an unowned cursor lets the idle editing camera avoid stealing it to warp stale.
            Assert.That(cursor.LockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(cursor.Warps, Is.Empty,
                "Resuming Playing warped the cursor even though playback owns no cursor lock.");

            // Simulates the next idle frame after resume: the still-enabled editing controller runs its
            // Update while Playing owns no lock, which is the path that warped the cursor back to the
            // stale right-click position in the reported bug.
            typeof(CameraController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editing, null);
            Assert.That(cursor.Warps, Is.Empty,
                "The idle editing camera warped the cursor to its stale right-click position while Playing resumed.");
            Assert.That(cursor.LockState, Is.EqualTo(CursorLockMode.None));

            TogglePlayingCursor(false);
            Assert.That(cursor.LockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(cursor.Warps, Is.Empty,
                "Stopping Playing warped the cursor even though playback owned no cursor lock.");
        }

        // RepeatedLockRequestsPreserveTheOwnersRestorePosition covers the idempotency contract the owner
        // fix must keep: a second lock request while already locked must not overwrite the saved restore
        // position, and a non-owner's requests must neither steal nor clear the lock.
        [Test]
        public void RepeatedLockRequestsPreserveTheOwnersRestorePosition()
        {
            var editing = cameras.CameraControllers[0];
            var playing = cameras.CameraControllers[1];
            var original = new Vector2(123, 234);

            input.Set(mouse.position, original);
            playing.SetLockState(true);
            input.Set(mouse.position, new Vector2(456, 345));
            playing.SetLockState(true);
            editing.SetLockState(true);
            playing.SetLockState(false);
            Assert.That(cursor.LockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(cursor.Warps, Is.EqualTo(new[] { original }));
            editing.SetLockState(false);
            Assert.That(cursor.Warps.Count, Is.EqualTo(1));
        }

        // DisablingPlayingCursorOwnerReleasesOnlyItsOwnLock covers both halves of the OnDisable release:
        // disabling the owner must release the global lock exactly once, while disabling the other
        // (still-lockless) controller must leave the owner's lock and saved restore position untouched.
        [TestCase(false)]
        [TestCase(true)]
        public void DisablingPlayingCursorOwnerReleasesOnlyItsOwnLock(bool disableOwner)
        {
            var editing = cameras.CameraControllers[0];
            var playing = cameras.CameraControllers[1];
            var position = new Vector2(456, 345);

            input.Set(mouse.position, position);
            playing.SetLockState(true);

            var disabled = disableOwner ? playing : editing;
            var wasEnabled = disabled.enabled;
            try
            {
                disabled.enabled = false;
                Assert.That(cursor.LockState, Is.EqualTo(disableOwner ? CursorLockMode.None : CursorLockMode.Locked));
                Assert.That(cursor.Warps.Count, Is.EqualTo(disableOwner ? 1 : 0));
                if (disableOwner)
                    Assert.That(cursor.Warps[0], Is.EqualTo(position));
            }
            finally
            {
                disabled.enabled = wasEnabled;
            }
        }

        // Resume/pause fires AudioTimeSyncController.OnPlayToggled; invoking the production subscriber
        // keeps the regression on the real OnPlayToggle path without requiring a playing audio clock.
        private void TogglePlayingCursor(bool playing) => typeof(UIMode)
            .GetMethod("OnPlayToggle", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(uiMode, new object[] { playing });

        private sealed class RecordingCursor : CameraController.ICursorState
        {
            public CursorLockMode LockState { get; set; }
            public readonly List<Vector2> Warps = new();
            public void Warp(Vector2 position) => Warps.Add(position);
        }
    }

    public abstract class PreviewWorkflowTestBase : TestBase
    {
        private bool previousAnimations;

        [SetUp]
        public void CapturePreviewSettings()
        {
            previousAnimations = Settings.Instance.Animations;
        }

        [UnityTearDown]
        public IEnumerator RestorePreviewWorkflow()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            var cameras = Object.FindAnyObjectByType<CameraManager>();
            if (cameras != null)
            {
                cameras.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = previousAnimations;
            yield break;
        }
    }
}

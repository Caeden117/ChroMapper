using System.Reflection;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tests.Editor
{
    public class UIModePreviewEscapeTest : PreviewWorkflowTestBase
    {
        // HideGridsHidesAndRestoresGlsEventLaneGridLines covers GLS lanes that are registered outside the
        // original gameplay-grid renderer list and must still follow the global Hide Grids UI mode.
        [Test]
        public void HideGridsHidesAndRestoresGlsEventLaneGridLines()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var gridLane = (GridLane)typeof(GLSEventGridProvider)
                .GetField("gridLane", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(provider);

            try
            {
                uiMode.SetUIMode(UIModeType.Normal, false);

                Assert.That(gridLane.XZ.Grid.enabled, Is.True, "The GLS beat grid was not visible before hiding grids.");
                Assert.That(gridLane.XY.Grid.enabled, Is.True, "The GLS vertical grid was not visible before hiding grids.");

                uiMode.SetUIMode(UIModeType.HideGrids, false);

                Assert.That(gridLane.XZ.Grid.enabled, Is.False, "Hide Grids left the GLS beat grid visible.");
                Assert.That(gridLane.XY.Grid.enabled, Is.False, "Hide Grids left the GLS vertical grid visible.");

                uiMode.SetUIMode(UIModeType.Normal, false);

                Assert.That(gridLane.XZ.Grid.enabled, Is.True, "Leaving Hide Grids did not restore the GLS beat grid.");
                Assert.That(gridLane.XY.Grid.enabled, Is.True, "Leaving Hide Grids did not restore the GLS vertical grid.");
            }
            finally
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
        }

        // EscapeFromPreviewRestoresThePreviousUIMode reproduces Escape opening the pause menu and losing the
        // UI mode that was active immediately before Preview.
        [TestCase(UIModeType.Normal)]
        [TestCase(UIModeType.HideUI)]
        [TestCase(UIModeType.HideGrids)]
        public void EscapeFromPreviewRestoresThePreviousUIMode(UIModeType previousMode)
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var pauseManager = Object.FindAnyObjectByType<PauseManager>();
            var inputFixture = new InputTestFixture();
            InputAction escapeAction = null;

            try
            {
                uiMode.SetUIMode(previousMode, false);
                uiMode.SetUIMode(UIModeType.Preview, false);

                inputFixture.Setup();
                var keyboard = InputSystem.AddDevice<Keyboard>();
                escapeAction = new InputAction(binding: "<Keyboard>/escape");
                escapeAction.performed += pauseManager.OnPauseEditor;
                escapeAction.Enable();

                inputFixture.Press(keyboard.escapeKey);

                Assert.That(UIMode.SelectedMode, Is.EqualTo(previousMode));
                Assert.That(PauseManager.IsPaused, Is.False, "Escape opened the pause menu instead of exiting Preview.");

                // EscapeFromPreviewRestoresThePreviousUIMode must consume only the Preview exit; a fresh second
                // press should retain Escape's normal pause behavior after Preview has ended.
                inputFixture.Release(keyboard.escapeKey);
                inputFixture.Press(keyboard.escapeKey);

                Assert.That(PauseManager.IsPaused, Is.True, "A second Escape did not open the pause menu normally.");
                Assert.That(
                    UIMode.SelectedMode,
                    Is.EqualTo(previousMode),
                    "Opening pause changed the ordinary UI mode instead of pausing it in place.");
            }
            finally
            {
                escapeAction?.Dispose();
                inputFixture.TearDown();
                if (PauseManager.IsPaused)
                    pauseManager.TogglePause();
                uiMode.SetUIMode(UIModeType.Normal, false);
            }
        }

        // ReenteringPlayingAfterEscapeStillConsumesTheFirstEscape reproduces mode 5 opening pause while exiting,
        // then re-entering Playing when the same pause is closed.
        [Test]
        public void ReenteringPlayingAfterEscapeStillConsumesTheFirstEscape()
        {
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var pauseManager = Object.FindAnyObjectByType<PauseManager>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var inputFixture = new InputTestFixture();
            InputAction playingAction = null;
            InputAction escapeAction = null;

            try
            {
                uiMode.SetUIMode(UIModeType.HideGrids, false);
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
                inputFixture.Press(keyboard.escapeKey);

                Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.HideGrids));
                Assert.That(PauseManager.IsPaused, Is.False, "The first Escape opened pause while exiting Playing.");

                inputFixture.Release(keyboard.escapeKey);
                inputFixture.Press(keyboard.escapeKey);
                Assert.That(PauseManager.IsPaused, Is.True, "The second Escape did not open pause normally.");
                Assert.That(
                    UIMode.SelectedMode,
                    Is.EqualTo(UIModeType.HideGrids),
                    "Opening pause changed Hide Grids instead of pausing it in place.");
                inputFixture.Release(keyboard.escapeKey);
                inputFixture.Press(keyboard.escapeKey);
                Assert.That(PauseManager.IsPaused, Is.False, "Escape did not close the pause menu.");
                Assert.That(
                    UIMode.SelectedMode,
                    Is.EqualTo(UIModeType.HideGrids),
                    "Closing pause re-entered Playing instead of restoring the exited mode.");

                inputFixture.Release(keyboard.escapeKey);
                inputFixture.Release(keyboard.digit5Key);
                inputFixture.Press(keyboard.digit5Key);
                inputFixture.Press(keyboard.escapeKey);

                Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.HideGrids));
                Assert.That(PauseManager.IsPaused, Is.False, "Playing stopped consuming its first Escape after re-entry.");
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
    }
}

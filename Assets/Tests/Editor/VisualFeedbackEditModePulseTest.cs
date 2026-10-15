using System.Collections;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // The Grid XY lane owns the scene VisualFeedback pulse. Switching Gameplay->GLS deactivates that lane
    // mid-pulse: Unity kills the VisualFeedbackAnim coroutine while t stays positive, so the next note
    // callback after returning to Gameplay takes the t>0 branch and never restarts the animation, leaving
    // the renderer stuck at its interrupted scale forever.
    public class VisualFeedbackEditModePulseTest : PreviewWorkflowTestBase
    {
        // Linux Jenkins has no reliable native audio clock, so playback time is driven directly through the
        // same private setter BasicEventChunkingTestBase uses.
        private static readonly PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        private static readonly FieldInfo targetRendererField = typeof(VisualFeedback)
            .GetField("targetRenderer", BindingFlags.Instance | BindingFlags.NonPublic);

        private AudioTimeSyncController atsc;
        private EditModeContext editModeContext;

        [UnityTearDown]
        public IEnumerator RestoreEditorState()
        {
            // A failed assertion must still leave playback, workspace, UI mode, and camera in their editing
            // defaults so the teardown scene reload cannot start into a half-playing editor.
            if (atsc != null)
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }

            if (editModeContext != null) editModeContext.EditingMode = EditingMode.Gameplay;

            var uiMode = Object.FindAnyObjectByType<UIMode>();
            if (uiMode != null) uiMode.SetUIMode(UIModeType.Normal, false);

            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            if (cameraManager != null) cameraManager.SelectCamera(CameraType.Editing);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NotePulseInterruptedByModeSwitchAnimatesAgainAfterGameplayReturns()
        {
            var firstNote = PlaceUtils.Place(new BaseNote { JsonTime = 2f, Type = (int)NoteType.Red });
            var secondNote = PlaceUtils.Place(new BaseNote { JsonTime = 4f, Type = (int)NoteType.Red });
            yield return null;

            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            Assert.That(editModeContext.EditingMode, Is.EqualTo(EditingMode.Gameplay));
            Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.Normal));

            var feedback = Object.FindAnyObjectByType<VisualFeedback>(FindObjectsInactive.Include);
            Assert.That(feedback, Is.Not.Null, "The mapper scene has no VisualFeedback component.");
            Assert.That(targetRendererField, Is.Not.Null, "VisualFeedback.targetRenderer was not found.");
            var renderer = (Renderer)targetRendererField.GetValue(feedback);
            Assert.That(renderer, Is.Not.Null, "The scene VisualFeedback has no target renderer bound.");
            var baselineMagnitude = renderer.transform.localScale.magnitude;

            StartDeterministicPlayback(atsc);

            // The "Vertical Grid Callback" controller has Offset 0 and fires once CurrentSongBpmTime reaches a
            // note, so jumping the playhead directly onto the note keeps the test under one second: the only
            // frames needed are the ones running LateUpdate and the pulse coroutine.
            SetSongBpmTime(firstNote.SongBpmTime + 0.001f);
            yield return WaitForPulse(renderer, baselineMagnitude);
            Assert.That(
                renderer.transform.localScale.magnitude,
                Is.GreaterThan(baselineMagnitude * 1.03f),
                "The first note never visibly pulsed the grid renderer before the mode switch.");

            // Interrupt the in-flight pulse the same way the F2 workspace switch does; the lane deactivates
            // synchronously inside the EditingMode setter.
            editModeContext.EditingMode = EditingMode.GLS;
            Assert.That(
                feedback.gameObject.activeInHierarchy,
                Is.False,
                "Switching to GLS did not deactivate the Gameplay grid lane that owns VisualFeedback.");

            // Interrupting a pulse must reset the renderer instead of leaving it frozen at a nonbaseline
            // scale; the unfixed component keeps the interrupted scale because its coroutine can no longer decay t.
            Assert.That(
                renderer.transform.localScale.magnitude,
                Is.EqualTo(baselineMagnitude).Within(baselineMagnitude * 0.01f),
                "Interrupting the pulse left the renderer frozen above baseline instead of resetting it.");

            editModeContext.EditingMode = EditingMode.Gameplay;
            Assert.That(
                feedback.gameObject.activeInHierarchy,
                Is.True,
                "Returning to Gameplay did not reactivate the VisualFeedback grid lane.");

            // The second note crossing the same callback path must start a fresh pulse while playback stays
            // active; on the unfixed component t is still positive, so no coroutine restarts.
            SetSongBpmTime(secondNote.SongBpmTime + 0.001f);
            yield return WaitForPulse(renderer, baselineMagnitude);
            Assert.That(atsc.IsPlaying, Is.True, "Playback stopped before the second note pulse.");
            Assert.That(
                renderer.transform.localScale.magnitude,
                Is.GreaterThan(baselineMagnitude * 1.03f),
                "The second note callback left the renderer at its interrupted scale instead of pulsing again.");
        }

        private static void StartDeterministicPlayback(AudioTimeSyncController controller)
        {
            Assert.That(controller.IsPlaying, Is.False, "Deterministic playback did not start paused.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "AudioTimeSyncController.CurrentSeconds was not found.");

            controller.TogglePlaying();
            controller.SongAudioSource.Stop();
            controller.StopScheduled = true;

            Assert.That(controller.IsPlaying, Is.True, "Deterministic playback did not enter the playing state.");
            Assert.That(
                controller.SongAudioSource.isPlaying,
                Is.False,
                "Deterministic playback unexpectedly retained a native audio backend.");
        }

        private void SetSongBpmTime(float songBpmTime) =>
            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBpmTime));

        // A note callback lands in LateUpdate and the pulse coroutine applies its first frame right after, so
        // a handful of frames bounds the wait without any wall-clock dependency.
        private static IEnumerator WaitForPulse(Renderer renderer, float baselineMagnitude)
        {
            for (var frame = 0; frame < 5; frame++)
            {
                if (renderer.transform.localScale.magnitude > baselineMagnitude * 1.03f) yield break;
                yield return null;
            }
        }
    }
}

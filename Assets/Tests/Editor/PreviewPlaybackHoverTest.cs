using System.Collections;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Preview and Playing UI modes hand the pointer to playback once the song is unpaused: a hover held at
    // unpause must clear immediately, and no container may acquire a hover outline until the song pauses again.
    // Application.isFocused is false in batchmode so the raycast acquisition branch cannot run here; held-hover
    // cases use a dragged container, which the unfocused and missed-raycast branches cannot clear.
    public class PreviewPlaybackHoverTest : PreviewWorkflowTestBase
    {
        private static readonly MethodInfo updateMethod = typeof(BeatmapInputController<NoteContainer>)
            .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

        private UIMode uiMode;
        private AudioTimeSyncController atsc;
        private CameraManager cameraManager;
        private BeatmapNoteInputController noteInput;
        private NoteContainer container;
        private InputTestFixture inputFixture;
        private Mouse virtualMouse;

        [UnityTest]
        public IEnumerator PlayingPreviewClearsHoveredNoteOnUnpause(
            [Values(UIModeType.Preview, UIModeType.Playing)] UIModeType mode)
        {
            var note = PlaceUtils.Place(new BaseNote { JsonTime = 2f });
            yield return null;

            SetupSharedState();
            container = GetNoteContainer(note);
            uiMode.SetUIMode(mode, false);
            HoldHover();

            StartDeterministicPlayback();
            try
            {
                InvokeUpdate();

                Assert.That(noteInput.IsHovering, Is.False,
                    "Unpausing in a preview UI mode kept the held hover active.");
                Assert.That(container.Highlighted, Is.False,
                    "Unpausing in a preview UI mode left the hover outline visible.");
            }
            finally
            {
                RestoreSharedState();
            }
        }

        // Only the playback gate may drop the held hover; paused previews and non-preview playback keep it.
        [UnityTest]
        public IEnumerator HeldHoverSurvivesOutsideUnpausedPreview(
            [Values(UIModeType.Preview, UIModeType.Playing, UIModeType.Normal)] UIModeType mode,
            [Values(false, true)] bool playing)
        {
            var note = PlaceUtils.Place(new BaseNote { JsonTime = 2f });
            yield return null;

            SetupSharedState();
            container = GetNoteContainer(note);
            uiMode.SetUIMode(mode, false);
            HoldHover();
            if (playing) StartDeterministicPlayback();
            try
            {
                InvokeUpdate();

                var suppressed = UIMode.PreviewMode && playing;
                Assert.That(noteInput.IsHovering, Is.EqualTo(!suppressed),
                    $"Hover state was wrong for mode={mode} playing={playing}.");
                Assert.That(container.Highlighted, Is.EqualTo(!suppressed),
                    $"Hover outline was wrong for mode={mode} playing={playing}.");
            }
            finally
            {
                RestoreSharedState();
            }
        }

        // The suppression gate is the narrow conjunction of the shared preview UI mode and an unpaused song.
        [UnityTest]
        public IEnumerator PlaybackHoverSuppressionMatchesUnpausedPreview(
            [Values(UIModeType.Preview, UIModeType.Playing, UIModeType.Normal)] UIModeType mode,
            [Values(false, true)] bool playing)
        {
            yield return null;

            SetupSharedState();
            uiMode.SetUIMode(mode, false);
            if (playing) StartDeterministicPlayback();
            try
            {
                Assert.That(
                    BeatmapInputController<NoteContainer>.PlaybackHoverSuppressed,
                    Is.EqualTo(UIMode.PreviewMode && playing),
                    $"Suppression was wrong for mode={mode} playing={playing}.");
            }
            finally
            {
                RestoreSharedState();
            }
        }

        private void SetupSharedState()
        {
            uiMode = Object.FindAnyObjectByType<UIMode>();
            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            noteInput = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            Assert.That(noteInput, Is.Not.Null, "The production note input controller was not available.");

            // CameraController.SetLockState reads Mouse.current when Playing mode toggles playback, so the
            // isolated runtime owns a dedicated virtual device rather than the absent host mouse.
            inputFixture = new InputTestFixture();
            inputFixture.Setup();
            virtualMouse = InputSystem.AddDevice<Mouse>();
        }

        private void HoldHover()
        {
            container.Dragged = true;
            container.Highlighted = true;
            noteInput.IsHovering = true;
            noteInput.HoveredObject = container;
        }

        // BasicEventChunkingTestBase.StartDeterministicPlaybackAtSongBpmTime established this Jenkins-safe pattern:
        // enter the production playing state, then detach the native audio backend and block its delayed stop.
        private void StartDeterministicPlayback()
        {
            atsc.TogglePlaying();
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            Assert.That(atsc.IsPlaying, Is.True, "Deterministic playback did not enter the production playing state.");
        }

        private static NoteContainer GetNoteContainer(BaseNote note)
        {
            var collection =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Assert.That(collection.LoadedContainers.TryGetValue(note, out var loaded), Is.True,
                "The placed note did not spawn a container.");
            return (NoteContainer)loaded;
        }

        private void InvokeUpdate() => updateMethod.Invoke(noteInput, null);

        private void RestoreSharedState()
        {
            BeatmapRaycastCache.Invalidate();
            if (atsc != null)
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }

            if (noteInput != null)
            {
                noteInput.IsHovering = false;
                noteInput.IsSelecting = false;
                noteInput.HoveredObject = null;
            }

            if (container != null)
            {
                container.Dragged = false;
                container.Highlighted = false;
                container = null;
            }

            if (inputFixture != null)
            {
                virtualMouse = null;
                inputFixture.TearDown();
                inputFixture = null;
            }

            if (cameraManager != null) cameraManager.SelectCamera(CameraType.Editing);
            if (uiMode != null) uiMode.SetUIMode(UIModeType.Normal, false);
        }
    }
}

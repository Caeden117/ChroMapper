using System;
using System.Reflection;
using NUnit.Framework;

namespace Tests.Infrastructure
{
    internal sealed class DeterministicPlaybackClock : IDisposable
    {
        private static readonly PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        private readonly AudioTimeSyncController controller;
        private readonly bool previousEnabled;

        public DeterministicPlaybackClock(AudioTimeSyncController controller)
        {
            this.controller = controller;
            previousEnabled = controller.enabled;
            // Update adds real frame time even when native audio is stopped. Only the test may advance this clock.
            controller.enabled = false;
        }

        public void StartAtJsonTime(float jsonTime)
        {
            controller.MoveToJsonTime(jsonTime);
            Resume();
        }

        public void Resume()
        {
            // TogglePlaying starts native audio and applies latency again on every resume.
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(controller, controller.CurrentSongBpmTime);
        }

        public void AdvanceToJsonTime(float jsonTime)
        {
            Assert.That(controller.IsPlaying, Is.True, "Playback must be running before advancing its clock.");
            var songBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);
            currentSecondsProperty.SetValue(controller, controller.GetSecondsFromBeat(songBpmTime));
        }

        public void Pause()
        {
            TestUtils.PauseDeterministicPlayback(controller);
        }

        public void Dispose()
        {
            if (controller == null)
                return;

            if (controller.IsPlaying)
                controller.CancelPlaying();

            controller.enabled = previousEnabled;
        }
    }
}

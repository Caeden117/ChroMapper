using System.Reflection;
using UnityEngine;

namespace Tests.Infrastructure
{
    // Advances AudioTimeSyncController.CurrentSeconds from the Update stage at gameplay order so playback
    // tests keep a deterministic clock without depending on a native audio backend advancing.
    [DefaultExecutionOrder(100)]
    internal class DeterministicSecondsDriver : MonoBehaviour
    {
        private static readonly PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        public AudioTimeSyncController Atsc;
        public float SecondsPerFrame;
        public float Seconds;
        public bool Running;

        // When set, each Update consumes the next entry cyclically instead of SecondsPerFrame so one
        // sweep can mix frame durations.
        public float[] StepPattern;
        private int stepPatternIndex;

        // When set, the step is clamped so Seconds lands exactly on the target and Running flips off
        // in the same Update; sampling code can then capture the exact beat instead of the nearest frame.
        public float? TargetSeconds;

        private void Update()
        {
            if (!Running || Atsc == null)
            {
                return;
            }

            var step = StepPattern != null && StepPattern.Length > 0
                ? StepPattern[stepPatternIndex++ % StepPattern.Length]
                : SecondsPerFrame;
            Seconds += step;
            if (TargetSeconds.HasValue)
            {
                var target = TargetSeconds.Value;
                var reached = step >= 0f ? Seconds >= target : Seconds <= target;
                if (reached)
                {
                    Seconds = target;
                    Running = false;
                }
            }

            currentSecondsProperty.SetValue(Atsc, Seconds);
        }
    }
}

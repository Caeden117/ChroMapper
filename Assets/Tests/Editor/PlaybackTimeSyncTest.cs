using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class PlaybackTimeSyncTest : TestBase
    {
        private static readonly MethodInfo update = AccessTools.Method(typeof(AudioTimeSyncController), "Update");
        private static float audioSeconds;
        private static float deltaTime;
        private static float smoothDeltaTime;

        private AudioTimeSyncController atsc;
        private Harmony harmony;
        private bool previousEnabled;

        [SetUp]
        public void ControlClockInputs()
        {
            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousEnabled = atsc.enabled;
            atsc.enabled = false;
            harmony = new Harmony("ChroMapper.Tests.PlaybackTimeSync");
            harmony.Patch(update, transpiler: new HarmonyMethod(typeof(PlaybackTimeSyncTest), nameof(ReplaceClockInputs)));
        }

        [TearDown]
        public void RestoreClockInputs()
        {
            harmony?.UnpatchSelf();
            if (atsc != null)
            {
                if (atsc.IsPlaying)
                    atsc.CancelPlaying();

                atsc.StopScheduled = false;
                atsc.enabled = previousEnabled;
            }
        }

        [Test]
        public void CapturedNativeFramesDoNotRewindOrOvershootAudio(
            [Values(false, true)] bool longFrame,
            [Values(0, 32)] int latency)
        {
            // Native Nutella capture: audio stayed monotonic while the shader clock jumped back 43 ms and 290 ms.
            StartPlayback(longFrame ? 17.527131f : 17.1428566f, latency);
            audioSeconds = longFrame ? 17.761519f : 17.1428566f;
            Tick(longFrame ? 0.297565f : 0.050575f, longFrame ? 0.075183f : 0.017660f);
            var before = atsc.CurrentSeconds;
            Tick(longFrame ? 0.007419f : 0.007525f, longFrame ? 0.061630f : 0.015633f);

            Assert.That(atsc.CurrentSeconds, Is.GreaterThanOrEqualTo(before),
                "Continuous playback rewound despite a monotonic native audio position.");
            Assert.That(before + (latency / 1000f) - audioSeconds, Is.LessThanOrEqualTo(0.04f),
                "A frame already represented by the audio position was added again after synchronization.");
            Assert.That(Shader.GetGlobalVector("_SongTime").y, Is.EqualTo(atsc.CurrentSeconds));
        }

        [Test]
        public void RepeatedCapturedStallsKeepThirtySecondsOfPlaybackMonotonicAndSynchronized(
            [Values(0, 32)] int latency)
        {
            StartPlayback(20f, latency);
            var audio = 20f;
            var elapsed = 0f;
            var smooth = 1f / 120f;
            var frame = 0;
            while (elapsed < 30f)
            {
                var step = frame++ % 360 == 120 ? 0.297565f : 1f / 120f;
                audio += step;
                elapsed += step;
                smooth += (step - smooth) * 0.2f;
                // The native capture used 1024-sample DSP buffers at 48 kHz.
                audioSeconds = 20f + (Mathf.Floor((audio - 20f) * 48000f / 1024f) * 1024f / 48000f);
                var before = atsc.CurrentSeconds;
                Tick(step, smooth);

                Assert.That(atsc.CurrentSeconds, Is.GreaterThanOrEqualTo(before),
                    $"Frame {frame} rewound the visual clock during uninterrupted playback.");
                Assert.That(Mathf.Abs(atsc.CurrentSeconds + (latency / 1000f) - audioSeconds),
                    Is.LessThanOrEqualTo(0.04f),
                    $"Frame {frame} left the visuals out of sync with the continuously advancing audio.");
            }
        }

        [Test]
        public void PauseSnappingAndResumeCompensationDoNotCauseSubsequentRewinds(
            [Values(0, 32)] int latency)
        {
            StartPlayback(17.19f, latency);
            atsc.TogglePlaying();
            var snapped = atsc.CurrentSeconds;
            Assert.That(atsc.IsSnapped, Is.True);
            ResumeWithoutNativeAudio();
            Assert.That(atsc.CurrentSeconds, Is.EqualTo(snapped - (latency / 1000f)).Within(0.00001f));
            audioSeconds = snapped;
            Tick(0.03f, 0.01f);
            var before = atsc.CurrentSeconds;
            Tick(0.02f, 0.012f);
            Tick(0.008f, 0.011f);

            Assert.That(atsc.CurrentSeconds, Is.GreaterThanOrEqualTo(before),
                "Waiting for native audio after resuming must not undo a visual frame already displayed.");
        }

        private void StartPlayback(float rawSeconds, int latency)
        {
            Settings.Instance.AudioLatencyCompensation = latency;
            atsc.GridMeasureSnapping = 4;
            atsc.MoveToTimeInSeconds(rawSeconds);
            ResumeWithoutNativeAudio();
        }

        private void ResumeWithoutNativeAudio()
        {
            atsc.TogglePlaying();
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            Assert.That(atsc.IsPlaying, Is.True);
            Assert.That(atsc.SongAudioSource.isPlaying, Is.False,
                "This regression must not depend on a native audio device advancing.");
        }

        private void Tick(float delta, float smooth)
        {
            deltaTime = delta;
            smoothDeltaTime = smooth;
            update.Invoke(atsc, null);
        }

        // Substitute only external clock reads in the compiled Update; all timing math and scene notifications remain production code.
        private static IEnumerable<CodeInstruction> ReplaceClockInputs(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                MethodInfo replacement = null;
                if (instruction.Calls(AccessTools.PropertyGetter(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.CurrentAudioSeconds))))
                    replacement = AccessTools.Method(typeof(PlaybackTimeSyncTest), nameof(ReadAudioSeconds));
                else if (instruction.Calls(AccessTools.PropertyGetter(typeof(AudioSource), nameof(AudioSource.isPlaying))))
                    replacement = AccessTools.Method(typeof(PlaybackTimeSyncTest), nameof(ReadAudioPlaying));
                else if (instruction.Calls(AccessTools.PropertyGetter(typeof(Time), nameof(Time.deltaTime))))
                    replacement = AccessTools.Method(typeof(PlaybackTimeSyncTest), nameof(ReadDeltaTime));
                else if (instruction.Calls(AccessTools.PropertyGetter(typeof(Time), nameof(Time.smoothDeltaTime))))
                    replacement = AccessTools.Method(typeof(PlaybackTimeSyncTest), nameof(ReadSmoothDeltaTime));

                if (replacement != null)
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                }

                yield return instruction;
            }
        }

        private static float ReadAudioSeconds(AudioTimeSyncController _) => audioSeconds;
        private static bool ReadAudioPlaying(AudioSource _) => true;
        private static float ReadDeltaTime() => deltaTime;
        private static float ReadSmoothDeltaTime() => smoothDeltaTime;
    }
}

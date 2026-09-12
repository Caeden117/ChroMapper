using System.Reflection;
using Beatmap.Base;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class BasicMovementCallbackTimingTest : TestBase
    {
        private const int ExactCallbackIndex = 9;
        private const float PreviewCallbackRate = 90f;

        [Test]
        public void LightRotationExactCallbackBoundaryUsesSharedPreviewClock()
        {
            var gameObject = new GameObject(nameof(LightRotationExactCallbackBoundaryUsesSharedPreviewClock));
            try
            {
                var effect = gameObject.AddComponent<LightRotationEffect>();
                effect.Atsc = GetAudioTimeSyncController();
                var current = new LightRotationStateData(new BaseEvent { Type = 12, Value = -1 });
                SetExactCallbackStartTime(effect.Atsc, current);

                InvokeComputeSnapshot(effect, new LightRotationStateData(new BaseEvent()), current);

                Assert.That(
                    current.CallbackSeconds,
                    Is.EqualTo(GetExpectedCallbackSeconds(effect.Atsc)).Within(0.000001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LightPairRotationExactCallbackBoundaryUsesSharedPreviewClock()
        {
            var gameObject = new GameObject(nameof(LightPairRotationExactCallbackBoundaryUsesSharedPreviewClock));
            try
            {
                var effect = gameObject.AddComponent<LightPairRotationEffect>();
                effect.Atsc = GetAudioTimeSyncController();
                var current = new LightPairRotationStateData(new BaseEvent { Type = 100, Value = -1 });
                SetExactCallbackStartTime(effect.Atsc, current);

                InvokeComputeSnapshot(effect, new LightPairRotationStateData(new BaseEvent()), current);

                Assert.That(
                    current.CallbackSeconds,
                    Is.EqualTo(GetExpectedCallbackSeconds(effect.Atsc)).Within(0.000001f));
                Assert.That(current.RandomCallbackFrame, Is.EqualTo(GetExpectedCallbackIndex(effect.Atsc)));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LightPairSinMoveExactCallbackBoundaryUsesSharedPreviewClock()
        {
            var gameObject = new GameObject(nameof(LightPairSinMoveExactCallbackBoundaryUsesSharedPreviewClock));
            try
            {
                var effect = gameObject.AddComponent<LightPairSinMoveEffect>();
                effect.Atsc = GetAudioTimeSyncController();
                var current = new LightPairSinMoveStateData(new BaseEvent { Type = 100, Value = -1 });
                SetExactCallbackStartTime(effect.Atsc, current);

                InvokeComputeSnapshot(effect, new LightPairSinMoveStateData(new BaseEvent()), current);

                Assert.That(
                    current.CallbackSeconds,
                    Is.EqualTo(GetExpectedCallbackSeconds(effect.Atsc)).Within(0.000001f));
                Assert.That(current.RandomPhaseFrame, Is.EqualTo(GetExpectedCallbackIndex(effect.Atsc)));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static AudioTimeSyncController GetAudioTimeSyncController()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(atsc, Is.Not.Null, "The shared editor test scene has no AudioTimeSyncController.");
            return atsc;
        }

        private static void SetExactCallbackStartTime<TState>(AudioTimeSyncController atsc, TState state)
            where TState : BasicMovementStateData
        {
            state.StartTime = atsc.GetBeatFromSeconds(ExactCallbackIndex / PreviewCallbackRate);
        }

        private static float GetExpectedCallbackSeconds(AudioTimeSyncController atsc)
        {
            var beat = atsc.GetBeatFromSeconds(ExactCallbackIndex / PreviewCallbackRate);
            return TimeHelper.GetPreviewCallbackSeconds(atsc.GetSecondsFromBeat(beat));
        }

        private static int GetExpectedCallbackIndex(AudioTimeSyncController atsc)
        {
            var beat = atsc.GetBeatFromSeconds(ExactCallbackIndex / PreviewCallbackRate);
            return TimeHelper.GetPreviewRenderIndex(atsc.GetSecondsFromBeat(beat));
        }

        private static void InvokeComputeSnapshot<TState>(
            BasicMovementEffect<TState> effect,
            TState previous,
            TState current)
            where TState : BasicMovementStateData
        {
            var method = effect.GetType().GetMethod(
                "ComputeSnapshot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"{effect.GetType().Name} has no ComputeSnapshot implementation.");
            method.Invoke(effect, new object[] { previous, current });
        }
    }
}

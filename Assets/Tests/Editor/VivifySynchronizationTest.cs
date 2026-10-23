using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class VivifySynchronizationTest
    {
        [Test]
        public void SpawnTimeReachesBothActiveAndInactiveChildren()
        {
            var root = new GameObject("Vivify synchronization test");
            try
            {
                var active = new GameObject("active child");
                active.transform.SetParent(root.transform);
                var activeSync = active.AddComponent<ParticleSystemSyncController>();
                activeSync.enabled = false;
                var inactive = new GameObject("inactive child");
                inactive.transform.SetParent(root.transform);
                inactive.SetActive(false);
                var inactiveSync = inactive.AddComponent<ParticleSystemSyncController>();
                inactiveSync.enabled = false;
                var prefab = root.AddComponent<VivifyObject>();
                prefab.SongSynchronize(12);
                var start = typeof(SyncController).GetProperty("StartTime", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(start.GetValue(activeSync), Is.EqualTo(12f));
                Assert.That(start.GetValue(inactiveSync), Is.EqualTo(12f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator DelayedAnimatorAwakeDoesNotAdvanceByTheAbsoluteSpawnTime()
        {
            using var assets = new VivifyTestAssets();
            var clip = assets.Own(new AnimationClip());
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", AnimationCurve.Linear(0, 0, 10, 10));
            assets.Controller.layers[0].stateMachine.defaultState.motion = clip;
            var root = assets.Own(new GameObject("delayed Animator"));
            root.SetActive(false);
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = assets.Controller;
            var sync = root.AddComponent<AnimatorSyncController>();
            sync.enabled = false;
            sync.SetStartTime(12);
            root.SetActive(true);
            Assert.That(root.transform.localPosition.x, Is.EqualTo(0).Within(.001f));
            sync.Sync(8 / .02f);
            animator.Update(.02f);
            Assert.That(root.transform.localPosition.x, Is.EqualTo(8).Within(.001f),
                "The first sample must advance eight elapsed seconds, not twenty absolute seconds.");
            animator.enabled = false;
            yield return null;
        }

        [TestCase(5f, .5f)]
        [TestCase(10f, 1f)]
        [TestCase(15f, 1.5f)]
        public void VideoPlaybackRateUsesTheEditorSpeedUnits(float editorSpeed, float expectedRate)
        {
            var root = new GameObject("Vivify video rate test");
            root.SetActive(false);
            try
            {
                var clock = root.AddComponent<AudioTimeSyncController>();
                clock.SongSpeed = editorSpeed;
                var player = root.AddComponent<VideoPlayer>();
                var sync = root.AddComponent<VideoPlayerSyncController>();
                sync.Atsc = clock;
                // Exercise resynchronization without preparing a video or using a platform decoder.
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(VideoPlayerSyncController).GetField("videoPlayer", flags).SetValue(sync, player);
                typeof(VideoPlayerSyncController).GetMethod("ResyncTime", flags).Invoke(sync, null);
                Assert.That(player.playbackSpeed, Is.EqualTo(expectedRate).Within(.0001f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}

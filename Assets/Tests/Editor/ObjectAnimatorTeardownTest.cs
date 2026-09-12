using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Beatmap.Animations;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // ObjectAnimatorTeardownTest isolates parent-before-child scene destruction without requiring an installed map.
    public class ObjectAnimatorTeardownTest
    {
        // ObjectAnimatorDisableAfterTrackDestroyedDoesNotThrow reproduces mapper scene destruction ordering by retaining
        // Unity's destroyed TrackAnimator proxy until the child ObjectAnimator receives its later OnDisable callback.
        [UnityTest]
        public IEnumerator ObjectAnimatorDisableAfterTrackDestroyedDoesNotThrow()
        {
            var trackObject = new GameObject("Destroyed parent track");
            var trackAnimator = trackObject.AddComponent<TrackAnimator>();
            var childObject = new GameObject("Later-disabled child animator");
            childObject.SetActive(false);
            var objectAnimator = childObject.AddComponent<ObjectAnimator>();
            var tracksField = typeof(ObjectAnimator).GetField("tracks", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(tracksField, Is.Not.Null);
            var tracks = (List<TrackAnimator>)tracksField.GetValue(objectAnimator);
            tracks.Add(trackAnimator);

            Object.Destroy(trackObject);
            yield return null;

            childObject.SetActive(true);
            Assert.DoesNotThrow(() => objectAnimator.enabled = false);
            Object.Destroy(childObject);
        }
    }
}

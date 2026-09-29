using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Mapper interaction gates are scene-owned even when their backing fields are static, so this regression leaves the
    // mapper through the production transition path and verifies no invisible lock can reach the next song.
    public class MapperInteractionStateLifecycleTest
    {
        // The regression forces preview animation on, so retain the user's prior test setting for teardown isolation.
        private bool previousAnimations;
        private bool capturedAnimations;

        // LeavingMapperClearsInteractionStateBeforeNextSong reproduces the confirmed invisible Node Editor lock together
        // with every other mapper-scoped static gate that could disable placement after returning to a song.
        [UnityTest]
        public IEnumerator LeavingMapperClearsInteractionStateBeforeNextSong()
        {
            yield return TestUtils.LoadMap(3);

            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var timeline = Object.FindAnyObjectByType<SongTimelineController>();
            Assert.IsNotNull(uiMode);
            Assert.IsNotNull(timeline);

            previousAnimations = Settings.Instance.Animations;
            capturedAnimations = true;
            Settings.Instance.Animations = true;
            uiMode.SetUIMode(UIModeType.Preview, false);
            timeline.OnPointerEnter(null);
            DeleteToolController.UpdateDeletion(true);
            NodeEditorController.IsActive = true;
            PauseManager.IsPaused = true;
            KeybindsController.MousePosition = Vector2.one;
            SetPrivateStaticProperty(typeof(KeybindsController), nameof(KeybindsController.IsMouseInWindow), false);
            SetPrivateStaticProperty(typeof(KeybindsController), nameof(KeybindsController.IsControlKeyHeld), true);
            SetPrivateStaticProperty(typeof(KeybindsController), nameof(KeybindsController.IsHoverKeyHeld), true);
            SetPrivateStaticProperty(typeof(KeybindsController), nameof(KeybindsController.IsSelectKeyHeld), true);

            Assert.IsTrue(UIMode.PreviewMode, "The reproduction must begin in preview mode.");
            Assert.IsTrue(UIMode.AnimationMode, "The reproduction must begin with preview animation active.");
            Assert.IsTrue(SongTimelineController.IsHovering, "The reproduction must begin with the timeline hovered.");
            Assert.IsTrue(DeleteToolController.IsActive, "The reproduction must begin with the delete tool active.");
            Assert.IsTrue(NodeEditorController.IsActive, "The reproduction must begin with Node Editor active.");
            Assert.IsTrue(PauseManager.IsPaused, "The reproduction must begin with the mapper paused.");
            Assert.IsFalse(KeybindsController.IsMouseInWindow, "The reproduction must begin with the pointer outside the mapper window.");
            Assert.IsTrue(KeybindsController.IsControlKeyHeld, "The reproduction must begin with Control latched.");
            Assert.IsTrue(KeybindsController.IsHoverKeyHeld, "The reproduction must begin with Alt latched.");
            Assert.IsTrue(KeybindsController.IsSelectKeyHeld, "The reproduction must begin with Shift latched.");

            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name.StartsWith("02") && !SceneTransitionManager.IsLoading);

            try
            {
                Assert.IsFalse(
                    NodeEditorController.IsActive,
                    "Leaving a song retained the invisible Node Editor placement and timeline lock.");
                Assert.AreEqual(UIModeType.Normal, UIMode.SelectedMode, "Leaving a song retained its selected UI mode.");
                Assert.IsFalse(UIMode.PreviewMode, "Leaving a song retained its preview placement lock.");
                Assert.IsFalse(UIMode.AnimationMode, "Leaving a song retained preview animation mode.");
                Assert.IsFalse(SongTimelineController.IsHovering, "Leaving a song retained hover state from its destroyed timeline.");
                Assert.IsFalse(DeleteToolController.IsActive, "Leaving a song retained its delete tool placement lock.");
                Assert.IsFalse(PauseManager.IsPaused, "Leaving a song retained its paused state.");
                Assert.AreEqual(Vector2.zero, KeybindsController.MousePosition, "Leaving a song retained its pointer position.");
                Assert.IsTrue(KeybindsController.IsMouseInWindow, "Leaving a song retained its out-of-window placement lock.");
                Assert.IsFalse(KeybindsController.IsControlKeyHeld, "Leaving a song retained its Control latch.");
                Assert.IsFalse(KeybindsController.IsHoverKeyHeld, "Leaving a song retained its Alt latch.");
                Assert.IsFalse(KeybindsController.IsSelectKeyHeld, "Leaving a song retained its Shift latch.");
                Assert.IsFalse(SceneTransitionManager.IsLoading, "The completed song exit remained marked as loading.");
            }
            finally
            {
                // LeavingMapperClearsInteractionStateBeforeNextSong must not let its intentionally leaked private-set flags hang mapper restoration after an expected pre-fix failure.
                ResetSeededStateForCleanup();
            }
        }

        // The regression intentionally leaves the mapper scene, so restore a clean shared map even when an assertion fails.
        [UnityTearDown]
        public IEnumerator RestoreMapperAfterStateLifecycleTest()
        {
            ResetSeededStateForCleanup();
            // Preserve the pre-test animation preference only when setup reached the point that captured it.
            if (capturedAnimations)
            {
                Settings.Instance.Animations = previousAnimations;
                capturedAnimations = false;
            }

            if (!SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                yield return TestUtils.LoadMap(3);
            }

            var uiMode = Object.FindAnyObjectByType<UIMode>();
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            var timeline = Object.FindAnyObjectByType<SongTimelineController>();
            if (timeline != null)
            {
                timeline.OnPointerExit(null);
            }

            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // Reflection is restricted to failure cleanup because the production regression intentionally exposes no public setters for these stale flags.
        private static void ResetSeededStateForCleanup()
        {
            NodeEditorController.IsActive = false;
            DeleteToolController.UpdateDeletion(false);
            PauseManager.IsPaused = false;
            UIMode.SelectedMode = UIModeType.Normal;
            KeybindsController.MousePosition = Vector2.zero;
            SetPrivateStaticProperty(typeof(UIMode), nameof(UIMode.PreviewMode), false);
            SetPrivateStaticProperty(typeof(UIMode), nameof(UIMode.AnimationMode), false);
            SetPrivateStaticProperty(typeof(SongTimelineController), nameof(SongTimelineController.IsHovering), false);
            SetPrivateStaticProperty(typeof(KeybindsController), nameof(KeybindsController.IsMouseInWindow), true);
            SetPrivateStaticProperty(typeof(KeybindsController), nameof(KeybindsController.IsControlKeyHeld), false);
            SetPrivateStaticProperty(typeof(KeybindsController), nameof(KeybindsController.IsHoverKeyHeld), false);
            SetPrivateStaticProperty(typeof(KeybindsController), nameof(KeybindsController.IsSelectKeyHeld), false);
        }

        // Reproduction and failure cleanup need the same narrow access to public state whose setters are intentionally private.
        private static void SetPrivateStaticProperty<T>(System.Type owner, string propertyName, T value) =>
            owner.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static).SetValue(null, value);
    }
}

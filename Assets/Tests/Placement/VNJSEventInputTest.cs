using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Placement
{
    // VNJS hover-wheel input needs an isolated device runtime because host focus and modifier state are nondeterministic in batchmode.
    public class VNJSEventInputTest : TestBase
    {
        private InputTestFixture inputFixture;
        private CMInput isolatedInput;
        private Mouse virtualMouse;
        private Keyboard virtualKeyboard;
        private bool? sharedVNJSInputWasEnabled;
        // CtrlShiftScrollHoveredVNJSDoesNotChangeCursorInterval restores every shared map and Timeline value it isolates.
        private bool? sharedTimelineInputWasEnabled;
        private bool? sharedUtilsInputWasEnabled;
        private AudioTimeSyncController audioTimeSyncController;
        private int? gridMeasureSnappingBeforeTest;

        // CtrlShiftScrollHoverCyclesVNJSEasingInBothDirections proves the authored chord dispatches the production undoable mutation.
        [UnityTest]
        public IEnumerator CtrlShiftScrollHoverCyclesVNJSEasingInBothDirections()
        {
            var data = PlaceVNJSEvent();
            yield return null;

            var container = GetRenderedContainer(data);

            InitializeIsolatedInput();
            var easingAction = isolatedInput.NJSEventObjects.Get().FindAction("Tweak VNJS Easing", false);
            Assert.That(easingAction, Is.Not.Null, "The permanent Ctrl+Shift+Scroll VNJS easing action was not authored.");

            SeedHoveredVNJS(container);
            ScrollWithCtrlShift(1f);
            Assert.That(data.Easing, Is.EqualTo((int)EaseType.Linear), "Upward scrolling did not advance VNJS easing.");

            SeedHoveredVNJS(container);
            ScrollWithCtrlShift(-1f);
            Assert.That(data.Easing, Is.EqualTo((int)EaseType.None), "Downward scrolling did not reverse VNJS easing.");
        }

        // CtrlShiftScrollWithoutHoveredVNJSDoesNotChangeEasing guards equal input chords in other editor contexts.
        [UnityTest]
        public IEnumerator CtrlShiftScrollWithoutHoveredVNJSDoesNotChangeEasing()
        {
            var data = PlaceVNJSEvent();
            yield return null;

            InitializeIsolatedInput();
            BeatmapRaycastCache.FirstHit = null;
            BeatmapRaycastCache.HasHit = false;
            BeatmapRaycastCache.HasRaycastThisFrame = true;

            ScrollWithCtrlShift(1f);
            Assert.That(data.Easing, Is.EqualTo((int)EaseType.None), "Scrolling without a hovered VNJS event changed easing.");
        }

        // CtrlShiftScrollWithVNJSActionMapDisabledDoesNotChangeEasing proves editor context activation owns the shortcut.
        [UnityTest]
        public IEnumerator CtrlShiftScrollWithVNJSActionMapDisabledDoesNotChangeEasing()
        {
            var data = PlaceVNJSEvent();
            yield return null;

            var container = GetRenderedContainer(data);
            InitializeIsolatedInput();
            isolatedInput.NJSEventObjects.Disable();
            SeedHoveredVNJS(container);

            ScrollWithCtrlShift(1f);
            Assert.That(data.Easing, Is.EqualTo((int)EaseType.None), "A disabled VNJS action map still changed easing.");
        }

        // CtrlShiftScrollHoveredVNJSDoesNotChangeCursorInterval reproduces both equal-chord production callbacks.
        [UnityTest]
        public IEnumerator CtrlShiftScrollHoveredVNJSDoesNotChangeCursorInterval()
        {
            var data = PlaceVNJSEvent();
            yield return null;

            var container = GetRenderedContainer(data);
            InitializeIsolatedInput(includeTimeline: true);
            audioTimeSyncController = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(audioTimeSyncController, Is.Not.Null, "The production Timeline callback owner was not available.");
            gridMeasureSnappingBeforeTest = audioTimeSyncController.GridMeasureSnapping;
            audioTimeSyncController.GridMeasureSnapping = 8;
            SeedHoveredVNJS(container);

            ScrollWithCtrlShift(1f);
            Assert.That(data.Easing, Is.EqualTo((int)EaseType.Linear), "The hovered VNJS event did not consume its chord.");
            Assert.That(
                audioTimeSyncController.GridMeasureSnapping,
                Is.EqualTo(8),
                "Changing hovered VNJS easing also changed the global beat cursor interval.");
        }

        // CtrlShiftScrollWithoutHoveredVNJSStillChangesCursorInterval protects the global Timeline chord outside VNJS ownership.
        [UnityTest]
        public IEnumerator CtrlShiftScrollWithoutHoveredVNJSStillChangesCursorInterval()
        {
            InitializeIsolatedInput(includeTimeline: true);
            audioTimeSyncController = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(audioTimeSyncController, Is.Not.Null, "The production Timeline callback owner was not available.");
            gridMeasureSnappingBeforeTest = audioTimeSyncController.GridMeasureSnapping;
            audioTimeSyncController.GridMeasureSnapping = 8;
            BeatmapRaycastCache.FirstHit = null;
            BeatmapRaycastCache.HasHit = false;
            BeatmapRaycastCache.HasRaycastThisFrame = true;

            ScrollWithCtrlShift(1f);
            Assert.That(
                audioTimeSyncController.GridMeasureSnapping,
                Is.Not.EqualTo(8),
                "The global beat cursor interval stopped responding when no VNJS event owned the pointer.");
            yield return null;
        }

        // PlainScrollHoveredVNJSStillMovesTimeline proves hover ownership is limited to the authored Ctrl+Shift chord.
        [UnityTest]
        public IEnumerator PlainScrollHoveredVNJSStillMovesTimeline()
        {
            var data = PlaceVNJSEvent();
            yield return null;

            var container = GetRenderedContainer(data);
            InitializeIsolatedInput(includeTimeline: true);
            audioTimeSyncController = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(audioTimeSyncController, Is.Not.Null, "The production Timeline callback owner was not available.");
            var originalJsonTime = audioTimeSyncController.CurrentJsonTime;
            SeedHoveredVNJS(container);

            ScrollWithoutModifiers(1f);
            Assert.That(
                audioTimeSyncController.CurrentJsonTime,
                Is.GreaterThan(originalJsonTime),
                "Plain scrolling stopped moving the Timeline while hovering a VNJS event.");
        }

        // Dispose the isolated asset before restoring the application Input System, then re-enable its original action map.
        protected override void AfterCleanup()
        {
            BeatmapRaycastCache.Invalidate();
            if (audioTimeSyncController != null && gridMeasureSnappingBeforeTest.HasValue)
                audioTimeSyncController.GridMeasureSnapping = gridMeasureSnappingBeforeTest.Value;
            audioTimeSyncController = null;
            gridMeasureSnappingBeforeTest = null;
            if (isolatedInput != null)
            {
                isolatedInput.NJSEventObjects.Disable();
                isolatedInput.Timeline.Disable();
                isolatedInput.Utils.Disable();
                isolatedInput.Dispose();
                isolatedInput = null;
            }

            virtualMouse = null;
            virtualKeyboard = null;
            if (inputFixture != null)
            {
                inputFixture.TearDown();
                inputFixture = null;
            }

            var sharedInput = CMInputCallbackInstaller.InputInstance;
            if (sharedInput != null && sharedVNJSInputWasEnabled == true)
            {
                sharedInput.NJSEventObjects.Enable();
            }
            sharedVNJSInputWasEnabled = null;

            if (sharedInput != null && sharedTimelineInputWasEnabled == true)
            {
                sharedInput.Timeline.Enable();
            }
            sharedTimelineInputWasEnabled = null;

            if (sharedInput != null && sharedUtilsInputWasEnabled == true)
            {
                sharedInput.Utils.Enable();
            }
            sharedUtilsInputWasEnabled = null;
        }

        // Capture and disable every shared map before InputTestFixture resets the runtime so teardown restores its real state.
        private void InitializeIsolatedInput(bool includeTimeline = false)
        {
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.That(sharedInput, Is.Not.Null, "The application's shared input asset was not initialized.");
            sharedVNJSInputWasEnabled = sharedInput.NJSEventObjects.enabled;
            sharedInput.NJSEventObjects.Disable();
            if (includeTimeline)
            {
                sharedTimelineInputWasEnabled = sharedInput.Timeline.enabled;
                sharedUtilsInputWasEnabled = sharedInput.Utils.enabled;
                sharedInput.Timeline.Disable();
                sharedInput.Utils.Disable();
            }

            inputFixture = new InputTestFixture();
            inputFixture.Setup();
            virtualMouse = InputSystem.AddDevice<Mouse>();
            virtualKeyboard = InputSystem.AddDevice<Keyboard>();

            isolatedInput = new CMInput();
            isolatedInput.NJSEventObjects.SetCallbacks(Object.FindAnyObjectByType<BeatmapNJSEventInputController>());
            isolatedInput.NJSEventObjects.Enable();

            if (includeTimeline)
            {
                isolatedInput.Timeline.SetCallbacks(Object.FindAnyObjectByType<AudioTimeSyncController>());
                isolatedInput.Utils.SetCallbacks(Object.FindAnyObjectByType<KeybindsController>());
                isolatedInput.Timeline.Enable();
                isolatedInput.Utils.Enable();
                MovePointerInsideEditor();
            }
        }

        // Timeline input accepts wheel changes only after the production pointer tracker sees an in-window position.
        private void MovePointerInsideEditor()
        {
#if UNITY_EDITOR
            var gameViewSize = UnityEditor.Handles.GetMainGameViewSize();
#else
            var gameViewSize = new Vector2(Screen.width, Screen.height);
#endif
            Assert.That(gameViewSize.x, Is.GreaterThan(2f), "The editor Game view had no usable width.");
            Assert.That(gameViewSize.y, Is.GreaterThan(2f), "The editor Game view had no usable height.");
            // Prove the virtual pointer callback observes both edges instead of passing from a cached default-true state.
            SetVirtualMouseState(-Vector2.one, Vector2.zero);
            Assert.That(
                KeybindsController.IsMouseInWindow,
                Is.False,
                "The virtual Timeline pointer did not leave the editor window.");
            SetVirtualMouseState((gameViewSize * 0.5f) + Vector2.one, Vector2.zero);
            SetVirtualMouseState(gameViewSize * 0.5f, Vector2.zero);
            Assert.That(KeybindsController.IsMouseInWindow, Is.True, "The virtual Timeline pointer did not enter the editor window.");
        }

        // Queue position and scroll in one synthetic mouse report so production pointer routing sees each transition.
        private void SetVirtualMouseState(Vector2 position, Vector2 scroll)
        {
            inputFixture.Set(virtualMouse.position, position, queueEventOnly: true);
            inputFixture.Set(virtualMouse.scroll, scroll, queueEventOnly: true);
            InputSystem.Update();
        }

        // All hover-input cases use one rendered VNJS fixture so only input context varies between regressions.
        private static BaseNJSEvent PlaceVNJSEvent() => PlaceUtils.Place(new BaseNJSEvent
        {
            JsonTime = 2f,
            Easing = (int)EaseType.None,
            RelativeNJS = 5f
        });

        // Resolve the authoritative grid entry rather than discovering a Unity component outside its owning collection.
        private static NJSEventContainer GetRenderedContainer(BaseNJSEvent data)
        {
            var grid = BeatmapObjectContainerCollection.GetCollectionForType<NJSEventGridContainer>(ObjectType.NJSEvent);
            Assert.That(grid.LoadedContainers.ContainsKey(data), Is.True, "The hovered VNJS fixture was not rendered.");
            return (NJSEventContainer)grid.LoadedContainers[data];
        }

        // Re-seed the same-frame production raycast cache so each wheel edge owns an explicit hovered VNJS node.
        private static void SeedHoveredVNJS(NJSEventContainer container)
        {
            BeatmapRaycastCache.FirstHit = container.gameObject;
            BeatmapRaycastCache.HasHit = true;
            BeatmapRaycastCache.HasRaycastThisFrame = true;
        }

        // Process packed keyboard deltas separately so neither modifier can overwrite the other's queued release state.
        private void ScrollWithCtrlShift(float direction)
        {
            inputFixture.Press(virtualKeyboard.leftCtrlKey, queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Press(virtualKeyboard.leftShiftKey, queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Set(virtualMouse.scroll, new Vector2(0f, direction), queueEventOnly: true);
            InputSystem.Update();

            inputFixture.Set(virtualMouse.scroll, Vector2.zero, queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Release(virtualKeyboard.leftShiftKey, queueEventOnly: true);
            InputSystem.Update();
            Assert.That(virtualKeyboard.leftShiftKey.isPressed, Is.False, "Virtual Shift remained pressed after release.");
            inputFixture.Release(virtualKeyboard.leftCtrlKey, queueEventOnly: true);
            InputSystem.Update();
            Assert.That(virtualKeyboard.leftCtrlKey.isPressed, Is.False, "Virtual Ctrl remained pressed after release.");
        }

        // Plain-scroll overlap coverage emits the same virtual wheel edge without either easing modifier.
        private void ScrollWithoutModifiers(float direction)
        {
            inputFixture.Set(virtualMouse.scroll, new Vector2(0f, direction), queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Set(virtualMouse.scroll, Vector2.zero, queueEventOnly: true);
            InputSystem.Update();
        }
    }
}

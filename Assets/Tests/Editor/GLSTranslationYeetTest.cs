using System.Linq;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tests.Editor
{
    public class GLSTranslationYeetTest : TestBase
    {
        private const float YeetOffset = 10_000f;
        private const float YeetCutoff = -5_000f;

        private InputTestFixture inputFixture;
        private CMInput isolatedInput;
        private Keyboard virtualKeyboard;
        private Mouse virtualMouse;
        private bool? sharedTranslationMapWasEnabled;
        private ScrollPrecisionController precision;
        private ScrollPrecision originalPrecision;

        // The authored action must be an exact Shift+Z composite so both translation hover controllers receive YEET.
        [Test]
        public void YeetBindingUsesShiftZ()
        {
            InitializeIsolatedInput(null);
            var action = isolatedInput.GLSTranslationObjects.Get().FindAction("YEET Translation (Hover)");

            Assert.NotNull(action);
            Assert.True(action.bindings.Any(binding => binding.isComposite && binding.path == "OneModifier"));
            Assert.True(action.bindings.Any(binding => binding.isPartOfComposite && binding.path == "<Keyboard>/shift"));
            Assert.True(action.bindings.Any(binding => binding.isPartOfComposite && binding.path == "<Keyboard>/z"));
        }

        // Inner EventBox hover must subtract the full YEET offset through the production Shift+Z callback.
        [Test]
        public void InnerTranslationNodeShiftZYeetsValue()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceTranslationGroup(130, 0f);
            var targetEvent = group.Boxes[0].Events[0];
            var containerObject = new GameObject("Inner translation YEET test container");
            var controllerObject = new GameObject("Inner translation YEET test controller");
            try
            {
                var container = containerObject.AddComponent<GLSEventContainer>();
                container.VisualSettings = GetInitializedVisualSettings();
                container.EventData = targetEvent;
                SetHighlightedWithoutVisualRefresh(container);
                var controller = controllerObject.AddComponent<TestGLSEventTranslationInputController>();
                controller.IsHovering = true;
                controller.HoveredObject = container;
                controller.RaycastTarget = container;
                InitializeIsolatedInput(controller);

                SendShiftZ();

                Assert.That(GetOpenTranslationEvent().Translation, Is.EqualTo(-YeetOffset));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // Outer GLS primary/ghost previews use the same action map and must add the offset back for an already-YEET value.
        [Test]
        public void OuterTranslationNodeShiftZUnyeetsValue()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceTranslationGroup(131, -YeetOffset);
            var targetEvent = group.Boxes[0].Events[0];
            var containerObject = new GameObject("Outer translation YEET test container");
            var controllerObject = new GameObject("Outer translation YEET test controller");
            try
            {
                var container = containerObject.AddComponent<GLSGroupContainer>();
                container.VisualSettings = GetInitializedVisualSettings();
                container.EventBoxGroupData = group;
                container.PreviewEventData = targetEvent;
                var controller = controllerObject.AddComponent<TestGLSGroupTranslationInputController>();
                controller.IsHovering = true;
                controller.HoveredObject = container;
                controller.RaycastTarget = container;
                InitializeIsolatedInput(controller);

                SendShiftZ();

                Assert.That(GetOpenTranslationEvent().Translation, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // The cutoff itself is YEET: toggling it adds the offset instead of subtracting a second time.
        [Test]
        public void ShiftZAtYeetCutoffUnyeetsInsteadOfDoubleYeeting()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceTranslationGroup(132, YeetCutoff);
            var targetEvent = group.Boxes[0].Events[0];
            var containerObject = new GameObject("YEET cutoff test container");
            var controllerObject = new GameObject("YEET cutoff test controller");
            try
            {
                var container = containerObject.AddComponent<GLSEventContainer>();
                container.VisualSettings = GetInitializedVisualSettings();
                container.EventData = targetEvent;
                SetHighlightedWithoutVisualRefresh(container);
                var controller = controllerObject.AddComponent<TestGLSEventTranslationInputController>();
                controller.IsHovering = true;
                controller.HoveredObject = container;
                controller.RaycastTarget = container;
                InitializeIsolatedInput(controller);

                SendShiftZ();

                Assert.That(GetOpenTranslationEvent().Translation, Is.EqualTo(YeetCutoff + YeetOffset));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // Float precision around the sentinel must not survive either toggle as visible position noise beyond hundredths.
        [Test]
        public void ShiftZRoundsYeetAndUnyeetToTwoDecimalPlaces()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceTranslationGroup(134, 1.234f);
            var targetEvent = group.Boxes[0].Events[0];
            var containerObject = new GameObject("YEET rounding test container");
            var controllerObject = new GameObject("YEET rounding test controller");
            try
            {
                var container = containerObject.AddComponent<GLSEventContainer>();
                container.VisualSettings = GetInitializedVisualSettings();
                container.EventData = targetEvent;
                SetHighlightedWithoutVisualRefresh(container);
                var controller = controllerObject.AddComponent<TestGLSEventTranslationInputController>();
                controller.IsHovering = true;
                controller.HoveredObject = container;
                controller.RaycastTarget = container;
                InitializeIsolatedInput(controller);

                SendShiftZ();

                var yeetedEvent = GetOpenTranslationEvent();
                Assert.That(yeetedEvent.Translation, Is.EqualTo(-9_998.77f));
                container.EventData = yeetedEvent;
                SendShiftZ();

                Assert.That(GetOpenTranslationEvent().Translation, Is.EqualTo(1.23f));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // Values at or below cutoff render YEET, while an ordinary value immediately above it keeps numeric rendering.
        [Test]
        public void TranslationNodeTextUsesYeetAtAndBelowCutoff()
        {
            StringAssert.StartsWith("YEET", GLSEventCommon.GetTranslationInfo(
                new BaseLightTranslationBase { Translation = YeetCutoff }));
            StringAssert.StartsWith("YEET", GLSEventCommon.GetTranslationInfo(
                new BaseLightTranslationBase { Translation = YeetCutoff - 1f }));
            Assert.False(GLSEventCommon.GetTranslationInfo(
                new BaseLightTranslationBase { Translation = YeetCutoff + 8f }).StartsWith("YEET"));
        }

        // The first Alt-scroll only restores the pre-YEET value; the following pulse applies ordinary precision and direction.
        [TestCase(1f)]
        [TestCase(-1f)]
        public void AltScrollRestoresYeetBeforeSubsequentNormalScrolling(float direction)
        {
            SetEditingMode(EditingMode.EventBox);
            // A normal preset value proves the sentinel representation retains sub-eight-unit precision through restoration.
            const float originalValue = 1f;
            var group = PlaceTranslationGroup(133, originalValue - YeetOffset);
            var targetEvent = group.Boxes[0].Events[0];
            var containerObject = new GameObject("YEET Alt-scroll test container");
            var controllerObject = new GameObject("YEET Alt-scroll test controller");
            try
            {
                var container = containerObject.AddComponent<GLSEventContainer>();
                container.VisualSettings = GetInitializedVisualSettings();
                container.EventData = targetEvent;
                SetHighlightedWithoutVisualRefresh(container);
                var controller = controllerObject.AddComponent<TestGLSEventTranslationInputController>();
                controller.IsHovering = true;
                controller.HoveredObject = container;
                controller.RaycastTarget = container;
                InitializeIsolatedInput(controller);
                var normalStep = precision.GetCurrentTranslationPrecision() / 100f;

                SendAltScroll(direction);

                var restoredEvent = GetOpenTranslationEvent();
                Assert.That(restoredEvent.Translation, Is.EqualTo(originalValue));
                container.EventData = restoredEvent;
                SendAltScroll(direction);

                Assert.That(GetOpenTranslationEvent().Translation, Is.EqualTo(originalValue + (direction * normalStep)));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // Capture and disable the shared map before InputTestFixture owns an isolated runtime and dedicated virtual devices.
        private void InitializeIsolatedInput(CMInput.IGLSTranslationObjectsActions controller)
        {
            Assert.That(inputFixture, Is.Null, "Virtual input was initialized twice in one test.");
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.That(sharedInput, Is.Not.Null);
            sharedTranslationMapWasEnabled = sharedInput.GLSTranslationObjects.enabled;
            sharedInput.GLSTranslationObjects.Disable();

            inputFixture = new InputTestFixture();
            inputFixture.Setup();
            virtualKeyboard = InputSystem.AddDevice<Keyboard>();
            virtualMouse = InputSystem.AddDevice<Mouse>();
            isolatedInput = new CMInput();
            if (controller != null)
            {
                isolatedInput.GLSTranslationObjects.SetCallbacks(controller);
            }
            isolatedInput.GLSTranslationObjects.Enable();
            Assert.NotNull(isolatedInput.GLSTranslationObjects.Get().FindAction("YEET Translation (Hover)"));

            precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            originalPrecision = precision.CurrentPrecision;
            precision.CurrentPrecision = ScrollPrecision.Medium;
            if (controller is TestGLSEventTranslationInputController innerController)
            {
                innerController.SetPrecision(precision);
            }
        }

        // Press both composite parts through the isolated keyboard, then release them for deterministic repeatability.
        private void SendShiftZ()
        {
            inputFixture.Press(virtualKeyboard.leftShiftKey, queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Press(virtualKeyboard.zKey, queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Release(virtualKeyboard.zKey, queueEventOnly: true);
            inputFixture.Release(virtualKeyboard.leftShiftKey, queueEventOnly: true);
            InputSystem.Update();
        }

        // Queue a fresh Alt+scroll pulse and reset the scroll axis so a second call performs a second action.
        private void SendAltScroll(float delta)
        {
            inputFixture.Press(virtualKeyboard.altKey, queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Set(virtualMouse.scroll, new Vector2(0f, delta), queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Set(virtualMouse.scroll, Vector2.zero, queueEventOnly: true);
            inputFixture.Release(virtualKeyboard.altKey, queueEventOnly: true);
            InputSystem.Update();
        }

        // Create one manager-owned translation node so every callback publishes through real parent replacement and undo paths.
        private static BaseLightTranslationEventBoxGroup PlaceTranslationGroup(float jsonTime, float value)
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = jsonTime,
                ID = (int)jsonTime,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Events = new[] { new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = value } }
                    }
                }
            };
            group.NormalizeLoadedEventConflicts();
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
            return group;
        }

        // Read the replacement event after each clone-producing mutation rather than inspecting stale source identity.
        private static BaseLightTranslationBase GetOpenTranslationEvent() =>
            (Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext as BaseLightTranslationEventBoxGroup)
            .Boxes[0].Events[0];

        // Set the production workspace represented by the controller under test.
        private static void SetEditingMode(EditingMode editingMode) =>
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = editingMode;

        // Reuse a scene-owned visual settings dependency for deliberately data-only hover containers.
        private static VisualSettingsSO GetInitializedVisualSettings() => Object
            .FindObjectsByType<ObjectContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(container => container.VisualSettings)
            .First(settings => settings != null);

        // Avoid renderer dependencies while retaining production hover ownership and callback behavior.
        private static void SetHighlightedWithoutVisualRefresh(ObjectContainer container)
        {
            var field = typeof(ObjectContainer).GetField("highlighted", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(container, true);
        }

        // Dispose isolated assets before restoring the application's Input System and original shared map state.
        protected override void AfterCleanup()
        {
            BeatmapRaycastCache.Invalidate();
            if (isolatedInput != null)
            {
                isolatedInput.GLSTranslationObjects.Disable();
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

            if (precision != null)
            {
                precision.CurrentPrecision = originalPrecision;
                precision = null;
            }

            var sharedInput = CMInputCallbackInstaller.InputInstance;
            if (sharedInput != null && sharedTranslationMapWasEnabled == true)
            {
                sharedInput.GLSTranslationObjects.Enable();
            }
            sharedTranslationMapWasEnabled = null;
        }

        private class TestGLSEventTranslationInputController : BeatmapGLSEventTranslationInputController
        {
            public GLSEventContainer RaycastTarget;

            // Inject the scene-owned precision dependency before exercising the inherited production callback.
            public void SetPrecision(ScrollPrecisionController value) => ScrollPrecisionController = value;

            // Return the exact inner node while retaining production ownership validation and group replacement.
            protected override bool TryRaycastHoveredEvent(out GLSEventContainer currentContainer)
            {
                currentContainer = RaycastTarget;
                return currentContainer != null;
            }
        }

        private class TestGLSGroupTranslationInputController : BeatmapGLSGroupTranslationInputController
        {
            public GLSGroupContainer RaycastTarget;

            // Return the primary or ghost outer preview selected by each regression case.
            protected override bool TryRaycastHoveredPreview(out GLSGroupContainer container)
            {
                container = RaycastTarget;
                return container != null;
            }
        }
    }
}

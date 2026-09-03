using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
// The regression fixture creates authoritative GLS groups through the same factory used by loaded maps.
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.Editor
{
    public class GLSColorStrobeFadeInputTest : TestBase
    {
        // InnerGlsColorNodeShiftScrollTogglesStrobeFade protects the EventBox controller's physical hover resolution and parent replacement.
        [Test]
        public void InnerGlsColorNodeShiftScrollTogglesStrobeFade()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(primaryStrobeFade: 0, ghostStrobeFade: 0);
            var targetEvent = group.Boxes[0].Events[0];
            var containerObject = new GameObject("Inner GLS color Strobe Fade test container");
            var controllerObject = new GameObject("Inner GLS color Strobe Fade test controller");
            try
            {
                var container = containerObject.AddComponent<GLSEventContainer>();
                // Data-only test containers still need the lifecycle dependency that OnDestroy unregisters from.
                container.VisualSettings = GetInitializedVisualSettings();
                container.EventData = targetEvent;
                SetHighlightedWithoutVisualRefresh(container);
                var controller = controllerObject.AddComponent<TestGLSEventColorInputController>();
                controller.IsHovering = true;
                controller.HoveredObject = container;
                controller.RaycastTarget = container;

                SendShiftScroll(controller);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.AreEqual(1, replacement.Boxes[0].Events[0].StrobeFade);
                Assert.AreEqual(0, replacement.Boxes[0].Events[1].StrobeFade);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterPrimaryGlsColorNodeShiftScrollTogglesStrobeFade protects the collection-owned outer preview node path.
        [Test]
        public void OuterPrimaryGlsColorNodeShiftScrollTogglesStrobeFade()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(primaryStrobeFade: 1, ghostStrobeFade: 0);
            var primaryEvent = group.Boxes[0].Events[0];
            var containerObject = new GameObject("Outer primary GLS color Strobe Fade test container");
            var controllerObject = new GameObject("Outer primary GLS color Strobe Fade test controller");
            try
            {
                var container = containerObject.AddComponent<GLSGroupContainer>();
                // Data-only test containers still need the lifecycle dependency that OnDestroy unregisters from.
                container.VisualSettings = GetInitializedVisualSettings();
                container.EventBoxGroupData = group;
                container.PreviewEventData = primaryEvent;
                var controller = controllerObject.AddComponent<TestGLSGroupColorInputController>();
                controller.IsHovering = true;
                controller.HoveredObject = container;
                controller.RaycastTarget = container;

                SendShiftScroll(controller);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.AreEqual(0, replacement.Boxes[0].Events[0].StrobeFade);
                Assert.AreEqual(0, replacement.Boxes[0].Events[1].StrobeFade);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterGhostPreviewGlsColorNodeShiftScrollTogglesStrobeFade protects targeting the later event represented by a translucent ghost.
        [Test]
        public void OuterGhostPreviewGlsColorNodeShiftScrollTogglesStrobeFade()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(primaryStrobeFade: 0, ghostStrobeFade: 0);
            var ghostEvent = group.Boxes[0].Events[1];
            var containerObject = new GameObject("Outer ghost GLS color Strobe Fade test container");
            var controllerObject = new GameObject("Outer ghost GLS color Strobe Fade test controller");
            try
            {
                var container = containerObject.AddComponent<GLSGroupContainer>();
                // Data-only test containers still need the lifecycle dependency that OnDestroy unregisters from.
                container.VisualSettings = GetInitializedVisualSettings();
                container.EventBoxGroupData = group;
                container.PreviewEventData = ghostEvent;
                SetPrivateField(container, "isPreviewGhost", true);
                var controller = controllerObject.AddComponent<TestGLSGroupColorInputController>();
                controller.IsHovering = true;
                controller.HoveredObject = container;
                controller.RaycastTarget = container;

                SendShiftScroll(controller);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.AreEqual(0, replacement.Boxes[0].Events[0].StrobeFade);
                Assert.AreEqual(1, replacement.Boxes[0].Events[1].StrobeFade);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // Exercise the authored composite instead of manufacturing CallbackContext so Shift+scroll binding regressions fail with the controller tests.
        private static void SendShiftScroll(CMInput.IGLSColorObjectsActions controller)
        {
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.NotNull(sharedInput);
            var sharedMapWasEnabled = sharedInput.GLSColorObjects.enabled;
            sharedInput.GLSColorObjects.Disable();
            var input = new CMInput();
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var addedKeyboard = keyboard == null;
            var addedMouse = mouse == null;
            if (addedKeyboard)
            {
                keyboard = InputSystem.AddDevice<Keyboard>();
            }
            if (addedMouse)
            {
                mouse = InputSystem.AddDevice<Mouse>();
            }

            try
            {
                input.GLSColorObjects.SetCallbacks(controller);
                input.GLSColorObjects.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift));
                InputSystem.Update();
                InputSystem.QueueStateEvent(
                    mouse,
                    new MouseState { scroll = new Vector2(0f, 1f) });
                InputSystem.Update();
            }
            finally
            {
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                input.GLSColorObjects.Disable();
                input.Dispose();
                if (addedMouse)
                {
                    InputSystem.RemoveDevice(mouse);
                }
                if (addedKeyboard)
                {
                    InputSystem.RemoveDevice(keyboard);
                }
                if (sharedMapWasEnabled)
                {
                    sharedInput.GLSColorObjects.Enable();
                }
            }
        }

        // Place one authoritative group with two distinct offsets so the outer tests can distinguish primary and ghost targets.
        private static BaseLightColorEventBoxGroup PlaceColorGroup(int primaryStrobeFade, int ghostStrobeFade)
        {
            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                $@"{{ ""b"": 20, ""g"": 1, ""e"": [
                    {{ ""f"": {{ ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }}, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ {{ ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 1, ""sb"": 1, ""sf"": {primaryStrobeFade} }},
                                 {{ ""b"": 0.75, ""c"": 1, ""s"": 1, ""i"": 0, ""f"": 1, ""sb"": 1, ""sf"": {ghostStrobeFade} }} ] }}
                ] }}"));
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;
            return group;
        }

        // Read the replacement parent published by the GLS action so assertions never inspect the stale pre-scroll event instance.
        private static BaseLightColorEventBoxGroup GetOpenColorGroup() =>
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext as BaseLightColorEventBoxGroup;

        // Set the production workspace for each regression so its controller represents the same view exercised by users.
        private static void SetEditingMode(EditingMode editingMode) =>
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = editingMode;

        // Reuse a scene-owned asset so test teardown follows ObjectContainer's normal initialized lifecycle.
        private static VisualSettingsSO GetInitializedVisualSettings()
        {
            var containers = Object.FindObjectsByType<ObjectContainer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var container in containers)
            {
                if (container.VisualSettings != null)
                {
                    return container.VisualSettings;
                }
            }

            Assert.Fail("The loaded editor scene had no initialized ObjectContainer VisualSettings dependency.");
            return null;
        }

        // Avoid invoking renderer dependencies on the deliberately data-only inner test container during post-mutation hover refresh.
        private static void SetHighlightedWithoutVisualRefresh(ObjectContainer container) =>
            SetPrivateField(container, "highlighted", true);

        // Test containers set only the private state that distinguishes production hover paths; all mutation data remains authoritative.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? typeof(ObjectContainer).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Could not find test field {fieldName}.");
            field.SetValue(target, value);
        }

        private class TestGLSEventColorInputController : BeatmapGLSEventColorInputController
        {
            public GLSEventContainer RaycastTarget;

            // Return the exact inner node under test while retaining the production callback, ownership checks, command, and refresh.
            protected override bool TryRaycastHoveredEvent(out GLSEventContainer firstObject)
            {
                firstObject = RaycastTarget;
                return firstObject != null;
            }
        }

        private class TestGLSGroupColorInputController : BeatmapGLSGroupColorInputController
        {
            public GLSGroupContainer RaycastTarget;

            // Return either the primary or ghost outer preview selected by the individual regression test.
            protected override bool TryRaycastHoveredPreview(out GLSGroupContainer firstObject)
            {
                firstObject = RaycastTarget;
                return firstObject != null;
            }
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tests.Editor
{
    public class PointerPositionConsumersTest : InputTestBase
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void TooltipFollowsInjectedPointer()
        {
            var ui = PersistentUI.Instance;
            using var serializedUi = new SerializedObject(ui);
            var panel = (RectTransform)serializedUi.FindProperty("tooltipPanelRect").objectReferenceValue;
            var updateTooltip = typeof(PersistentUI).GetMethod("LateUpdate", PrivateInstance);
            var center = Handles.GetMainGameViewSize() * 0.5f;
            var step = new Vector2(center.x * 0.1f, 0);
            var savedPosition = panel.position;
            var previousInput = CMInputCallbackInstaller.InputInstance;

            InitializeVirtualInput(includeKeyboard: false);
            var input = CreatePointerInput();
            var unboundMouse = InputSystem.AddDevice<Mouse>();
            try
            {
                CMInputCallbackInstaller.InputInstance = input;
                ui.SetTooltip("Pointer test");
                ui.ShowTooltip();
                InjectPointer(center - step, unboundMouse);
                updateTooltip.Invoke(ui, null);
                var firstX = panel.position.x;

                InjectPointer(center + step, unboundMouse);
                updateTooltip.Invoke(ui, null);
                Assert.That(panel.position.x - firstX, Is.EqualTo(step.x * 2).Within(1f));
            }
            finally
            {
                ui.HideTooltip();
                panel.position = savedPosition;
                CMInputCallbackInstaller.InputInstance = previousInput;
                input.Disable();
                input.Dispose();
                TearDownVirtualInput();
            }
        }

        [Test]
        public void EventPlacementPreviewReturnsWhenInjectedPointerMoves()
        {
            var controller = BeatmapEventInputController.ActiveInstance;
            var keybinds = Object.FindAnyObjectByType<KeybindsController>();
            var selection = Object.FindAnyObjectByType<SelectionController>();
            using var serializedSelection = new SerializedObject(selection);
            var placement = (EventPlacement)serializedSelection.FindProperty("eventPlacement").objectReferenceValue;
            var preview = placement.PlacementVisualContainer.gameObject;
            var controllerType = typeof(BeatmapEventInputController);
            var startScrolling = controllerType.GetMethod("HidePreviewWhileScrolling", PrivateInstance);
            var updatePreview = controllerType.GetMethod("UpdatePreviewVisualOnMouseMove", PrivateInstance);
            var selectionField = controllerType.GetField("selectionController", PrivateInstance);
            var isScrolling = controllerType.GetField("isScrolling", PrivateInstance);
            var lastPointer = controllerType.GetField("lastMousePosition", PrivateInstance);
            var savedSelection = selectionField.GetValue(controller);
            var wasScrolling = isScrolling.GetValue(controller);
            var savedPointer = lastPointer.GetValue(controller);
            var savedMapperPointer = KeybindsController.MousePosition;
            var wasVisible = preview.activeSelf;

            InitializeVirtualInput(includeKeyboard: false);
            var input = CreatePointerInput();
            var unboundMouse = InputSystem.AddDevice<Mouse>();
            var pointerAction = input.Utils.MouseMovement;
            pointerAction.performed += keybinds.OnMouseMovement;
            try
            {
                selectionField.SetValue(controller, selection);
                isScrolling.SetValue(controller, false);
                InjectPointer(new Vector2(250, 250), unboundMouse);
                Assert.That(KeybindsController.MousePosition, Is.EqualTo(new Vector2(250, 250)));
                startScrolling.Invoke(controller, null);
                updatePreview.Invoke(controller, null);
                Assert.That(preview.activeSelf, Is.False, "Scrolling should hide the stationary event preview.");

                InjectPointer(new Vector2(275, 250), unboundMouse);
                Assert.That(KeybindsController.MousePosition, Is.EqualTo(new Vector2(275, 250)));
                updatePreview.Invoke(controller, null);
                Assert.That(preview.activeSelf, Is.True, "Moving the pointer should restore the event preview.");
            }
            finally
            {
                selectionField.SetValue(controller, savedSelection);
                isScrolling.SetValue(controller, wasScrolling);
                lastPointer.SetValue(controller, savedPointer);
                KeybindsController.MousePosition = savedMapperPointer;
                preview.SetActive(wasVisible);
                pointerAction.performed -= keybinds.OnMouseMovement;
                input.Disable();
                input.Dispose();
                TearDownVirtualInput();
            }
        }

        private CMInput CreatePointerInput()
        {
            var input = new CMInput { devices = new InputDevice[] { VirtualMouse } };
            input.Utils.MouseMovement.Enable();
            return input;
        }

        private void InjectPointer(Vector2 position, Mouse unboundMouse)
        {
            WindowsHighPollingRateWorkaround.ApplyMouseState(
                VirtualMouse,
                position,
                Vector2.zero,
                Vector2.zero,
                0,
                swapButtons: false);
            unboundMouse.MakeCurrent();
        }
    }
}

using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.Editor
{
    public class WindowsMouseButtonTest : InputTestBase
    {
        [TestCase(false, MouseButton.Left, true)]
        [TestCase(false, MouseButton.Right, false)]
        [TestCase(true, MouseButton.Left, false)]
        [TestCase(true, MouseButton.Right, true)]
        public void InjectedPhysicalButtonUsesWindowsPrimarySetting(
            bool swapButtons,
            MouseButton physicalButton,
            bool isPrimary)
        {
            InitializeVirtualInput(includeKeyboard: false);
            var mouse = VirtualMouse;
            using var uiInput = new DefaultInputActions();
            using var editorInput = new CMInput();
            uiInput.UI.Click.Enable();
            uiInput.UI.RightClick.Enable();
            editorInput.PlacementControllers.PlaceObject.Enable();
            editorInput.Camera.HoldtoMoveCamera.Enable();

            var position = new Vector2(120f, 80f);
            var delta = new Vector2(3f, -4f);
            var scroll = new Vector2(0f, 1f);
            var state = new MouseState()
                .WithButton(physicalButton)
                .WithButton(MouseButton.Middle)
                .WithButton(MouseButton.Back);
            WindowsHighPollingRateWorkaround.ApplyMouseState(mouse, position, delta, scroll, state.buttons, swapButtons);

            Assert.That(mouse.leftButton.isPressed, Is.EqualTo(isPrimary));
            Assert.That(mouse.rightButton.isPressed, Is.EqualTo(!isPrimary));
            Assert.That(uiInput.UI.Click.ReadValue<float>(), Is.EqualTo(isPrimary ? 1f : 0f));
            Assert.That(uiInput.UI.RightClick.ReadValue<float>(), Is.EqualTo(isPrimary ? 0f : 1f));
            Assert.That(editorInput.PlacementControllers.PlaceObject.IsPressed(), Is.EqualTo(isPrimary));
            Assert.That(editorInput.Camera.HoldtoMoveCamera.IsPressed(), Is.EqualTo(!isPrimary));
            Assert.That(mouse.middleButton.isPressed, Is.True);
            Assert.That(mouse.backButton.isPressed, Is.True);
            Assert.That(mouse.forwardButton.isPressed, Is.False);
            Assert.That(mouse.position.ReadValue(), Is.EqualTo(position));
            Assert.That(mouse.delta.ReadValue(), Is.EqualTo(delta));
            Assert.That(mouse.scroll.ReadValue(), Is.EqualTo(scroll));

            WindowsHighPollingRateWorkaround.ApplyMouseState(mouse, position, Vector2.zero, Vector2.zero, 0, swapButtons);

            Assert.That(mouse.leftButton.isPressed, Is.False);
            Assert.That(mouse.rightButton.isPressed, Is.False);
            Assert.That(editorInput.PlacementControllers.PlaceObject.IsPressed(), Is.False);
            Assert.That(editorInput.Camera.HoldtoMoveCamera.IsPressed(), Is.False);
        }

        [TearDown]
        public void RestoreInputRuntime() => TearDownVirtualInput();
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Tests.Infrastructure
{
    public abstract class InputTestBase : TestBase
    {
        // Virtual devices isolate input tests from editor focus and Jenkins host state.
        private InputTestFixture inputTestFixture;
        private Mouse virtualMouse;
        private Keyboard virtualKeyboard;
        private readonly List<InputActionMap> sharedEnabledActionMaps = new();

        protected Mouse VirtualMouse => virtualMouse;

        protected Keyboard VirtualKeyboard => virtualKeyboard;

        protected void InitializeVirtualInput(bool includeKeyboard)
        {
            Assert.That(inputTestFixture, Is.Null, "Virtual input was initialized twice in one test.");
            // InputTestFixture replaces the global runtime, so disable every shared map before its controls
            // bind to temporary devices; restore exactly these maps after the original runtime returns.
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.That(sharedInput, Is.Not.Null, "The application's shared input asset was not initialized.");
            sharedEnabledActionMaps.Clear();
            foreach (var actionMap in sharedInput.asset.actionMaps)
            {
                if (!actionMap.enabled)
                    continue;

                sharedEnabledActionMaps.Add(actionMap);
                actionMap.Disable();
            }

            inputTestFixture = new InputTestFixture();
            inputTestFixture.Setup();
            virtualMouse = InputSystem.AddDevice<Mouse>();
            if (includeKeyboard)
                virtualKeyboard = InputSystem.AddDevice<Keyboard>();
        }

        protected void SetVirtualMouseState(Vector2 position, Vector2 scroll)
        {
            Assert.That(inputTestFixture, Is.Not.Null, "Virtual input was not initialized before setting mouse state.");
            inputTestFixture.Set(virtualMouse.position, position, queueEventOnly: true);
            inputTestFixture.Set(virtualMouse.scroll, scroll, queueEventOnly: true);
            InputSystem.Update();
        }

        protected void SetVirtualControl<TValue>(
            InputControl<TValue> control,
            TValue value,
            bool queueEventOnly = false)
            where TValue : struct
        {
            inputTestFixture.Set(control, value, queueEventOnly: queueEventOnly);
        }

        protected void PressVirtualButton(ButtonControl button, bool queueEventOnly = false)
        {
            inputTestFixture.Press(button, queueEventOnly: queueEventOnly);
        }

        protected void ReleaseVirtualButton(ButtonControl button, bool queueEventOnly = false)
        {
            inputTestFixture.Release(button, queueEventOnly: queueEventOnly);
        }

        // Apply each key event before constructing the next chord state.
        protected void PressVirtualKeys(params Key[] keys)
        {
            Assert.That(virtualKeyboard, Is.Not.Null, "Virtual keyboard input was not initialized.");
            foreach (var key in keys)
            {
                inputTestFixture.Press(virtualKeyboard[key], queueEventOnly: true);
                InputSystem.Update();
                Assert.That(virtualKeyboard[key].isPressed, Is.True, $"Virtual modifier {key} was not pressed.");
            }
        }

        protected void ReleaseVirtualKeys(params Key[] keys)
        {
            Assert.That(virtualKeyboard, Is.Not.Null, "Virtual keyboard input was not initialized.");
            foreach (var key in keys)
            {
                inputTestFixture.Release(virtualKeyboard[key], queueEventOnly: true);
                InputSystem.Update();
                Assert.That(virtualKeyboard[key].isPressed, Is.False, $"Virtual modifier {key} was not released.");
            }
        }

        protected void TearDownVirtualInput()
        {
            virtualMouse = null;
            virtualKeyboard = null;
            if (inputTestFixture == null)
                return;

            inputTestFixture.TearDown();
            inputTestFixture = null;

            // Re-enabling after TearDown forces control resolution against the restored application runtime.
            foreach (var actionMap in sharedEnabledActionMaps)
                actionMap.Enable();

            sharedEnabledActionMaps.Clear();
        }
    }
}

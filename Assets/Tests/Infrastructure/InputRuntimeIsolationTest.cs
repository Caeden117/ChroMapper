using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Infrastructure
{
    public class InputRuntimeIsolationTest : InputTestBase
    {
        [UnityTest]
        public IEnumerator RuntimeSwapSuspendsAndRestoresActionsOutsideTheSharedAsset()
        {
            InitializeVirtualInput(includeKeyboard: true);
            var originalMouse = VirtualMouse;
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Scene UI");
            asset.AddActionMap(map);
            var position = map.AddAction("Point", InputActionType.Value, "<Mouse>/position");
            var scroll = map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll");
            var disabled = map.AddAction("Disabled", InputActionType.Button, "<Keyboard>/space");
            var positionCallbacks = 0;
            var scrollCallbacks = 0;
            position.performed += _ => positionCallbacks++;
            scroll.performed += _ => scrollCallbacks++;
            position.Enable();
            scroll.Enable();
            var nested = new InputRuntimeProbe();
            try
            {
                nested.Begin();
                yield return null;
                Assert.That(
                    position.enabled,
                    Is.False,
                    "A scene action outside CMInput remained enabled across the Input System runtime swap.");
                Assert.That(scroll.enabled, Is.False);
                nested.End();

                Assert.That(position.enabled, Is.True);
                Assert.That(scroll.enabled, Is.True);
                Assert.That(
                    disabled.enabled,
                    Is.False,
                    "Restoring one enabled action must not enable its entire map.");
                SetVirtualControl(originalMouse.position, new Vector2(123f, 456f));
                SetVirtualControl(originalMouse.scroll, new Vector2(0f, 1f));
                yield return null;
                Assert.That(positionCallbacks, Is.EqualTo(1));
                Assert.That(scrollCallbacks, Is.EqualTo(1));
                Assert.That(position.ReadValue<Vector2>(), Is.EqualTo(new Vector2(123f, 456f)));
            }
            finally
            {
                nested.End();
                Object.DestroyImmediate(asset);
                TearDownVirtualInput();
            }
        }

        [Test]
        public void RestoredMouseInputReleasesMonitorsFromDisposedSceneActions()
        {
            InitializeVirtualInput(includeKeyboard: false);
            var originalMouse = VirtualMouse;
            var map = new InputActionMap("Outgoing scene UI");
            var scroll = map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll");
            scroll.Enable();
            SetVirtualControl(originalMouse.scroll, new Vector2(0f, 1f));
            InputTestFixture nested = new IsolatedInputTestFixture();
            try
            {
                nested.Setup();
                // Unloading UI during an isolated test disposes its state while the original mouse is saved.
                map.Dispose();
                nested.TearDown();
                nested = null;
                scroll.Enable();
                SetVirtualControl(originalMouse.scroll, new Vector2(0f, 2f));
                Assert.That(scroll.ReadValue<Vector2>(), Is.EqualTo(new Vector2(0f, 2f)));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                nested?.TearDown();
                map.Dispose();
                TearDownVirtualInput();
            }
        }

        private sealed class InputRuntimeProbe : InputTestBase
        {
            public void Begin() => InitializeVirtualInput(includeKeyboard: true);

            public void End() => TearDownVirtualInput();
        }
    }
}

using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tests.Editor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace Tests.Infrastructure
{
    public class InputRuntimeIsolationTest : InputTestBase
    {
        [Test]
        public void KeybindFixtureDoesNotReuseCachedUIDevicesFromAnotherRuntime()
        {
            InitializeVirtualInput(includeKeyboard: true);
            InputSystem.AddDevice<Gamepad>();
            var savedMouse = InputSystem.AddDevice<Mouse>();
            var defaultsField = typeof(InputSystemUIInputModule)
                .GetField("defaultActions", BindingFlags.Static | BindingFlags.NonPublic);
            var previousDefaults = defaultsField.GetValue(null);
            var cachedDefaults = new DefaultInputActions();
            cachedDefaults.asset.devices = new InputDevice[] { savedMouse };
            cachedDefaults.UI.Enable();
            defaultsField.SetValue(null, cachedDefaults);
            var fixture = new OptionsKeybindRebindTest();
            try
            {
                try
                {
                    fixture.Setup();
                    var module = GameObject.Find("Keybind test").GetComponentInChildren<InputSystemUIInputModule>();
                    Assert.That(module.actionsAsset, Is.Not.SameAs(cachedDefaults.asset));
                    Assert.That(
                        module.point.action.controls.All(control => InputSystem.devices.Contains(control.device)),
                        Is.True,
                        "The keybind UI must use devices from its own isolated runtime.");
                }
                finally
                {
                    fixture.TearDown();
                }
            }
            finally
            {
                defaultsField.SetValue(null, previousDefaults);
                cachedDefaults.Dispose();
                TearDownVirtualInput();
            }
        }

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

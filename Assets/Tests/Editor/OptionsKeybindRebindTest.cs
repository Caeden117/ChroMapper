using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class OptionsKeybindRebindTest : InputTestFixture
    {
        private GameObject root;
        private EventSystem eventSystem;
        private InputSystemUIInputModule inputModule;
        private CMInput previousInput;
        private CMInput isolatedInput;
        private List<LoadKeybindsController.KeybindOverride> previousOverrides;
        private Keyboard keyboard;
        private TMP_InputField[] fields;
        private OptionsInputActionController controller;
        private readonly List<InputActionMap> previouslyEnabledMaps = new();
        private readonly List<EventSystem> previouslyEnabledEventSystems = new();
        private static readonly string[] CameraModes =
        {
            "Toggle UI Mode (Normal)",
            "Toggle UI Mode (Hide UI)",
            "Toggle UI Mode (Hide Grids)",
            "Toggle UI Mode (Preview)",
            "Toggle UI Mode (Playing)"
        };

        public override void Setup()
        {
            previousInput = CMInputCallbackInstaller.InputInstance;
            previouslyEnabledMaps.Clear();
            previouslyEnabledEventSystems.Clear();
            if (previousInput != null)
            {
                foreach (var map in previousInput.asset.actionMaps)
                {
                    if (!map.enabled)
                        continue;

                    previouslyEnabledMaps.Add(map);
                    map.Disable();
                }
            }

            foreach (var existing in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (!existing.enabled)
                    continue;

                previouslyEnabledEventSystems.Add(existing);
                existing.enabled = false;
            }

            base.Setup();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            previousOverrides = LoadKeybindsController.AllOverrides;
            isolatedInput = new CMInput();
            CMInputCallbackInstaller.InputInstance = isolatedInput;
            LoadKeybindsController.AllOverrides = new List<LoadKeybindsController.KeybindOverride>();

            root = new GameObject("Keybind test", typeof(RectTransform), typeof(Canvas), typeof(OptionsKeybindsLoader));
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(root.transform);
            eventSystem = events.GetComponent<EventSystem>();
            inputModule = events.GetComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            var scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/04_Options.unity");
            try
            {
                var template = scene.GetRootGameObjects()
                    .SelectMany(obj => obj.GetComponentsInChildren<OptionsInputActionController>(true)).Single();
                controller = Object.Instantiate(template, root.transform);
                controller.gameObject.SetActive(true);
                fields = (TMP_InputField[])typeof(OptionsInputActionController)
                    .GetField("keybindInputFields", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        public override void TearDown()
        {
            inputModule.UnassignActions();
            Object.DestroyImmediate(root);
            isolatedInput.Dispose();
            CMInputCallbackInstaller.InputInstance = previousInput;
            LoadKeybindsController.AllOverrides = previousOverrides;
            base.TearDown();
            foreach (var map in previouslyEnabledMaps)
            {
                map.Enable();
            }

            foreach (var existing in previouslyEnabledEventSystems)
            {
                existing.enabled = true;
            }
        }

        [UnityTest]
        public IEnumerator EnterSavesTwoKeyCameraModeBindingFromFirstField()
        {
            yield return RebindCameraMode(0);
        }

        [UnityTest]
        public IEnumerator EnterSavesTwoKeyCameraModeBindingFromSecondField()
        {
            yield return RebindCameraMode(1);
        }

        [UnityTest]
        public IEnumerator EnterCanShortenThreeKeyCameraModeBindingToTwoKeys(
            [ValueSource(nameof(CameraModes))] string actionName)
        {
            SetExistingChord(actionName, keyboard.ctrlKey, keyboard.altKey, keyboard.oKey);
            yield return RebindCameraMode(0, actionName);
        }

        [UnityTest]
        public IEnumerator EnterCanShortenFourKeyCameraModeBindingToTwoKeys()
        {
            SetExistingChord("Toggle UI Mode (Playing)", keyboard.ctrlKey, keyboard.altKey, keyboard.shiftKey, keyboard.oKey);
            yield return RebindCameraMode(0);
        }

        [UnityTest]
        public IEnumerator EnterCanShortenThreeKeyCameraModeBindingToOneKey()
        {
            SetExistingChord("Toggle UI Mode (Playing)", keyboard.ctrlKey, keyboard.altKey, keyboard.oKey);
            var action = isolatedInput.asset.FindAction("Toggle UI Mode (Playing)", true);
            InitializeController(action);
            yield return null;
            eventSystem.SetSelectedGameObject(fields[0].gameObject);
            yield return PressAndRelease(keyboard.pKey);
            yield return PressAndRelease(keyboard.enterKey);
            AssertSavedPaths(action, keyboard.pKey.path);
        }

        [UnityTest]
        public IEnumerator AxisRequiresBothKeysBeforeSaving()
        {
            yield return RebindAxis(false);
        }

        [UnityTest]
        public IEnumerator VectorRequiresAllFourKeysBeforeSaving()
        {
            yield return RebindAxis(true);
        }

        [UnityTest]
        public IEnumerator DeselectCancelsWithoutSavingCapturedKeys()
        {
            var action = isolatedInput.asset.FindAction("Toggle UI Mode (Playing)", true);
            var originalBindings = action.bindings.Select(binding => binding.path).ToArray();
            InitializeController(action);
            yield return null;
            eventSystem.SetSelectedGameObject(fields[0].gameObject);
            yield return PressAndRelease(keyboard.pKey);
            eventSystem.SetSelectedGameObject(null);
            yield return PressAndRelease(keyboard.enterKey);
            Assert.That(LoadKeybindsController.AllOverrides, Is.Empty);
            Assert.That(action.bindings.Select(binding => binding.path), Is.EqualTo(originalBindings));
        }

        private IEnumerator RebindCameraMode(int selectedField, string actionName = "Toggle UI Mode (Playing)")
        {
            var action = isolatedInput.asset.FindAction(actionName, true);
            InitializeController(action);
            yield return null;

            eventSystem.SetSelectedGameObject(fields[selectedField].gameObject);
            Assert.That(fields[selectedField].gameObject.activeInHierarchy, Is.True,
                "Starting a rebind must not hide the field that TextMeshPro is activating.");
            yield return null;
            yield return PressAndRelease(keyboard.leftShiftKey);
            yield return PressAndRelease(keyboard.pKey);
            Assert.That(fields[0].text, Is.EqualTo("Shift"), "The first key was not captured.");
            Assert.That(fields[1].text, Is.EqualTo("P"), "The second key was not captured.");
            yield return PressAndRelease(keyboard.enterKey);

            AssertSavedPaths(action, keyboard.shiftKey.path, keyboard.pKey.path);
        }

        private IEnumerator RebindAxis(bool vector)
        {
            var action = isolatedInput.asset.FindActionMap("Camera", true).AddAction("Rebind Axis Test", InputActionType.Value);
            if (vector)
            {
                action.AddCompositeBinding("2DVector").With("up", "<Keyboard>/w").With("left", "<Keyboard>/a")
                    .With("down", "<Keyboard>/s").With("right", "<Keyboard>/d");
            }
            else
            {
                action.AddCompositeBinding("1DAxis").With("positive", "<Keyboard>/w").With("negative", "<Keyboard>/s");
            }

            action.ChangeBinding(0).WithName("Axis");
            InitializeController(action);
            yield return null;
            eventSystem.SetSelectedGameObject(fields[0].gameObject);
            yield return PressAndRelease(keyboard.pKey);
            yield return PressAndRelease(keyboard.enterKey);
            Assert.That(LoadKeybindsController.AllOverrides, Is.Empty, "Enter must not save an incomplete axis.");
            yield return PressAndRelease(keyboard.kKey);
            if (vector)
            {
                yield return PressAndRelease(keyboard.enterKey);
                Assert.That(LoadKeybindsController.AllOverrides, Is.Empty, "A vector still requires its other two directions.");
                yield return PressAndRelease(keyboard.lKey);
                yield return PressAndRelease(keyboard.oKey);
                AssertSavedPaths(action, keyboard.pKey.path, keyboard.kKey.path, keyboard.lKey.path, keyboard.oKey.path);
            }
            else
            {
                AssertSavedPaths(action, keyboard.pKey.path, keyboard.kKey.path);
            }

            Assert.That(LoadKeybindsController.AllOverrides[0].IsAxisComposite, Is.True);
        }

        private void InitializeController(InputAction action)
        {
            var composite = action.bindings.First(binding => binding.isComposite);
            controller.Init("UI Mode", action, action.bindings.Where(binding => binding.isPartOfComposite).ToList(), composite.name);
        }

        private void SetExistingChord(string actionName, params ButtonControl[] keys)
        {
            var action = isolatedInput.asset.FindAction(actionName, true);
            var composite = action.bindings.First(binding => binding.isComposite);
            LoadKeybindsController.AddKeybindOverride(new LoadKeybindsController.KeybindOverride(
                action.name, composite.name, keys.Select(key => key.path).ToList()));
        }

        private void AssertSavedPaths(InputAction action, params string[] paths)
        {
            Assert.That(LoadKeybindsController.AllOverrides.Count, Is.EqualTo(1), "The rebind should save exactly one override.");
            var saved = LoadKeybindsController.AllOverrides[0];
            Assert.That(saved.InputActionName, Is.EqualTo(action.name));
            Assert.That(saved.OverrideKeybindPaths, Is.EqualTo(paths), "Enter must replace the old chord with the captured keys.");
            Assert.That(action.bindings.Where(binding => !binding.isComposite).Select(binding => binding.path), Is.EqualTo(paths));
            Assert.That(eventSystem.currentSelectedGameObject, Is.Null, "A saved rebind must release text-field selection.");
        }

        private IEnumerator PressAndRelease(ButtonControl key)
        {
            Press(key, queueEventOnly: true);
            yield return null;
            yield return null;
            Release(key, queueEventOnly: true);
            yield return null;
            yield return null;
        }
    }
}

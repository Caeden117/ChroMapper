# ChroMapper Input Architecture

This directory contains ChroMapper's Unity Input System configuration and generated wrapper. Use this guide when adding or changing permanent input actions.

## Data flow

1. `Master.inputactions` is the authoritative source for action maps, actions, bindings, composites, and stable IDs.
2. Unity's importer generates `Master.cs`, including the `CMInput.I<ActionMapName>Actions` callback interfaces.
3. `CMInputCallbackInstaller` owns one shared `CMInput`, discovers scene components implementing generated interfaces, and installs their callbacks.
4. `InputSystemPatch` prevents a less-specific chord from firing when an enabled, more-specific chord containing the same controls is active. Equal chords remain available to separate context-specific action maps.
5. The keybind options UI discovers authored actions, while `LoadKeybindsController` restores saved binding overrides.

Unity's shortcut-input consumption is disabled in `InputSystem.inputsettings.asset`; do not change that setting from a runtime callback.

## Adding a permanent action

### Author the action

Edit `Master.inputactions`. Do not add permanent actions at runtime because the shared asset is enabled before scene controllers run.

For a mouse-wheel value:

- Use action type `Value` and expected control type `Axis`.
- Use `<Mouse>/scroll/y` as the final composite part.
- Use the value-capable `OneModifier`, `TwoModifiers`, or `ThreeModifiers` composite.
- Name modifier parts `modifier`, or `modifier1`, `modifier2`, and `modifier3`.
- Name the final value part `binding`.
- Give every action and binding a unique stable GUID.

For a button, use action type `Button` and bind the control directly unless modifiers are required.

### Regenerate the wrapper

`Master.inputactions.meta` enables wrapper generation with class name `CMInput`. Let Unity reimport the asset and regenerate `Master.cs`.

Never hand-edit generated action fields, subscriptions, or interfaces. Action names become callback names by removing spaces and punctuation.

### Implement the callback

The responsible scene component implements `CMInput.I<ActionMapName>Actions` and every method in that generated interface:

```csharp
public void OnActionName(InputAction.CallbackContext context)
```

`CMInputCallbackInstaller` handles subscription. Mutation callbacks should accept only `context.performed`, validate their UI or object context, and use the established undoable command path.

## Composite and overlap rules

`ThreeModifiersComposite` is the value-capable three-modifier equivalent of Unity's modifier composites. `EvaluateMagnitude`, unsafe `ReadValue`, and `ReadValueAsObject` must all require every modifier.

`ButtonWithThreeModifiers` is retained for rebuilding persisted legacy overrides; do not use it for a newly authored axis action.

`InputSystemPatch` compares each binding's non-composite control paths. A binding is blocked only by an enabled binding with more paths that contains every one of its paths. Runtime-added actions are absent from that startup cache.

When multiple action maps share a chord, callbacks must validate ownership through their editor context, such as the active controller, hovered object, or blocking UI. Do not inspect hardcoded physical modifier keys in the mutation helper; the authored binding and overlap patch own chord resolution.

## Rebinding

Authored actions appear in keybind options unless their action-map or action name starts with `+`, the internal identifier. An action name starting with `=` is persistent and excluded from more-specific-binding blocking.

The options UI and override loader must preserve every composite path:

- `TwoModifiers` and `ButtonWithTwoModifiers`: three paths.
- `ThreeModifiers` and `ButtonWithThreeModifiers`: four paths.

Test the default binding, rebind it, save and reload it, then rebind the restored composite again.

## Automated input tests

Use `Unity.InputSystem.TestFramework`'s `InputTestFixture` for action and binding tests. The fixture replaces platform input with an isolated runtime, so tests behave consistently in an interactive editor, Windows batchmode, and Linux Jenkins under Xvfb.

- Add dedicated virtual devices with `InputSystem.AddDevice<Mouse>()` and `InputSystem.AddDevice<Keyboard>()`.
- Never use `Mouse.current` or `Keyboard.current` from the host editor session.
- If a fixture already inherits another base class, compose an `InputTestFixture` and call `Setup()` and `TearDown()` explicitly.
- Create and enable the test's `CMInput` only after fixture setup.
- Dispose the test `CMInput` before fixture teardown, then restore shared application action maps after teardown restores the original Input System.
- Manual `InputSystem.Update()` is appropriate only inside `InputTestFixture` when the regression depends on multiple input reports before the next Unity frame.
- Verify that the production callback actually ran; do not accept a static field's default value as evidence that pointer or modifier input was dispatched.

See [the test harness guide](../Tests/README.md) for CLI execution and Jenkins audio constraints.

## Regression checklist

- Test both input directions where applicable.
- Test the intended context and every other enabled context sharing the controls.
- Test simpler and more-specific overlapping bindings.
- Test pointer-over-UI and inactive-controller behavior.
- Test keybind discovery, rebinding, saved override reload, and rebinding after reload.
- Confirm generated `Master.cs` matches `Master.inputactions`.
- Run a clean Unity compile/build.

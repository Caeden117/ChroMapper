# Tests

## Running tests

From the repository root, using PowerShell 7 or newer (works on linux):

```powershell
# run all tests
pwsh ./Assets/Tests/Test-ChroMapper.ps1
# run specific test
pwsh ./Assets/Tests/Test-ChroMapper.ps1 -TestFilter 'Tests.Infrastructure.InputTestPolicyTest'
# or filter to specific tests using anything from https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-command-line.html#testfilter
# EG run all tests with "GLS" in their class/test name
pwsh ./Assets/Tests/Test-ChroMapper.ps1 -TestFilter '(?i)GLS'
```

The runner executes PlayMode and EditMode tests and saves the full logs and results under `TestResults/cli/<timestamp>`. Add `-IncludeManual` to include manual tests. Close Unity if it has this checkout open. An editor using another worktree can stay open.

## Input tests

For mapper tests that send input, inherit [InputTestBase](Infrastructure/InputTestBase.cs). `PreviewWorkflowTestBase` and `BasicEventChunkingTestBase` already inherit it. The base isolates the Input System from editor focus and physical devices. Creating a separate `CMInput` asset alone still uses the shared runtime.

1. Call `InitializeVirtualInput(includeKeyboard: true)` before creating or enabling test actions. Use `false` for mouse-only tests.
2. Bind the production callbacks to your test actions. Send input through `VirtualMouse`, `VirtualKeyboard`, and the helpers on the base class.
3. Dispose the test actions before calling `TearDownVirtualInput`. Put cleanup in teardown or a `finally` block so it also runs after a failed assertion.

For several input reports in one Unity frame, use `queueEventOnly: true` and call `InputSystem.Update()` while the isolated runtime is active. Process modifier presses before sending the mouse or key event that uses them. Assert the effect of the production callback, such as a changed selection or a new undoable action.

If you manage `InputTestFixture` directly, create dedicated virtual devices and disable shared action maps before `Setup()`. Dispose your actions before `TearDown()`, then restore the shared maps afterward.

Use the virtual device references instead of `Mouse.current` or `Keyboard.current`. `InputTestPolicyTest` checks for those accesses. See the [input architecture guide](../Input/README.md) for action maps, bindings, and composites.

## Playback tests

Jenkins runs Linux Unity under Xvfb without a dependable native audio clock. A test that waits for `AudioSource.time` to advance can pass on Windows and hang on Jenkins.

Use `TestUtils.StartDeterministicPlaybackAtSongBpmTime` and `TestUtils.PauseDeterministicPlayback` to enter and leave the production playing state with native audio stopped. Those helpers do not freeze the clock: `AudioTimeSyncController.Update` still advances it each frame. When stepping time explicitly, keep that update from advancing the clock between your steps and assertions, while letting the playback callbacks and animation updates run. [AuroraMapParityTest](Editor/AuroraMapParityTest.cs) shows this setup and restores the controller afterward.

## Map fixtures

- Pass the map's BPM, environment, clip length, and `difficultyInfo` to `TestUtils.ReloadMap`. The default reload reuses the mapper scene and replaces the environment. Use `forceSceneReload: true` when the test needs a fresh scene.
- If a fixture restores the map by reloading it, override `CleanupTestObjects` with an empty body. Ordinary object cleanup can destroy enhancement controllers before the reload releases their targets.
- Restore `UIModeType.Normal`, the editing camera, and saved settings in `[UnityTearDown]`. For fixtures that replace the shared map, reload an empty map with `forceSceneReload: true` in `[UnityOneTimeTearDown]`.
- Clone fixture JSON before loading it if you also use it to calculate expected results. The loader mutates custom-event data.
- Capture rendered results after animation `LateUpdate` calls. `AuroraRenderStageProbe` in [AuroraMapParityTest](Editor/AuroraMapParityTest.cs) is an example.

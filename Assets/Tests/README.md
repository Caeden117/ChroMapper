## Input System tests

Input tests must not depend on editor focus, Xvfb, physical hardware, or devices discovered by the host operating system. Reference `Unity.InputSystem.TestFramework`, activate an `InputTestFixture`, and add dedicated virtual `Mouse` and `Keyboard` devices for the test when emulating input in a test.

Do not use `Mouse.current` or `Keyboard.current` before creating the fixture-owned devices. When a test already inherits a project fixture such as `TestBase`, compose `InputTestFixture` and call `Setup()` and `TearDown()` explicitly. Dispose action assets created in the isolated runtime before teardown; restore shared application action maps only after teardown restores the original Input System.

`InputSystem.Update()` may be called manually while `InputTestFixture` owns the runtime when the behavior under test requires multiple reports in one rendered frame. Outside that fixture, let Unity's player loop process input. Assertions should prove that the intended production callback ran rather than relying on pre-existing static state.

The authored action and composite conventions are documented in [the input architecture guide](../Input/README.md).

## Jenkins audio limitation

Jenkins runs Linux Unity in batchmode under Xvfb. That environment does not provide a dependable native audio playback clock: `AudioSource.Play()` may not make `AudioSource.isPlaying` true, and `AudioSource.time` or `timeSamples` may not advance. Windows batchmode can provide working audio, so a passing local CLI run alone does not prove that an audio-dependent test is Jenkins-safe.

Tests of playback-driven behavior must use a deterministic test clock and retain the production behavior they claim to cover, including playback state, `OnPlayToggled`, `OnTimeChanged`, and callback-controller frame ordering. Explicitly stop native audio in the test so Windows exercises the same unavailable-backend condition. Do not replace the production callback path with a direct visual refresh merely to avoid the audio dependency.

`BasicEventChunkingTestBase.StartDeterministicPlaybackAtSongBpmTime` and `PauseDeterministicPlayback` are the established helpers for Basic Event chunking tests.

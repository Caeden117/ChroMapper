using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // The fixture retains the sampled objects, their track parents, and all referenced custom data.
    public class SaltyPlaybackParityTest : InputTestBase
    {
        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;
        private EditModeContext editMode;
        private EditingMode previousEditingMode;
        private bool editingModeChanged;

        private static string FixturePath => PathUtils.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "SaltyPlaybackFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            // Deployed ChroMapperSettings.json runs camera FOV 90 with zero offset; the test-runner
            // defaults (FOV 60, offset 3.6) produced materially different captures, so match the
            // deployed camera settings before the map load applies them.
            previousPlayerCameraFOV = Settings.Instance.PlayerCameraFOV;
            previousPlayerCameraOffsetZ = Settings.Instance.PlayerCameraOffsetZ;
            previousCameraFOV = Settings.Instance.CameraFOV;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 215,
                environmentName: "PanicEnvironment",
                songLengthSeconds: 250);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        [SetUp]
        public void EnablePreviewAnimations()
        {
            Settings.Instance.Animations = true;
        }

        protected override void CleanupTestObjects()
        {
        }

        // Deployed ChroMapper.log shows a repeated NRE from CameraController.Update ->
        // SetLockState during the Salty map load: SetLockState dereferences the static
        // `instance` (cleared by ANY CameraController.OnDisable, set only by the editing
        // camera's Start) and Mouse.current. This drives the production OnHoldtoMoveCamera
        // path through a dedicated virtual mouse in an isolated Input System runtime.
        [UnityTest]
        public IEnumerator PreviewRightMouseHoldReachesSetLockState()
        {
            InitializeVirtualInput(includeKeyboard: true);
            var virtualMouse = InputSystem.AddDevice<Mouse>();
            InputAction holdAction = null;

            try
            {
                // The deployed failure walked a Playing -> Preview UI transition before the
                // camera moved, so reproduce that ordering before the right-button hold.
                uiMode.SetUIMode(UIModeType.Playing, false);
                yield return null;
                uiMode.SetUIMode(UIModeType.Preview, false);
                cameraManager.SelectCamera(CameraType.Editing);
                yield return null;

                var editingController = cameraManager.CameraControllers[0];
                holdAction = new InputAction("holdToMoveCamera", binding: "<Mouse>/rightButton");
                holdAction.performed += context => editingController.OnHoldtoMoveCamera(context);
                holdAction.canceled += context => editingController.OnHoldtoMoveCamera(context);
                holdAction.Enable();


                InputSystem.QueueStateEvent(virtualMouse,
                    new MouseState().WithButton(MouseButton.Right));
                InputSystem.Update();
                Assert.That(editingController.MovingCamera, Is.True,
                    "The production hold-to-move callback did not latch; the lock-state path was not reached.");

                // Let CameraController.Update run so the latched movement calls SetLockState(true).
                yield return null;
                yield return null;
            }
            finally
            {
                if (holdAction != null)
                {
                    holdAction.Disable();
                    holdAction.Dispose();
                }
                if (virtualMouse != null && virtualMouse.rightButton.isPressed)
                    InputSystem.QueueStateEvent(virtualMouse, new MouseState());
                InputSystem.Update();
                Cursor.lockState = CursorLockMode.None;
                virtualMouse = null;
                TearDownVirtualInput();
            }
        }

        // Deployed CM shows an intense white ring on the opening frame while an explicit seek to
        // beat 0 renders dark. If the b=0 ring-light event is only applied by seeking, the frame
        // right after load should differ from the frame after MoveToJsonTime(0).
        [UnityTest]
        public IEnumerator InitialLoadAndExplicitSeekKeepPanicRingsDark()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            yield return null;
            yield return null;

            // No seek: capture exactly what the freshly loaded map draws.
            var noSeek = CountNearWhitePixels(camera);
            Assert.That(noSeek, Is.LessThan(100),
                $"Initial load frame already contains {noSeek} near-white pixels in the ring window.");

            yield return SeekTo(0f);
            var afterSeek = CountNearWhitePixels(camera);
            Assert.That(afterSeek, Is.LessThan(100),
                $"Post-seek frame contains {afterSeek} near-white pixels in the ring window.");
        }

        private int CountNearWhitePixels(Camera camera)
        {
            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var nearWhite = 0;
            try
            {
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                for (var topY = 120; topY <= 430; topY++)
                {
                    var y = 575 - topY;
                    for (var x = 350; x <= 680; x++)
                    {
                        var p = pixels[y * 1024 + x];
                        if (p.r > 204 && p.g > 204 && p.b > 204) nearWhite++;
                    }
                }
                Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            return nearWhite;
        }

        private void ApplyDeployedCameraSettings()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
        }

        // Deployed-vs-game discriminator: with deployed camera settings (FOV 90, offset 0) the game
        // shows a dark opening while CM draws a white circular ring around a black center roughly at
        // x390..634 / top-down y178..414 of a 1024x576 frame. Near-white pixels inside that window
        // (excluding the bottom red platform) mean the ring is being rendered.
        [UnityTest]
        public IEnumerator OpeningPlayingViewHasNoBrightCentralRing()
        {
            ApplyDeployedCameraSettings();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            yield return SeekTo(0f);

            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var postProcess = Object.FindAnyObjectByType<PostProcessRenderingController>();
            var bloom = Object.FindAnyObjectByType<PyramidBloomController>();
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var nearWhite = 0;
            try
            {
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                var pixels = texture.GetPixels32();
                // ReadPixels is bottom-up; the reported ring spans top-down y120..430.
                for (var topY = 120; topY <= 430; topY++)
                {
                    var y = 575 - topY;
                    for (var x = 350; x <= 680; x++)
                    {
                        var p = pixels[y * 1024 + x];
                        if (p.r > 204 && p.g > 204 && p.b > 204) nearWhite++;
                    }
                }
                Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            Assert.That(nearWhite, Is.LessThan(100),
                $"The opening frame contains {nearWhite} near-white pixels inside the reported ring window.");
        }

        private static string GetPath(Transform t) =>
            t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;

        private Texture2D RenderPixels(Camera camera)
        {
            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                RenderTexture.active = rt;
                camera.Render();
                var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        [UnityTest]
        public IEnumerator PlayingNoteColorsRemainCorrectAcrossPause()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            var currentSecondsProperty = typeof(AudioTimeSyncController)
                .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
            Assert.That(currentSecondsProperty, Is.Not.Null,
                "AudioTimeSyncController.CurrentSeconds property missing.");

            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");
            var chainGrid = Object.FindAnyObjectByType<ChainGridContainer>();
            var spawnCheckedNotes = 0;
            var spawnCheckedChains = 0;
            void OnNoteSpawned(Beatmap.Base.BaseObject spawned)
            {
                var now = atsc.CurrentJsonTime;
                if (now < 176f || now >= 177f) return;
                if (!noteGrid.LoadedContainers.TryGetValue(spawned, out var con)
                    || con is not Beatmap.Containers.NoteContainer fresh || fresh.NoteData == null) return;
                if (fresh.NoteData.CustomTrack is not JSONString trackName
                    || (trackName.Value != "dropL" && trackName.Value != "dropR")) return;
                var (left, right) = ExpectedDropColors(now, scheme);
                var expected = trackName.Value == "dropL" ? left : right;
                var actual = fresh.ModelController.MpbController.Mpb.GetColor("_Color");
                if (!ColorMatches(actual, expected))
                    failures.Add($"spawn t={now:F4}: {trackName.Value} note b{fresh.NoteData.JsonTime:F3} " +
                        $"model _Color={actual} expected {expected}.");
                if (TryGetRendererColor(fresh.ModelController, out var rendered)
                    && !ColorMatches(rendered, expected))
                    failures.Add($"spawn t={now:F4}: {trackName.Value} note b{fresh.NoteData.JsonTime:F3} " +
                        $"renderer _Color={rendered} expected {expected}.");
                spawnCheckedNotes++;
            }
            void OnChainSpawned(Beatmap.Base.BaseObject spawned)
            {
                var now = atsc.CurrentJsonTime;
                if (now < 176f || now >= 177f || chainGrid == null) return;
                if (!chainGrid.LoadedContainers.TryGetValue(spawned, out var con)
                    || con is not Beatmap.Containers.ChainContainer fresh || fresh.ChainData == null) return;
                if (!Mathf.Approximately(fresh.ChainData.JsonTime, 176f)
                    || fresh.ChainData.CustomTrack is not JSONString trackName
                    || trackName.Value != "dropR") return;
                var (_, expected) = ExpectedDropColors(now, scheme);
                spawnCheckedChains++;
                foreach (var node in fresh.Nodes.Where(n => n.gameObject.activeSelf))
                {
                    var actual = node.ModelController.MpbController.Mpb.GetColor("_Color");
                    if (!ColorMatches(actual, expected))
                        failures.Add($"spawn t={now:F4}: chain b176 node " +
                            $"model _Color={actual} expected {expected}.");
                    if (TryGetRendererColor(node.ModelController, out var rendered)
                        && !ColorMatches(rendered, expected))
                        failures.Add($"spawn t={now:F4}: chain b176 node " +
                            $"renderer _Color={rendered} expected {expected}.");
                }
            }
            Mouse virtualMouse = null;
            try
            {
                InitializeVirtualInput(includeKeyboard: true);
                virtualMouse = InputSystem.AddDevice<Mouse>();

                atsc.MoveToJsonTime(160.75f);
                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                if (!atsc.IsPlaying)
                    failures.Add("setup: TogglePlaying did not enter IsPlaying.");

                foreach (var beat in new[] { 162.125f, 175.25f })
                {
                    PinClock(atsc, currentSecondsProperty, beat);
                    yield return null;
                    PinClock(atsc, currentSecondsProperty, beat);
                    yield return null;
                    CheckDropTrackColors(failures, atsc, beat, scheme);
                }

                atsc.TogglePlaying();
                atsc.StopScheduled = true;
                var pausedBeat = atsc.CurrentJsonTime;
                yield return null;
                yield return null;
                if (atsc.IsPlaying) failures.Add("pause: still IsPlaying after TogglePlaying.");
                var paused = CaptureNoteColors(noteGrid, $"paused@{pausedBeat:F3}");
                CheckNoteColor(failures, pausedBeat, "dropL", null, scheme.LeftNoteColor, 8f);
                CheckNoteColor(failures, pausedBeat, "dropR", null, scheme.RightNoteColor, 8f);

                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                PinClock(atsc, currentSecondsProperty, pausedBeat);
                var playing = CaptureNoteColors(noteGrid, $"playing@{pausedBeat:F3}");

                atsc.TogglePlaying();
                atsc.StopScheduled = true;
                yield return null;
                yield return null;
                var pausedAgain = CaptureNoteColors(noteGrid, $"pausedAgain@{pausedBeat:F3}");

                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                PinClock(atsc, currentSecondsProperty, pausedBeat);
                var resumed = CaptureNoteColors(noteGrid, $"resumed@{pausedBeat:F3}");

                CompareResumedAgainstPaused(
                    failures, "playing", playing, "paused", paused, pausedBeat);
                CompareLoadedNoteColors(failures, "pausedAgain", pausedAgain, "paused", paused);
                CompareResumedAgainstPaused(
                    failures, "resumed", resumed, "paused", paused, pausedBeat);

                var dropLGray = new Color(0.2f, 0.2f, 0.2f, 0.2f);
                var dropRGray = new Color(0.42f, 0.42f, 0.42f, 0.42f);
                var dropLStrobe = new List<(float time, float noteBeat, Color color)>();
                var dropRStrobe = new List<(float time, float noteBeat, Color color)>();
                var dropLRendererStrobe = new List<(float time, float noteBeat, Color color)>();
                var dropRRendererStrobe = new List<(float time, float noteBeat, Color color)>();
                var chainStrobe = new List<(float time, float noteBeat, Color color)>();
                var chainRendererStrobe = new List<(float time, float noteBeat, Color color)>();
                noteGrid.OnContainerSpawned += OnNoteSpawned;
                chainGrid.OnContainerSpawned += OnChainSpawned;
                atsc.TogglePlaying();
                atsc.StopScheduled = true;
                yield return null;
                atsc.MoveToJsonTime(172f);
                yield return null;
                atsc.MoveToJsonTime(176.1f);
                yield return null;
                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                yield return null;
                yield return null;
                for (var scan = 176.1f; scan <= 176.4f; scan += 0.01f)
                {
                    PinClock(atsc, currentSecondsProperty, scan);
                    yield return null;
                    yield return null;
                    var scanTime = atsc.CurrentJsonTime;
                    var dropLNote = NearestLoadedNote("dropL", scanTime, null, 8f);
                    var dropRNote = NearestLoadedNote("dropR", scanTime, null, 8f);
                    if (dropLNote != null)
                    {
                        dropLStrobe.Add((scanTime, dropLNote.NoteData.JsonTime,
                            dropLNote.ModelController.MpbController.Mpb.GetColor("_Color")));
                        if (TryGetRendererColor(dropLNote.ModelController, out var dropLRendered))
                            dropLRendererStrobe.Add((scanTime, dropLNote.NoteData.JsonTime, dropLRendered));
                    }
                    if (dropRNote != null)
                    {
                        dropRStrobe.Add((scanTime, dropRNote.NoteData.JsonTime,
                            dropRNote.ModelController.MpbController.Mpb.GetColor("_Color")));
                        if (TryGetRendererColor(dropRNote.ModelController, out var dropRRendered))
                            dropRRendererStrobe.Add((scanTime, dropRNote.NoteData.JsonTime, dropRRendered));
                    }
                    var strobeChain = chainGrid
                        .LoadedContainers.Values.OfType<Beatmap.Containers.ChainContainer>()
                        .FirstOrDefault(c => Mathf.Approximately(c.ChainData.JsonTime, 176f)
                            && c.ChainData.CustomTrack is JSONString st && st.Value == "dropR");
                    if (strobeChain != null)
                    {
                        foreach (var node in strobeChain.Nodes.Where(n => n.gameObject.activeSelf))
                        {
                            chainStrobe.Add((scanTime, 176f,
                                node.ModelController.MpbController.Mpb.GetColor("_Color")));
                            if (TryGetRendererColor(node.ModelController, out var nodeRendered))
                                chainRendererStrobe.Add((scanTime, 176f, nodeRendered));
                        }
                    }
                }

                CheckStrobeObserved(failures, "dropL note", dropLStrobe, dropLGray);
                CheckStrobeObserved(failures, "dropR note", dropRStrobe, dropRGray);
                CheckStrobeObserved(failures, "dropL note renderer", dropLRendererStrobe, dropLGray);
                CheckStrobeObserved(failures, "dropR note renderer", dropRRendererStrobe, dropRGray);
                if (chainStrobe.Count > 0)
                    CheckStrobeObserved(failures, "dropR chain node", chainStrobe, dropRGray);
                if (chainRendererStrobe.Count > 0)
                    CheckStrobeObserved(failures, "dropR chain node renderer", chainRendererStrobe, dropRGray);
                if (spawnCheckedNotes == 0)
                    failures.Add("strobe window: no dropL/dropR note spawn callback fired.");
                if (spawnCheckedChains == 0)
                    failures.Add("strobe window: no dropR chain b176 spawn callback fired.");

                foreach (var beat in new[]
                {
                    177.375f, 188.75f,
                    189.125f, 191f, 193.125f, 194.5f,
                })
                {
                    PinClock(atsc, currentSecondsProperty, beat);
                    yield return null;
                    PinClock(atsc, currentSecondsProperty, beat);
                    yield return null;
                    CheckDropTrackColors(failures, atsc, beat, scheme);
                }
            }
            finally
            {
                if (noteGrid != null) noteGrid.OnContainerSpawned -= OnNoteSpawned;
                if (chainGrid != null) chainGrid.OnContainerSpawned -= OnChainSpawned;
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
                virtualMouse = null;
                TearDownVirtualInput();
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static bool TryGetRendererColor(VisualModelController ctrl, out Color color)
        {
            color = default;
            if (ctrl == null || ctrl.Renderers.Count == 0) return false;
            var renderer = ctrl.Renderers.FirstOrDefault(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                ?? ctrl.Renderers.FirstOrDefault(r => r != null);
            if (renderer == null) return false;
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            if (!mpb.HasColor("_Color")) return false;
            color = mpb.GetColor("_Color");
            return true;
        }

        private static void PinClock(
            AudioTimeSyncController atsc, PropertyInfo currentSecondsProperty, float jsonBeat)
        {
            var songBeat = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonBeat);
            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat));
        }

        private static void CheckDropTrackColors(
            List<string> failures, AudioTimeSyncController atsc, float targetBeat, ColorSchemeSO scheme)
        {
            var actual = atsc.CurrentJsonTime;
            var (left, right) = ExpectedDropColors(actual, scheme);
            CheckNoteColor(failures, actual, "dropL", null, left, 8f);
            CheckNoteColor(failures, actual, "dropR", null, right, 8f);
            if (actual >= 175.9f && actual < 177.1f)
            {
                var chainLoaded = Object.FindAnyObjectByType<ChainGridContainer>()
                    .LoadedContainers.Values.OfType<Beatmap.Containers.ChainContainer>()
                    .Any(c => Mathf.Approximately(c.ChainData.JsonTime, 176f));
                if (chainLoaded)
                    CheckChainColors(failures, actual, 176f, "dropR", right);
            }
        }

        private static void CheckStrobeObserved(
            List<string> failures, string label,
            List<(float time, float noteBeat, Color color)> observed, Color gray)
        {
            var sawGray = false;
            var sawClear = false;
            foreach (var (time, noteBeat, color) in observed)
            {
                if (ColorMatches(color, gray))
                {
                    sawGray = true;
                }
                else if (ColorMatches(color, Color.clear))
                {
                    sawClear = true;
                }
                else
                {
                    failures.Add($"strobe window: {label} b{noteBeat:F3} at {time:F4} " +
                        $"showed unexpected _Color={color}.");
                }
            }
            if (observed.Count == 0)
                failures.Add($"strobe window: no {label} loaded to sample.");
            if (!sawGray)
                failures.Add($"strobe window: {label} never showed authored gray {gray}.");
            if (!sawClear)
                failures.Add($"strobe window: {label} never showed authored clear.");
        }

        private static (Color left, Color right) ExpectedDropColors(float beat, ColorSchemeSO scheme)
        {
            if (beat >= 176f && beat < 177f)
            {
                var phase = Mathf.Repeat(beat - 176f, 0.125f);
                return phase < 0.0625f
                    ? (new Color(0.2f, 0.2f, 0.2f, 0.2f), new Color(0.42f, 0.42f, 0.42f, 0.42f))
                    : (Color.clear, Color.clear);
            }
            if (beat >= 189f && beat < 193f)
            {
                var p = (beat - 189f) / 4f;
                return (Color.LerpUnclamped(Color.clear, scheme.LeftNoteColor, p),
                    Color.LerpUnclamped(new Color(0.2f, 0.2f, 0.2f, 0.2f), scheme.RightNoteColor, p));
            }
            return (scheme.LeftNoteColor, scheme.RightNoteColor);
        }

        private static Dictionary<Beatmap.Base.BaseObject, Color> CaptureNoteColors(
            NoteGridContainer grid, string phase)
        {
            var colors = new Dictionary<Beatmap.Base.BaseObject, Color>();
            foreach (var pair in grid.LoadedContainers)
            {
                if (pair.Value is not Beatmap.Containers.NoteContainer note || note.NoteData == null)
                    continue;
                colors[pair.Key] = note.ModelController.MpbController.Mpb.GetColor("_Color");
            }
            return colors;
        }

        private static void CompareLoadedNoteColors(
            List<string> failures,
            string aLabel,
            Dictionary<Beatmap.Base.BaseObject, Color> a,
            string bLabel,
            Dictionary<Beatmap.Base.BaseObject, Color> b)
        {
            foreach (var key in a.Keys.Union(b.Keys))
            {
                var inA = a.TryGetValue(key, out var ca);
                var inB = b.TryGetValue(key, out var cb);
                if (inA != inB)
                {
                    failures.Add($"{aLabel} vs {bLabel}: note b{key.JsonTime:F3} " +
                        $"present={inA}->{inB}.");
                }
                else if (inA && !ColorMatches(ca, cb))
                {
                    failures.Add($"{aLabel} vs {bLabel}: note b{key.JsonTime:F3} " +
                        $"_Color {ca}->{cb}.");
                }
            }
        }

        private static void CompareResumedAgainstPaused(
            List<string> failures,
            string resumedLabel,
            Dictionary<Beatmap.Base.BaseObject, Color> resumed,
            string pausedLabel,
            Dictionary<Beatmap.Base.BaseObject, Color> paused,
            float pausedBeat)
        {
            foreach (var key in resumed.Keys.Union(paused.Keys))
            {
                var inResumed = resumed.TryGetValue(key, out var cr);
                var inPaused = paused.TryGetValue(key, out var cp);
                if (inResumed && !inPaused)
                {
                    failures.Add($"{resumedLabel} vs {pausedLabel}: note b{key.JsonTime:F3} " +
                        "appeared only on resume.");
                }
                else if (inPaused && !inResumed && key.SongBpmTime <= pausedBeat + 0.001f)
                {
                    failures.Add($"{resumedLabel} vs {pausedLabel}: live note b{key.JsonTime:F3} " +
                        "dropped on resume.");
                }
                else if (inResumed && !ColorMatches(cr, cp))
                {
                    failures.Add($"{resumedLabel} vs {pausedLabel}: note b{key.JsonTime:F3} " +
                        $"_Color {cp}->{cr}.");
                }
            }
        }

        // The user's persisted workspace is EditingMode.BasicEvent (Info.dat mode 4); entering
        // Playing from it deactivates the gameplay object tracks for one frame before the mode
        // switch, the same path that detached the hehe sphere animators. The prior mode is
        // restored in [UnityTearDown], not only inline, so a failure mid-test can't leak it.
        private IEnumerator EnterPlayingFromBasicEventWorkspace()
        {
            editMode = Object.FindAnyObjectByType<EditModeContext>();
            Assert.That(editMode, Is.Not.Null, "No EditModeContext in the loaded scene.");
            previousEditingMode = editMode.EditingMode;
            editingModeChanged = true;
            editMode.EditingMode = EditingMode.BasicEvent;
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
        }

        private static Beatmap.Containers.NoteContainer NearestLoadedNote(
            string track, float beat, int? type, float maxDistance = 1f)
        {
            Beatmap.Containers.NoteContainer best = null;
            var bestDistance = float.MaxValue;
            foreach (var container in Object.FindAnyObjectByType<NoteGridContainer>()
                .LoadedContainers.Values.OfType<Beatmap.Containers.NoteContainer>())
            {
                var data = container.NoteData;
                if (data == null || data.CustomFake) continue;
                if (data.CustomTrack is not JSONString t || t.Value != track) continue;
                if (type.HasValue && data.Type != type.Value) continue;
                var distance = Mathf.Abs(data.JsonTime - beat);
                if (distance > maxDistance) continue;
                if (best == null || distance < bestDistance ||
                    (Mathf.Approximately(distance, bestDistance) && data.JsonTime < best.NoteData.JsonTime))
                {
                    best = container;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static bool ColorMatches(Color actual, Color expected) =>
            Mathf.Abs(actual.r - expected.r) <= 0.03f
            && Mathf.Abs(actual.g - expected.g) <= 0.03f
            && Mathf.Abs(actual.b - expected.b) <= 0.03f
            && Mathf.Abs(actual.a - expected.a) <= 0.03f;

        private static void CheckNoteColor(
            List<string> failures, float beat, string track, int? type, Color expected,
            float maxDistance = 1f)
        {
            var note = NearestLoadedNote(track, beat, type, maxDistance);
            if (note == null)
            {
                failures.Add($"beat {beat}: no loaded real note on track '{track}'" +
                    (type.HasValue ? $" type {type}" : "") + $" within {maxDistance} beat(s).");
                return;
            }

            var modelMpb = note.ModelController.MpbController.Mpb.GetColor("_Color");
            var containerMpb = note.MpbController.Mpb.GetColor("_Color");
            var renderers = note.ModelController.MpbController.Renderers;
            var rendererColor = (Color?)null;
            string rendererColorText = "<none>";
            if (renderers != null && renderers.Count > 0)
            {
                var block = new MaterialPropertyBlock();
                renderers[0].GetPropertyBlock(block);
                if (!block.isEmpty)
                {
                    rendererColor = block.GetColor("_Color");
                    rendererColorText = rendererColor.Value.ToString();
                }
                else
                {
                    rendererColorText = "<empty>";
                }
            }

            if (!ColorMatches(modelMpb, expected))
                failures.Add($"beat {beat}: '{track}' note JsonTime={note.NoteData.JsonTime} " +
                    $"modelMpb _Color={modelMpb} expected={expected}.");
            if (rendererColor.HasValue && !ColorMatches(rendererColor.Value, expected))
                failures.Add($"beat {beat}: '{track}' note JsonTime={note.NoteData.JsonTime} " +
                    $"rendererMpb _Color={rendererColor.Value} expected={expected}.");
        }

        private static void CheckChainColors(
            List<string> failures, float beat, float chainJsonTime, string track, Color expected)
        {
            var chain = Object.FindAnyObjectByType<ChainGridContainer>()
                .LoadedContainers.Values.OfType<Beatmap.Containers.ChainContainer>()
                .FirstOrDefault(c => Mathf.Approximately(c.ChainData.JsonTime, chainJsonTime)
                    && c.ChainData.CustomTrack is JSONString t && t.Value == track);
            if (chain == null)
            {
                var loaded = string.Join(",", Object.FindAnyObjectByType<ChainGridContainer>()
                    .LoadedContainers.Values.OfType<Beatmap.Containers.ChainContainer>()
                    .Select(c => c.ChainData.JsonTime));
                failures.Add($"beat {beat}: chain JsonTime={chainJsonTime} track='{track}' " +
                    $"not loaded (loaded chain times: {loaded}).");
                return;
            }

            var active = chain.Nodes.Where(n => n.gameObject.activeSelf).ToList();
            if (active.Count < 7)
            {
                failures.Add($"beat {beat}: chain JsonTime={chainJsonTime} has {active.Count} " +
                    "active nodes, expected >=7 for sc=8.");
                return;
            }

            foreach (var node in active)
            {
                var color = node.ModelController.MpbController.Mpb.GetColor("_Color");
                if (!ColorMatches(color, expected))
                    failures.Add($"beat {beat}: chain node '{node.name}' _Color={color} " +
                        $"expected={expected}.");
            }
        }

        // Compare rendered note colors before and after rewinding through the color animations.
        [UnityTest]
        public IEnumerator EarlyNoteRenderedColorAcrossReverseSeek()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;
            var failures = new List<string>();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;

            var assertBeats = new HashSet<float>
                { 160.75f, 161.25f, 162.125f, 165.25f, 175.25f, 188.75f };
            var visits = new Dictionary<float, int>();

            foreach (var beat in new[]
            {
                160.75f, 162.125f, 165.25f, 175.25f,
                176.09375f, 180f, 194.5f,
                162.125f, 165.25f, 175.25f, 161.25f, 162.125f,
                188.75f,
            })
            {
                yield return SeekTo(beat);
                visits.TryGetValue(beat, out var visit);
                visits[beat] = visit + 1;
                var label = $"b{beat}-v{visit}";
                var candidates = CollectFrustumNoteCandidates(camera, GeometryUtility.CalculateFrustumPlanes(camera));

                var baseline = RenderPixels(camera);
                try
                {
                    MeasureNoteBodyContribution(
                        failures,
                        camera,
                        baseline,
                        candidates,
                        beat,
                        label,
                        0,
                        "red",
                        cand => cand.Container.NoteData.CustomColor ?? scheme.LeftNoteColor,
                        assertBeats.Contains(beat));
                    MeasureNoteBodyContribution(
                        failures,
                        camera,
                        baseline,
                        candidates,
                        beat,
                        label,
                        1,
                        "blue",
                        cand => cand.Container.NoteData.CustomColor ?? scheme.RightNoteColor,
                        assertBeats.Contains(beat));
                }
                finally
                {
                    Object.DestroyImmediate(baseline);
                }
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private sealed class NoteCandidate
        {
            public Beatmap.Containers.NoteContainer Container;
            public List<Renderer> BodyRenderers;
            public Bounds Bounds;
        }

        // In-frustum loaded note containers that own at least one enabled/active model body
        // renderer (ModelController.MpbController.Renderers). Falls back to every active child
        // renderer only when the container has no model MPB renderer list (e.g. fake notes).
        private static List<NoteCandidate> CollectFrustumNoteCandidates(Camera camera, Plane[] planes)
        {
            var candidates = new List<NoteCandidate>();
            var notes = Object.FindAnyObjectByType<NoteGridContainer>();
            if (notes == null) return candidates;
            foreach (var container in notes.LoadedContainers.Values
                .OfType<Beatmap.Containers.NoteContainer>())
            {
                if (container == null || container.NoteData == null) continue;
                var allActive = container.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
                var model = container.ModelController;
                var mpbController = model == null ? null : model.MpbController;
                var body = mpbController == null
                    ? allActive
                    : mpbController.Renderers
                        .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                        .ToList();
                if (body.Count == 0) continue;
                var bounds = body[0].bounds;
                foreach (var r in body.Skip(1)) bounds.Encapsulate(r.bounds);
                if (!GeometryUtility.TestPlanesAABB(planes, bounds)) continue;
                candidates.Add(new NoteCandidate
                {
                    Container = container,
                    BodyRenderers = body,
                    Bounds = bounds,
                });
            }
            return candidates;
        }

        // Clamped on-screen pixel area of a world bounds; 0 when fully behind the near clip.
        private static float ProjectedViewportArea(Camera camera, Bounds bounds)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var allBehind = true;
            for (var cx = 0; cx < 2; cx++)
            for (var cy = 0; cy < 2; cy++)
            for (var cz = 0; cz < 2; cz++)
            {
                var corner = new Vector3(
                    cx == 0 ? bounds.min.x : bounds.max.x,
                    cy == 0 ? bounds.min.y : bounds.max.y,
                    cz == 0 ? bounds.min.z : bounds.max.z);
                var v = camera.WorldToViewportPoint(corner);
                if (v.z > camera.nearClipPlane) allBehind = false;
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            if (allBehind) return 0f;
            var width = Mathf.Clamp(max.x * 1024f, 0f, 1024f) - Mathf.Clamp(min.x * 1024f, 0f, 1024f);
            var height = Mathf.Clamp(max.y * 576f, 0f, 576f) - Mathf.Clamp(min.y * 576f, 0f, 576f);
            return Mathf.Max(0f, width) * Mathf.Max(0f, height);
        }

        private static int DominantChannel(Color color) =>
            color.r >= color.g
                ? (color.r >= color.b ? 0 : 2)
                : (color.g >= color.b ? 1 : 2);

        // Disables the representative type-matched note's model body renderers and diffs the
        // frame so the changed-pixel baseline color proves what the note actually draws,
        // excluding environment untouched by the A/B. "Representative" = the nearest UPCOMING
        // note of that type with real on-screen area: a note already at/past the strike line
        // (e.g. dropR b162 at 162.125) projects almost entirely off-screen and its body would
        // measure 0 px for purely geometric reasons, not color ones.
        private void MeasureNoteBodyContribution(
            List<string> failures,
            Camera camera,
            Texture2D baseline,
            List<NoteCandidate> candidates,
            float beat,
            string label,
            int type,
            string colorName,
            System.Func<NoteCandidate, Color> expectedColor,
            bool assert)
        {
            var typed = candidates
                .Where(c => c.Container.NoteData.Type == type)
                .Select(c => new
                {
                    Candidate = c,
                    Delta = c.Container.NoteData.JsonTime - beat,
                    Area = ProjectedViewportArea(camera, c.Bounds),
                })
                .ToList();
            var onScreen = typed.Where(t => t.Area >= 64f).ToList();
            var cand = (onScreen.Where(t => t.Delta >= -0.001f).OrderBy(t => t.Delta).FirstOrDefault()
                ?? onScreen.OrderBy(t => Mathf.Abs(t.Delta)).FirstOrDefault()
                ?? typed.OrderBy(t => Mathf.Abs(t.Delta)).FirstOrDefault())?.Candidate;
            if (cand == null)
            {
                var missing = $"{label}: no in-frustum {colorName} (type {type}) note with " +
                    "active model renderers to isolate.";
                if (assert) failures.Add(missing);
                return;
            }

            var data = cand.Container.NoteData;
            var expected = expectedColor(cand);
            var expectedChannel = DominantChannel(expected);
            var modelMpb = cand.Container.ModelController == null
                || cand.Container.ModelController.MpbController == null
                ? null
                : cand.Container.ModelController.MpbController.Mpb;
            var modelColor = modelMpb == null ? Color.clear : modelMpb.GetColor("_Color");

            foreach (var r in cand.BodyRenderers) r.enabled = false;
            Texture2D off = null;
            try
            {
                off = RenderPixels(camera);
            }
            finally
            {
                foreach (var r in cand.BodyRenderers) r.enabled = true;
            }

            var normalPixels = baseline.GetPixels32();
            var offPixels = off.GetPixels32();
            var count = 0;
            var minX = int.MaxValue; var maxX = int.MinValue;
            var minTopY = int.MaxValue; var maxTopY = int.MinValue;
            long sumR = 0, sumG = 0, sumB = 0, sumDR = 0, sumDG = 0, sumDB = 0;
            for (var y = 0; y < 576; y++)
            for (var x = 0; x < 1024; x++)
            {
                var n = normalPixels[y * 1024 + x];
                var o = offPixels[y * 1024 + x];
                var delta = Mathf.Abs(n.r - o.r) + Mathf.Abs(n.g - o.g) + Mathf.Abs(n.b - o.b);
                if (delta < 45) continue;
                count++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                var topY = 575 - y;
                if (topY < minTopY) minTopY = topY;
                if (topY > maxTopY) maxTopY = topY;
                sumR += n.r; sumG += n.g; sumB += n.b;
                sumDR += n.r - o.r; sumDG += n.g - o.g; sumDB += n.b - o.b;
            }
            Object.DestroyImmediate(off);

            var meanR = count == 0 ? 0f : sumR / (float)count;
            var meanG = count == 0 ? 0f : sumG / (float)count;
            var meanB = count == 0 ? 0f : sumB / (float)count;
            var measuredChannel = meanR >= meanG
                ? (meanR >= meanB ? 0 : 2)
                : (meanG >= meanB ? 1 : 2);

            if (!assert) return;
            var dominantMean = measuredChannel == 0 ? meanR : measuredChannel == 1 ? meanG : meanB;
            if (count < 10)
            {
                failures.Add($"{label}: {colorName} note b={data.JsonTime} changed only " +
                    $"{count} px when its body renderers were disabled; the note body is not " +
                    "visibly rendered.");
            }
            else if (dominantMean < 16f)
            {
                // A black body over bright environment still wins its own channel by a few
                // levels (e.g. (5,1,1) is nominally "red"), so dominance alone masks the
                // reported early-black appearance; the dominant channel must be visibly lit.
                failures.Add($"{label}: {colorName} note b={data.JsonTime} pixel contribution " +
                    $"baseMean=({meanR:F0},{meanG:F0},{meanB:F0}) renders near-black " +
                    $"(dominant channel mean {dominantMean:F0}/255), expected {expected} " +
                    "per the note type; early-black repro.");
            }
            else if (measuredChannel != expectedChannel)
            {
                failures.Add($"{label}: {colorName} note b={data.JsonTime} pixel contribution " +
                    $"baseMean=({meanR:F0},{meanG:F0},{meanB:F0}) is channel-{measuredChannel} " +
                    $"dominant, expected {colorName} (channel {expectedChannel}, {expected}) " +
                    "per the note type; early-black repro.");
            }
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        // A failed assertion must still leave the editor camera and preview mode safe for reload.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            TearDownVirtualInput();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null)
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = previousPlayerCameraFOV;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerCameraOffsetZ;
            Settings.Instance.CameraFOV = previousCameraFOV;
            if (editingModeChanged && editMode != null)
            {
                editMode.EditingMode = previousEditingMode;
                editingModeChanged = false;
            }
            yield break;
        }

        // The large environment scene owns AudioSource references that require a fresh scene on teardown.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

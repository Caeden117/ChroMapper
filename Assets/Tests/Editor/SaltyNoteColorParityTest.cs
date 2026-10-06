using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Salty beats 161-195 note/chain color parity reduced to fixture size: 105 real colorNotes
    // (original indices 116-220, tracks bass/seeman/dropL/dropR), the single authored
    // burstSlider (b176 c1 tb176.25 sc8 on dropR), the b0 AssignPathAnimation on
    // dropL/dropR (original index 2) and the nine b153-189 AnimateTrack events (original
    // indices 364-372, including the six color events). baseNote0Color/baseNote1Color are
    // live ColorScheme base providers, not named pointDefinitions. Fixture preserves authored
    // order; the user's Info.dat keeps EditingMode.BasicEvent so tests enter Playing from it.
    public class SaltyNoteColorParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;
        private EditModeContext editMode;
        private EditingMode previousEditingMode;
        private bool editingModeChanged;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "SaltyBeat161To195ColorFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            // Match deployed camera settings (FOV 90, offset 0) before the map load applies them.
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

        protected override void CleanupTestObjects()
        {
        }

        // User report: notes/chains on tracks `bass`, `dropL`, `dropR` between beats 161-195
        // should follow the authored track color animations (gray/black strobe at 176, base
        // color immediately after the single-point b177 events, 4-beat fade from 189-193)
        // and otherwise show the live ColorScheme base note colors.
        [UnityTest]
        public IEnumerator NotesUseBaseColorsBeforeBeat176()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;

            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");
            foreach (var (beat, side) in new[]
            {
                (160.75f, 0), (160.75f, 1),
                (162.125f, 0), (162.125f, 1),
                (175.25f, 0), (175.25f, 1),
            })
            {
                yield return SeekTo(beat);
                var track = side == 0 ? (beat < 161f ? "bass" : "dropL")
                    : beat < 161f ? "bass" : "dropR";
                var expected = side == 0 ? scheme.LeftNoteColor : scheme.RightNoteColor;
                CheckNoteColor(failures, beat, track, side, expected);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Authored b176 events [367]/[368]: 0.125-beat .42/.2 gray -> clear strobe, repeat 6969.
        // The b176 c1 burstSlider (tb=176.25, sc=8) on dropR must color every active chain node.
        // Single-point b177 base-note-color points restore the base color immediately.
        // Regression pin for the fixed repeat-overrun bug: the strobe's expanded repeats must die at
        // the b177 events (Heck stops the coroutine), chain nodes must follow the animated color, and
        // neither may leak the strobe's clear into the b177+ base region.
        [UnityTest]
        public IEnumerator Beat176FlashCyclesGrayBlackAndColorsChainSegments()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;

            var dropLGray = new Color(0.2f, 0.2f, 0.2f, 0.2f);
            var dropRGray = new Color(0.42f, 0.42f, 0.42f, 0.42f);
            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");
            foreach (var (beat, left, right) in new[]
            {
                (176.03125f, dropLGray, dropRGray),
                (176.09375f, Color.clear, Color.clear),
                (176.15625f, dropLGray, dropRGray),
                (176.21875f, Color.clear, Color.clear),
            })
            {
                yield return SeekTo(beat);
                CheckNoteColor(failures, beat, "dropL", null, left);
                CheckNoteColor(failures, beat, "dropR", null, right);
                CheckChainColors(failures, beat, 176f, "dropR", right);
            }

            yield return SeekTo(177.375f);
            CheckNoteColor(failures, 177.375f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 177.375f, "dropR", null, scheme.RightNoteColor);

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Authored b189 events [371]/[372]: 4-beat linear fade dropL clear->baseNote0Color,
        // dropR (0.2 gray)->baseNote1Color, finishing at b193.
        // Regression pin for the fixed reverse-seek staleness: a stopped scrub back before the first
        // color event must restore authored note colors, not keep the last animated clear.
        [UnityTest]
        public IEnumerator Beat189FadeReturnsBothTracksToBaseByBeat194()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;
            var fadeStartRight = new Color(0.2f, 0.2f, 0.2f, 0.2f);

            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");
            // At 191.5 both tracks have upcoming notes within the one-beat selection window.
            foreach (var beat in new[] { 188.75f, 189.125f, 190f, 191.5f, 192.5f, 193.125f, 194.5f, 195f })
            {
                yield return SeekTo(beat);
                Color left;
                Color right;
                if (beat < 189f || beat >= 193f)
                {
                    left = scheme.LeftNoteColor;
                    right = scheme.RightNoteColor;
                }
                else
                {
                    var p = (beat - 189f) / 4f;
                    left = Color.LerpUnclamped(Color.clear, scheme.LeftNoteColor, p);
                    right = Color.LerpUnclamped(fadeStartRight, scheme.RightNoteColor, p);
                }
                CheckNoteColor(failures, beat, "dropL", null, left);
                CheckNoteColor(failures, beat, "dropR", null, right);
            }

            // Reverse scrub must not leave a stale held flash/fade value behind.
            yield return SeekTo(175.25f);
            CheckNoteColor(failures, 175.25f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 175.25f, "dropR", null, scheme.RightNoteColor);
            yield return SeekTo(176.09375f);
            CheckNoteColor(failures, 176.09375f, "dropL", null, Color.clear);
            CheckNoteColor(failures, 176.09375f, "dropR", null, Color.clear);
            yield return SeekTo(193.125f);
            CheckNoteColor(failures, 193.125f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 193.125f, "dropR", null, scheme.RightNoteColor);

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Full-map reproduction narrowed to this fixture's authored events: the b176 repeat=6969
        // strobe must end when the b177 base-color events start, the b189-193 fade must leave both
        // tracks at base color by b194.5, and scrubbing back across the fade/strobe must restore
        // the same base colors the forward pass showed instead of a stale clear.
        // Regression pin for the fixed repeat-overrun + stale-MPB bugs: unfixed, b194.5 evaluated a
        // dead strobe repeat and the reverse b175.25 visit kept the pushed clear-black.
        [UnityTest]
        public IEnumerator ReverseScrubThrough176StrobeRestoresBaseNoteColors()
        {
            Settings.Instance.Animations = true;
            ApplyDeployedCameraSettings();
            yield return EnterPlayingFromBasicEventWorkspace();
            var scheme = Object.FindAnyObjectByType<BeatmapRuntimeContext>().ColorScheme;

            var failures = new List<string>();
            if (!UIMode.AnimationMode)
                failures.Add("UIMode.AnimationMode is false in Playing; track color animation may not evaluate.");

            yield return SeekTo(175.25f);
            CheckNoteColor(failures, 175.25f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 175.25f, "dropR", null, scheme.RightNoteColor);

            yield return SeekTo(176.09375f);
            CheckNoteColor(failures, 176.09375f, "dropL", null, Color.clear);
            CheckNoteColor(failures, 176.09375f, "dropR", null, Color.clear);

            yield return SeekTo(194.5f);
            CheckNoteColor(failures, 194.5f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 194.5f, "dropR", null, scheme.RightNoteColor);

            yield return SeekTo(175.25f);
            CheckNoteColor(failures, 175.25f, "dropL", null, scheme.LeftNoteColor);
            CheckNoteColor(failures, 175.25f, "dropR", null, scheme.RightNoteColor);

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
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

        private void ApplyDeployedCameraSettings()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
        }

        private static Beatmap.Containers.NoteContainer NearestLoadedNote(
            string track, float beat, int? type)
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
                if (distance > 1f) continue;
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
            List<string> failures, float beat, string track, int? type, Color expected)
        {
            var note = NearestLoadedNote(track, beat, type);
            if (note == null)
            {
                failures.Add($"beat {beat}: no loaded real note on track '{track}'" +
                    (type.HasValue ? $" type {type}" : "") + " within 1 beat.");
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
            Debug.Log($"[SaltyDiag] beat {beat}: note JsonTime={note.NoteData.JsonTime} " +
                $"track={track} type={note.NoteData.Type} modelMpb={modelMpb} " +
                $"containerMpb={containerMpb} rendererMpb={rendererColorText} expected={expected}");

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
            Debug.Log($"[SaltyDiag] beat {beat}: chain {chainJsonTime} '{track}' " +
                $"activeNodes={active.Count} node0Color={active[0].ModelController.MpbController.Mpb.GetColor("_Color")} expected={expected}");
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        // A failed assertion must still leave the editor camera, workspace, and settings safe.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);
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
    }
}

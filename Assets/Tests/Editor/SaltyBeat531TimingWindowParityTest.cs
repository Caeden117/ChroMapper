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
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // Portable Salty beat-529 "timingwindow" fake-obstacle square parity: authored
    // customData.scale (thin bars) must reach the obstacle visual, matching Heck; CM previously
    // ignored it and used only w/h dims (regression fixed via BaseObstacle.CustomVisualScale
    // applied to Animator.LocalTarget). Fixture SaltyBeat531TimingWindowFixture.json
    // preserves the authored entries in source order; the native Panic scene supplies the rig.
    public class SaltyBeat531TimingWindowParityTest : TestBase
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
            "SaltyBeat531TimingWindowFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
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

        // The b529 "timingwindow" fake-obstacle square used to render thick/small vs the game's
        // slim large outline: Heck applies authored customData.scale ([1,.1,.1] horiz /
        // [.1,1.035,.1] vert) to the obstacle visual while CM previously only applied w/h dims —
        // now fixed (visual-child scale, positions unscaled). Assertions measure
        // observable renderer bounds, not transform targets; no aggregator .Get() (mutates).
        [UnityTest]
        public IEnumerator Beat531TimingWindowKeepsThinHollowSquare()
        {
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;

            var failures = new List<string>();
            foreach (var beat in new[] { 528.7f, 529.25f, 530f, 531f, 531f })
            {
                yield return SeekTo(beat);
                var walls = Object.FindAnyObjectByType<ObstacleGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && c.ObstacleData.CustomFake
                        && Mathf.Approximately(c.ObstacleData.JsonTime, 529f)
                        && c.ObstacleData.CustomTrack is JSONString s && s.Value == "timingwindow")
                    .ToList();
                if (walls.Count != 4)
                {
                    failures.Add($"beat {beat}: loaded {walls.Count} timingwindow walls, expected 4.");
                    continue;
                }
                Beatmap.Containers.ObstacleContainer bottom = null, top = null, left = null, right = null;
                foreach (var wall in walls)
                {
                    var d = wall.ObstacleData;
                    var scale = d.CustomData?["scale"];
                    var coord = d.CustomCoordinate;
                    if (scale == null || !scale.IsArray || scale.Count != 3)
                    {
                        failures.Add($"beat {beat}: wall coord={coord} missing authored customData.scale.");
                        continue;
                    }
                    var cx = coord != null && coord.IsArray ? coord[0].AsFloat : float.NaN;
                    var cy = coord != null && coord.IsArray ? coord[1].AsFloat : float.NaN;
                    if (d.Width == 4 && Mathf.Approximately(cy, 0f)) bottom = wall;
                    else if (d.Width == 4 && Mathf.Approximately(cy, 3f)) top = wall;
                    else if (d.Width == 1 && Mathf.Approximately(cx, -2.5f)) left = wall;
                    else if (d.Width == 1 && Mathf.Approximately(cx, 1.5f)) right = wall;
                    else failures.Add($"beat {beat}: unrecognized wall w={d.Width} coord={coord}.");

                    // w/h dims must stay authored; only the visual scale differs.
                    var expectedObstacleScale = d.Width == 4
                        ? new Vector3(2.4f, 0.6f, wall.ObstacleScale.z)
                        : new Vector3(0.6f, 1.8f, wall.ObstacleScale.z);
                    if (Mathf.Abs(wall.ObstacleScale.x - expectedObstacleScale.x) > 0.03f
                        || Mathf.Abs(wall.ObstacleScale.y - expectedObstacleScale.y) > 0.03f)
                        failures.Add($"beat {beat}: wall coord={coord} ObstacleScale={wall.ObstacleScale}, " +
                            $"expected x{expectedObstacleScale.x} y{expectedObstacleScale.y} from w/h.");

                    var core = wall.CoreRenderer;
                    if (core == null)
                    {
                        failures.Add($"beat {beat}: wall coord={coord} missing CoreRenderer.");
                        continue;
                    }
                    var size = core.bounds.size;
                    // Heck applies customData.scale to the obstacle visual: horizontal bars become
                    // ~0.06 thick, verticals ~0.06 wide and 1.035x taller. Raw w/h dims instead
                    // produce 0.59/0.58-thick bars and a tiny opening.
                    if (d.Width == 4)
                    {
                        if (Mathf.Abs(size.x - 2.34f) > 0.06f)
                            failures.Add($"beat {beat}: horiz coord={coord} core size.x={size.x:F3}, expected 2.34.");
                        if (Mathf.Abs(size.y - 0.059f) > 0.02f)
                            failures.Add($"beat {beat}: horiz coord={coord} core size.y={size.y:F3}, " +
                                "expected ~0.059 (authored scale y0.1).");
                        if (Mathf.Abs(size.z - 0.055f) > 0.03f)
                            failures.Add($"beat {beat}: horiz coord={coord} core size.z={size.z:F3}, expected ~0.055.");
                    }
                    else
                    {
                        if (Mathf.Abs(size.x - 0.058f) > 0.02f)
                            failures.Add($"beat {beat}: vert coord={coord} core size.x={size.x:F3}, " +
                                "expected ~0.058 (authored scale x0.1).");
                        if (Mathf.Abs(size.y - 1.853f) > 0.06f)
                            failures.Add($"beat {beat}: vert coord={coord} core size.y={size.y:F3}, expected 1.853.");
                        if (Mathf.Abs(size.z - 0.055f) > 0.03f)
                            failures.Add($"beat {beat}: vert coord={coord} core size.z={size.z:F3}, expected ~0.055.");
                    }
                    var mpb = new MaterialPropertyBlock();
                    core.GetPropertyBlock(mpb);
                    var cutout = mpb.GetFloat("_Cutout");
                    if (Mathf.Approximately(beat, 528.7f) && cutout < 0.99f)
                        failures.Add($"beat 528.7: wall coord={coord} _Cutout={cutout:F3}, expected ~1 (hidden pre-fade).");
                    if (Mathf.Approximately(beat, 531f))
                    {
                        if (cutout > 0.01f)
                            failures.Add($"beat 531: wall coord={coord} _Cutout={cutout:F3}, expected ~0 (faded in).");
                        if (!core.enabled || !core.gameObject.activeInHierarchy)
                            failures.Add($"beat 531: wall coord={coord} core renderer not active.");
                    }
                }
                if (left != null && right != null && top != null && bottom != null
                    && left.CoreRenderer != null && right.CoreRenderer != null
                    && top.CoreRenderer != null && bottom.CoreRenderer != null)
                {
                    var lb = left.CoreRenderer.bounds;
                    var rb = right.CoreRenderer.bounds;
                    var tb = top.CoreRenderer.bounds;
                    var bb = bottom.CoreRenderer.bounds;
                    var xSep = rb.center.x - lb.center.x;
                    var ySep = tb.center.y - bb.center.y;
                    if (Mathf.Abs(xSep - 2.4f) > 0.06f)
                        failures.Add($"beat {beat}: left/right center x separation {xSep:F3}, expected 2.4.");
                    if (Mathf.Abs(ySep - 1.8f) > 0.06f)
                        failures.Add($"beat {beat}: top/bottom center y separation {ySep:F3}, expected 1.8.");
                    if (beat >= 530f)
                    {
                        var xOuter = rb.max.x - lb.min.x;
                        var xGap = rb.min.x - lb.max.x;
                        var yOuter = tb.max.y - bb.min.y;
                        var yGap = tb.min.y - bb.max.y;
                        if (xGap <= 0 || yGap <= 0)
                            failures.Add($"beat {beat}: square has no hollow opening (xGap={xGap:F2} yGap={yGap:F2}).");
                        else
                        {
                            if (xGap / xOuter < 0.88f)
                                failures.Add($"beat {beat}: x opening ratio {xGap / xOuter:F2} " +
                                    $"(gap {xGap:F2}/outer {xOuter:F2}), expected >=0.88 thin-frame.");
                            if (yGap / yOuter < 0.88f)
                                failures.Add($"beat {beat}: y opening ratio {yGap / yOuter:F2} " +
                                    $"(gap {yGap:F2}/outer {yOuter:F2}), expected >=0.88 thin-frame.");
                        }
                    }
                }
            }
            Debug.Log($"[SaltyDiag] square test camera fov={camera.fieldOfView} aspect={camera.aspect}");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Regression (full-map RED artifact TestResults/cli/20260928-183421): the four b529
        // timingwindow walls have authored d=2 but the b529 AnimateTrack carries time:[0], which
        // freezes their despawn lifetime. Continuous playback keeps them pooled and faded-in well
        // past b531, while a stopped seek to b602.25 unloads all four — the "walls missing after
        // scrub" report reduced to this portable fixture.
        [UnityTest]
        public IEnumerator TimeFrozenWallsSurviveStoppedSeekPastAuthoredDuration()
        {
            var obstacleGrid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(obstacleGrid, Is.Not.Null, "ObstacleGridContainer missing.");

            var authored = BeatSaberSongContainer.Instance.Map.Obstacles
                .Where(o => o.CustomFake && Mathf.Approximately(o.JsonTime, 529f))
                .ToList();
            Assert.That(authored.Count, Is.EqualTo(4),
                $"expected 4 authored b529 fake walls, found {authored.Count}.");

            var failures = new List<string>();

            List<Beatmap.Containers.ObstacleContainer> TimingWindowWalls() =>
                obstacleGrid.LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && authored.Contains(c.ObstacleData))
                    .ToList();

            void Sample(float beat, string modeName)
            {
                var walls = TimingWindowWalls();
                if (walls.Count != 4)
                {
                    failures.Add($"{modeName} beat {beat}: loaded {walls.Count}/4 timingwindow walls " +
                        "(time:[0] freezes their authored d=2 lifetime).");
                    return;
                }
                foreach (var wall in walls)
                {
                    var coord = wall.ObstacleData.CustomCoordinate;
                    var core = wall.CoreRenderer;
                    if (!wall.gameObject.activeInHierarchy)
                        failures.Add($"{modeName} beat {beat}: wall coord={coord} inactive.");
                    if (core == null || !core.enabled)
                        failures.Add($"{modeName} beat {beat}: wall coord={coord} core renderer not enabled.");
                    if (wall.Animator == null || !wall.Animator.isActiveAndEnabled)
                        failures.Add($"{modeName} beat {beat}: wall coord={coord} animator not enabled.");
                    if (core == null) continue;
                    var b = core.bounds;
                    if (b.size.sqrMagnitude <= 0f)
                        failures.Add($"{modeName} beat {beat}: wall coord={coord} degenerate bounds {b}.");
                    var mpb = new MaterialPropertyBlock();
                    core.GetPropertyBlock(mpb);
                    var cutout = mpb.GetFloat("_Cutout");
                    // Fixture has no later dissolve-out: once the b529 d2 fade completes the
                    // square stays fully shown (cutout ~0) for the rest of the frozen lifetime.
                    var p = Mathf.Clamp01((beat - 529f) / 2f);
                    var expectedCutout = (1f - p) * (1f - p); // dissolve easeOutQuad -> cutout
                    if (Mathf.Abs(cutout - expectedCutout) > 0.12f)
                        failures.Add($"{modeName} beat {beat}: wall coord={coord} _Cutout={cutout:F3}, " +
                            $"expected ~{expectedCutout:F2} from the authored d2 dissolve-in.");
                }
            }

            foreach (var preview in new[] { false, true })
            {
                var modeName = preview ? "Preview" : "Playing";
                if (preview)
                {
                    // Gameplay workspace must be applied AFTER SetUIMode restores the pre-play
                    // mode (see SaltyFullMapPlacementParityTest scrub parity test).
                    uiMode.SetUIMode(UIModeType.Preview, false);
                    editMode.EditingMode = EditingMode.Gameplay;
                    editingModeChanged = true;
                    cameraManager.SelectCamera(CameraType.Editing);
                    yield return null;
                    Assert.That(obstacleGrid.enabled, Is.True,
                        "Preview: ObstacleGridContainer is disabled under Gameplay workspace.");
                }
                else
                {
                    yield return EnterPlayingFromBasicEventWorkspace();
                }
                yield return null;

                foreach (var beat in new[] { 528.7f, 530.25f, 602.25f, 530.25f, 602.25f })
                {
                    yield return SeekTo(beat);
                    Sample(beat, modeName);
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

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

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

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

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

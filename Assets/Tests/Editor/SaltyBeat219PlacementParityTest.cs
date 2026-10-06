using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Portable Salty beat-219..229 positional parity: fake note pairs on [slayN, shitballsN]
    // tracks and the b221 fake walls. Heck Noodle semantics multiply track/path position offsets
    // by the 0.6 lane distance (AnimationHelper.GetObjectOffset); CM previously applied them raw
    // for V3 GameplayObject (now fixed in TrackAnimator).
    // Fixture SaltyBeat219To225PlacementFixture.json preserves the
    // authored entries in source order; the native Panic scene supplies the ring tunnel.
    public class SaltyBeat219PlacementParityTest : TestBase
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
            "SaltyBeat219To225PlacementFixture.json");

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

        // Track offsetPosition (slay225-229 authored Y=4,2,0,-2,-4 in lane units) must be
        // multiplied by the 0.6 lane distance like Heck's AnimationHelper.GetObjectOffset, so the
        // pair heights render at 2.4/1.2/0/-1.2/-2.4. Regression for TrackAnimator GameplayObject
        // offsets that were applied raw.
        [UnityTest]
        public IEnumerator EarlyFakePairsUseNoodleLaneDistance()
        {
            yield return EnterPlayingFromBasicEventWorkspace();
            if (!UIMode.AnimationMode)
                Debug.LogWarning("[SaltyDiag] fake-pair UIMode.AnimationMode=false");

            var failures = new List<string>();
            foreach (var beat in new[] { 219.5f, 220.25f, 219.5f })
            {
                yield return SeekTo(beat);
                var fakes = FakePairNotes();
                if (fakes.Count != 10)
                    failures.Add($"beat {beat}: loaded {fakes.Count} fake pair notes, expected 10.");
                var expectedY = new Dictionary<float, float>
                {
                    { 225f, 2.4f }, { 226f, 1.2f }, { 227f, 0f }, { 228f, -1.2f }, { 229f, -2.4f },
                };
                var c0BodyDelta = new Dictionary<float, float>();
                foreach (var note in fakes)
                {
                    var animator = note.Animator;
                    if (animator == null || animator.TargetType != Beatmap.Animations.ObjectAnimator.TargetTypes.GameplayObject)
                    {
                        failures.Add($"beat {beat}: fake {note.NoteData.JsonTime} c{note.NoteData.Type} " +
                            "animator missing/not GameplayObject.");
                        continue;
                    }
                    if (animator.OffsetPosition.Count <= 0)
                    {
                        failures.Add($"beat {beat}: fake {note.NoteData.JsonTime} c{note.NoteData.Type} " +
                            "OffsetPosition is empty.");
                        continue;
                    }
                    var expected = expectedY[note.NoteData.JsonTime];
                    // OffsetPosition.Get() mutates the aggregator (resets Count to Keep); measure the
                    // non-mutating rendered outcome instead.
                    var renderer = note.GetComponentsInChildren<MeshRenderer>(true)
                        .FirstOrDefault(r => r.enabled && r.gameObject.activeInHierarchy
                            && r.name == "CubeNoteSmooth_LOD0");
                    if (renderer == null)
                    {
                        failures.Add($"beat {beat}: fake {note.NoteData.JsonTime} c{note.NoteData.Type} " +
                            "no active CubeNoteSmooth_LOD0 renderer.");
                        continue;
                    }
                    var bodyDelta = renderer.bounds.center.y - note.transform.position.y;
                    if (note.NoteData.Type == 0)
                        c0BodyDelta[note.NoteData.JsonTime] = bodyDelta;
                    if (Mathf.Abs(bodyDelta - expected) > 0.12f)
                        failures.Add($"beat {beat}: fake {note.NoteData.JsonTime} c{note.NoteData.Type} " +
                            $"renderer {renderer.name} center.y - container y = {bodyDelta:F3}, expected {expected} " +
                            "(lane-unit offsets must be multiplied by 0.6).");
                }
                if (c0BodyDelta.TryGetValue(225f, out var d225) && c0BodyDelta.TryGetValue(226f, out var d226)
                    && Mathf.Abs(d225 - d226 - 1.2f) > 0.03f)
                    failures.Add($"beat {beat}: slay225->226 body gap {d225 - d226:F3}, expected 1.2.");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // The b225 fake pair (definitePosition [[0,0,12,0.5]]) must hold a fixed world position
        // while fading: post-LateUpdate sampling pins z at 7.2 (12 x 0.6) under the ring tunnel
        // and y at the resting lane height + slay225 offset. Regression for the track-parent
        // jump y leaking into definite-position notes (fixed via Track.HoldDefinitePosition).
        [UnityTest]
        public IEnumerator FirstFakePairHoldsDefinitePositionUnderRingsWhileFading()
        {
            yield return EnterPlayingFromBasicEventWorkspace();

            var failures = new List<string>();
            // Baseline center.y captured at 219.5; definite-position notes must not drift while
            // fading in (Heck DefiniteNoteJump pins them at definite pos + internal offset).
            var baselineY = new Dictionary<int, float>();
            foreach (var beat in new[] { 219.5f, 220.25f, 221.5f })
            {
                yield return SeekTo(beat);
                var pair = FakePairNotes()
                    .Where(n => Mathf.Approximately(n.NoteData.JsonTime, 225f))
                    .OrderBy(n => n.NoteData.Type)
                    .ToList();
                if (pair.Count != 2)
                {
                    failures.Add($"beat {beat}: loaded {pair.Count} fake@225 notes, expected 2.");
                    continue;
                }
                var centers = new List<Vector3>();
                foreach (var note in pair)
                {
                    var anim = note.NoteData.CustomAnimation;
                    if (anim == null || !anim.HasKey("definitePosition"))
                        failures.Add($"beat {beat}: fake@225 c{note.NoteData.Type} missing definitePosition.");
                    var animator = note.Animator;
                    if (animator == null || animator.OffsetPosition.Count <= 0
                        || animator.WorldPosition.Count <= 0)
                    {
                        failures.Add($"beat {beat}: fake@225 c{note.NoteData.Type} animator " +
                            "must have both track offset and definite world position controllers.");
                        continue;
                    }
                    if (!animator.isActiveAndEnabled)
                    {
                        failures.Add($"beat {beat}: fake@225 c{note.NoteData.Type} animator not enabled.");
                        continue;
                    }
                    // SeekTo samples between Update() (track jump travel) and LateUpdate()
                    // (definite WorldPosition + Track.HoldDefinitePosition). Run LateUpdate once
                    // to align with the runtime end-of-frame phase; aggregator counts only, no Get().
                    animator.LateUpdate();
                    var renderer = note.GetComponentsInChildren<MeshRenderer>(true)
                        .FirstOrDefault(r => r.enabled && r.gameObject.activeInHierarchy
                            && r.name == "CubeNoteSmooth_LOD0");
                    if (renderer == null)
                    {
                        failures.Add($"beat {beat}: fake@225 c{note.NoteData.Type} no active CubeNoteSmooth_LOD0.");
                        continue;
                    }
                    var center = renderer.bounds.center;
                    centers.Add(center);
                    if (Mathf.Abs(center.z - 7.2f) > 0.15f)
                        failures.Add($"beat {beat}: fake@225 c{note.NoteData.Type} center z={center.z:F2}, " +
                            "expected 7.2 (definitePosition z12 x 0.6).");
                    var expectedY = note.NoteData.GetPosition().y
                        + BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset
                        + 4f * BeatmapConstant.LaneSize;
                    if (Mathf.Abs(center.y - expectedY) > 0.1f)
                        failures.Add($"beat {beat}: fake@225 c{note.NoteData.Type} center y={center.y:F2}, " +
                            $"expected {expectedY:F2} (note lane + offsets + slay225 y4 x 0.6).");
                    if (!baselineY.TryGetValue(note.NoteData.Type, out var by))
                        baselineY[note.NoteData.Type] = center.y;
                    else if (Mathf.Abs(center.y - by) > 0.05f)
                        failures.Add($"beat {beat}: fake@225 c{note.NoteData.Type} center y={center.y:F2} " +
                            $"drifted {center.y - by:+0.00;-0.00} from 219.5 baseline {by:F2} " +
                            "(definite position must hold while fading).");
                }

                var ring = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                    .Where(r => r.name == "Ring" && r.transform.parent != null
                        && r.transform.parent.name.StartsWith("Panels4TrackLaneRing")
                        && r.enabled && r.gameObject.activeInHierarchy)
                    .OrderBy(r => Mathf.Abs(r.bounds.center.z - 10f))
                    .FirstOrDefault();
                if (ring != null && centers.Count == 2)
                {
                    var centroid = centers.Aggregate(Vector3.zero, (a, c) => a + c) / 2f;
                    if (Mathf.Abs(centroid.z - ring.bounds.center.z) > 3f)
                        failures.Add($"beat {beat}: fake@225 centroid z={centroid.z:F2} is " +
                            $"{Mathf.Abs(centroid.z - ring.bounds.center.z):F2}m from nearest ring " +
                            $"z={ring.bounds.center.z:F2}; should hover under the rings.");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // b221 red walls (beat0-7) and the white wall (["1/4","pee2"]) must keep authored width 4
        // while their track offsetPosition depth applies lane units: z=9.36/8.76, not the raw
        // 13.76/12.76 that made them render too narrow. Regression for TrackAnimator
        // GameplayObject offsets applied without the 0.6 lane distance.
        [UnityTest]
        public IEnumerator Beat221WhiteAndRedWallsSpanAtGameDepth()
        {
            // Order-dependent broad-gate failure (TestResults/cli/20260928-190056): green solo
            // but red after Beat221WallsStayAtAuthoredDepthOnImmediateReverseSeek. Settings are
            // NOT the cause (failing runs log Animations=True FOV=90 OffsetZ=0); the +2.0z lands
            // in the animated Parent node under each beatN track object. Re-assert settings per
            // case anyway so teardown-restored values can never leak in.
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;
            DumpB221Diag("post-mode-switch");

            var failures = new List<string>();
            foreach (var beat in new[] { 222.25f, 223.25f, 221.5f })
            {
                if (Mathf.Approximately(beat, 222.25f))
                {
                    // Stopped-seek staging for the order-dependent +2.0z probe: same shape as
                    // SeekTo but with a dump before MoveToJsonTime, right after the seek, and
                    // after the settling frame.
                    DumpB221Diag("pre-seek@222.25");
                    Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
                    yield return null;
                    DumpB221Diag("post-seek-f0@222.25");
                    yield return null;
                    DumpB221Diag("post-seek-f1@222.25");
                }
                else
                {
                    yield return SeekTo(beat);
                }
                var walls = Object.FindAnyObjectByType<ObstacleGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && Mathf.Approximately(c.ObstacleData.JsonTime, 221f))
                    .ToList();
                var redWalls = walls.Where(c => c.ObstacleData.CustomTrack is JSONString s
                    && s.Value.StartsWith("beat")).ToList();
                var whiteWall = walls.FirstOrDefault(c => c.ObstacleData.CustomTrack is JSONArray arr
                    && arr.Children.Any(v => (string)v == "1/4" || (string)v == "pee2"));
                if (redWalls.Count != 8)
                    failures.Add($"beat {beat}: {redWalls.Count} red beat walls loaded, expected 8.");
                if (whiteWall == null)
                    failures.Add($"beat {beat}: white [1/4,pee2] wall not loaded.");

                foreach (var wall in redWalls.Concat(whiteWall == null
                    ? Enumerable.Empty<Beatmap.Containers.ObstacleContainer>()
                    : new[] { whiteWall }))
                {
                    var isWhite = ReferenceEquals(wall, whiteWall);
                    var expectedZ = isWhite ? 8.76f : 9.36f;
                    var expectedRatio = isWhite ? 0.26f : 0.24f;
                    if (Mathf.Abs(wall.ObstacleScale.x - 2.4f) > 0.03f)
                        failures.Add($"beat {beat}: wall track={wall.ObstacleData.CustomTrack} " +
                            $"ObstacleScale.x={wall.ObstacleScale.x}, expected 2.4 (authored width 4).");
                    var core = wall.CoreRenderer;
                    if (core == null)
                    {
                        failures.Add($"beat {beat}: wall track={wall.ObstacleData.CustomTrack} no CoreRenderer.");
                        continue;
                    }
                    var b = core.bounds;
                    if (Mathf.Abs(b.size.x - 2.34f) > 0.06f)
                        failures.Add($"beat {beat}: wall track={wall.ObstacleData.CustomTrack} " +
                            $"core width={b.size.x:F3}, expected 2.34.");
                    if (Mathf.Abs(b.center.z - expectedZ) > 0.12f)
                        failures.Add($"beat {beat}: wall track={wall.ObstacleData.CustomTrack} " +
                            $"core center z={b.center.z:F2}, expected {expectedZ} " +
                            "(track offsetPosition is in lane units, x0.6).");
                    var ratio = b.size.x / (b.center.z - camera.transform.position.z);
                    if (ratio < expectedRatio)
                        failures.Add($"beat {beat}: wall track={wall.ObstacleData.CustomTrack} " +
                            $"apparent width ratio {ratio:F3} < {expectedRatio} (z={b.center.z:F2}).");
                    if (isWhite)
                    {
                        var outline = wall.OutlineTransform == null ? null
                            : wall.OutlineTransform.GetComponentInChildren<MeshRenderer>(true);
                        if (outline != null && outline.bounds.size.y < 0.05f)
                            failures.Add($"beat {beat}: white wall outline height {outline.bounds.size.y:F3} < 0.05.");
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Post-fix regression coverage: stopped seeks once left the b221 walls at un-offset jump
        // depth (core z~2.755, ~3x too large on screen) instead of z9.355/8.755 — recycled wall
        // animators double-subscribed OnTimeChanged, so a second handler rewrote the preload pose
        // over the pushed track value, and drained aggregators decayed it again on later frames.
        // The fix refreshes the pool on OnTimeFlushPending, subscribes each animator once, and lets
        // gameplay OffsetPosition hold until the next flush. Full-map counterpart:
        // SaltyFullMapPlacementParityTest.Beat221WallsSeekSynchronouslyToStableGameDepth.
        [UnityTest]
        public IEnumerator Beat221WallsStayAtAuthoredDepthOnImmediateReverseSeek()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null; // let CameraController.Update apply the FOV before sampling
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            var failures = new List<string>();
            void Sample(float beat, string phase)
            {
                if (beat < 221f) return; // crossing trigger only, no wall-depth assertion
                var walls = Object.FindAnyObjectByType<ObstacleGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && Mathf.Approximately(c.ObstacleData.JsonTime, 221f))
                    .ToList();
                var redWalls = walls.Where(c => c.ObstacleData.CustomTrack is JSONString s
                    && s.Value.StartsWith("beat")).ToList();
                var whiteWall = walls.FirstOrDefault(c => c.ObstacleData.CustomTrack is JSONArray arr
                    && arr.Children.Any(v => (string)v == "1/4" || (string)v == "pee2"));
                if (redWalls.Count != 8)
                {
                    failures.Add($"beat {beat} {phase}: {redWalls.Count} red beat walls loaded, expected 8.");
                    return;
                }
                if (whiteWall == null)
                {
                    failures.Add($"beat {beat} {phase}: white [1/4,pee2] wall not loaded.");
                    return;
                }
                var camPos = camera.transform.position;
                if (Vector3.Distance(camPos, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                    failures.Add($"beat {beat} {phase}: playing camera world={camPos}, expected ~(0,1.65,0).");
                if (Mathf.Abs(camera.fieldOfView - 90f) > 0.5f)
                    failures.Add($"beat {beat} {phase}: playing camera fov={camera.fieldOfView}, expected 90.");
                foreach (var wall in redWalls.Concat(new[] { whiteWall }))
                {
                    var isWhite = ReferenceEquals(wall, whiteWall);
                    // b221.1 sits mid-approach: its exact depth is history-dependent and cannot be
                    // pinned from a mid-phase sample, so require only a sane world-depth range that
                    // excludes the wrong preload pose (z~1-3) but allows approach or stable depth.
                    var isApproachSample = Mathf.Approximately(beat, 221.1f);
                    var expectedZ = isWhite ? 8.755f : 9.355f;
                    var minRatio = isWhite ? 0.26f : 0.24f;
                    var core = wall.CoreRenderer;
                    if (core == null)
                    {
                        failures.Add($"beat {beat} {phase}: wall track={wall.ObstacleData.CustomTrack} no CoreRenderer.");
                        continue;
                    }
                    var b = core.bounds;
                    if (isApproachSample
                        ? b.center.z < 5f || b.center.z > 11f
                        : Mathf.Abs(b.center.z - expectedZ) > 0.12f)
                        failures.Add($"beat {beat} {phase}: wall track={wall.ObstacleData.CustomTrack} " +
                            $"core center z={b.center.z:F3}, expected " +
                            (isApproachSample ? "in range [5,11]" : $"{expectedZ}") + " (track offset applied).");
                    if (Mathf.Abs(b.size.x - 2.342f) > 0.05f)
                        failures.Add($"beat {beat} {phase}: wall track={wall.ObstacleData.CustomTrack} " +
                            $"core size.x={b.size.x:F3}, expected 2.342.");
                    var ratio = b.size.x / (b.center.z - camPos.z);
                    if (ratio < minRatio)
                        failures.Add($"beat {beat} {phase}: wall track={wall.ObstacleData.CustomTrack} " +
                            $"apparent width ratio {ratio:F3} < {minRatio} (z={b.center.z:F2}).");
                }
            }

            foreach (var beat in new[] { 221.1f, 222.25f, 223f, 222.25f, 220.75f, 222.25f, 221.5f, 222.25f })
            {
                atsc.MoveToJsonTime(beat);
                Sample(beat, "immediate");
                yield return null;
                Sample(beat, "post-frame");
            }

            // Same-map mode cycle: Normal/Edit -> Playing, then the same checks again.
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            atsc.MoveToJsonTime(222.25f);
            Sample(222.25f, "transition-immediate");
            yield return null;
            Sample(222.25f, "transition-post-frame");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static List<Beatmap.Containers.NoteContainer> FakePairNotes() =>
            Object.FindAnyObjectByType<NoteGridContainer>()
                .LoadedContainers.Values
                .OfType<Beatmap.Containers.NoteContainer>()
                .Where(c => c.NoteData != null && c.NoteData.CustomFake
                    && c.NoteData.JsonTime is >= 225f and <= 229f
                    && c.NoteData.CustomTrack is JSONArray arr
                    && arr.Children.Any(v => (string)v == $"slay{(int)c.NoteData.JsonTime}")
                    && arr.Children.Any(v => (string)v == $"shitballs{(int)c.NoteData.JsonTime}"))
                .ToList();

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
            yield return null; // one frame for the playing camera to apply settings
            var playingCamera = cameraManager.CameraControllers[1].Camera;
            Debug.Log($"[SaltyDiag] enter-playing settings Animations={Settings.Instance.Animations} " +
                $"PlayerCameraFOV={Settings.Instance.PlayerCameraFOV} " +
                $"PlayerCameraOffsetZ={Settings.Instance.PlayerCameraOffsetZ} " +
                $"CameraFOV={Settings.Instance.CameraFOV} " +
                $"cameraPos={playingCamera.transform.position} cameraFov={playingCamera.fieldOfView}");
        }

        // Data-only probe for the order-dependent +2.0z on the b221 walls: dumps the first loaded
        // b221 wall's OffsetPosition aggregator bookkeeping, transform/animator parents, and the
        // bruh/beatN TrackAnimator bookkeeping (children, versions, property keys) at each phase.
        // Never calls Aggregator.Get() (it mutates Count/held state).
        private void DumpB221Diag(string phase)
        {
            var obstacleGrid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            var sb = new System.Text.StringBuilder(
                $"[WallDiag2] {phase} gridEnabled={(obstacleGrid != null && obstacleGrid.enabled)}");
            if (obstacleGrid != null && obstacleGrid.SpawnCallbackController != null)
                sb.Append($" spawnOffset={obstacleGrid.SpawnCallbackController.Offset}");
            var wall = obstacleGrid == null ? null : obstacleGrid.LoadedContainers.Values
                .OfType<Beatmap.Containers.ObstacleContainer>()
                .Where(c => c.ObstacleData != null && Mathf.Approximately(c.ObstacleData.JsonTime, 221f))
                .OrderBy(c => c.ObstacleData.CustomTrack?.ToString())
                .FirstOrDefault();
            var atsc2 = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc2 != null) sb.Append($" songBpm={atsc2.CurrentSongBpmTime:F3}");
            if (wall == null)
            {
                sb.Append(" wall=<none>");
            }
            else
            {
                var an = wall.Animator;
                sb.Append($" wall={wall.name} track={wall.ObstacleData.CustomTrack} " +
                    $"readPos={wall.ReadPosition()} hjd={wall.ObstacleData.HalfJumpDuration:F3} " +
                    $"hjd2={wall.ObstacleData.HalfJumpDistance:F3}");
                if (wall.CoreRenderer != null)
                    sb.Append($" coreBounds={wall.CoreRenderer.bounds}");
                if (an != null)
                {
                    sb.Append($" animEnabled={an.isActiveAndEnabled} " +
                        $"offN={an.OffsetPosition.Count}/{an.OffsetPosition.Keep} " +
                        $"lpN={an.LocalPosition.Count}/{an.LocalPosition.Keep} " +
                        $"wpN={an.WorldPosition.Count}/{an.WorldPosition.Keep}");
                    var animTrack = an.AnimationTrack;
                    sb.Append($" animTrack={(animTrack == null ? "<null>" : animTrack.name)}");
                    if (animTrack != null)
                    {
                        sb.Append($" optLocal={animTrack.ObjectParentTransform.localPosition} " +
                            $"optWorld={animTrack.ObjectParentTransform.position} " +
                            $"trackRefs={TrackFieldDump(animTrack, wall.ObstacleData, wall)}");
                    }
                }
                var chain = new System.Text.StringBuilder();
                for (var t = wall.transform; t != null; t = t.parent)
                    chain.Append($"{t.name}:l{t.localPosition}/w{t.position}>");
                sb.Append($" chain={chain}");
            }
            foreach (var tn in new[] { "bruh", "beat7", "1/4" })
            {
                if (tracks != null && tracks.TryGetAnimationTrack(tn, out var ta) && ta != null)
                {
                    sb.Append($" | {tn}#{ta.GetInstanceID()} en={ta.enabled} " +
                        $"children={ta.Children.Count} cached={ta.CachedChildren.Length} " +
                        $"ver={ta.UpdateVersion} props=[{string.Join(",", ta.AnimatedProperties.Keys)}] " +
                        $"optLocal={ta.Track.ObjectParentTransform.localPosition} " +
                        $"trackRefs={TrackFieldDump(ta.Track, null, null)}");
                    if (ta.Animator != null)
                    {
                        var a = ta.Animator;
                        sb.Append($" animT={a.TargetType} animEn={a.isActiveAndEnabled} " +
                            $"lpN={a.LocalPosition.Count}/{a.LocalPosition.Keep} " +
                            $"wpN={a.WorldPosition.Count}/{a.WorldPosition.Keep}{AggregatorItems(a.WorldPosition)} " +
                            $"opN={a.OffsetPosition.Count}/{a.OffsetPosition.Keep} " +
                            $"lt={(a.LocalTarget == null ? "<null>" : $"{a.LocalTarget.name}@{a.LocalTarget.localPosition}")} " +
                            $"wt={(a.WorldTarget == null ? "<null>" : $"{a.WorldTarget.name}@l{a.WorldTarget.localPosition}/w{a.WorldTarget.position}")}");
                    }
                    // Non-mutating evaluation of the authored `position` push: discriminates a
                    // doubled source value from a doubled destination compose.
                    if (ta.AnimatedProperties.TryGetValue("position", out var posProp)
                        && posProp is Beatmap.Animations.AnimateProperty<Vector3> p
                        && atsc2 != null)
                    {
                        sb.Append($" posEval={p.GetLerpedValue(atsc2.CurrentJsonTime)}");
                    }
                }
                else
                {
                    sb.Append($" | {tn}=<none>");
                }
            }
            Debug.Log(sb.ToString());
        }

        // Reads the private Aggregator<T>.items backing array WITHOUT calling Get() (which would
        // mutate Count/held state), to see the actual pushed values composing Count.
        private static string AggregatorItems<T>(Beatmap.Animations.ObjectAnimator.Aggregator<T> agg)
            where T : struct
        {
            var items = (T[])typeof(Beatmap.Animations.ObjectAnimator.Aggregator<T>)
                .GetField("items", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(agg);
            return items == null ? " items=<n/a>"
                : $" items=[{string.Join(",", items.Take(agg.Count))}]";
        }

        // Reflect Track's private cached jump fields without touching aggregators: determines
        // whether ObjectParentTransform z comes from UpdateTime's cached spawn/despawn params
        // (and which gridObject/gridContainer they were cached from) rather than a value push.
        private static string TrackFieldDump(Track track, Beatmap.Base.BaseObject expectedObj,
            Beatmap.Containers.ObjectContainer expectedContainer)
        {
            const BindingFlags f = BindingFlags.NonPublic | BindingFlags.Instance;
            object Get(string n) => typeof(Track).GetField(n, f)?.GetValue(track);
            var gridObject = Get("gridObject");
            var gridContainer = Get("gridContainer");
            return $"gridObj#{(gridObject == null ? "null" : gridObject.GetHashCode().ToString())}" +
                (expectedObj == null ? "" : $" eqObj={ReferenceEquals(gridObject, expectedObj)}") +
                $" gridCtr#{(gridContainer == null ? "null" : gridContainer.GetHashCode().ToString())}" +
                (expectedContainer == null ? "" : $" eqCtr={ReferenceEquals(gridContainer, expectedContainer)}") +
                $" spawnT={Get("spawnTime"):F3} spawnP={Get("spawnPosition"):F3} " +
                $"despawnT={Get("despawnTime"):F3} despawnP={Get("despawnPosition"):F3} " +
                $"useCustom={Get("useCustom")} zScale={Get("zScale"):F2} v2={Get("v2")}";
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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // Salty positional parity (beats 219-229 fake pairs + b221 walls) against the full real map.
    // Heck Noodle semantics: track/path position offsets are in lane units and get multiplied by
    // 0.6 lane distance (AnimationHelper.GetObjectOffset); CM previously applied them raw for
    // V3 GameplayObject (now fixed in TrackAnimator). Explicit because the source map path is
    // machine-specific; run with
    // -TestFilter 'Tests.Editor.SaltyFullMapPlacementParityTest.<name>'.
    [Explicit]
    public class SaltyFullMapPlacementParityTest : TestBase
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

        private const string SourceMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/4b476 (G1ll35 d3 R415 - Salty)/ExpertPlusStandard.dat";

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
                JSON.Parse(File.ReadAllText(SourceMapPath)),
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

        // Fake pairs b225-229 sit on tracks [slayN, shitballsN]; the b0 AnimateTrack events give
        // slay225-229 offsetPosition Y = 4,2,0,-2,-4 in lane units. Heck multiplies track offsets
        // by the 0.6 lane distance, so the applied offsets should be 2.4,1.2,0,-1.2,-2.4.
        [UnityTest]
        public IEnumerator EarlyFakePairsUseNoodleLaneDistance()
        {
            yield return EnterPlayingFromBasicEventWorkspace();
            if (!UIMode.AnimationMode)
                Debug.LogWarning("[SaltyDiag] fake-pair UIMode.AnimationMode=false");

            var failures = new List<string>();
            var fakePairSpawnDiagDumped = false;
            foreach (var beat in new[] { 219.5f, 220.25f, 219.5f })
            {
                yield return SeekTo(beat);
                var fakes = FakePairNotes();
                // Diagnostics for the current 2/10 count failure: dump the ten authored candidates
                // and the first ineligible map notes once (first seek, 219.5) so we can tell
                // sequential cutoff ordering from authored not-yet-eligible notes.
                if (!fakePairSpawnDiagDumped)
                {
                    fakePairSpawnDiagDumped = true;
                    LogFakePairSpawnDiagnostics();
                }
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

        // The b225 pair has definitePosition [[0,0,12,0.5]]; post-LateUpdate sampling shows z is
        // already pinned at 7.2 (12 x 0.6) under the nearest ring (z10) — the earlier z failures
        // were a mid-phase sampling artifact — while y is now held at the resting lane height +
        // slay225 offset (Track.HoldDefinitePosition) instead of inheriting jump travel.
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

        // b221 red walls beat0..beat7 (size [4,0.2,0.01]) and white wall ["1/4","pee2"]
        // (size [4,0.01,0.01]): authored width 4 must survive, but Heck lane-unit semantics put
        // their offsetPosition depth at ~9.36/8.76, not the raw 13.76/12.76 CM computes.
        [UnityTest]
        public IEnumerator Beat221WhiteAndRedWallsSpanAtGameDepth()
        {
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;

            var failures = new List<string>();
            foreach (var beat in new[] { 222.25f, 223.25f, 221.5f })
            {
                yield return SeekTo(beat);
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

        // TEMP diagnostic: trace which transform in the fake-note hierarchy carries the
        // definitePosition z drift (39.96 -> 29.41 -> 23.83) and whether the z=7.2 definite
        // offset is applied on two ancestors. No aggregator .Get() calls (they consume Count).
        [UnityTest]
        public IEnumerator DefinitePositionTrackHierarchyDiagnostics()
        {
            yield return EnterPlayingFromBasicEventWorkspace();

            var report = new StringBuilder();
            var camera = cameraManager.CameraControllers[1].Camera;
            foreach (var beat in new[] { 219.5f, 220.25f, 221.5f, 225f, 219.5f })
            {
                yield return SeekTo(beat);
                var c0 = FakePairNotes()
                    .Where(n => Mathf.Approximately(n.NoteData.JsonTime, 225f) && n.NoteData.Type == 0)
                    .FirstOrDefault();
                var c1 = FakePairNotes()
                    .Where(n => Mathf.Approximately(n.NoteData.JsonTime, 225f) && n.NoteData.Type == 1)
                    .FirstOrDefault();
                report.AppendLine($"=== beat {beat} ===");
                report.AppendLine($"camera pos={camera.transform.position} rot={camera.transform.eulerAngles} fov={camera.fieldOfView}");
                if (c0 == null)
                {
                    report.AppendLine("fake@225 c0 not loaded");
                    continue;
                }
                var animator = c0.Animator;
                report.AppendLine($"c0 container worldPos={c0.transform.position} localPos={c0.transform.localPosition} " +
                    $"parent={(c0.transform.parent == null ? "null" : c0.transform.parent.name)} " +
                    $"njs={c0.NoteData.CustomNoteJumpMovementSpeed} " +
                    $"jumpOffset={c0.NoteData.CustomNoteJumpStartBeatOffset} " +
                    $"hjd={c0.NoteData.HalfJumpDuration} spawnBpm={c0.NoteData.SpawnSongBpmTime}");
                if (animator != null)
                {
                    report.AppendLine($"animator TargetType={animator.TargetType} " +
                        $"WorldPosition.Count={animator.WorldPosition.Count} " +
                        $"OffsetPosition.Count={animator.OffsetPosition.Count} " +
                        $"AnimatedTrack={animator.AnimatedTrack}");
                    var track = animator.AnimationTrack;
                    if (track != null)
                    {
                        var self = track.SelfTransform;
                        var parent = track.ObjectParentTransform;
                        report.AppendLine($"track name={track.name} " +
                            $"SelfTransform {self.name} world={self.position} local={self.localPosition} " +
                            $"scale={self.localScale} active={self.gameObject.activeInHierarchy}");
                        report.AppendLine($"track name={track.name} " +
                            $"ObjectParentTransform {parent.name} world={parent.position} " +
                            $"local={parent.localPosition} scale={parent.localScale} " +
                            $"active={parent.gameObject.activeInHierarchy}");
                    }
                    var wt = animator.WorldTarget;
                    if (wt != null)
                        report.AppendLine($"WorldTarget {wt.name} world={wt.position} " +
                            $"local={wt.localPosition} parent={(wt.parent == null ? "null" : wt.parent.name)}");
                    var lt = animator.LocalTarget;
                    if (lt != null)
                        report.AppendLine($"LocalTarget {lt.name} world={lt.position} " +
                            $"local={lt.localPosition} parent={(lt.parent == null ? "null" : lt.parent.name)}");
                }
                var renderer = c0.GetComponentsInChildren<MeshRenderer>(true)
                    .FirstOrDefault(r => r.name == "CubeNoteSmooth_LOD0");
                if (renderer != null)
                {
                    var vp = camera.WorldToViewportPoint(renderer.bounds.center);
                    report.AppendLine($"c0 LOD0 boundsCenter={renderer.bounds.center} viewport={vp}");
                    var t = renderer.transform;
                    var depth = 0;
                    while (t != null && depth++ < 30)
                    {
                        report.AppendLine($"  ancestor[{depth}] name={t.name} world={t.position} " +
                            $"local={t.localPosition} scale={t.localScale} " +
                            $"active={t.gameObject.activeInHierarchy}");
                        t = t.parent;
                    }
                }
                if (c1 != null)
                {
                    var r1 = c1.GetComponentsInChildren<MeshRenderer>(true)
                        .FirstOrDefault(r => r.name == "CubeNoteSmooth_LOD0");
                    if (r1 != null)
                        report.AppendLine($"c1 sibling LOD0 boundsCenter={r1.bounds.center}");
                }
            }
            var dir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "salty-definite-position-hierarchy.txt");
            File.WriteAllText(path, report.ToString());
            Debug.Log($"[SaltyDiag] definite-position hierarchy report: {path}");
        }

        // The b529 "timingwindow" fake-obstacle square renders thick/small vs the game's slim
        // large outline: Heck applies authored customData.scale ([1,.1,.1] horiz / [.1,1.035,.1]
        // vert) to the obstacle visual root while CM only applies w/h dims. Assertions measure
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

        // TEMP diagnostic: the b529 "timingwindow" fake-obstacle square used to render
        // thick/small vs the game's slim large outline. Authored customData.scale ([1,.1,.1]
        // horiz bars, [.1,1.035,.1] vertical bars) is applied to the obstacle visual in Heck;
        // CM previously ignored it and applied only w/h dims — now fixed via
        // BaseObstacle.CustomVisualScale on Animator.LocalTarget. Postfix filenames keep the
        // frozen pre-fix captures intact. Evidence only — no assertions beyond presence.
        [UnityTest]
        public IEnumerator Beat531TimingWindowSquareDiagnostics()
        {
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;
            var report = new StringBuilder();
            var dir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(dir);

            List<Beatmap.Containers.ObstacleContainer> SquareWalls() =>
                Object.FindAnyObjectByType<ObstacleGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && c.ObstacleData.CustomFake
                        && Mathf.Approximately(c.ObstacleData.JsonTime, 529f)
                        && c.ObstacleData.CustomTrack is JSONString s && s.Value == "timingwindow")
                    .ToList();

            void ProjectCorners(StringBuilder sb, string label, MeshRenderer r)
            {
                var e = r.bounds;
                float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f, minDepth = float.MaxValue;
                foreach (var cx in new[] { e.min.x, e.max.x })
                foreach (var cy in new[] { e.min.y, e.max.y })
                foreach (var cz in new[] { e.min.z, e.max.z })
                {
                    var vp = camera.WorldToViewportPoint(new Vector3(cx, cy, cz));
                    if (vp.z > camera.nearClipPlane)
                    {
                        minX = Mathf.Min(minX, vp.x); maxX = Mathf.Max(maxX, vp.x);
                        minY = Mathf.Min(minY, vp.y); maxY = Mathf.Max(maxY, vp.y);
                        minDepth = Mathf.Min(minDepth, vp.z);
                    }
                }
                sb.AppendLine($"    {label}: worldBounds min={e.min} max={e.max} " +
                    $"viewport x[{minX:F3}..{maxX:F3}] y[{minY:F3}..{maxY:F3}] depth={minDepth:F2} " +
                    $"pixels x[{minX * 1024:F0}..{maxX * 1024:F0}] y[{minY * 576:F0}..{maxY * 576:F0}]");
            }

            foreach (var beat in new[] { 528.7f, 528.95f, 529.25f, 530f, 531f, 531.25f, 532f, 531f })
            {
                yield return SeekTo(beat);
                report.AppendLine($"=== beat {beat} camera pos={camera.transform.position} " +
                    $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} " +
                    $"aspect={camera.aspect} ===");
                var walls = SquareWalls();
                report.AppendLine($"loaded timingwindow walls: {walls.Count}");
                foreach (var wall in walls)
                {
                    var d = wall.ObstacleData;
                    var scale = d.CustomData?["scale"];
                    report.AppendLine($"  wall w={d.Width} h={d.Height} coord={d.CustomCoordinate} " +
                        $"customScale={(scale == null ? "none" : scale.ToString())} " +
                        $"ObstacleScale={wall.ObstacleScale} " +
                        $"transform localScale={wall.transform.localScale} lossy={wall.transform.lossyScale} " +
                        $"worldPos={wall.transform.position} localPos={wall.transform.localPosition} " +
                        $"parent={(wall.transform.parent == null ? "null" : wall.transform.parent.name)} " +
                        $"Animator.Scale.Count={(wall.Animator == null ? "no-animator" : wall.Animator.Scale.Count.ToString())} " +
                        $"CoreScale={(wall.CoreTransform == null ? "null" : wall.CoreTransform.localScale.ToString())} " +
                        $"OutlineScale={(wall.OutlineTransform == null ? "null" : wall.OutlineTransform.localScale.ToString())}");
                    if (wall.CoreRenderer != null)
                    {
                        var r = wall.CoreRenderer;
                        var mpb = new MaterialPropertyBlock();
                        r.GetPropertyBlock(mpb);
                        report.AppendLine($"    core active={r.enabled}/{r.gameObject.activeInHierarchy} " +
                            $"mat={(r.sharedMaterial == null ? "null" : r.sharedMaterial.name)} " +
                            $"_Color={mpb.GetColor("_Color")} _Cutout={mpb.GetFloat("_Cutout")}");
                        ProjectCorners(report, "core  ", r);
                    }
                    var outline = wall.OutlineTransform == null ? null
                        : wall.OutlineTransform.GetComponentInChildren<MeshRenderer>(true);
                    if (outline != null)
                    {
                        var mpb = new MaterialPropertyBlock();
                        outline.GetPropertyBlock(mpb);
                        report.AppendLine($"    outline active={outline.enabled}/{outline.gameObject.activeInHierarchy} " +
                            $"_Color={mpb.GetColor("_Color")} _Cutout={mpb.GetFloat("_Cutout")}");
                        ProjectCorners(report, "outln ", outline);
                    }
                }

                if (Mathf.Approximately(beat, 531f) && walls.Count == 4)
                {
                    // Union of outline bounds + inside-edge gaps (frame slimness evidence).
                    Bounds? u = null;
                    var verticals = new List<Bounds>();
                    var horizontals = new List<Bounds>();
                    foreach (var wall in walls)
                    {
                        var r = wall.CoreRenderer;
                        if (r == null) continue;
                        u = u.HasValue ? u.Value : r.bounds;
                        if (!Equals(u.Value, r.bounds))
                        {
                            var ub = u.Value; ub.Encapsulate(r.bounds); u = ub;
                        }
                        var isHoriz = wall.ObstacleData.Width == 4;
                        (isHoriz ? horizontals : verticals).Add(r.bounds);
                    }
                    if (u.HasValue)
                    {
                        report.AppendLine($"  union outline world bounds min={u.Value.min} max={u.Value.max} " +
                            $"size={u.Value.size}");
                        var c0 = camera.WorldToViewportPoint(u.Value.min);
                        var c1 = camera.WorldToViewportPoint(u.Value.max);
                        report.AppendLine($"  union viewport x[{c0.x:F3}..{c1.x:F3}] y[{c0.y:F3}..{c1.y:F3}] " +
                            $"pixels x[{c0.x * 1024:F0}..{c1.x * 1024:F0}] y[{c0.y * 576:F0}..{c1.y * 576:F0}]");
                    }
                    if (horizontals.Count == 2 && verticals.Count == 2)
                    {
                        var h = horizontals.OrderBy(b => b.min.y).ToArray();
                        var v = verticals.OrderBy(b => b.min.x).ToArray();
                        report.AppendLine($"  inner gap: horizontal bars y[{h[0].max.y:F2}..{h[1].min.y:F2}] " +
                            $"(gap={h[1].min.y - h[0].max.y:F2}) " +
                            $"vertical bars x[{v[0].max.x:F2}..{v[1].min.x:F2}] (gap={v[1].min.x - v[0].max.x:F2}) " +
                            $"bar thickness h={(h[0].max.y - h[0].min.y):F3} v={(v[0].max.x - v[0].min.x):F3}");
                    }
                }
            }

            // A/B visibility proof at 531: disable only these four walls' renderers.
            // Filenames carry -postfix so the pre-fix captures stay frozen for comparison.
            yield return SeekTo(531f);
            var wallsAt531 = SquareWalls();
            Assert.That(wallsAt531, Has.Count.EqualTo(4), "expected 4 timingwindow walls at b531");
            var renderers = wallsAt531
                .SelectMany(w => new[] { w.CoreRenderer,
                    w.OutlineTransform == null ? null : w.OutlineTransform.GetComponentInChildren<MeshRenderer>(true) })
                .Where(r => r != null && r.enabled)
                .ToList();
            RenderPixelsAndSave(camera, Path.Combine(dir, "salty-square-postfix-b531-normal.png"));
            try
            {
                foreach (var r in renderers) r.enabled = false;
                RenderPixelsAndSave(camera, Path.Combine(dir, "salty-square-postfix-b531-off.png"));
            }
            finally
            {
                foreach (var r in renderers) r.enabled = true;
            }
            // End-of-frame phase: Update only ran mid-phase here; LateUpdate applies track/world
            // placement. Run it once per wall and capture the real frame state.
            foreach (var wall in wallsAt531)
            {
                if (wall.Animator != null && wall.Animator.isActiveAndEnabled)
                {
                    wall.Animator.LateUpdate();
                    report.AppendLine($"  post-LateUpdate b531 coord={wall.ObstacleData.CustomCoordinate} " +
                        $"coreBounds={(wall.CoreRenderer == null ? "null" : wall.CoreRenderer.bounds.ToString())}");
                }
            }
            RenderPixelsAndSave(camera, Path.Combine(dir, "salty-square-postfix-b531-endframe.png"));
            foreach (var beat in new[] { 529.25f, 530f, 531.25f })
            {
                yield return SeekTo(beat);
                RenderPixelsAndSave(camera, Path.Combine(dir, $"salty-square-postfix-b{beat}-normal.png"));
            }

            var reportPath = Path.Combine(dir, "salty-square-b531-postfix-fullmap-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] square report: {reportPath}");
        }

        // TEMP phase discriminator: ObjectAnimator.Update() runs the track jump (NJS z travel)
        // while LateUpdate() applies Track.HoldDefinitePosition() + WorldPosition. The SeekTo sample
        // lands between them. This test samples fake@225 before and after ONE manual LateUpdate()
        // (the real production method, same frame stage) to prove whether the z drift/hold is a
        // real state or a mid-phase sampling artifact. No aggregator .Get() calls.
        [UnityTest]
        public IEnumerator FakeDefinitePositionAfterLateUpdateDiagnostics()
        {
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;

            var report = new StringBuilder();
            var dir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(dir);
            foreach (var beat in new[] { 219.5f, 220.25f, 221.5f })
            {
                yield return SeekTo(beat);
                report.AppendLine($"=== beat {beat} ===");
                var pair = FakePairNotes()
                    .Where(n => Mathf.Approximately(n.NoteData.JsonTime, 225f))
                    .OrderBy(n => n.NoteData.Type)
                    .ToList();
                foreach (var note in pair)
                {
                    var lod0 = note.GetComponentsInChildren<MeshRenderer>(true)
                        .FirstOrDefault(r => r.enabled && r.gameObject.activeInHierarchy
                            && r.name == "CubeNoteSmooth_LOD0");
                    var animator = note.Animator;
                    var parentZ = animator == null || animator.AnimationTrack == null
                        || animator.AnimationTrack.ObjectParentTransform == null
                        ? float.NaN
                        : animator.AnimationTrack.ObjectParentTransform.position.z;
                    void Log(string phase)
                    {
                        report.AppendLine($"  c{note.NoteData.Type} {phase} " +
                            $"animatorEnabled={animator.enabled}/{animator.isActiveAndEnabled} " +
                            $"worldPosCount={animator.WorldPosition.Count} offsetCount={animator.OffsetPosition.Count} " +
                            $"trackParentZ={parentZ:F2} " +
                            $"containerLocalZ={note.transform.localPosition.z:F2} " +
                            $"containerWorld={note.transform.position} " +
                            $"lod0Center={(lod0 == null ? "null" : lod0.bounds.center.ToString())}");
                    }
                    Log("pre-LateUpdate ");
                }
                RenderPixelsAndSave(camera, Path.Combine(dir, $"salty-fake-final-phase-b{beat}-pre.png"));
                foreach (var note in pair)
                {
                    var lod0 = note.GetComponentsInChildren<MeshRenderer>(true)
                        .FirstOrDefault(r => r.enabled && r.gameObject.activeInHierarchy
                            && r.name == "CubeNoteSmooth_LOD0");
                    var animator = note.Animator;
                    animator.LateUpdate();
                    var parentZ = animator.AnimationTrack == null
                        || animator.AnimationTrack.ObjectParentTransform == null
                        ? float.NaN
                        : animator.AnimationTrack.ObjectParentTransform.position.z;
                    report.AppendLine($"  c{note.NoteData.Type} post-LateUpdate " +
                        $"trackParentZ={parentZ:F2} " +
                        $"containerLocalZ={note.transform.localPosition.z:F2} " +
                        $"containerWorld={note.transform.position} " +
                        $"lod0Center={(lod0 == null ? "null" : lod0.bounds.center.ToString())}");
                }
                RenderPixelsAndSave(camera, Path.Combine(dir, $"salty-fake-final-phase-b{beat}-post.png"));
            }
            // Post-fix wall-depth capture at the same beat as the pre-fix salty-pos-beat222.25.png.
            yield return SeekTo(222.25f);
            RenderPixelsAndSave(camera, Path.Combine(dir, "salty-pos-final-b222.25.png"));
            var reportPath = Path.Combine(dir, "salty-fake-phase-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] phase report: {reportPath}");
        }

        // TEMP diagnostic (no assertions on parity): the deployed app reports the b221-225 fake
        // red/white walls alternating giant/normal screen size while scrubbing. This samples each
        // of the 9 b221 fake walls at three frame phases per seek — immediately after
        // MoveToJsonTime (OnTimeChanged synchronous), after one yielded frame (Unity Update ran),
        // and after one manual Animator.LateUpdate per wall (real render-phase state) — to
        // discriminate a mid-phase sampling artifact from a real geometry regression.
        // No aggregator .Get() calls (they consume Count).
        [UnityTest]
        public IEnumerator WallScrubImmediateVersusFrameDiagnostics()
        {
            // FOV90/offset0 matches the user's live screenshots; the shared settings restore across
            // methods would otherwise leave these frames at FOV60 and not comparable.
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.Animations = true;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null;
            var cameraController = cameraManager.CameraControllers[1];
            var camera = cameraController.Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var report = new StringBuilder();
            var dir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(dir);

            List<Beatmap.Containers.ObstacleContainer> Beat221Walls() =>
                Object.FindAnyObjectByType<ObstacleGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && Mathf.Approximately(c.ObstacleData.JsonTime, 221f))
                    .ToList();

            TrackAnimator FindTrackAnimator(string trackName) =>
                Object.FindObjectsByType<TrackAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.gameObject.name == trackName);

            MeshRenderer OutlineOf(Beatmap.Containers.ObstacleContainer wall) =>
                wall.OutlineTransform == null ? null
                    : wall.OutlineTransform.GetComponentInChildren<MeshRenderer>(true);

            void LogPhase(float beat, string phase)
            {
                report.AppendLine($"=== beat {beat} phase={phase} ===");
                var walls = Beat221Walls();
                report.AppendLine($"b221 fake walls loaded: {walls.Count}");
                foreach (var wall in walls)
                {
                    var animator = wall.Animator;
                    var outline = OutlineOf(wall);
                    report.AppendLine($"  wall track={wall.ObstacleData.CustomTrack} " +
                        $"coreCenterZ={(wall.CoreRenderer == null ? "null" : wall.CoreRenderer.bounds.center.z.ToString("F3"))} " +
                        $"coreSizeX={(wall.CoreRenderer == null ? "null" : wall.CoreRenderer.bounds.size.x.ToString("F3"))} " +
                        $"coreBounds={(wall.CoreRenderer == null ? "null" : wall.CoreRenderer.bounds.ToString())} " +
                        $"outlineBounds={(outline == null ? "null" : outline.bounds.ToString())} " +
                        $"animatorEnabled={(animator == null ? "no-animator" : animator.enabled.ToString())} " +
                        $"targetType={(animator == null ? "-" : animator.TargetType.ToString())} " +
                        $"offsetPosCount={(animator == null ? "-" : animator.OffsetPosition.Count.ToString())}");
                }
                var beat0 = FindTrackAnimator("beat0");
                var quarter = FindTrackAnimator("1/4");
                report.AppendLine($"  track beat0 CachedChildren={(beat0 == null ? "no-track" : beat0.CachedChildren.Length.ToString())} " +
                    $"track 1/4 CachedChildren={(quarter == null ? "no-track" : quarter.CachedChildren.Length.ToString())}");
                var ct = camera.transform;
                report.AppendLine($"  playing camera world={ct.position} local={ct.localPosition} " +
                    $"parent={(ct.parent == null ? "null" : ct.parent.name)} fov={camera.fieldOfView} " +
                    $"animMode={UIMode.AnimationMode} jsonBeat={atsc.CurrentJsonTime} songBeat={atsc.CurrentSongBpmTime}");
            }

            var scrubs = new[] { 220.75f, 221.1f, 222.25f, 223f, 222.25f, 220.75f, 222.25f, 221.5f, 222.25f };
            var visit222 = 0;
            foreach (var beat in scrubs)
            {
                atsc.MoveToJsonTime(beat);
                LogPhase(beat, "immediate");
                var is222 = Mathf.Approximately(beat, 222.25f);
                if (is222)
                {
                    visit222++;
                    RenderPixelsAndSave(camera,
                        Path.Combine(dir, $"wall-scrub-fixed90-b222.25-v{visit222}-immediate.png"));
                }
                yield return null;
                LogPhase(beat, "post-frame");
                foreach (var wall in Beat221Walls())
                {
                    if (wall.Animator != null && wall.Animator.isActiveAndEnabled)
                        wall.Animator.LateUpdate();
                }
                LogPhase(beat, "post-LateUpdate");
                if (is222)
                    RenderPixelsAndSave(camera,
                        Path.Combine(dir, $"wall-scrub-fixed90-b222.25-v{visit222}-post.png"));
            }

            // Optional: Playing -> Normal/Editing -> Playing transition at b222.25 (same map load).
            report.AppendLine("=== mode transition Normal/Edit -> Playing at 222.25 ===");
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            atsc.MoveToJsonTime(222.25f);
            LogPhase(222.25f, "transition-immediate");
            yield return null;
            LogPhase(222.25f, "transition-post-frame");
            foreach (var wall in Beat221Walls())
            {
                if (wall.Animator != null && wall.Animator.isActiveAndEnabled)
                    wall.Animator.LateUpdate();
            }
            LogPhase(222.25f, "transition-post-LateUpdate");
            RenderPixelsAndSave(camera, Path.Combine(dir, "wall-scrub-fixed90-b222.25-transition-post.png"));

            var reportPath = Path.Combine(dir, "salty-wall-scrub-fixed90-phase-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] wall scrub phase report: {reportPath}");
        }

        // TEMP diagnostic (no assertions): the deployed app reports the b537 `asdkm` Head camera
        // AnimateTrack (z0 -> z-3 over 8 beats, easeOutSine) stuck/left behind after rewind or
        // mode switches. This logs the playing camera rig + reflected cameraAnimator/currentTrack
        // around the b537 window, across a jump to b222.25 and back, and across a
        // Playing -> Normal/Edit -> Preview/Edit -> Playing mode cycle. The camera's ObjectAnimator
        // is intentionally NOT ticked manually — production drives it.
        [UnityTest]
        public IEnumerator HeadCameraBeat537ScrubDiagnostics()
        {
            yield return EnterPlayingFromBasicEventWorkspace();
            var cameraController = cameraManager.CameraControllers[1];
            var camera = cameraController.Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var report = new StringBuilder();
            var dir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(dir);

            var animatorField = typeof(CameraController).GetField("cameraAnimator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var currentTrackField = typeof(CameraController).GetField("currentTrack",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            string HierarchyPath(Transform t)
            {
                var sb = new StringBuilder(t.name);
                while (t.parent != null)
                {
                    t = t.parent;
                    sb.Insert(0, t.name + "/");
                }
                return sb.ToString();
            }

            void LogPhase(float beat, string phase)
            {
                var t = cameraController.transform;
                var animator = animatorField.GetValue(cameraController) as Beatmap.Animations.ObjectAnimator;
                var track = currentTrackField.GetValue(cameraController) as TrackAnimator;
                report.AppendLine($"=== beat {beat} phase={phase} ===");
                report.AppendLine($"  controller world={t.position} local={t.localPosition} " +
                    $"rot={t.eulerAngles} localRot={t.localEulerAngles}");
                report.AppendLine($"  camera world={camera.transform.position} " +
                    $"local={camera.transform.localPosition} rot={camera.transform.eulerAngles} " +
                    $"path={HierarchyPath(camera.transform)} fov={camera.fieldOfView}");
                report.AppendLine($"  animMode={UIMode.AnimationMode} jsonBeat={atsc.CurrentJsonTime} " +
                    $"songBeat={atsc.CurrentSongBpmTime} " +
                    $"currentTrack={(track == null ? "null" : track.gameObject.name)} " +
                    $"cameraAnimatorEnabled={(animator == null ? "no-field" : animator.enabled.ToString())} " +
                    $"worldPosCount={(animator == null ? "-" : animator.WorldPosition.Count.ToString())} " +
                    $"offsetPosCount={(animator == null ? "-" : animator.OffsetPosition.Count.ToString())} " +
                    $"animatorTrack={(animator == null || animator.AnimationTrack == null ? "null" : animator.AnimationTrack.gameObject.name)}");
            }

            var scrubs = new[]
                { 536.5f, 537f, 537.25f, 538f, 539f, 541f, 543f, 545f, 541f, 536.5f, 222.25f, 537.5f, 536.5f };
            foreach (var beat in scrubs)
            {
                atsc.MoveToJsonTime(beat);
                LogPhase(beat, "immediate");
                yield return null;
                LogPhase(beat, "post-frame1");
                yield return null;
                LogPhase(beat, "post-frame2");
            }

            // Mode cycle without map reload: Playing -> Normal/Edit -> Preview/Edit -> Playing.
            report.AppendLine("=== mode cycle at 536.5 ===");
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            atsc.MoveToJsonTime(536.5f);
            LogPhase(536.5f, "cycle-immediate");
            yield return null;
            LogPhase(536.5f, "cycle-post-frame1");
            yield return null;
            LogPhase(536.5f, "cycle-post-frame2");

            var reportPath = Path.Combine(dir, "salty-camera537-fixed-scrub-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] camera537 scrub report: {reportPath}");
        }

        // Regression: the deployed app shows the b221-225 fake walls flickering ~3x screen size
        // while scrubbing. Diagnostics (salty-wall-scrub-phase-report.txt) show ObjectAnimator.Update
        // leaves the walls at un-offset jump depth (core z ~0.755-2.755) and only LateUpdate applies
        // the +6/+6.6 track offset (red z=9.355, white z=8.755). A correctly-seeking frame must land
        // on the end-of-frame state immediately after MoveToJsonTime; this test never ticks
        // Animator manually — production is expected to drive the settled state.
        [UnityTest]
        public IEnumerator Beat221WallsSeekSynchronouslyToStableGameDepth()
        {
            // UnityTearDown restores the user's settings after each test in this fixture while
            // OnMapLoaded only ran once; re-apply the FOV90/offset0 setup per test.
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            List<Beatmap.Containers.ObstacleContainer> Beat221Walls() =>
                Object.FindAnyObjectByType<ObstacleGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && Mathf.Approximately(c.ObstacleData.JsonTime, 221f))
                    .ToList();

            void Sample(float beat, string phase)
            {
                if (beat < 221f) return; // crossing trigger only, no wall-depth assertion
                var walls = Beat221Walls();
                var red = walls.Where(c => c.ObstacleData.CustomTrack is JSONString s
                    && s.Value.StartsWith("beat")).ToList();
                var white = walls.FirstOrDefault(c => c.ObstacleData.CustomTrack is JSONArray arr
                    && arr.Children.Any(v => (string)v == "1/4" || (string)v == "pee2"));
                if (red.Count != 8)
                {
                    failures.Add($"beat {beat} {phase}: {red.Count} red beat walls loaded, expected 8.");
                    return;
                }
                if (white == null)
                {
                    failures.Add($"beat {beat} {phase}: white [1/4,pee2] wall not loaded.");
                    return;
                }
                var camPos = camera.transform.position;
                if (Vector3.Distance(camPos, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                    failures.Add($"beat {beat} {phase}: playing camera world={camPos}, expected ~(0,1.65,0).");
                if (Mathf.Abs(camera.fieldOfView - 90f) > 0.5f)
                    failures.Add($"beat {beat} {phase}: playing camera fov={camera.fieldOfView}, expected 90.");
                foreach (var wall in red.Concat(new[] { white }))
                {
                    var isWhite = ReferenceEquals(wall, white);
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

            // Same-map mode cycle: Normal/Edit -> Playing, then the same immediate-depth check.
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            atsc.MoveToJsonTime(222.25f);
            Sample(222.25f, "transition-immediate");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Regression: authored asdkm (Head) AnimateTrack at b537 eases the camera z0 -> -3 over
        // 8 beats with easeOutSine. Diagnostics show the seek lands one beat behind (the camera
        // keeps the previous frame's z immediately after MoveToJsonTime) and the camera world y
        // jumps 1.65 -> 2.25 when the rig binds under the track parent. The seek must place the
        // camera on the authored curve synchronously.
        [UnityTest]
        public IEnumerator Beat537HeadCameraSeeksToAuthoredEaseWithoutLag()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null; // let CameraController.Update apply the FOV before sampling
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            var pristine = camera.transform.position;
            if (Vector3.Distance(pristine, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                failures.Add($"setup: pristine playing camera world={pristine}, expected ~(0,1.65,0).");

            void Sample(float beat, string phase)
            {
                var expectedZ = -3f * Mathf.Sin(Mathf.Clamp01((beat - 537f) / 8f) * Mathf.PI / 2f);
                var pos = camera.transform.position;
                if (Mathf.Abs(pos.z - expectedZ) > 0.07f)
                    failures.Add($"beat {beat} {phase}: camera z={pos.z:F3}, expected {expectedZ:F3} " +
                        "(authored easeOutSine z0->-3 over 8 beats).");
                if (Mathf.Abs(pos.x) > 0.02f)
                    failures.Add($"beat {beat} {phase}: camera x={pos.x:F3}, expected ~0.");
                if (Mathf.Abs(pos.y - 1.65f) > 0.05f)
                    failures.Add($"beat {beat} {phase}: camera y={pos.y:F3}, expected ~1.65 " +
                        "(map authors no y motion; binding must not lift the camera).");
                if (Mathf.Abs(camera.fieldOfView - 90f) > 0.5f)
                    failures.Add($"beat {beat} {phase}: camera fov={camera.fieldOfView}, expected 90.");
            }

            foreach (var beat in new[] { 537f, 537.25f, 538f, 539f, 541f, 543f, 545f, 541f, 537.5f })
            {
                atsc.MoveToJsonTime(beat);
                Sample(beat, "immediate");
                yield return null;
                Sample(beat, "post-frame");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Regression: rewinding before b537 (or to b222.25) disconnects the camera animator but
        // leaves the rig parented under the asdkm track at the last animated pose (z~-2.12), and a
        // Playing->Normal/Edit->Preview/Edit->Playing cycle does not recover it. Disconnect must
        // restore the pre-bind playing pose and parent.
        [UnityTest]
        public IEnumerator Beat537CameraRewindAndModeCycleRestoresPlayingPose()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null; // let CameraController.Update apply the FOV before sampling
            var cameraController = cameraManager.CameraControllers[1];
            var camera = cameraController.Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            var animatorField = typeof(CameraController).GetField("cameraAnimator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cameraAnimator = animatorField.GetValue(cameraController) as ObjectAnimator;
            Assert.That(cameraAnimator, Is.Not.Null, "cameraAnimator field missing on CameraController.");

            var pristinePos = camera.transform.position;
            var pristineRigParent = cameraAnimator.transform.parent;

            void RequirePristine(string phase)
            {
                var pos = camera.transform.position;
                if (Vector3.Distance(pos, pristinePos) > 0.05f)
                    failures.Add($"{phase}: camera world={pos}, expected pristine {pristinePos}.");
                var parent = cameraAnimator.transform.parent;
                if (!ReferenceEquals(parent, pristineRigParent))
                    failures.Add($"{phase}: camera rig parent={HierarchyName(parent)}, expected " +
                        $"{HierarchyName(pristineRigParent)} (rig must leave the asdkm track).");
            }

            string HierarchyName(Transform t) => t == null ? "null" : t.name;

            // Drive onto the authored curve first so the disconnect has something to undo.
            atsc.MoveToJsonTime(541f);
            yield return null;
            yield return null;
            var z541 = camera.transform.position.z;
            if (Mathf.Abs(z541 - -2.121f) > 0.08f)
                failures.Add($"beat 541: camera z={z541:F3}, expected ~-2.121 on the authored ease.");

            foreach (var beat in new[] { 536.5f, 222.25f })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                RequirePristine($"rewind to {beat}");
            }

            // Mode cycle without map reload, still sitting before the b537 bind beat.
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            atsc.MoveToJsonTime(536.5f);
            yield return null;
            yield return null;
            RequirePristine("mode cycle at 536.5");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // TEMP diagnostic: Beat221WallsSeekSynchronouslyToStableGameDepth is RED because the walls
        // sit at un-offset jump depth (z~2.755) even post-frame, while one manual Animator.LateUpdate
        // lands them at game depth (z9.355/8.755). This discriminates whether the seek-time
        // OnTimeChanged callback ran too early (before TrackAnimator pushed the track offset) vs
        // never re-ran: invoking the compiled private callback once more right after the seek and
        // re-reading bounds separates "callback already ran with correct data" from "missing push".
        // No aggregator .Get() calls (they consume Count).
        [UnityTest]
        public IEnumerator WallHeldOffsetCallbackOrderDiagnostics()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var report = new StringBuilder();
            var dir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(dir);

            var onTimeChanged = typeof(ObjectAnimator).GetMethod("OnTimeChanged",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(onTimeChanged, Is.Not.Null, "ObjectAnimator.OnTimeChanged not found via reflection.");

            List<Beatmap.Containers.ObstacleContainer> Selected() =>
                Object.FindAnyObjectByType<ObstacleGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && Mathf.Approximately(c.ObstacleData.JsonTime, 221f)
                        && (c.ObstacleData.CustomTrack is JSONString s && s.Value == "beat0"
                            || c.ObstacleData.CustomTrack is JSONArray arr
                                && arr.Children.Any(v => (string)v == "1/4")))
                    .ToList();

            TrackAnimator FindTrackAnimator(string trackName) =>
                Object.FindObjectsByType<TrackAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.gameObject.name == trackName);

            void LogState(float beat, string phase)
            {
                report.AppendLine($"=== beat {beat} phase={phase} jsonBeat={atsc.CurrentJsonTime} " +
                    $"songBeat={atsc.CurrentSongBpmTime} ===");
                var walls = Selected();
                report.AppendLine($"selected walls: {walls.Count}");
                foreach (var wall in walls)
                {
                    var animator = wall.Animator;
                    var track = animator == null ? null : animator.AnimationTrack;
                    report.AppendLine($"  wall track={wall.ObstacleData.CustomTrack} " +
                        $"animatorActive={(animator == null ? "no-animator" : animator.isActiveAndEnabled.ToString())} " +
                        $"offsetPosCount={(animator == null ? "-" : animator.OffsetPosition.Count.ToString())} " +
                        $"localTargetPos={(animator == null || animator.LocalTarget == null ? "null" : animator.LocalTarget.localPosition.ToString())} " +
                        $"trackParentLocalPos={(track == null || track.ObjectParentTransform == null ? "null" : track.ObjectParentTransform.localPosition.ToString())} " +
                        $"coreCenterZ={(wall.CoreRenderer == null ? "null" : wall.CoreRenderer.bounds.center.z.ToString("F3"))}");
                }
                var beat0 = FindTrackAnimator("beat0");
                var quarter = FindTrackAnimator("1/4");
                report.AppendLine($"  beat0 CachedChildren={(beat0 == null ? "no-track" : beat0.CachedChildren.Length.ToString())} " +
                    $"1/4 CachedChildren={(quarter == null ? "no-track" : quarter.CachedChildren.Length.ToString())} " +
                    $"camera world={camera.transform.position} fov={camera.fieldOfView}");
            }

            foreach (var beat in new[] { 221.1f, 222.25f, 223f, 222.25f })
            {
                atsc.MoveToJsonTime(beat);
                LogState(beat, "immediate");
                var walls = Selected();
                foreach (var wall in walls)
                {
                    if (wall.Animator != null && wall.Animator.isActiveAndEnabled)
                        onTimeChanged.Invoke(wall.Animator, null);
                }
                LogState(beat, "post-OnTimeChanged-invoke");
                yield return null;
                LogState(beat, "post-frame-pre-LateUpdate");
                foreach (var wall in walls)
                {
                    if (wall.Animator != null && wall.Animator.isActiveAndEnabled)
                        wall.Animator.LateUpdate();
                }
                LogState(beat, "post-manual-LateUpdate");
            }

            var reportPath = Path.Combine(dir, "salty-wall-held-callback-order-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] wall held-offset callback order report: {reportPath}");
        }

        // TEMP discriminator: ObstacleGridContainer.OnTimeChanged recycles every wall container on
        // each stopped-time seek, and the new ObjectAnimator attaches AFTER the OnTimeChangedEarly
        // TrackAnimator push already fired — so the fresh wall never receives the track's held
        // OffsetPosition until the next frame (measured: Count goes 1 -> 2 -> consumed by LateUpdate).
        // This proves the minimal fix seam: PushToChild(newChild) + one OnTimeChanged re-apply lands
        // the wall at game depth (z9.355/8.755) synchronously inside the seek frame.
        // No aggregator .Get() calls (they consume Count).
        [UnityTest]
        public IEnumerator WallNewChildHeldPushThenApplyDiagnostics()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var manager = Object.FindAnyObjectByType<TracksManager>();
            var report = new StringBuilder();
            var dir = "C:/Users/tdrak/AppData/Local/Temp/devin-salty-diag";
            Directory.CreateDirectory(dir);

            var onTimeChanged = typeof(ObjectAnimator).GetMethod("OnTimeChanged",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(onTimeChanged, Is.Not.Null, "ObjectAnimator.OnTimeChanged not found via reflection.");
            var beat0Track = manager.GetAnimationTrack("beat0");
            var quarterTrack = manager.GetAnimationTrack("1/4");
            var pee2Track = manager.GetAnimationTrack("pee2");
            Assert.That(beat0Track, Is.Not.Null, "beat0 TrackAnimator missing.");
            Assert.That(quarterTrack, Is.Not.Null, "1/4 TrackAnimator missing.");
            Assert.That(pee2Track, Is.Not.Null, "pee2 TrackAnimator missing.");

            List<Beatmap.Containers.ObstacleContainer> Selected() =>
                Object.FindAnyObjectByType<ObstacleGridContainer>()
                    .LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null && Mathf.Approximately(c.ObstacleData.JsonTime, 221f)
                        && (c.ObstacleData.CustomTrack is JSONString s && s.Value == "beat0"
                            || c.ObstacleData.CustomTrack is JSONArray arr
                                && arr.Children.Any(v => (string)v == "1/4")))
                    .ToList();

            void LogTrackState(string label)
            {
                foreach (var (name, ta) in new[] { ("beat0", beat0Track), ("1/4", quarterTrack), ("pee2", pee2Track) })
                    report.AppendLine($"  track {name} {label}: enabled={ta.enabled} " +
                        $"active={ta.isActiveAndEnabled} CachedChildren={ta.CachedChildren.Length}");
            }

            var tracksField = typeof(ObjectAnimator).GetField("tracks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            void LogWall(string label, Beatmap.Containers.ObstacleContainer wall)
            {
                var animator = wall.Animator;
                var tracksList = animator == null ? null
                    : tracksField.GetValue(animator) as List<TrackAnimator>;
                report.AppendLine($"  wall track={wall.ObstacleData.CustomTrack} {label} " +
                    $"goId={wall.gameObject.GetInstanceID()} " +
                    $"animatorEnabled={(animator == null ? "-" : animator.enabled.ToString())} " +
                    $"tracks={(tracksList == null ? "-" : string.Join("+", tracksList.Select(t => t == null ? "null" : t.gameObject.name)))} " +
                    $"offsetCount={(animator == null ? "-" : animator.OffsetPosition.Count.ToString())} " +
                    $"offsetKeep={(animator == null ? "-" : animator.OffsetPosition.Keep.ToString())} " +
                    $"localPosCount={(animator == null ? "-" : animator.LocalPosition.Count.ToString())} " +
                    $"localPosKeep={(animator == null ? "-" : animator.LocalPosition.Keep.ToString())} " +
                    $"worldPosCount={(animator == null ? "-" : animator.WorldPosition.Count.ToString())} " +
                    $"scaleCount={(animator == null ? "-" : animator.Scale.Count.ToString())} " +
                    $"localTargetPos={(animator == null || animator.LocalTarget == null ? "null" : animator.LocalTarget.localPosition.ToString())} " +
                    $"localTarget={(animator == null || animator.LocalTarget == null ? "null" : animator.LocalTarget.name)} " +
                    $"coreZ={(wall.CoreRenderer == null ? "null" : wall.CoreRenderer.bounds.center.z.ToString("F3"))} " +
                    $"coreWidthX={(wall.CoreRenderer == null ? "null" : wall.CoreRenderer.bounds.size.x.ToString("F3"))}");
            }

            foreach (var beat in new[] { 221.1f, 222.25f, 223f, 222.25f })
            {
                atsc.MoveToJsonTime(beat);
                report.AppendLine($"=== beat {beat} jsonBeat={atsc.CurrentJsonTime} ===");
                LogTrackState("post-seek");
                var walls = Selected();
                Assert.That(walls, Has.Count.EqualTo(2), $"beat {beat}: expected beat0 + [1/4,pee2] walls.");
                foreach (var wall in walls) LogWall("post-seek", wall);

                foreach (var wall in walls)
                {
                    var isWhite = wall.ObstacleData.CustomTrack is JSONArray;
                    if (wall.Animator == null || !wall.Animator.isActiveAndEnabled)
                    {
                        report.AppendLine($"  wall track={wall.ObstacleData.CustomTrack} animator inactive, skipped");
                        continue;
                    }
                    if (isWhite)
                    {
                        quarterTrack.PushToChild(wall.Animator);
                        pee2Track.PushToChild(wall.Animator);
                    }
                    else
                    {
                        beat0Track.PushToChild(wall.Animator);
                    }
                    LogWall("post-PushToChild", wall);
                    onTimeChanged.Invoke(wall.Animator, null);
                    LogWall("post-OnTimeChanged", wall);
                }
            }

            report.AppendLine($"camera world={camera.transform.position} fov={camera.fieldOfView}");
            var reportPath = Path.Combine(dir, "salty-wall-held-push-apply-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[SaltyDiag] wall held push+apply report: {reportPath}");
        }

        private static void RenderPixelsAndSave(Camera camera, string path)
        {
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            var rt = new RenderTexture(1024, 576, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(1024, 576, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
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

        // Diagnostic for EarlyFakePairsUseNoodleLaneDistance's loaded-2-of-10 count: enumerate the
        // map's authored fake-pair candidates with spawn timing and LoadedContainers membership,
        // then the first notes past `time + offset` that fail the animation lookahead. The
        // recursive spawn walk returns at the first ineligible object, so one blocked note stalls
        // the whole tail even when later pairs would qualify.
        private static void LogFakePairSpawnDiagnostics()
        {
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var time = atsc.CurrentSongBpmTime;
            var offset = noteGrid.SpawnCallbackController.Offset;
            Debug.Log($"[SaltyDiag] fakepair spawn: CurrentSongBpmTime={time:F3} " +
                $"audioBeats={atsc.CurrentAudioBeats:F3} isPlaying={atsc.IsPlaying} " +
                $"spawnOffset={offset:F3} JUMP_TIME={Track.JUMP_TIME}");

            var map = BeatSaberSongContainer.Instance.Map;
            var candidates = map.Notes
                .Where(n => n.CustomFake
                    && n.JsonTime is >= 225f and <= 229f
                    && n.CustomTrack is JSONArray arr
                    && arr.Children.Any(v => (string)v == $"slay{(int)n.JsonTime}")
                    && arr.Children.Any(v => (string)v == $"shitballs{(int)n.JsonTime}"))
                .OrderBy(n => n.SongBpmTime)
                .ToList();
            Debug.Log($"[SaltyDiag] fakepair authored candidates={candidates.Count}");
            foreach (var n in candidates)
            {
                var eligible = n.SongBpmTime
                    <= time + Mathf.Max(n.HalfJumpDuration, offset) + Track.JUMP_TIME;
                Debug.Log($"[SaltyDiag]   candidate b={n.JsonTime} type={n.Type} " +
                    $"bpmTime={n.SongBpmTime:F3} hjd={n.HalfJumpDuration:F3} " +
                    $"spawnBpm={n.SpawnSongBpmTime:F3} " +
                    $"njs={(n.CustomNoteJumpMovementSpeed == null ? "none" : n.CustomNoteJumpMovementSpeed.AsFloat.ToString("F2"))} " +
                    $"jumpOffset={(n.CustomNoteJumpStartBeatOffset == null ? "none" : n.CustomNoteJumpStartBeatOffset.AsFloat.ToString("F3"))} " +
                    $"eligible={eligible} loaded={noteGrid.LoadedContainers.ContainsKey(n)}");
            }

            var ineligible = map.Notes
                .Where(n => n.SongBpmTime > time + offset
                    && n.SongBpmTime > time + Mathf.Max(n.HalfJumpDuration, offset) + Track.JUMP_TIME)
                .OrderBy(n => n.SongBpmTime)
                .Take(8)
                .ToList();
            Debug.Log($"[SaltyDiag] first ineligible notes past time+offset (showing {ineligible.Count})");
            foreach (var n in ineligible)
            {
                var needed = time + Mathf.Max(n.HalfJumpDuration, offset) + Track.JUMP_TIME;
                Debug.Log($"[SaltyDiag]   ineligible b={n.JsonTime} bpmTime={n.SongBpmTime:F3} " +
                    $"hjd={n.HalfJumpDuration:F3} spawnBpm={n.SpawnSongBpmTime:F3} " +
                    $"fake={n.CustomFake} track={n.CustomTrack} " +
                    $"needs bpmTime<={needed:F3} (miss by {n.SongBpmTime - needed:F3})");
            }
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

        // User report: after repeated scrubs the b221 Noodle fake walls are missing even though
        // continuous playback shows them. Matched-time parity: drive deterministic production
        // playback (audio stopped, CurrentSeconds stepped <=0.5 beat per rendered frame) from
        // b216 through the lead-in at 219.5 into 221.25/223.25, snapshot the nine authored b221
        // fake walls (tracks beat0..beat7 plus [1/4,pee2]) at the ACTUAL sampled CurrentJsonTime,
        // then compare against stopped seeks to that exact beat approached repeatedly from behind
        // and ahead, and against the first frame after resume. Runs in Playing and Preview so the
        // editing-camera path is covered with runtime renderer geometry, not UI affordances.
        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
        private static readonly int cutoutPropertyId = Shader.PropertyToID("_Cutout");

        [UnityTest]
        public IEnumerator Beat221FakeWallsMatchPlaybackPoseAfterRepeatedScrubs()
        {
            // Batchmode has no input devices; UIMode.OnPlayToggle -> SetLockState reads
            // Mouse.current when toggling playback in Playing mode. A virtual mouse keeps the
            // production toggle path intact without touching production code.
            var testMouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var obstacleGrid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(obstacleGrid, Is.Not.Null, "ObstacleGridContainer missing.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "CurrentSeconds property not found.");

            var authoredWalls = BeatSaberSongContainer.Instance.Map.Obstacles
                .Where(o => o.CustomFake && Mathf.Approximately(o.JsonTime, 221f))
                .ToList();
            Assert.That(authoredWalls.Count, Is.EqualTo(9),
                $"expected 9 authored b221 fake walls, found {authoredWalls.Count}: " +
                string.Join("; ", authoredWalls.Select(DescribeObstacle)));
            foreach (var o in authoredWalls)
            {
                Debug.Log($"[WallDiag] authored {DescribeObstacle(o)} " +
                    $"songBpm={o.SongBpmTime:F3} hjd={o.HalfJumpDuration:F3} " +
                    $"spawnBpm={o.SpawnSongBpmTime:F3} dur={o.Duration}");
            }

            var failures = new List<string>();
            var sampleTargets = new[] { 219.5f, 221.25f, 223.25f };
            try
            {
                foreach (var preview in new[] { false, true })
                {
                    var modeName = preview ? "Preview" : "Playing";
                    if (preview)
                    {
                        // Preview must inherit the Gameplay workspace, not BasicEvent: with a
                        // basic-event workspace the gameplay obstacle collection stays disabled
                        // and every b221 wall is inactive, which made the first Preview baseline
                        // vacuous (all active=False/animator=False). Leaving Playing restores
                        // editingModeBeforePlaying inside SetUIMode, so Gameplay must be applied
                        // AFTER the mode switch.
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

                    // Deterministic production playback from b216 in <=0.5-beat rendered steps.
                    atsc.MoveToJsonTime(216f);
                    atsc.TogglePlaying();
                    atsc.SongAudioSource.Stop();
                    atsc.StopScheduled = true;
                    yield return null; yield return null;
                    if (!atsc.IsPlaying)
                    {
                        failures.Add($"{modeName}: deterministic playback did not start.");
                        continue;
                    }

                    var playbackSnapshots =
                        new Dictionary<float, (float beat, Dictionary<Beatmap.Base.BaseObject, WallSnapshot> snap)>();
                    var nextSample = 0;
                    for (var t = 216.5f; t <= 223.5f && nextSample < sampleTargets.Length; t += 0.5f)
                    {
                        var target = Mathf.Min(t, sampleTargets[nextSample]);
                        SetCurrentSeconds(atsc, target);
                        yield return null; yield return null;
                        if (atsc.CurrentJsonTime < sampleTargets[nextSample] - 0.001f) continue;
                        var sampled = atsc.CurrentJsonTime;
                        var snap = CaptureWalls(obstacleGrid, authoredWalls);
                        playbackSnapshots[sampleTargets[nextSample]] = (sampled, snap);
                        LogWallSnapshot(obstacleGrid, authoredWalls, $"{modeName} playback@{sampled:F3}");
                        // Non-vacuous baseline per sampled beat: once the sample is inside the
                        // authored wall window every b221 wall is due (spawnBpm ~216.7) and must
                        // be an active, animated, enabled wall; the lead-in sample only requires
                        // the walls to be loaded (they may legitimately sit far/inactive early).
                        var sampledSongBpm =
                            (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(sampled);
                        var due = authoredWalls.Count(o =>
                            o.SpawnSongBpmTime <= sampledSongBpm);
                        var loaded = snap.Values.Count(s => s.loaded);
                        var activeAnimated = snap.Values.Count(s =>
                            s.loaded && s.active && s.coreEnabled && s.animatorEnabled);
                        if (loaded != due)
                            failures.Add($"{modeName} playback@{sampled:F3}: {loaded}/{due} due " +
                                "b221 walls loaded during continuous playback.");
                        if (sampled >= 221f && activeAnimated != due)
                            failures.Add($"{modeName} playback@{sampled:F3}: {activeAnimated}/{due} " +
                                "due b221 walls active+enabled+animated during continuous playback.");
                        nextSample++;
                    }
                    atsc.TogglePlaying();
                    yield return null;
                    Assert.That(nextSample, Is.EqualTo(sampleTargets.Length),
                        $"{modeName}: playback stepping missed sample targets.");

                    foreach (var pair in playbackSnapshots)
                    {
                        var sampled = pair.Value.beat;
                        var expected = pair.Value.snap;

                        // 3 reverse+forward scrub cycles to the exact sampled beat.
                        for (var cycle = 0; cycle < 3; ++cycle)
                        {
                            atsc.MoveToJsonTime(Mathf.Max(0f, sampled - 6f));
                            yield return null;
                            atsc.MoveToJsonTime(sampled + 4f);
                            yield return null;
                            atsc.MoveToJsonTime(sampled);
                            yield return null; yield return null;
                            CompareWallSnapshots(expected, CaptureWalls(obstacleGrid, authoredWalls),
                                $"{modeName} seek-cycle{cycle}@{sampled:F3}", failures);
                        }

                        // First frame after resume: the toggle applies an audio-latency offset
                        // to CurrentSeconds, so compare the resumed frame against a stopped seek
                        // to the ACTUAL resumed beat rather than the older sampled snapshot.
                        atsc.TogglePlaying();
                        yield return null; yield return null;
                        var resumedBeat = atsc.CurrentJsonTime;
                        var resumedSnap = CaptureWalls(obstacleGrid, authoredWalls);
                        atsc.TogglePlaying();
                        yield return null;
                        atsc.MoveToJsonTime(resumedBeat);
                        yield return null; yield return null;
                        CompareWallSnapshots(CaptureWalls(obstacleGrid, authoredWalls), resumedSnap,
                            $"{modeName} resumed@{resumedBeat:F3} vs seek", failures);
                    }
                }
            }
            finally
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
                UnityEngine.InputSystem.InputSystem.RemoveDevice(testMouse);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Six authored b217 fake walls on track "1": dissolve hidden at b0, shown by the b217
        // AnimateTrack (dissolve 0->1 d1), hidden again by the b321 event (dissolve 1->0 d1).
        // Matched-time parity samples before/during show and after the hide.
        [UnityTest]
        public IEnumerator Beat217FakeWallsMatchPlaybackShowHideAfterScrubs()
        {
            var authored = BeatSaberSongContainer.Instance.Map.Obstacles
                .Where(o => o.CustomFake && Mathf.Approximately(o.JsonTime, 217f))
                .ToList();
            Assert.That(authored.Count, Is.EqualTo(6),
                $"expected 6 authored b217 fake walls, found {authored.Count}: " +
                string.Join("; ", authored.Select(DescribeObstacle)));
            yield return RunFakeWallGroupParity("b217", authored, 213f,
                new[] { 216.5f, 217.5f, 322.25f }, new HashSet<float> { 217.5f });
        }

        // Eight authored b337 fake walls on tracks uh/uh2/0: hidden at b0, shown by the shared
        // b337 event (dissolve 0->1 d1), hidden by the b448 event (dissolve 1->0 d2); uh/uh2 also
        // ride a repeating offsetPosition +-5 over d16.
        [UnityTest]
        public IEnumerator Beat337FakeWallsMatchPlaybackShowHideAfterScrubs()
        {
            var authored = BeatSaberSongContainer.Instance.Map.Obstacles
                .Where(o => o.CustomFake && Mathf.Approximately(o.JsonTime, 337f))
                .ToList();
            Assert.That(authored.Count, Is.EqualTo(8),
                $"expected 8 authored b337 fake walls, found {authored.Count}: " +
                string.Join("; ", authored.Select(DescribeObstacle)));
            yield return RunFakeWallGroupParity("b337", authored, 333f,
                new[] { 336.5f, 338.25f, 449.5f }, new HashSet<float> { 338.25f });
        }

        // Four authored b529 fake walls on track timingwindow (d2, time:[0] freezes lifetime):
        // shown by the b529 event (dissolve 0->1 d2), hidden by b601 (dissolve 1->0 d4). The b609
        // bruhwork fake wall (njsOff5 njs17, d0.5, no track events) rides along the same playback
        // pass; its membership at each sample is playback-defined.
        [UnityTest]
        public IEnumerator Beat529TimingWindowAndBeat609WallsMatchPlaybackAfterScrubs()
        {
            var authored = BeatSaberSongContainer.Instance.Map.Obstacles
                .Where(o => o.CustomFake
                    && (Mathf.Approximately(o.JsonTime, 529f) || Mathf.Approximately(o.JsonTime, 609f)))
                .ToList();
            Assert.That(authored.Count, Is.EqualTo(5),
                $"expected 5 authored b529+b609 fake walls, found {authored.Count}: " +
                string.Join("; ", authored.Select(DescribeObstacle)));
            // A/B pixel proof candidates: the frozen-lifetime hide fade keeps the square in the
            // Playing frustum at b602.25 (cutout ~0.53), with the b530.25 mid-fade-in sample as
            // fallback should camera geometry ever exclude it. A/B isolates the four timingwindow
            // walls so a nearby bruhwork wall can never dominate the pixel count; the shared
            // snapshot comparison still covers all five authored walls.
            var abWalls = authored
                .Where(o => o.CustomTrack is SimpleJSON.JSONString s && s.Value == "timingwindow")
                .ToList();
            Assert.That(abWalls.Count, Is.EqualTo(4),
                $"expected 4 authored timingwindow walls for the A/B proof, found {abWalls.Count}.");
            yield return RunFakeWallGroupParity("b529/b609", authored, 525f,
                new[] { 528.5f, 530.25f, 602.25f, 609.75f }, new HashSet<float> { 530.25f },
                abBeats: new[] { 602.25f, 530.25f }, abWalls: abWalls);
        }

        // Shared matched-time scrub-vs-playback parity driver for a group of authored fake
        // obstacles. Deterministic production playback (audio stopped, CurrentSeconds stepped in
        // <=0.5-beat rendered increments) is sampled at the ACTUAL CurrentJsonTime, then compared
        // against 3 reverse/forward stopped-seek cycles to that exact beat and against the first
        // frame after resume (compared to a seek at the actual resumed beat). Runs in both Playing
        // and Preview (Gameplay workspace, Editing camera). Samples listed in shownTargets must
        // have at least one wall actively contributing (loaded + active + core enabled + cutout
        // < 0.98) during playback; other samples stay informational and are compared against the
        // playback snapshot so legitimately unloaded/hidden pools are not hardcoded.
        private IEnumerator RunFakeWallGroupParity(
            string label,
            List<Beatmap.Base.BaseObstacle> authored,
            float playbackStart,
            float[] sampleTargets,
            HashSet<float> shownTargets,
            float[] abBeats = null,
            List<Beatmap.Base.BaseObstacle> abWalls = null)
        {
            // Batchmode has no input devices; UIMode.OnPlayToggle -> SetLockState reads
            // Mouse.current when toggling playback in Playing mode.
            var testMouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var obstacleGrid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(obstacleGrid, Is.Not.Null, "ObstacleGridContainer missing.");

            foreach (var o in authored)
            {
                Debug.Log($"[WallDiag] {label} authored {DescribeObstacle(o)} " +
                    $"songBpm={o.SongBpmTime:F3} hjd={o.HalfJumpDuration:F3} " +
                    $"spawnBpm={o.SpawnSongBpmTime:F3} dur={o.Duration}");
            }

            var failures = new List<string>();
            try
            {
                foreach (var preview in new[] { false, true })
                {
                    var modeName = preview ? "Preview" : "Playing";
                    if (preview)
                    {
                        // See Beat221FakeWallsMatchPlaybackPoseAfterRepeatedScrubs: the Gameplay
                        // workspace must be applied AFTER SetUIMode restores the pre-play mode.
                        uiMode.SetUIMode(UIModeType.Preview, false);
                        editMode.EditingMode = EditingMode.Gameplay;
                        editingModeChanged = true;
                        cameraManager.SelectCamera(CameraType.Editing);
                        yield return null;
                        Assert.That(obstacleGrid.enabled, Is.True,
                            $"{label} Preview: ObstacleGridContainer is disabled under Gameplay workspace.");
                    }
                    else
                    {
                        yield return EnterPlayingFromBasicEventWorkspace();
                    }
                    yield return null;

                    atsc.MoveToJsonTime(playbackStart);
                    atsc.TogglePlaying();
                    atsc.SongAudioSource.Stop();
                    atsc.StopScheduled = true;
                    yield return null; yield return null;
                    if (!atsc.IsPlaying)
                    {
                        failures.Add($"{label} {modeName}: deterministic playback did not start.");
                        continue;
                    }

                    var playbackSnapshots =
                        new Dictionary<float, (float beat, Dictionary<Beatmap.Base.BaseObject, WallSnapshot> snap)>();
                    var nextSample = 0;
                    var lastTarget = sampleTargets[sampleTargets.Length - 1];
                    for (var t = playbackStart + 0.5f; t <= lastTarget + 0.5f && nextSample < sampleTargets.Length; t += 0.5f)
                    {
                        var target = Mathf.Min(t, sampleTargets[nextSample]);
                        SetCurrentSeconds(atsc, target);
                        yield return null; yield return null;
                        if (atsc.CurrentJsonTime < sampleTargets[nextSample] - 0.001f) continue;
                        var sampled = atsc.CurrentJsonTime;
                        var snap = CaptureWalls(obstacleGrid, authored);
                        playbackSnapshots[sampleTargets[nextSample]] = (sampled, snap);
                        LogWallSnapshot(obstacleGrid, authored,
                            $"{label} {modeName} playback@{sampled:F3}");
                        var loaded = snap.Values.Count(s => s.loaded);
                        var shown = snap.Values.Count(s =>
                            s.loaded && s.active && s.coreEnabled && s.cutout < 0.98f);
                        Debug.Log($"[WallDiag] {label} {modeName} playback@{sampled:F3}: " +
                            $"authored={authored.Count} loaded={loaded} shown={shown}");
                        if (shownTargets.Contains(sampleTargets[nextSample]) && shown == 0)
                        {
                            failures.Add($"{label} {modeName} playback@{sampled:F3}: 0/{authored.Count} " +
                                "authored walls actively contributing during continuous playback " +
                                "where authored dissolve shows them (non-vacuous baseline).");
                        }
                        nextSample++;
                    }
                    atsc.TogglePlaying();
                    yield return null;
                    Assert.That(nextSample, Is.EqualTo(sampleTargets.Length),
                        $"{label} {modeName}: playback stepping missed sample targets.");

                    foreach (var pair in playbackSnapshots)
                    {
                        var sampled = pair.Value.beat;
                        var expected = pair.Value.snap;

                        for (var cycle = 0; cycle < 3; ++cycle)
                        {
                            atsc.MoveToJsonTime(Mathf.Max(0f, sampled - 6f));
                            yield return null;
                            atsc.MoveToJsonTime(sampled + 4f);
                            yield return null;
                            atsc.MoveToJsonTime(sampled);
                            yield return null; yield return null;
                            CompareWallSnapshots(expected, CaptureWalls(obstacleGrid, authored),
                                $"{label} {modeName} seek-cycle{cycle}@{sampled:F3}", failures);
                        }

                        // A/B pixel proof at the frozen post-seek beat (Playing camera only):
                        // group renderers that intersect the camera frustum with cutout < .98
                        // must actually rasterize into the frame. Beats with no in-view renderer
                        // stay verified by renderer state only.
                        if (!preview && abBeats != null
                            && abBeats.Any(b => Mathf.Approximately(b, pair.Key)))
                        {
                            var playingCam = cameraManager.CameraControllers[1].Camera;
                            var frustum = GeometryUtility.CalculateFrustumPlanes(playingCam);
                            var group = new List<Renderer>();
                            foreach (var o in abWalls ?? authored)
                            {
                                if (!obstacleGrid.LoadedContainers.TryGetValue(o, out var c)
                                    || c is not Beatmap.Containers.ObstacleContainer w)
                                {
                                    continue;
                                }
                                var cut = w.MpbController != null
                                    ? w.MpbController.Mpb.GetFloat(cutoutPropertyId)
                                    : float.NaN;
                                if (float.IsFinite(cut) && cut >= 0.98f) continue;
                                if (w.CoreRenderer != null) group.Add(w.CoreRenderer);
                                var outline = w.OutlineTransform == null ? null
                                    : w.OutlineTransform.GetComponentInChildren<MeshRenderer>(true);
                                if (outline != null) group.Add(outline);
                            }
                            group = group.Where(r => r != null && r.enabled
                                    && r.gameObject.activeInHierarchy
                                    && GeometryUtility.TestPlanesAABB(frustum, r.bounds))
                                .ToList();
                            if (group.Count == 0)
                            {
                                Debug.Log($"[WallDiag] {label} A/B @{sampled:F3}: no group renderer " +
                                    "in the Playing-camera frustum with cutout<.98 - this beat is " +
                                    "verified by renderer state, not raster.");
                            }
                            else
                            {
                                var delta = CaptureGroupDelta(playingCam, group);
                                Debug.Log($"=== A/B pixel proof: {label} {modeName} b{sampled:F3} " +
                                    $"renderers={group.Count} maxDiff={delta.MaxDiff} " +
                                    $"pixels={delta.Count} ===");
                                if (delta.Count < 10)
                                {
                                    failures.Add($"{label} {modeName} A/B @{sampled:F3}: in-view " +
                                        $"group changed {delta.Count} pixels (maxDiff={delta.MaxDiff}), " +
                                        "expected >=10.");
                                }
                            }
                        }

                        // Resume applies an audio-latency offset to CurrentSeconds; compare the
                        // resumed frame against a stopped seek to the ACTUAL resumed beat.
                        atsc.TogglePlaying();
                        yield return null; yield return null;
                        var resumedBeat = atsc.CurrentJsonTime;
                        var resumedSnap = CaptureWalls(obstacleGrid, authored);
                        atsc.TogglePlaying();
                        yield return null;
                        atsc.MoveToJsonTime(resumedBeat);
                        yield return null; yield return null;
                        // The resumed frame's animated cutout is evaluated at the post-latency
                        // audio clock, which can sit a few tenths of a beat off the read
                        // CurrentJsonTime on a steep dissolve fade; presence and pose stay strict,
                        // cutout gets a clock-adjusted tolerance.
                        CompareWallSnapshots(CaptureWalls(obstacleGrid, authored), resumedSnap,
                            $"{label} {modeName} resumed@{resumedBeat:F3} vs seek", failures,
                            cutoutTolerance: 0.2f);
                    }
                }
            }
            finally
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
                UnityEngine.InputSystem.InputSystem.RemoveDevice(testMouse);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Same A/B capture pattern as CensoredFullMapVisibilityTest.CaptureGroupDelta: render the
        // camera once with the group enabled and once disabled at an unchanged beat, then count
        // pixels whose max RGB channel delta reaches 8/255.
        private static (int MaxDiff, int Count) CaptureGroupDelta(
            Camera camera, List<Renderer> group)
        {
            var previousTarget = camera.targetTexture;
            var sceneTexture = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            var withGroup = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var withoutGroup = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var original = group.Select(r => r.enabled).ToList();
            try
            {
                camera.targetTexture = sceneTexture;
                camera.Render();
                ReadRenderTexture(sceneTexture, withGroup);

                for (var i = 0; i < group.Count; i++) group[i].enabled = false;
                try
                {
                    camera.Render();
                    ReadRenderTexture(sceneTexture, withoutGroup);
                }
                finally
                {
                    for (var i = 0; i < group.Count; i++) group[i].enabled = original[i];
                }

                var maxDiff = 0;
                var count = 0;
                for (var y = 0; y < 512; y++)
                {
                    for (var x = 0; x < 1024; x++)
                    {
                        var a = withGroup.GetPixel(x, y);
                        var b = withoutGroup.GetPixel(x, y);
                        var diff = Mathf.RoundToInt(255f * Mathf.Max(
                            Mathf.Abs(a.r - b.r),
                            Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))));
                        maxDiff = Mathf.Max(maxDiff, diff);
                        if (diff >= 8) count++;
                    }
                }
                return (maxDiff, count);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Object.Destroy(sceneTexture);
                Object.Destroy(withGroup);
                Object.Destroy(withoutGroup);
            }
        }

        private static void ReadRenderTexture(RenderTexture source, Texture2D destination)
        {
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                destination.Apply();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private sealed class WallSnapshot
        {
            public bool loaded;
            public bool active;
            public bool coreEnabled;
            public bool animatorEnabled;
            public Vector3 center;
            public Vector3 size;
            public float cutout;
        }

        private static void SetCurrentSeconds(AudioTimeSyncController atsc, float jsonBeat)
        {
            var songBeat = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonBeat);
            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat));
        }

        private static string DescribeObstacle(Beatmap.Base.BaseObstacle o) =>
            $"b{o.JsonTime:F2} d{o.Duration} fake={o.CustomFake} track={o.CustomTrack}";

        private static Dictionary<Beatmap.Base.BaseObject, WallSnapshot> CaptureWalls(
            ObstacleGridContainer grid, List<Beatmap.Base.BaseObstacle> authored)
        {
            var snap = new Dictionary<Beatmap.Base.BaseObject, WallSnapshot>();
            foreach (var o in authored)
            {
                var s = new WallSnapshot();
                if (grid.LoadedContainers.TryGetValue(o, out var baseContainer)
                    && baseContainer is Beatmap.Containers.ObstacleContainer wall)
                {
                    s.loaded = true;
                    s.active = wall.gameObject.activeInHierarchy;
                    var core = wall.CoreRenderer;
                    s.coreEnabled = core != null && core.enabled;
                    if (core != null)
                    {
                        s.center = core.bounds.center;
                        s.size = core.bounds.size;
                    }
                    s.animatorEnabled = wall.Animator != null && wall.Animator.isActiveAndEnabled;
                    s.cutout = wall.MpbController != null
                        ? wall.MpbController.Mpb.GetFloat(cutoutPropertyId)
                        : float.NaN;
                }
                snap[o] = s;
            }
            return snap;
        }

        private static void LogWallSnapshot(
            ObstacleGridContainer grid, List<Beatmap.Base.BaseObstacle> authored, string phase)
        {
            var lines = authored.Select(o =>
            {
                var loaded = grid.LoadedContainers.TryGetValue(o, out var c)
                    && c is Beatmap.Containers.ObstacleContainer;
                if (!loaded) return $"  {DescribeObstacle(o)} NOT LOADED";
                var wall = (Beatmap.Containers.ObstacleContainer)grid.LoadedContainers[o];
                var core = wall.CoreRenderer;
                var animatorOk = wall.Animator != null && wall.Animator.isActiveAndEnabled;
                return $"  {DescribeObstacle(o)} active={wall.gameObject.activeInHierarchy} " +
                    $"coreEnabled={(core != null && core.enabled)} " +
                    $"center={(core != null ? core.bounds.center.ToString() : "<none>")} " +
                    $"size={(core != null ? core.bounds.size.ToString() : "<none>")} " +
                    $"cutout={(wall.MpbController != null ? wall.MpbController.Mpb.GetFloat(cutoutPropertyId) : float.NaN):F2} " +
                    $"animator={animatorOk}";
            });
            Debug.Log($"[WallDiag] {phase}:\n" + string.Join("\n", lines));
        }

        private static void CompareWallSnapshots(
            Dictionary<Beatmap.Base.BaseObject, WallSnapshot> expected,
            Dictionary<Beatmap.Base.BaseObject, WallSnapshot> actual,
            string phase,
            List<string> failures,
            float cutoutTolerance = 0.05f)
        {
            foreach (var pair in expected)
            {
                var e = pair.Value;
                var a = actual[pair.Key];
                var name = DescribeObstacle((Beatmap.Base.BaseObstacle)pair.Key);
                if (e.loaded != a.loaded || e.active != a.active || e.coreEnabled != a.coreEnabled)
                {
                    failures.Add($"{phase}: '{name}' presence differs " +
                        $"(loaded {e.loaded}->{a.loaded} active {e.active}->{a.active} " +
                        $"coreEnabled {e.coreEnabled}->{a.coreEnabled}).");
                    continue;
                }
                if (!e.loaded || !e.coreEnabled) continue;
                var dPos = Vector3.Distance(e.center, a.center);
                var dSize = Vector3.Distance(e.size, a.size);
                var dCut = Mathf.Abs(e.cutout - a.cutout);
                if (dPos > 0.05f || dSize > 0.05f || (float.IsFinite(dCut) && dCut > cutoutTolerance))
                {
                    failures.Add($"{phase}: '{name}' pose differs (center {e.center}->{a.center} " +
                        $"dPos={dPos:F3} size {e.size}->{a.size} dSize={dSize:F3} " +
                        $"cutout {e.cutout:F2}->{a.cutout:F2}).");
                }
            }
        }

        // Regression: the authored Head (asdkm) camera animation starts at b537 — there is no
        // authored Head event near b532 — yet scrubbing forward to ~b532 then rewinding leaves the
        // playing camera lurched forward and it never comes home. Before any b537 visit the camera
        // must stay at its pristine pose across seeks; after a b541 visit + rewind the rig must be
        // fully restored (position AND original parent), including across a UI mode cycle.
        [UnityTest]
        public IEnumerator Beat532CameraStaysHomeBeforeAndAfterHeadTrackVisit()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null; // let CameraController.Update apply the FOV before sampling
            var cameraController = cameraManager.CameraControllers[1];
            var camera = cameraController.Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            var animatorField = typeof(CameraController).GetField("cameraAnimator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cameraAnimator = animatorField.GetValue(cameraController) as ObjectAnimator;
            Assert.That(cameraAnimator, Is.Not.Null, "cameraAnimator field missing on CameraController.");

            var pristinePos = camera.transform.position;
            var pristineRigParent = cameraAnimator.transform.parent;
            if (Vector3.Distance(pristinePos, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                failures.Add($"setup: pristine playing camera world={pristinePos}, expected ~(0,1.65,0).");

            string HierarchyName(Transform t) => t == null ? "null" : t.name;

            void RequirePristine(string phase)
            {
                var pos = camera.transform.position;
                if (Vector3.Distance(pos, pristinePos) > 0.05f)
                    failures.Add($"{phase}: camera world={pos}, expected pristine {pristinePos}.");
                var parent = cameraAnimator.transform.parent;
                if (!ReferenceEquals(parent, pristineRigParent))
                    failures.Add($"{phase}: camera rig parent={HierarchyName(parent)}, expected " +
                        $"{HierarchyName(pristineRigParent)} (no Head track is authored yet).");
            }

            // No authored Head camera event before b537: the camera must not move at all.
            foreach (var beat in new[] { 531.75f, 532f, 532.25f, 534f, 536.5f })
            {
                atsc.MoveToJsonTime(beat);
                RequirePristine($"beat {beat} immediate");
                yield return null;
                yield return null;
                RequirePristine($"beat {beat} post-frames");
            }

            // Visit past the bind so the b537 Head track animation actually runs.
            atsc.MoveToJsonTime(541f);
            yield return null;
            yield return null;
            yield return null;
            var z541 = camera.transform.position.z;
            if (Mathf.Abs(z541 - -2.121f) > 0.08f)
                failures.Add($"beat 541: camera z={z541:F3}, expected ~-2.121 on the authored ease " +
                    "(b537 Head event must have executed).");

            // Every rewind to a pre-b537 beat must restore the pristine pose and rig parent.
            foreach (var beat in new[] { 536.5f, 534f, 532.25f, 532f, 531.75f, 532f, 222.25f, 532f })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                RequirePristine($"rewind to {beat}");
            }

            // Mode cycle without map reload, still sitting at a pre-b537 beat.
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            atsc.MoveToJsonTime(532f);
            yield return null;
            yield return null;
            RequirePristine("mode cycle at 532");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Leaving Playing must detach the Head track synchronously, not only on a rewind seek:
        // SyncPlayerTrack's UI-mode guard used to run after the AnimationMode early return, so
        // switching Playing→Normal at a bound beat left the camera rig parented under asdkm at its
        // animated pose until a seek forced a re-evaluation.
        [UnityTest]
        public IEnumerator LeavingPlayingAtBeat541UnbindsHeadCameraWithoutSeek()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null;
            var cameraController = cameraManager.CameraControllers[1];
            var camera = cameraController.Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            var animatorField = typeof(CameraController).GetField("cameraAnimator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cameraAnimator = animatorField.GetValue(cameraController) as ObjectAnimator;
            Assert.That(cameraAnimator, Is.Not.Null, "cameraAnimator field missing on CameraController.");

            var pristinePos = camera.transform.position;
            var pristineRigParent = cameraAnimator.transform.parent;
            if (Vector3.Distance(pristinePos, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                failures.Add($"setup: pristine playing camera world={pristinePos}, expected ~(0,1.65,0).");

            string HierarchyName(Transform t) => t == null ? "null" : t.name;

            void RequirePristine(string phase)
            {
                var pos = camera.transform.position;
                if (Vector3.Distance(pos, pristinePos) > 0.05f)
                    failures.Add($"{phase}: camera world={pos}, expected pristine {pristinePos}.");
                var parent = cameraAnimator.transform.parent;
                if (!ReferenceEquals(parent, pristineRigParent))
                    failures.Add($"{phase}: camera rig parent={HierarchyName(parent)}, expected " +
                        $"{HierarchyName(pristineRigParent)} (mode switch must detach the Head track).");
            }

            void RequireBound541(string phase)
            {
                var pos = camera.transform.position;
                if (Mathf.Abs(pos.z - -2.121f) > 0.08f)
                    failures.Add($"{phase}: camera z={pos.z:F3}, expected ~-2.121 on the authored ease.");
                if (Mathf.Abs(pos.y - 1.65f) > 0.05f)
                    failures.Add($"{phase}: camera y={pos.y:F3}, expected ~1.65.");
            }

            // Bind the Head track: b537 assigns asdkm to Head, z eases 0→-3 over 8 beats.
            atsc.MoveToJsonTime(541f);
            RequireBound541("beat 541 immediate");
            yield return null;
            RequireBound541("beat 541 post-frame");

            // Leaving Playing without seeking must already detach and restore the home pose.
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            RequirePristine("Normal mode exit immediate");
            yield return null;
            RequirePristine("Normal mode exit post-frame");

            // Preview keeps AnimationMode true, so the detach must come from the UI-mode guard.
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            RequirePristine("Preview mode at 541");

            // Returning to Playing while still at 541 must synchronously rebind asdkm.
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            RequireBound541("re-enter Playing at 541 immediate");
            yield return null;
            RequireBound541("re-enter Playing at 541 post-frame");

            // Rewind to a pre-b537 beat restores the baseline, and a mode exit there is a no-op.
            atsc.MoveToJsonTime(532f);
            yield return null;
            RequirePristine("rewind to 532");
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            RequirePristine("Normal mode exit at 532");
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return null;
            RequirePristine("Playing re-entry at 532");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // TEMP trial: after the real bind, disconnect via the existing compiled path and re-attach
        // the camera rig through the shared Transform-target seam (AttachToTrack with
        // trackParentTarget) to see if the standard animator path can drive the Head track without
        // the y-lift or seek lag of the bespoke rig bind. Not a behavioral spec — reports what the
        // seam actually produces.
        [UnityTest]
        public IEnumerator HeadCameraTrackParentTargetTrial()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            yield return null; // let CameraController.Update apply the FOV before sampling
            var cameraController = cameraManager.CameraControllers[1];
            var camera = cameraController.Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();

            var t = typeof(CameraController);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var animatorField = t.GetField("cameraAnimator", flags);
            var currentTrackField = t.GetField("currentTrack", flags);
            var disconnectMethod = t.GetMethod("DisconnectPlayerTrack", flags);
            Assert.That(animatorField, Is.Not.Null, "cameraAnimator field missing on CameraController.");
            Assert.That(currentTrackField, Is.Not.Null, "currentTrack field missing on CameraController.");
            Assert.That(disconnectMethod, Is.Not.Null, "DisconnectPlayerTrack missing on CameraController.");
            var cameraAnimator = animatorField.GetValue(cameraController) as ObjectAnimator;
            Assert.That(cameraAnimator, Is.Not.Null, "cameraAnimator is null on CameraController.");

            var pristinePos = camera.transform.position;
            var pristineRigParent = cameraAnimator.transform.parent;
            var pristineLocalPos = cameraAnimator.transform.localPosition;
            var pristineLocalRot = cameraAnimator.transform.localRotation;
            var pristineLocalScale = cameraAnimator.transform.localScale;
            if (Vector3.Distance(pristinePos, new Vector3(0f, 1.65f, 0f)) > 0.05f)
                failures.Add($"setup: pristine playing camera world={pristinePos}, expected ~(0,1.65,0).");

            string HierarchyName(Transform x) => x == null ? "null" : x.name;

            void RequirePose(float beat, string phase)
            {
                var expectedZ = -3f * Mathf.Sin(Mathf.Clamp01((beat - 537f) / 8f) * Mathf.PI / 2f);
                var pos = camera.transform.position;
                if (Mathf.Abs(pos.y - 1.65f) > 0.05f)
                    failures.Add($"beat {beat} {phase}: camera y={pos.y:F3}, expected ~1.65.");
                if (Mathf.Abs(pos.z - expectedZ) > 0.07f)
                    failures.Add($"beat {beat} {phase}: camera z={pos.z:F3}, expected {expectedZ:F3} " +
                        "(authored easeOutSine z0->-3 over 8 beats).");
            }

            // Trigger the production bind at 537.25.
            atsc.MoveToJsonTime(537.25f);
            yield return null;
            yield return null;

            var asdkm = currentTrackField.GetValue(cameraController) as TrackAnimator;
            Assert.That(asdkm, Is.Not.Null, "currentTrack is null after b537.25 bind.");
            Assert.That(asdkm.gameObject.name, Is.EqualTo("asdkm"),
                $"currentTrack bound to '{asdkm.gameObject.name}', expected 'asdkm'.");

            // Disconnect via the existing compiled path (restores the rig home), then re-attach via
            // the shared AttachToTrack seam: rig rides the track's ObjectParentTransform as
            // trackParentTarget, driven by held track values instead of a bespoke rig transform.
            disconnectMethod.Invoke(cameraController, null);
            Assert.That(currentTrackField.GetValue(cameraController), Is.Null,
                "currentTrack not cleared after DisconnectPlayerTrack.");

            var rig = cameraAnimator.transform;
            cameraAnimator.enabled = true;
            rig.SetParent(asdkm.Track.ObjectParentTransform, false);
            rig.localPosition = pristineLocalPos;
            rig.localRotation = pristineLocalRot;
            rig.localScale = pristineLocalScale;
            cameraAnimator.AttachToTrack(asdkm.Track, "asdkm", isV2Map: false);
            asdkm.AddChild(cameraAnimator);
            asdkm.PushToChild(cameraAnimator);
            // Keep CameraController.Update from re-running its own bind path on later frames.
            currentTrackField.SetValue(cameraController, asdkm);
            cameraAnimator.LateUpdate();
            RequirePose(537.25f, "trial-immediate");

            foreach (var beat in new[] { 538f, 541f, 543f })
            {
                atsc.MoveToJsonTime(beat);
                RequirePose(beat, "immediate");
                yield return null;
                RequirePose(beat, "post-frame");
            }

            // Rewind before the bind beat: CameraController.Update should disconnect and restore home.
            atsc.MoveToJsonTime(536.5f);
            yield return null;
            var home = camera.transform.position;
            if (Vector3.Distance(home, pristinePos) > 0.05f)
                failures.Add($"rewind 536.5: camera world={home}, expected pristine {pristinePos}.");
            var parent = cameraAnimator.transform.parent;
            if (!ReferenceEquals(parent, pristineRigParent))
                failures.Add($"rewind 536.5: rig parent={HierarchyName(parent)}, expected " +
                    $"{HierarchyName(pristineRigParent)}.");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // TEMP diagnostic (no parity assertions): probes why freshly recycled b221 wall animators
        // never show the held track z — records subscriber order inside the ATS stopped-seek events
        // (ObstacleGridContainer.OnTimeChanged vs TrackAnimator.PushOnStoppedTimeChanged), whether
        // the fresh animator is in the track's CachedChildren at push time, its aggregator
        // Count/Keep, and whether a manual PushOnStoppedTimeChanged + OnTimeChanged on just the new
        // animator delivers the held depth. Drives Beat221WallsSeekSynchronouslyToStableGameDepth /
        // Beat221WallsStayAtAuthoredDepthOnImmediateReverseSeek.
        [UnityTest]
        public IEnumerator WallNewAnimatorSubscriptionAndPushDiagnostics()
        {
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return EnterPlayingFromBasicEventWorkspace();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var obstacleCollection = Object.FindAnyObjectByType<ObstacleGridContainer>();
            var report = new StringBuilder();
            report.AppendLine("salty-wall-fresh-animator-delivery-report");

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var animatorOnTimeChanged = typeof(ObjectAnimator).GetMethod("OnTimeChanged", flags);
            Assert.That(animatorOnTimeChanged, Is.Not.Null,
                "ObjectAnimator.OnTimeChanged not found via reflection.");

            System.Delegate[] Invocations(string fieldName)
            {
                var f = typeof(AudioTimeSyncController).GetField(fieldName, flags);
                if (f == null)
                {
                    report.AppendLine($"{fieldName}: backing event field NOT FOUND via reflection");
                    return null;
                }
                var d = f.GetValue(atsc) as System.Delegate;
                if (d == null)
                {
                    report.AppendLine($"{fieldName}: event has no subscribers");
                    return System.Array.Empty<System.Delegate>();
                }
                return d.GetInvocationList();
            }

            int IndexOf(System.Delegate[] list, object target, string method) =>
                list == null ? -2 : System.Array.FindIndex(list,
                    d => ReferenceEquals(d.Target, target) && d.Method.Name == method);

            var trackAnimators = Object.FindObjectsByType<TrackAnimator>(FindObjectsSortMode.None);
            TrackAnimator NamedTrack(string n) =>
                trackAnimators.FirstOrDefault(t => t.gameObject.name == n);

            List<Beatmap.Containers.ObstacleContainer> Walls() =>
                obstacleCollection.LoadedContainers.Values
                    .OfType<Beatmap.Containers.ObstacleContainer>()
                    .Where(c => c.ObstacleData != null
                        && Mathf.Approximately(c.ObstacleData.JsonTime, 221f))
                    .ToList();
            Beatmap.Containers.ObstacleContainer RedBeat0() => Walls().FirstOrDefault(c =>
                c.ObstacleData.CustomTrack is JSONString s && s.Value == "beat0");
            Beatmap.Containers.ObstacleContainer White() => Walls().FirstOrDefault(c =>
                c.ObstacleData.CustomTrack is JSONArray arr
                && arr.Children.Any(v => (string)v == "1/4" || (string)v == "pee2"));

            int SubCount(System.Delegate[] list, object target) =>
                list == null ? -1 : list.Count(d => ReferenceEquals(d.Target, target));

            void Snapshot(string phase)
            {
                report.AppendLine($"== {phase} ==");
                var red = RedBeat0();
                var white = White();
                report.AppendLine($"walls loaded={Walls().Count} redBeat0={(red == null ? "<none>" : "found")} " +
                    $"white={(white == null ? "<none>" : "found")}");
                foreach (var wall in new[] { red, white })
                {
                    if (wall == null) continue;
                    var a = wall.Animator;
                    var label = ReferenceEquals(wall, white) ? "white[1/4,pee2]" : "red beat0";
                    var core = wall.CoreRenderer;
                    report.AppendLine($"  wall {label} containerId={wall.GetInstanceID()} " +
                        $"animatorId={(a == null ? -1 : a.GetInstanceID())} enabled={(a != null && a.enabled)} " +
                        $"targetType={(a != null ? a.TargetType.ToString() : "<null>")} " +
                        $"offsetCount={(a != null ? a.OffsetPosition.Count : -1)} " +
                        $"offsetKeep={(a != null ? a.OffsetPosition.Keep : -1)} " +
                        $"localPos={(a != null && a.LocalTarget != null ? a.LocalTarget.localPosition.ToString("F3") : "<null>")} " +
                        $"coreZ={(core != null ? core.bounds.center.z.ToString("F3") : "<null>")} " +
                        $"animatorOnTimeChangedSubs={SubCount(Invocations("OnTimeChanged"), a)} " +
                        $"animatorOnTimeFlushSubs={SubCount(Invocations("OnTimeFlushPending"), a)}");
                }
                foreach (var name in new[] { "beat0", "beat1", "beat4", "pee2" })
                {
                    var t = NamedTrack(name);
                    if (t == null)
                    {
                        report.AppendLine($"  track {name}: <not found>");
                        continue;
                    }
                    var childIds = t.CachedChildren.Select(c => c.GetInstanceID()).ToArray();
                    var containsWalls = new[] { red != null ? red.Animator : null,
                            white != null ? white.Animator : null }
                        .Where(x => x != null)
                        .Select(x => childIds.Contains(x.GetInstanceID()));
                    report.AppendLine($"  track {name}: enabled={t.enabled} active={t.isActiveAndEnabled} " +
                        $"cachedChildren=[{string.Join(",", childIds)}] containsSelected=[{string.Join(",", containsWalls)}]");
                }
            }

            // Invocation order inside each stopped-seek event.
            foreach (var evt in new[] { "OnTimeFlushPending", "OnTimeChangedEarly", "OnTimeChanged" })
            {
                var list = Invocations(evt);
                if (list == null) continue;
                var wallIdx = IndexOf(list, obstacleCollection, "OnTimeChanged");
                var pushIdx = new[] { "beat0", "beat1", "beat4", "pee2" }
                    .Select(n => (n, IndexOf(list, NamedTrack(n), "PushOnStoppedTimeChanged")));
                report.AppendLine($"event {evt}: total={list.Length} " +
                    $"ObstacleGridContainer.OnTimeChanged@{wallIdx} " +
                    $"PushOnStoppedTimeChanged: {string.Join(" ", pushIdx.Select(p => $"{p.n}@{p.Item2}"))}");
            }

            atsc.MoveToJsonTime(220.75f);
            Snapshot("beat 220.75 pre-seek");
            var preRed = RedBeat0();
            var preWhite = White();
            var oldRedId = preRed != null && preRed.Animator != null ? preRed.Animator.GetInstanceID() : -1;
            var oldWhiteId = preWhite != null && preWhite.Animator != null ? preWhite.Animator.GetInstanceID() : -1;

            atsc.MoveToJsonTime(222.25f);
            Snapshot("beat 222.25 immediate");
            var postRed = RedBeat0();
            var postWhite = White();
            var newRedId = postRed != null && postRed.Animator != null ? postRed.Animator.GetInstanceID() : -1;
            var newWhiteId = postWhite != null && postWhite.Animator != null ? postWhite.Animator.GetInstanceID() : -1;
            report.AppendLine($"recycled: red {oldRedId}->{newRedId} white {oldWhiteId}->{newWhiteId}");

            // Manual delivery on just the two fresh animators via the actual production early handler.
            var redWall = RedBeat0();
            var whiteWall = White();
            Assert.That(redWall, Is.Not.Null, "red beat0 wall missing at 222.25.");
            Assert.That(whiteWall, Is.Not.Null, "white [1/4,pee2] wall missing at 222.25.");
            var redTrack = NamedTrack("beat0");
            var whiteTracks = new[] { NamedTrack("1/4"), NamedTrack("pee2") }.Where(t => t != null);
            Assert.That(redTrack, Is.Not.Null, "TrackAnimator 'beat0' not found.");
            Assert.That(whiteTracks.Count(), Is.EqualTo(2), "TrackAnimators '1/4'/'pee2' not found.");

            void ManualDeliver(Beatmap.Containers.ObstacleContainer wall, string label,
                IEnumerable<TrackAnimator> tracks)
            {
                var a = wall.Animator;
                report.AppendLine($"manual {label}: pre-push offsetCount={a.OffsetPosition.Count} " +
                    $"keep={a.OffsetPosition.Keep} coreZ={wall.CoreRenderer.bounds.center.z:F3}");
                foreach (var t in tracks)
                {
                    var touched = t.CachedChildren.Select(c => c.GetInstanceID()).ToArray();
                    t.PushOnStoppedTimeChanged();
                    report.AppendLine($"  PushOnStoppedTimeChanged({t.gameObject.name}) touched [{string.Join(",", touched)}] " +
                        $"thisAnimatorInCached={touched.Contains(a.GetInstanceID())} " +
                        $"offsetCount={a.OffsetPosition.Count} keep={a.OffsetPosition.Keep}");
                }
                animatorOnTimeChanged.Invoke(a, null);
                report.AppendLine($"  post-OnTimeChanged coreZ={wall.CoreRenderer.bounds.center.z:F3} " +
                    $"localPos={(a.LocalTarget != null ? a.LocalTarget.localPosition.ToString("F3") : "<null>")}");
            }

            ManualDeliver(redWall, "red beat0", new[] { redTrack });
            ManualDeliver(whiteWall, "white", whiteTracks);

            yield return null;
            Snapshot("beat 222.25 post-frame");

            var dir = @"C:\Users\tdrak\AppData\Local\Temp\devin-salty-diag";
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "salty-wall-fresh-animator-delivery-report.txt"),
                report.ToString());
            Debug.Log(report.ToString());
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

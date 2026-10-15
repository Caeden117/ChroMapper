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
    // Fixture source: "The 15 Sublimit", mapped by Mawntee (ft. Lonely) (BeatSaver ID: 1c8c0).
    // User report: The 15 Sublimit ExpertPlusStandard.dat shows missing/wrong notes after
    // scrubbing b320-b370 in Preview/Playing. Unlike The15SublimitMapParityTest (which must stay
    // on the portable <=b156 fixture), this class loads the complete installed map: b1
    // AssignTrackParent puts "note"/"fakenote" under "notes+player", and AnimateTrack writes
    // stepped x positions in the millions to that shared parent every few beats across the
    // window (316.05, 319, 320.05, 322.05, 324.05, 348.05, 352.05, 354.05, 356.05, 360.05,
    // 363.05, 370.05, 372.05). AssignPlayerToTrack carries the camera, so track-relative pose
    // is the strict comparison while world x is compared at >=1m (float precision in millions).
    [Explicit]
    public class The15SublimitFullMapScrubParityTest : BasicEventChunkingTestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private EditModeContext editModeContext;
        private EditingMode editingModeBeforeTest;
        private bool editingModeCaptured;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;
        private Vector3 previousEditingCameraPosition;
        private Quaternion previousEditingCameraRotation;
        private bool editingCameraMoved;
        private AudioTimeSyncController playbackClock;
        private bool playbackClockWasEnabled;

        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");

        private const string SourceMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/" +
            "1c8c0 (The 15 Sublimit - Mawntee (ft. Lonely))/ExpertPlusStandard.dat";

        // Authored real notes inside the scrub window (beat, x, y, color, cutDirection) from
        // ExpertPlusStandard.dat; used to prove each checkpoint samples non-vacuous authored data.
        private static readonly (float b, int x, int y, int c, int d)[] authoredWindowNotes =
        {
            (316f, 0, 2, 0, 0), (316f, 1, 0, 1, 6), (316f, 2, 1, 1, 6),
            (320f, 1, 1, 0, 7), (320f, 3, 0, 1, 5),
            (322f, 0, 0, 1, 2), (322f, 0, 1, 0, 2),
            (324f, 2, 2, 1, 5), (324f, 3, 0, 0, 7),
            (348f, 2, 2, 0, 1), (348f, 3, 1, 1, 5),
            (352f, 0, 0, 1, 2), (352f, 0, 1, 0, 2),
            (354f, 3, 0, 0, 3), (354f, 3, 1, 1, 3),
            (356f, 0, 0, 1, 2), (356f, 1, 0, 1, 2), (356f, 1, 2, 0, 4),
            (368f, 0, 1, 0, 4),
            (370f, 1, 0, 1, 2),
            (372f, 2, 0, 0, 3), (372f, 2, 2, 1, 5), (372f, 3, 0, 0, 3),
        };

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
                2,
                JSON.Parse(File.ReadAllText(SourceMapPath)),
                beatsPerMinute: 120,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 320);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // Same matched-time contract as DeleteItAllEndingNoteFacingTest.MatchedTimeScrubParityAcrossSections:
        // deterministic production playback (audio stopped, CurrentSeconds stepped <=0.5 beat per
        // rendered frame) up to each checkpoint, sample the ACTUAL CurrentJsonTime, then 3 stopped
        // backward/forward seek cycles (one same-chunk b352->b348 for the 352.25 checkpoint) plus
        // first resumed frame compared against a seek at its ACTUAL beat. No manual Animator.LateUpdate.
        [UnityTest]
        public IEnumerator MatchedTimeScrubParityBeats316To372()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(noteGrid, Is.Not.Null, "NoteGridContainer missing.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "CurrentSeconds property not found.");

            InitializeVirtualInput(includeKeyboard: false);

            var failures = new List<string>();
            // 371.75 not 372.25: real notes despawn at their hit beat, so a checkpoint 0.25b
            // after the b372 triple is authored-empty; 371.75 keeps it upcoming.
            var sections = new[] { 316.25f, 320.25f, 322.25f, 348.25f, 352.25f, 354.25f, 368.25f, 370.25f, 371.75f };
            var repeatSections = new[] { 348.25f, 368.25f };
            // Checkpoints in the reported window get one wide cross-window excursion cycle
            // (backward to b320.25, forward to b370.25) in addition to the local +-6/+3 hops.
            var wideExcursionSections = new[] { 320.25f, 348.25f, 352.25f, 368.25f, 371.75f };
            try
            {
                foreach (var playing in new[] { true, false })
                {
                    var modeName = playing ? "Playing" : "Preview";
                    var visibleSections = 0;
                    if (playing)
                    {
                        uiMode.SetUIMode(UIModeType.Playing, false);
                        cameraManager.SelectCamera(CameraType.Playing);
                    }
                    else
                    {
                        uiMode.SetUIMode(UIModeType.Preview, false);
                        cameraManager.SelectCamera(CameraType.Editing);
                        var editingCam = cameraManager.CameraControllers[0].Camera;
                        if (!editingCameraMoved)
                        {
                            previousEditingCameraPosition = editingCam.transform.position;
                            previousEditingCameraRotation = editingCam.transform.rotation;
                            editingCameraMoved = true;
                        }
                        editingCam.transform.SetPositionAndRotation(
                            new Vector3(0f, 1.65f, 0f), Quaternion.LookRotation(Vector3.forward));
                    }
                    var cam = cameraManager.CameraControllers[playing ? 1 : 0].Camera;
                    yield return null;

                    IEnumerator VisitSection(float section)
                    {
                        var start = Mathf.Max(0f, section - 4f);
                        atsc.MoveToJsonTime(start);
                        StartPinnedPlayback(atsc);
                        yield return null;
                        yield return null;
                        if (!atsc.IsPlaying)
                        {
                            failures.Add($"{modeName} section {section}: playback did not start.");
                            atsc.StopScheduled = false;
                            yield break;
                        }
                        for (var t = start + 0.5f; t <= section && atsc.CurrentJsonTime < section - 0.001f; t += 0.5f)
                        {
                            var songBeat = (float)BeatSaberSongContainer.Instance.Map
                                .JsonTimeToSongBpmTime(Mathf.Min(t, section));
                            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat));
                            yield return null;
                            yield return null;
                        }
                        var sampled = atsc.CurrentJsonTime;
                        var playback = CaptureRenderedNotes(noteGrid, cam,
                            $"{modeName} playback@{sampled:F3}");

                        // Preview's Editing camera is not carried by AssignPlayerToTrack; aim it at
                        // the rendered note cluster once so frustum visibility is meaningful there.
                        if (!playing && playback.Count > 0)
                        {
                            var cluster = playback.Values.Where(s => s.rendered).ToList();
                            if (cluster.Count > 0)
                            {
                                var centroid = Vector3.zero;
                                foreach (var s in cluster) centroid += s.pos;
                                centroid /= cluster.Count;
                                cam.transform.SetPositionAndRotation(
                                    centroid + new Vector3(0f, 1f, -12f),
                                    Quaternion.LookRotation(centroid - (centroid + new Vector3(0f, 1f, -12f))));
                                playback = CaptureRenderedNotes(noteGrid, cam,
                                    $"{modeName} playback@{sampled:F3} recam");
                            }
                        }

                        var nearKeys = playback.Keys
                            .Where(k => Mathf.Abs(k.JsonTime - sampled) <= 4f).ToList();
                        var visiblePlayback = playback.Count(p => p.Value.visible);
                        Debug.Log($"[S15Parity] {modeName} section {section}: sampled={sampled:F3} " +
                            $"camPos={cam.transform.position} camRot={cam.transform.eulerAngles} " +
                            $"loaded={playback.Count} visible={visiblePlayback} " +
                            $"nearIds=[{string.Join("; ", nearKeys.Take(8).Select(Describe))}]");

                        // Positive authored identity check: at least one authored real note from this
                        // window must be loaded with an active body during playback, else the section
                        // is vacuous rather than a parity pass.
                        // Real notes despawn at their hit beat, so only upcoming authored notes
                        // (beat >= sampled - 0.5) inside the window count; sections with none are
                        // legitimately sparse and logged rather than asserted.
                        var expectedAuthored = authoredWindowNotes
                            .Where(n => Mathf.Abs(n.b - sampled) <= 4f && n.b >= sampled - 0.5f)
                            .ToList();
                        var loadedAuthored = expectedAuthored.Count(n =>
                            noteGrid.LoadedContainers.Keys.OfType<Beatmap.Base.BaseNote>().Any(k =>
                                Mathf.Abs(k.JsonTime - n.b) < 0.01f && k.PosX == n.x && k.PosY == n.y
                                && k.Type == n.c && k.CutDirection == n.d
                                && playback.TryGetValue(k, out var s) && s.rendered));
                        Debug.Log($"[S15Parity] {modeName} section {section}: authored window notes " +
                            $"{loadedAuthored}/{expectedAuthored.Count} loaded+rendered.");
                        if (expectedAuthored.Count > 0 && loadedAuthored == 0)
                        {
                            failures.Add($"{modeName} section {section}: none of the " +
                                $"{expectedAuthored.Count} authored window notes are rendered during " +
                                "playback - section is vacuous.");
                        }

                        // DIAGNOSTIC ONLY (b372 missing-after-seek probe): fired only on a real
                        // mismatch so the log volume doesn't shift frame timing.
                        var diagB372 = playing && Mathf.Approximately(section, 371.75f);

                        atsc.TogglePlaying();
                        yield return null;
                        if (visiblePlayback == 0)
                        {
                            Debug.Log($"[S15Parity] {modeName} section {section}: no visually " +
                                "contributing note during playback - informational only.");
                            yield break;
                        }
                        visibleSections++;

                        var wideExcursion = wideExcursionSections.Any(w => Mathf.Approximately(w, section));
                        for (var cycle = 0; cycle < 3; ++cycle)
                        {
                            if (wideExcursion && cycle == 2)
                            {
                                atsc.MoveToJsonTime(320.25f);
                                yield return null;
                                atsc.MoveToJsonTime(370.25f);
                                yield return null;
                            }
                            else
                            {
                                var backBeat = Mathf.Approximately(section, 352.25f) && cycle == 1
                                    ? 348f // same-chunk backward seek inside the reported window
                                    : Mathf.Max(0f, sampled - 6f);
                                atsc.MoveToJsonTime(backBeat);
                                yield return null;
                                atsc.MoveToJsonTime(sampled + 3f);
                                yield return null;
                            }
                            atsc.MoveToJsonTime(sampled);
                            yield return null; yield return null;
                            var scrubbed = CaptureRenderedNotes(noteGrid, cam,
                                $"{modeName} scrub-cycle{cycle}@{sampled:F3}");
                            var failMark = failures.Count;
                            CompareNoteSnapshots(playback, scrubbed,
                                $"{modeName} scrub-cycle{cycle}@{sampled:F3}", failures);
                            if (diagB372 && failures.Count > failMark)
                                DumpB372Diag(noteGrid, atsc, cam,
                                    $"scrub-cycle{cycle}-return onMismatch");

                            // A/B pixel proof on the Playing camera at the frozen seek state: an
                            // authored b352 note's body renderers must rasterize into the view.
                            if (cycle == 0 && playing && Mathf.Approximately(section, 352.25f))
                            {
                                var target = noteGrid.LoadedContainers.Values
                                    .OfType<Beatmap.Containers.NoteContainer>()
                                    .FirstOrDefault(c => c.NoteData != null
                                        && Mathf.Approximately(c.NoteData.JsonTime, 352f)
                                        && c.NoteData.CustomFake == false
                                        && c.NoteData.CustomTrack is JSONString s
                                        && s.Value == "note");
                                var group = target == null
                                    ? new List<Renderer>()
                                    : (target.ModelController.MpbController.Renderers
                                        ?? Enumerable.Empty<Renderer>())
                                        .Where(r => r != null && r.enabled
                                            && r.gameObject.activeInHierarchy)
                                        .ToList();
                                var cutout = target != null && target.MpbController != null
                                    ? target.MpbController.Mpb.GetFloat(cutoutId) : float.NaN;
                                if (group.Count == 0 || !(cutout < 0.98f))
                                {
                                    Debug.Log($"[S15Parity] Playing A/B @{sampled:F3}: b352 note body " +
                                        $"renderers unavailable for pixel proof (group={group.Count} " +
                                        $"cutout={cutout:F6}).");
                                }
                                else
                                {
                                    var delta = CaptureGroupDelta(cam, group);
                                    Debug.Log($"=== A/B pixel proof: {modeName} b{sampled:F3} b352note " +
                                        $"maxDiff={delta.MaxDiff} pixels={delta.Count} ===");
                                    if (delta.Count < 10)
                                        failures.Add($"{modeName} A/B @{sampled:F3}: b352 note changed " +
                                            $"{delta.Count} pixels (maxDiff={delta.MaxDiff}), expected >=10.");
                                }
                            }
                        }

                        // Keep TogglePlaying's latency offset, but stop native audio and freeze the
                        // automatic clock so rendered transforms and the sampled beat refer to the same time.
                        StartPinnedPlayback(atsc);
                        yield return null;
                        yield return null;
                        var resumedBeat = atsc.CurrentJsonTime;
                        var resumed = CaptureRenderedNotes(noteGrid, cam,
                            $"{modeName} resumed@{resumedBeat:F3}");
                        atsc.TogglePlaying();
                        yield return null;
                        atsc.MoveToJsonTime(resumedBeat);
                        yield return null; yield return null;
                        var seekRef = CaptureRenderedNotes(noteGrid, cam,
                            $"{modeName} seekRef@{resumedBeat:F3}");
                        var failMarkResumed = failures.Count;
                        CompareNoteSnapshots(seekRef, resumed,
                            $"{modeName} resumed@{resumedBeat:F3} vs seek", failures);
                        if (diagB372 && failures.Count > failMarkResumed)
                            DumpB372Diag(noteGrid, atsc, cam, "resumed-vs-seek onMismatch");
                    }

                    foreach (var section in sections)
                        yield return VisitSection(section);

                    // Leave and re-enter the mode, then re-run the two densest checkpoints so a
                    // mode switch does not mask state left by the first pass.
                    uiMode.SetUIMode(UIModeType.Normal, false);
                    yield return null;
                    if (playing)
                    {
                        uiMode.SetUIMode(UIModeType.Playing, false);
                        cameraManager.SelectCamera(CameraType.Playing);
                    }
                    else
                    {
                        uiMode.SetUIMode(UIModeType.Preview, false);
                        cameraManager.SelectCamera(CameraType.Editing);
                    }
                    yield return null;
                    foreach (var section in repeatSections)
                        yield return VisitSection(section);

                    if (visibleSections < 2)
                        failures.Add($"{modeName}: only {visibleSections} sections had a visible note " +
                            "during playback - too few to be a meaningful parity check.");
                }
            }
            finally
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying)
                {
                    atsc.CancelPlaying();
                }

                TearDownVirtualInput();
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Repeated cross-step seeks through the notes+player step boundary (b370.05 raw 966996 ->
        // world x 580197.6, then b372.05 raw 6942069 -> world x 4165241.4). The 200731 failure was
        // nondeterministic: this hammers the exact seek/resume seam so a single missing b372 note
        // or stale parent step is a reliable RED rather than luck.
        [UnityTest]
        public IEnumerator RepeatedCrossStepSeeksKeepBeat372Notes()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(noteGrid, Is.Not.Null, "NoteGridContainer missing.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "CurrentSeconds property not found.");

            InitializeVirtualInput(includeKeyboard: false);
            var failures = new List<string>();
            const float expectedParentX = 580197.6f;   // b370.05 step raw 966996 -> world
            const float staleParentX = 4165241.4f;     // b372.05 step raw 6942069 -> world
            try
            {
                // 10 iterations in Playing, 5 in Preview, then a final short Playing pass after
                // the Playing->Preview->Playing mode switch.
                var plan = new[] { (true, 10), (false, 5), (true, 2) };
                foreach (var (playing, iterations) in plan)
                {
                    var modeName = playing ? "Playing" : "Preview";
                    if (playing)
                    {
                        uiMode.SetUIMode(UIModeType.Playing, false);
                        cameraManager.SelectCamera(CameraType.Playing);
                    }
                    else
                    {
                        uiMode.SetUIMode(UIModeType.Preview, false);
                        cameraManager.SelectCamera(CameraType.Editing);
                    }
                    var cam = cameraManager.CameraControllers[playing ? 1 : 0].Camera;
                    yield return null;

                    for (var i = 0; i < iterations; ++i)
                    {
                        // Wide cross-step excursion then return to just before b372.
                        foreach (var beat in new[] { 378.5f, 320.25f, 371.75f })
                        {
                            atsc.MoveToJsonTime(beat);
                            yield return null;
                        }

                        // Reproduce the exact failing pause edge every iteration: resume into
                        // deterministic playback, sample the ACTUAL beat, pause (which snaps the
                        // cursor to b372), then seek back to the sampled beat.
                        StartPinnedPlayback(atsc);
                        yield return null;
                        yield return null;
                        var resumedBeat = atsc.CurrentJsonTime;
                        atsc.TogglePlaying();
                        yield return null;
                        atsc.MoveToJsonTime(resumedBeat);
                        var phase = $"{modeName} iter{i} pause-edge@{resumedBeat:F3}";
                        CheckBeat372State(noteGrid, tracksManager, cam, playing,
                            expectedParentX, staleParentX, $"{phase} immediate", failures);
                        yield return null; yield return null;
                        CheckBeat372State(noteGrid, tracksManager, cam, playing,
                            expectedParentX, staleParentX, $"{phase} +2f", failures);

                        // Same-chunk backward hop inside the edge each iteration too.
                        atsc.MoveToJsonTime(372f);
                        yield return null;
                        atsc.MoveToJsonTime(371.75f);
                        yield return null; yield return null;
                        CheckBeat372State(noteGrid, tracksManager, cam, playing,
                            expectedParentX, staleParentX,
                            $"{modeName} iter{i} same-chunk 372->371.75", failures);

                        // Strict Playing A/B on the authored b372 note x2y0 c0 d3 at the frozen
                        // b371.75 seek state: body renderers must rasterize into the camera.
                        if (playing && i == 0)
                        {
                            var target = noteGrid.LoadedContainers.Values
                                .OfType<Beatmap.Containers.NoteContainer>()
                                .FirstOrDefault(c => c.NoteData != null
                                    && Mathf.Approximately(c.NoteData.JsonTime, 372f)
                                    && c.NoteData.PosX == 2 && c.NoteData.PosY == 0
                                    && c.NoteData.Type == 0 && c.NoteData.CutDirection == 3
                                    && c.NoteData.CustomFake == false);
                            var group = target == null
                                ? new List<Renderer>()
                                : (target.ModelController.MpbController.Renderers
                                    ?? Enumerable.Empty<Renderer>())
                                    .Where(r => r != null && r.enabled
                                        && r.gameObject.activeInHierarchy)
                                    .ToList();
                            var cutout = target != null && target.MpbController != null
                                ? target.MpbController.Mpb.GetFloat(cutoutId) : float.NaN;
                            if (group.Count == 0 || !(cutout < 0.98f))
                            {
                                failures.Add($"Playing A/B @371.75: b372 x2y0 c0 d3 body renderers " +
                                    $"unavailable for pixel proof (group={group.Count} " +
                                    $"cutout={cutout:F6}).");
                            }
                            else
                            {
                                var delta = CaptureGroupDelta(cam, group);
                                Debug.Log($"=== A/B pixel proof: Playing b371.75 b372note " +
                                    $"maxDiff={delta.MaxDiff} pixels={delta.Count} ===");
                                if (delta.Count < 10)
                                    failures.Add($"Playing A/B @371.75: b372 note changed " +
                                        $"{delta.Count} pixels (maxDiff={delta.MaxDiff}), " +
                                        "expected >=10.");
                            }
                        }
                    }
                }
            }
            finally
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying)
                {
                    atsc.CancelPlaying();
                }

                TearDownVirtualInput();
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Verifies the three authored b372 real notes stay loaded/active/rendered at the frozen
        // seek beat, and that the note/fakenote track parents still hold the b370.05 step world x
        // (~580197.6) rather than a stale future step (~4165241.4).
        private static void CheckBeat372State(
            NoteGridContainer grid, TracksManager tracksManager, Camera cam, bool playing,
            float expectedParentX, float staleParentX, string phase, List<string> failures)
        {
            var frustum = GeometryUtility.CalculateFrustumPlanes(cam);
            foreach (var (b, x, y, c, d) in new[]
                { (372f, 2, 0, 0, 3), (372f, 3, 0, 0, 3), (372f, 2, 2, 1, 5) })
            {
                var key = grid.LoadedContainers.Keys.OfType<Beatmap.Base.BaseNote>()
                    .FirstOrDefault(k => Mathf.Abs(k.JsonTime - b) < 0.01f
                        && k.PosX == x && k.PosY == y && k.Type == c && k.CutDirection == d);
                if (key == null)
                {
                    failures.Add($"{phase}: b372 note x{x}y{y} c{c} d{d} missing from " +
                        $"LoadedContainers (loaded={grid.LoadedContainers.Count}).");
                    continue;
                }
                var con = (Beatmap.Containers.NoteContainer)grid.LoadedContainers[key];
                var active = con.gameObject.activeInHierarchy;
                var renderers = con.ModelController.MpbController.Renderers;
                var activeRenderers = renderers != null
                    ? renderers.Count(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                    : 0;
                var mpb = con.MpbController != null ? con.MpbController.Mpb : null;
                var cutout = mpb != null ? mpb.GetFloat(cutoutId) : float.NaN;
                var body = renderers != null
                    ? renderers.FirstOrDefault(r => r != null && r.enabled) : null;
                var inFrustum = body != null && GeometryUtility.TestPlanesAABB(frustum, body.bounds);
                if (!active || activeRenderers == 0)
                    failures.Add($"{phase}: b372 note x{x}y{y} c{c} d{d} not rendered " +
                        $"(active={active} renderers={activeRenderers}).");
                if (playing && !inFrustum)
                    failures.Add($"{phase}: b372 note x{x}y{y} c{c} d{d} body out of Playing " +
                        $"camera frustum (cutout={cutout:F6}).");
            }

            foreach (var name in new[] { "note", "fakenote" })
            {
                if (tracksManager == null
                    || !tracksManager.TryGetAnimationTrack(name, out var ta) || ta == null
                    || ta.Track.ObjectParentTransform == null)
                {
                    continue;
                }
                var x = ta.Track.ObjectParentTransform.position.x;
                if (Mathf.Abs(x - expectedParentX) > 1f)
                {
                    failures.Add($"{phase}: track '{name}' parent x={x:F3}, expected " +
                        $"{expectedParentX:F1} (b370.05 step) - landed on a different step.");
                }
                if (Mathf.Abs(x - staleParentX) < 2f)
                {
                    failures.Add($"{phase}: track '{name}' parent x={x:F3} matches the FUTURE " +
                        "b372.05 step - evaluated the wrong authored event.");
                }
            }

            // A b379.2 fakenote rendered at the future step's x is direct evidence the parent
            // composed a later authored event early.
            foreach (var pair in grid.LoadedContainers)
            {
                if (pair.Value is not Beatmap.Containers.NoteContainer note || note.NoteData == null)
                    continue;
                if (Mathf.Abs(note.NoteData.JsonTime - 379.2f) > 0.05f) continue;
                var px = note.transform.position.x;
                if (Mathf.Abs(px - staleParentX) < 2f)
                {
                    failures.Add($"{phase}: b379.2 fakenote sits at future-step x={px:F3} " +
                        $"(expected near {expectedParentX:F1}).");
                }
            }
        }

        // Rapid scrub through the authored seek sequence that produced the 200731 failure: a
        // forward hop past the b372.05 step event (378.5), far backward (320.25), returns to the
        // b372 boundary, the same-chunk 372.1->371.8 hop, and returns at the exact resumed edge.
        // Every return to ~371.8 must keep the authored b372 triple loaded/rendered with the
        // note/fakenote parents on the b370.05 step (x 580197.6), never the future 4165241.4.
        [UnityTest]
        public IEnumerator RapidCrossStepScrollKeepsBeat372Notes()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(noteGrid, Is.Not.Null, "NoteGridContainer missing.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "CurrentSeconds property not found.");

            InitializeVirtualInput(includeKeyboard: false);
            var failures = new List<string>();
            const float expectedParentX = 580197.6f;
            const float staleParentX = 4165241.4f;
            var sequence = new[]
            {
                371.75f, 378.5f, 320.25f, 371.75f, 372.1f, 371.8f, 352.25f, 348.25f, 370.25f, 371.82f,
            };
            try
            {
                foreach (var (playing, iterations) in new[] { (true, 8), (false, 4) })
                {
                    var modeName = playing ? "Playing" : "Preview";
                    if (playing)
                    {
                        uiMode.SetUIMode(UIModeType.Playing, false);
                        cameraManager.SelectCamera(CameraType.Playing);
                    }
                    else
                    {
                        uiMode.SetUIMode(UIModeType.Preview, false);
                        cameraManager.SelectCamera(CameraType.Editing);
                    }
                    var cam = cameraManager.CameraControllers[playing ? 1 : 0].Camera;
                    yield return null;

                    for (var i = 0; i < iterations; ++i)
                    {
                        foreach (var beat in sequence)
                        {
                            atsc.MoveToJsonTime(beat);
                            yield return null;
                            if (beat >= 371.5f && beat < 372f)
                            {
                                var phase = $"{modeName} rapid{i} return@{beat:F2}";
                                CheckBeat372State(noteGrid, tracksManager, cam, playing,
                                    expectedParentX, staleParentX, $"{phase} immediate", failures);
                                yield return null;
                                CheckBeat372State(noteGrid, tracksManager, cam, playing,
                                    expectedParentX, staleParentX, $"{phase} +1f", failures);
                            }
                        }

                        // Pause-edge at the end of each sequence: resume into deterministic
                        // playback, pause (cursor snaps to b372), seek back to the actual beat.
                        StartPinnedPlayback(atsc);
                        yield return null;
                        yield return null;
                        var resumedBeat = atsc.CurrentJsonTime;
                        atsc.TogglePlaying();
                        yield return null;
                        atsc.MoveToJsonTime(resumedBeat);
                        yield return null;
                        CheckBeat372State(noteGrid, tracksManager, cam, playing,
                            expectedParentX, staleParentX,
                            $"{modeName} rapid{i} pause-edge@{resumedBeat:F3}", failures);
                    }
                }
            }
            finally
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying)
                {
                    atsc.CancelPlaying();
                }

                TearDownVirtualInput();
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // UI-equivalent snapped scrolling: real scrolls run MoveToJsonTime + SnapToGrid, which
        // resolves the VisualBeatOrigin-relative grid line and skips ValidatePosition - a
        // different seek path than the direct MoveToJsonTime stress above. Hammer the authored
        // scroll sequence across mode transitions so the intermittent b372 triple unload and
        // stale 4165241.4 parent step would surface as a reliable RED.
        [UnityTest]
        public IEnumerator SnappedScrollKeepsBeat372Notes()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(noteGrid, Is.Not.Null, "NoteGridContainer missing.");

            InitializeVirtualInput(includeKeyboard: false);
            var snappingBefore = atsc.GridMeasureSnapping;
            var failures = new List<string>();
            const float expectedParentX = 580197.6f;
            const float staleParentX = 4165241.4f;
            var scrollBeats = new[]
            {
                320f, 320.25f, 322.25f, 348f, 348.25f, 351.75f, 352f, 352.25f,
                370f, 371.75f, 372.25f, 371.75f, 348.25f, 371.75f,
            };
            editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            Assert.That(editModeContext, Is.Not.Null, "EditModeContext missing.");
            editingModeBeforeTest = editModeContext.EditingMode;
            editingModeCaptured = true;
            try
            {
                atsc.GridMeasureSnapping = 4;
                foreach (var (playing, iterations) in new[] { (true, 5), (false, 2), (true, 2) })
                {
                    var modeName = playing ? "Playing" : "Preview";
                    if (playing)
                    {
                        // Match the live repro: enter Playing from the BasicEvent workspace;
                        // SetUIMode must switch the editor to Gameplay so note grids exist.
                        editModeContext.EditingMode = EditingMode.BasicEvent;
                        yield return null;
                        uiMode.SetUIMode(UIModeType.Playing, false);
                        yield return null;
                        Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.Playing), modeName);
                        Assert.That(editModeContext.EditingMode, Is.EqualTo(EditingMode.Gameplay),
                            $"{modeName}: Playing entry did not switch BasicEvent workspace to Gameplay.");
                        Assert.That(noteGrid.gameObject.activeInHierarchy, Is.True,
                            $"{modeName}: note grid stayed disabled after BasicEvent->Playing.");
                        cameraManager.SelectCamera(CameraType.Playing);
                    }
                    else
                    {
                        // Preview restores the BasicEvent workspace; return to Gameplay so notes are visible.
                        uiMode.SetUIMode(UIModeType.Preview, false);
                        yield return null;
                        editModeContext.EditingMode = EditingMode.Gameplay;
                        yield return null;
                        Assert.That(editModeContext.EditingMode, Is.EqualTo(EditingMode.Gameplay),
                            "Preview: Gameplay workspace not active.");
                        Assert.That(noteGrid.gameObject.activeInHierarchy, Is.True,
                            "Preview: note grid stayed disabled after workspace restore.");
                        cameraManager.SelectCamera(CameraType.Editing);
                    }
                    var cam = cameraManager.CameraControllers[playing ? 1 : 0].Camera;
                    yield return null;

                    for (var i = 0; i < iterations; ++i)
                    {
                        foreach (var beat in scrollBeats)
                        {
                            atsc.MoveToJsonTime(beat);
                            atsc.SnapToGrid(true);
                            yield return null;
                            var snapped = atsc.CurrentJsonTime;

                            if (Mathf.Approximately(snapped, 371.75f))
                            {
                                var phase = $"{modeName} snap{i} @{beat:F2}->{snapped:F3}";
                                CheckBeat372State(noteGrid, tracksManager, cam, playing,
                                    expectedParentX, staleParentX, $"{phase} immediate", failures);
                                yield return null;
                                CheckBeat372State(noteGrid, tracksManager, cam, playing,
                                    expectedParentX, staleParentX, $"{phase} +1f", failures);
                            }
                            else if (Mathf.Approximately(snapped, 351.75f)
                                || Mathf.Approximately(snapped, 352.25f))
                            {
                                // Non-vacuous check: authored upcoming b352/b354 real notes must be
                                // loaded with a body at these snapped beats.
                                var upcoming = authoredWindowNotes
                                    .Where(n => (Mathf.Approximately(n.b, 352f)
                                        || Mathf.Approximately(n.b, 354f))
                                        && n.b >= snapped - 0.5f)
                                    .ToList();
                                var rendered = upcoming.Count(n =>
                                    noteGrid.LoadedContainers.Keys.OfType<Beatmap.Base.BaseNote>().Any(k =>
                                        Mathf.Abs(k.JsonTime - n.b) < 0.01f && k.PosX == n.x
                                        && k.PosY == n.y && k.Type == n.c && k.CutDirection == n.d
                                        && noteGrid.LoadedContainers[k].gameObject.activeInHierarchy));
                                if (upcoming.Count > 0 && rendered == 0)
                                    failures.Add($"{modeName} snap{i} @{snapped:F3}: none of " +
                                        $"{upcoming.Count} authored b352/b354 notes loaded+active.");
                            }
                        }
                    }
                }
            }
            finally
            {
                atsc.GridMeasureSnapping = snappingBefore;
                atsc.StopScheduled = false;
                if (atsc.IsPlaying)
                {
                    atsc.CancelPlaying();
                }

                TearDownVirtualInput();
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private sealed class NoteSnapshot
        {
            public bool active;
            public bool rendered;
            public Vector3 pos;
            public Vector3 localPos;
            public Quaternion rot;
            public float cutout;
            public bool inFrustum;
            public bool visible;
        }

        // DIAGNOSTIC ONLY (b372 missing-after-seek probe): dumps paused-filter inputs, b372 note
        // membership, and the notes+player/note/fakenote animator state. Data-only - never calls
        // Aggregator.Get() or any production writer.
        private static void DumpB372Diag(
            NoteGridContainer grid, AudioTimeSyncController atsc, Camera cam, string phase)
        {
            var time = atsc.CurrentSongBpmTime;
            var spawnOffset = grid.SpawnCallbackController.Offset;
            var despawnOffset = grid.DespawnCallbackController.Offset;
            var despawnFloor = time + despawnOffset;
            var spawnCap = time + spawnOffset;
            var walkStart = grid.MapObjects.FindIndex(o => o.SongBpmTime >= spawnCap);
            var spawnFloor = walkStart >= 0 && walkStart < grid.MapObjects.Count
                ? grid.MapObjects[walkStart].JsonTime
                : float.PositiveInfinity;

            var noteLines = new List<string>();
            foreach (var n in BeatSaberSongContainer.Instance.Map.Notes
                .OfType<Beatmap.Base.BaseNote>()
                .Where(n => Mathf.Abs(n.JsonTime - 372f) < 0.01f))
            {
                var idx = grid.MapObjects.IndexOf(n);
                var loaded = grid.LoadedContainers.TryGetValue(n, out var con);
                var active = loaded && con.gameObject.activeInHierarchy;
                var renderers = loaded && con is Beatmap.Containers.NoteContainer nc
                    ? nc.ModelController.MpbController.Renderers?.Count(r => r != null && r.enabled) ?? 0
                    : -1;
                var inCap = n.SongBpmTime >= despawnFloor && n.SongBpmTime <= spawnCap;
                var pastFloor = n.JsonTime < spawnFloor;
                var lookahead = UIMode.AnimationMode
                    ? Mathf.Max(n.HalfJumpDuration, spawnOffset) + Track.JUMP_TIME
                    : spawnOffset;
                var inLookahead = n.SongBpmTime <= time + lookahead;
                noteLines.Add($"  {Describe(n)} idx={idx} bpm={n.SongBpmTime:F3} hjd={n.HalfJumpDuration:F3} " +
                    $"loaded={loaded} active={active} rend={renderers} inCap={inCap} " +
                    $"pastFloor={pastFloor} look={lookahead:F2} inLook={inLookahead} " +
                    $"=> eligible={inCap || (!pastFloor && inLookahead)}");
            }

            var trackLines = new List<string>();
            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            foreach (var name in new[] { "notes+player", "note", "fakenote" })
            {
                if (tracksManager == null
                    || !tracksManager.TryGetAnimationTrack(name, out var ta) || ta == null)
                {
                    trackLines.Add($"  track={name}: <missing>");
                    continue;
                }
                var posVal = "<none>";
                if (ta.AnimatedProperties.TryGetValue("_position", out var prop)
                    && prop is Beatmap.Animations.AnimateProperty<Vector3> vp)
                {
                    posVal = $"raw={vp.GetLerpedValue(atsc.CurrentJsonTime):F3} " +
                        $"points={vp.PointDefinitions.Sum(pd => pd.Points.Length)}";
                }
                var anim = ta.Animator;
                trackLines.Add($"  track={name} ver={ta.UpdateVersion} props=[{string.Join(",", ta.AnimatedProperties.Keys)}] " +
                    $"posEval={posVal} en={ta.isActiveAndEnabled} " +
                    $"selfL={(ta.Track.SelfTransform != null ? ta.Track.SelfTransform.localPosition.ToString("F2") : "<null>")} " +
                    $"selfW={(ta.Track.SelfTransform != null ? ta.Track.SelfTransform.position.ToString("F2") : "<null>")} " +
                    $"optL={(ta.Track.ObjectParentTransform != null ? ta.Track.ObjectParentTransform.localPosition.ToString("F2") : "<null>")} " +
                    $"optW={(ta.Track.ObjectParentTransform != null ? ta.Track.ObjectParentTransform.position.ToString("F2") : "<null>")} " +
                    $"animEn={(anim != null ? anim.isActiveAndEnabled.ToString() : "<null>")} " +
                    $"wpN={(anim != null ? anim.WorldPosition.Count : -1)}/{(anim != null ? anim.WorldPosition.Keep : -1)} " +
                    $"opN={(anim != null ? anim.OffsetPosition.Count : -1)}/{(anim != null ? anim.OffsetPosition.Keep : -1)} " +
                    $"lpN={(anim != null ? anim.LocalPosition.Count : -1)}/{(anim != null ? anim.LocalPosition.Keep : -1)}");
            }

            var lastSteps = BeatSaberSongContainer.Instance.Map.CustomEvents
                .Where(e => e.Type == "AnimateTrack"
                    && e.Data != null
                    && e.Data.HasKey("_track")
                    && e.Data["_track"].Value == "notes+player"
                    && e.JsonTime <= atsc.CurrentJsonTime + 0.5f)
                .OrderByDescending(e => e.JsonTime)
                .Take(3)
                .OrderBy(e => e.JsonTime)
                .Select(e => $"b{e.JsonTime:F2}->{e.Data["_position"].ToString()}")
                .ToList();

            Debug.Log($"[B372Diag] {phase}: json={atsc.CurrentJsonTime:F3} bpm={time:F3} " +
                $"playing={atsc.IsPlaying} animMode={UIMode.AnimationMode} gridEn={grid.enabled} " +
                $"spawnOff={spawnOffset:F3} despawnOff={despawnOffset:F3} spawnCap={spawnCap:F3} " +
                $"despawnFloor={despawnFloor:F3} walkStart={walkStart} spawnFloor={spawnFloor:F3} " +
                $"camPos={cam.transform.position} loaded={grid.LoadedContainers.Count} " +
                $"lastParentSteps=[{string.Join("; ", lastSteps)}]\n" +
                string.Join("\n", noteLines) + "\n" + string.Join("\n", trackLines));
        }

        private static void CompareNoteSnapshots(
            Dictionary<Beatmap.Base.BaseObject, NoteSnapshot> expected,
            Dictionary<Beatmap.Base.BaseObject, NoteSnapshot> actual,
            string phase,
            List<string> failures)
        {
            foreach (var key in expected.Keys.Union(actual.Keys))
            {
                var hasE = expected.TryGetValue(key, out var e);
                var hasA = actual.TryGetValue(key, out var a);
                // Only notes that were actually visually contributing count - offscreen preload
                // membership differences between playback and seek histories are not defects.
                var matteredE = hasE && e.visible;
                var matteredA = hasA && a.visible;
                if (!matteredE && !matteredA) continue;
                // Visibility thresholds at cutout ~0.98 are phase-boundary edges: only when BOTH
                // cutouts sit inside [0.95, 1.01] with abs diff <= 0.01, frustum membership agrees,
                // and pose/rendered state is identical does a visibility-flag flip count as
                // informational. Any other flip - including frustum changes or larger cutout
                // deltas - is a real defect and fails below.
                if (hasE && hasA && e.rendered == a.rendered && e.inFrustum == a.inFrustum
                    && Vector3.Distance(e.pos, a.pos) <= 1f
                    && Vector3.Distance(e.localPos, a.localPos) <= 0.1f
                    && Quaternion.Angle(e.rot, a.rot) <= 5f
                    && float.IsFinite(e.cutout) && float.IsFinite(a.cutout)
                    && e.cutout >= 0.95f && e.cutout <= 1.01f
                    && a.cutout >= 0.95f && a.cutout <= 1.01f
                    && Mathf.Abs(e.cutout - a.cutout) <= 0.01f)
                {
                    if (matteredE != matteredA)
                        Debug.Log($"[S15Parity] {phase}: '{Describe(key)}' visibility-threshold edge " +
                            $"(visible {matteredE}->{matteredA} cutout {e.cutout:F6}->{a.cutout:F6}) " +
                            "- informational, identical body state within tolerances.");
                    continue;
                }
                if (matteredE != matteredA)
                {
                    failures.Add($"{phase}: '{Describe(key)}' visible differs " +
                        $"(visible {matteredE}->{matteredA} loaded {hasE}->{hasA} " +
                        $"pos {(hasE ? e.pos : Vector3.zero)}->{(hasA ? a.pos : Vector3.zero)} " +
                        $"cutout {(hasE ? e.cutout : float.NaN):F6}->{(hasA ? a.cutout : float.NaN):F6}).");
                    continue;
                }
                if (e.rendered != a.rendered)
                {
                    failures.Add($"{phase}: '{Describe(key)}' rendered differs " +
                        $"({e.rendered}->{a.rendered}).");
                    continue;
                }
                // World x lives in the millions on the notes+player parent, so the absolute world
                // tolerance is >=1m; track-local pose, identity, rendered and cutout stay strict.
                var dPos = Vector3.Distance(e.pos, a.pos);
                var dLocal = Vector3.Distance(e.localPos, a.localPos);
                var dRot = Quaternion.Angle(e.rot, a.rot);
                var dCut = Mathf.Abs(e.cutout - a.cutout);
                if (dPos > 1f || dLocal > 0.1f || dRot > 5f || (float.IsFinite(dCut) && dCut > 0.05f))
                    failures.Add($"{phase}: '{Describe(key)}' pose differs " +
                        $"(pos {e.pos}->{a.pos} dPos={dPos:F3} local {e.localPos}->{a.localPos} " +
                        $"dLocal={dLocal:F3} rotAngle={dRot:F1} cutout {e.cutout:F6}->{a.cutout:F6}).");
            }
        }

        private static Dictionary<Beatmap.Base.BaseObject, NoteSnapshot> CaptureRenderedNotes(
            NoteGridContainer grid, Camera cam, string phase)
        {
            var snapshot = new Dictionary<Beatmap.Base.BaseObject, NoteSnapshot>();
            var interesting = new List<string>();
            var frustum = GeometryUtility.CalculateFrustumPlanes(cam);
            foreach (var pair in grid.LoadedContainers)
            {
                if (pair.Value is not Beatmap.Containers.NoteContainer note || note.NoteData == null)
                    continue;
                var front = note.DirectionTarget != null ? note.DirectionTarget : note.transform;
                var pos = front.position;
                var renderers = note.ModelController.MpbController.Renderers;
                var activeRenderers = renderers != null
                    ? renderers.Count(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                    : 0;
                var bodyInFrustum = renderers != null && renderers.Any(r =>
                    r != null && r.enabled && r.gameObject.activeInHierarchy
                    && GeometryUtility.TestPlanesAABB(frustum, r.bounds));
                var mpb = note.MpbController != null ? note.MpbController.Mpb : null;
                var cutout = mpb != null ? mpb.GetFloat(cutoutId) : float.NaN;
                var visible = note.gameObject.activeInHierarchy && activeRenderers > 0
                    && bodyInFrustum && (!float.IsFinite(cutout) || cutout < 0.98f);
                snapshot[pair.Key] = new NoteSnapshot
                {
                    active = note.gameObject.activeInHierarchy,
                    rendered = note.gameObject.activeInHierarchy && activeRenderers > 0,
                    pos = pos,
                    localPos = front.localPosition,
                    rot = front.rotation,
                    cutout = cutout,
                    inFrustum = bodyInFrustum,
                    visible = visible,
                };
                var data = note.NoteData;
                if (data.JsonTime >= 314f && data.JsonTime <= 376f)
                {
                    interesting.Add($"{Describe(pair.Key)} active={note.gameObject.activeInHierarchy} " +
                        $"renderers={activeRenderers} pos={pos} local={front.localPosition} " +
                        $"cutout={cutout:F6} inFrustum={bodyInFrustum} visible={visible}");
                }
            }
            Debug.Log($"[S15Diag] {phase}: loadedNotes={snapshot.Count} rendered=" +
                $"{snapshot.Values.Count(s => s.rendered)}\n  " + string.Join("\n  ", interesting));
            return snapshot;
        }

        private static string Describe(Beatmap.Base.BaseObject obj)
        {
            if (obj is Beatmap.Base.BaseNote n)
            {
                var track = n.CustomTrack is JSONString s ? s.Value : "-";
                return $"b{n.JsonTime:F3} track={track} type={n.Type} d{n.CutDirection} " +
                    $"x{n.PosX}y{n.PosY} fake={n.CustomFake}";
            }
            return $"b{obj.JsonTime:F3} {obj.GetType().Name}";
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

        private void StartPinnedPlayback(AudioTimeSyncController atsc)
        {
            if (playbackClock == null)
            {
                playbackClock = atsc;
                playbackClockWasEnabled = atsc.enabled;
            }

            atsc.enabled = false;
            atsc.TogglePlaying();
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            Assert.That(atsc.IsPlaying, Is.True);
            Assert.That(atsc.SongAudioSource.isPlaying, Is.False,
                "Pinned playback must not depend on the native audio backend.");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (editModeContext != null && editingModeCaptured)
            {
                editModeContext.EditingMode = editingModeBeforeTest;
                editingModeCaptured = false;
            }
            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
                if (editingCameraMoved)
                {
                    cameraManager.CameraControllers[0].Camera.transform.SetPositionAndRotation(
                        previousEditingCameraPosition, previousEditingCameraRotation);
                    editingCameraMoved = false;
                }
            }
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null)
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }

            if (playbackClock != null)
            {
                playbackClock.enabled = playbackClockWasEnabled;
                playbackClock = null;
            }

            TearDownVirtualInput();
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = previousPlayerCameraFOV;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerCameraOffsetZ;
            Settings.Instance.CameraFOV = previousCameraFOV;
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

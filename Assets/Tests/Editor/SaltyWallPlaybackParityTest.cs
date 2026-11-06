using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "G1ll35 d3 R415", mapped by Salty (BeatSaver ID: 4b476).
    // The fixture retains the sampled objects, their track parents, and all referenced custom data.
    public class SaltyWallPlaybackParityTest : InputTestBase
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
        private float previousCaptureDeltaTime;

        private static string FixturePath => PathUtils.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "SaltyWallPlaybackFixture.json");

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

        [SetUp]
        public void EnablePreviewAnimations()
        {
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Settings.Instance.Animations = true;
        }

        protected override void CleanupTestObjects()
        {
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

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            Time.captureDeltaTime = previousCaptureDeltaTime;
            TearDownVirtualInput();
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
        private static readonly int cutoutPropertyId = Shader.PropertyToID("_Cutout");

        [UnityTest]
        public IEnumerator Beat221FakeWallsMatchPlaybackPoseAfterRepeatedScrubs(
            [Values(0f, 0.1f)] float frameDuration)
        {
            Time.captureDeltaTime = frameDuration;
            // Batchmode has no input devices; UIMode.OnPlayToggle -> SetLockState reads
            // Mouse.current when toggling playback in Playing mode. A virtual mouse keeps the
            // production toggle path intact without touching production code.
            InitializeVirtualInput(includeKeyboard: true);
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var obstacleGrid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(obstacleGrid, Is.Not.Null, "ObstacleGridContainer missing.");

            var authoredWalls = BeatSaberSongContainer.Instance.Map.Obstacles
                .Where(o => o.CustomFake && Mathf.Approximately(o.JsonTime, 221f))
                .ToList();
            Assert.That(authoredWalls.Count, Is.EqualTo(9),
                $"expected 9 authored b221 fake walls, found {authoredWalls.Count}: " +
                string.Join("; ", authoredWalls.Select(DescribeObstacle)));

            var failures = new List<string>();
            var sampleTargets = new[] { 219.5f, 221.25f, 223.25f };
            try
            {
                using var playbackClock = new DeterministicPlaybackClock(atsc);
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
                    playbackClock.StartAtJsonTime(216f);
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
                        playbackClock.AdvanceToJsonTime(target);
                        yield return null; yield return null;
                        Assert.That(
                            atsc.CurrentJsonTime,
                            Is.EqualTo(target).Within(0.0001f),
                            "Rendered frames advanced beyond the test-owned playback sample.");
                        if (atsc.CurrentJsonTime < sampleTargets[nextSample] - 0.001f) continue;
                        var sampled = atsc.CurrentJsonTime;
                        var snap = CaptureWalls(obstacleGrid, authoredWalls);
                        playbackSnapshots[sampleTargets[nextSample]] = (sampled, snap);
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
                    playbackClock.Pause();
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

                        playbackClock.Resume();
                        yield return null; yield return null;
                        Assert.That(atsc.CurrentJsonTime, Is.EqualTo(sampled).Within(0.0001f));
                        Assert.That(atsc.SongAudioSource.isPlaying, Is.False);
                        var resumedBeat = atsc.CurrentJsonTime;
                        var resumedSnap = CaptureWalls(obstacleGrid, authoredWalls);
                        playbackClock.Pause();
                        yield return null;
                        atsc.MoveToJsonTime(resumedBeat);
                        yield return null; yield return null;
                        CompareWallSnapshots(
                            CaptureWalls(obstacleGrid, authoredWalls),
                            resumedSnap,
                            $"{modeName} resumed@{resumedBeat:F3} vs seek",
                            failures);
                    }
                }
            }
            finally
            {
                TearDownVirtualInput();
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
        public IEnumerator Beat337FakeWallsMatchPlaybackShowHideAfterScrubs(
            [Values(0f, 0.1f)] float frameDuration)
        {
            Time.captureDeltaTime = frameDuration;
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
        public IEnumerator Beat529TimingWindowAndBeat609WallsMatchPlaybackAfterScrubs(
            [Values(0, 32)] int audioLatency)
        {
            Settings.Instance.AudioLatencyCompensation = audioLatency;
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
            InitializeVirtualInput(includeKeyboard: true);
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var obstacleGrid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(obstacleGrid, Is.Not.Null, "ObstacleGridContainer missing.");

            var failures = new List<string>();
            try
            {
                using var playbackClock = new DeterministicPlaybackClock(atsc);
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

                    playbackClock.StartAtJsonTime(playbackStart);
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
                        playbackClock.AdvanceToJsonTime(target);
                        yield return null; yield return null;
                        Assert.That(
                            atsc.CurrentJsonTime,
                            Is.EqualTo(target).Within(0.0001f),
                            "Rendered frames advanced beyond the test-owned playback sample.");
                        if (atsc.CurrentJsonTime < sampleTargets[nextSample] - 0.001f) continue;
                        var sampled = atsc.CurrentJsonTime;
                        var snap = CaptureWalls(obstacleGrid, authored);
                        playbackSnapshots[sampleTargets[nextSample]] = (sampled, snap);
                        var loaded = snap.Values.Count(s => s.loaded);
                        var shown = snap.Values.Count(s =>
                            s.loaded && s.active && s.coreEnabled && s.cutout < 0.98f);
                        if (shownTargets.Contains(sampleTargets[nextSample]) && shown == 0)
                        {
                            failures.Add($"{label} {modeName} playback@{sampled:F3}: 0/{authored.Count} " +
                                "authored walls actively contributing during continuous playback " +
                                "where authored dissolve shows them (non-vacuous baseline).");
                        }
                        nextSample++;
                    }
                    playbackClock.Pause();
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
                            }
                            else
                            {
                                var delta = CaptureGroupDelta(playingCam, group);
                                if (delta.Count < 10)
                                {
                                    failures.Add($"{label} {modeName} A/B @{sampled:F3}: in-view " +
                                        $"group changed {delta.Count} pixels (maxDiff={delta.MaxDiff}), " +
                                        "expected >=10.");
                                }
                            }
                        }

                        playbackClock.Resume();
                        yield return null; yield return null;
                        Assert.That(atsc.CurrentJsonTime, Is.EqualTo(sampled).Within(0.0001f));
                        Assert.That(atsc.SongAudioSource.isPlaying, Is.False);
                        var resumedBeat = atsc.CurrentJsonTime;
                        var resumedSnap = CaptureWalls(obstacleGrid, authored);
                        playbackClock.Pause();
                        yield return null;
                        atsc.MoveToJsonTime(resumedBeat);
                        yield return null; yield return null;
                        CompareWallSnapshots(
                            CaptureWalls(obstacleGrid, authored),
                            resumedSnap,
                            $"{label} {modeName} resumed@{resumedBeat:F3} vs seek",
                            failures);
                    }
                }
            }
            finally
            {
                TearDownVirtualInput();
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

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

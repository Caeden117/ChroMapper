using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "DELETE IT ALL", mapped by Mawntee (BeatSaver ID: 4cf62).
    public class DeleteItAllNoteLookParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;
        private Vector3 previousEditingCameraPosition;
        private Quaternion previousEditingCameraRotation;
        private bool editingCameraMoved;
        private bool restoreEndingFixture;
        private AudioTimeSyncController playbackClock;
        private bool previousClockEnabled;

        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "DeleteItAllEndingDotFacingFixture.json");

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
                beatsPerMinute: 155,
                environmentName: "InterscopeEnvironment",
                songLengthSeconds: 250);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator SpawnFacingUsesTimeWhenDefinitePositionDoesNotTravel()
        {
            var path = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "NoteSpawnFacingClockFixture.json");
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(path)), beatsPerMinute: 150);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var head = cameraManager.SelectedCameraController.transform.position;
            var failures = new List<string>();
            Vector3? pinnedPosition = null;
            foreach (var sample in new[] { (Beat: 0.2f, Progress: 0.001f), (Beat: 0.7f, Progress: 0.05f), (Beat: 1.2f, Progress: 0.125f),
                         (Beat: 2.2f, Progress: 0.499f), (Beat: 0.2f, Progress: 0.001f) })
            {
                yield return SeekTo(sample.Beat);
                var loaded = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<Beatmap.Containers.NoteContainer>().Where(note => Mathf.Abs(note.NoteData.JsonTime - 10f) < 0.001f).ToArray();
                Assert.That(loaded.Length, Is.EqualTo(2), "Both normal and stationary notes must remain loaded.");
                foreach (var note in loaded)
                {
                    note.Animator.LateUpdate();
                    var front = note.DirectionTarget;
                    if (sample.Progress < 0.01f)
                    {
                        // Native NoteJump begins at identity and reaches the twenty-degree middle turn through a sine phase.
                        var angle = Vector3.Angle(front.up, front.parent.up);
                        if (angle > 3f)
                            failures.Add($"spawn time={sample.Progress}: fake={note.NoteData.CustomFake} begins with full cut-direction roll ({angle:F2} degrees).");
                    }
                    else if (sample.Progress < 0.1f)
                    {
                        // At time .05 the native sine phase reaches about53 degrees of this90-degree roll, with a bounded20-degree middle turn.
                        var angle = Vector3.Angle(front.up, front.parent.up);
                        if (angle < 25f || angle > 80f)
                            failures.Add($"spawn time={sample.Progress}: fake={note.NoteData.CustomFake} skipped the native intermediate roll ({angle:F2} degrees).");
                    }
                    else if (sample.Progress > 0.49f)
                    {
                        var adjustedHead = head;
                        adjustedHead.y = Mathf.Lerp(head.y, front.position.y, 0.8f);
                        var angle = Vector3.Angle(front.forward, front.position - adjustedHead);
                        if (angle > 1f)
                            failures.Add($"spawn time={sample.Progress}: fake={note.NoteData.CustomFake} has not reached the native facing ray ({angle:F2} degrees).");
                    }

                    if (note.NoteData.CustomFake)
                    {
                        if (pinnedPosition.HasValue && Vector3.Distance(pinnedPosition.Value, front.position) > 0.001f)
                            failures.Add("The definitePosition control moved while only its animation time changed.");

                        pinnedPosition = front.position;
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator EndingDotsFacePlayingCameraAcrossSeeksAndPreserveRoll()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;

            yield return SeekTo(460f);
            var look = FindFakeDotNote("benjamin", 469f);
            Assert.That(look, Is.Not.Null,
                "No loaded fake blue dot on track 'benjamin' at b469.");
            var hidden = FindFakeDotNote("evilbenjamin", 469f);
            Assert.That(hidden, Is.Not.Null,
                "No loaded fake red dot on track 'evilbenjamin' at b469.001.");
            var noLook = FindFakeDotNote("benjaminblock", 469f);
            Assert.That(noLook, Is.Not.Null,
                "No loaded fake dot on track 'benjaminblock' (disableNoteLook control) at b469.");
            var baselineRight = FindRealNote(470f, 1);
            var baselineDownLeft = FindRealNote(470f, 2);
            Assert.That(baselineRight, Is.Not.Null, "No loaded baseline real note at x1 b470.");
            Assert.That(baselineDownLeft, Is.Not.Null, "No loaded baseline real note at x2 b470.");

            var failures = new List<string>();
            foreach (var beat in new[] { 460f, 468.5f, 460f })
            {
                yield return SeekTo(beat);
                CheckFacing(failures, beat, FindFakeDotNote("benjamin", 469f), camera,
                    expectFacing: true, requireRendered: true);
                CheckFacing(failures, beat, FindFakeDotNote("evilbenjamin", 469.001f), camera,
                    expectFacing: true, requireRendered: false);
                CheckFacing(failures, beat, FindFakeDotNote("benjaminblock", 469f), camera,
                    expectFacing: false, requireRendered: true);
                CheckRoll(failures, beat, baselineRight);
                CheckRoll(failures, beat, baselineDownLeft);
            }

            yield return SeekTo(490f);
            CheckFacing(failures, 490f, FindFakeDotNote("benjamin", 469f), camera,
                expectFacing: true, requireRendered: true);
            CheckFacing(failures, 490f, FindFakeDotNote("benjaminblock", 469f), camera,
                expectFacing: false, requireRendered: true);

            yield return SeekTo(460f);
            CheckFacing(failures, 460f, FindFakeDotNote("benjamin", 469f), camera,
                expectFacing: true, requireRendered: true);
            CheckFacing(failures, 460f, FindFakeDotNote("benjaminblock", 469f), camera,
                expectFacing: false, requireRendered: true);
            var respawnedRight = FindRealNote(470f, 1);
            var respawnedDownLeft = FindRealNote(470f, 2);
            if (respawnedRight == null || respawnedDownLeft == null)
            {
                failures.Add("beat 460 after 490 excursion: baseline notes did not respawn.");
            }
            else
            {
                CheckRoll(failures, 460f, respawnedRight);
                CheckRoll(failures, 460f, respawnedDownLeft);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator PreviewDotsFacePlayerCameraWithoutDrift()
        {
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            var data = JSON.Parse(File.ReadAllText(FixturePath));
            data["customData"]["customEvents"].Add(
                new JSONObject
                {
                    ["b"] = 440f,
                    ["t"] = "AssignPlayerToTrack",
                    ["d"] = new JSONObject { ["track"] = "previewPlayer" }
                });
            data["customData"]["customEvents"].Add(
                new JSONObject
                {
                    ["b"] = 440f,
                    ["t"] = "AnimateTrack",
                    ["d"] = new JSONObject
                    {
                        ["track"] = "previewPlayer",
                        ["duration"] = 40f,
                        ["position"] = JSON.Parse("[[12,0,-6,0],[20,0,-14,1]]")
                    }
                });
            restoreEndingFixture = true;
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                data,
                beatsPerMinute: 155,
                environmentName: "InterscopeEnvironment",
                songLengthSeconds: 250);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            var editing = cameraManager.CameraControllers[0].Camera;
            var player = cameraManager.CameraControllers[1].Camera.transform;
            var playerHome = player.position;
            previousEditingCameraPosition = editing.transform.position;
            previousEditingCameraRotation = editing.transform.rotation;
            editingCameraMoved = true;

            // Playing is a positive control before checking the hidden player's Preview binding.
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return SeekTo(460f);
            Assert.That(
                Vector3.Distance(player.position, ExpectedPlayerPosition(playerHome, 460f)),
                Is.LessThan(0.001f),
                $"Playing control: player={player.position}, home={playerHome}, authored={ExpectedPlayerPosition(playerHome, 460f)}.");
            var failures = new List<string>();
            CheckPlayerFacing(failures, 460f, ExpectedPlayerPosition(playerHome, 460f), "playing control");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            var forwards = new List<Vector3>();
            foreach (var sample in new[]
                     {
                         (Mode: UIModeType.Preview, Beat: 455f),
                         (Mode: UIModeType.Playing, Beat: 460f),
                         (Mode: UIModeType.Preview, Beat: 460f),
                         (Mode: UIModeType.Preview, Beat: 468.5f),
                         (Mode: UIModeType.Preview, Beat: 475f),
                         (Mode: UIModeType.Preview, Beat: 460f),
                         (Mode: UIModeType.Preview, Beat: 439.999f),
                         (Mode: UIModeType.Preview, Beat: 440f),
                         (Mode: UIModeType.Preview, Beat: 440.001f)
                     })
            {
                uiMode.SetUIMode(sample.Mode, false);
                cameraManager.SelectCamera(
                    sample.Mode == UIModeType.Playing
                        ? CameraType.Playing
                        : CameraType.Editing);
                yield return SeekTo(sample.Beat);
                var expectedPlayer = ExpectedPlayerPosition(playerHome, sample.Beat);
                if (Vector3.Distance(player.position, expectedPlayer) > 0.001f)
                    failures.Add($"{sample.Mode} b{sample.Beat}: player={player.position}, authored player={expectedPlayer}.");

                // Keep assignment and late player history separate from this fixture's loaded dot observations.
                if (sample.Beat < 455f || sample.Beat == 475f)
                    continue;

                if (sample.Mode == UIModeType.Preview)
                {
                    foreach (var position in new[] { new Vector3(-60f, 1f, 12f), new Vector3(-90f, 2f, 15f), new Vector3(-55f, 3f, 30f) })
                    {
                        var note = FindFakeDotNote("benjamin", 469f);
                        Assert.That(note, Is.Not.Null, $"Source dot is missing at b{sample.Beat}.");
                        note.Animator.LateUpdate();
                        var beforeRotation = note.DirectionTarget.rotation;
                        var beforePosition = note.DirectionTarget.position;
                        editing.transform.SetPositionAndRotation(
                            position,
                            Quaternion.LookRotation(beforePosition - position));
                        yield return null;
                        yield return null;
                        note.Animator.LateUpdate();
                        if (Quaternion.Angle(beforeRotation, note.DirectionTarget.rotation) > 0.01f)
                            failures.Add($"Preview b{sample.Beat}: moving the editor camera changed note facing.");

                        Assert.That(Vector3.Distance(beforePosition, note.DirectionTarget.position), Is.LessThan(0.001f));
                        CheckPlayerFacing(failures, sample.Beat, expectedPlayer, "moved preview camera");
                    }

                    forwards.Add(FindFakeDotNote("benjamin", 469f).DirectionTarget.forward);
                }

                if (sample.Mode == UIModeType.Preview && (sample.Beat == 460f || sample.Beat == 468.5f))
                    yield return CheckPreviewRollAgainstPlaying(failures, sample.Beat);
            }

            for (var i = 1; i < forwards.Count; i++)
            {
                if (Vector3.Angle(forwards[i - 1], forwards[i]) > 15f)
                    failures.Add("Pinned dot facing drifted while only the source player track advanced or rewound.");
            }

            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            playbackClock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = playbackClock.enabled;
            playbackClock.enabled = false;
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(playbackClock, 455f);
            Quaternion? firstPlaybackFacing = null;
            Vector3? firstPlaybackPosition = null;
            foreach (var beat in new[] { 455f, 460f, 468f, 468.5f, 475f })
            {
                currentSecondsProperty.SetValue(playbackClock, playbackClock.GetSecondsFromBeat(beat));
                editing.transform.position = new Vector3(-90f + (beat - 455f), 2f, 12f);
                yield return null;
                yield return null;
                var expectedPlayer = ExpectedPlayerPosition(playerHome, beat);
                if (Vector3.Distance(player.position, expectedPlayer) > 0.001f)
                    failures.Add($"playing Preview b{beat}: hidden player lost the authored track.");

                if (beat == 475f)
                    continue;

                CheckPlayerFacing(failures, beat, expectedPlayer, "deterministic Preview playback");
                var front = FindFakeDotNote("benjamin", 469f).DirectionTarget;
                if (!firstPlaybackFacing.HasValue)
                {
                    firstPlaybackFacing = front.rotation;
                    firstPlaybackPosition = front.position;
                }
                else if (beat == 468f)
                {
                    Assert.That(Vector3.Distance(front.position, firstPlaybackPosition.Value), Is.LessThan(0.001f));
                    if (Quaternion.Angle(front.rotation, firstPlaybackFacing.Value) < 0.1f)
                        failures.Add("The authored player motion did not change the pinned dot's facing during Preview playback.");
                }
            }

            TestUtils.PauseDeterministicPlayback(playbackClock);
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(player.position, playerHome), Is.LessThan(0.001f), "Normal mode must still detach the player track and restore its home pose.");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private IEnumerator CheckPreviewRollAgainstPlaying(List<string> failures, float beat)
        {
            var previewUp = new List<Vector3>();
            foreach (var lane in new[] { 1, 2 })
            {
                var note = FindRealNote(470f, lane);
                Assert.That(note, Is.Not.Null);
                note.Animator.LateUpdate();
                previewUp.Add(note.DirectionTarget.up);
            }

            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return null;
            yield return null;
            for (var i = 0; i < previewUp.Count; i++)
            {
                var note = FindRealNote(470f, i + 1);
                Assert.That(note, Is.Not.Null);
                note.Animator.LateUpdate();
                if (Vector3.Dot(previewUp[i], note.DirectionTarget.up) < 0.99999f)
                    failures.Add($"b{beat}: Preview changed lane{i + 1}'s authored roll relative to the same player-facing Playing pose.");
            }

            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            yield return null;
            yield return null;
        }

        private static Vector3 ExpectedPlayerPosition(Vector3 home, float beat) => beat < 440f
            ? home
            : home + Vector3.Lerp(new Vector3(12f, 0f, -6f), new Vector3(20f, 0f, -14f), (beat - 440f) / 40f);

        // Noodle uses the player head and adjusts its height 80% toward the note in the animated world frame.
        private static void CheckPlayerFacing(List<string> failures, float beat, Vector3 head, string route)
        {
            var note = FindFakeDotNote("benjamin", 469f);
            Assert.That(note, Is.Not.Null, $"Source dot is missing at b{beat}.");
            Assert.That(note.gameObject.activeInHierarchy, Is.True);
            Assert.That(note.DotModelController.gameObject.activeInHierarchy, Is.True);
            Assert.That(note.ModelController.MpbController.Renderers.Any(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy), Is.True);
            note.Animator.LateUpdate();
            var front = note.DirectionTarget;
            var inverse = Quaternion.Inverse(note.Animator.WorldTarget.rotation);
            var headHeight = Vector3.Dot(inverse * head, Vector3.up);
            var noteHeight = Vector3.Dot(inverse * front.position, Vector3.up);
            head += note.Animator.WorldTarget.rotation * Vector3.up * (Mathf.Lerp(headHeight, noteHeight, 0.8f) - headHeight);
            var ray = front.parent.rotation * (inverse * (front.position - head).normalized);
            var authored = front.parent.rotation * Quaternion.Euler(note.DirectionTargetEuler);
            var look = Quaternion.LookRotation(ray, authored * Vector3.up);
            var progress = Mathf.Clamp01((beat - note.NoteData.SpawnSongBpmTime) / (note.NoteData.HalfJumpDuration * 2f));
            var expected = Quaternion.Lerp(authored, look, progress * 2f);
            if (Quaternion.Angle(front.rotation, expected) > 0.1f)
                failures.Add($"{route} b{beat}: dot follows the viewing camera rather than the authored player ray.");

            var noLook = FindFakeDotNote("benjaminblock", 469f);
            Assert.That(noLook, Is.Not.Null);
            noLook.Animator.LateUpdate();
            Assert.That(
                Quaternion.Angle(noLook.DirectionTarget.rotation, noLook.DirectionTarget.parent.rotation * Quaternion.Euler(noLook.DirectionTargetEuler)),
                Is.LessThan(0.01f),
                "disableNoteLook must retain its authored pose.");
        }

        [UnityTest]
        public IEnumerator PausedPreviewKeepsGameplayNoteSet()
        {
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            var editingCam = cameraManager.CameraControllers[0].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(noteGrid, Is.Not.Null, "NoteGridContainer missing.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "CurrentSeconds property not found.");

            Debug.Log($"[PauseDiagOffsets] UseChunkLoadingWhenPlaying={noteGrid.UseChunkLoadingWhenPlaying} " +
                $"ChunksLoadedWhilePlaying={noteGrid.ChunksLoadedWhilePlaying} " +
                $"spawnOffset={noteGrid.SpawnCallbackController.Offset} " +
                $"despawnOffset={noteGrid.DespawnCallbackController.Offset} " +
                $"spawnUseOffsetFromConfig={(noteGrid.SpawnCallbackController != null ? "see ctrl" : "null")}");

            var failures = new List<string>();
            try
            {
                atsc.MoveToJsonTime(410f);
                var songBeat425 = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(425f);
                playbackClock = atsc;
                previousClockEnabled = atsc.enabled;
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(atsc, songBeat425);
                atsc.enabled = false;
                yield return null;
                yield return null;
                if (!atsc.IsPlaying)
                    failures.Add("setup: deterministic playback did not start.");

                FinalizeNotePoses(noteGrid);
                var playing = CaptureRenderedNotes(noteGrid, editingCam, "playing@425");

                atsc.TogglePlaying();
                var pausedBeat = atsc.CurrentJsonTime;
                yield return null;
                yield return null;
                FinalizeNotePoses(noteGrid);
                var paused = CaptureRenderedNotes(noteGrid, editingCam, $"paused@{pausedBeat:F2}");

                noteGrid.RefreshPool(true);
                yield return null;
                yield return null;
                FinalizeNotePoses(noteGrid);
                var pausedForced =
                    CaptureRenderedNotes(noteGrid, editingCam, $"pausedForced@{pausedBeat:F2}");

                var pausedSongBeat =
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(pausedBeat);
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(atsc, pausedSongBeat);
                yield return null;
                yield return null;
                currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(pausedSongBeat));
                FinalizeNotePoses(noteGrid);
                var resumed = CaptureRenderedNotes(noteGrid, editingCam, $"resumed@{pausedBeat:F2}");

                atsc.TogglePlaying();
                atsc.MoveToJsonTime(460f);
                yield return null;
                yield return null;
                FinalizeNotePoses(noteGrid);
                var pausedSeek = CaptureRenderedNotes(noteGrid, editingCam, "pausedSeek@460");

                var songBeat460 = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(460f);
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(atsc, songBeat460);
                yield return null;
                yield return null;
                currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat460));
                FinalizeNotePoses(noteGrid);
                var resumed460 = CaptureRenderedNotes(noteGrid, editingCam, "resumed@460");

                AssertRenderedSubset(failures, $"paused@{pausedBeat:F2}", paused, $"playing@{pausedBeat:F2}", playing);
                CompareNoteSets(failures, $"pausedForced@{pausedBeat:F2}", pausedForced, $"paused@{pausedBeat:F2}", paused);
                CompareNoteSets(failures, $"paused@{pausedBeat:F2}", paused, $"resumed@{pausedBeat:F2}", resumed);
                CompareNoteSets(failures, "pausedSeek@460", pausedSeek, "resumed@460", resumed460);

                foreach (var label in new[] { "benjamin", "evilbenjamin", "benjaminblock" })
                {
                    var key = FindKeyByTrack(playing, label);
                    if (key == null)
                    {
                        failures.Add($"playing@425: '{label}' fake dot was not rendered before pause.");
                        continue;
                    }
                    if (!paused.TryGetValue(key, out var p) || !p.rendered)
                        failures.Add($"paused@{pausedBeat:F2}: '{label}' fake dot disappeared while paused.");
                    if (!resumed.TryGetValue(key, out var r) || !r.rendered)
                        failures.Add($"resumed@{pausedBeat:F2}: '{label}' fake dot stayed gone after resume.");
                }
            }
            finally
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private sealed class PauseNoteSnapshot
        {
            public bool rendered;
            public Vector3 pos;
            public int activeRenderers;
            public bool dotActive;
        }

        private static void FinalizeNotePoses(NoteGridContainer grid)
        {
            foreach (var container in grid.LoadedContainers.Values)
            {
                if (container is Beatmap.Containers.NoteContainer note
                    && note.gameObject.activeInHierarchy
                    && note.Animator != null
                    && note.Animator.isActiveAndEnabled)
                {
                    note.Animator.LateUpdate();
                }
            }
        }

        private static Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> CaptureRenderedNotes(
            NoteGridContainer grid, Camera cam, string phase)
        {
            var snapshot = new Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot>();
            var lines = new List<string>();
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
                var dotActive = note.DotModelController != null
                    && note.DotModelController.gameObject.activeInHierarchy;
                snapshot[pair.Key] = new PauseNoteSnapshot
                {
                    rendered = note.gameObject.activeInHierarchy && activeRenderers > 0,
                    pos = pos,
                    activeRenderers = activeRenderers,
                    dotActive = dotActive,
                };
                lines.Add($"{Describe(pair.Key)} rendered={note.gameObject.activeInHierarchy && activeRenderers > 0} " +
                    $"renderers={activeRenderers} dot={dotActive} pos={pos} fwd={front.forward}");
            }
            Debug.Log($"[PauseDiag] {phase}: loadedNotes={snapshot.Count} rendered=" +
                $"{snapshot.Values.Count(s => s.rendered)}\n  " + string.Join("\n  ", lines));
            return snapshot;
        }

        private static void AssertRenderedSubset(
            List<string> failures,
            string aLabel,
            Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> a,
            string bLabel,
            Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> b)
        {
            foreach (var kv in a)
            {
                if (kv.Value.rendered && (!b.TryGetValue(kv.Key, out var sb) || !sb.rendered))
                    failures.Add($"{aLabel}: '{Describe(kv.Key)}' rendered but was not rendered " +
                        $"in {bLabel} (pos={kv.Value.pos}).");
            }
        }

        private static void CompareNoteSets(
            List<string> failures,
            string aLabel,
            Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> a,
            string bLabel,
            Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> b)
        {
            foreach (var key in a.Keys.Union(b.Keys))
            {
                var inA = a.TryGetValue(key, out var sa);
                var inB = b.TryGetValue(key, out var sb);
                if (inA && (!inB || !sb.rendered))
                {
                    if (sa.rendered)
                        failures.Add($"{bLabel}: '{Describe(key)}' rendered in {aLabel} but missing there " +
                            $"(pos={sa.pos}).");
                }
                else if (inB && (!inA || !sa.rendered))
                {
                    if (sb.rendered)
                        failures.Add($"{bLabel}: '{Describe(key)}' rendered there but not in {aLabel} " +
                            $"(pos={sb.pos}).");
                }
                else if (inA && inB)
                {
                    var delta = Vector3.Distance(sa.pos, sb.pos);
                    if (sa.rendered != sb.rendered || delta > 0.05f)
                        failures.Add($"{aLabel} vs {bLabel}: '{Describe(key)}' differs " +
                            $"(rendered {sa.rendered}->{sb.rendered} pos {sa.pos}->{sb.pos} delta={delta:F3}).");
                }
            }
        }

        private static Beatmap.Base.BaseObject FindKeyByTrack(
            Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> snapshot, string track) =>
            snapshot.Keys.FirstOrDefault(k => k is Beatmap.Base.BaseNote n && n.CustomFake
                && n.CustomTrack is JSONString s && s.Value == track);

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

        private struct DotPoseSample
        {
            public Vector3 pos;
            public Vector3 forward;
            public Vector3 bodyCenter;
            public Vector3 dotCenter;
        }

        private static DotPoseSample DumpDotPhase(
            Beatmap.Containers.NoteContainer note,
            Camera editingCam,
            Transform playingCam,
            float beat,
            string phase)
        {
            var sample = new DotPoseSample
            {
                pos = note.DirectionTarget.position,
                forward = note.DirectionTarget.forward,
            };
            var renderers = note.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                .ToList();
            var body = renderers.FirstOrDefault(r => r.name.Contains("CubeNote"));
            var dot = renderers.FirstOrDefault(r =>
                note.DotModelController != null
                    && r.transform.IsChildOf(note.DotModelController.transform));
            sample.bodyCenter = body != null ? body.bounds.center : note.transform.position;
            sample.dotCenter = dot != null ? dot.bounds.center : sample.bodyCenter;

            var parent = note.transform.parent;
            var toEditor = (sample.pos - editingCam.transform.position).normalized;
            var toHead = (sample.pos - playingCam.position).normalized;
            Debug.Log($"[BenjaminPhaseDiag] beat {beat} {phase}: notePos={sample.pos} " +
                $"containerLocal={note.transform.localPosition} " +
                $"parentLocal={(parent != null ? parent.localPosition.ToString() : "<none>")} " +
                $"parentName={(parent != null ? parent.name : "<none>")} " +
                $"forward={sample.forward} up={note.DirectionTarget.up} " +
                $"bodyCenter={sample.bodyCenter} dotCenter={sample.dotCenter} " +
                $"editorAlign={Vector3.Dot(toEditor, sample.forward):F3} " +
                $"headAlign={Vector3.Dot(toHead, sample.forward):F3} " +
                $"renderers=[{string.Join(", ", renderers.Select(r => r.name))}]");
            return sample;
        }

        private static void CheckFacing(
            List<string> failures,
            float beat,
            Beatmap.Containers.NoteContainer note,
            Camera camera,
            bool expectFacing,
            bool requireRendered)
        {
            if (note == null || note.NoteData == null || !note.gameObject.activeInHierarchy)
            {
                Debug.Log($"[BenjaminFixtureDiag] beat {beat}: note absent from paused pool.");
                return;
            }
            var track = note.NoteData.CustomTrack is JSONString s ? s.Value : "<none>";
            if (!note.gameObject.activeInHierarchy)
            {
                failures.Add($"beat {beat}: '{track}' note container inactive.");
                return;
            }
            var renderers = note.ModelController.MpbController.Renderers;
            var activeRenderers = renderers != null
                ? renderers.Count(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                : 0;
            var dotActive = note.DotModelController != null
                && note.DotModelController.gameObject.activeInHierarchy;
            if (requireRendered && (activeRenderers == 0 || !dotActive))
            {
                failures.Add($"beat {beat}: '{track}' dot not rendered " +
                    $"(activeRenderers={activeRenderers} dotActive={dotActive}).");
                return;
            }
            if (note.Animator != null) note.Animator.LateUpdate();
            var front = note.DirectionTarget;
            var notePos = front.position;
            var toNote = (notePos - camera.transform.position).normalized;
            var forward = front.forward;
            var alignment = Vector3.Dot(toNote, forward);
            Debug.Log($"[BenjaminFixtureDiag] beat {beat}: '{track}' notePos={notePos} " +
                $"toNote={toNote} forward={forward} up={front.up} alignment={alignment:F3} " +
                $"dotActive={dotActive} activeRenderers={activeRenderers}");
            if (expectFacing && alignment <= 0.9f)
            {
                failures.Add($"beat {beat}: '{track}' dot front does not point away from the " +
                    $"playing camera along the camera->note ray: alignment={alignment:F3} " +
                    $"notePos={notePos} toNote={toNote} forward={forward}.");
            }
            if (!expectFacing && alignment > 0.9f)
            {
                failures.Add($"beat {beat}: '{track}' disableNoteLook dot rotated toward the " +
                    $"camera anyway: alignment={alignment:F3} forward={forward}.");
            }

            if (expectFacing && dotActive)
            {
                var allRenderers = note.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy).ToList();
                var body = allRenderers.FirstOrDefault(r => r.name.Contains("CubeNote"));
                var dot = allRenderers.FirstOrDefault(r =>
                    r.transform.IsChildOf(note.DotModelController.transform));
                if (body != null && dot != null)
                {
                    var dotAdvance = Vector3.Dot(dot.bounds.center - body.bounds.center, toNote);
                    if (dotAdvance >= -0.01f)
                        failures.Add($"beat {beat}: '{track}' dot is not on the camera-facing " +
                            $"side of the body post-pose (alongRay={dotAdvance:F3} " +
                            $"dotCenter={dot.bounds.center} bodyCenter={body.bounds.center}).");
                }
            }
        }

        private static void CheckRoll(
            List<string> failures,
            float beat,
            Beatmap.Containers.NoteContainer note)
        {
            if (!note.gameObject.activeInHierarchy)
            {
                failures.Add($"beat {beat}: baseline note b{note.NoteData.JsonTime} inactive.");
                return;
            }
            if (note.Animator != null) note.Animator.LateUpdate();
            var expectedUp = (note.DirectionTarget.parent.rotation
                    * Quaternion.Euler(note.DirectionTargetEuler)
                    * Vector3.up).normalized;
            var upDot = Vector3.Dot(note.DirectionTarget.up, expectedUp);
            Debug.Log($"[BenjaminFixtureDiag] beat {beat}: baseline b{note.NoteData.JsonTime} " +
                $"d{note.NoteData.CutDirection} up={note.DirectionTarget.up} " +
                $"expectedUp={expectedUp} upDot={upDot:F3} forward={note.DirectionTarget.forward}");
            if (upDot <= 0.9f)
            {
                failures.Add($"beat {beat}: baseline note b{note.NoteData.JsonTime} " +
                    $"d{note.NoteData.CutDirection} lost authored roll: up={note.DirectionTarget.up} " +
                    $"expectedUp={expectedUp} upDot={upDot:F3}.");
            }
        }

        private static Beatmap.Containers.NoteContainer FindFakeDotNote(string track, float beat)
        {
            return Object.FindAnyObjectByType<NoteGridContainer>()
                .LoadedContainers.Values
                .OfType<Beatmap.Containers.NoteContainer>()
                .Where(c => c.NoteData != null
                    && c.NoteData.CustomFake
                    && c.NoteData.CustomTrack is JSONString t && t.Value == track
                    && c.NoteData.CutDirection == (int)NoteCutDirection.Any
                    && Mathf.Abs(c.NoteData.JsonTime - beat) < 0.25f)
                .OrderBy(c => Mathf.Abs(c.NoteData.JsonTime - beat))
                .FirstOrDefault();
        }

        private static Beatmap.Containers.NoteContainer FindRealNote(float beat, int posX)
        {
            return Object.FindAnyObjectByType<NoteGridContainer>()
                .LoadedContainers.Values
                .OfType<Beatmap.Containers.NoteContainer>()
                .Where(c => c.NoteData != null
                    && !c.NoteData.CustomFake
                    && c.NoteData.PosX == posX
                    && Mathf.Abs(c.NoteData.JsonTime - beat) < 0.25f)
                .OrderBy(c => Mathf.Abs(c.NoteData.JsonTime - beat))
                .FirstOrDefault();
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
            if (playbackClock != null)
            {
                if (playbackClock.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(playbackClock);
                }

                playbackClock.enabled = previousClockEnabled;
                playbackClock = null;
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
                if (editingCameraMoved)
                {
                    cameraManager.CameraControllers[0].Camera.transform
                        .SetPositionAndRotation(previousEditingCameraPosition, previousEditingCameraRotation);
                    editingCameraMoved = false;
                }
            }
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null)
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = previousPlayerCameraFOV;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerCameraOffsetZ;
            Settings.Instance.CameraFOV = previousCameraFOV;
            if (restoreEndingFixture)
            {
                restoreEndingFixture = false;
                yield return TestUtils.ReloadMap(
                    3,
                    JSON.Parse(File.ReadAllText(FixturePath)),
                    beatsPerMinute: 155,
                    environmentName: "InterscopeEnvironment",
                    songLengthSeconds: 250);
                TestUtils.CaptureCurrentMapAsSharedBaseline();
            }
        }
    }
}

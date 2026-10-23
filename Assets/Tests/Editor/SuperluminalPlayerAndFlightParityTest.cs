using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Source notes at beats 92, 100, 100.75, 108, 116 and 138 retain spawnEffect:false and their player parent.
    public class SuperluminalPlayerAndFlightParityTest : TestBase
    {
        private bool previousAnimations;
        private float previousFov;
        private float previousCameraOffset;
        private bool previousClockEnabled;
        private bool clockCaptured;
        private UIMode uiMode;
        private CameraManager cameras;
        private AudioTimeSyncController clock;
        private NoteGridContainer notes;
        private Vector3 cameraHome;
        private ObjectAnimator[] platformTargets;
        private readonly List<string> failures = new();
        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousCameraOffset = Settings.Instance.PlayerCameraOffsetZ;
            clockCaptured = false;
            failures.Clear();
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        private IEnumerator LoadFixture()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var fullPath = System.Environment.GetEnvironmentVariable("CHROMAPPER_SUPERLUMINAL_FULL_MAP_PATH");
            var path = string.IsNullOrEmpty(fullPath)
                ? PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "SuperluminalPlayerAndFlightFixture.json")
                : fullPath;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 16,
                NoteStartBeatOffset = 0,
                CustomRequirements = new System.Collections.Generic.List<string> { "Chroma", "Noodle Extensions" },
                CustomColorLeft = new Color(0.6039216f, 0.254902f, 0.9960784f),
                CustomColorRight = new Color(0.1294118f, 0.2156863f, 0.827451f),
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(path)),
                beatsPerMinute: 150,
                environmentName: "GagaEnvironment",
                songLengthSeconds: 270,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            notes = Object.FindAnyObjectByType<NoteGridContainer>();
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            platformTargets = tracks.GetAnimationTrack("PlayersPlaceTrack").Children.ToArray();
            previousClockEnabled = clock.enabled;
            clockCaptured = true;
            clock.enabled = false;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            yield return Seek(68f);
            cameraHome = cameras.SelectedCameraController.Camera.transform.position;
        }

        [UnityTest]
        public IEnumerator PlayerTravelAndIncomingNotesMatchNativeAcrossSeekAndPlayback()
        {
            yield return LoadFixture();
            foreach (var beat in new[] { 88.9f, 89.9f, 90.1f, 89.9f, 97.9f, 100f, 105.9f, 108f, 113.9f, 116f, 120f, 124f, 135.9f, 113.9f, 97.9f, 88.9f, 89.9f })
            {
                yield return Seek(beat);
                Sample(beat, "seek");
            }

            yield return Seek(88.6f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 88.6f);
            foreach (var beat in new[] { 88.9f, 89.9f, 90.1f, 97.9f, 100f, 105.9f, 108f, 113.9f, 116f, 120f, 124f, 135.9f })
            {
                currentSecondsProperty.SetValue(clock, clock.GetSecondsFromBeat(beat));
                yield return null;
                yield return null;
                yield return null;
                Sample(beat, "playback");
            }

            TestUtils.PauseDeterministicPlayback(clock);
            yield return PlayFloorWindow(92f);
            yield return PlayFloorWindow(100.75f);
            yield return Seek(97.9f);
            Sample(97.9f, "paused reverse");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private IEnumerator PlayFloorWindow(float hit)
        {
            yield return Seek(hit - 3.4f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, hit - 3.4f);
            var beats = new List<float> { hit - 3.4f };
            for (var i = 0; i <= 12; i++)
                beats.Add(hit - 3.25f + i * 0.1f);

            beats.Add(hit - 2f);
            beats.Add(hit - 1.95f);
            beats.Add(hit - 1.85f);
            foreach (var beat in beats)
            {
                currentSecondsProperty.SetValue(clock, clock.GetSecondsFromBeat(beat));
                yield return null;
                yield return null;
                yield return null;
                Sample(beat, "dense playback", hit);
            }

            TestUtils.PauseDeterministicPlayback(clock);
        }

        private void Sample(float beat, string phase, float sourceHit = 0f)
        {
            var camera = cameras.SelectedCameraController.Camera;
            var expectedCamera = cameraHome + Vector3.forward * NativePlayerZ(beat);
            var cameraZ = camera.transform.position.z;
            if (Mathf.Abs(cameraZ - expectedCamera.z) > 0.25f)
                failures.Add($"{phase} b{beat}: camera world z={cameraZ:F4}, native={expectedCamera.z:F4}.");

            if (platformTargets.Length == 0)
                failures.Add($"{phase} b{beat}: source PlayersPlaceTrack enhancement has no runtime target.");

            foreach (var animator in platformTargets)
            {
                var target = animator.LocalTarget;
                if (Mathf.Abs(target.position.z - NativePlayerZ(beat)) > 0.25f)
                    failures.Add($"{phase} b{beat}: source enhanced PlayersPlace '{target.name}' world z={target.position.z:F4}, native inherited PlayerTrack={NativePlayerZ(beat):F4}.");

            }

            var hit = sourceHit != 0f
                ? sourceHit
                : Mathf.Abs(beat - 88.9f) < 0.01f || Mathf.Abs(beat - 89.9f) < 0.01f || Mathf.Abs(beat - 90.1f) < 0.01f
                    ? 92f
                    : Mathf.Abs(beat - 97.9f) < 0.01f
                        ? 100f
                        : Mathf.Abs(beat - 105.9f) < 0.01f
                        ? 108f
                        : Mathf.Abs(beat - 113.9f) < 0.01f
                            ? 116f
                            : Mathf.Abs(beat - 135.9f) < 0.01f
                            ? 138f
                            : 0f;
            if (hit != 0f)
            {
                var note = notes.LoadedContainers.Values.OfType<NoteContainer>()
                    .FirstOrDefault(candidate => !candidate.NoteData.CustomFake && candidate.NoteData.JsonTime == hit);
                var nativeSpawnBeat = hit - 3.25f;
                var inNativeWindow = beat >= nativeSpawnBeat;
                if (note == null && inNativeWindow)
                    failures.Add($"{phase} b{beat}: incoming source note b{hit} must exist during native floor movement.");

                else if (note != null)
                {
                    if (!inNativeWindow)
                    {
                        var beforeSpawnPixels = RenderNote(note, camera, null, null);
                        if (beforeSpawnPixels >= 2)
                            failures.Add($"{phase} b{beat}: preloaded b{hit} is visible before native spawn b{nativeSpawnBeat} ({beforeSpawnPixels} pixels).");

                        AssertSpawnFlashPreservesFloorPose(note, beat, note.transform.position, phase);
                        return;
                    }

                    Assert.That(note.NoteData.HalfJumpDuration, Is.EqualTo(2f).Within(0.0001f));
                    Assert.That(note.NoteData.CustomSpawnEffect.AsBool, Is.False);
                    Assert.That(note.NoteData.CustomData["disableNoteGravity"].AsBool, Is.True);
                    // Native floor movement runs for .5 seconds before the two-beat half jump.
                    // CM's hit plane is one world unit ahead of its coordinate origin.
                    var localZ = beat < hit - 2f
                        ? 12.8f + 100f * ((hit - 2f - beat) / 1.25f)
                        : (hit - beat) * 6.4f;
                    var expectedPosition = note.transform.position;
                    expectedPosition.x = (note.NoteData.PosX - 1.5f) * 0.6f;
                    var floorHeight = 0.25f + note.NoteData.PosY * 0.6f;
                    if (beat <= hit - 2f)
                        expectedPosition.y = floorHeight;

                    expectedPosition.z = NativePlayerZ(beat) + localZ + 1f;
                    var viewport = ViewportAt(camera, expectedCamera, expectedPosition);
                    var actualPixels = RenderNote(note, camera, null, null);
                    var referencePixels = RenderNote(note, camera, expectedPosition, expectedCamera);
                    if (Mathf.Abs(note.transform.position.x - expectedPosition.x) > 0.001f)
                        failures.Add($"{phase} b{beat}: incoming b{hit} lane x={note.transform.position.x:F4}, native={expectedPosition.x:F4}.");

                    if (beat <= hit - 2f && Mathf.Abs(note.transform.position.y - floorHeight) > 0.001f)
                        failures.Add($"{phase} b{beat}: incoming b{hit} floor y={note.transform.position.y:F4}, authored disabled-gravity layer={floorHeight:F4}.");

                    if (sourceHit != 0f && beat > hit - 2f
                        && (note.transform.position.y < floorHeight - 0.001f || note.transform.position.y > floorHeight + 0.2f))
                    {
                        failures.Add($"{phase} b{beat}: incoming b{hit} must continue the native floor height into the jump; y={note.transform.position.y:F4}, floor={floorHeight:F4}.");
                    }

                    if (Mathf.Abs(note.transform.position.z - expectedPosition.z) > 0.25f)
                        failures.Add($"{phase} b{beat}: incoming b{hit} world z={note.transform.position.z:F4}, native floor/jump={expectedPosition.z:F4}.");

                    if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                        failures.Add($"{phase} b{beat}: native reference note b{hit} must lie in the playing-camera frustum, viewport={viewport}.");

                    if (localZ <= 80f && referencePixels < 2)
                        failures.Add($"{phase} b{beat}: native pose rendering control for b{hit} produced only {referencePixels} pixels.");

                    if (localZ <= 80f && actualPixels < 2)
                        failures.Add($"{phase} b{beat}: actual incoming note b{hit} is invisible ({actualPixels} pixels).");

                    if (beat < hit - 2f)
                        AssertSpawnFlashPreservesFloorPose(note, beat, expectedPosition, phase);

                }
            }

            if (beat == 100f || beat == 108f || beat == 120f || beat == 89.9f)
                CapturePlayingView(camera, $"motion-{phase.Replace(' ', '-')}-b{beat}");

        }

        private void AssertSpawnFlashPreservesFloorPose(NoteContainer note, float beat, Vector3 expected, string phase)
        {
            // Chroma's flash flag must not change the source note's native floor movement.
            var sourceSpawnEffect = note.NoteData.CustomSpawnEffect;
            try
            {
                note.NoteData.CustomSpawnEffect = true;
                note.Animator.AnimationTrack.UpdateTime(beat);
                if (Vector3.Distance(note.transform.position, expected) > 0.25f)
                    failures.Add($"{phase} b{beat}: enabling the spawn flash changed b{note.NoteData.JsonTime}'s floor pose to {note.transform.position}, expected {expected}.");

            }

            finally
            {
                note.NoteData.CustomSpawnEffect = sourceSpawnEffect;
                note.Animator.AnimationTrack.UpdateTime(beat);
            }
        }

        private static float NativePlayerZ(float beat)
        {
            // Exact authored PlayerTrack points, sampled with the native point easing formulas.
            if (beat <= 68f)
                return 0f;

            if (beat <= 98f)
                return 100f * ((beat - 68f) / 30f);

            if (beat <= 102f)
            {
                var t = (beat - 98f) / 4f;
                var ease = t < 0.5f
                    ? Mathf.Pow(2f, 20f * t - 10f) / 2f
                    : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
                return 100f + 50f * ease;
            }

            if (beat <= 116f)
                return 150f + 50f * (1f - Mathf.Cos(Mathf.PI * ((beat - 102f) / 14f))) / 2f;

            if (beat < 124f)
                return 200f * (1f - Mathf.Pow(2f, 10f * ((beat - 116f) / 8f) - 10f));

            if (beat <= 132f)
                return 0f;

            if (beat <= 136f)
                return 50f * (1f - Mathf.Pow(1f - (beat - 132f) / 4f, 3f));

            return 50f;
        }

        private static Vector3 ViewportAt(Camera camera, Vector3 cameraPosition, Vector3 worldPosition)
        {
            var position = camera.transform.position;
            try
            {
                camera.transform.position = cameraPosition;
                return camera.WorldToViewportPoint(worldPosition);
            }

            finally
            {
                camera.transform.position = position;
            }
        }

        private static int RenderNote(NoteContainer note, Camera playingCamera, Vector3? notePosition, Vector3? cameraPosition)
        {
            var objects = new List<GameObject>();
            var cameraObject = new GameObject("Superluminal incoming note sample camera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var image = new Texture2D(512, 512, TextureFormat.RGBA32, false, true);
            var previousTarget = RenderTexture.active;
            try
            {
                var offset = notePosition.HasValue
                    ? notePosition.Value - note.transform.position
                    : Vector3.zero;
                foreach (var renderer in note.ModelController.Renderers.Concat(note.ArrowMpbController.Renderers).Distinct())
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;

                    var model = new GameObject("Superluminal incoming note render sample");
                    objects.Add(model);
                    model.layer = 30;
                    model.transform.SetPositionAndRotation(renderer.transform.position + offset, renderer.transform.rotation);
                    model.transform.localScale = renderer.transform.lossyScale;
                    model.AddComponent<MeshFilter>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var copy = model.AddComponent<MeshRenderer>();
                    copy.sharedMaterials = renderer.sharedMaterials;
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    copy.SetPropertyBlock(properties);
                }

                camera.enabled = false;
                camera.cullingMask = 1 << 30;
                camera.fieldOfView = playingCamera.fieldOfView;
                camera.nearClipPlane = playingCamera.nearClipPlane;
                camera.farClipPlane = playingCamera.farClipPlane;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.transform.SetPositionAndRotation(cameraPosition ?? playingCamera.transform.position, playingCamera.transform.rotation);
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply();
                return image.GetPixels().Count(pixel => pixel.maxColorComponent > 0.003f);
            }

            finally
            {
                RenderTexture.active = previousTarget;
                foreach (var model in objects)
                    Object.DestroyImmediate(model);

                target.Release();
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }

        private static string CaptureDirectory => System.Environment.GetEnvironmentVariable("CHROMAPPER_SUPERLUMINAL_CAPTURE_DIRECTORY");

        private static void CapturePlayingView(Camera camera, string name)
        {
            if (string.IsNullOrEmpty(CaptureDirectory))
                return;

            Directory.CreateDirectory(CaptureDirectory);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(PathUtils.Combine(CaptureDirectory, name + ".png"), image.EncodeToPNG());
            }

            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }

        private IEnumerator Seek(float beat)
        {
            clock.MoveToJsonTime(beat);
            yield return null;
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (clock != null && clockCaptured)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
                clock.StopScheduled = false;
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousCameraOffset;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

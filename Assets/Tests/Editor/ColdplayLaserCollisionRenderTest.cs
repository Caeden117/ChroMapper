using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Compare paused and playing renders of Coldplay's sphere ring at the same beats and camera pose.
    // Collision lengths alone cannot detect the ring turning white during playback.
    public class ColdplayLaserCollisionRenderTest : BasicEventChunkingTestBase
    {
        private const int renderWidth = 512;
        private const int renderHeight = 288;
        private const int settleFrames = 4;
        private const float sweepStartJsonTime = 134f;
        private const float sweepEndJsonTime = 140.6f;
        private const float playbackSecondsPerFrame = 1f / 60f;
        private const float collisionTolerance = 0.01f;

        // Use the camera pose saved in the map so both captures face the sphere ring shown in the report.
        private static readonly Vector3 captureCameraPosition =
            new(-1.60726249f, 2.87418f, -6.11830664f);
        private static readonly Quaternion captureCameraRotation =
            new(-0.0119350953f, 0.040497072f, -0.000483767828f, -0.999108255f);

        // PNG pairs are written for every sampled beat; 137..140 bracket the reported white ring.
        private static readonly float[] captureJsonTimes =
            { 134f, 135f, 136f, 137f, 137.5f, 138f, 138.5f, 139f, 139.5f, 140f };

        // The playing-camera route only needs a few samples inside the failing window.
        private static readonly float[] playingCameraJsonTimes = { 137.5f, 138f, 138.5f, 139f, 139.5f };

        private GameObject helperObject;
        private DeterministicSecondsDriver driver;
        private RenderCaptureObserver observer;
        private RenderTexture renderTarget;
        private string runDirectory;

        private bool capturedSettings;
        private bool previousAnimations;
        private bool previousBloom;

        [UnityTest]
        public IEnumerator PlayingRenderMatchesPausedRenderAtSameBeats()
        {
            yield return RunRenderComparison(CameraType.Editing, captureJsonTimes);
        }

        // Use the same pose and field of view for the playing camera, isolating its render callbacks from the
        // editing camera's.
        [UnityTest]
        public IEnumerator PlayingCameraRenderMatchesPausedRenderAtSameBeats()
        {
            yield return RunRenderComparison(CameraType.Playing, playingCameraJsonTimes);
        }

        [UnityTest]
        public IEnumerator PlayingCameraRenderMatchesPausedRenderAtSameBeatsAt30Hz()
        {
            yield return RunRenderComparison(CameraType.Playing, playingCameraJsonTimes, 1f / 30f);
        }

        [UnityTest]
        public IEnumerator PlayingCameraRenderMatchesPausedRenderAtSameBeatsAt120Hz()
        {
            yield return RunRenderComparison(CameraType.Playing, playingCameraJsonTimes, 1f / 120f);
        }

        private IEnumerator RunRenderComparison(
            CameraType cameraType,
            float[] beats,
            float secondsPerFrame = playbackSecondsPerFrame)
        {
            var cadenceFps = Mathf.RoundToInt(1f / secondsPerFrame);
            var route = $"paused-vs-live render on {cameraType} camera ({cadenceFps}fps)";
            runDirectory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "build-logs",
                "coldplay-minimal-only",
                $"paused-vs-live-{cameraType}-{cadenceFps}fps-{DateTime.Now:yyyyMMdd-HHmmss}");
            Directory.CreateDirectory(runDirectory);
            Assert.That(
                Directory.Exists(Path.GetDirectoryName(runDirectory)),
                Is.True,
                $"{route}: render artifact parent directory {Path.GetDirectoryName(runDirectory)} is missing.");
            Debug.Log($"[ColdplayLaserCollisionRender] {route}: writing artifacts to {runDirectory}");

            yield return TestUtils.ReloadMap(
                3,
                ColdplayLaserCollisionTest.LoadMinimalFixtureJson(),
                ColdplayLaserCollisionTest.MinimalEditorState(),
                100f,
                ColdplayLaserCollisionTest.FixtureEnvironment,
                ColdplayLaserCollisionTest.FixtureSongLengthSeconds);

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var lightshow = Object.FindAnyObjectByType<LightshowController>();
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var map = BeatSaberSongContainer.Instance.Map;
            Assert.That(atsc, Is.Not.Null, $"{route}: AudioTimeSyncController missing.");
            Assert.That(lightshow, Is.Not.Null, $"{route}: LightshowController missing.");
            Assert.That(uiMode, Is.Not.Null, $"{route}: UIMode missing.");
            Assert.That(cameraManager, Is.Not.Null, $"{route}: CameraManager missing.");
            Assert.That(map, Is.Not.Null, $"{route}: map missing.");
            Assert.That(
                Settings.Instance.MapVersion,
                Is.EqualTo(3),
                $"{route}: the minimal fixture must run under map version 3.");
            Assert.That(
                map.MajorVersion,
                Is.EqualTo(3),
                $"{route}: the parsed map must report major version 3.");
            Assert.That(
                EnvironmentInfoHelper.GetCurrentEnvironment(),
                Is.EqualTo(ColdplayLaserCollisionTest.FixtureEnvironment),
                $"{route}: the fixture must resolve to the Coldplay environment.");
            Assert.That(
                map.LightColorEventBoxGroups.Count
                    + map.LightRotationEventBoxGroups.Count
                    + map.LightTranslationEventBoxGroups.Count,
                Is.GreaterThan(0),
                $"{route}: the minimal lightshow must populate indexed light groups; got none.");

            var bpmAt137 = map.BpmAtJsonTime(137f);
            Assert.That(
                bpmAt137.HasValue && bpmAt137.Value > 200f,
                Is.True,
                $"{route}: the BPM region covering authored beat 137 must keep its high BPM;"
                + $" the Info.dat 100 BPM is only nominal. Actual {bpmAt137}.");

            var lasers = ColdplayLaserCollisionTest.FindSpotlightLasers();
            Assert.That(
                lasers.Count,
                Is.EqualTo(16),
                $"{route}: expected the 16 group-{ColdplayLaserCollisionTest.SpotlightGroupId}"
                + $" LasersSpotlights LightCollision lasers, found {lasers.Count}.");

            var environmentScene = lasers[0].gameObject.scene;
            var colliders = Object
                .FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.gameObject.scene == environmentScene
                    && ((1 << c.gameObject.layer) & lasers[0].EnvironmentLayerMask.value) != 0)
                .ToList();
            Assert.That(
                colliders.Count,
                Is.EqualTo(32),
                $"{route}: expected the 32 layer-{ColdplayLaserCollisionTest.CollisionLayer} sphere"
                + $" colliders, found {colliders.Count}.");
#pragma warning disable CS0618
            Assert.That(
                Physics.autoSyncTransforms,
                Is.False,
                $"{route}: repro requires deferred PhysX sync.");
#pragma warning restore CS0618

            if (lightshow.Mode != LightshowMode.Full)
            {
                lightshow.SetMode(LightshowMode.Full);
            }

            CaptureSettings();
            Settings.Instance.Animations = true;
            Settings.Instance.Bloom = true;

            var bloomfog = BloomfogRenderingController.Instance;
            Assert.That(
                bloomfog == null || bloomfog.CanRenderReflections,
                Is.True,
                $"{route}: bloom/fog rendering did not activate on the live controller.");

            InitializeVirtualInput(false);
            // Playing hides the editor UI groups, grid container renderers, canvases, and placement
            // objects while leaving the environment itself untouched. SetUIMode alone does not switch
            // the selected camera, so the editing camera keeps every per-camera render callback.
            uiMode.SetUIMode(UIModeType.Playing, false);

            cameraManager.SelectCamera(cameraType);
            var cameraController = cameraManager.SelectedCameraController;
            if (cameraType == CameraType.Editing)
            {
                // The editing camera is parented to the scrolling beat grid; detach it so playback
                // cannot drag the fixed capture pose.
                cameraController.LockedOntoNoteGrid = false;
            }
            else
            {
                // The playing camera binds animation tracks while preview animations are on; drop them
                // so the pinned pose is not overwritten mid-sweep.
                cameraController.ClearPlayerTracks();
            }

            var camera = cameraController.Camera;
            PinCamera(camera);
            Assert.That(
                camera.enabled && camera.isActiveAndEnabled,
                Is.True,
                $"{route}: the {cameraType} camera must be the active render camera.");

            renderTarget = new RenderTexture(
                renderWidth,
                renderHeight,
                24,
                RenderTextureFormat.DefaultHDR,
                RenderTextureReadWrite.Linear);
            renderTarget.Create();
            camera.targetTexture = renderTarget;

            helperObject = new GameObject("Coldplay laser collision render observer");
            driver = helperObject.AddComponent<DeterministicSecondsDriver>();
            observer = helperObject.AddComponent<RenderCaptureObserver>();
            driver.Atsc = atsc;
            observer.Atsc = atsc;
            observer.Lasers = lasers;
            observer.Colliders = colliders;
            observer.Cam = camera;
            observer.Target = renderTarget;
            observer.OutputDirectory = runDirectory;
            observer.Init();

            var pausedCaptures = new Dictionary<float, RenderCaptureObserver.RenderCapture>();
            var liveCaptures = new Dictionary<float, RenderCaptureObserver.RenderCapture>();

            // Paused reference renders: ordinary scrubs plus fixed-update settles, same camera.
            foreach (var beat in beats)
            {
                atsc.MoveToJsonTime(beat);
                for (var frame = 0; frame < settleFrames; frame++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                observer.QueueCapture(PausedTag(beat));
                // QueueCapture is consumed in this frame's LateUpdate; the result is readable next frame.
                yield return null;
                yield return null;
                Assert.That(
                    observer.Captures.TryGetValue(PausedTag(beat), out var pausedCapture),
                    Is.True,
                    $"{route}: paused capture at beat {beat} was not recorded.");
                pausedCaptures[beat] = pausedCapture;
            }

            // Run normal playback with native audio stopped; the deterministic driver advances the clock
            // during Update.
            var startSongBpmTime = (float)map.JsonTimeToSongBpmTime(sweepStartJsonTime);
            var endSongBpmTime = (float)map.JsonTimeToSongBpmTime(sweepEndJsonTime);
            var endSeconds = atsc.GetSecondsFromBeat(endSongBpmTime);
            Assert.That(
                float.IsFinite(endSeconds) && endSeconds > 0f
                    && endSeconds < ColdplayLaserCollisionTest.FixtureSongLengthSeconds,
                Is.True,
                $"{route}: sweep end seconds must be finite inside the song, got {endSeconds}.");

            atsc.MoveToJsonTime(sweepStartJsonTime);
            yield return null;
            // Measure motion and enabled renderers from playback frames only; the paused scrubs above
            // move the same transforms and would make the vacuous-pass checks meaningless.
            observer.SawMotion = false;
            observer.SawEnabledRenderer = false;
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(atsc, startSongBpmTime);
            atsc.enabled = false;
            driver.Seconds = atsc.CurrentSeconds;
            driver.SecondsPerFrame = secondsPerFrame;
            driver.Running = true;
            yield return null;
            yield return null;
            Assert.That(
                driver.Seconds,
                Is.GreaterThan(atsc.GetSecondsFromBeat(startSongBpmTime)),
                $"{route}: deterministic seconds driver did not advance playback time.");

            var sweepFrames = 0;
            const int sweepFrameLimit = 600;
            var liveTargetSeconds = new Dictionary<float, float>();
            foreach (var beat in beats)
            {
                var targetSeconds = atsc.GetSecondsFromBeat((float)map.JsonTimeToSongBpmTime(beat));
                liveTargetSeconds[beat] = targetSeconds;
                driver.SecondsPerFrame = targetSeconds >= driver.Seconds
                    ? secondsPerFrame
                    : -secondsPerFrame;
                driver.TargetSeconds = targetSeconds;
                driver.Running = true;
                while (driver.Running && sweepFrames < sweepFrameLimit)
                {
                    sweepFrames++;
                    yield return null;
                }

                // The driver stops inside the Update that lands on the target, and this coroutine
                // resumes before that frame's LateUpdate, so the capture below reads the exact beat.
                Assert.That(
                    Mathf.Abs(driver.Seconds - targetSeconds),
                    Is.LessThan(0.0001f),
                    $"{route}: driver stopped {driver.Seconds - targetSeconds} seconds from"
                    + $" the target for beat {beat}.");
                observer.QueueCapture(LiveTag(beat));
                yield return null;
                yield return null;

                Assert.That(
                    observer.Captures.TryGetValue(LiveTag(beat), out var liveCapture),
                    Is.True,
                    $"{route}: live capture at beat {beat} was not recorded.");
                liveCaptures[beat] = liveCapture;
            }

            driver.TargetSeconds = null;
            driver.SecondsPerFrame = secondsPerFrame;
            driver.Running = true;
            while (driver.Seconds < endSeconds && sweepFrames < sweepFrameLimit)
            {
                sweepFrames++;
                yield return null;
            }

            driver.Running = false;
            Assert.That(
                sweepFrames,
                Is.LessThan(sweepFrameLimit),
                $"{route}: sweep did not reach {endSeconds} seconds within {sweepFrameLimit} frames"
                + $" (stalled at {driver.Seconds} seconds).");
            yield return null;
            atsc.enabled = true;
            TestUtils.PauseDeterministicPlayback(atsc);

            WriteReport(pausedCaptures, liveCaptures, route, beats);

            // Require visible, moving, lit beams so an empty render cannot pass the comparison.
            Assert.That(
                observer.SawMotion,
                Is.True,
                $"{route}: no collider or laser transform moved during the playback sweep; captures"
                + " would compare two static scenes.");
            Assert.That(
                observer.SawEnabledRenderer,
                Is.True,
                $"{route}: no group-{ColdplayLaserCollisionTest.SpotlightGroupId} laser had renderers"
                + " enabled during the sweep.");
            Assert.That(
                liveCaptures.Values.All(capture => capture.FrameMeanLuminance > 0.0001f),
                Is.True,
                $"{route}: a live capture rendered a fully black frame; the camera produced no output.");
            Assert.That(
                pausedCaptures.Values.All(capture => capture.FrameMeanLuminance > 0.0001f),
                Is.True,
                $"{route}: a paused capture rendered a fully black frame; the camera produced no output.");

            // The reported symptom is the interior of the sphere ring saturating white while playing.
            // Compare the playing render against the paused render at the same beat: the paused capture
            // is the baseline, so the thresholds scale with what the map actually produces.
            var mismatches = new List<string>();
            var fieldMismatches = new List<string>();
            foreach (var beat in beats)
            {
                var pausedCapture = pausedCaptures[beat];
                var liveCapture = liveCaptures[beat];
                Assert.That(
                    Mathf.Abs(liveCapture.Seconds - liveTargetSeconds[beat]),
                    Is.LessThan(0.0001f),
                    $"{route}: live capture at beat {beat} sampled {liveCapture.Seconds} seconds"
                    + $" vs target {liveTargetSeconds[beat]}; the deterministic clock missed the beat.");
                Assert.That(
                    Mathf.Abs(liveCapture.JsonTime - beat),
                    Is.LessThan(0.01f),
                    $"{route}: live capture at beat {beat} sampled json {liveCapture.JsonTime};"
                    + " the deterministic clock missed the beat.");
                Assert.That(
                    pausedCapture.SawPostRenderCallback,
                    Is.True,
                    $"{route}: paused capture at beat {beat} never received the camera post-render"
                    + " callback; post-render fields are not authoritative.");
                Assert.That(
                    liveCapture.SawPostRenderCallback,
                    Is.True,
                    $"{route}: live capture at beat {beat} never received the camera post-render"
                    + " callback; post-render fields are not authoritative.");

                CollectPostRenderFieldMismatches(pausedCapture, fieldMismatches);
                CollectPostRenderFieldMismatches(liveCapture, fieldMismatches);

                var whiteCut = Mathf.Max(
                    pausedCapture.InnerP95Luminance * 2f,
                    pausedCapture.InnerMeanLuminance * 4f,
                    0.5f);
                var pausedWhite = pausedCapture.InnerWhiteFraction(whiteCut);
                var liveWhite = liveCapture.InnerWhiteFraction(whiteCut);

                if (liveCapture.InnerMeanLuminance
                        > (pausedCapture.InnerMeanLuminance * 1.5f) + 0.05f)
                {
                    mismatches.Add(
                        $"beat {beat.ToString("0.00", CultureInfo.InvariantCulture)}: live inner-circle mean"
                        + $" luminance {liveCapture.InnerMeanLuminance:F3} exceeds paused"
                        + $" {pausedCapture.InnerMeanLuminance:F3} by more than 1.5x baseline");
                }

                if (liveWhite > pausedWhite + 0.15f)
                {
                    mismatches.Add(
                        $"beat {beat.ToString("0.00", CultureInfo.InvariantCulture)}: live white fraction"
                        + $" {liveWhite:F3} exceeds paused {pausedWhite:F3} + 0.15"
                        + $" (cut {whiteCut:F3} from paused p95 {pausedCapture.InnerP95Luminance:F3})");
                }
            }

            Assert.That(
                fieldMismatches.Count,
                Is.EqualTo(0),
                $"{route}: the post-render collision state diverged from the same-frame analytic"
                + $" sphere geometry in {fieldMismatches.Count} laser captures"
                + $" (artifacts: {runDirectory}):\n{string.Join("\n", fieldMismatches.Take(10))}");
            Assert.That(
                mismatches.Count,
                Is.EqualTo(0),
                $"{route}: the playing render diverged from the paused render inside the sphere ring"
                + $" in {mismatches.Count} of {beats.Length} samples"
                + $" (artifacts: {runDirectory}):\n{string.Join("\n", mismatches)}");
        }

        // Compare the collision length captured after rendering with the sphere distance from the same frame.
        private static void CollectPostRenderFieldMismatches(
            RenderCaptureObserver.RenderCapture capture,
            List<string> mismatches)
        {
            foreach (var laser in capture.Lasers)
            {
                if (!laser.RenderersEnabled || !laser.HasPostRender)
                {
                    continue;
                }

                var collisionMatches = laser.ExpectedHit
                    ? Mathf.Abs(laser.PostRenderCollisionLength - laser.ExpectedDistance)
                        <= collisionTolerance
                    : laser.PostRenderCollisionLength == float.MaxValue;
                if (!collisionMatches)
                {
                    mismatches.Add(
                        $"{capture.Tag} [{laser.LaserId}] post-render collisionLength"
                        + $" {laser.PostRenderCollisionLength:R} expected"
                        + $" {(laser.ExpectedHit ? laser.ExpectedDistance.ToString("R", CultureInfo.InvariantCulture) : "none")}"
                        + $" hit {laser.ExpectedColliderId}");
                    continue;
                }

                var expectedClipLength = laser.ExpectedHit
                    ? Mathf.Min(laser.ExpectedDistance, laser.SpriteLength)
                    : laser.SpriteLength;
                if (laser.SpriteRendererEnabled
                    && Mathf.Abs(laser.PostRenderSizeParams.y - expectedClipLength)
                        > collisionTolerance)
                {
                    mismatches.Add(
                        $"{capture.Tag} [{laser.LaserId}] post-render sprite _SizeParams.y"
                        + $" {laser.PostRenderSizeParams.y:R} expected clipped length"
                        + $" {expectedClipLength.ToString("R", CultureInfo.InvariantCulture)}");
                }

                var expectedBoxScaleY = Mathf.Min(
                        laser.ExpectedHit ? laser.ExpectedDistance : laser.BoxHeight,
                        laser.BoxHeight)
                    * 0.5f;
                if (laser.BoxRendererEnabled
                    && Mathf.Abs(laser.PostRenderBoxLocalScale.y - expectedBoxScaleY)
                        > collisionTolerance)
                {
                    mismatches.Add(
                        $"{capture.Tag} [{laser.LaserId}] post-render box localScale.y"
                        + $" {laser.PostRenderBoxLocalScale.y:R} expected"
                        + $" {expectedBoxScaleY.ToString("R", CultureInfo.InvariantCulture)}");
                }
            }
        }

        // CameraController.Update rewrites the playing camera's FOV every frame and track animators can
        // move either camera mid-sweep, so the observer re-pins pose and projection before each capture.
        private void PinCamera(Camera camera)
        {
            camera.transform.SetPositionAndRotation(captureCameraPosition, captureCameraRotation);
            camera.fieldOfView = Settings.Instance.CameraFOV;
        }

        private static string PausedTag(float beat) =>
            $"paused-{beat.ToString("0.00", CultureInfo.InvariantCulture)}";

        private static string LiveTag(float beat) =>
            $"live-{beat.ToString("0.00", CultureInfo.InvariantCulture)}";

        private void CaptureSettings()
        {
            if (capturedSettings)
            {
                return;
            }

            previousAnimations = Settings.Instance.Animations;
            previousBloom = Settings.Instance.Bloom;
            capturedSettings = true;
        }

        private void WriteReport(
            Dictionary<float, RenderCaptureObserver.RenderCapture> pausedCaptures,
            Dictionary<float, RenderCaptureObserver.RenderCapture> liveCaptures,
            string route,
            float[] beats)
        {
            var report = new StringBuilder();
            report.AppendLine($"run: {route}");
            report.AppendLine($"directory: {runDirectory}");
            report.AppendLine(
                $"settings: Animations={Settings.Instance.Animations} Bloom={Settings.Instance.Bloom}");
            report.AppendLine($"camera pose: {captureCameraPosition} {captureCameraRotation.eulerAngles}"
                + $" fov={Settings.Instance.CameraFOV}");
            report.AppendLine($"target: {renderWidth}x{renderHeight} {renderTarget.format}");
            var bloomfog = BloomfogRenderingController.Instance;
            report.AppendLine(
                $"bloomfog: instance={bloomfog != null}"
                + $" canRenderReflections={bloomfog != null && bloomfog.CanRenderReflections}");
            report.AppendLine("beat | paused json | paused innerMean innerP95 whiteFrac frameMean |"
                + " live json | live innerMean innerP95 whiteFrac frameMean | meanAbsDiff");
            foreach (var beat in beats)
            {
                if (!pausedCaptures.TryGetValue(beat, out var pausedCapture)
                    || !liveCaptures.TryGetValue(beat, out var liveCapture))
                {
                    report.AppendLine(
                        $"beat {beat.ToString("0.00", CultureInfo.InvariantCulture)}: missing capture");
                    continue;
                }

                var whiteCut = Mathf.Max(
                    pausedCapture.InnerP95Luminance * 2f,
                    pausedCapture.InnerMeanLuminance * 4f,
                    0.5f);
                report.AppendLine(
                    $"beat {beat.ToString("0.00", CultureInfo.InvariantCulture)}"
                    + $" | {pausedCapture.JsonTime.ToString("0.000", CultureInfo.InvariantCulture)}"
                    + $" | {pausedCapture.InnerMeanLuminance:F4} {pausedCapture.InnerP95Luminance:F4}"
                    + $" {pausedCapture.InnerWhiteFraction(whiteCut):F3} {pausedCapture.FrameMeanLuminance:F4}"
                    + $" | {liveCapture.JsonTime.ToString("0.000", CultureInfo.InvariantCulture)}"
                    + $" | {liveCapture.InnerMeanLuminance:F4} {liveCapture.InnerP95Luminance:F4}"
                    + $" {liveCapture.InnerWhiteFraction(whiteCut):F3} {liveCapture.FrameMeanLuminance:F4}"
                    + $" | {MeanAbsDiff(pausedCapture, liveCapture):F4}");

                WriteDiffPng(pausedCapture, liveCapture);
            }

            File.WriteAllText(PathUtils.Combine(runDirectory, "report.txt"), report.ToString());
        }

        private static float MeanAbsDiff(
            RenderCaptureObserver.RenderCapture a,
            RenderCaptureObserver.RenderCapture b)
        {
            if (a.Pixels == null || b.Pixels == null || a.Pixels.Length != b.Pixels.Length)
            {
                return -1f;
            }

            double sum = 0;
            for (var i = 0; i < a.Pixels.Length; i++)
            {
                sum += Mathf.Abs(a.Pixels[i].r - b.Pixels[i].r)
                    + Mathf.Abs(a.Pixels[i].g - b.Pixels[i].g)
                    + Mathf.Abs(a.Pixels[i].b - b.Pixels[i].b);
            }

            return (float)(sum / (a.Pixels.Length * 3));
        }

        private void WriteDiffPng(
            RenderCaptureObserver.RenderCapture pausedCapture,
            RenderCaptureObserver.RenderCapture liveCapture)
        {
            if (pausedCapture.Pixels == null || liveCapture.Pixels == null
                || pausedCapture.Pixels.Length != liveCapture.Pixels.Length)
            {
                return;
            }

            var diff = new Color[pausedCapture.Pixels.Length];
            for (var i = 0; i < diff.Length; i++)
            {
                // Amplify 4x so subtle beam-length deltas stay visible in the artifact.
                diff[i] = new Color(
                    Mathf.Clamp01(Mathf.Abs(liveCapture.Pixels[i].r - pausedCapture.Pixels[i].r) * 4f),
                    Mathf.Clamp01(Mathf.Abs(liveCapture.Pixels[i].g - pausedCapture.Pixels[i].g) * 4f),
                    Mathf.Clamp01(Mathf.Abs(liveCapture.Pixels[i].b - pausedCapture.Pixels[i].b) * 4f),
                    1f);
            }

            var texture = new Texture2D(renderWidth, renderHeight, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels(diff);
                texture.Apply();
                File.WriteAllBytes(
                    PathUtils.Combine(
                        runDirectory,
                        $"diff-{pausedCapture.Beat.ToString("0.00", CultureInfo.InvariantCulture)}.png"),
                    texture.EncodeToPNG());
            }
            finally
            {
                Object.Destroy(texture);
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreAfterRenderTest()
        {
            if (driver != null)
            {
                driver.Running = false;
            }

            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            if (cameraManager != null && cameraManager.SelectedCameraController != null)
            {
                var selectedCamera = cameraManager.SelectedCameraController.Camera;
                if (selectedCamera != null)
                {
                    selectedCamera.targetTexture = null;
                }
            }

            if (helperObject != null)
            {
                Object.Destroy(helperObject);
                helperObject = null;
                driver = null;
                observer = null;
            }

            if (renderTarget != null)
            {
                renderTarget.Release();
                Object.Destroy(renderTarget);
                renderTarget = null;
            }

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null)
            {
                atsc.enabled = true;
                if (atsc.IsPlaying)
                {
                    atsc.CancelPlaying();
                }
            }

            var uiMode = Object.FindAnyObjectByType<UIMode>();
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null && cameraManager.SelectedCameraController != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            if (capturedSettings)
            {
                Settings.Instance.Animations = previousAnimations;
                Settings.Instance.Bloom = previousBloom;
                capturedSettings = false;
            }

            TearDownVirtualInput();

            if (SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
                TestUtils.CaptureCurrentMapAsSharedBaseline();
            }
        }

        // One observer runs after every ordinary LateUpdate so captures see post-refresh renderer state.
        [DefaultExecutionOrder(10000)]
        internal class RenderCaptureObserver : MonoBehaviour
        {
            private static readonly int sizeParamsId = Shader.PropertyToID("_SizeParams");

            public AudioTimeSyncController Atsc;
            public List<LightCollision> Lasers = new();
            public List<Collider> Colliders = new();
            public Camera Cam;
            public RenderTexture Target;
            public string OutputDirectory;

            public readonly Dictionary<string, RenderCapture> Captures = new();
            public bool SawMotion;
            public bool SawEnabledRenderer;

            private RenderCapture captureInFlight;

            private readonly Queue<string> pendingTags = new();
            private readonly Dictionary<Collider, Vector3> previousColliderCenters = new();
            private readonly Dictionary<LightCollision, Vector3> previousLaserDirections = new();
            // MaterialPropertyBlock must not be constructed in a field initializer; Unity forbids
            // CreateImpl from a MonoBehaviour constructor.
            private MaterialPropertyBlock propertyBlock;
            private Texture2D hdrReadback;
            private Texture2D ldrWrite;

            public void QueueCapture(string tag) => pendingTags.Enqueue(tag);

            // Called after all fields are assigned; Awake would run before Target is set.
            public void Init()
            {
                propertyBlock = new MaterialPropertyBlock();
                hdrReadback = new Texture2D(
                    Target.width, Target.height, TextureFormat.RGBAHalf, false, true);
                ldrWrite = new Texture2D(
                    Target.width, Target.height, TextureFormat.RGBA32, false);
                // Capture only this camera's post-render callback, after Cam.Render has used the collision
                // values.
                Camera.onPostRender += HandlePostRender;
            }

            private void OnDestroy()
            {
                Camera.onPostRender -= HandlePostRender;
                if (hdrReadback != null) Destroy(hdrReadback);
                if (ldrWrite != null) Destroy(ldrWrite);
            }

            private void LateUpdate()
            {
                var colliderSnapshots = SnapshotColliders();
                var motion = TrackLaserMotion();

                if (pendingTags.Count == 0)
                {
                    return;
                }

                // Re-pin the capture viewpoint at the last ordinary stage: CameraController.Update
                // rewrites the player camera's FOV every frame and animation tracks can move the rig.
                Cam.transform.SetPositionAndRotation(captureCameraPosition, captureCameraRotation);
                Cam.fieldOfView = Settings.Instance.CameraFOV;

                var capture = BuildCapture(pendingTags.Dequeue(), colliderSnapshots, motion);
                Captures[capture.Tag] = capture;
                captureInFlight = capture;
                Cam.Render();
                captureInFlight = null;
                ReadPixelsInto(capture);
                WritePng(capture);
                WriteFields(capture);
            }

            // Record collision values after the camera draws, rather than earlier in LateUpdate.
            private void HandlePostRender(Camera cam)
            {
                var capture = captureInFlight;
                if (capture == null || cam != Cam)
                {
                    return;
                }

                capture.SawPostRenderCallback = true;
                for (var i = 0; i < Lasers.Count && i < capture.Lasers.Count; i++)
                {
                    var laser = Lasers[i];
                    var laserCapture = capture.Lasers[i];
                    if (laser == null || laser.ParametricLight == null)
                    {
                        continue;
                    }

                    laserCapture.HasPostRender = true;
                    laserCapture.PostRenderCollisionLength = laser.ParametricLight.CollisionLength;
                    var sprite = laser.ParametricLight.SpriteLight;
                    if (sprite != null && sprite.Renderer != null)
                    {
                        propertyBlock.Clear();
                        sprite.Renderer.GetPropertyBlock(propertyBlock);
                        laserCapture.PostRenderSizeParams = propertyBlock.GetVector(sizeParamsId);
                    }

                    var box = laser.ParametricLight.BoxLight;
                    if (box != null)
                    {
                        laserCapture.PostRenderBoxLocalScale = box.transform.localScale;
                    }
                }
            }

            private List<(Collider collider, string id, Vector3 center, float radius)> SnapshotColliders()
            {
                var snapshots = new List<(Collider, string, Vector3, float)>(Colliders.Count);
                foreach (var collider in Colliders)
                {
                    if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var sphere = (SphereCollider)collider;
                    var colliderTransform = collider.transform;
                    var lossyScale = colliderTransform.lossyScale;
                    var center = colliderTransform.TransformPoint(sphere.center);
                    var radius = sphere.radius * Mathf.Max(
                        Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
                    if (previousColliderCenters.TryGetValue(collider, out var previousCenter)
                        && previousCenter != center)
                    {
                        SawMotion = true;
                    }

                    previousColliderCenters[collider] = center;
                    snapshots.Add((collider, ColdplayLaserCollisionTest.ChromaId(colliderTransform), center, radius));
                }

                return snapshots;
            }

            private bool TrackLaserMotion()
            {
                var motion = false;
                foreach (var laser in Lasers)
                {
                    if (laser == null || laser.ParametricLight == null)
                    {
                        continue;
                    }

                    var direction = laser.transform.up;
                    if (previousLaserDirections.TryGetValue(laser, out var previousDirection)
                        && previousDirection != direction)
                    {
                        motion = true;
                    }

                    previousLaserDirections[laser] = direction;
                    if (laser.ParametricLight.EnabledRenderers)
                    {
                        SawEnabledRenderer = true;
                    }
                }

                SawMotion |= motion;
                return motion;
            }

            private RenderCapture BuildCapture(
                string tag,
                List<(Collider collider, string id, Vector3 center, float radius)> colliderSnapshots,
                bool motion)
            {
                var capture = new RenderCapture
                {
                    Tag = tag,
                    Beat = ParseBeat(tag),
                    Frame = Time.frameCount,
                    JsonTime = Atsc.CurrentJsonTime,
                    SongBpmTime = Atsc.CurrentSongBpmTime,
                    Seconds = Atsc.CurrentSeconds,
                    Motion = motion,
                    CameraPosition = Cam.transform.position,
                    CameraRotation = Cam.transform.rotation,
                    Width = Target.width,
                    Height = Target.height,
                };

                // Measure the interior of the sphere ring at its current screen position.
                var projected = new List<Vector2>();
                foreach (var snapshot in colliderSnapshots)
                {
                    var viewport = Cam.WorldToViewportPoint(snapshot.center);
                    if (viewport.z <= 0f)
                    {
                        continue;
                    }

                    projected.Add(new Vector2(viewport.x * Target.width, viewport.y * Target.height));
                }

                capture.ProjectedSphereCount = projected.Count;
                if (projected.Count > 0)
                {
                    var centroid = Vector2.zero;
                    foreach (var point in projected) centroid += point;
                    centroid /= projected.Count;
                    var radius = 0f;
                    foreach (var point in projected)
                    {
                        radius = Mathf.Max(radius, Vector2.Distance(centroid, point));
                    }

                    capture.CircleCenter = centroid;
                    capture.CircleRadius = radius;
                    capture.InnerRadius = radius * 0.7f;
                }

                foreach (var laser in Lasers)
                {
                    capture.Lasers.Add(SnapshotLaser(laser, colliderSnapshots));
                }

                return capture;
            }

            private LaserCapture SnapshotLaser(
                LightCollision laser,
                List<(Collider collider, string id, Vector3 center, float radius)> colliderSnapshots)
            {
                var light = laser.ParametricLight;
                var ray = new Ray(laser.transform.position, laser.transform.up);
                var snapshot = new LaserCapture
                {
                    LaserId = ColdplayLaserCollisionTest.Describe(laser),
                    RenderersEnabled = light.EnabledRenderers,
                    Mask = laser.EnvironmentLayerMask.value,
                    MaxDistance = light.Length,
                    CollisionLength = light.CollisionLength,
                    Length = light.Length,
                    Color = light.Color,
                    HitPointActive = laser.HitPointGameObject != null
                        && laser.HitPointGameObject.activeInHierarchy,
                    HitPointPosition = laser.HitPointTransform != null
                        ? laser.HitPointTransform.position
                        : Vector3.zero
                };

                // Use this frame's sphere transforms so a stale CollisionLength fails the comparison.
                var bestDistance = float.MaxValue;
                string bestColliderId = null;
                foreach (var collider in colliderSnapshots)
                {
                    if (((1 << collider.collider.gameObject.layer) & snapshot.Mask) == 0)
                    {
                        continue;
                    }

                    var distance = ColdplayLaserCollisionTest.SphereIntersection(
                        ray,
                        collider.center,
                        collider.radius,
                        snapshot.MaxDistance);
                    if (distance >= 0f && distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestColliderId = collider.id;
                    }
                }

                snapshot.ExpectedHit = bestDistance != float.MaxValue;
                snapshot.ExpectedDistance = bestDistance;
                snapshot.ExpectedColliderId = bestColliderId;

                if (light.BoxLight != null)
                {
                    var box = light.BoxLight;
                    snapshot.BoxCollisionHeight = box.CollisionHeight;
                    snapshot.BoxHeight = box.Height;
                    snapshot.BoxRendererEnabled = box.Renderer != null && box.Renderer.enabled;
                }

                if (light.SpriteLight != null)
                {
                    var sprite = light.SpriteLight;
                    snapshot.SpriteCollisionLength = sprite.CollisionLength;
                    snapshot.SpriteLength = sprite.Length;
                    snapshot.SpriteRendererEnabled = sprite.Renderer != null && sprite.Renderer.enabled;
                }

                return snapshot;
            }

            private void ReadPixelsInto(RenderCapture capture)
            {
                var previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = Target;
                    hdrReadback.ReadPixels(
                        new Rect(0f, 0f, Target.width, Target.height), 0, 0, false);
                    hdrReadback.Apply();
                    capture.Pixels = hdrReadback.GetPixels();
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }

            private void WritePng(RenderCapture capture)
            {
                var ldr = new Color[capture.Pixels.Length];
                for (var i = 0; i < ldr.Length; i++)
                {
                    // Reinhard keeps saturated beam detail visible instead of clipping to flat white.
                    var pixel = capture.Pixels[i];
                    ldr[i] = new Color(
                        pixel.r / (1f + Mathf.Max(pixel.r, 0f)),
                        pixel.g / (1f + Mathf.Max(pixel.g, 0f)),
                        pixel.b / (1f + Mathf.Max(pixel.b, 0f)),
                        1f);
                }

                ldrWrite.SetPixels(ldr);
                ldrWrite.Apply();
                File.WriteAllBytes(
                    PathUtils.Combine(OutputDirectory, $"{capture.Tag}.png"),
                    ldrWrite.EncodeToPNG());
            }

            private void WriteFields(RenderCapture capture)
            {
                var builder = new StringBuilder();
                builder.AppendLine(
                    $"tag={capture.Tag} frame={capture.Frame}"
                    + $" json={capture.JsonTime.ToString("0.0000", CultureInfo.InvariantCulture)}"
                    + $" songBpm={capture.SongBpmTime.ToString("0.0000", CultureInfo.InvariantCulture)}"
                    + $" seconds={capture.Seconds.ToString("0.0000", CultureInfo.InvariantCulture)}"
                    + $" motion={capture.Motion} spheres={capture.ProjectedSphereCount}"
                    + $" circle={capture.CircleCenter} r={capture.CircleRadius.ToString("0.0", CultureInfo.InvariantCulture)}"
                    + $" cam={capture.CameraPosition} {capture.CameraRotation.eulerAngles}");
                builder.AppendLine($"postRenderCallback={capture.SawPostRenderCallback}");
                foreach (var laser in capture.Lasers)
                {
                    builder.AppendLine(laser.Describe());
                }

                File.WriteAllText(
                    PathUtils.Combine(OutputDirectory, $"fields-{capture.Tag}.txt"),
                    builder.ToString());
            }

            private static float ParseBeat(string tag)
            {
                var separator = tag.IndexOf('-');
                return separator >= 0
                    && float.TryParse(
                        tag[(separator + 1)..],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var beat)
                        ? beat
                        : float.NaN;
            }

            internal class LaserCapture
            {
                public string LaserId;
                public bool RenderersEnabled;
                public int Mask;
                public float MaxDistance;
                public float CollisionLength;
                public float Length;
                public Color Color;
                public bool ExpectedHit;
                public float ExpectedDistance;
                public string ExpectedColliderId;
                public float BoxCollisionHeight;
                public float BoxHeight;
                public bool BoxRendererEnabled;
                public float SpriteCollisionLength;
                public float SpriteLength;
                public bool SpriteRendererEnabled;
                public bool HitPointActive;
                public Vector3 HitPointPosition;
                public bool HasPostRender;
                public Vector4 PostRenderSizeParams;
                public Vector3 PostRenderBoxLocalScale;
                public float PostRenderCollisionLength;

                public string Describe() =>
                    $"{LaserId} enabled={RenderersEnabled}"
                    + $" collisionLength={CollisionLength:R} length={Length:R} color={Color}"
                    + $" expected={(ExpectedHit ? ExpectedDistance.ToString("R", CultureInfo.InvariantCulture) : "none")}@{ExpectedColliderId}"
                    + $"\n  box collisionHeight={BoxCollisionHeight:R} height={BoxHeight:R}"
                    + $" rendererEnabled={BoxRendererEnabled}"
                    + $"\n  sprite collisionLength={SpriteCollisionLength:R} length={SpriteLength:R}"
                    + $" rendererEnabled={SpriteRendererEnabled}"
                    + $"\n  hitpoint active={HitPointActive} pos={HitPointPosition}"
                    + $"\n  postRender={(HasPostRender ? "" : "none ")}"
                    + $"sizeParams={PostRenderSizeParams} boxScale={PostRenderBoxLocalScale}"
                    + $" collisionLength={PostRenderCollisionLength:R}";
            }

            internal class RenderCapture
            {
                public string Tag;
                public float Beat;
                public int Frame;
                public float JsonTime;
                public float SongBpmTime;
                public float Seconds;
                public bool Motion;
                public Vector3 CameraPosition;
                public Quaternion CameraRotation;
                public int ProjectedSphereCount;
                public Vector2 CircleCenter;
                public float CircleRadius;
                public float InnerRadius;
                public int Width;
                public int Height;
                public Color[] Pixels;
                public bool SawPostRenderCallback;
                public readonly List<LaserCapture> Lasers = new();

                private int innerPixelCount;
                private float innerMeanLuminance;
                private float innerP95Luminance;
                private float frameMeanLuminance;
                private float[] innerLuminances;
                private bool measured;

                public float FrameMeanLuminance
                {
                    get { EnsureMeasured(); return frameMeanLuminance; }
                }

                public float InnerMeanLuminance
                {
                    get { EnsureMeasured(); return innerMeanLuminance; }
                }

                public float InnerP95Luminance
                {
                    get { EnsureMeasured(); return innerP95Luminance; }
                }

                public int InnerPixelCount
                {
                    get { EnsureMeasured(); return innerPixelCount; }
                }

                // Fraction of ring-interior pixels brighter than the caller-provided cut.
                public float InnerWhiteFraction(float cut)
                {
                    EnsureMeasured();
                    if (innerPixelCount == 0)
                    {
                        return 0f;
                    }

                    var over = 0;
                    foreach (var luminance in innerLuminances)
                    {
                        if (luminance >= cut) over++;
                    }

                    return over / (float)innerPixelCount;
                }

                private void EnsureMeasured()
                {
                    if (measured)
                    {
                        return;
                    }

                    measured = true;
                    innerLuminances = Array.Empty<float>();
                    if (Pixels == null || Pixels.Length != Width * Height)
                    {
                        return;
                    }

                    var inner = new List<float>(Pixels.Length / 4);
                    double frameSum = 0;
                    var innerSum = 0.0;
                    for (var y = 0; y < Height; y++)
                    {
                        for (var x = 0; x < Width; x++)
                        {
                            var pixel = Pixels[(y * Width) + x];
                            var luminance = (0.2126f * pixel.r) + (0.7152f * pixel.g) + (0.0722f * pixel.b);
                            frameSum += luminance;
                            if (InnerRadius > 0f
                                && Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), CircleCenter)
                                    <= InnerRadius)
                            {
                                inner.Add(luminance);
                                innerSum += luminance;
                            }
                        }
                    }

                    frameMeanLuminance = (float)(frameSum / Pixels.Length);
                    innerPixelCount = inner.Count;
                    if (inner.Count > 0)
                    {
                        inner.Sort();
                        innerMeanLuminance = (float)(innerSum / inner.Count);
                        innerP95Luminance = inner[Mathf.Min(inner.Count - 1, (int)(inner.Count * 0.95f))];
                        innerLuminances = inner.ToArray();
                    }
                }
            }
        }
    }
}

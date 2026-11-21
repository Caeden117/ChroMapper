using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class ColdplayLaserCollisionTest : BasicEventChunkingTestBase
    {
        internal const string MinimalFixtureFileName = "ColdplayLaserCollisionMinimalFixture.json";
        internal const string FixtureEnvironment = "ColdplayEnvironment";
        internal const int FixtureSongLengthSeconds = 60;
        internal const int SpotlightGroupId = 13;
        internal const int CollisionLayer = 14;
        private const float sweepStartJsonTime = 133.5f;
        private const float sweepEndJsonTime = 140.6f;
        private const float collisionTolerance = 0.01f;
        private const int settleFrames = 4;
        private static readonly float[] scrubControlJsonTimes = { 134.5f, 136f, 137f, 138.5f, 140f };

        private GameObject helperObject;
        private DeterministicSecondsDriver driver;
        private CollisionFrameObserver observer;
        private bool previousAnimations;
        private bool capturedAnimations;

        [UnityTest]
        public IEnumerator MinimalFixtureSpotlightCollisionMatchesSameFrameGeometry()
        {
            yield return RunCollisionAssertion();
        }

        internal static JSONNode LoadMinimalFixtureJson()
        {
            var fixturePath = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                MinimalFixtureFileName);
            Assert.That(File.Exists(fixturePath), Is.True, $"Missing minimal fixture at {fixturePath}");
            return JSON.Parse(File.ReadAllText(fixturePath));
        }

        private IEnumerator RunCollisionAssertion()
        {
            const string route = "minimal fixture";
            yield return TestUtils.ReloadMap(
                3,
                LoadMinimalFixtureJson(),
                MinimalEditorState(),
                100f,
                FixtureEnvironment,
                FixtureSongLengthSeconds);

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

            var bpmAt137 = map.BpmAtJsonTime(137f);
            Assert.That(
                bpmAt137.HasValue,
                Is.True,
                $"{route}: the fixture's BPM regions were not loaded.");
            Assert.That(
                bpmAt137.Value,
                Is.EqualTo(286f).Within(2f),
                $"{route}: the BPM region covering authored beat 137 must keep its ~286 BPM;"
                + $" the Info.dat 100 BPM is only nominal. Actual {bpmAt137.Value}.");

            var lasers = FindSpotlightLasers();
            Assert.That(
                lasers.Count,
                Is.EqualTo(16),
                $"{route}: expected the 16 group-{SpotlightGroupId} LasersSpotlights LightCollision lasers"
                + $" in {FixtureEnvironment}, found {lasers.Count}.");
            foreach (var laser in lasers)
            {
                Assert.That(
                    laser.EnvironmentLayerMask.value,
                    Is.EqualTo(1 << CollisionLayer),
                    $"{route}: {Describe(laser)} must raycast layer {CollisionLayer}.");
                Assert.That(laser.UseScale, Is.False, $"{route}: {Describe(laser)} must not use scale.");
                Assert.That(laser.ShowHitPoint, Is.True, $"{route}: {Describe(laser)} must show its hit point.");
                Assert.That(
                    laser.ParametricLight.BoxLight,
                    Is.Not.Null,
                    $"{route}: {Describe(laser)} must have a ParametricBoxLight.");
            }

            var environmentScene = lasers[0].gameObject.scene;
            var colliders = Object
                .FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.gameObject.scene == environmentScene
                    && ((1 << c.gameObject.layer) & lasers[0].EnvironmentLayerMask.value) != 0)
                .ToList();
            Assert.That(
                colliders.Count,
                Is.EqualTo(32),
                $"{route}: expected the 32 layer-{CollisionLayer} SphereScale colliders, found {colliders.Count}.");
            var nonSphere = colliders.Where(c => c is not SphereCollider).ToList();
            Assert.That(
                nonSphere.Count,
                Is.EqualTo(0),
                $"{route}: non-sphere colliders on the collision mask need separate handling:"
                + $" {string.Join(", ", nonSphere.Select(Describe))}");
#pragma warning disable CS0618
            Assert.That(Physics.autoSyncTransforms, Is.False, $"{route}: repro requires deferred PhysX sync.");
#pragma warning restore CS0618

            if (lightshow.Mode != LightshowMode.Full)
            {
                lightshow.SetMode(LightshowMode.Full);
            }

            if (!capturedAnimations)
            {
                previousAnimations = Settings.Instance.Animations;
                capturedAnimations = true;
            }

            Settings.Instance.Animations = true;
            InitializeVirtualInput(false);
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);

            helperObject = new GameObject("Coldplay laser collision determinism");
            driver = helperObject.AddComponent<DeterministicSecondsDriver>();
            observer = helperObject.AddComponent<CollisionFrameObserver>();
            driver.Atsc = atsc;
            observer.Atsc = atsc;
            observer.CameraManager = cameraManager;
            observer.Lasers = lasers;
            observer.Colliders = colliders;
            observer.Tolerance = collisionTolerance;
            observer.Recording = true;
            observer.Frames.Clear();
            observer.ClearMotionBaseline();

            var settledMismatches = new List<string>();
            foreach (var stop in scrubControlJsonTimes)
            {
                atsc.MoveToJsonTime(stop);
                for (var frame = 0; frame < settleFrames; frame++)
                {
                    // WaitForFixedUpdate lets auto-simulation catch PhysX up, then one more frame so
                    // LightCollision.RefreshCollision raycasts the synced state before the observer records.
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                if (observer.Frames.Count > 0)
                {
                    settledMismatches.AddRange(
                        CollectMismatches(
                            new List<CollisionFrameObserver.FrameSnapshot> { observer.Frames[^1] })
                            .Select(m => $"settled stop {stop.ToString(CultureInfo.InvariantCulture)} {m}"));
                }
            }

            Assert.That(
                observer.Frames.Count,
                Is.GreaterThan(0),
                $"{route}: the selected camera never rendered; the post-render observer recorded no frames.");
            observer.Frames.Clear();
            observer.ClearMotionBaseline();
            Assert.That(
                settledMismatches.Count,
                Is.EqualTo(0),
                $"{route}: settled scrubs must match the same-frame analytic collision, found"
                + $" {settledMismatches.Count} mismatched laser frames:\n{string.Join("\n", settledMismatches.Take(5))}");

            var startSongBpmTime = (float)map.JsonTimeToSongBpmTime(sweepStartJsonTime);
            var endSongBpmTime = (float)map.JsonTimeToSongBpmTime(sweepEndJsonTime);
            var endSeconds = atsc.GetSecondsFromBeat(endSongBpmTime);
            Assert.That(
                float.IsFinite(endSeconds) && endSeconds > 0f && endSeconds < FixtureSongLengthSeconds,
                Is.True,
                $"{route}: sweep end seconds must be finite inside the song, got {endSeconds}.");

            var allMismatches = new List<string>();
            var sawExpectedHit = false;
            var sawMotion = false;
            var stepPatterns = new[]
            {
                new[] { 1f / 60f },
                new[] { 1f / 120f },
            };
            foreach (var stepPattern in stepPatterns)
            {
                var stepLabel = string.Join(
                    "/",
                    stepPattern.Select(step => step.ToString("F4", CultureInfo.InvariantCulture)));
                atsc.MoveToJsonTime(sweepStartJsonTime);
                yield return null;
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(atsc, startSongBpmTime);
                atsc.enabled = false;
                observer.Frames.Clear();
                observer.ClearMotionBaseline();
                driver.Seconds = atsc.CurrentSeconds;
                var startSeconds = driver.Seconds;
                driver.SecondsPerFrame = stepPattern[0];
                driver.StepPattern = stepPattern.Length > 1 ? stepPattern : null;
                driver.Running = true;
                yield return null;
                yield return null;
                Assert.That(
                    driver.Seconds,
                    Is.GreaterThan(startSeconds),
                    $"{route}: deterministic seconds driver did not advance playback time.");
                var sweepFrames = 0;
                const int sweepFrameLimit = 600;
                while (driver.Seconds < endSeconds && sweepFrames < sweepFrameLimit)
                {
                    sweepFrames++;
                    yield return null;
                }

                driver.Running = false;
                driver.StepPattern = null;
                Assert.That(
                    sweepFrames,
                    Is.LessThan(sweepFrameLimit),
                    $"{route}: sweep did not reach {endSeconds} seconds within {sweepFrameLimit} frames"
                    + $" (stalled at {driver.Seconds} seconds).");
                yield return null;
                atsc.enabled = true;
                TestUtils.PauseDeterministicPlayback(atsc);

                Assert.That(
                    observer.Frames.Count,
                    Is.GreaterThan(0),
                    $"{route}: the selected camera never rendered during the {stepLabel}s step;"
                    + " the post-render observer recorded no frames.");
                sawExpectedHit |= observer.Frames.Any(f => f.Lasers.Any(l => l.Enabled && l.ExpectedHit));
                sawMotion |= observer.Frames.Any(f => f.Motion);
                allMismatches.AddRange(
                    CollectMismatches(observer.Frames)
                        .Select(m => $"step {stepLabel}s {m}"));
            }

            Assert.That(
                sawExpectedHit,
                Is.True,
                $"{route}: no analytic sphere hit on an enabled laser occurred in the sweep window.");
            Assert.That(
                sawMotion,
                Is.True,
                $"{route}: no collider or laser transform moved during the sweep window.");
            Assert.That(
                allMismatches.Count,
                Is.EqualTo(0),
                $"{route}: rendered spotlight collision did not match same-frame geometry in"
                + $" {allMismatches.Count} laser frames:\n{string.Join("\n", allMismatches.Take(10))}");
        }

        internal static List<LightCollision> FindSpotlightLasers() =>
            Object
                .FindObjectsByType<LightCollision>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.ParametricLight != null
                    && c.ParametricLight.Kind == LightController.LightKind.Group
                    && c.ParametricLight.Type == SpotlightGroupId
                    && HierarchyPath(c.transform).Contains("LasersSpotlights"))
                .OrderBy(c => c.ParametricLight.ID)
                .ToList();

        internal static string HierarchyPath(Transform transform)
        {
            var names = new List<string>();
            for (var node = transform; node != null; node = node.parent)
            {
                names.Add(node.name);
            }

            names.Reverse();
            return string.Join("/", names);
        }

        internal static string Describe(LightCollision laser) =>
            $"type {laser.ParametricLight.Type} id {laser.ParametricLight.ID} {ChromaId(laser.transform)}";

        private static string Describe(Collider collider) =>
            $"{collider.GetType().Name} {ChromaId(collider.transform)}";

        internal static string ChromaId(Transform transform) =>
            transform.GetComponentInParent<ChromaIDMarker>() is { } marker
                ? marker.ChromaID
                : HierarchyPath(transform);

        internal static float SphereIntersection(Ray ray, Vector3 center, float radius, float maxDistance)
        {
            var offset = center - ray.origin;
            var radiusSquared = radius * radius;
            if (offset.sqrMagnitude < radiusSquared)
            {
                return -1f;
            }

            var along = Vector3.Dot(offset, ray.direction);
            var distanceSquared = offset.sqrMagnitude - (along * along);
            if (distanceSquared > radiusSquared)
            {
                return -1f;
            }

            var distance = along - Mathf.Sqrt(radiusSquared - distanceSquared);
            return distance >= 0f && distance <= maxDistance ? distance : -1f;
        }

        private static List<string> CollectMismatches(
            List<CollisionFrameObserver.FrameSnapshot> frames)
        {
            var mismatches = new List<string>();
            foreach (var frame in frames)
            {
                foreach (var laser in frame.Lasers)
                {
                    if (!laser.Mismatch)
                    {
                        continue;
                    }

                    mismatches.Add(
                        $"frame {frame.Frame} json {frame.JsonTime.ToString("F4", CultureInfo.InvariantCulture)}"
                        + $" [{laser.LaserId}] rendered {laser.RenderedCollisionLength:R}"
                        + $" box {laser.BoxCollisionHeight:R} expected {(laser.ExpectedHit ? laser.ExpectedDistance.ToString("R", CultureInfo.InvariantCulture) : "none")}"
                        + $" hit {laser.ExpectedColliderId}"
                        + $" hitpoint {laser.HitPointActive}@{laser.HitPointPosition}"
                        + $" origin={laser.Origin} dir={laser.Direction}");
                }
            }

            return mismatches;
        }

        // The map's saved editor position is beat 140.203125, in the middle of the ring motion.
        internal static JSONObject MinimalEditorState() =>
            new()
            {
                ["components"] = new JSONObject
                {
                    ["currentJsonTime"] = new JSONObject { ["value"] = 140.203125f },
                },
            };

        [UnityTearDown]
        public IEnumerator RestoreAfterCollisionTest()
        {
            if (driver != null)
            {
                driver.Running = false;
            }

            if (observer != null)
            {
                observer.Recording = false;
            }

            if (helperObject != null)
            {
                Object.Destroy(helperObject);
                helperObject = null;
                driver = null;
                observer = null;
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

            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            if (cameraManager != null && cameraManager.SelectedCameraController != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            if (capturedAnimations)
            {
                Settings.Instance.Animations = previousAnimations;
                capturedAnimations = false;
            }

            TearDownVirtualInput();

            if (SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
                TestUtils.CaptureCurrentMapAsSharedBaseline();
            }
        }

        // The explicit render in LateUpdate is what makes the camera pipeline fire in batchmode; the
        // snapshot itself is still taken from the camera post-render callback so the recorded values
        // are the ones the frame actually drew.
        [DefaultExecutionOrder(10000)]
        private class CollisionFrameObserver : MonoBehaviour
        {
            public AudioTimeSyncController Atsc;
            public CameraManager CameraManager;
            public List<LightCollision> Lasers = new();
            public List<Collider> Colliders = new();
            public readonly List<FrameSnapshot> Frames = new();
            public bool Recording;
            public float Tolerance;

            public class ColliderSnapshot
            {
                public Collider Collider;
                public string Id;
                public Vector3 Center;
                public float Radius;
            }

            public class LaserSnapshot
            {
                public string LaserId;
                public bool Enabled;
                public Vector3 Origin;
                public Vector3 Direction;
                public float MaxDistance;
                public float RenderedCollisionLength;
                public float BoxCollisionHeight;
                public bool HitPointActive;
                public Vector3 HitPointPosition;
                public bool ExpectedHit;
                public float ExpectedDistance;
                public string ExpectedColliderId;
                public bool Mismatch;
            }

            public class FrameSnapshot
            {
                public int Frame;
                public float JsonTime;
                public bool Motion;
                public readonly List<LaserSnapshot> Lasers = new();
            }

            private readonly Dictionary<Collider, Vector3> previousColliderCenters = new();
            private readonly Dictionary<LightCollision, Vector3> previousLaserDirections = new();

            public void ClearMotionBaseline()
            {
                previousColliderCenters.Clear();
                previousLaserDirections.Clear();
            }

            private void OnEnable()
            {
                Camera.onPostRender += HandleCameraPostRender;
            }

            private void OnDisable()
            {
                Camera.onPostRender -= HandleCameraPostRender;
            }

            private void LateUpdate()
            {
                if (!Recording || Lasers.Count == 0)
                {
                    return;
                }

                var camera = SelectedCamera();
                if (camera == null || !camera.isActiveAndEnabled)
                {
                    return;
                }

                camera.Render();
            }

            private Camera SelectedCamera()
            {
                var selectedCameraController = CameraManager != null
                    ? CameraManager.SelectedCameraController
                    : null;
                return selectedCameraController != null
                    ? selectedCameraController.Camera
                    : null;
            }

            // The rendered collision values are only final once the selected camera has drawn; sampling
            // earlier would read the stale pre-render state the same way gameplay rendering did.
            private void HandleCameraPostRender(Camera cam)
            {
                if (!Recording || Lasers.Count == 0)
                {
                    return;
                }

                if (cam != SelectedCamera())
                {
                    return;
                }

                var frame = new FrameSnapshot
                {
                    Frame = Time.frameCount,
                    JsonTime = Atsc.CurrentJsonTime,
                };

                var colliderSnapshots = new List<ColliderSnapshot>(Colliders.Count);
                foreach (var collider in Colliders)
                {
                    if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var sphere = (SphereCollider)collider;
                    var colliderTransform = collider.transform;
                    var lossyScale = colliderTransform.lossyScale;
                    var snapshot = new ColliderSnapshot
                    {
                        Collider = collider,
                        Id = ChromaId(colliderTransform),
                        Center = colliderTransform.TransformPoint(sphere.center),
                        Radius = sphere.radius * Mathf.Max(
                            Mathf.Abs(lossyScale.x),
                            Mathf.Abs(lossyScale.y),
                            Mathf.Abs(lossyScale.z))
                    };

                    if (previousColliderCenters.TryGetValue(collider, out var previousCenter)
                        && previousCenter != snapshot.Center)
                    {
                        frame.Motion = true;
                    }

                    previousColliderCenters[collider] = snapshot.Center;
                    colliderSnapshots.Add(snapshot);
                }

                foreach (var laser in Lasers)
                {
                    if (laser == null || laser.ParametricLight == null)
                    {
                        continue;
                    }

                    var light = laser.ParametricLight;
                    var laserTransform = laser.transform;
                    var snapshot = new LaserSnapshot
                    {
                        LaserId = Describe(laser),
                        Enabled = light.EnabledRenderers,
                        Origin = laserTransform.position,
                        Direction = laserTransform.up,
                        MaxDistance = light.Length,
                        RenderedCollisionLength = light.CollisionLength,
                        BoxCollisionHeight = light.BoxLight.CollisionHeight,
                        HitPointActive = laser.HitPointGameObject != null
                            && laser.HitPointGameObject.activeInHierarchy,
                        HitPointPosition = laser.HitPointTransform != null
                            ? laser.HitPointTransform.position
                            : Vector3.zero
                    };

                    if (previousLaserDirections.TryGetValue(laser, out var previousDirection)
                        && previousDirection != snapshot.Direction)
                    {
                        frame.Motion = true;
                    }

                    previousLaserDirections[laser] = snapshot.Direction;

                    var mask = laser.EnvironmentLayerMask.value;
                    var ray = new Ray(snapshot.Origin, snapshot.Direction);
                    var bestDistance = float.MaxValue;
                    string bestColliderId = null;
                    foreach (var collider in colliderSnapshots)
                    {
                        if (((1 << collider.Collider.gameObject.layer) & mask) == 0)
                        {
                            continue;
                        }

                        var distance = SphereIntersection(
                            ray,
                            collider.Center,
                            collider.Radius,
                            snapshot.MaxDistance);
                        if (distance >= 0f && distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestColliderId = collider.Id;
                        }
                    }

                    snapshot.ExpectedHit = bestDistance != float.MaxValue;
                    snapshot.ExpectedDistance = bestDistance;
                    snapshot.ExpectedColliderId = bestColliderId;

                    if (snapshot.Enabled)
                    {
                        var expectedHitPoint = snapshot.Origin + (snapshot.Direction * snapshot.ExpectedDistance);
                        var lengthMatches = snapshot.ExpectedHit
                            ? Mathf.Abs(snapshot.RenderedCollisionLength - snapshot.ExpectedDistance)
                                <= Tolerance
                            : snapshot.RenderedCollisionLength == float.MaxValue;
                        var boxMatches = snapshot.ExpectedHit
                            ? Mathf.Abs(snapshot.BoxCollisionHeight - snapshot.ExpectedDistance)
                                <= Tolerance
                            : snapshot.BoxCollisionHeight == float.MaxValue;
                        var hitPointMatches = snapshot.ExpectedHit
                            ? snapshot.HitPointActive
                                && Vector3.Distance(snapshot.HitPointPosition, expectedHitPoint)
                                    <= Tolerance
                            : !snapshot.HitPointActive;
                        snapshot.Mismatch = !(lengthMatches && boxMatches && hitPointMatches);
                    }

                    frame.Lasers.Add(snapshot);
                }

                Frames.Add(frame);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class SuperluminalMoleculeParityTest : TestBase
    {
        private const string ParentTrackName = "MainSceneTrack";
        private static readonly string[] PartTrackNames = { "modelScene0_sph_0", "modelScene0_wires_0" };
        private bool previousAnimations;
        private UIMode uiMode;
        private CameraManager cameras;
        private AudioTimeSyncController clock;
        private bool previousClockEnabled;
        private bool clockCaptured;
        private JSONNode oracleData;
        private string fixtureName;
        private readonly Dictionary<string, Renderer> parts = new();
        private readonly Dictionary<string, List<JSONNode>> requests = new();
        private readonly List<string> errors = new();
        private float largestPositionError;
        private float largestRotationError;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            parts.Clear();
            requests.Clear();
            errors.Clear();
            largestPositionError = 0f;
            largestRotationError = 0f;
            clockCaptured = false;
            clock = null;
            uiMode = null;
            cameras = null;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Local reproduction needs the exact original Superluminal map.")]
        public IEnumerator FullMapMoleculesFollowNativeHeldTransformsThroughParentMotion()
        {
            var path = System.Environment.GetEnvironmentVariable("CHROMAPPER_SUPERLUMINAL_FULL_MAP_PATH")
                ?? PathUtils.Combine(Application.dataPath, "..", "TestResults", "Superluminal", "SuperluminalFullMapFixture.json");
            if (!File.Exists(path))
                Assert.Ignore("The optional full-source reproduction requires the ignored original map.");

            yield return VerifyMolecules(JSON.Parse(File.ReadAllText(path)), "full");
        }

        [UnityTest]
        public IEnumerator MoleculesFollowNativeHeldTransformsThroughParentMotion()
        {
            var path = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "SuperluminalMoleculeFixture.json");
            yield return VerifyMolecules(JSON.Parse(File.ReadAllText(path)), "reduced");
        }

        private IEnumerator VerifyMolecules(JSONNode data, string nameOfFixture)
        {
            parts.Clear();
            requests.Clear();
            errors.Clear();
            largestPositionError = 0f;
            largestRotationError = 0f;
            clockCaptured = false;
            clock = null;
            uiMode = null;
            cameras = null;
            oracleData = data;
            fixtureName = nameOfFixture;
            foreach (var name in PartTrackNames.Concat(new[] { ParentTrackName }))
                requests.Add(
                    name,
                    oracleData["customData"]["customEvents"].Children
                    .Where(ev => ev["t"].Value == "AnimateTrack" && HasTrack(ev["d"]["track"], name))
                    .OrderBy(ev => ev["b"].AsFloat).ToList());

            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus", NoteJumpSpeed = 16, NoteStartBeatOffset = 0,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                CustomColorLeft = new Color(0.6039216f, 0.254902f, 0.9960784f),
                CustomColorRight = new Color(0.1294118f, 0.2156863f, 0.827451f),
                LightshowFileName = "MissingTestLightshow.dat"
            };
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                oracleData.Clone(),
                beatsPerMinute: 150,
                environmentName: "GagaEnvironment",
                songLengthSeconds: 270,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clockCaptured = true;
            clock.enabled = false;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            var geometries = Object.FindAnyObjectByType<GeometryGridContainer>().LoadedContainers.Values
                .OfType<GeometryContainer>().ToArray();
            foreach (var name in PartTrackNames)
                parts.Add(
                    name,
                    geometries.Single(part => part.EnvironmentEnhancement.Track == name)
                    .MpbController.Renderers.Single());

            foreach (var beat in new[]
                     {
                         263f, 263.001f, 263.999f, 264f, 264.001f, 265.5f, 268.999f, 269f,
                         269.001f, 274.999f, 275f, 275.001f, 291.999f, 292f, 292.001f, 292.999f, 293f
                     })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                RecordPose(beat, "cold");
            }

            foreach (var window in new[] { (Start: 263f, End: 275f), (Start: 292f, End: 308f) })
            {
                for (var sample = 0; sample <= (window.End - window.Start) * 16; sample++)
                {
                    var beat = window.Start + (sample / 16f);
                    clock.MoveToJsonTime(beat);
                    yield return null;
                    yield return null;
                    RecordPose(beat, "dense cold");
                }
            }

            clock.MoveToJsonTime(263f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 263f);
            var seconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
            for (var sample = 0; sample <= 720; sample++)
            {
                var beat = 263f + (sample / 16f);
                seconds.SetValue(clock, clock.GetSecondsFromBeat(beat));
                yield return null;
                yield return null;
                RecordPose(beat, "playing");
            }

            TestUtils.PauseDeterministicPlayback(clock);
            foreach (var beat in new[] { 308f, 307.999f, 304f, 296f, 293f, 292.001f, 292f, 275.001f, 275f, 274.999f, 269.001f, 269f, 264.001f, 264f, 263.999f, 263.001f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                RecordPose(beat, "reverse");
            }

            foreach (var beat in new[] { 267.125f, 269.001f, 300f, 307.999f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var actual = RenderMask(beat, false);
                var native = RenderMask(beat, true);
                var nativePixels = native.Count(visible => visible);
                var differentPixels = actual.Zip(native, (left, right) => left != right).Count(different => different);
                if (nativePixels < 10 || differentPixels > 4)
                    errors.Add($"b{beat} rendered native footprint={nativePixels}px, differing actual/native silhouette={differentPixels}px.");

            }

            Assert.That(
                errors,
                Is.Empty,
                $"Native constant child writes must inherit the animated parent. Max position error={largestPositionError}, rotation={largestRotationError}.\n"
                + string.Join("\n", errors));
        }

        private static bool HasTrack(JSONNode data, string name) => data is JSONString
            ? data.Value == name
            : data is JSONArray array && array.Children.Any(track => track.Value == name);

        private JSONNode LastRequest(string track, string key, float beat)
        {
            JSONNode latest = null;
            foreach (var request in requests[track])
            {
                if (request["b"].AsFloat <= beat && request["d"].HasKey(key))
                    latest = request;

            }

            return latest;
        }

        private JSONNode Points(JSONNode data) => data is JSONString
            ? oracleData["customData"]["pointDefinitions"][data.Value]
            : data;

        private static Vector3 Vector(JSONNode row) => new(row[0].AsFloat, row[1].AsFloat, row[2].AsFloat);

        private static float Duration(JSONNode request) => request["d"].GetValueOrDefault("duration", null)?.AsFloat ?? 0f;

        private static float NormalTime(JSONNode request, float beat) => Duration(request) == 0f
            ? 1f
            : Mathf.Clamp01((beat - request["b"].AsFloat) / Duration(request));

        private static Vector3 SampleVector(JSONNode points, float normal)
        {
            if (!points[0].IsArray)
                return Vector(points);

            for (var i = 1; i < points.Count; i++)
            {
                if (normal < points[i][3].AsFloat)
                {
                    var previous = points[i - 1];
                    var next = points[i];
                    return Vector3.Lerp(
                        Vector(previous),
                        Vector(next),
                        (normal - previous[3].AsFloat) / (next[3].AsFloat - previous[3].AsFloat));
                }
            }

            return Vector(points[points.Count - 1]);
        }

        private static Quaternion SampleRotation(JSONNode points, float normal)
        {
            if (!points[0].IsArray)
                return Quaternion.Euler(Vector(points));

            for (var i = 1; i < points.Count; i++)
            {
                if (normal < points[i][3].AsFloat)
                {
                    var previous = points[i - 1];
                    var next = points[i];
                    return Quaternion.Slerp(
                        Quaternion.Euler(Vector(previous)),
                        Quaternion.Euler(Vector(next)),
                        (normal - previous[3].AsFloat) / (next[3].AsFloat - previous[3].AsFloat));
                }
            }

            return Quaternion.Euler(Vector(points[points.Count - 1]));
        }

        private Matrix4x4 ParentMatrix(float beat)
        {
            var position = LastRequest(ParentTrackName, "position", beat);
            var rotation = LastRequest(ParentTrackName, "rotation", beat);
            return Matrix4x4.TRS(
                position == null
                    ? Vector3.zero
                    : SampleVector(Points(position["d"]["position"]), NormalTime(position, beat)),
                rotation == null
                    ? Quaternion.identity
                    : SampleRotation(Points(rotation["d"]["rotation"]), NormalTime(rotation, beat)),
                Vector3.one);
        }

        private (Vector3 Position, Quaternion Rotation, Vector3 Scale) NativePose(string name, float beat)
        {
            var position = LastRequest(name, "position", beat);
            var rotation = LastRequest(name, "rotation", beat);
            var scale = LastRequest(name, "scale", beat);
            var positionPoints = Points(position["d"]["position"]);
            Assert.That(
                !positionPoints[0].IsArray || positionPoints.Count == 1,
                Is.True,
                "This independent oracle covers the source's literal single-point child writes, not arbitrary grammar.");
            var positionWrite = NativeConstantWriteTime(name, beat);
            var worldPosition = ParentMatrix(beat).MultiplyPoint3x4(
                ParentMatrix(positionWrite).inverse.MultiplyPoint3x4(SampleVector(positionPoints, 1f)));
            var worldRotation = Quaternion.identity;
            if (rotation != null)
            {
                var rotationWrite = positionWrite;
                worldRotation = ParentMatrix(beat).rotation * Quaternion.Inverse(ParentMatrix(rotationWrite).rotation)
                    * SampleRotation(Points(rotation["d"]["rotation"]), 1f);
            }

            return (worldPosition, worldRotation, scale == null
                ? Vector3.one
                : SampleVector(Points(scale["d"]["scale"]), 1f));
        }

        // Native constant requests mark a track updated only when a value changes, including repeated yeet requests.
        private float NativeConstantWriteTime(string name, float beat)
        {
            var values = new Dictionary<string, Vector3>();
            var latest = 0f;
            foreach (var request in requests[name])
            {
                if (request["b"].AsFloat > beat)
                    break;

                var changed = false;
                foreach (var key in new[] { "position", "rotation", "scale" })
                {
                    if (!request["d"].HasKey(key))
                        continue;

                    var points = Points(request["d"][key]);
                    Assert.That(
                        !points[0].IsArray || points.Count == 1,
                        Is.True,
                        "Selected source children contain constant transform requests only.");
                    var value = SampleVector(points, 1f);
                    changed |= !values.TryGetValue(key, out var previous) || Vector3.Distance(previous, value) > 0.00001f;
                    values[key] = value;
                }

                if (changed)
                    latest = request["b"].AsFloat;

            }

            return latest;
        }

        private void RecordPose(float beat, string route)
        {
            foreach (var part in parts)
            {
                var expected = NativePose(part.Key, beat);
                var transform = part.Value.transform;
                var positionError = Vector3.Distance(transform.position, expected.Position);
                var rotationError = Quaternion.Angle(transform.rotation, expected.Rotation);
                largestPositionError = Mathf.Max(largestPositionError, positionError);
                largestRotationError = Mathf.Max(largestRotationError, rotationError);
                if (errors.Count < 24 && (positionError > 0.03f || rotationError > 0.1f
                    || Vector3.Distance(transform.lossyScale, expected.Scale) > 0.03f))
                {
                    errors.Add($"{route} b{beat} {part.Key}: world={transform.position}, native={expected.Position}, rotation error={rotationError}deg.");
                }
            }
        }

        // Neutral material isolates geometry pose from the separately audited native material appearance.
        private bool[] RenderMask(float beat, bool nativePose)
        {
            var created = new List<GameObject>();
            var cameraObject = new GameObject("Molecule pose control camera");
            var camera = cameraObject.AddComponent<Camera>();
            var material = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var image = new Texture2D(512, 512, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            try
            {
                foreach (var part in parts)
                {
                    var source = part.Value.transform;
                    var expected = NativePose(part.Key, beat);
                    var copy = new GameObject("Molecule controlled geometry");
                    created.Add(copy);
                    copy.layer = 30;
                    copy.transform.SetPositionAndRotation(
                        nativePose
                            ? expected.Position
                            : source.position,
                        nativePose
                            ? expected.Rotation
                            : source.rotation);
                    copy.transform.localScale = nativePose
                        ? expected.Scale
                        : source.lossyScale;
                    copy.AddComponent<MeshFilter>().sharedMesh = part.Value.GetComponent<MeshFilter>().sharedMesh;
                    copy.AddComponent<MeshRenderer>().sharedMaterial = material;
                }

                camera.enabled = false;
                camera.cullingMask = 1 << 30;
                camera.fieldOfView = 90f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 10000f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply();
                var directory = System.Environment.GetEnvironmentVariable("CHROMAPPER_SUPERLUMINAL_CAPTURE_DIRECTORY");
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllBytes(
                        PathUtils.Combine(
                            directory,
                            $"{fixtureName}-molecule-b{beat:0.000}-" + (nativePose
                            ? "native-pose.png"
                            : "actual-pose.png")),
                        image.EncodeToPNG());
                }

                return image.GetPixels().Select(pixel => pixel.maxColorComponent > 0.5f).ToArray();
            }

            finally
            {
                RenderTexture.active = previous;
                foreach (var item in created)
                    Object.DestroyImmediate(item);

                target.Release();
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
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
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreFreshMapperAfterFixture()
        {
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.2.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

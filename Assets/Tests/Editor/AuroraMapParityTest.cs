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
    // Fixture source: "Aurora", mapped by HypersonicSharkz & Bitz (BeatSaver ID: 1e56a).
    public class AuroraMapParityTest : BasicEventChunkingTestBase
    {
        private bool previousAnimations;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private string fixtureLabel;
        private JSONNode fixtureData;
        private AudioTimeSyncController playbackClock;
        private bool previousClockEnabled;
        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        protected override EditingMode InitialEditingMode => EditingMode.Gameplay;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield break;
        }

        private IEnumerator LoadFixture(string fixture)
        {
            fixtureLabel = Path.GetFileNameWithoutExtension(fixture);
            fixtureData = JSON.Parse(File.ReadAllText(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", fixture)));
            Settings.Instance.Animations = true;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 19,
                NoteStartBeatOffset = -0.3f,
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(2,
                fixtureData,
                beatsPerMinute: 184, environmentName: "BillieEnvironment", songLengthSeconds: 210,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [TestCase(2, 0.083f)]
        [TestCase(3, 0.083f)]
        [TestCase(2, 3f)]
        [TestCase(3, 3f)]
        [TestCase(2, -3f)]
        [TestCase(3, -3f)]
        [TestCase(2, 0f)]
        [TestCase(3, 0f)]
        public void CustomLengthUsesLaneUnitsForBothMapFormats(int version, float authoredLength)
        {
            var custom = new JSONObject
            {
                [version == 2 ? "_scale" : "size"] = new JSONArray { [0] = 1f, [1] = 2f, [2] = authoredLength }
            };
            var json = version == 2
                ? new JSONObject { ["_time"] = 234f, ["_lineIndex"] = 0, ["_type"] = 0,
                    ["_duration"] = -4f, ["_width"] = 0, ["_customData"] = custom }
                : new JSONObject { ["b"] = 234f, ["d"] = -4f, ["w"] = 0, ["h"] = 5, ["customData"] = custom };
            var previousVersion = Settings.Instance.MapVersion;
            var root = new GameObject("Custom obstacle length");
            try
            {
                Settings.Instance.MapVersion = version;
                var data = version == 2 ? Beatmap.V2.V2Obstacle.GetFromJson(json) : Beatmap.V3.V3Obstacle.GetFromJson(json);
                var wall = root.AddComponent<ObstacleContainer>();
                wall.ObstacleData = data;
                Assert.That(wall.GetLength(1f), Is.EqualTo(authoredLength * 0.6f).Within(0.00001f));
                Assert.That(wall.GetLength(100f), Is.EqualTo(authoredLength * 0.6f).Within(0.00001f));
            }
            finally
            {
                Settings.Instance.MapVersion = previousVersion;
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(75f)]
        [TestCase(-60f)]
        public void CapturedPairRestPoseDoesNotRepeatNativeStart(float startAngle)
        {
            var root = new GameObject("Captured pair pose");
            try
            {
                root.transform.rotation = Quaternion.Euler(20f, 40f, 10f);
                var visual = root.AddComponent<LightPairRotation>();
                visual.StartRotation = startAngle;
                visual.RotationVector = Vector3.up;
                var rest = Quaternion.Euler(30f, 50f, 60f);
                for (var i = 0; i < 2; i++)
                {
                    var child = new GameObject("Pair side").transform;
                    child.SetParent(root.transform, false);
                    child.localRotation = rest * Quaternion.Euler(0f, i == 0 ? startAngle : -startAngle, 0f);
                    visual.Transforms[i] = new LightPairRotation.TransformContainer { Transform = child };
                }

                visual.Initialize();
                foreach (var side in visual.Transforms)
                {
                    Assert.That(Quaternion.Angle(side.Start, rest), Is.LessThan(0.1f));
                }

                visual.Apply(startAngle, -startAngle);
                Assert.That(Quaternion.Angle(visual.Transforms[0].Transform.localRotation,
                    rest * Quaternion.Euler(0, startAngle, 0)), Is.LessThan(0.1f));
                Assert.That(Quaternion.Angle(visual.Transforms[1].Transform.localRotation,
                    rest * Quaternion.Euler(0, -startAngle, 0)), Is.LessThan(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator TinyCaveFrameKeepsItsGrayFaceAfterAnimatedScaling()
        {
            yield return LoadFixture("AuroraCaveFixture.json");
            EnterPreview();
            yield return Seek(223.062f);
            AssertFilledFace(FindCave());
        }

        [UnityTest]
        public IEnumerator AuthoredPillarRotationsSurviveOpeningAndReverseSeek()
        {
            yield return CheckPillarRotations("AuroraFullMapFixture.json");
        }

        [UnityTest]
        public IEnumerator MinimalPillarRotationsSurviveOpeningAndReverseSeek()
        {
            yield return CheckPillarRotations("AuroraLaserFixture.json");
        }

        [UnityTest]
        public IEnumerator ReducedLockedPairMotionMatchesNativeRotations()
        {
            yield return CheckPillarRotations("AuroraLaserMotionFixture.json");
        }

        private IEnumerator CheckPillarRotations(string fixture)
        {
            yield return LoadFixture(fixture);
            EnterPreview();
            var failures = new List<string>();
            foreach (var beat in new[] { 6.375f, 11.75f, 16.5f, 24f, 60.5f, 6.375f })
            {
                yield return Seek(beat);
                Capture($"pillars-{beat}");
                var pillars = Object.FindObjectsByType<ChromaIDMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(marker => marker.ChromaID.EndsWith("PillarL") || marker.ChromaID.EndsWith("PillarR")).ToArray();
                Assert.That(pillars.Length, Is.GreaterThan(0));
                foreach (var pillar in pillars)
                {
                    var expected = Quaternion.Euler(90, pillar.name == "PillarL" ? -60 : 60, 0);
                    var error = Quaternion.Angle(pillar.transform.rotation, expected);
                    Debug.Log($"[AuroraParity] beat={beat} {pillar.ChromaID} rotation={pillar.transform.eulerAngles} error={error}");
                    if (beat == 6.375f && error > 0.1f)
                    {
                        failures.Add($"{pillar.ChromaID} lost the authored V angle: {error} degrees");
                    }
                }

                foreach (var visual in Object.FindObjectsByType<LightPairRotation>(FindObjectsSortMode.None)
                    .Where(pair => pair.name.StartsWith("BottomPairLasers")))
                {
                    var actualDirections = new Vector2[2];
                    var expectedDirections = new Vector2[2];
                    for (var sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        var side = visual.Transforms[sideIndex];
                        var marker = side.Transform.GetComponent<ChromaIDMarker>();
                        // 1.44.1 Billie scene: Pillar local quaternion and RotationBase local X=20.
                        // Native Start captures their product before Chroma's end-of-frame enhancements.
                        var nativePillar = new Quaternion(-0.120590314f,
                            sideIndex == 0 ? 0.3794095f : -0.3794095f,
                            sideIndex == 0 ? -0.049950138f : 0.049950138f, 0.91597563f);
                        var expectedStart = nativePillar * Quaternion.Euler(20f, 0f, 0f);
                        var error = Quaternion.Angle(side.Start, expectedStart);
                        if (error > 0.1f)
                        {
                            failures.Add($"beat {beat}: {marker.ChromaID} differs from native Start by {error} degrees");
                        }

                        var angle = GetNativeLockedAngle(sideIndex == 0 ? 12 : 13, beat);
                        var expectedLocal = expectedStart * Quaternion.Euler(0, 0, angle);
                        if (Quaternion.Angle(side.Transform.localRotation, expectedLocal) > 0.1f)
                        {
                            failures.Add($"beat {beat}: {marker.ChromaID} differs from native locked rotation.");
                        }

                        var laser = side.Transform.GetChild(0);
                        var camera = cameraManager.CameraControllers[1].Camera;
                        actualDirections[sideIndex] = ProjectDirection(camera, laser.position, laser.up);
                        var expectedWorld = side.Transform.parent.rotation * expectedLocal * laser.localRotation;
                        expectedDirections[sideIndex] = ProjectDirection(camera, laser.position, expectedWorld * Vector3.up);
                    }

                    var actualAngle = Vector2.Angle(actualDirections[0], actualDirections[1]);
                    var expectedAngle = Vector2.Angle(expectedDirections[0], expectedDirections[1]);
                    Debug.Log($"[AuroraParity] beat={beat} {visual.name} projected V actual={actualAngle} native={expectedAngle}");
                    if (Mathf.Abs(actualAngle - expectedAngle) > 0.2f)
                    {
                        failures.Add($"beat {beat}: projected V is {actualAngle}, native pose projects to {expectedAngle}");
                    }
                }
            }

            Debug.Log(string.Join("\n", failures));
            Assert.That(failures.Count, Is.Zero, failures.FirstOrDefault());
        }

        private float GetNativeLockedAngle(int eventType, float beat)
        {
            var seconds = beat * 60f / 184f;
            var lastSeconds = 0f;
            var angle = 0f;
            var speed = 0f;
            foreach (var evt in fixtureData["_events"].Children)
            {
                if (evt["_type"].AsInt != eventType)
                    continue;

                var callback = Mathf.Ceil(evt["_time"].AsFloat * 60f / 184f * 90f - 0.0001f) / 90f;
                if (callback > seconds)
                    break;

                Assert.That(evt["_customData"]["_lockPosition"].AsBool, Is.True);
                angle += (callback - lastSeconds) * speed;
                lastSeconds = callback;
                var value = evt["_value"].AsInt;
                var dir = evt["_customData"]["_direction"].AsInt;
                var direction = (eventType == 12) == (dir == 0) ? -1f : 1f;
                speed = value > 0
                    ? direction * 20f * (evt["_customData"]["_preciseSpeed"].IsNumber
                        ? evt["_customData"]["_preciseSpeed"].AsFloat : value)
                    : 0f;
            }

            return angle + (seconds - lastSeconds) * speed;
        }

        private static Vector2 ProjectDirection(Camera camera, Vector3 origin, Vector3 direction)
        {
            var start = camera.WorldToScreenPoint(origin);
            var end = camera.WorldToScreenPoint(origin + direction);
            return (Vector2)(end - start);
        }

        [UnityTest]
        public IEnumerator BillieCloudsKeepNativeCompositingOrder()
        {
            yield return CheckCloudCompositing("AuroraFullMapFixture.json");
        }

        [UnityTest]
        public IEnumerator MinimalCloudKeepsNativeCompositingOrder()
        {
            yield return CheckCloudCompositing("AuroraCloudFixture.json");
        }

        private IEnumerator CheckCloudCompositing(string fixture)
        {
            yield return LoadFixture(fixture);
            EnterPreview();
            yield return Seek(113.8f);
            Capture("clouds-113.8");
            var cloud = Object.FindObjectsByType<ChromaIDMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(marker => marker.ChromaID.EndsWith("Clouds"));
            var renderer = cloud.GetComponent<Renderer>();
            Debug.Log($"[AuroraParity] clouds position={cloud.transform.position} material={renderer.sharedMaterial.name} queue={renderer.sharedMaterial.renderQueue}");
            AssertCloudDoesNotMaskLaterGlow(renderer.sharedMaterial);
            Assert.That(renderer.sharedMaterial.renderQueue, Is.EqualTo(2500),
                "Billie's shipped clouds composite before the transparent environment lights and displacement grab.");
        }

        [UnityTest]
        public IEnumerator ZeroBloomAlphaCaveWallsKeepNativeDimensionsAndGrayFaces()
        {
            yield return CheckCaveFrame("AuroraFullMapFixture.json");
        }

        [UnityTest]
        public IEnumerator MinimalCaveWallKeepsNativeDimensionsAndGrayFaces()
        {
            yield return CheckCaveFrame("AuroraCaveFixture.json");
        }

        private IEnumerator CheckCaveFrame(string fixture)
        {
            yield return LoadFixture(fixture);
            EnterPreview();
            foreach (var beat in new[] { 214f, 215f, 216f, 223.062f })
            {
                yield return Seek(beat);
                Capture($"cave-{beat}");
                AssertNativeSize(FindCave());
            }

            var cave = FindCave();
            var color = cave.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
            Debug.Log($"[AuroraParity] cave color={color} core={cave.CoreRenderer.sharedMaterial.name} bounds={cave.CoreRenderer.bounds}");
            Assert.That(color.a, Is.Zero);
            AssertFilledFace(cave);
        }

        private static ObstacleContainer FindCave() =>
            Object.FindObjectsByType<ObstacleContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(wall => wall.ObstacleData != null
                    && Mathf.Abs(wall.ObstacleData.JsonTime - 234.402f) < 0.001f
                    && Mathf.Abs(wall.ObstacleData.CustomCoordinate[0].AsFloat - 18.44f) < 0.001f
                    && wall.ObstacleData.CustomColor.HasValue && wall.ObstacleData.CustomColor.Value.a == 0f);

        [UnityTest]
        public IEnumerator FullMapCliffsKeepNativeDimensionsAndFlatFaces()
        {
            yield return CheckCliff("AuroraFullMapFixture.json");
        }

        [UnityTest]
        public IEnumerator MinimalCliffKeepsNativeDimensionsAndFlatFaces()
        {
            yield return CheckCliff("AuroraCliffFixture.json");
        }

        private IEnumerator CheckCliff(string fixture)
        {
            yield return LoadFixture(fixture);
            EnterPreview();
            yield return Seek(478f);
            playbackClock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = playbackClock.enabled;
            StartDeterministicPlaybackAtSongBpmTime(playbackClock, 478f);
            playbackClock.enabled = false;
            var cliff = Object.FindObjectsByType<ObstacleContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(wall => wall.ObstacleData != null && wall.ObstacleData.JsonTime == 363f
                    && wall.ObstacleData.CustomTrack.Value == "bigCliff"
                    && Mathf.Abs(wall.ObstacleData.CustomAnimation["_scale"][0][0].AsFloat - 168.679f) < 0.001f);
            var probeObject = new GameObject("Aurora render stage probe");
            try
            {
                var probe = probeObject.AddComponent<AuroraRenderStageProbe>();
                foreach (var beat in new[] { 478f, 580f, 600f })
                {
                    var captured = false;
                    var renderedZ = float.NaN;
                    var renderedTravelZ = float.NaN;
                    probe.AfterAnimations = () =>
                    {
                        if (captured)
                            return;

                        renderedZ = cliff.transform.localPosition.z;
                        renderedTravelZ = cliff.Animator.AnimationTrack.ObjectParentTransform.localPosition.z;
                        Capture($"cliff-{beat}");
                        captured = true;
                    };
                    currentSecondsProperty.SetValue(playbackClock, playbackClock.GetSecondsFromBeat(beat));
                    yield return null;
                    yield return null;
                    Assert.That(captured, Is.True);
                    Debug.Log($"[AuroraParity] cliff beat={beat} definite Z={renderedZ} travel Z={renderedTravelZ}");
                    Assert.That(renderedTravelZ, Is.EqualTo(0f).Within(0.0001f));
                    Assert.That(renderedZ, Is.EqualTo(221.632f * 0.6f).Within(0.01f),
                        "Definite position must replace normal obstacle travel before the camera renders.");
                }
            }
            finally
            {
                Object.DestroyImmediate(probeObject);
            }

            AssertFilledFace(cliff);
            AssertNativeSize(cliff);
        }

        private static void AssertNativeSize(ObstacleContainer wall)
        {
            // Noodle's GetCustomLength uses kNoteLinesDistance=.6, independently of duration/NJS.
            var length = wall.ObstacleData.CustomSize[2].AsFloat * 0.6f;
            var failures = new List<string>();
            if (Mathf.Abs(wall.GetLength(1f) - length) > 0.00001f
                || Mathf.Abs(wall.GetLength(100f) - length) > 0.00001f)
            {
                failures.Add($"Noodle length must be {length} metres, was {wall.GetLength(1f)}");
            }

            var size = wall.ObstacleData.CustomSize.ReadVector3() * 0.6f;
            size.x *= 0.98f;
            var offset = new Vector3(0, size.y * 0.5f, size.z * 0.5f);
            var coreSize = size - Vector3.one * 0.01f;
            var frameSize = new Vector3(size.x, Mathf.Max(size.y, 0.05f), size.z);
            if (Vector3.Distance(wall.CoreTransform.localScale, coreSize) > 0.00001f)
            {
                failures.Add($"Core size must be {coreSize}, was {wall.CoreTransform.localScale}");
            }

            if (Vector3.Distance(wall.CoreTransform.localPosition, offset) > 0.00001f
                || Vector3.Distance(wall.OutlineTransform.localPosition, offset) > 0.00001f)
            {
                failures.Add($"Frame/core pivot must be {offset}, was {wall.CoreTransform.localPosition}");
            }

            if (Vector3.Distance(wall.OutlineTransform.localScale, frameSize) > 0.00001f)
            {
                failures.Add($"Frame envelope must be {frameSize}, was {wall.OutlineTransform.localScale}");
            }

            var actualFront = float.PositiveInfinity;
            var expectedFront = float.PositiveInfinity;
            var actualFrameFront = float.PositiveInfinity;
            var expectedFrameFront = float.PositiveInfinity;
            for (var cornerIndex = 0; cornerIndex < 8; cornerIndex++)
            {
                var corner = new Vector3((cornerIndex & 1) == 0 ? -0.5f : 0.5f,
                    (cornerIndex & 2) == 0 ? -0.5f : 0.5f, (cornerIndex & 4) == 0 ? -0.5f : 0.5f);
                actualFront = Mathf.Min(actualFront, wall.CoreTransform.TransformPoint(corner).z);
                expectedFront = Mathf.Min(expectedFront,
                    wall.Animator.LocalTarget.TransformPoint(offset + Vector3.Scale(corner, coreSize)).z);
                actualFrameFront = Mathf.Min(actualFrameFront, wall.OutlineTransform.TransformPoint(corner).z);
                expectedFrameFront = Mathf.Min(expectedFrameFront,
                    wall.Animator.LocalTarget.TransformPoint(offset + Vector3.Scale(corner, frameSize)).z);
            }

            if (Mathf.Abs(actualFront - expectedFront) > 0.0001f
                || Mathf.Abs(actualFrameFront - expectedFrameFront) > 0.0001f)
            {
                failures.Add($"Core/frame leading Z must be {expectedFront}/{expectedFrameFront}, was {actualFront}/{actualFrameFront}");
            }

            Debug.Log($"[AuroraParity] wall={wall.ObstacleData.JsonTime} leading Z core={actualFront} frame={actualFrameFront} native={expectedFront}/{expectedFrameFront}");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void AssertCloudDoesNotMaskLaterGlow(Material source)
        {
            var cloud = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("Aurora cloud camera");
            var camera = cameraObject.AddComponent<Camera>();
            var cloudMaterial = new Material(source);
            var glowMaterial = new Material(Shader.Find("Unlit/Transparent"));
            var green = new Texture2D(1, 1);
            var target = new RenderTexture(32, 32, 24);
            var texture = new Texture2D(32, 32, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            try
            {
                cloudMaterial.shaderKeywords = System.Array.Empty<string>();
                cloudMaterial.SetColor("_Color", Color.black);
                green.SetPixel(0, 0, Color.green);
                green.Apply();
                glowMaterial.mainTexture = green;
                cloud.layer = 30;
                glow.layer = 30;
                cloud.transform.position = new Vector3(0, 0, 5);
                glow.transform.position = new Vector3(0, 0, 10);
                cloud.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
                glow.GetComponent<Renderer>().sharedMaterial = glowMaterial;
                camera.cullingMask = 1 << 30;
                camera.orthographic = true;
                camera.orthographicSize = 0.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
                texture.Apply();
                Assert.That(texture.GetPixel(16, 16).g, Is.GreaterThan(0.8f),
                    "A dark cloud must composite before the later transparent glow, rather than cut a hard silhouette out of it.");
            }
            finally
            {
                RenderTexture.active = active;
                target.Release();
                Object.DestroyImmediate(cloud);
                Object.DestroyImmediate(glow);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(cloudMaterial);
                Object.DestroyImmediate(glowMaterial);
                Object.DestroyImmediate(green);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        private static void AssertFilledFace(ObstacleContainer cave)
        {
            var source = cave.OutlineTransform.GetComponentInChildren<MeshRenderer>();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cameraObject = new GameObject("Aurora frame camera");
            var camera = cameraObject.AddComponent<Camera>();
            var material = new Material(source.sharedMaterial);
            var flatMaterial = new Material(Shader.Find("Unlit/Color"));
            var target = new RenderTexture(256, 256, 24);
            var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            var previewKeyword = Shader.IsKeywordEnabled("CM_PREVIEW_MODE");
            try
            {
                Shader.DisableKeyword("CM_PREVIEW_MODE");
                material.DisableKeyword("_WHITEBOOSTTYPE_MAINEFFECT");
                material.DisableKeyword("_WHITEBOOSTTYPE_ALWAYS");
                // Isolate one face's RGB from the additive accumulation of front and back faces.
                material.SetFloat("_BlendDstFactor", 0f);
                wall.layer = 30;
                wall.transform.position = new Vector3(0, 0, 30);
                var scale = source.transform.lossyScale;
                wall.transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                var renderer = wall.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.SetPropertyBlock(cave.MpbController.Mpb);
                camera.cullingMask = 1 << 30;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) * 0.6f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                texture.Apply();
                var lit = 0;
                var samples = 0;
                var width = Mathf.Abs(scale.x) / (camera.orthographicSize * 2) * 256;
                var height = Mathf.Abs(scale.y) / (camera.orthographicSize * 2) * 256;
                for (var y = Mathf.CeilToInt(128 - height * 0.4f); y < 128 + height * 0.4f; y++)
                {
                    for (var x = Mathf.CeilToInt(128 - width * 0.4f); x < 128 + width * 0.4f; x++)
                    {
                        if (texture.GetPixel(x, y).r > 0.1f)
                        {
                            lit++;
                        }

                        samples++;
                    }
                }

                Debug.Log($"[AuroraParity] cave frame scale={scale} filled interior={lit}/{samples}");
                Assert.That((float)lit / samples, Is.GreaterThan(0.95f),
                    "The native .05 edge fills the face of the tiny authored box before its parent is scaled.");
                var nativeColor = cave.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
                var facePixel = texture.GetPixel(128, 128);
                renderer.sharedMaterial = flatMaterial;
                flatMaterial.SetColor("_Color", nativeColor);
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                texture.Apply();
                var referencePixel = texture.GetPixel(128, 128);
                Debug.Log($"[AuroraParity] face={facePixel} plain native-color reference={referencePixel}");
                Assert.That(facePixel.r, Is.EqualTo(referencePixel.r).Within(0.03f),
                    "Zero bloom alpha retains the unpremultiplied gray/white face color.");
            }
            finally
            {
                if (previewKeyword)
                {
                    Shader.EnableKeyword("CM_PREVIEW_MODE");
                }

                RenderTexture.active = active;
                target.Release();
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(flatMaterial);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        private void EnterPreview()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
        }

        private static IEnumerator Seek(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        private void Capture(string label)
        {
            var camera = cameraManager.CameraControllers[1].Camera;
            var target = new RenderTexture(1024, 576, 24, RenderTextureFormat.DefaultHDR);
            var texture = new Texture2D(1024, 576, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 1024, 576), 0, 0);
                texture.Apply();
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", GetType().Name);
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(PathUtils.Combine(directory, fixtureLabel + "-" + label + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        [UnityTearDown]
        public IEnumerator RestorePreviewState()
        {
            if (playbackClock != null)
            {
                if (playbackClock.IsPlaying)
                {
                    PauseDeterministicPlayback(playbackClock);
                }

                playbackClock.enabled = previousClockEnabled;
                playbackClock = null;
            }

            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = previousAnimations;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
        }
    }

    // Test coroutines resume before LateUpdate; camera evidence must follow the production animation pass.
    [DefaultExecutionOrder(32000)]
    public class AuroraRenderStageProbe : MonoBehaviour
    {
        public System.Action AfterAnimations;

        private void LateUpdate()
        {
            AfterAnimations?.Invoke();
        }
    }
}

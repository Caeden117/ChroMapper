using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class SuperluminalEditModeMaterialTest : TestBase
    {
        private bool previousAnimations;
        private float previousOpacity;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode uiMode;
        private CameraManager cameraManager;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousOpacity = Settings.Instance.ObstacleOpacity;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Local reproduction requires the ignored complete Superluminal difficulty.")]
        public IEnumerator FullMapEditorWallOpacityControlsRenderedSurfaces()
        {
            yield return CheckWalls(
                PathUtils.Combine(
                    Application.dataPath,
                    "..",
                    "TestResults",
                    "Superluminal",
                    "SuperluminalFullMapFixture.json"));
        }

        [UnityTest]
        public IEnumerator ReducedEditorWallOpacityControlsRenderedSurfaces()
        {
            yield return CheckWalls(
                PathUtils.Combine(
                    Application.dataPath,
                    "Tests",
                    "Fixtures",
                    "SuperluminalEditModeWallFixture.json"));
        }

        private IEnumerator CheckWalls(string path)
        {
            Assert.That(File.Exists(path), Is.True);
            Settings.Instance.Animations = true;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 16,
                NoteStartBeatOffset = 0,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                CustomColorLeft = new Color(0.6039216f, 0.254902f, 0.9960784f),
                CustomColorRight = new Color(0.1294118f, 0.2156863f, 0.827451f),
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(path)),
                beatsPerMinute: 150,
                environmentName: "GagaEnvironment",
                songLengthSeconds: 220,
                forceSceneReload: true,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var failures = new List<string>();
            foreach (var mode in new[] { UIModeType.Normal, UIModeType.Preview, UIModeType.Normal, UIModeType.Preview, UIModeType.Normal })
            {
                uiMode.SetUIMode(mode, false);
                cameraManager.SelectCamera(CameraType.Editing);
                clock.MoveToJsonTime(527.4f);
                yield return null;
                yield return null;
                var wall = Object.FindObjectsByType<ObstacleContainer>(FindObjectsSortMode.None)
                    .Single(candidate => Mathf.Abs(candidate.ObstacleData.JsonTime - 526.2195833f) < 0.0001f);
                Assert.That(
                    wall.CoreRenderer.sharedMaterial.shader.name,
                    Is.EqualTo(mode == UIModeType.Preview
                        ? "ChroMapper/Object/Obstacle Distortion"
                        : "ChroMapper/Object/Obstacle Simple"));
                Assert.That(wall.CoreRenderer.sharedMaterial.shader.isSupported, Is.True);
                if (mode == UIModeType.Normal)
                {
                    var color = wall.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
                    if (Vector4.Distance(color, new Color(1f, 1f, 1f, 10f)) > 0.001f)
                        failures.Add("Leaving Preview discarded the bar's authored white color: " + color);

                    CheckOpacity(wall.CoreRenderer, failures, false);
                    var outline = wall.MpbController.Renderers.Single(
                        renderer => renderer.sharedMaterial.shader.name == "ChroMapper/Object/Obstacle Outline");
                    CheckOpacity(outline, failures, true);
                }

            }

            if (path.Contains("SuperluminalFullMapFixture.json"))
            {
                SetOpacity(previousOpacity);
                cameraManager.SelectedCameraController.transform.SetPositionAndRotation(
                    new Vector3(0f, 12f, -20f),
                    Quaternion.Euler(20f, 0f, 0f));
                foreach (var beat in new[] { 527.4f, 263f })
                {
                    clock.MoveToJsonTime(beat);
                    yield return null;
                    yield return null;
                    CaptureEditorFrame(cameraManager.SelectedCameraController.Camera, beat);
                }

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void CaptureEditorFrame(Camera camera, float beat)
        {
            var target = new RenderTexture(512, 256, 24, RenderTextureFormat.ARGBFloat);
            var texture = new Texture2D(512, 256, TextureFormat.RGBAFloat, false, true);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 512, 256), 0, 0);
                texture.Apply();
                var directory = PathUtils.Combine(
                    Application.dataPath,
                    "..",
                    "TestResults",
                    "Superluminal",
                    "EditMode");
                Directory.CreateDirectory(directory);
                var filename = "after-" + beat.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".png";
                File.WriteAllBytes(PathUtils.Combine(directory, filename), texture.EncodeToPNG());
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

        private static void CheckOpacity(Renderer renderer, List<string> failures, bool outline)
        {
            var surfaceName = outline
                ? "outline"
                : "core";
            var previousLayer = renderer.gameObject.layer;
            var previousPosition = renderer.transform.position;
            var previousRotation = renderer.transform.rotation;
            var previousScale = renderer.transform.localScale;
            var filter = renderer.GetComponent<MeshFilter>();
            var previousMesh = filter.sharedMesh;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            filter.sharedMesh = quad.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(quad);
            var previousBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(previousBlock);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetVector("_WorldScale", new Vector4(4f, 0.02f, 1f, 0f));
            renderer.SetPropertyBlock(block);
            GameObject overlapping = null;
            var cameraObject = new GameObject("Editor wall opacity control camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << 31;
            camera.allowHDR = true;
            try
            {
                renderer.gameObject.layer = 31;
                renderer.transform.SetPositionAndRotation(new Vector3(0f, 0f, 10f), Quaternion.identity);
                renderer.transform.localScale = new Vector3(4f, 4f, 0.1f);
                SetOpacity(1f);
                var opaque = ChromaBaseWaterRenderingParityTest.CaptureCenter(camera);
                var baseline = new Vector3(opaque.r, opaque.g, opaque.b);
                Assert.That(
                    baseline.sqrMagnitude,
                    Is.GreaterThan(0.01f),
                    "The wall must cover the sample before checking opacity.");
                foreach (var opacity in new[] { 0f, 0.25f, 0.5f, 1f })
                {
                    SetOpacity(opacity);
                    Assert.That(Shader.GetGlobalFloat("_MainAlpha"), Is.EqualTo(opacity));
                    var pixel = ChromaBaseWaterRenderingParityTest.CaptureCenter(camera);
                    var expected = baseline * opacity;
                    var actual = new Vector3(pixel.r, pixel.g, pixel.b);
                    if (Vector3.Distance(actual, expected) > 0.003f)
                        failures.Add(
                            "Editor wall " + surfaceName + " opacity=" + opacity
                            + " actual=" + actual + " expected=" + expected);

                    if (pixel.a > 0.003f)
                        failures.Add(
                            "Editor wall " + surfaceName + " writes bloom alpha=" + pixel.a
                            + " at opacity=" + opacity + ". Editor transparency must leave the bloom channel unchanged.");

                }

                if (outline)
                {
                    overlapping = new GameObject("Overlapping editor wall frame control");
                    overlapping.layer = 31;
                    overlapping.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    overlapping.transform.localScale = renderer.transform.lossyScale;
                    overlapping.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    var second = overlapping.AddComponent<MeshRenderer>();
                    second.sharedMaterial = renderer.sharedMaterial;
                    second.SetPropertyBlock(block);
                    var stacked = ChromaBaseWaterRenderingParityTest.CaptureCenter(camera);
                    var actual = new Vector3(stacked.r, stacked.g, stacked.b);
                    if (Vector3.Distance(actual, baseline) > 0.003f)
                        failures.Add("Opaque editor outlines accumulate RGB when overlapping: actual=" + actual + " expected=" + baseline);

                }

                foreach (var message in ShaderUtil.GetShaderMessages(renderer.sharedMaterial.shader))
                {
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                        failures.Add(message.message);

                }

            }
            finally
            {
                renderer.gameObject.layer = previousLayer;
                renderer.transform.SetPositionAndRotation(previousPosition, previousRotation);
                renderer.transform.localScale = previousScale;
                renderer.SetPropertyBlock(previousBlock);
                filter.sharedMesh = previousMesh;
                if (overlapping != null)
                    Object.DestroyImmediate(overlapping);

                Object.DestroyImmediate(cameraObject);
            }

        }

        private static void SetOpacity(float opacity)
        {
            Settings.Instance.ObstacleOpacity = opacity;
            Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.ObstacleOpacity), opacity);
        }

        [UnityTearDown]
        public IEnumerator RestoreEditing()
        {
            if (clock != null)
                clock.enabled = previousClockEnabled;

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            SetOpacity(previousOpacity);
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.3.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

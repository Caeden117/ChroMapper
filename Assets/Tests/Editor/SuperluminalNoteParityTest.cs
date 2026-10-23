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
    public class SuperluminalNoteParityTest : TestBase
    {
        private bool previousAnimations;
        private UIMode uiMode;
        private CameraManager cameras;
        private AudioTimeSyncController clock;
        private NoteGridContainer notes;
        private Random.State previousRandom;
        private bool previousClockEnabled;
        private bool playbackClockCaptured;
        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
        private static readonly int cutoutOffsetId = Shader.PropertyToID("_CutoutTexOffset");

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousRandom = Random.state;
            playbackClockCaptured = false;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        private IEnumerator LoadMapFixture()
        {
            Settings.Instance.Animations = true;
            var fullMapPath = System.Environment.GetEnvironmentVariable("CHROMAPPER_SUPERLUMINAL_FULL_MAP_PATH");
            var path = string.IsNullOrEmpty(fullMapPath)
                ? PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "SuperluminalNoteParityFixture.json")
                : fullMapPath;
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
                songLengthSeconds: 70,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            notes = Object.FindAnyObjectByType<NoteGridContainer>();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
        }

        [UnityTest]
        public IEnumerator OpeningDissolveUsesAuthoredLifetimeAndTransparentArrow()
        {
            yield return LoadMapFixture();
            yield return Seek(34.937f);
            var fake = FindNote(35, true);
            var real = FindNote(37, false);
            Assert.That(fake, Is.Not.Null, "A: fake red b35 arrow must be loaded at b34.937.");
            Assert.That(real, Is.Not.Null, "C: real blue b37 note must be loaded at b34.937.");
            var arrowDissolve = 0.2f * ((34.937f - 33f) / 2f);
            var realProgress = (34.937f - 23f) / 28f;
            var realDissolve = Mathf.Pow(2f, 10f * ((realProgress / 0.45f) - 1f));
            Assert.That(
                fake.ArrowMpbController.Mpb.GetFloat(cutoutId),
                Is.EqualTo(1f - arrowDissolve).Within(0.0001f),
                "A: native Noodle normalTime must use the full jump lifetime.");
            Assert.That(
                real.MpbController.Mpb.GetFloat(cutoutId),
                Is.EqualTo(1f - realDissolve).Within(0.001f),
                "C: native Noodle easeInExpo uses the full jump lifetime.");
            var failures = new List<string>();
            // Native DisappearingArrowControllerBase keeps a colored decoration independent of the solid core's cutout.
            if (CountArrowPixels(fake, true, true, captureName: "A-b34.937-transparent-remainder") < 16)
                failures.Add("A: hiding the solid arrow core must preserve the faint purple decoration at authored dissolveArrow.");

            CountArrowPixels(fake, false, false, captureName: "A-b34.937-complete-arrow");
            CountArrowPixels(fake, false, false, 0f, "A-b34.937-opaque-core-alignment");
            var renderers = fake.ArrowModelController.Renderers;
            if (renderers.Count != 2)
                failures.Add("A: the default arrow must contain the native solid and transparent decoration renderers.");

            else
            {
                // Native NormalGameNote places sibling core/glow at y.108/.1036 and z-.2535/-.25320023.
                var nativeGlowOffset = new Vector3(0f, -0.0044f, 0.00029977f);
                var glowOffset = renderers[0].transform.InverseTransformPoint(renderers[1].transform.position);
                if (Vector3.Distance(glowOffset, nativeGlowOffset) > 0.0001f)
                    failures.Add($"A: native glow/core alignment differs. Actual relative offset={glowOffset:F6}, expected={nativeGlowOffset:F6}.");
            }

            CapturePlayingView("AC-playing-b34.937");
            var offset = (Vector3)real.MpbController.Mpb.GetVector(cutoutOffsetId);
            // Native CutoutEffect.Start selects an offset on the radius-ten sphere, preserving the volume's sample distribution.
            if (Mathf.Abs(offset.magnitude - 10f) > 0.001f)
                failures.Add($"C: native cutout noise offset must lie on the radius-ten sphere, actual length={offset.magnitude:F6}.");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator EditorMarkerPreservesArrowCutoutAndGlowShape()
        {
            yield return LoadMapFixture();
            yield return Seek(34.937f);
            var note = FindNote(35, true);
            System.Action<MaterialPropertyBlock> coreMarker = properties =>
            {
                properties.SetFloat("_OutlineWidth", 100000f);
                properties.SetColor("_OverNoteInterfaceColor", Color.white);
                properties.SetFloat("_AlwaysTranslucent", 1f);
                properties.SetFloat("_AnimationSpawned", 0f);
                properties.SetFloat("_TranslucentAlpha", 0f);
            };
            var visibleCore = CountArrowPixels(
                note,
                false,
                false,
                0f,
                configureProperties: coreMarker,
                previewMode: false,
                glowOnly: false);
            var hiddenCore = CountArrowPixels(
                note,
                false,
                false,
                1f,
                configureProperties: coreMarker,
                previewMode: false,
                glowOnly: false);
            Assert.That(
                visibleCore,
                Is.GreaterThan(16),
                "The editor marker must precede translucent dithering on the remaining solid core.");

            System.Action<MaterialPropertyBlock> glowProperties = properties =>
            {
                properties.SetFloat("_ArrowDissolve", 1f);
                properties.SetColor("_GlowColor", Color.white);
                properties.SetColor("_OverNoteInterfaceColor", Color.white);
                properties.SetFloat("_AlwaysTranslucent", 0f);
                properties.SetFloat("_AnimationSpawned", 1f);
                properties.SetFloat("_OutlineWidth", 0f);
            };
            var glowPixels = CountArrowPixels(
                note,
                false,
                false,
                configureProperties: glowProperties,
                previewMode: false,
                glowOnly: true);
            var markerPixels = CountArrowPixels(
                note,
                false,
                false,
                configureProperties: properties =>
                {
                    glowProperties(properties);
                    properties.SetFloat("_OutlineWidth", 100000f);
                },
                previewMode: false,
                glowOnly: true);
            var invisibleMarkerPixels = CountArrowPixels(
                note,
                false,
                false,
                configureProperties: properties =>
                {
                    glowProperties(properties);
                    properties.SetFloat("_OutlineWidth", 100000f);
                    properties.SetFloat("_ArrowDissolve", 0f);
                },
                previewMode: false,
                glowOnly: true);
            Assert.That(glowPixels, Is.GreaterThan(16), "The texture-masked glow is the editor render control.");
            var failures = new List<string>();
            if (hiddenCore != 0)
                failures.Add($"Editor marker revealed {hiddenCore} pixels of a fully cutout solid core.");
            if (markerPixels > glowPixels * 1.05f)
                failures.Add($"Editor marker filled the glow quad: marker={markerPixels}, texture control={glowPixels} pixels.");
            if (invisibleMarkerPixels != 0)
                failures.Add($"Editor marker revealed {invisibleMarkerPixels} pixels of a fully dissolved arrow glow.");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static int CountArrowPixels(
            NoteContainer note,
            bool hideCore,
            bool coloredOnly,
            float? coreCutout = null,
            string captureName = null,
            System.Action<MaterialPropertyBlock> configureProperties = null,
            bool? previewMode = null,
            bool? glowOnly = null)
        {
            var objects = new List<GameObject>();
            var cameraObject = new GameObject("Superluminal isolated arrow camera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
            var previousTarget = RenderTexture.active;
            var previousPreviewMode = Shader.IsKeywordEnabled("CM_PREVIEW_MODE");
            try
            {
                var bounds = new Bounds(note.DirectionTarget.position, Vector3.zero);
                foreach (var renderer in note.ArrowMpbController.Renderers)
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;
                    if (glowOnly.HasValue && renderer.sharedMaterial.IsKeywordEnabled("ARROW_GLOW") != glowOnly.Value)
                        continue;

                    bounds.Encapsulate(renderer.bounds);
                    var model = new GameObject("Superluminal arrow render sample");
                    objects.Add(model);
                    model.layer = 30;
                    model.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    model.transform.localScale = renderer.transform.lossyScale;
                    model.AddComponent<MeshFilter>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var copy = model.AddComponent<MeshRenderer>();
                    copy.sharedMaterials = renderer.sharedMaterials;
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    if (hideCore)
                        properties.SetFloat(cutoutId, 1f);
                    else if (coreCutout.HasValue)
                        properties.SetFloat(cutoutId, coreCutout.Value);
                    configureProperties?.Invoke(properties);
                    copy.SetPropertyBlock(properties);
                }

                camera.enabled = false;
                camera.cullingMask = 1 << 30;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.magnitude * 1.2f, 0.1f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.transform.SetPositionAndRotation(
                    bounds.center - note.DirectionTarget.forward,
                    note.DirectionTarget.rotation);
                camera.targetTexture = target;
                RenderTexture.active = target;
                if (previewMode.HasValue)
                {
                    if (previewMode.Value)
                        Shader.EnableKeyword("CM_PREVIEW_MODE");
                    else
                        Shader.DisableKeyword("CM_PREVIEW_MODE");
                }

                camera.Render();
                texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                texture.Apply();
                if (captureName != null)
                    SaveCapture(texture, captureName, true);
                var coloredPixels = 0;
                foreach (var pixel in texture.GetPixels())
                {
                    if (coloredOnly
                        ? pixel.b > 0.003f && pixel.r > pixel.g * 1.4f && pixel.b > pixel.r * 1.2f && pixel.b < 0.5f
                        : pixel.maxColorComponent > 0.003f)
                        coloredPixels++;
                }

                return coloredPixels;
            }

            finally
            {
                RenderTexture.active = previousTarget;
                if (previousPreviewMode)
                    Shader.EnableKeyword("CM_PREVIEW_MODE");
                else
                    Shader.DisableKeyword("CM_PREVIEW_MODE");
                target.Release();
                foreach (var model in objects)
                    Object.DestroyImmediate(model);

                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        [UnityTest]
        public IEnumerator FakeArrowRespawnsWhenRewindingAcrossItsHitBeat()
        {
            yield return LoadMapFixture();
            Random.InitState(34);
            yield return Seek(34.9f);
            AssertFakeArrowVisible("before forward seek");
            var initial = FindNote(35, true);
            Assert.That(
                CountArrowPixels(initial, false, false, 0f),
                Is.GreaterThan(16),
                "B: isolated camera must render the complete opaque arrow as a positive control.");
            var initialPixels = CountArrowPixels(initial, false, false, captureName: "B-before-forward-seek");
            var failures = new List<string>();
            for (var round = 0; round < 8; round++)
            {
                yield return Seek(35f);
                yield return Seek(34.9f);
                AssertFakeArrowVisible($"after rewind round{round}");
                var fake = FindNote(35, true);
                var pixels = CountArrowPixels(fake, false, false);
                if (initialPixels > 16 && pixels <= 16)
                    failures.Add($"B: fake arrow rendered {pixels} pixels after rewind round{round}.");
            }

            previousClockEnabled = clock.enabled;
            playbackClockCaptured = true;
            clock.enabled = false;
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 34.9f);
            foreach (var beat in new[] { 35f, 35.0625f, 35.1f })
            {
                currentSecondsProperty.SetValue(clock, clock.GetSecondsFromBeat(beat));
                yield return null;
                yield return null;
                foreach (var note in notes.LoadedContainers.Values.OfType<NoteContainer>())
                    note.Animator.LateUpdate();

                AssertFakeArrowVisible($"during playback at b{beat}", beat);
                var fake = FindNote(35, true);
                var pixels = CountArrowPixels(fake, false, false);
                if (pixels <= 16)
                    failures.Add($"B: fake arrow rendered {pixels} pixels during playback at b{beat}.");
            }

            TestUtils.PauseDeterministicPlayback(clock);
            yield return Seek(34.9f);
            AssertFakeArrowVisible("after playback crossing35.1 and paused rewind");
            var pausedPixels = CountArrowPixels(FindNote(35, true), false, false, captureName: "B-after-playback-and-paused-reverse");
            if (pausedPixels <= 16)
                failures.Add($"B: fake arrow rendered {pausedPixels} pixels after playback crossing35.1 and paused rewind.");

            Assert.That(initialPixels, Is.GreaterThan(16), "B: fake arrow must render pixels before the forward seek.");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private void AssertFakeArrowVisible(string phase, float beat = 34.9f)
        {
            var fake = FindNote(35, true);
            Assert.That(fake, Is.Not.Null, "B: fake b35 arrow must be loaded at b34.9, including after rewind.");
            Assert.That(
                fake.ArrowMpbController.gameObject.activeInHierarchy,
                Is.True,
                "B: the rewound fake arrow decoration must be active.");
            Assert.That(fake.ArrowModelController.Renderers, Is.Not.Empty);
            foreach (var renderer in fake.ArrowModelController.Renderers)
                Assert.That(
                    renderer.enabled && renderer.gameObject.activeInHierarchy,
                    Is.True,
                    $"B: fake arrow renderer must be enabled and active {phase}.");

            var expectedCutout = 1f - (0.2f * Mathf.Clamp01((beat - 33f) / 2f));
            Assert.That(
                fake.ArrowMpbController.Mpb.GetFloat(cutoutId),
                Is.EqualTo(expectedCutout).Within(0.0001f),
                "B: rewind must restore the fake arrow's authored dissolve at b34.9.");
        }

        [UnityTest]
        public IEnumerator AnimatedRealNoteRetainsNativeSpawnFacing()
        {
            yield return LoadMapFixture();
            var failures = new List<string>();
            var captureIndex = 0;
            foreach (var beat in new[] { 43f, 43.687f, 45f, 43.687f })
            {
                yield return Seek(beat);
                var note = FindNote(46, false);
                Assert.That(note, Is.Not.Null, $"E: real purple b46 note missing at b{beat}.");
                if (Mathf.Abs(beat - 43.687f) < 0.001f)
                    CapturePlayingView($"E-playing-b43.687-{captureIndex++}");
                var expected = NativeFacing(note, beat, cameras.SelectedCameraController.transform.position);
                var angle = Vector3.Angle(note.DirectionTarget.forward, expected * Vector3.forward);
                if (angle > 4f)
                    failures.Add($"E b{beat}: native spawn-facing forward differs by {angle:F3} degrees.");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static Quaternion NativeFacing(NoteContainer note, float beat, Vector3 head)
        {
            // NoteJump.ManualUpdate and Noodle's DoNoteLook rotate the camera ray by the animated base pose.
            var progress = Mathf.Clamp01((beat - (46f - 14f)) / 28f);
            var baseRotation = note.DirectionTarget.parent.rotation;
            var end = Quaternion.Euler(note.DirectionTargetEuler);
            // Native's twenty-degree random middle turn contributes under four degrees at these late spawn samples.
            var authored = end;
            var position = note.DirectionTarget.position;
            head.y = Mathf.Lerp(head.y, position.y, 0.8f);
            var ray = baseRotation * (position - head).normalized;
            var look = Quaternion.LookRotation(ray, baseRotation * end * Vector3.up);
            return Quaternion.Lerp(baseRotation * authored, look, progress * 2f);
        }

        private static void SaveCapture(Texture2D texture, string name, bool linear)
        {
            var directory = System.Environment.GetEnvironmentVariable("CHROMAPPER_SUPERLUMINAL_CAPTURE_DIRECTORY");
            if (string.IsNullOrEmpty(directory))
                return;

            Directory.CreateDirectory(directory);
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            try
            {
                var pixels = texture.GetPixels();
                if (linear)
                {
                    for (var i = 0; i < pixels.Length; i++)
                        pixels[i] = pixels[i].gamma;
                }

                image.SetPixels(pixels);
                image.Apply();
                File.WriteAllBytes(PathUtils.Combine(directory, name + ".png"), image.EncodeToPNG());
            }

            finally
            {
                Object.DestroyImmediate(image);
            }
        }

        private void CapturePlayingView(string name)
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("CHROMAPPER_SUPERLUMINAL_CAPTURE_DIRECTORY")))
                return;

            var camera = cameras.SelectedCameraController.Camera;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var texture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                SaveCapture(texture, name, false);
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

        private NoteContainer FindNote(float beat, bool fake)
        {
            return notes.LoadedContainers.Values.OfType<NoteContainer>().FirstOrDefault(
                note =>
                note.NoteData.CustomFake == fake && Mathf.Abs(note.NoteData.JsonTime - beat) < 0.0001f);
        }

        private IEnumerator Seek(float beat)
        {
            clock.MoveToJsonTime(beat);
            yield return null;
            yield return null;
            foreach (var note in notes.LoadedContainers.Values.OfType<NoteContainer>())
                note.Animator.LateUpdate();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (clock != null && playbackClockCaptured)
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
            Random.state = previousRandom;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}

using System.Collections;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaGeometryPreviewParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private bool clockCaptured;
        private AudioTimeSyncController clock;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private GameObject beam;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator AuthoredCollisionShortensPlayingLightOnlyForCollidableGeometry()
        {
            var environment = new JSONArray();
            foreach (var collision in new[] { true, false })
                environment.Add(
                    new JSONObject
                    {
                        ["track"] = collision
                            ? "collidable"
                            : "noncollidable",
                        ["position"] = new Vector3(
                            collision
                                ? 10000f
                                : 10010f,
                            0f,
                            5f),
                        ["scale"] = new Vector3(2f, 2f, 1f),
                        ["geometry"] = new JSONObject
                        {
                            ["type"] = "Cube", ["collision"] = collision,
                            ["material"] = new JSONObject { ["shader"] = "Standard", ["color"] = Color.white }
                        }
                    });

            yield return LoadPreview(environment);
            var target = Find("collidable").MpbController.Renderers.Single();
            beam = new GameObject("Authored collision beam");
            beam.SetActive(false);
            beam.transform.position = new Vector3(10000f, 0f, 0f);
            beam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var controller = beam.AddComponent<ParametricBloomFogLightController>();
            var volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(volume.GetComponent<Collider>());
            volume.transform.SetParent(beam.transform, false);
            var box = volume.AddComponent<ParametricBoxLight>();
            box.Renderer = volume.GetComponent<Renderer>();
            controller.BoxLight = box;
            controller.Color = Color.white;
            controller.Length = 10f;
            controller.Center = 0f;
            var collisionProbe = beam.AddComponent<LightCollision>();
            collisionProbe.ParametricLight = controller;
            collisionProbe.EnvironmentLayerMask = 1 << target.gameObject.layer;
            beam.SetActive(true);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            controller.Refresh();
            Assert.That(
                controller.CollisionLength,
                Is.EqualTo(4.5f).Within(0.01f),
                "Native collision:true retains the cube collider, so Playing light rays stop at its front face.");
            Assert.That(
                box.CollisionHeight,
                Is.EqualTo(4.5f).Within(0.01f),
                "The collision distance must reach the visible parametric light.");

            beam.transform.position = new Vector3(10010f, 0f, 0f);
            Physics.SyncTransforms();
            yield return null;
            controller.Refresh();
            Assert.That(
                controller.CollisionLength,
                Is.EqualTo(float.MaxValue),
                "Native collision:false removes the collider and lets the light pass through.");

            beam.transform.position = new Vector3(10000f, 0f, 0f);
            Physics.SyncTransforms();
            yield return null;
            controller.Refresh();
            Assert.That(controller.CollisionLength, Is.EqualTo(4.5f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator GeneratedStandardDoesNotIntroduceNativeDisabledDitherInPlaying()
        {
            var environment = new JSONArray();
            environment.Add(
                new JSONObject
                {
                    ["track"] = "standard-surface",
                    ["position"] = new Vector3(0f, 1.65f, 10f),
                    ["scale"] = new Vector3(40f, 40f, 0.1f),
                    ["geometry"] = new JSONObject
                    {
                        ["type"] = "Cube",
                        ["material"] = new JSONObject { ["shader"] = "Standard", ["color"] = Color.white }
                    }
                });
            yield return LoadPreview(environment);
            foreach (var light in Object.FindObjectsByType<DirectionalLight>(FindObjectsSortMode.None))
                light.Color = Color.clear;

            foreach (var light in Object.FindObjectsByType<PointLight>(FindObjectsSortMode.None))
                light.Color = Color.clear;

            var front = Object.FindObjectsByType<DirectionalLight>(FindObjectsSortMode.None)
                .First(light => light.name.Contains("DirectionalLightFront"));
            front.transform.rotation = Quaternion.identity;
            front.Color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var renderer = Find("standard-surface").MpbController.Renderers.Single();
            var actualMaterial = renderer.sharedMaterial;
            var nativeDefault = new Material(actualMaterial);
            var noise = new Texture2D(2, 2, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            noise.SetPixels(new[] { Color.black, Color.white, Color.white, Color.black });
            noise.Apply();
            var previousNoise = Shader.GetGlobalTexture("_GlobalBlueNoiseTex");
            try
            {
                // Native Chroma's generated Standard keyword set omits the local NOISE_DITHERING variant.
                // Both renders receive identical lighting and uniform inputs, isolating that preset difference.
                nativeDefault.DisableKeyword("NOISE_DITHERING");
                Shader.SetGlobalTexture("_GlobalBlueNoiseTex", noise);
                var actual = Capture(cameraManager.SelectedCameraController.Camera);
                renderer.sharedMaterial = nativeDefault;
                var reference = Capture(cameraManager.SelectedCameraController.Camera);
                var differences = 0;
                for (var y = 24; y < 104; y++)
                {
                    for (var x = 24; x < 104; x++)
                    {
                        var index = y * 128 + x;
                        if (Vector4.Distance(actual[index], reference[index]) > 0.00001f)
                            differences++;

                    }

                }

                Assert.That(
                    differences,
                    Is.Zero,
                    "Generated Standard introduced per-surface noise absent from the native compiled preset.");
            }

            finally
            {
                renderer.sharedMaterial = actualMaterial;
                Shader.SetGlobalTexture("_GlobalBlueNoiseTex", previousNoise);
                Object.DestroyImmediate(nativeDefault);
                Object.DestroyImmediate(noise);
            }

        }

        private IEnumerator LoadPreview(JSONArray environment)
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject
                {
                    ["version"] = "3.3.0", ["customData"] = new JSONObject { ["environment"] = environment }
                },
                environmentName: "GagaEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            previousClockEnabled = clock.enabled;
            clockCaptured = true;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(0f);
            yield return null;
            yield return null;
        }

        private static GeometryContainer Find(string track) => Object.FindObjectsByType<GeometryContainer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None)
            .Single(container => container.EnvironmentEnhancement.Track == track);

        private static Color[] Capture(Camera camera)
        {
            var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBFloat);
            var texture = new Texture2D(128, 128, TextureFormat.RGBAFloat, false, true);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                texture.Apply();
                return texture.GetPixels();
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
        public IEnumerator RestorePreview()
        {
            if (clockCaptured && clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            if (beam != null)
                Object.DestroyImmediate(beam);

            clockCaptured = false;
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

    }
}

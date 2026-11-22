using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TestsEditMode
{
    public class BloomfogRendererTest
    {
        [Test]
        public void BloomFogTextureKeepsLightsOnTheirProjectedSide()
        {
            const int size = 128;
            var renderer = ScriptableObject.CreateInstance<BloomfogRendererSO>();
            var material = new Material(Shader.Find("ChroMapper/BloomfogMesh"));
            var root = new GameObject("BloomfogLights");
            var target = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var previousActive = RenderTexture.active;
            var previousTransform = Shader.GetGlobalMatrix("_VertexTransformMatrix");
            var previousRatio = Shader.GetGlobalVector("_CustomFogTextureToScreenRatio");
            var previousEyeOffsets = Shader.GetGlobalVector("_StereoCameraEyeOffsets");
            var light = CreateLight(root, material);
            try
            {
                renderer.FOV = new Vector2(90f, 90f);
                light.transform.SetPositionAndRotation(new Vector3(0f, 1f, -3f), Quaternion.Euler(0f, 0f, 90f));
                light.Center = 0.5f;
                light.SetColor(Color.white);

                renderer.RenderToTexture(Matrix4x4.identity, Matrix4x4.Perspective(90f, 1f, 0.1f, 100f), target, out _);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                var aboveCenter = pixels.GetPixel(size / 2, size * 2 / 3).r;
                var belowCenter = pixels.GetPixel(size / 2, size / 3).r;

                Assert.That(aboveCenter, Is.GreaterThan(belowCenter),
                    $"A light above the camera center must render above the texture center. "
                    + $"Red above: {aboveCenter}, red below: {belowCenter}.");
            }
            finally
            {
                RenderTexture.active = previousActive;
                Shader.SetGlobalMatrix("_VertexTransformMatrix", previousTransform);
                Shader.SetGlobalVector("_CustomFogTextureToScreenRatio", previousRatio);
                Shader.SetGlobalVector("_StereoCameraEyeOffsets", previousEyeOffsets);
                BloomFogObject.AllBloomFogLights.Remove(light);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(renderer);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(target);
            }
        }

        // RenderQuads rewrites submesh descriptors in place each frame, and Unity validates every
        // SetSubMesh against the descriptors left over from the previous pass. Reproduces the
        // deployed-log warning "SetSubMesh #0 shares part of its index buffer with SubMesh #1":
        // when the first material batch grows, its new range overlaps submesh 1's stale range
        // before submesh 1 is rewritten.
        [Test]
        public void GrowingFirstBatchDoesNotWarnAboutSharedIndexBuffer()
        {
            var renderer = ScriptableObject.CreateInstance<BloomfogRendererSO>();
            var materialA = new Material(Shader.Find("Hidden/InternalErrorShader"));
            var materialB = new Material(Shader.Find("Hidden/InternalErrorShader"));
            var root = new GameObject("BloomfogLights");
            var warnings = new List<string>();
            Application.LogCallback capture = (condition, _, type) =>
            {
                if (type == LogType.Warning && condition.Contains("SetSubMesh"))
                    warnings.Add(condition);
            };
            Application.logMessageReceived += capture;
            try
            {
                var lightA1 = CreateLight(root, materialA);
                CreateLight(root, materialB).SetColor(Color.white);
                var lightA2 = CreateLight(root, materialA);
                lightA1.SetColor(Color.white);
                // lightA2 keeps its default alpha-0 color so the first pass leaves it unrendered.

                renderer.Initialize();
                // First pass: batch A renders one quad [0,6), batch B renders one quad [6,6).
                RenderQuads(renderer);
                // Second pass: batch A grows to [0,12), overlapping submesh 1's stale [6,12).
                lightA2.SetColor(Color.white);
                RenderQuads(renderer);

                Assert.That(warnings, Is.Empty);
            }
            finally
            {
                Application.logMessageReceived -= capture;
                BloomFogObject.AllBloomFogLights.Clear();
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(renderer);
                Object.DestroyImmediate(materialA);
                Object.DestroyImmediate(materialB);
            }
        }

        private static BloomFogObject CreateLight(GameObject root, Material material)
        {
            var light = root.AddComponent<BloomFogObject>();
            light.CachedTransform = light.transform;
            light.DisableRenderersOnZeroAlpha = true;
            light.LightType = new BloomFogLightType(material, 0);
            // Register explicitly so the test does not depend on edit-mode enable callbacks.
            if (!BloomFogObject.AllBloomFogLights.Contains(light))
                BloomFogObject.AllBloomFogLights.Add(light);
            return light;
        }

        // RenderQuads is the CPU-side batching path; it needs no camera or render target.
        private static void RenderQuads(BloomfogRendererSO renderer) =>
            typeof(BloomfogRendererSO)
                .GetMethod("RenderQuads", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(renderer, new object[] { Matrix4x4.identity, Matrix4x4.identity, 0.02f });
    }
}

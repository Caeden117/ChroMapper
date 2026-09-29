using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TestsEditMode
{
    public class BloomfogRendererTest
    {
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

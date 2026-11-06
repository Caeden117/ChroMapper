using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.TestTools;
using Unity.Profiling;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSStrobeSdfIconTest
    {
        [Test]
        public void StrobeShaderSamplesAtlasAndSupportsInstancing()
        {
            var shader = Shader.Find("ChroMapper/GLS Strobe Icon");
            Assert.That(shader, Is.Not.Null, "Strobe icons need the instanced SDF shader.");
            var material = new Material(shader) { enableInstancing = true };
            try
            {
                Assert.That(shader.isSupported, Is.True);
                Assert.That(material.HasProperty("_SdfAtlas"), Is.True);
                Assert.That(material.enableInstancing, Is.True);
                ShaderUtil.CompilePass(material, 0, true);
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                {
                    Assert.That(message.severity.ToString(), Is.Not.EqualTo("Error"), message.message);
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ExistingAtlasContainsOnlyTheOriginalIcons()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                "Assets/_Graphics/Textures/GLS Event Icons/GLS Event Icons.spriteatlasv2");
            Assert.That(atlas.spriteCount, Is.EqualTo(38), "Only easing and rotation sprites belong in this atlas.");
        }

        [Test]
        public void DestroyedCameraRetiresItsBatchWhileNodesRemainActive()
        {
            var instance = GLSStrobePulseWidthIconTest.CreateIcon(EaseType.Linear, 0.5f);
            var cameraObject = new GameObject("Temporary strobe camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(32, 32, 24);
            try
            {
                camera.enabled = false;
                camera.targetTexture = target;
                camera.Render();
                var registry = (IDictionary)typeof(GLSStrobeIconBatch)
                    .GetField("cameras", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Assert.That(registry.Contains(camera), Is.True);
                Object.DestroyImmediate(cameraObject);
                Assert.That(registry.Contains(camera), Is.False,
                    "A destroyed camera must release its instance buffer while other strobe nodes remain active.");
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(instance);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        [UnityTest]
        public IEnumerator MoreThan1023FacesRenderInOneBatchAndRetireWithTheirPool()
        {
            const int count = 1120;
            var objects = new List<GameObject>();
            var otherCameras = new List<Camera>();
            var cameraObject = new GameObject("Strobe instancing camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1280, 640, 24);
            var image = new Texture2D(1280, 640, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            using var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            try
            {
                camera.enabled = false;
                foreach (var other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (other != camera && other.enabled)
                    {
                        otherCameras.Add(other);
                        other.enabled = false;
                    }
                }

                camera.orthographic = true;
                camera.orthographicSize = 0.9f;
                camera.aspect = 2f;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.gray;
                camera.targetTexture = target;
                camera.enabled = true;
                yield return null;
                yield return null;
                yield return null;
                var baselineDraws = drawCalls.LastValue;
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Graphics/Materials/Editor/GLS Strobe Icon.mat");
                for (var i = 0; i < count; i++)
                {
                    var surface = new GameObject("Instanced strobe face");
                    objects.Add(surface);
                    surface.SetActive(false);
                    surface.layer = 31;
                    surface.transform.position = new Vector3(((i % 40) - 19.5f) * 0.09f, ((i / 40) - 13.5f) * 0.055f);
                    surface.transform.localScale = Vector3.one * 0.025f;
                    var renderer = surface.AddComponent<GLSStrobeIconRenderer>();
                    renderer.Initialize(material);
                    renderer.SetIcon(
                        i % 2 == 0 ? GLSEventIconType.EaseLinear : GLSEventIconType.NoEasingStep,
                        0.2f + ((i % 7) * 0.1f));
                    surface.SetActive(true);
                }

                yield return null;
                yield return null;
                yield return null;
                camera.enabled = false;
                var populatedDraws = drawCalls.LastValue;
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 640), 0, 0);
                image.Apply();
                var pixels = image.GetPixels32();
                for (var i = 0; i < count; i++)
                {
                    var pulseWidth = 0.2f + ((i % 7) * 0.1f);
                    var local = new Vector3(
                        (13f + (129f * pulseWidth) - 142f) / 100f,
                        i % 2 == 0 ? 0f : -0.51f);
                    var position = objects[i].transform.TransformPoint(local);
                    var viewport = camera.WorldToViewportPoint(position);
                    var x = Mathf.RoundToInt(viewport.x * 1280);
                    var y = Mathf.RoundToInt(viewport.y * 640);
                    var brightest = 0;
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            brightest = Mathf.Max(brightest, pixels[(y + dy) * 1280 + x + dx].r);
                        }
                    }

                    Assert.That(brightest, Is.GreaterThan(170), $"Instanced face {i} did not render its curve.");
                }

                var buffers = camera.GetCommandBuffers(CameraEvent.AfterForwardAlpha);
                Assert.That(buffers.Length, Is.EqualTo(1));
                Assert.That(buffers[0].name, Is.EqualTo("GLS Strobe Icons"));
                TestContext.WriteLine($"GPU statistics: baseline={baselineDraws} draw calls, populated={populatedDraws}, faces={count}.");
                Assert.That(populatedDraws - baselineDraws, Is.EqualTo(1),
                    "All 1120 faces must add exactly one GPU draw call.");
                var directory = "TestResults/strobe-sdf";
                Directory.CreateDirectory(directory);
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i].a = 255;
                }

                image.SetPixels32(pixels);
                image.Apply();
                File.WriteAllBytes(directory + "/1120-instanced-faces.png", image.EncodeToPNG());

                foreach (var surface in objects)
                {
                    surface.SetActive(false);
                }

                camera.Render();
                Assert.That(camera.GetCommandBuffers(CameraEvent.AfterForwardAlpha), Is.Empty,
                    "Returning every face to the pool must detach the shared batch and release its resources.");
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 640), 0, 0);
                image.Apply();
                foreach (var pixel in image.GetPixels32())
                {
                    Assert.That(pixel.r, Is.EqualTo(128).Within(2));
                }
            }
            finally
            {
                RenderTexture.active = previous;
                foreach (var surface in objects)
                {
                    Object.DestroyImmediate(surface);
                }

                foreach (var other in otherCameras)
                {
                    if (other != null)
                    {
                        other.enabled = true;
                    }
                }

                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(image);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}

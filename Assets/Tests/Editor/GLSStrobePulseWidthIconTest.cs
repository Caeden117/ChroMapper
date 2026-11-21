using System;
using System.Collections.Generic;
using System.IO;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSStrobePulseWidthIconTest
    {
        [Test]
        public void ClonedActiveStrobeGroupRendersWithoutUninitializedBatchEntries()
        {
            var source = CreateIcon(EaseType.OutBack, 0.15f, "GLS Group");
            GameObject clone = null;
            var cameraObject = new GameObject("Live strobe clone camera", typeof(Camera));
            var target = new RenderTexture(32, 32, 24);
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.targetTexture = target;
                clone = Object.Instantiate(source);
                camera.Render();
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(clone);
                Object.DestroyImmediate(source);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        [TestCase("GLS Event", true, false, 1)]
        [TestCase("GLS Event", false, false, 1)]
        [TestCase("GLS Group", true, false, 1)]
        [TestCase("GLS Group", false, false, 1)]
        [TestCase("GLS Event", true, true, 1)]
        [TestCase("GLS Group", true, true, 1)]
        [TestCase("GLS Event", true, true, 0)]
        [TestCase("GLS Group", true, true, 0)]
        public void ClonedLiveStrobeIconsRetainOnlyTheirInitializedSurfaces(
            string prefabName,
            bool activeSource,
            bool inactivePreviewParent,
            int frequency)
        {
            var source = CreateIcon(EaseType.OutBack, 0.15f, prefabName);
            var previewParent = new GameObject("Strobe preview root");
            previewParent.SetActive(!inactivePreviewParent);
            GameObject clone = null;
            var cameraObject = new GameObject("Cloned strobe icon camera", typeof(Camera));
            var target = new RenderTexture(32, 32, 24);
            try
            {
                source.SetActive(activeSource);
                clone = Object.Instantiate(source, previewParent.transform);
                clone.SetActive(true);
                var surfaces = clone.GetComponentsInChildren<GLSStrobeIconRenderer>(true);
                Assert.That(
                    surfaces,
                    Has.Length.EqualTo(2),
                    "Cloning a live GLS node must reuse its copied icon surfaces rather than create another pair.");
                var evt = new BaseLightColorBase
                {
                    Frequency = frequency,
                    StrobeFade = 1,
                    ChromaStrobeEasing = (int)EaseType.OutBack,
                    ChromaStrobePulseWidth = 0.15f
                };
                Assert.DoesNotThrow(() => clone.GetComponent<GLSEventIconView>().SetIcons(
                    GLSEventIconResolver.Resolve(evt),
                    evt,
                    clone.GetComponentsInChildren<TextMeshPro>(true)));
                previewParent.SetActive(true);
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.targetTexture = target;
                camera.Render();
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(clone);
                Object.DestroyImmediate(previewParent);
                Object.DestroyImmediate(source);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        [TestCase("GLS Event", 0)]
        [TestCase("GLS Event", 1)]
        [TestCase("GLS Group", 0)]
        [TestCase("GLS Group", 1)]
        public void HiddenPlacementPreviewInitializesIconsBeforeItsFirstActivation(string prefabName, int frequency)
        {
            var track = new GameObject("Inactive GLS placement track");
            var placementObject = new GameObject("Inactive GLS placement controller");
            track.SetActive(false);
            placementObject.SetActive(false);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Prefabs/MapEditor/Beatmap/{prefabName}.prefab");
                GameObject preview;
                if (prefabName == "GLS Event")
                {
                    var placement = placementObject.AddComponent<GLSEventColorPlacement>();
                    placement.ObjectContainerPrefab = prefab;
                    placement.PlacementTrack = track.transform;
                    placement.CreateVisual();
                    preview = placement.PlacementVisualContainer.gameObject;
                }
                else
                {
                    var placement = placementObject.AddComponent<GLSGroupRotationPlacement>();
                    placement.ObjectContainerPrefab = prefab;
                    placement.PlacementTrack = track.transform;
                    placement.CreateVisual();
                    preview = placement.PlacementVisualContainer.gameObject;
                }

                Assert.That(preview.activeInHierarchy, Is.False);
                var surface = preview.transform.Find("Secondary Icon Side/Strobe Icon Surface");
                Assert.That(surface, Is.Not.Null, "Hidden placement previews need their icon mesh before controls refresh them.");
                var topSurface = preview.transform.Find("Secondary Icon Top/Strobe Icon Surface");
                Assert.That(topSurface, Is.Not.Null);
                var renderer = surface.GetComponent<GLSStrobeIconRenderer>();
                Assert.That(renderer, Is.Not.Null);
                var evt = new BaseLightColorBase { Frequency = frequency, StrobeFade = 1 };
                var view = preview.GetComponent<GLSEventIconView>();
                Assert.DoesNotThrow(() =>
                {
                    view.SetIcons(
                        GLSEventIconResolver.Resolve(evt),
                        evt,
                        preview.GetComponentsInChildren<TextMeshPro>(true));
                });

                track.SetActive(true);
                Assert.That(surface.GetComponent<GLSStrobeIconRenderer>(), Is.SameAs(renderer));
                Assert.That(topSurface.GetComponent<GLSStrobeIconRenderer>().enabled, Is.EqualTo(frequency > 0));
                Assert.That(
                    preview.transform.Find("Secondary Icon Side/Strobe Icon Surface"),
                    Is.SameAs(surface));
                Assert.That(
                    preview.transform.Find("Secondary Icon Top/Strobe Icon Surface"),
                    Is.SameAs(topSurface));
                Assert.That(surface.parent.GetComponentsInChildren<GLSStrobeIconRenderer>(true).Length, Is.EqualTo(1));
                Assert.That(topSurface.parent.GetComponentsInChildren<GLSStrobeIconRenderer>(true).Length, Is.EqualTo(1));
            }
            finally
            {
                // Activate only the preview hierarchy so its existing Awake/OnDestroy cleanup also runs after a failed assertion.
                track.SetActive(true);
                Object.DestroyImmediate(track);
                Object.DestroyImmediate(placementObject);
            }
        }

        private static IEnumerable<TestCaseData> AdditionalRenderedEasings()
        {
            foreach (EaseType easing in Enum.GetValues(typeof(EaseType)))
            {
                if (easing is not (EaseType.Linear or EaseType.OutElastic))
                {
                    yield return new TestCaseData(easing, 0.942f);
                }

                foreach (var pulseWidth in new[] { 0f, 0.01f, 0.02f, 0.03f, 0.97f, 0.98f, 0.99f, 1f })
                {
                    if (easing == EaseType.InOutCubic && pulseWidth is 0f or 0.01f or 0.99f or 1f)
                    {
                        continue;
                    }

                    yield return new TestCaseData(easing, pulseWidth);
                }
            }
        }

        [Test]
        public void PulseWidthKeepsItsExactValue()
        {
            foreach (var pulseWidth in new[]
            {
                0.01f, 0.02f, 0.03f, 0.34f, 0.36f, 0.44f, 0.46f, 0.54f, 0.56f, 0.64f, 0.66f, 0.97f, 0.98f, 0.99f,
                0f, 0.001f, 0.024f, 0.025f, 0.074f, 0.075f, 0.942f, 0.976f, 0.999f, 1f
            })
            {
                var instance = CreateIcon(EaseType.Linear, pulseWidth);
                try
                {
                    var renderer = instance.transform.Find("Secondary Icon Side/Strobe Icon Surface")
                        .GetComponent<GLSStrobeIconRenderer>();
                    Assert.That(renderer.CurveParameters.y, Is.EqualTo(pulseWidth));
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [TestCase("GLS Event", 0f)]
        [TestCase("GLS Event", 0.01f)]
        [TestCase("GLS Event", 0.5f)]
        [TestCase("GLS Event", 0.942f)]
        [TestCase("GLS Event", 0.99f)]
        [TestCase("GLS Event", 1f)]
        [TestCase("GLS Group", 0.942f)]
        public void PulseWidthUsesOneProceduralSurfaceOnEachFace(string prefabName, float pulseWidth)
        {
            var instance = CreateIcon(EaseType.InOutCubic, pulseWidth, prefabName);
            try
            {
                var side = instance.transform.Find("Secondary Icon Side");
                var renderer = side.Find("Strobe Icon Surface").GetComponent<GLSStrobeIconRenderer>();
                var top = instance.transform.Find("Secondary Icon Top/Strobe Icon Surface")
                    .GetComponent<GLSStrobeIconRenderer>();
                Assert.That(renderer.enabled, Is.True);
                Assert.That(top.enabled, Is.True);
                Assert.That(side.GetComponent<SpriteRenderer>().enabled, Is.False);
                Assert.That(renderer.CurveParameters, Is.EqualTo(top.CurveParameters));
                Assert.That(renderer.CurveParameters.y, Is.EqualTo(pulseWidth));
                Assert.That(side.localPosition.x, Is.EqualTo(GLSEventIconView.StrobeIconHorizontalPosition));
                Assert.That(renderer.GetComponent<MeshRenderer>(), Is.Null, "Only the shared batch submits these surfaces.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void EveryEasingUsesItsRuntimeCurveAndSharedOvershootRange()
        {
            foreach (EaseType easing in Enum.GetValues(typeof(EaseType)))
            {
                var instance = CreateIcon(easing, 0.942f);
                try
                {
                    var renderer = instance.transform.Find("Secondary Icon Side/Strobe Icon Surface")
                        .GetComponent<GLSStrobeIconRenderer>();
                    var curve = easing == EaseType.None ? Easing.Step : Easing.FromID((int)easing);
                    GetCurveRange(curve, out var minimum, out var maximum);
                    Assert.That(renderer.CurveParameters.x, Is.EqualTo(Easing.EasingShaderId((int)easing)), easing.ToString());
                    Assert.That(renderer.CurveParameters.z, Is.EqualTo(minimum).Within(0.00001f));
                    Assert.That(renderer.CurveParameters.w, Is.EqualTo(maximum - minimum).Within(0.00001f));
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void BothPrefabsUseTheSameAtlasMaterial()
        {
            var expected = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Graphics/Materials/Editor/GLS Strobe Icon.mat");
            Assert.That(expected, Is.Not.Null);
            Assert.That(expected.enableInstancing, Is.True);
            Assert.That(expected.GetTexture("_SdfAtlas"), Is.SameAs(
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Graphics/Textures/GLS Strobe SDF.glsstrobesdf")));
            foreach (var name in new[] { "GLS Event", "GLS Group" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Prefabs/MapEditor/Beatmap/{name}.prefab");
                var strobeRenderers = new SerializedObject(prefab.GetComponent<GLSEventIconView>())
                    .FindProperty("strobeRenderers");
                for (var i = 0; i < strobeRenderers.arraySize; i++)
                {
                    var surface = strobeRenderers.GetArrayElementAtIndex(i).objectReferenceValue;
                    var material = new SerializedObject(surface).FindProperty("material").objectReferenceValue;
                    Assert.That(material, Is.SameAs(expected));
                }
            }
        }

        private static IEnumerable<TestCaseData> StretchedMiddleReferences()
        {
            foreach (var easing in new[] { EaseType.InBounce, EaseType.OutBounce, EaseType.InOutBounce, EaseType.BeatSaberInOutBounce, EaseType.OutElastic })
            {
                foreach (var pulseWidth in new[] { 0.15f, 0.2f, 0.3f, 0.374f, 0.376f, 0.4f, 0.6f, 0.624f, 0.626f, 0.7f, 0.8f, 0.85f })
                    yield return new TestCaseData(easing, pulseWidth);
            }
        }

        [TestCase(EaseType.Linear, 0.942f)]
        [TestCase(EaseType.OutElastic, 0.942f)]
        [TestCase(EaseType.OutElastic, 0.011f)]
        [TestCase(EaseType.OutElastic, 0.019f)]
        [TestCase(EaseType.OutElastic, 0.029f)]
        [TestCase(EaseType.OutElastic, 0.041f)]
        [TestCase(EaseType.OutElastic, 0.951f)]
        [TestCase(EaseType.OutElastic, 0.96f)]
        [TestCase(EaseType.OutElastic, 0.975f)]
        [TestCase(EaseType.OutElastic, 0.985f)]
        [TestCase(EaseType.OutElastic, 0.995f)]
        [TestCase(EaseType.InBack, 0.37f)]
        [TestCase(EaseType.BeatSaberInOutElastic, 0.5f)]
        [TestCase(EaseType.InOutCubic, 0f)]
        [TestCase(EaseType.InOutCubic, 0.01f)]
        [TestCase(EaseType.InOutCubic, 0.99f)]
        [TestCase(EaseType.InOutCubic, 1f)]
        [TestCaseSource(nameof(AdditionalRenderedEasings))]
        [TestCaseSource(nameof(StretchedMiddleReferences))]
        public void RenderedSurfaceFollowsTheExactPulseWidthCurve(EaseType easing, float pulseWidth)
        {
            var pixels = RenderCurve(easing, pulseWidth, Color.gray);
            var curve = easing == EaseType.None ? Easing.Step : Easing.FromID((int)easing);
            GetCurveRange(curve, out var minimum, out var maximum);
            void AssertRendered(float phase, float value)
            {
                var x = Mathf.RoundToInt(2f * (13f + (phase * 258f)));
                var y = Mathf.RoundToInt(2f * (13f + (((value - minimum) / (maximum - minimum)) * 102f)));
                AssertWhiteNearPixel(pixels, x, y, $"{easing}, {pulseWidth}");
            }

            for (var sample = 0; sample <= 71; sample++)
            {
                var progress = sample / 71f;
                if (pulseWidth > 0f)
                {
                    AssertRendered(pulseWidth * progress, curve(progress));
                }

                if (pulseWidth < 1f)
                {
                    AssertRendered(pulseWidth + ((1f - pulseWidth) * progress), 1f - curve(progress));
                }
            }

            if (pulseWidth == 0f || pulseWidth == 1f)
            {
                for (var sample = 0; sample <= 32; sample++)
                {
                    AssertRendered(pulseWidth, sample / 32f);
                }
            }

            foreach (var pixel in pixels)
            {
                if (pixel.r > 245 && pixel.g > 245 && pixel.b > 245)
                {
                    Assert.That(pixel.a, Is.Zero, "The visible stroke must retain the no-bloom alpha mask.");
                }
            }
        }

        internal static GameObject CreateIcon(EaseType easing, float pulseWidth, string prefabName = "GLS Event")
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Prefabs/MapEditor/Beatmap/{prefabName}.prefab");
            var instance = Object.Instantiate(prefab);
            var evt = new BaseLightColorBase
            {
                Frequency = 1,
                StrobeFade = easing == EaseType.None ? 0 : 1,
                ChromaStrobePulseWidth = pulseWidth,
                ChromaStrobeEasing = (int)easing
            };
            instance.GetComponent<GLSEventIconView>().SetIcons(
                GLSEventIconResolver.Resolve(evt), evt, instance.GetComponentsInChildren<TextMeshPro>());
            return instance;
        }

        internal static Color32[] RenderCurve(
            EaseType easing,
            float pulseWidth,
            Color background,
            Vector2 pixelOffset = default)
        {
            var instance = CreateIcon(easing, pulseWidth);
            var cameraObject = new GameObject("Strobe SDF render test camera", typeof(Camera));
            var target = new RenderTexture(568, 256, 24, RenderTextureFormat.ARGB32);
            var readback = new Texture2D(568, 256, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var side = instance.transform.Find("Secondary Icon Side");
                side.localPosition = Vector3.zero;
                side.localRotation = Quaternion.identity;
                side.localScale = Vector3.one;
                var surface = side.Find("Strobe Icon Surface");
                // Raster assertions use native icon coordinates independently of the face's display scale.
                surface.localScale = Vector3.one;
                surface.gameObject.layer = 31;
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = GLSStrobeIconRenderer.Size.y * 0.5f;
                camera.aspect = GLSStrobeIconRenderer.Size.x / GLSStrobeIconRenderer.Size.y;
                camera.transform.position = new Vector3(
                    pixelOffset.x * GLSStrobeIconRenderer.Size.x / 568f,
                    pixelOffset.y * GLSStrobeIconRenderer.Size.y / 256f,
                    -10f);
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 568, 256), 0, 0);
                readback.Apply();
                var pixels = readback.GetPixels32();
                var directory = "TestResults/strobe-sdf";
                Directory.CreateDirectory(directory);
                var preview = (Color32[])pixels.Clone();
                for (var i = 0; i < preview.Length; i++)
                {
                    preview[i].a = 255;
                }

                readback.SetPixels32(preview);
                readback.Apply();
                File.WriteAllBytes($"{directory}/{easing}-{pulseWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png", readback.EncodeToPNG());

                var renderer = surface.GetComponent<GLSStrobeIconRenderer>();
                var view = instance.GetComponent<GLSEventIconView>();
                var displays = instance.GetComponentsInChildren<TextMeshPro>();
                var evt = new BaseLightColorBase { Frequency = 1, StrobeFade = 1, ChromaStrobePulseWidth = 0.013f };
                view.SetIcons(GLSEventIconResolver.Resolve(evt), evt, displays);
                Assert.That(surface.GetComponent<GLSStrobeIconRenderer>(), Is.SameAs(renderer));
                Assert.That(renderer.CurveParameters.y, Is.EqualTo(0.013f));
                var rotation = new BaseLightRotationBase();
                view.SetIcons(GLSEventIconResolver.Resolve(rotation), rotation, displays);
                Assert.That(renderer.enabled, Is.False);
                Assert.That(side.GetComponent<SpriteRenderer>().enabled, Is.True);
                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 568, 256), 0, 0);
                readback.Apply();
                var cleared = readback.GetPixels32();
                Assert.That(cleared[128 * 568 + 284].r, Is.EqualTo(((Color32)background).r).Within(2), "A pooled rebind must clear the camera's previous strobe draw.");
                return pixels;
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(readback);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        internal static void GetCurveRange(Func<float, float> curve, out float minimum, out float maximum)
        {
            minimum = 0f;
            maximum = 1f;
            for (var sample = 0; sample < 512; sample++)
            {
                var value = curve(sample / 511f);
                minimum = Mathf.Min(minimum, value, 1f - value);
                maximum = Mathf.Max(maximum, value, 1f - value);
            }

            if (minimum < 0f || maximum > 1f)
            {
                var padding = (maximum - minimum) * 0.08f;
                minimum -= padding;
                maximum += padding;
            }
        }

        internal static void AssertWhiteNearPixel(Color32[] pixels, int x, int y, string context)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    var pixel = pixels[(Mathf.Clamp(y + dy, 0, 255) * 568) + Mathf.Clamp(x + dx, 0, 567)];
                    if (pixel.r > 220 && pixel.g > 220 && pixel.b > 220)
                    {
                        return;
                    }
                }
            }

            Assert.Fail($"{context}: no white stroke near ({x}, {y}).");
        }
    }
}

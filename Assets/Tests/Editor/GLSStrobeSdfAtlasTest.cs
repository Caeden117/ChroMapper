using System;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSStrobeSdfAtlasTest
    {
        private const string AtlasPath = "Assets/_Graphics/Textures/GLS Strobe SDF.glsstrobesdf";

        [Test]
        public void TwentyThreeReferenceWidthsRetainEndpointDetailAndMirroredPhases()
        {
            Assert.That(GLSStrobeSdfAtlas.PulseWidthCount, Is.EqualTo(23));
            for (var index = 0; index < GLSStrobeSdfAtlas.PulseWidthCount; index++)
            {
                var reference = GLSStrobeSdfAtlas.ReferencePulseWidth(index);
                var complement = GLSStrobeSdfAtlas.ReferencePulseWidth(GLSStrobeSdfAtlas.PulseWidthCount - 1 - index);
                Assert.That(reference + complement, Is.EqualTo(1f).Within(0.000001f));
            }

            foreach (var width in new[] { 0f, 0.01f, 0.02f, 0.03f, 0.05f, 0.95f, 0.97f, 0.98f, 0.99f, 1f })
            {
                var parameters = GLSStrobeSdfAtlas.GetFieldParameters(0, width, 1f, out _);
                Assert.That(parameters.z, Is.EqualTo(width), $"Keep an exact reference for the compressed phase at {width}.");
            }
        }

        [Test]
        public void AtlasUses32PixelFieldsInASingleChannel()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            Assert.That(atlas, Is.Not.Null, "Strobe icons need an imported, precomputed SDF atlas.");
            Assert.That(atlas.width, Is.EqualTo(1800));
            Assert.That(atlas.height, Is.EqualTo(1440));
            Assert.That(atlas.format, Is.EqualTo(TextureFormat.R8));
            Assert.That(atlas.isDataSRGB, Is.False, "Distance samples must not undergo color-space conversion.");
            Assert.That(atlas.mipmapCount, Is.EqualTo(1), "Ordinary color mipmaps do not preserve signed distances.");
            Assert.That(atlas.filterMode, Is.EqualTo(FilterMode.Bilinear));
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(atlas, out string guid, out long fileId);
            TestContext.WriteLine($"SDF atlas binding: guid={guid}, fileID={fileId}, size={atlas.width}x{atlas.height}, format={atlas.format}.");
        }

        [Test]
        public void SharedStrobeMaterialSamplesTheBakedAtlas()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Graphics/Materials/Editor/GLS Strobe Icon.mat");
            Assert.That(material.HasProperty("_SdfAtlas"), Is.True,
                "The fragment shader must consume the baked field instead of evaluating easing segments.");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            Assert.That(material.GetTexture("_SdfAtlas"), Is.SameAs(atlas));
            Assert.That(material.enableInstancing, Is.True);
        }

        [Test]
        public void EveryReferenceFieldContainsItsEasingCurve()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            var target = new RenderTexture(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(atlas, target);
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
                readback.Apply();
                var pixels = readback.GetPixels32();
                var fields = 0;
                foreach (EaseType easing in Enum.GetValues(typeof(EaseType)))
                {
                    var curve = Easing.FromID((int)easing);
                    GLSStrobePulseWidthIconTest.GetCurveRange(curve, out var minimum, out var maximum);
                    var shaderId = Easing.EasingShaderId((int)easing);
                    for (var index = 0; index < GLSStrobeSdfAtlas.PulseWidthCount; index++)
                    {
                        var pulseWidth = GLSStrobeSdfAtlas.ReferencePulseWidth(index);
                        var field = (shaderId * GLSStrobeSdfAtlas.PulseWidthCount) + index;
                        var origin = new Vector2(
                            ((field % GLSStrobeSdfAtlas.Columns) * GLSStrobeSdfAtlas.TileWidth) + GLSStrobeSdfAtlas.Padding,
                            ((field / GLSStrobeSdfAtlas.Columns) * GLSStrobeSdfAtlas.TileHeight) + GLSStrobeSdfAtlas.Padding);
                        for (var sample = 0; sample <= 16; sample++)
                        {
                            var progress = sample / 16f;
                            var value = pulseWidth == 0f ? progress : curve(progress);
                            var coordinate = new Vector2(
                                13f + (258f * pulseWidth * progress),
                                13f + (102f * (value - minimum) / (maximum - minimum)));
                            var position = origin + (coordinate / 4f) - (Vector2.one * 0.5f);
                            var encoded = Sample(pixels, atlas.width, position).r;
                            var cornerField = GLSStrobeSdfAtlas.CornerFieldIndex(shaderId, index);
                            if (cornerField >= 0)
                            {
                                var cornerPosition = GLSStrobeSdfAtlas.FieldOrigin(cornerField) + (coordinate / 4f) - (Vector2.one * 0.5f);
                                encoded = Mathf.Min(encoded, Sample(pixels, atlas.width, cornerPosition).r);
                            }

                            var distance = ((encoded - 0.5f) * 32f) + 5.90625f;
                            // A 32px field samples every four icon units, bounding centerline interpolation error by half a texel diagonal.
                            Assert.That(distance, Is.LessThan(3f), $"{easing}, pulse width {pulseWidth}, progress {progress}: baked curve missing.");
                        }

                        fields++;
                    }
                }

                Assert.That(fields, Is.EqualTo(805));
                Assert.That(atlas.isReadable, Is.False, "The imported atlas must not retain a CPU pixel copy.");
                TestContext.WriteLine($"SDF atlas runtime memory: {UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(atlas)} bytes, graphics format={atlas.graphicsFormat}.");
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        private static Color Sample(Color32[] pixels, int width, Vector2 position)
        {
            var x = Mathf.FloorToInt(position.x);
            var y = Mathf.FloorToInt(position.y);
            var fractionX = position.x - x;
            var fractionY = position.y - y;
            var bottom = Color.Lerp(pixels[(y * width) + x], pixels[(y * width) + x + 1], fractionX);
            var top = Color.Lerp(pixels[((y + 1) * width) + x], pixels[((y + 1) * width) + x + 1], fractionX);
            return Color.Lerp(bottom, top, fractionY);
        }
    }
}

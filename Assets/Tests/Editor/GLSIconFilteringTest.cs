using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Tests.Editor
{
    public class GLSIconFilteringTest
    {
        private const string IconRoot = "Assets/_Graphics/Textures/GLS Event Icons/";

        [Test]
        public void MinifiedIconPreservesSampledAlpha()
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBAFloat, true, true)
            {
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var material = new Material(Shader.Find("ChroMapper/GLS Icon Sprite"));
            try
            {
                var pixels = new Color[64 * 64];
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(1f, 1f, 1f, 0.25f);
                texture.SetPixels(pixels);
                texture.Apply(true);
                material.mainTexture = texture;
                var result = RenderIcon(material, FullQuad(), 16, 0f, 1f);
                Assert.That(result[32 * 64 + 32].r, Is.EqualTo(0.25f).Within(0.005f),
                    "Minification must preserve filtered alpha, rather than brighten and thicken the icon.");
                Assert.That(result[32 * 64 + 32].a, Is.Zero.Within(0.001f),
                    "Icons must retain their existing no-bloom alpha mask.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        // Check the packed textures used by real node prefabs. Requesting mipmaps in atlas settings does not
        // guarantee the imported page has them.
        [Test]
        public void PrefabIconTexturesHaveFilteredMipmaps()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(IconRoot + "GLS Event Icons.spriteatlasv2");
            Assert.That(atlas, Is.Not.Null);
            var textures = new System.Collections.Generic.HashSet<Texture2D>();
            foreach (var prefabName in new[] { "GLS Event", "GLS Group" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Prefabs/MapEditor/Beatmap/{prefabName}.prefab");
                var view = prefab.GetComponent<Beatmap.Containers.GLSEventIconView>();
                var icons = new SerializedObject(view).FindProperty("icons");
                Assert.That(icons.arraySize, Is.GreaterThan(30));
                for (var i = 0; i < icons.arraySize; i++)
                {
                    var sprite = (Sprite)icons.GetArrayElementAtIndex(i).objectReferenceValue;
                    var iconType = (GLSEventIconType)(i + 1);
                    // The retired Instant/Transition slots preserve serialized enum indices but have no artwork.
                    if (iconType is GLSEventIconType.Instant or GLSEventIconType.Transition)
                    {
                        Assert.That(sprite, Is.Null, iconType.ToString());
                        continue;
                    }

                    Assert.That(sprite, Is.Not.Null, iconType.ToString());
                    Assert.That(atlas.CanBindTo(sprite), Is.True, sprite.name);
                    var texture = sprite.texture;
                    if (textures.Add(texture))
                        TestContext.WriteLine($"{sprite.name}: packed={sprite.packed}, texture={texture.name}, "
                            + $"size={texture.width}x{texture.height}, mips={texture.mipmapCount}, "
                            + $"filter={texture.filterMode}, anisotropy={texture.anisoLevel}");
                    Assert.That(texture.mipmapCount, Is.GreaterThan(1), sprite.name);
                    Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Trilinear), sprite.name);
                    Assert.That(texture.anisoLevel, Is.GreaterThanOrEqualTo(4), sprite.name);
                }
            }
        }

        // Real atlas glyphs should match ordinary filtered sprite coverage at
        // small projected sizes, including oblique views, without alpha expansion.
        [TestCase("EaseInQuadratic")]
        [TestCase("EaseInOutSinusoidal")]
        [TestCase("RotationClockwise")]
        [TestCase("RotationCounterClockwise")]
        public void MinifiedAtlasIconMatchesFilteredSprite(string name)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(IconRoot + "GLS Event Icons.spriteatlasv2");
            var sprite = atlas.GetSprite(name);
            Assert.That(sprite, Is.Not.Null);
            var actual = new Material(Shader.Find("ChroMapper/GLS Icon Sprite"));
            var reference = new Material(Shader.Find("Sprites/Default"));
            try
            {
                actual.mainTexture = reference.mainTexture = sprite.texture;
                foreach (var size in new[] { 12, 24 })
                {
                    foreach (var squish in new[] { 1f, 0.35f })
                    {
                        var rendered = RenderIcon(actual, SpriteQuad(sprite), size, 17f, squish);
                        var expected = RenderIcon(reference, SpriteQuad(sprite), size, 17f, squish);
                        var stem = $"{name}-{size}-{squish}";
                        SaveIcon(stem + "-actual", rendered);
                        SaveIcon(stem + "-reference", expected);
                        var worst = 0f;
                        var visible = 0;
                        for (var i = 0; i < rendered.Length; i++)
                        {
                            worst = Mathf.Max(worst, Mathf.Abs(rendered[i].r - expected[i].r),
                                Mathf.Abs(rendered[i].g - expected[i].g), Mathf.Abs(rendered[i].b - expected[i].b));
                            if (Mathf.Max(expected[i].r, expected[i].g, expected[i].b) > 0.05f)
                                visible++;
                        }
                        Assert.That(visible, Is.GreaterThan(3), stem);
                        Assert.That(worst, Is.LessThan(0.022f),
                            $"{stem}: filtered coverage changed by {worst}; only the existing 0.02 cutout is permitted.");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(actual);
                Object.DestroyImmediate(reference);
                Object.DestroyImmediate(sprite);
            }
        }

        private static Mesh FullQuad() => Quad(new[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        });

        private static Mesh SpriteQuad(Sprite sprite)
        {
            var rect = sprite.textureRect;
            var texture = sprite.texture;
            return Quad(new[]
            {
                new Vector2(rect.xMin / texture.width, rect.yMin / texture.height),
                new Vector2(rect.xMax / texture.width, rect.yMin / texture.height),
                new Vector2(rect.xMax / texture.width, rect.yMax / texture.height),
                new Vector2(rect.xMin / texture.width, rect.yMax / texture.height)
            });
        }

        private static Mesh Quad(Vector2[] uv) => new Mesh
        {
            vertices = new[]
            {
                new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f),
                new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f)
            },
            uv = uv,
            colors = new[] { Color.white, Color.white, Color.white, Color.white },
            triangles = new[] { 0, 1, 2, 0, 2, 3 }
        };

        private static Color[] RenderIcon(Material material, Mesh mesh, int size, float angle, float squish)
        {
            var target = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(64, 64, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderTexture.active = target;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadOrtho();
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0f),
                    Quaternion.Euler(0, 0, angle), new Vector3(size / 64f, size * squish / 64f, 1f)));
                GL.PopMatrix();
                readback.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
                readback.Apply(false);
                return readback.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(readback);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        private static void SaveIcon(string name, Color[] pixels)
        {
            var image = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            try
            {
                // Encode a copy so saved previews do not change the linear pixel assertions.
                var encoded = new Color[pixels.Length];
                for (var i = 0; i < pixels.Length; i++)
                {
                    encoded[i] = pixels[i].gamma;
                    encoded[i].a = 1f;
                }
                image.SetPixels(encoded);
                image.Apply(false);
                var directory = Path.Combine(Application.dataPath, "..", "TestResults", "gls-icon-aa");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }
    }
}

using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public partial class GLSColorTransitionCacheTest
    {
        // MonstercatStrobeRibbonEdgesMatchSupersampling keeps the separated
        // first-node edges alongside both later color-to-color transition spans.
        [TestCase(false, 6f, 6.5f)]
        [TestCase(false, 6.5f, 7f)]
        [TestCase(false, 7f, 7.5f)]
        [TestCase(true, 6f, 6.5f)]
        [TestCase(true, 6.5f, 7f)]
        public void MonstercatStrobeRibbonEdgesMatchSupersampling(bool inner, float start, float end)
        {
            var group = CreateGroup(6f, 15, 0, filterParam: 2,
                filterChunks: 4, brightnessDistribution: 0f);
            var second = CreateGroup(6f, 15, 0, filterParam: 2, filterParam1: 1,
                filterChunks: 4, brightnessDistribution: 0f);
            ((JSONArray)group["e"]).Add(second["e"][0]);
            for (var lane = 0; lane < 2; lane++)
            {
                var box = group["e"][lane];
                box["w"] = 1;
                box["d"] = 1;
                box["b"] = 0;
                var events = new JSONArray();
                for (var n = 0; n < 2; n++)
                {
                    events.Add(new JSONObject
                    {
                        ["b"] = n * 0.5f, ["c"] = 0, ["s"] = n == 0 ? 0.2f : 0.7f,
                        ["i"] = n, ["f"] = 2, ["sb"] = 0.3f, ["sf"] = 0,
                        ["customData"] = new JSONObject
                        {
                            ["color"] = WaveColor(lane == 0 ? 0f : 0.9f, 1f, n == 0 ? 0.197f : 0.254f),
                            ["strobeColor"] = WaveColor(lane == 0 ? 1f : 0f, lane == 0 ? 0f : 1f, 0.999f),
                            ["colorEasing"] = n == 0 ? 0 : 20
                        }
                    });
                }
                box["e"] = events;
            }
            var following = CreateGroup(7.5f, 15, 2, filterChunks: 0, brightnessDistribution: 0f);
            following["e"][0]["w"] = 0;
            following["e"][0]["b"] = 0;
            following["e"][0]["e"][0]["c"] = 0;
            following["e"][0]["e"][0]["s"] = 0.5f;
            VerifyPerspectiveRibbonEdges(new[] { group, following }, 4, start, end,
                inner, $"beat{start}-strobe");
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void SeparatedStripCoverageIsLinear(float direction)
        {
            const int width = 80;
            const int height = 512;
            var material = new Material(Shader.Find("ChroMapper/Object/Basic Gradient"));
            var texture = new Texture2D(4, 9, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var target = new RenderTexture(width, height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var read = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            var mesh = new Mesh();
            var previous = RenderTexture.active;
            try
            {
                var pixels = new Color[36];
                for (var column = 0; column < 4; column++)
                {
                    var active = column % 2 == 0;
                    pixels[column] = pixels[4 + column] = new Color(0f, 0.5f, 0.5f, 1f);
                    pixels[16 + column] = active
                        ? new Color(0f, 1f, 0f, 1f)
                        : new Color(0f, -1f, 0f, -1f);
                    pixels[20 + column] = new Color(0f, 0f, 1f, 1f);
                    pixels[24 + column] = new Color(0f, 0f, 1f, 1f);
                    pixels[28 + column] = new Color(0f, 0f, 0f, 2f);
                }
                texture.SetPixels(pixels);
                texture.Apply();
                material.SetTexture("_LightDistributionTex", texture);
                material.SetFloat("_LightDistributionWidth", 4f);
                material.SetFloat("_LightTimelineDuration", 1f);
                material.SetFloat("_UseLightTimeline", 1f);
                var full = RenderGradientPixel(material, 0.5f, 0.125f).g;
                var shift = direction * 16f;
                mesh.vertices = new[]
                {
                    new Vector3(16f / width, 0), new Vector3(64f / width, 0),
                    new Vector3((64f + shift) / width, 1),
                    new Vector3((16f + shift) / width, 1)
                };
                mesh.uv = new[]
                {
                    new Vector2(0.5f, 0), new Vector2(0.5f, 1),
                    new Vector2(0.5f, 1), new Vector2(0.5f, 0)
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                target.Create();
                RenderTexture.active = target;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadOrtho();
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                GL.PopMatrix();
                read.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                read.Apply();
                var rendered = read.GetPixels();
                var maxError = 0f;
                var worst = "";
                for (var y = 2; y < height - 2; y++)
                {
                    var left = 16f + shift * (y + 0.5f) / height;
                    for (var boundary = 1; boundary < 4; boundary++)
                    {
                        var edge = left + boundary * 12f;
                        var x = Mathf.FloorToInt(edge);
                        var coverage = Mathf.Clamp01(0.5f + (x + 0.5f - edge)
                            / (1f + Mathf.Abs(shift) / height));
                        if (boundary % 2 != 0)
                            coverage = 1f - coverage;
                        var actual = rendered[y * width + x].g / full;
                        var error = Mathf.Abs(actual - coverage);
                        if (error > maxError)
                        {
                            maxError = error;
                            worst = $"({x},{y}), edge {boundary}: {actual} vs {coverage}";
                        }
                    }
                }
                Assert.That(maxError, Is.LessThan(0.025f), worst);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(read);
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(material);
            }
        }
    }
}

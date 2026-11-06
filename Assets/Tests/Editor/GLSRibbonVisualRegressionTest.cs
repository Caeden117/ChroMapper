using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    // These image regressions use Monstercat 2's twenty group-10 lights and a real
    // projective plane; an affine trapezoid changes derivatives across its diagonal.
    public partial class GLSColorTransitionCacheTest
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RandomizedMonstercatRibbonEdgesMatchSupersampling(bool inner)
        {
            VerifyPerspectiveRibbonEdges(new[] { CreateRandomizedMonstercatGroup() },
                20, 225.7f, 227.1f, inner, "beat225-randomized");
        }

        // A shared plane must follow the real grid scale, offset, reflected width,
        // scrolling beat and live Y rotation without shifting its pixel coverage.
        [Test]
        public void TransformedMonstercatRibbonPlaneKeepsPixelCoverage()
        {
            VerifyPerspectiveRibbonEdges(new[] { CreateRandomizedMonstercatGroup() },
                20, 225.7f, 227.1f, true, "beat225-transformed-grid", verifyGridTransform: true);
        }

        // AlternatingMonstercatRibbonEdgesMatchSupersampling retains opposite box
        // palettes while independently rendering the two inner lanes.
        [TestCase(false)]
        [TestCase(true)]
        public void AlternatingMonstercatRibbonEdgesMatchSupersampling(bool inner)
        {
            var group = CreateAlternatingPulseGroup(409f);
            foreach (var node in group["e"][1]["e"].Children)
                node["c"] = 1 - node["c"].AsInt;
            VerifyPerspectiveRibbonEdges(new[] { group }, 20, 410.25f, 410.5f, inner, "beat409-alternating");
        }

        // The bright distributed join crosses separately owned group quads, so
        // both groups must participate in the same perspective render.
        [Test]
        public void DistributedMonstercatGroupJoinMatchesSupersampling()
        {
            VerifyPerspectiveRibbonEdges(new[]
            {
                CreateReportedBrightJoinGroup(202f), CreateReportedBrightJoinGroup(202.75f)
            }, 4, 202.1f, 203.1f, false, "beat202-group-join");
        }

        // The interruption is authored before every distributed light reaches
        // its next local node, which previously exposed pink ownership seams.
        [TestCase(false)]
        [TestCase(true)]
        public void InterruptedMonstercatRibbonEdgesMatchSupersampling(bool inner)
        {
            var group = CreateAlternatingPulseGroup(427f);
            foreach (var node in group["e"][1]["e"].Children)
                node["c"] = 1 - node["c"].AsInt;
            var following = CreateGroup(427.875f, 10, 1,
                filterChunks: 0, brightnessDistribution: 0f);
            following["e"][0]["w"] = 0f;
            following["e"][0]["b"] = 0f;
            following["e"][0]["e"][0]["c"] = 0;
            following["e"][0]["e"][0]["s"] = 0f;
            VerifyPerspectiveRibbonEdges(new[] { group, following }, 20,
                427.25f, 427.75f, inner, "beat427-interrupted");
        }

        // Similar neighboring green colors expose division lines without
        // allowing a large hue contrast to conceal the erroneous darker pixels.
        [TestCase(false)]
        [TestCase(true)]
        public void GreenMonstercatRibbonEdgesMatchSupersampling(bool inner)
        {
            var group = CreateGroup(204.5f, 10, 0, filterChunks: 4, brightnessDistribution: 0f);
            var box = group["e"][0];
            box["b"] = 0;
            box["e"][0]["c"] = 0;
            box["e"][0]["s"] = 0.5f;
            box["e"][0]["customData"] = JSON.Parse(
                "{\"color\":[0,1,0.197],\"strobeColor\":[1,0,0.999]}");
            ((JSONArray)box["e"]).Add(new JSONObject
            {
                ["b"] = 0.5f, ["c"] = 0, ["s"] = 0.7f, ["i"] = 1,
                ["f"] = 0, ["sb"] = 0, ["sf"] = 0,
                ["customData"] = JSON.Parse(
                    "{\"color\":[0,1,0.254],\"strobeColor\":[1,0,0.999],\"colorEasing\":20}")
            });
            VerifyPerspectiveRibbonEdges(new[] { group }, 20,
                204.7f, 205.7f, inner, "beat204-green");
        }

        // A supersampled production render is independent of the low-resolution
        // pixel derivatives and exposes missing edge coverage without pinning HLSL.
        private static void VerifyPerspectiveRibbonEdges(
            JSONNode[] groups, int lightCount, float timeStart, float timeEnd, bool inner, string name,
            bool verifyGridTransform = false)
        {
            var map = LoadMap(CreateDifficultyJson(groups));
            var boxes = map.LightColorEventBoxGroups[0].Boxes;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.RedColor = new Color(1f, 0f, 0.5f);
            appearance.BlueColor = new Color(0.1f, 0.4f, 1f);
            appearance.OffColor = Color.clear;
            var objects = new List<GameObject>();
            var materials = new List<Material>();
            var ribbons = new List<LightGradientController>();
            var failures = new List<string>();
            const int width = 160;
            const int height = 480;
            const int samples = 4;
            try
            {
                for (var lane = 0; lane < (inner ? boxes.Count : 1); lane++)
                {
                    materials.Clear();
                    ribbons.Clear();
                    IEnumerable<BaseLightColorBase> nodes = inner
                        ? boxes[lane].Events
                        // GLSGroupContainer creates one outer preview per relative beat;
                        // drawing both boxes here would composite the aggregate twice.
                        : map.LightColorEventBoxGroups.SelectMany(eventGroup => eventGroup.Boxes
                            .SelectMany(box => box.Events).OrderBy(node => node.RelativeJsonTime)
                            .GroupBy(node => node.RelativeJsonTime).Select(atBeat => atBeat.First()));
                    foreach (var node in nodes)
                    {
                        var owner = new GameObject("Perspective ribbon regression owner");
                        objects.Add(owner);
                        var ribbon = CreateRibbonController(owner, out var renderer);
                        GLSEventCommon.UpdateColorTransitionRibbon(ribbon, node, appearance,
                            _ => false, lightCount, aggregateSameTimeBoxes: !inner);
                        if (ribbon.ColorTimelineDuration <= 0f)
                            continue;
                        var properties = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(properties);
                        materials.Add(CreateWaveSampleMaterial(properties));
                        ribbons.Add(ribbon);
                    }
                    foreach (var roll in new[] { -7f, 4f })
                    {
                        var native = RenderPerspectiveRibbonOwners(materials.ToArray(), ribbons.ToArray(),
                            timeStart, timeEnd, width, height, roll);
                        var high = RenderPerspectiveRibbonOwners(materials.ToArray(), ribbons.ToArray(),
                            timeStart, timeEnd, width * samples, height * samples, roll);
                        var reference = DownsampleRibbonPixels(high, width, height, samples);
                        var stem = $"{name}-{(inner ? "inner" : "outer")}-lane{lane}-roll{roll}";
                        SaveRibbonImage(stem + "-native", native, width, height);
                        SaveRibbonImage(stem + "-reference", reference, width, height);
                        // Check coordinate equivalence separately from the supersampled edge-coverage
                        // comparison below.
                        if (verifyGridTransform)
                        {
                            var transformed = RenderPerspectiveRibbonOwners(materials.ToArray(), ribbons.ToArray(),
                                timeStart, timeEnd, width, height, roll, transformedGrid: true);
                            SaveRibbonImage(stem + "-transformed", transformed, width, height);
                            var coordinateError = 0f;
                            var coordinatePixel = 0;
                            for (var pixel = 0; pixel < native.Length; pixel++)
                            {
                                var difference = Mathf.Max(Mathf.Abs(native[pixel].r - transformed[pixel].r),
                                    Mathf.Abs(native[pixel].g - transformed[pixel].g),
                                    Mathf.Abs(native[pixel].b - transformed[pixel].b));
                                if (difference > coordinateError)
                                {
                                    coordinateError = difference;
                                    coordinatePixel = pixel;
                                }
                            }
                            Assert.That(coordinateError, Is.LessThan(0.003f),
                                $"Grid transform shifted pixel ({coordinatePixel % width}, {coordinatePixel / width}) "
                                + $"at roll {roll}: {native[coordinatePixel]} versus {transformed[coordinatePixel]}.");
                        }
                        var errors = new Color[native.Length];
                        var worst = 0f;
                        var worstIndex = 0;
                        var compared = 0;
                        for (var y = 2; y < height - 2; y++)
                        {
                            for (var x = 2; x < width - 2; x++)
                            {
                                var index = y * width + x;
                                var error = Mathf.Max(Mathf.Abs(native[index].r - reference[index].r),
                                    Mathf.Abs(native[index].g - reference[index].g),
                                    Mathf.Abs(native[index].b - reference[index].b));
                                errors[index] = new Color(error * 8f, 0f, 0f, 1f);
                                // Longitudinal color changes are intentionally nonuniform;
                                // compare side edges only where nearby rows agree in color.
                                var above = reference[index + width * 2];
                                var below = reference[index - width * 2];
                                if (Mathf.Max(Mathf.Abs(above.r - below.r),
                                    Mathf.Abs(above.g - below.g), Mathf.Abs(above.b - below.b)) > 0.03f)
                                {
                                    continue;
                                }
                                // Render targets keep alpha=1 even over black;
                                // only visible RGB counts as a populated fixture.
                                if (Mathf.Max(reference[index].r, reference[index].g, reference[index].b) < 0.01f
                                    && Mathf.Max(native[index].r, native[index].g, native[index].b) < 0.01f)
                                {
                                    continue;
                                }
                                compared++;
                                if (error > worst)
                                {
                                    worst = error;
                                    worstIndex = index;
                                }
                            }
                        }
                        SaveRibbonImage(stem + "-error-x8", errors, width, height);
                        Assert.That(compared, Is.GreaterThan(100), "The perspective fixture must contain visible ribbon pixels.");
                        if (worst > 0.055f)
                        {
                            WritePerspectiveRibbonContributors(stem, materials, ribbons,
                                timeStart, timeEnd, width, height, roll, worstIndex);
                            failures.Add($"{stem}: edge error {worst:F5} at ({worstIndex % width},"
                                + $"{worstIndex / width}); native {native[worstIndex]}, reference {reference[worstIndex]}.");
                        }
                    }
                    foreach (var material in materials)
                        Object.DestroyImmediate(material);
                    materials.Clear();
                }
                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }
            finally
            {
                foreach (var material in materials)
                    Object.DestroyImmediate(material);
                foreach (var owner in objects)
                    Object.DestroyImmediate(owner);
                Object.DestroyImmediate(appearance);
            }
        }

        // Each owner occupies its real beat interval; clipping to the inspected
        // time window preserves its original UV mapping and perspective derivatives.
        private static Color[] RenderPerspectiveRibbonOwners(
            Material[] materials, LightGradientController[] ribbons, float timeStart, float timeEnd,
            int width, int height, float roll, bool transformedGrid = false)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear) { antiAliasing = 1 };
            var readback = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            var cameraObject = new GameObject("Ribbon regression projection");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.aspect = (float)width / height;
            camera.fieldOfView = 43f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(-0.35f, 5f, -2f);
            camera.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 0f, 3f) - camera.transform.position)
                * Quaternion.AngleAxis(roll, Vector3.forward);
            // Move the camera with the grid, including reflection through its
            // view matrix, so identical authored ribbons have identical images.
            var grid = transformedGrid
                ? Matrix4x4.TRS(new Vector3(0f, 0f, 1f), Quaternion.identity, new Vector3(-0.6f, 0.6f, 0.6f))
                : Matrix4x4.identity;
            var gridRotation = transformedGrid ? 37f : 0f;
            var scroll = transformedGrid ? 3.25f : 0f;
            var worldWarp = Matrix4x4.Rotate(Quaternion.Euler(0f, gridRotation, 0f)) * grid
                * Matrix4x4.Translate(new Vector3(0f, 0f, -scroll * 6f / (timeEnd - timeStart)));
            var mesh = new Mesh();
            var previous = RenderTexture.active;
            // GL immediate rendering does not install Camera shader globals;
            // the vertex AA footprint must use this target's actual pixel size.
            var previousScreenParams = Shader.GetGlobalVector("_ScreenParams");
            // Match AudioTimeSyncController's shared scrolling lane clock so
            // differently sized owner quads evaluate identical screen pixels.
            var previousRotation = Shader.GetGlobalFloat("_Rotation");
            var previousSongTime = Shader.GetGlobalVector("_SongBpmTime");
            try
            {
                target.Create();
                Shader.SetGlobalFloat("_Rotation", gridRotation);
                Shader.SetGlobalVector("_SongBpmTime", new Vector4(0, timeStart + scroll, 0, 0));
                RenderTexture.active = target;
                Shader.SetGlobalVector("_ScreenParams", new Vector4(width, height,
                    1f + 1f / width, 1f + 1f / height));
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadProjectionMatrix(camera.projectionMatrix);
                GL.modelview = camera.worldToCameraMatrix * worldWarp.inverse;
                for (var n = 0; n < materials.Length; n++)
                {
                    var ownerStart = ribbons[n].ColorTimelineStart;
                    // Keep events whose ribbons extend into the framing window; removing them changes where
                    // other ribbons end. Keep mesh endpoints too, since clipping would move the shader's one-
                    // pixel fringe.
                    const float startUv = 0f;
                    const float endUv = 1f;
                    // Production uses a unit XY quad, with local X along time and
                    // local Y across lights; the shader's edge fringe uses those axes.
                    mesh.vertices = new[]
                    {
                        new Vector3(startUv - 0.5f, -0.5f, 0f), new Vector3(startUv - 0.5f, 0.5f, 0f),
                        new Vector3(endUv - 0.5f, 0.5f, 0f), new Vector3(endUv - 0.5f, -0.5f, 0f)
                    };
                    mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                    mesh.uv = new[]
                    {
                        new Vector2(startUv, 0f), new Vector2(startUv, 1f),
                        new Vector2(endUv, 1f), new Vector2(endUv, 0f)
                    };
                    var transform = Matrix4x4.identity;
                    transform.SetColumn(0, new Vector4(0f, 0f,
                        6f * ribbons[n].ColorTimelineDuration / (timeEnd - timeStart), 0f));
                    transform.SetColumn(1, new Vector4(1f, 0f, 0f, 0f));
                    transform.SetColumn(2, new Vector4(0f, 1f, 0f, 0f));
                    transform.SetColumn(3, new Vector4(0f, 0f,
                        6f * (ownerStart + ribbons[n].ColorTimelineDuration * 0.5f - timeStart)
                            / (timeEnd - timeStart), 1f));
                    materials[n].SetFloat("_RibbonEdgePadding", 1f);
                    // All independently transformed owners share the same lane
                    // coordinate basis, exactly as the production grid binds it.
                    materials[n].SetFloat("_UseRibbonPlane", 1f);
                    materials[n].SetVector("_RibbonPlaneOrigin", grid.MultiplyPoint3x4(new Vector3(-0.5f, 0, 0)));
                    materials[n].SetVector("_RibbonPlaneTime", grid.MultiplyVector(new Vector3(0, 0, 6f / (timeEnd - timeStart))));
                    materials[n].SetVector("_RibbonPlaneWidth", grid.MultiplyVector(Vector3.right));
                    materials[n].SetPass(0);
                    Graphics.DrawMeshNow(mesh, worldWarp * transform);
                }
                GL.PopMatrix();
                readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readback.Apply(false, false);
                return readback.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                Shader.SetGlobalVector("_ScreenParams", previousScreenParams);
                Shader.SetGlobalFloat("_Rotation", previousRotation);
                Shader.SetGlobalVector("_SongBpmTime", previousSongTime);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(readback);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        // Failed image comparisons retain per-owner evidence so a bright seam can
        // be distinguished from a missing neighbor or temporal overlap.
        private static void WritePerspectiveRibbonContributors(string name,
            List<Material> materials, List<LightGradientController> ribbons,
            float timeStart, float timeEnd, int width, int height, float roll, int pixel)
        {
            var report = new StringBuilder();
            report.AppendLine($"Pixel ({pixel % width}, {pixel / width}), roll {roll}, time {timeStart}..{timeEnd}");
            for (var owner = 0; owner < materials.Count; owner++)
            {
                var rendered = RenderPerspectiveRibbonOwners(new[] { materials[owner] }, new[] { ribbons[owner] },
                    timeStart, timeEnd, width, height, roll);
                var contribution = rendered[pixel];
                if (Mathf.Max(contribution.r, contribution.g, contribution.b) < 0.00001f)
                    continue;
                report.AppendLine($"Owner {owner} start={ribbons[owner].ColorTimelineStart:R} "
                    + $"duration={ribbons[owner].ColorTimelineDuration:R} pixel={contribution}");
                var texture = (Texture2D)materials[owner].GetTexture("_LightDistributionTex");
                for (var light = 0; light < texture.width; light++)
                {
                    report.AppendLine($"  light {light}: times={texture.GetPixel(light, 4)} "
                        + $"flags={texture.GetPixel(light, 7)}"
                        + (texture.height > 9 ? $" header={texture.GetPixel(light, 9)}" : ""));
                }
            }
            var directory = Path.Combine(Application.dataPath, "..", "TestResults", "ribbon-aa");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name + "-contributors.txt"), report.ToString());
        }

        // Downsample the high-resolution render to check pixel coverage without duplicating the shader's
        // anti-aliasing formula.
        private static Color[] DownsampleRibbonPixels(Color[] high, int width, int height, int samples)
        {
            var result = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var sum = Color.clear;
                    for (var sy = 0; sy < samples; sy++)
                    {
                        for (var sx = 0; sx < samples; sx++)
                            sum += high[((y * samples + sy) * width * samples) + x * samples + sx];
                    }
                    result[y * width + x] = sum / (samples * samples);
                }
            }
            return result;
        }

        // Keep the evidence inspectable even on a failing run; PNGs encode the
        // linear render as sRGB so image viewers show the same visible brightness.
        private static void SaveRibbonImage(string name, Color[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                var encoded = new Color[pixels.Length];
                for (var i = 0; i < pixels.Length; i++)
                {
                    encoded[i] = pixels[i].gamma;
                    encoded[i].a = 1f;
                }
                texture.SetPixels(encoded);
                texture.Apply(false, false);
                var directory = Path.Combine(Application.dataPath, "..", "TestResults", "ribbon-aa");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // Preserve every authored event: a shortened fixture changes the outgoing
        // ownership windows and hides the node-2-through-5 regression.
        private static JSONNode CreateRandomizedMonstercatGroup()
        {
            var group = CreateGroup(225.5f, 10, 1, filterChunks: 0, brightnessDistribution: 0f);
            var box = group["e"][0];
            box["f"]["n"] = 2;
            box["f"]["s"] = 1130463232;
            box["w"] = 0.05f;
            box["d"] = 2;
            box["b"] = 0;
            var events = new JSONArray();
            var authored = new[]
            {
                (0f, 1, 0f, 0), (0.25f, 1, 2.3f, 0), (0.5f, 1, 0f, 0),
                (0.75f, 1, 3.2f, 0), (1.25f, 1, 0f, 0), (1.5f, 1, 1.8f, 0),
                (2.25f, 1, 0f, 0), (2.75f, 0, 2.8f, 4), (3.5f, 0, 0f, 0),
                (4f, 1, 0f, 0), (4.25f, 1, 2.3f, 0), (4.5f, 1, 0f, 0),
                (4.75f, 1, 3.2f, 0), (5.25f, 1, 0f, 0), (5.5f, 1, 1.8f, 0),
                (6.25f, 1, 0f, 0), (6.5f, 0, 2.4f, 4), (7.5f, 1, 0f, 0),
                (7.75f, 1, 2.3f, 0), (8f, 1, 0f, 0), (8.25f, 1, 3.2f, 0),
                (8.75f, 1, 0f, 0), (9f, 1, 1.8f, 0), (9.75f, 1, 0f, 0),
                (10.25f, 0, 3.2f, 4), (11f, 0, 0f, 4)
            };
            foreach (var node in authored)
            {
                events.Add(new JSONObject
                {
                    ["b"] = node.Item1, ["c"] = node.Item2, ["s"] = node.Item3,
                    ["i"] = 1, ["f"] = node.Item4, ["sb"] = 0, ["sf"] = 0
                });
            }
            box["e"] = events;
            return group;
        }
    }
}

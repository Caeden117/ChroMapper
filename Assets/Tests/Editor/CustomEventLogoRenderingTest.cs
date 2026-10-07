using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class CustomEventLogoRenderingTest
    {
        private const int ImageSize = 512;

        [TestCase(30f, 0f, false)]
        [TestCase(55f, 17f, false)]
        [TestCase(75f, -23f, false)]
        [TestCase(30f, 0f, true)]
        [TestCase(55f, 17f, true)]
        [TestCase(75f, -23f, true)]
        public void OpaqueLogoInteriorHasNoMissingPixels(float pitch, float yaw, bool includeBox)
        {
            using var scene = new RenderScene(pitch, yaw);
            scene.SetBoxVisible(includeBox);
            if (!includeBox)
            {
                scene.ActualMaterial.SetFloat("_ZTest", (float)CompareFunction.Always);
                scene.ReferenceMaterial.SetFloat("_ZTest", (float)CompareFunction.Always);
            }

            scene.ReferenceMaterial.SetFloat("_CutoutThreshold", 0.5f);
            scene.Logo.sharedMaterial = scene.ActualMaterial;
            var actual = scene.Capture();
            scene.Logo.sharedMaterial = scene.ReferenceMaterial;
            var reference = scene.Capture();
            var stem = $"pitch{pitch}-yaw{yaw}-box{includeBox}";
            scene.SaveImage(stem + "-actual", actual);
            scene.SaveImage(stem + "-reference", reference);
            var opaque = 0;
            var missing = 0;
            for (var y = 2; y < ImageSize - 2; y++)
            {
                for (var x = 2; x < ImageSize - 2; x++)
                {
                    if (!IsWhiteInterior(reference, x, y))
                        continue;

                    opaque++;
                    var pixel = actual[y * ImageSize + x];
                    if (pixel.r < 240 || pixel.g < 240 || pixel.b < 240)
                        missing++;
                }
            }

            TestContext.WriteLine($"{stem}: opaque interior={opaque}, missing={missing}, " +
                $"cutoff={scene.ActualMaterial.GetFloat("_CutoutThreshold")}, " +
                $"texture={scene.Logo.sprite.texture.format}, filter={scene.Logo.sprite.texture.filterMode}, " +
                $"graphics={SystemInfo.graphicsDeviceType}");
            Assert.That(opaque, Is.GreaterThan(100), "The reference must contain a visible white logo interior.");
            Assert.That(missing, Is.Zero, $"{stem}: {missing}/{opaque} opaque logo pixels were discarded.");
            if (includeBox)
            {
                scene.Logo.sharedMaterial = scene.ActualMaterial;
                scene.AimCamera(-pitch, yaw);
                var below = scene.Capture();
                scene.SaveImage(stem + "-below", below);
                var visibleLogo = 0;
                var visibleBox = 0;
                foreach (var pixel in below)
                {
                    if (pixel.r > 240 && pixel.g > 240 && pixel.b > 240)
                        visibleLogo++;

                    if (pixel.r > 10 && pixel.r < 240)
                        visibleBox++;
                }

                Assert.That(visibleBox, Is.GreaterThan(100), "The below-box view must contain the box.");
                Assert.That(visibleLogo, Is.Zero, "The box must still occlude the logo when viewed from below.");
            }
            else
            {
                var leaked = 0;
                for (var i = 0; i < actual.Length; i++)
                {
                    if (reference[i].r == 0 && reference[i].g == 0 && reference[i].b == 0
                        && (actual[i].r > 0 || actual[i].g > 0 || actual[i].b > 0))
                    {
                        leaked++;
                    }
                }

                Assert.That(leaked, Is.Zero, "Transparent logo texels must not draw a rectangular background.");
            }
        }

        [TestCase(false, 30f, 0f, false)]
        [TestCase(false, 55f, 17f, false)]
        [TestCase(true, 30f, 0f, false)]
        [TestCase(true, 55f, 17f, false)]
        [TestCase(false, 30f, 0f, true)]
        [TestCase(false, 55f, 17f, true)]
        [TestCase(true, 30f, 0f, true)]
        [TestCase(true, 55f, 17f, true)]
        public void XYGridOverlaysOnlyTheRearHalfOfNodeDecoration(bool useText, float pitch, float yaw, bool includeBoxAndLines)
        {
            using var scene = new RenderScene(pitch, yaw);
            scene.SetBoxVisible(includeBoxAndLines);
            var tint = new Color(0.25f, 0.4f, 0.6f, 1f);
            if (useText)
                scene.AddNodeText(tint);
            else
                scene.Logo.color = tint;

            var grid = scene.AddGrid();
            grid.Grid.enabled = includeBoxAndLines;
            grid.gameObject.SetActive(false);
            var decorationOnly = scene.Capture();
            scene.Node.SetActive(false);
            grid.gameObject.SetActive(true);
            var gridOnly = scene.Capture();
            scene.Node.SetActive(true);
            var combined = scene.Capture();
            var stem = $"grid-{(useText ? "text" : "logo")}-pitch{pitch}-yaw{yaw}-boxAndLines{includeBoxAndLines}";
            scene.SaveImage(stem + "-decoration", decorationOnly);
            scene.SaveImage(stem + "-grid", gridOnly);
            scene.SaveImage(stem + "-combined", combined);
            var brightest = Vector3.zero;
            foreach (var pixel in decorationOnly)
                brightest = Vector3.Max(brightest, new Vector3(pixel.r, pixel.g, pixel.b));

            TestContext.WriteLine($"{stem}: brightest decoration RGB={brightest}");
            var decorationPlane = new Plane(Vector3.up, scene.Logo.transform.position);
            var front = 0;
            var rear = 0;
            var wrongFront = 0;
            var wrongRear = 0;
            for (var y = 0; y < ImageSize; y++)
            {
                for (var x = 0; x < ImageSize; x++)
                {
                    var index = y * ImageSize + x;
                    var pixel = decorationOnly[index];
                    if (pixel.b < 40 || pixel.b < pixel.r * 1.5f || pixel.g < pixel.r * 1.2f)
                        continue;

                    var ray = scene.Camera.ViewportPointToRay(new Vector3(
                        (x + 0.5f) / ImageSize, (y + 0.5f) / ImageSize, 0f));
                    if (!decorationPlane.Raycast(ray, out var distance))
                        continue;

                    var point = ray.GetPoint(distance);
                    if (Mathf.Abs(point.z) < 0.02f)
                        continue;

                    var expected = (Color)pixel;
                    if (point.z > 0f)
                    {
                        rear++;
                        var overlay = (Color)gridOnly[index];
                        Assert.That(overlay.maxColorComponent, Is.GreaterThan(0.05f),
                            "The grid must visibly overlap the rear decoration samples.");
                        expected = new Color(
                            overlay.r + expected.r * (1f - overlay.r),
                            overlay.g + expected.g * (1f - overlay.g),
                            overlay.b + expected.b * (1f - overlay.b));
                    }
                    else
                    {
                        front++;
                    }

                    var rendered = (Color)combined[index];
                    var error = Mathf.Max(Mathf.Abs(rendered.r - expected.r),
                        Mathf.Abs(rendered.g - expected.g), Mathf.Abs(rendered.b - expected.b));
                    if (error <= 0.015f)
                        continue;

                    if (point.z > 0f)
                        wrongRear++;
                    else
                        wrongFront++;
                }
            }

            TestContext.WriteLine($"{stem}: front={front}, rear={rear}, wrongFront={wrongFront}, wrongRear={wrongRear}");
            Assert.That(front, Is.GreaterThan(100), "The decoration must extend in front of the grid.");
            Assert.That(rear, Is.GreaterThan(100), "The decoration must extend behind the grid.");
            Assert.That(wrongRear, Is.Zero, $"{stem}: the grid failed to overlay {wrongRear}/{rear} rear pixels.");
            Assert.That(wrongFront, Is.Zero, $"{stem}: the grid incorrectly overlaid {wrongFront}/{front} front pixels.");
        }

        [Test]
        public void NodeTextEdgesRetainBlendedCoverage()
        {
            using var scene = new RenderScene(55f, 17f);
            scene.AddNodeText(Color.white);
            var blackBackground = scene.Capture();
            scene.Camera.backgroundColor = Color.white;
            var whiteBackground = scene.Capture();
            var fractional = 0;
            for (var i = 0; i < blackBackground.Length; i++)
            {
                var coverage = 1f - (whiteBackground[i].r - blackBackground[i].r) / 255f;
                if (coverage > 0.05f && coverage < 0.95f)
                    fractional++;
            }

            Assert.That(fractional, Is.GreaterThan(10), "Node text must retain blended SDF edges rather than binary cutouts.");
        }

        private static bool IsWhiteInterior(Color32[] pixels, int x, int y)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    var pixel = pixels[(y + dy) * ImageSize + x + dx];
                    if (pixel.r < 250 || pixel.g < 250 || pixel.b < 250)
                        return false;
                }
            }

            return true;
        }

        private sealed class RenderScene : IDisposable
        {
            private readonly GameObject root = new("Node decoration render test");
            private readonly RenderTexture target;
            private readonly Texture2D readback;
            private readonly RenderTexture previousActive;

            public GameObject Node { get; }
            public SpriteRenderer Logo { get; }
            public Camera Camera { get; }
            public Material ActualMaterial { get; }
            public Material ReferenceMaterial { get; }

            public RenderScene(float pitch, float yaw)
            {
                previousActive = RenderTexture.active;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Prefabs/MapEditor/Beatmap/Custom Event.prefab");
                Node = Object.Instantiate(prefab, root.transform);
                Node.transform.localScale = Vector3.one * 0.75f;
                Node.transform.position = new Vector3(0f, 0.5f, 0f);
                SetLayer(Node);
                SetBoxVisible(false);
                Logo = Node.GetComponentInChildren<SpriteRenderer>();
                ActualMaterial = new Material(Logo.sharedMaterial);
                ReferenceMaterial = new Material(Logo.sharedMaterial);
                Logo.sharedMaterial = ActualMaterial;
                var cameraObject = new GameObject("Node decoration test camera");
                cameraObject.transform.SetParent(root.transform);
                Camera = cameraObject.AddComponent<Camera>();
                Camera.enabled = false;
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.backgroundColor = Color.black;
                Camera.cullingMask = 1 << 31;
                Camera.nearClipPlane = 0.3f;
                Camera.farClipPlane = 5000f;
                Camera.fieldOfView = 60f;
                Camera.allowHDR = false;
                Camera.allowMSAA = false;
                AimCamera(pitch, yaw);
                target = new RenderTexture(ImageSize, ImageSize, 24, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
                readback = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBA32, false, true);
                Camera.targetTexture = target;
                target.Create();
            }

            public void AimCamera(float pitch, float yaw)
            {
                Camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                Camera.transform.position = Logo.transform.position - Camera.transform.forward * 1.6f;
            }

            public void SetBoxVisible(bool visible)
            {
                foreach (var renderer in Node.GetComponentsInChildren<MeshRenderer>())
                    renderer.enabled = visible && renderer.name == "CM_Block";
            }

            public void AddNodeText(Color tint)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Prefabs/MapEditor/Beatmap/GLS Event.prefab");
                var source = prefab.transform.Find("TextTop");
                var textObject = Object.Instantiate(source.gameObject, Logo.transform.parent);
                var text = textObject.GetComponent<TextMeshPro>();
                text.enabled = true;
                text.text = "888";
                text.color = tint;
                text.alignment = TextAlignmentOptions.Center;
                text.ForceMeshUpdate();
                text.transform.position = Logo.transform.position - text.transform.TransformVector(text.mesh.bounds.center);
                SetLayer(textObject);
                Logo.enabled = false;
            }

            public GridPlane AddGrid()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/MapEditor/Grid XY.prefab");
                var gridObject = Object.Instantiate(prefab, root.transform);
                gridObject.transform.position = new Vector3(0f, 0.5f, 0f);
                gridObject.transform.localScale = new Vector3(2f, 1.2f, 1f);
                var grid = gridObject.GetComponent<GridPlane>();
                grid.SetInterfaceColor(new Color(0.2f, 0.6f, 0.9f, 0.5f));
                grid.RefreshVisual();
                grid.Grid.enabled = false;
                SetLayer(gridObject);
                return grid;
            }

            public Color32[] Capture()
            {
                Camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0);
                readback.Apply(false);
                return readback.GetPixels32();
            }

            public void SaveImage(string name, Color32[] pixels)
            {
                var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", nameof(CustomEventLogoRenderingTest));
                Directory.CreateDirectory(directory);
                var encoded = (Color32[])pixels.Clone();
                for (var i = 0; i < encoded.Length; i++)
                    encoded[i].a = 255;

                readback.SetPixels32(encoded);
                readback.Apply(false);
                File.WriteAllBytes(PathUtils.Combine(directory, name + ".png"), readback.EncodeToPNG());
            }

            public void Dispose()
            {
                RenderTexture.active = previousActive;
                Camera.targetTexture = null;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(ActualMaterial);
                Object.DestroyImmediate(ReferenceMaterial);
                Object.DestroyImmediate(readback);
                target.Release();
                Object.DestroyImmediate(target);
            }

            private static void SetLayer(GameObject go)
            {
                foreach (var child in go.GetComponentsInChildren<Transform>())
                    child.gameObject.layer = 31;
            }
        }
    }
}

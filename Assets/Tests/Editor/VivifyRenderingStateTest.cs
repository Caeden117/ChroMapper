using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class VivifyRenderingStateTest
    {
        [Test]
        public void TimedBlitsEndAtTheirBoundaryAndPastSnapshotsStayConsumedOnResume()
        {
            using var rendering = new VivifyRenderingManager(null, null, new ObjectPropertyManager());
            var timed = VivifyTestAssets.State("Blit", "{\"duration\":2}", 1, 2);
            rendering.Sample(1, true);
            rendering.AddBlit(timed);
            Assert.That(rendering.HasBlits(VivifyRenderOrder.AfterMainEffect), Is.True);
            rendering.Sample(2.5f, true);
            Assert.That(rendering.HasBlits(VivifyRenderOrder.AfterMainEffect), Is.True);
            rendering.Sample(3, true);
            Assert.That(rendering.HasBlits(VivifyRenderOrder.AfterMainEffect), Is.False);
            Assert.That(PendingBlits(rendering), Is.Zero, "Expired blits must leave the per-frame work list.");
            rendering.Sample(10, false);
            rendering.AddBlit(timed);
            rendering.AddBlit(VivifyTestAssets.State("Blit", "{}", 1));
            rendering.Sample(10, true);
            Assert.That(rendering.HasBlits(VivifyRenderOrder.AfterMainEffect), Is.False);
            Assert.That(PendingBlits(rendering), Is.Zero);
        }

        [UnityTest]
        public IEnumerator CapturedFrameSurvivesPausedReconstructionAndDisposalRestoresTheGlobal()
        {
            const string id = "_VivifyTestFrozenFrame";
            var original = Shader.GetGlobalTexture(id);
            using var assets = new VivifyTestAssets();
            var baseline = assets.Own(new Texture2D(1, 1));
            var output = assets.Own(new RenderTexture(8, 8, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
            output.Create();
            var root = assets.Own(new GameObject("Vivify capture camera"));
            var camera = root.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.red;
            camera.targetTexture = output;
            var rendering = new VivifyRenderingManager(null, null, new ObjectPropertyManager());
            RenderTexture captured = null;
            try
            {
                Shader.SetGlobalTexture(id, baseline);
                rendering.AssignToCamera(camera);
                var declaration = JSON.Parse("{\"id\":\"" + id + "\",\"width\":8,\"height\":8,\"colorFormat\":\"ARGB32\"}");
                rendering.CreateScreenTexture(declaration);
                rendering.Sample(1, true);
                rendering.AddBlit(VivifyTestAssets.State("Blit",
                    "{\"destination\":\"" + id + "\",\"order\":\"AfterOpaque\"}", 1));
                camera.Render();
                Assert.That(rendering.TryGetTexture(id, out captured), Is.True);
                AssertRed(captured);
                Assert.That(rendering.HasBlits(VivifyRenderOrder.AfterOpaque), Is.False,
                    "A successfully captured one-frame blit must retire.");
                Assert.That(PendingBlits(rendering), Is.Zero);
                camera.backgroundColor = Color.blue;
                rendering.Sample(2, true);
                camera.Render();
                AssertRed(captured);
                rendering.Sample(10, false);
                rendering.Reset(true);
                rendering.CreateScreenTexture(declaration);
                camera.Render();
                Assert.That(rendering.TryGetTexture(id, out var retained), Is.True);
                Assert.That(retained, Is.SameAs(captured));
                AssertRed(retained);
                rendering.Dispose();
                rendering = null;
                Assert.That(Shader.GetGlobalTexture(id), Is.SameAs(baseline));
                Assert.That(camera.GetCommandBuffers(UnityEngine.Rendering.CameraEvent.AfterForwardOpaque), Is.Empty);
            }
            finally
            {
                if (rendering != null) rendering.Dispose();
                camera.targetTexture = null;
                Shader.SetGlobalTexture(id, original);
            }
            yield return null;
            Assert.That(captured == null, Is.True, "Disposal must release the retained texture after deferred destruction.");
        }

        private static void AssertRed(RenderTexture source)
        {
            var previous = RenderTexture.active;
            var readback = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            try
            {
                RenderTexture.active = source;
                readback.ReadPixels(new Rect(3, 3, 1, 1), 0, 0);
                readback.Apply();
                var color = readback.GetPixel(0, 0);
                Assert.That(color.r, Is.GreaterThan(.95f));
                Assert.That(color.g, Is.LessThan(.05f));
                Assert.That(color.b, Is.LessThan(.05f));
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
            }
        }

        private static int PendingBlits(VivifyRenderingManager rendering) =>
            ((ICollection)typeof(VivifyRenderingManager).GetField("blits", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(rendering)).Count;
    }
}

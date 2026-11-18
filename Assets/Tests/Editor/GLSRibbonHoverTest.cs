using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Containers;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Outgoing ribbon edits target the next node for the light under the cursor. Held tails and the first
    // node's absent incoming ribbon have no edit target.
    public class GLSRibbonHoverTest : GLSColorPlaybackTestBase
    {
        // One source now has different destinations across its width; hover must follow the visible physical strip, not light zero.
        [TestCase(0, 5f, true)]
        [TestCase(1, 5f, true)]
        [TestCase(7, 23.6f, true)]
        [TestCase(0, 20f, false)]
        public void RibbonHoverResolvesThePhysicalLightDestination(int light, float beat, bool hasTarget)
        {
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            var owner = (GLSGroupContainer)collection.CreateContainer();
            try
            {
                owner.ObjectData = map.LightColorEventBoxGroups[0];
                owner.Setup();
                owner.PreviewEventData = Node(0);
                var ribbon = owner.lightGradientController;
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, Node(0), appearance, _ => false, LightCount);
                var renderer = (MeshRenderer)typeof(LightGradientController)
                    .GetField("meshRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                var duration = ribbon.transform.localScale.x / (EditorScaleController.EditorScale * (4f / 3f));
                SimulateRibbonHit(renderer, new Vector2(
                    (SongTime(beat) - Node(0).SongBpmTime) / duration,
                    (LightCount - light - 0.5f) / LightCount));
                Assert.AreEqual(hasTarget, GLSEventCommon.TryGetColorTransitionTarget(owner, Node(0), out var target),
                    "Masked portions of a ribbon must not resolve an endpoint for hover mutation.");
                if (hasTarget)
                    Assert.AreSame(light % 2 == 0 ? Node(1) : Node(1, 1), target,
                        "Hover must resolve the endpoint of the hit physical strip.");
            }
            finally
            {
                BeatmapRaycastCache.Invalidate();
                Object.DestroyImmediate(owner.gameObject);
            }
        }

        // The first node's outer body no longer projects a sentinel head, so no incoming strip
        // exists to hover and no owner may be resolved through it.
        [Test]
        public void FirstNodeOuterIncomingRibbonIsInactiveAndHasNoHoverTarget()
        {
            LoadAlternatingChunks();
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            var owner = (GLSGroupContainer)collection.CreateContainer();
            try
            {
                owner.ObjectData = map.LightColorEventBoxGroups[0];
                owner.Setup();
                owner.PreviewEventData = Node(0);
                var ribbon = owner.IncomingLightGradientController;
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    ribbon, Node(0), appearance, _ => false, LightCount, aggregateSameTimeBoxes: true);
                Assert.IsFalse(ribbon.gameObject.activeSelf,
                    "With no sentinel-owned span, the outer incoming ribbon must not render for a first node.");
                BeatmapRaycastCache.Invalidate();
                Assert.IsFalse(GLSEventCommon.TryGetColorTransitionTarget(owner, Node(0), out _),
                    "Hover must not resolve a target through a strip that does not render.");
            }
            finally
            {
                BeatmapRaycastCache.Invalidate();
                Object.DestroyImmediate(owner.gameObject);
            }
        }

        // A held tail still renders but owns no destination: hover must not resolve a mutation target through it.
        [Test]
        public void LitTailHoverHasNoTransitionTarget()
        {
            var collection = Object.FindAnyObjectByType<GLSGroupColorGridContainer>();
            var owner = (GLSGroupContainer)collection.CreateContainer();
            try
            {
                owner.ObjectData = map.LightColorEventBoxGroups[2];
                owner.Setup();
                owner.PreviewEventData = Node(2);
                var ribbon = owner.lightGradientController;
                GLSEventCommon.UpdateColorTransitionRibbon(
                    ribbon, Node(2), appearance, _ => false, LightCount, aggregateSameTimeBoxes: true);
                Assert.IsTrue(ribbon.gameObject.activeSelf, "The lit tail must still render.");
                var renderer = (MeshRenderer)typeof(LightGradientController)
                    .GetField("meshRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                SimulateRibbonHit(renderer, new Vector2(
                    (SongTime(80f) - ribbon.ColorTimelineStart) / ribbon.ColorTimelineDuration,
                    0.5f / LightCount));
                Assert.IsFalse(GLSEventCommon.TryGetColorTransitionTarget(owner, Node(2), out _),
                    "A held tail has no destination node, so hover chords must not resolve one.");
            }
            finally
            {
                BeatmapRaycastCache.Invalidate();
                Object.DestroyImmediate(owner.gameObject);
            }
        }

        // Ribbon quads have affine UVs; one triangle supplies an exact world-space point for the target UV,
        // which is fed through the shared raycast cache the same way a real cursor hit would be.
        private static void SimulateRibbonHit(MeshRenderer renderer, Vector2 targetUv)
        {
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            var triangles = mesh.triangles;
            var a = triangles[0];
            var b = triangles[1];
            var c = triangles[2];
            var first = uv[b] - uv[a];
            var second = uv[c] - uv[a];
            var delta = targetUv - uv[a];
            var determinant = (first.x * second.y) - (second.x * first.y);
            var weightB = ((delta.x * second.y) - (delta.y * second.x)) / determinant;
            var weightC = ((first.x * delta.y) - (first.y * delta.x)) / determinant;
            var point = renderer.transform.TransformPoint(vertices[a]
                + ((vertices[b] - vertices[a]) * weightB) + ((vertices[c] - vertices[a]) * weightC));
            BeatmapRaycastCache.Invalidate();
            BeatmapRaycastCache.FirstHit = renderer.gameObject;
            BeatmapRaycastCache.HasHit = true;
            BeatmapRaycastCache.HasRaycastThisFrame = true;
            typeof(BeatmapRaycastCache).GetField("FirstHitPoint")?.SetValue(null, point);
        }
    }
}

using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Shared;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public partial class GLSColorTransitionCacheTest
    {
        // The canonical frame must match the shipped quad hierarchy, rather than
        // only a synthetic test plane which can repeat a shader's wrong assumptions.
        [TestCase("GLS Event", false, 0.6f, 4f)]
        [TestCase("GLS Group", false, 0.6f, 3.25f)]
        [TestCase("GLS Group", true, 0.85f, 5.5f)]
        public void PrefabRibbonPlaneMatchesScrolledRotatedGrid(
            string prefabName, bool ghostParent, float laneScale, float editorScale)
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(427f, 10, 0),
                CreateGroup(427.875f, 10, 1),
                CreateGroup(430.25f, 10, 1)));
            var appearance = CreateWaveAppearance();
            var oldEditorScale = EditorScaleController.EditorScale;
            var root = new GameObject("Ribbon plane binding fixture");
            // Keep unrelated icon, raycast and pooled-container callbacks inactive;
            // the serialized renderer transforms and production binding still run.
            root.SetActive(false);
            try
            {
                EditorScaleController.EditorScale = editorScale;
                root.transform.localRotation = Quaternion.Euler(0f, 37.5f, 0f);
                var laneObject = new GameObject("Grid lane");
                laneObject.transform.SetParent(root.transform, false);
                var lane = laneObject.AddComponent<GridLane>();
                lane.Controller = Object.FindAnyObjectByType<GridViewController>();
                lane.transform.localPosition = new Vector3(3.7f, 0.55f, 1f);
                lane.transform.localScale = Vector3.one * laneScale;
                var track = new GameObject("Scrolling track").transform;
                track.SetParent(lane.transform, false);
                var scrollBeat = map.LightColorEventBoxGroups[0].Boxes[0].Events[0].SongBpmTime - 0.37f;
                track.localPosition = new Vector3(0f, 0f, -scrollBeat * editorScale);
                var nodeParent = track;
                if (ghostParent)
                {
                    nodeParent = new GameObject("Preview ghost root").transform;
                    nodeParent.SetParent(track, false);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Prefabs/MapEditor/Beatmap/{prefabName}.prefab");
                Assert.That(prefab, Is.Not.Null);
                var ribbons = new LightGradientController[2];
                var renderers = new MeshRenderer[2];
                var initialFrames = new Vector4[2, 3];
                for (var ownerIndex = 0; ownerIndex < 2; ownerIndex++)
                {
                    var node = Object.Instantiate(prefab, nodeParent, false);
                    var source = map.LightColorEventBoxGroups[ownerIndex].Boxes[0].Events[0];
                    node.transform.localPosition = new Vector3(1.5f,
                        BeatmapConstant.EventNodeGroundedCenterY, source.SongBpmTime * editorScale);
                    node.transform.localScale = Vector3.one * EventAppearanceSO.FinalNodeScale;
                    var ribbon = node.GetComponentInChildren<LightGradientController>(true);
                    // Interaction registration is outside this geometry regression;
                    // these isolated prefabs have no scene-owned ObjectData binding.
                    typeof(LightGradientController).GetField("interactionOwner",
                        BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ribbon, null);
                    var renderer = (MeshRenderer)typeof(LightGradientController).GetField("meshRenderer",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
                    ribbon.BindRibbonLane(lane, node.transform, track);
                    GLSEventCommon.UpdateColorTransitionRibbon(
                        ribbon, source, appearance, _ => false, 20, aggregateSameTimeBoxes: true);
                    Assert.That(ribbon.ColorTimelineDuration, Is.GreaterThan(0f));
                    ribbons[ownerIndex] = ribbon;
                    renderers[ownerIndex] = renderer;
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    Assert.That(properties.GetFloat("_UseRibbonPlane"), Is.EqualTo(1f));
                    initialFrames[ownerIndex, 0] = properties.GetVector("_RibbonPlaneOrigin");
                    initialFrames[ownerIndex, 1] = properties.GetVector("_RibbonPlaneTime");
                    initialFrames[ownerIndex, 2] = properties.GetVector("_RibbonPlaneWidth");
                    AssertPrefabRibbonPlaneMatchesMesh(ribbon, renderer, root.transform, scrollBeat);
                }

                for (var axis = 0; axis < 3; axis++)
                {
                    Assert.That(initialFrames[1, axis], Is.EqualTo(initialFrames[0, axis]),
                        "Different owner beats and durations must bind exactly the same lane coordinates.");
                }

                // A lane-layout refresh must move the plane with the existing mesh,
                // without reloading the timeline or depending on the old lane origin.
                lane.transform.localPosition += new Vector3(-2.25f, 0.15f, 0.5f);
                for (var ownerIndex = 0; ownerIndex < 2; ownerIndex++)
                {
                    ribbons[ownerIndex].RefreshRibbonPlane();
                    AssertPrefabRibbonPlaneMatchesMesh(ribbons[ownerIndex], renderers[ownerIndex],
                        root.transform, scrollBeat);
                }

                // Pooled basic-event gradients must stop using a former GLS frame
                // and its geometry fringe when the renderer changes modes.
                ribbons[0].UpdateGradientData(new ChromaLightGradient(Color.red, Color.blue));
                var legacyProperties = new MaterialPropertyBlock();
                renderers[0].GetPropertyBlock(legacyProperties);
                Assert.That(legacyProperties.GetFloat("_UseRibbonPlane"), Is.Zero);
                Assert.That(legacyProperties.GetFloat("_RibbonEdgePadding"), Is.Zero);
            }
            finally
            {
                EditorScaleController.EditorScale = oldEditorScale;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(appearance);
            }
        }

        // Compare actual imported mesh points with the material's affine lane frame;
        // this covers prefab orientation and offsets independently of frame assembly.
        private static void AssertPrefabRibbonPlaneMatchesMesh(
            LightGradientController ribbon, MeshRenderer renderer, Transform gridRoot, float scrollBeat)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            var origin = (Vector3)properties.GetVector("_RibbonPlaneOrigin");
            var timeAxis = (Vector3)properties.GetVector("_RibbonPlaneTime");
            var widthAxis = (Vector3)properties.GetVector("_RibbonPlaneWidth");
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            for (var vertex = 0; vertex < vertices.Length; vertex++)
            {
                var beat = ribbon.ColorTimelineStart + (ribbon.ColorTimelineDuration * uv[vertex].x);
                var canonical = gridRoot.TransformPoint(origin + ((beat - scrollBeat) * timeAxis)
                    + (uv[vertex].y * widthAxis));
                var actual = renderer.transform.TransformPoint(vertices[vertex]);
                Assert.That(Vector3.Distance(canonical, actual), Is.LessThan(0.0005f),
                    $"Prefab vertex {vertex} must share the shader's lane plane: {canonical:R} versus {actual:R}.");
            }
        }
    }
}

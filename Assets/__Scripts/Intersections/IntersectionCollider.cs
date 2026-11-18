using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     A custom collider that makes use of the fast <see cref="Intersections" /> algorithms.
/// </summary>
public class IntersectionCollider : MonoBehaviour
{
    // Ribbon meshes can change without replacing the Mesh object. Reuse readback lists, but keep each
    // collider's transformed vertices separate and reread the mesh when the collider refreshes.
    private static readonly List<Vector3> vertexReadback = new();
    private static readonly List<int> triangleReadback = new();

    /// <summary>
    ///     The collider mesh. A more detailed mesh results in less performance.
    /// </summary>
    [Tooltip("The collider mesh. A more detailed mesh results in less performance.")]
    public Mesh Mesh;

    public Vector3 Center = Vector3.zero;
    public Vector3 Size = Vector3.one;

    /// <summary>
    ///     A cached array of triangles from the <see cref="Mesh" />.
    /// </summary>
    [NonSerialized] public int[] MeshTriangles;

    /// <summary>
    ///     A cached array of vertices from the <see cref="Mesh" />.
    /// </summary>
    [NonSerialized] public Vector3[] MeshVertices;

    /// <summary>
    ///     Bounding box of the collider in local space.
    /// </summary>
    [NonSerialized] public Bounds CollisionBounds;

    /// <summary>
    ///     The cached layers that the collider is on.
    /// </summary>
    [NonSerialized] public int CollisionLayer;

    /// <summary>
    ///     The group the collider is in
    /// </summary>
    [NonSerialized] public List<int> CollisionGroups = new List<int> { 0 };

    private void OnEnable() => RefreshMeshData();

    private void OnDisable() => Intersections.UnregisterColliderFromGroups(this);

    private void OnDestroy() => Intersections.UnregisterColliderFromGroups(this);

    //private void OnValidate() => RefreshMeshData();

    private void OnDrawGizmosSelected()
    {
        if (Mesh == null) return;

        var center = transform.TransformPoint(Center);

        Gizmos.color = Color.green;

        var modifiedScale = default(Vector3);
        modifiedScale.x = transform.lossyScale.x * Size.x;
        modifiedScale.y = transform.lossyScale.y * Size.y;
        modifiedScale.z = transform.lossyScale.z * Size.z;

        Gizmos.DrawWireMesh(Mesh, center, transform.rotation, modifiedScale);

        var bounds = Mesh.bounds;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center + center, bounds.size);
    }

    /// <summary>
    ///     Unregisters the collider from the Intersections system, refreshes Mesh information, then re-registers the collider.
    /// </summary>
    public void HardRefresh()
    {
        var wasRegistered = Intersections.UnregisterColliderFromGroups(this);
        RefreshMeshData(wasRegistered);
    }

    private void RefreshMeshData(bool register = true)
    {
        if (Mesh == null) return;

        CollisionLayer = gameObject.layer;
        Mesh.GetVertices(vertexReadback);
        if (MeshVertices == null || MeshVertices.Length != vertexReadback.Count)
            Array.Resize(ref MeshVertices, vertexReadback.Count);

        var triangleCount = 0;
        for (var submesh = 0; submesh < Mesh.subMeshCount; submesh++)
            triangleCount += (int)Mesh.GetIndexCount(submesh);

        if (MeshTriangles == null || MeshTriangles.Length != triangleCount)
            Array.Resize(ref MeshTriangles, triangleCount);

        var triangleOffset = 0;
        for (var submesh = 0; submesh < Mesh.subMeshCount; submesh++)
        {
            Mesh.GetTriangles(triangleReadback, submesh);
            triangleReadback.CopyTo(0, MeshTriangles, triangleOffset, triangleReadback.Count);
            triangleOffset += triangleReadback.Count;
        }

        CollisionBounds = new();

        for (var i = 0; i < MeshVertices.Length; i++)
        {
            MeshVertices[i] = Vector3.Scale(vertexReadback[i] + Center, Size);

            CollisionBounds.Encapsulate(MeshVertices[i]);
        }

        if (CollisionGroups == null || CollisionGroups.Count == 0) CollisionGroups = new List<int> { 0 };

        if (register) Intersections.RegisterColliderToGroups(this);
    }
}

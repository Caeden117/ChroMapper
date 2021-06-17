using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A custom collider that makes use of the fast <see cref="Intersections"/> algorithms.
/// </summary>
public class IntersectionCollider : MonoBehaviour
{
    /// <summary>
    /// The collider mesh. A more detailed mesh results in less performance.
    /// </summary>
    [Tooltip("The collider mesh. A more detailed mesh results in less performance.")]
    public Mesh Mesh;

    /// <summary>
    /// A renderer on the object that acts as world-space bounds.
    /// </summary>
    [Tooltip("A renderer on the object that actas as world-space bounds.")]
    public Renderer BoundsRenderer;

    public Vector3 Center = Vector3.zero;
    public Vector3 Size = Vector3.one;

    /// <summary>
    /// A cached array of triangles from the <see cref="Mesh"/>.
    /// </summary>
    [HideInInspector] public int[] MeshTriangles;
    /// <summary>
    /// A cached array of vertices from the <see cref="Mesh"/>.
    /// </summary>
    [HideInInspector] public Vector3[] MeshVertices;
    /// <summary>
    /// The cached layers that the collider is on.
    /// </summary>
    [HideInInspector] public int CollisionLayer;
    /// <summary>
    /// The group the collider is in
    /// </summary>
    [HideInInspector] public List<int> CollisionGroups = new List<int>() { 0 };

    /// <summary>
    /// Unregisters the collider from the Intersections system, refreshes Mesh information, then re-registers the collider.
    /// </summary>
    private void RefreshMeshData()
    {
        if (Mesh == null) return;

        CollisionLayer = gameObject.layer;
        MeshTriangles = Mesh.triangles;
        MeshVertices = Mesh.vertices;
        
        for (int i = 0; i < MeshVertices.Length; i++)
        {
            MeshVertices[i].x = (MeshVertices[i].x + Center.x) * Size.x;
            MeshVertices[i].y = (MeshVertices[i].y + Center.y) * Size.y;
            MeshVertices[i].z = (MeshVertices[i].z + Center.z) * Size.z;
        }

        if (CollisionGroups == null || CollisionGroups.Count == 0)
        {
            CollisionGroups = new List<int>() { 0 };
        }

        Intersections.RegisterColliderToGroups(this);
    }

    private void OnEnable() => RefreshMeshData();

    private void OnDisable() => Intersections.UnregisterColliderFromGroups(this);

    private void OnDestroy() => Intersections.UnregisterColliderFromGroups(this);

    //private void OnValidate() => RefreshMeshData();

    private void OnDrawGizmosSelected()
    {
        if (Mesh == null) return;

        Gizmos.color = Color.green;

        var modifiedScale = default(Vector3);
        modifiedScale.x = transform.lossyScale.x * Size.x;
        modifiedScale.y = transform.lossyScale.y * Size.y;
        modifiedScale.z = transform.lossyScale.z * Size.z;

        Gizmos.DrawWireMesh(Mesh, transform.TransformPoint(Center), transform.rotation, modifiedScale);

        if (BoundsRenderer == null) return;

        var bounds = BoundsRenderer.bounds;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}

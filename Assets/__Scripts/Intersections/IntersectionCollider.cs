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
    /// Unregisters the collider from the Intersections system, refreshes Mesh information, then re-registers the collider.
    /// </summary>
    public void RefreshMeshData()
    {
        Intersections.UnregisterCollider(this);
        CollisionLayer = gameObject.layer;
        MeshTriangles = Mesh.triangles;
        MeshVertices = Mesh.vertices;
        Intersections.RegisterCollider(this);
    }

    private void OnEnable() => RefreshMeshData();

    private void OnDisable() => Intersections.UnregisterCollider(this);

    private void OnDrawGizmosSelected()
    {
        if (Mesh == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireMesh(Mesh, transform.position, transform.rotation, transform.lossyScale);

        if (BoundsRenderer == null) return;

        var bounds = BoundsRenderer.bounds;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
    private const float INTERSECTION_EPSILON = 0.0001f;

    private static List<IntersectionCollider>[] colliders = new List<IntersectionCollider>[32];

    static Intersections()
    {
        for (int i = 0; i < 32; i++)
        {
            colliders[i] = new List<IntersectionCollider>();
        }
    }

    /// <summary>
    /// Registers a custom collider to the system.
    /// </summary>
    public static void RegisterCollider(IntersectionCollider collider)
    {
        colliders[collider.CollisionLayer].Add(collider);
    }

    /// <summary>
    /// Unregisters a custom collider from the system.
    /// </summary>
    public static void UnregisterCollider(IntersectionCollider collider) => colliders[collider.CollisionLayer].Remove(collider);

    /// <summary>
    /// Clears the internal colliders list.
    /// </summary>
    public static void Clear()
    {
        foreach (var colliderList in colliders)
        {
            colliderList.Clear();
        }
    }

    /// <summary>
    /// Cleans up the colliders list of all null or disabled colliders.
    /// </summary>
    public static void Cleanup()
    {
        foreach (var colliderList in colliders)
        {
            colliderList.RemoveAll(x => x == null || !x.enabled);
        }
    }

    /// <summary>
    /// A data structure holding information about a collider that intersected with a ray.
    /// </summary>
    // This can be further expanded upon with more information if need be.
    public readonly struct IntersectionHit
    {
        public readonly GameObject GameObject;
        public readonly Bounds Bounds;
        public readonly Vector3 Point;
        public readonly float Distance;

        public IntersectionHit(GameObject gameObject, Bounds bounds, Ray impactRay, float distance)
        {
            GameObject = gameObject;
            Bounds = bounds;
            Point = impactRay.GetPoint(distance);
            Distance = distance;
        }
    }
}

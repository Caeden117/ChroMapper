using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
    private const float INTERSECTION_EPSILON = 0.0001f;

    private static LinkedList<IntersectionCollider> colliders = new LinkedList<IntersectionCollider>();

    /// <summary>
    /// Registers a custom collider to the system.
    /// </summary>
    public static void RegisterCollider(IntersectionCollider collider) => colliders.AddLast(collider);

    /// <summary>
    /// Unregisters a custom collider from the system.
    /// </summary>
    public static void UnregisterCollider(IntersectionCollider collider) => colliders.Remove(collider);

    /// <summary>
    /// A data structure holding information about a collider that intersected with a ray.
    /// </summary>
    // This can be further expanded upon with more information if need be.
    public class IntersectionHit
    {
        public readonly GameObject GameObject;
        public readonly Bounds Bounds;
        public readonly Vector3 Point;

        public IntersectionHit(GameObject gameObject, Bounds bounds, Ray impactRay, float distance)
        {
            GameObject = gameObject;
            Bounds = bounds;
            Point = impactRay.GetPoint(distance);
        }
    }
}

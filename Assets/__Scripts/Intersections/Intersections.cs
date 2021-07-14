using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
    public const int ChunkSize = 1;
    private const float INTERSECTION_EPSILON = 0.0001f;

    public static Func<int, int> NextGroupSearchFunction = x => ++x;
    public static int CurrentGroup = 0;

    private static List<IntersectionCollider>[] colliders = new List<IntersectionCollider>[32];
    private static Dictionary<int, List<IntersectionCollider>>[] groupedColliders = new Dictionary<int, List<IntersectionCollider>>[32];

    static Intersections()
    {
        for (int i = 0; i < 32; i++)
        {
            colliders[i] = new List<IntersectionCollider>();
            groupedColliders[i] = new Dictionary<int, List<IntersectionCollider>>();
        }
    }

    /// <summary>
    /// Registers a custom collider to the system.
    /// </summary>
    //public static void RegisterCollider(IntersectionCollider collider) => colliders[collider.CollisionLayer].Add(collider);

    /// <summary>
    /// Unregisters a custom collider from the system.
    /// </summary>
    //public static void UnregisterCollider(IntersectionCollider collider) => colliders[collider.CollisionLayer].Remove(collider);

    public static void RegisterColliderToGroups(IntersectionCollider collider)
    {
        var groupDictionary = groupedColliders[collider.CollisionLayer];

        foreach (var group in collider.CollisionGroups)
        {
            if (!groupDictionary.TryGetValue(group, out var list))
            {
                list = new List<IntersectionCollider>();
                groupDictionary.Add(group, list);
            }

            list.Add(collider);
        }
    }

    public static bool UnregisterColliderFromGroups(IntersectionCollider collider)
    {
        var groupDictionary = groupedColliders[collider.CollisionLayer];

        var successful = false;

        foreach (var group in collider.CollisionGroups)
        {
            if (groupDictionary.TryGetValue(group, out var list))
            {
                successful |= list.Remove(collider);
                if (list.Count == 0) groupDictionary.Remove(group);
            }
        }

        return successful;
    }

    /// <summary>
    /// Clears the internal colliders list.
    /// </summary>
    public static void Clear()
    {
        foreach (var colliderList in groupedColliders)
        {
            colliderList.Clear();
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

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
#if UNITY_EDITOR
    /// <summary>
    /// Cast a ray against all custom colliders, and returns additional information on the raycasting process.
    /// </summary>
    /// <remarks>None of the output lists are sorted in any way.</remarks>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="considered">A list of all colliders that were further considered for intersections.</param>
    /// <param name="optimizedAway">A list of all colliders that were not considered for intersections.</param>
    /// <returns>Returns a list of information on all intersecting colliders, if any.</returns>
    public static List<IntersectionHit> DebugRaycastAll(Ray ray, out List<IntersectionCollider> considered, out List<IntersectionCollider> optimizedAway)
    {
        var hits = new List<IntersectionHit>();
        optimizedAway = new List<IntersectionCollider>();
        considered = new List<IntersectionCollider>();

        var rayDirection = ray.direction;
        var rayOrigin = ray.origin;

        foreach (var collider in colliders)
        {
            // The first pass checks if the ray even intersects the bounds of the collider.
            // If not, the collider is considered unnecessary, and no further work is done on it.
            if (!collider.BoundsRenderer.bounds.IntersectRay(ray))
            {
                optimizedAway.Add(collider);
                continue;
            }

            considered.Add(collider);

            // See the RaycastIndividual_Internal method for more information on the second pass.
            if (RaycastIndividual_Internal(collider, in rayDirection, in rayOrigin, out _))
            {
                hits.Add(new IntersectionHit(collider.gameObject));
            }
        }

        return hits;
    }
#endif
}

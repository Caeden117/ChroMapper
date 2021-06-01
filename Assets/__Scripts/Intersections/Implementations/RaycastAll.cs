using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
    /// <summary>
    /// Cast a ray against all custom colliders, and returns a list of all intersecting colliders.
    /// </summary>
    /// <remarks>The returned list is not sorted in any way.</remarks>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <returns>Returns a list of information on all intersecting colliders, if any.</returns>
    public static List<IntersectionHit> RaycastAll(Ray ray)
    {
        var hits = new List<IntersectionHit>();

        var rayDirection = ray.direction;
        var rayOrigin = ray.origin;

        foreach (var collider in colliders)
        {
            // The first pass checks if the ray even intersects the bounds of the collider.
            // If not, the collider is considered unnecessary, and no further work is done on it.
            // See the RaycastIndividual_Internal method for more information on the second pass.
            if (collider.BoundsRenderer.bounds.IntersectRay(ray) && RaycastIndividual_Internal(collider, in rayDirection, in rayOrigin, out _))
            {
                hits.Add(new IntersectionHit(collider.gameObject));
            }
        }

        return hits;
    }

    /// <summary>
    /// Cast a ray against all custom colliders on a given layer, and returns a list of all intersecting colliders.
    /// </summary>
    /// <remarks>The returned list is not sorted in any way.</remarks>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="layer">GameObject layer to raycast against. An input of <c>-1</c> will cast against all layers.</param>
    /// <returns>Returns a list of information on all intersecting colliders, if any.</returns>
    public static List<IntersectionHit> RaycastAll(Ray ray, int layer)
    {
        var hits = new List<IntersectionHit>();

        var rayDirection = ray.direction;
        var rayOrigin = ray.origin;

        foreach (var collider in colliders)
        {
            // The first pass checks if the ray even intersects the bounds of the collider.
            // If not, the collider is considered unnecessary, and no further work is done on it.
            // See the RaycastIndividual_Internal method for more information on the second pass.
            if ((layer == -1 || collider.CollisionLayer == layer)
                && collider.BoundsRenderer.bounds.IntersectRay(ray)
                && RaycastIndividual_Internal(collider, in rayDirection, in rayOrigin, out _))
            {
                hits.Add(new IntersectionHit(collider.gameObject));
            }
        }

        return hits;
    }
}

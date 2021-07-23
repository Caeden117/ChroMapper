using System;
using System.Collections.Generic;
using System.Linq;
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
    public static IEnumerable<IntersectionHit> RaycastAll(Ray ray) => RaycastAll(ray, -1);

    /// <summary>
    /// Cast a ray against all custom colliders on a given layer, and returns a list of all intersecting colliders.
    /// </summary>
    /// <remarks>The returned list is not sorted in any way.</remarks>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="layer">GameObject layer to raycast against. An input of <c>-1</c> will cast against all layers.</param>
    /// <returns>Returns a list of information on all intersecting colliders, if any.</returns>
    public static IEnumerable<IntersectionHit> RaycastAll(Ray ray, int layer)
    {
        var hits = new List<IntersectionHit>();

        var rayDirection = ray.direction;
        var rayOrigin = ray.origin;

        var layerMin = layer == -1 ? 0 : layer;
        var layerMax = layer == -1 ? 32 : layer + 1;

        for (int currentLayer = layerMin; currentLayer < layerMax; currentLayer++)
        {
            var groupedCollidersInLayer = groupedColliders[currentLayer];

            if (groupedCollidersInLayer.Count <= 0) continue;

            var groupKeys = groupedCollidersInLayer.Keys;
            var (lowestKey, highestKey) = (groupKeys.Min(), groupKeys.Max());

            var groupID = Mathf.Clamp(CurrentGroup, lowestKey, highestKey);
            var rounds = Math.Max(groupID - lowestKey, highestKey - groupID) * 2 + 1;

            for (var k = 0; k < rounds; k++)
            //while (groupedCollidersInLayer.TryGetValue(startingGroup, out var collidersInLayer))
            {
                if (groupID < lowestKey || groupID > highestKey)
                {
                    groupID = NextGroupSearchFunction(groupID);
                    continue;
                }
                
                if (groupedCollidersInLayer.TryGetValue(groupID, out var collidersInLayer) && collidersInLayer.Count > 0)
                {
                    var count = collidersInLayer.Count;

                    for (int i = 0; i < count; i++)
                    {
                        var collider = collidersInLayer[i];

                        if (layer == -1 || collider.CollisionLayer == layer)
                        {
                            var bounds = collider.BoundsRenderer.bounds;

                            // The first pass checks if the ray even intersects the bounds of the collider.
                            // If not, the collider is considered unnecessary, and no further work is done on it.
                            // See the RaycastIndividual_Internal method for more information on the second pass.
                            if (bounds.IntersectRay(ray)
                                && RaycastIndividual_Internal(collider, in rayDirection, in rayOrigin, out var dist))
                            {
                                hits.Add(new IntersectionHit(collider.gameObject, bounds, ray, dist));
                            }
                        }
                    }
                }

                groupID = NextGroupSearchFunction(groupID);
            }
        }

        return hits;
    }
}

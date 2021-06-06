using System.Linq;
using UnityEngine;

/// <summary>
/// A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
    /// <summary>
    /// Cast a ray against all custom colliders, and returns the closest intersecting collider.
    /// </summary>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="hit">Information on the object that was hit.</param>
    /// <returns>Returns <c>true</c> if the ray successfully intersected a collider, and <c>false</c> if not.</returns>
    public static bool Raycast(Ray ray, out IntersectionHit hit) => Raycast(ray, -1, out hit, out _);

    /// <summary>
    /// Cast a ray against all custom colliders, and returns the closest intersecting collider.
    /// </summary>
    /// <param name="ray">The ray to cast against all colliders.<</param>
    /// <param name="hit">Information on the object that was hit.</param>
    /// <param name="distance">
    /// If the ray intersects, this is the distance from the ray origin to the intersection point,
    /// and <see cref="float.PositiveInfinity"/> otherwise.
    /// </param>
    /// <returns>Returns <c>true</c> if the ray successfully intersected a collider, and <c>false</c> if not.</returns>
    public static bool Raycast(Ray ray, out IntersectionHit hit, out float distance) => Raycast(ray, -1, out hit, out distance);

    /// <summary>
    /// Cast a ray against all custom colliders in the provided layer, and returns the closest intersecting collider.
    /// </summary>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="layer">GameObject layer to raycast against. An input of <c>-1</c> will cast against all layers.</param>
    /// <param name="hit">Information on the object that was hit.</param>
    /// <returns>Returns <c>true</c> if the ray successfully intersected a collider, and <c>false</c> if not.</returns>
    public static bool Raycast(Ray ray, int layer, out IntersectionHit hit) => Raycast(ray, layer, out hit, out _);

    /// <summary>
    /// Cast a ray against all custom colliders in the provided layer, and returns the closest intersecting collider.
    /// </summary>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="layer">GameObject layer to raycast against. An input of <c>-1</c> will cast against all layers.</param>
    /// <param name="hit">Information on the object that was hit.</param>
    /// <param name="distance">
    /// If the ray intersects, this is the distance from the ray origin to the intersection point,
    /// and <see cref="float.PositiveInfinity"/> otherwise.
    /// </param>
    /// <returns>Returns <c>true</c> if the ray successfully intersected a collider, and <c>false</c> if not.</returns>
    public static bool Raycast(Ray ray, int layer, out IntersectionHit hit, out float distance)
    {
        var hits = RaycastAll(ray, layer);

        distance = float.PositiveInfinity;
        hit = new IntersectionHit();

        var count = hits.Count();

        if (count == 0)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            var newHit = hits.ElementAt(i);

            if (newHit.Distance < distance)
            {
                hit = newHit;
                distance = newHit.Distance;
            }
        }

        return true;
    }
}

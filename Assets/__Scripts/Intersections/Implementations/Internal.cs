using System;
using UnityEngine;

/// <summary>
/// A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
    // Once we've determined that the ray intersects the bounding box of the collider,
    // we loop through all triangles until we find one that intersects the ray.
    // Doing things this way loses a little bit of speed, but increases accuracy on non-cube meshes.
    private static bool RaycastIndividual_Internal(IntersectionCollider collider, in Vector3 rayDirection, in Vector3 rayOrigin, out float distance)
    {
        var success = false;
        distance = 0;

        var localToWorldMatrix = collider.transform.localToWorldMatrix;

        // The triangles/vertices arrays are cached as to not allocate garbage every frame.
        var meshTriangles = collider.MeshTriangles;
        var meshVertices = collider.MeshVertices;

        for (var i = 0; i < meshTriangles.Length; i += 3)
        {
            // Calculate world-space positions of triangle vertices
            var vert1 = localToWorldMatrix.FastMultiplyPoint3x4(in meshVertices[meshTriangles[i]]);
            var vert2 = localToWorldMatrix.FastMultiplyPoint3x4(in meshVertices[meshTriangles[i + 1]]);
            var vert3 = localToWorldMatrix.FastMultiplyPoint3x4(in meshVertices[meshTriangles[i + 2]]);

            // If our ray intersects this triangle, the entire collider intersects, no more work to be done.
            if (RayTriangleIntersect(in vert1, in vert2, in vert3, in rayDirection, in rayOrigin, out var localDistance) && (!success || localDistance < distance))
            {
                success = true;
                distance = localDistance;
            }
        }

        // The ray did not intersect any triangles; the ray did not collide.
        return success;
    }

    // (These vectors are moved outside of the Ray-Triangle intersection algorithm to keep runtime allocations at bay)
    private static Vector3 e1 = new Vector3();
    private static Vector3 e2 = new Vector3();
    private static Vector3 p = new Vector3();
    private static Vector3 q = new Vector3();
    private static Vector3 t = new Vector3();

    // Fast Möller–Trumbore intersection algorithm
    // Variables passed by-reference to prevent copying
    private static bool RayTriangleIntersect(in Vector3 p1, in Vector3 p2, in Vector3 p3, in Vector3 rayDirection, in Vector3 rayOrigin, out float distance)
    {
        distance = 0;

        //Find vectors for two edges sharing vertex/point p1
        VectorUtils.FastSubtraction(ref e1, in p2, in p1);
        VectorUtils.FastSubtraction(ref e2, in p3, in p1);

        // calculating determinant 
        VectorUtils.FastCross(ref p, in rayDirection, in e2);

        //Calculate determinat
        var det = VectorUtils.FastDot(in e1, in p);

        //if determinant is near zero, ray lies in plane of triangle otherwise not
        if (det > -INTERSECTION_EPSILON && det < INTERSECTION_EPSILON) return false;

        var invDet = 1.0f / det;

        //calculate distance from p1 to ray origin
        VectorUtils.FastSubtraction(ref t, in rayOrigin, in p1);

        //Calculate u parameter
        var u = VectorUtils.FastDot(in t, in p) * invDet;

        //Check for ray hit
        if (u < 0 || u > 1) return false;

        //Prepare to test v parameter
        VectorUtils.FastCross(ref q, in t, in e1);

        //Calculate v parameter
        var v = VectorUtils.FastDot(in rayDirection, in q) * invDet;

        //Check for ray hit
        if (v < 0 || u + v > 1) return false;

        // If this dot product is within our epsilon, a hit is confirmed.
        if ((distance = VectorUtils.FastDot(in e2, in q) * invDet) > INTERSECTION_EPSILON) return true;

        // No hit at all
        return false;
    }
}

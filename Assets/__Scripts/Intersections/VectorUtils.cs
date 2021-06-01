using System;
using UnityEngine;

/// <summary>
/// Faster implementations of various Unity <see cref="Vector3"/> algorithms.
/// The most common optimizion is to pass values by-ref instead of by-value, which removes the need for data copying.
/// </summary>
public static class VectorUtils
{
    public static float FastDot(in Vector3 lhs, in Vector3 rhs)
    {
        return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
    }

    public static void FastCross(ref Vector3 res, in Vector3 lhs, in Vector3 rhs)
    {
        res.x = lhs.y * rhs.z - lhs.z * rhs.y;
        res.y = lhs.z * rhs.x - lhs.x * rhs.z;
        res.z = lhs.x * rhs.y - lhs.y * rhs.x;
    }

    public static void FastSubtraction(ref Vector3 res, in Vector3 a, in Vector3 b)
    {
        res.x = a.x - b.x;
        res.y = a.y - b.y;
        res.z = a.z - b.z;
    }

    public static float FastDistance(in Vector3 a, in Vector3 b)
    {
        float num = a.x - b.x;
        float num2 = a.y - b.y;
        float num3 = a.z - b.z;
        return (float)Math.Sqrt(num * num + num2 * num2 + num3 * num3);
    }
}

using UnityEngine;

/// <summary>
/// Faster implementations of various Unity <see cref="Matrix4x4"/> algorithms.
/// The most common optimizion is to pass values by-ref instead of by-value, which removes the need for data copying.
/// </summary>
public static class MatrixUtils
{
	public static Vector3 FastMultiplyPoint3x4(this in Matrix4x4 matrix, in Vector3 point)
	{
		Vector3 result;
		result.x = matrix.m00 * point.x + matrix.m01 * point.y + matrix.m02 * point.z + matrix.m03;
		result.y = matrix.m10 * point.x + matrix.m11 * point.y + matrix.m12 * point.z + matrix.m13;
		result.z = matrix.m20 * point.x + matrix.m21 * point.y + matrix.m22 * point.z + matrix.m23;
		return result;
	}

	public static Vector3 FastMultiplyDirection(this in Matrix4x4 matrix, in Vector3 vector)
    {
		Vector3 result;
		result.x = matrix.m00 * vector.x + matrix.m01 * vector.y + matrix.m02 * vector.z;
		result.y = matrix.m10 * vector.x + matrix.m11 * vector.y + matrix.m12 * vector.z;
		result.z = matrix.m20 * vector.x + matrix.m21 * vector.y + matrix.m22 * vector.z;
		return result;
	}
}

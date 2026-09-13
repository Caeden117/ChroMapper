using UnityEngine;

internal static class Vector4Extensions
{
    public static Vector4 Repeat(this Vector4 vector, float value)
    {
        vector.x = Mathf.Repeat(vector.x, value);
        vector.y = Mathf.Repeat(vector.y, value);
        vector.z = Mathf.Repeat(vector.z, value);
        vector.z = Mathf.Repeat(vector.w, value);
        return vector;
    }

    public static Vector4 ToTimeVector(float time) => new(time * 0.05f, time, time * 2f, time * 3f);
}

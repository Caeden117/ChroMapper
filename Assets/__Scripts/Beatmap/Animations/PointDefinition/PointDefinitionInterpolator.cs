using System;
using UnityEngine;

/// <summary>A normalized curve whose parsed structure is shared by all typed animation consumers.</summary>
public sealed class PointDefinitionInterpolator
{
    private readonly IPointDefinitionValue[] points;
    public int Count => points.Length;
    internal bool HasBaseProvider { get; }
    public IPointDefinitionValue this[int index] => points[index];

    public PointDefinitionInterpolator(IPointDefinitionValue value) : this(new[] { value }) { }

    public PointDefinitionInterpolator(IPointDefinitionValue[] values, bool hasBaseProvider = false)
    {
        if (values.Length == 0) throw new ArgumentException("Point definition must contain at least one point.");
        points = (IPointDefinitionValue[])values.Clone();
        HasBaseProvider = hasBaseProvider;
    }

    public T Get<T>(float normalized) where T : struct => typeof(T) switch
    {
        var type when type == typeof(float) => (T)(object)GetFloat(normalized),
        var type when type == typeof(Vector3) => (T)(object)GetVector3(normalized),
        var type when type == typeof(Vector4) => (T)(object)GetVector4(normalized),
        var type when type == typeof(Quaternion) => (T)(object)GetQuaternion(normalized),
        var type when type == typeof(Color) => (T)(object)GetColor(normalized),
        _ => throw new ArgumentException($"Unsupported point definition type {typeof(T).Name}.")
    };

    public float GetFloat(float normalized)
    {
        Search(normalized, out var a, out var b, out var t);
        return a == b ? points[b].Float : Mathf.LerpUnclamped(points[a].Float, points[b].Float, t);
    }

    public Vector3 GetVector3(float normalized)
    {
        Search(normalized, out var a, out var b, out var t);
        if (a == b) return points[b].Vector3;
        if (!points[b].SplineCatmullRom) return Vector3.LerpUnclamped(points[a].Vector3, points[b].Vector3, t);

        var p0 = points[Mathf.Max(a - 1, 0)].Vector3;
        var p1 = points[a].Vector3;
        var p2 = points[b].Vector3;
        var p3 = points[Mathf.Min(b + 1, points.Length - 1)].Vector3;
        var tt = t * t;
        var ttt = tt * t;
        return 0.5f * ((p0 * (-ttt + 2 * tt - t)) + (p1 * (3 * ttt - 5 * tt + 2))
            + (p2 * (-3 * ttt + 4 * tt + t)) + (p3 * (ttt - tt)));
    }

    public Vector4 GetVector4(float normalized)
    {
        Search(normalized, out var a, out var b, out var t);
        if (a == b) return points[b].Vector4;
        return points[b].LerpHSV
            ? LerpHSV(points[a].Color, points[b].Color, t)
            : Vector4.LerpUnclamped(points[a].Vector4, points[b].Vector4, t);
    }

    public Quaternion GetQuaternion(float normalized)
    {
        Search(normalized, out var a, out var b, out var t);
        // Evaluate point modifiers before interpolating the resulting quaternions.
        return a == b ? points[b].Quaternion : Quaternion.SlerpUnclamped(points[a].Quaternion, points[b].Quaternion, t);
    }

    public Color GetColor(float normalized)
    {
        Search(normalized, out var a, out var b, out var t);
        if (a == b) return points[b].Color;
        return points[b].LerpHSV
            ? LerpHSV(points[a].Color, points[b].Color, t)
            : Color.LerpUnclamped(points[a].Color, points[b].Color, t);
    }

    // Heck's lerpHSV point flag interpolates normalized hue as a scalar, unlike Chroma's trueHSV light effect.
    private static Color LerpHSV(Color start, Color end, float t)
    {
        Color.RGBToHSV(start, out var startHue, out var startSaturation, out var startValue);
        Color.RGBToHSV(end, out var endHue, out var endSaturation, out var endValue);
        var color = Color.HSVToRGB(
            Mathf.LerpUnclamped(startHue, endHue, t),
            Mathf.LerpUnclamped(startSaturation, endSaturation, t),
            Mathf.LerpUnclamped(startValue, endValue, t));
        color.a = Mathf.LerpUnclamped(start.a, end.a, t);
        return color;
    }

    private void Search(float time, out int a, out int b, out float interpolation)
    {
        // Upper bound chooses the last authored row at an exact (including repeated) point time.
        var left = 0;
        var right = points.Length;
        while (left < right)
        {
            var middle = left + (right - left) / 2;
            if (points[middle].T <= time) left = middle + 1;
            else right = middle;
        }
        a = Mathf.Max(left - 1, 0);
        b = Mathf.Min(left, points.Length - 1);
        if (left == 0 || left == points.Length)
        {
            b = a;
            interpolation = 0;
            return;
        }
        interpolation = points[b].Easing((time - points[a].T) / (points[b].T - points[a].T));
    }
}

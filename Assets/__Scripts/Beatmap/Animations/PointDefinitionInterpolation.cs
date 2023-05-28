// Mostly just copied from Heck
using System;
using System.Collections.Generic;

using UnityEngine;

namespace Beatmap.Animations
{
    public class PointDefinitionInterpolation
    {
        public static T Lerp<T>(PointDefinition<T>? prev, PointDefinition<T> next, float interpolation, float time, T _default) where T : struct
        {
            T start = prev?.Interpolate(time, out var _) ?? _default;
            T end = next.Interpolate(time, out var _);
            return (start, end) switch
            {
                (float f1, float f2) => (T)(object)Mathf.LerpUnclamped(f1, f2, interpolation),
                (Color c1, Color c2) => (T)(object)Color.LerpUnclamped(c1, c2, interpolation),
                (Vector3 v1, Vector3 v2) => (T)(object)Vector3.LerpUnclamped(v1, v2, interpolation),
                (Quaternion q1, Quaternion q2) => (T)(object)Quaternion.SlerpUnclamped(q1, q2, interpolation),
                _ => throw new Exception($"Unhandled PointDefinition Lerp for type {typeof(T).Name}"),
            };
        }
    }
}

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
            dynamic start = prev?.Interpolate(time, out var _) ?? _default;
            dynamic end = next.Interpolate(time, out var _);
            return _default switch
            {
                float f => Mathf.LerpUnclamped(start, end, interpolation),
                Vector3 v3 => Vector3.LerpUnclamped(start, end, interpolation),
                Quaternion q => Quaternion.SlerpUnclamped(start, end, interpolation),
                _ => throw new Exception($"Unhandled PointDefinition Lerp for type {typeof(T).Name}"),
            };
        }
    }
}

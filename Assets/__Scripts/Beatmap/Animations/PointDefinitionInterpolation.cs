// Mostly just copied from Heck
using System;
using System.Collections.Generic;

using UnityEngine;

namespace Beatmap.Animations
{
    public class PointDefinitionInterpolation
    {
        public static T Lerp<T>(PointDefinition<T>? prev, PointDefinition<T> next, float interpolation, float time) where T : struct
        {
            switch (next)
            {
            case PointDefinition<float> floats:
                return (T)(object)FloatLerp(prev as PointDefinition<float>, floats, interpolation, time);
            case PointDefinition<Vector3> vectors:
                return (T)(object)Vector3Lerp(prev as PointDefinition<Vector3>, vectors, interpolation, time);
            }
            return default;
        }
        
        public static float FloatLerp(PointDefinition<float>? prev, PointDefinition<float> next, float interpolation, float time)
        {
            return prev == null
                ? next.Interpolate(time, out var _)
                : Mathf.LerpUnclamped(prev.Interpolate(time, out var _), next.Interpolate(time, out var _), interpolation);
        }
        
        static Vector3 Vector3Lerp(PointDefinition<Vector3>? prev, PointDefinition<Vector3> next, float interpolation, float time)
        {
            return prev == null
                ? next.Interpolate(time, out var _)
                : Vector3.LerpUnclamped(prev.Interpolate(time, out var _), next.Interpolate(time, out var _), interpolation);
        }
    }
}

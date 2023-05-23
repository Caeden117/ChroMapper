// Mostly just copied from Heck
using System;
using System.Collections.Generic;

using UnityEngine;

namespace Beatmap.Animations
{
    public class PointDefinitionInterpolation
    {
        public static T Lerp<T>(PointDefinition<T>? prev, PointDefinition<T> next, float interpolation, float time, T _default = default(T)) where T : struct
        {
            switch (next)
            {
            case PointDefinition<float> floats:
                return (T)(object)FloatLerp(prev as PointDefinition<float>, floats, interpolation, time, (float)(object)_default);
            case PointDefinition<Vector3> vectors:
                return (T)(object)Vector3Lerp(prev as PointDefinition<Vector3>, vectors, interpolation, time, (Vector3)(object)_default);
            }
            return default;
        }

        public static float FloatLerp(PointDefinition<float>? prev, PointDefinition<float> next, float interpolation, float time, float _default)
        {
            return prev == null
                ? Mathf.LerpUnclamped(_default, next.Interpolate(time, out var _), interpolation)
                : Mathf.LerpUnclamped(prev.Interpolate(time, out var _), next.Interpolate(time, out var _), interpolation);
        }

        static Vector3 Vector3Lerp(PointDefinition<Vector3>? prev, PointDefinition<Vector3> next, float interpolation, float time, Vector3 _default)
        {
            return prev == null
                ? Vector3.SlerpUnclamped(_default, next.Interpolate(time, out var _), interpolation)
                : Vector3.SlerpUnclamped(prev.Interpolate(time, out var _), next.Interpolate(time, out var _), interpolation);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beatmap.Animations
{
    // Goofy way to do template specialization from https://stackoverflow.com/a/29379250
    public interface IPointDataInterpolator<T> where T :　struct
    {
        public T Lerp(PointDefinition<T>.PointData[] points, int prev, int next, float time);
    }

    public class LinearPDI<T> : IPointDataInterpolator<T> where T : struct
    {
        public static readonly IPointDataInterpolator<T> Instance = LinearPDI.Instance as IPointDataInterpolator<T> ?? new LinearPDI<T>();

        T IPointDataInterpolator<T>.Lerp(PointDefinition<T>.PointData[] points, int prev, int next, float time)
            => throw new Exception($"Unhandled LerpFunc for type {typeof(T).Name}");
    }

    public class LinearPDI : IPointDataInterpolator<float>, IPointDataInterpolator<Color>, IPointDataInterpolator<Vector3>, IPointDataInterpolator<Quaternion>
    {
        public static LinearPDI Instance = new LinearPDI();

        float IPointDataInterpolator<float>.Lerp(PointDefinition<float>.PointData[] points, int prev, int next, float time)
        {
            return Mathf.LerpUnclamped(points[prev].Value, points[next].Value, time);
        }
        Color IPointDataInterpolator<Color>.Lerp(PointDefinition<Color>.PointData[] points, int prev, int next, float time)
        {
            return Color.LerpUnclamped(points[prev].Value, points[next].Value, time);
        }
        Vector3 IPointDataInterpolator<Vector3>.Lerp(PointDefinition<Vector3>.PointData[] points, int prev, int next, float time)
        {
            return Vector3.LerpUnclamped(points[prev].Value, points[next].Value, time);
        }
        Quaternion IPointDataInterpolator<Quaternion>.Lerp(PointDefinition<Quaternion>.PointData[] points, int prev, int next, float time)
        {
            return Quaternion.SlerpUnclamped(points[prev].Value, points[next].Value, time);
        }
    }

    public class CatmullRomPDI<T> : IPointDataInterpolator<T> where T : struct
    {
        public static readonly IPointDataInterpolator<T> Instance = CatmullRomPDI.Instance as IPointDataInterpolator<T> ?? new CatmullRomPDI<T>();

        T IPointDataInterpolator<T>.Lerp(PointDefinition<T>.PointData[] points, int prev, int next, float time)
            => LinearPDI<T>.Instance.Lerp(points, prev, next, time);
    }

    public class CatmullRomPDI : IPointDataInterpolator<Vector3>
    {
        public static CatmullRomPDI Instance = new CatmullRomPDI();

        Vector3 IPointDataInterpolator<Vector3>.Lerp(PointDefinition<Vector3>.PointData[] points, int a, int b, float time)
        {
            // Catmull-Rom Spline
            Vector3 p0 = a - 1 < 0 ? points[a].Value : points[a - 1].Value;
            Vector3 p1 = points[a].Value;
            Vector3 p2 = points[b].Value;
            Vector3 p3 = b + 1 > points.Length - 1 ? points[b].Value : points[b + 1].Value;

            float tt = time * time;
            float ttt = tt * time;

            float q0 = -ttt + (2.0f * tt) - time;
            float q1 = (3.0f * ttt) - (5.0f * tt) + 2.0f;
            float q2 = (-3.0f * ttt) + (4.0f * tt) + time;
            float q3 = ttt - tt;

            Vector3 c = 0.5f * ((p0 * q0) + (p1 * q1) + (p2 * q2) + (p3 * q3));

            return c;
        }
    }

    public class HSVPDI<T> : IPointDataInterpolator<T> where T : struct
    {
        public static readonly IPointDataInterpolator<T> Instance = HSVPDI.Instance as IPointDataInterpolator<T> ?? new HSVPDI<T>();

        T IPointDataInterpolator<T>.Lerp(PointDefinition<T>.PointData[] points, int prev, int next, float time)
            => LinearPDI<T>.Instance.Lerp(points, prev, next, time);
    }

    public class HSVPDI : IPointDataInterpolator<Color>
    {
        public static HSVPDI Instance = new HSVPDI();

        Color IPointDataInterpolator<Color>.Lerp(PointDefinition<Color>.PointData[] points, int a, int b, float time)
        {
            Color.RGBToHSV(points[a].Value, out float hl, out float sl, out float vl);
            Color.RGBToHSV(points[b].Value, out float hr, out float sr, out float vr);
            Color lerped = Color.HSVToRGB(Mathf.LerpUnclamped(hl, hr, time), Mathf.LerpUnclamped(sl, sr, time), Mathf.LerpUnclamped(vl, vr, time));
            return new Color(lerped.r, lerped.g, lerped.b, Mathf.LerpUnclamped(points[a].Value.a, points[b].Value.a, time));
        }
    }

    public class PointDataInterpolators
    {
        public static PointDefinition<T>.InterpolationHandler LinearLerp<T>() where T : struct
        {
            return LinearPDI<T>.Instance.Lerp;
        }
        public static PointDefinition<T>.InterpolationHandler CatmullRomLerp<T>() where T : struct
        {
            return CatmullRomPDI<T>.Instance.Lerp;
        }
        public static PointDefinition<T>.InterpolationHandler HSVLerp<T>() where T : struct
        {
            return HSVPDI<T>.Instance.Lerp;
        }
    }
}

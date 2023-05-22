// Mostly just copied from Heck
using System;
using System.Collections.Generic;
using UnityEngine;

using SimpleJSON;

namespace Beatmap.Animations
{
    public class PointDefinition<T> : IComparable<PointDefinition<T>>
        where T : struct
    {
        public List<PointData> Points;
        public float StartTime = 0;
        public float Duration = 0;
        public Func<float, float> Easing;

        public delegate T Parser(JSONArray data, out int i);
        public delegate T InterpolationHandler(List<PointData> points, int prev, int next, float time);

        // Used for searching ONLY
        public PointDefinition(float start)
        {
            StartTime = start;
        }

        public PointDefinition(Parser parser, JSONArray data, float start, float duration, Func<float, float> easing, float tbegin = 0, float tend = 0)
        {
            Points = new List<PointData>();
            StartTime = start;
            Duration = duration;
            Easing = easing;

            foreach (var row in data) {
                // WTF, Jevk
                if (row.Value.AsArray == null) {
                    Points.Add(new PointData(parser, data, tbegin, tend));
                    break;
                }
                Points.Add(new PointData(parser, row.Value.AsArray, tbegin, tend));
            }
        }

        public T Interpolate(float time, out bool last)
        {
            last = false;

            var count = Points.Count;

            if (count == 0) {
                last = true;
                return default;
            }

            if (Points[count - 1].Time <= time) {
                last = true;
                return Points[count - 1].Value;
            }

            if (Points[0].Time >= time) {
                return Points[0].Value;
            }

            GetIndexes(time, out int prev, out int next);

            float normalTime;
            float divisor = Points[next].Time - Points[prev].Time;
            if (divisor != 0)
            {
                normalTime = (time - Points[prev].Time) / divisor;
            }
            else
            {
                normalTime = 0;
            }

            normalTime = Points[next].Easing(normalTime);

            return Points[next].Lerp(Points, prev, next, normalTime);
        }

        private void GetIndexes(float time, out int prev, out int next)
        {
            prev = 0;
            next = Points.Count;

            while (prev < next - 1)
            {
                int m = (prev + next) / 2;
                float pointTime = Points[m].Time;

                if (pointTime < time)
                {
                    prev = m;
                }
                else
                {
                    next = m;
                }
            }
        }

        public int CompareTo(PointDefinition<T> other)
        {
            // TODO: might be able to cheese previous/next with this
            return StartTime.CompareTo(other.StartTime);
        }

        public class PointData : IComparable<PointData>
        {
            public PointData(Parser parser, JSONArray data, float tbegin = 0, float tend = 0)
            {
                Value = parser(data, out int i);
                if (data.Count > i)
                {
                    // Track or Path animation
                    Time = (tend == 0)
                        ? data[i++].AsFloat
                        : Mathf.LerpUnclamped(tbegin, tend, data[i++]);
                }
                else
                {
                    // WTF Jevk
                    Time = 0;
                }
                if (data.Count > i && ((string)data[i]).StartsWith("easing")) {
                    Easing = global::Easing.Named(data[i++]);
                }
                else {
                    Easing = global::Easing.Linear;
                }

                if (data.Count > i && ((string)data[i]) == "splineCatmullRom") {
                    Lerp = PointDataInterpolators.CatmullRomLerp<T>;
                }
                else {
                    Lerp = PointDataInterpolators.LinearLerp<T>;
                }
            }

            public T Value { get; }
            public float Time { get; }
            public Func<float, float> Easing { get; }
            public InterpolationHandler Lerp { get; }

            public int CompareTo(PointData other)
            {
                return Time.CompareTo(other.Time);
            }
        }
    }

    public class PointDataInterpolators
    {
        public static T LinearLerp<T>(List<PointDefinition<T>.PointData> points, int prev, int next, float time) where T : struct
        {
            switch (points)
            {
             case List<PointDefinition<float>.PointData> floats:
                return (T)(object)FloatLerp(floats, prev, next, time);
            case List<PointDefinition<Vector3>.PointData> vectors:
                return (T)(object)LinearVectorLerp(vectors, prev, next, time);
            }
            return default;
        }

        public static T CatmullRomLerp<T>(List<PointDefinition<T>.PointData> points, int prev, int next, float time) where T : struct
        {
            switch (points)
            {
            case List<PointDefinition<Vector3>.PointData> vectors:
                return (T)(object)LinearVectorLerp(vectors, prev, next, time);
            }
            return default;
        }

        public static float FloatLerp(List<PointDefinition<float>.PointData> points, int prev, int next, float time)
        {
            return Mathf.LerpUnclamped(points[prev].Value, points[next].Value, time);
        }

        public static Vector3 LinearVectorLerp(List<PointDefinition<Vector3>.PointData> points, int prev, int next, float time)
        {
            return Vector3.LerpUnclamped(points[prev].Value, points[next].Value, time);
        }

        public static Vector3 SmoothVectorLerp(List<PointDefinition<Vector3>.PointData> points, int a, int b, float time)
        {
            // Catmull-Rom Spline
            Vector3 p0 = a - 1 < 0 ? points[a].Value : points[a - 1].Value;
            Vector3 p1 = points[a].Value;
            Vector3 p2 = points[b].Value;
            Vector3 p3 = b + 1 > points.Count - 1 ? points[b].Value : points[b + 1].Value;

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

    public class PointDataParsers
    {
        public static float ParseFloat(JSONArray data, out int i)
        {
            if (data.Count < 1) throw new Exception($"Invalid data: {data}");
            i = 1;
            return data[0];
        }

        public static Vector3 ParseVector3(JSONArray data, out int i)
        {
            i = 3;
            return new Vector3(data[0], data[1], data[2]);
        }
    }
}

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
        // For AnimateTrack
        public float Duration = 0;
        // For AssignPathAnimation
        public float Transition = 0;
        public Func<float, float> Easing;

        public delegate T Parser(JSONArray data, out int i);
        public delegate T InterpolationHandler(List<PointData> points, int prev, int next, float time);

        // Used for searching ONLY
        public PointDefinition(float start)
        {
            StartTime = start;
        }

        public PointDefinition(Parser parser, JSONArray data, float start, float duration, bool path, Func<float, float> easing, float tbegin = 0, float tend = 0)
        {
            Points = new List<PointData>();
            StartTime = start;
            if (path)
            {
                Transition = duration;
            }
            else
            {
                Duration = duration;
            }
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
                if (data.Count > i && ((string)data[i]).StartsWith("eas")) {
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
            return LinearLerpFunc<T>()(points[prev].Value, points[next].Value, time);
        }

        public delegate T LerpFunc<T>(T prev, T next, float time);

        public static LerpFunc<T> LinearLerpFunc<T>() where T : struct
        {
            // I hate C#
            return typeof(T) switch
            {
                var n when n == typeof(float) => (T a, T b, float c) => Mathf.LerpUnclamped((dynamic)a, (dynamic)b, c),
                var n when n == typeof(Vector3) => (T a, T b, float c) => Vector3.LerpUnclamped((dynamic)a, (dynamic)b, c),
                var n when n == typeof(Quaternion) => (T a, T b, float c) => Quaternion.SlerpUnclamped((dynamic)a, (dynamic)b, c),
                _ => throw new Exception($"Unhandled LerpFunc for type {typeof(T).Name}"),
            };
        }

        public static T CatmullRomLerp<T>(List<PointDefinition<T>.PointData> points, int prev, int next, float time) where T : struct
        {
            return typeof(T) switch
            {
                var n when n == typeof(Vector3) => SmoothVectorLerp((dynamic)points, prev, next, time),
                _ => LinearLerp<T>(points, prev, next, time),
            };
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
            i = 1;
            return data[0];
        }

        public static Vector3 ParseVector3(JSONArray data, out int i)
        {
            i = 3;
            return new Vector3(data[0], data[1], data[2]);
        }

        public static Quaternion ParseQuaternion(JSONArray data, out int i)
        {
            i = 3;
            return Quaternion.Euler(data[0], data[1], data[2]);
        }
    }
}

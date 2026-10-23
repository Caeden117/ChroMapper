// Mostly just copied from Heck

using System;
using UnityEngine;
using SimpleJSON;
using Beatmap.Base.Customs;
using Beatmap.Base;

namespace Beatmap.Animations
{
    public interface IPointDefinition
    {
        struct UntypedParams
        {
            public string Key;
            public bool Overwrite;
            public JSONNode Points;
            public string Easing;
            public float Time;
            public float Transition;
            public float Duration;
            public float TimeBegin;
            public float TimeEnd;
            public int Repeat;
            internal int PreparedIndex;
            internal int PreparedLength;
            internal BaseObject LogSource;
        }
    }

    /// <summary>Legacy typed event wrapper over the shared normalized point-definition curve.</summary>
    public class PointDefinition<T> : IPointDefinition, IComparable<PointDefinition<T>> where T : struct
    {
        public BaseCustomEvent Source;
        public PointData[] Points;
        public float StartTime { get; private set; }
        public float Duration;
        public int Repeat;
        public float Transition;
        public Func<float, float> Easing;
        internal long Sequence;
        private readonly PointDefinitionInterpolator curve;
        private readonly float timeBegin;
        private readonly float timeEnd;
        internal bool HasCurve => curve != null;
        internal bool HasBaseProvider => curve?.HasBaseProvider == true;
        internal bool IsConstant => curve?.Count == 1 && !HasBaseProvider;

        public delegate T Parser(JSONArray data, ref int i);
        public delegate T InterpolationHandler(PointData[] points, int prev, int next, float time);

        public PointDefinition(float start) => StartTime = start;

        public PointDefinition(Parser parser, IPointDefinition.UntypedParams p, BaseCustomEvent source)
        {
            Source = source;
            StartTime = p.Time;
            Duration = p.Duration;
            Repeat = Math.Max(0, p.Repeat);
            Transition = p.Transition;
            Easing = global::Easing.HeckNamed(p.Easing ?? "easeLinear");
            timeBegin = p.TimeBegin;
            timeEnd = p.TimeEnd;
            curve = PointDefinitionParser.Get(p.Points, PointDefinitionParser.Dimension<T>(),
                new AnimationLogContext(p, source), typeof(T) == typeof(Quaternion), typeof(T).Name);
            Points = new PointData[curve?.Count ?? 0];
            for (var i = 0; i < Points.Length; ++i) Points[i] = new PointData(curve[i], timeBegin, timeEnd);
        }

        public T Interpolate(float time)
        {
            if (curve == null) return default;
            if (Source?.Type == "AnimateTrack" && Duration == 0) return curve.Get<T>(1);
            if (timeEnd == 0) return curve.Get<T>(time);
            var span = timeEnd - timeBegin;
            return curve.Get<T>(span == 0 ? float.PositiveInfinity : (time - timeBegin) / span);
        }

        // Color paths sample object lifetime independently of the event transition clock.
        internal T InterpolateNormalized(float normalized) => curve != null ? curve.Get<T>(normalized) : default;

        public int CompareTo(PointDefinition<T> other)
        {
            var comparison = StartTime.CompareTo(other.StartTime);
            return comparison != 0 ? comparison : Sequence.CompareTo(other.Sequence);
        }

        // Retained for callers of the typed interpolation helpers; values use the same shared parser.
        public class PointData : IComparable<PointData>
        {
            private readonly IPointDefinitionValue point;
            public T Value => typeof(T) switch
            {
                var type when type == typeof(float) => (T)(object)point.Float,
                var type when type == typeof(Vector3) => (T)(object)point.Vector3,
                var type when type == typeof(Vector4) => (T)(object)point.Vector4,
                var type when type == typeof(Quaternion) => (T)(object)point.Quaternion,
                var type when type == typeof(Color) => (T)(object)point.Color,
                _ => throw new ArgumentException($"Unsupported point definition type {typeof(T).Name}.")
            };
            public float Time { get; }
            public Func<float, float> Easing { get; }
            public InterpolationHandler Lerp { get; }

            public PointData(Parser parser, JSONArray data, float tbegin = 0, float tend = 0)
                : this(PointDefinitionParser.ParsePoint<T>(data), tbegin, tend) { }

            internal PointData(IPointDefinitionValue value, float begin, float end)
            {
                point = value;
                Time = end == 0 ? value.T : Mathf.LerpUnclamped(begin, end, value.T);
                Easing = value.Easing;
                Lerp = value.SplineCatmullRom ? PointDataInterpolators.CatmullRomLerp<T>()
                    : value.LerpHSV ? PointDataInterpolators.HSVLerp<T>() : PointDataInterpolators.LinearLerp<T>();
            }

            public int CompareTo(PointData other) => Time.CompareTo(other.Time);
        }
    }

    public class PointDataParsers
    {
        public static ColorSchemeSO ColorScheme;

        public static float ParseFloat(JSONArray data, ref int i)
        {
            i += 1;
            return data[0];
        }

        public static Color ParseColor(JSONArray data, ref int i)
        {
            Color result;

            if (data[i].IsString)
            {
                i += 1;

                result = data[0].Value switch
                {
                    // Intentionally not supporting baseSaber_Color since those don't mirror with left handed
                    // mode while baseNote_Color does. Should almost always use baseNote over baseSaber.
                    "baseNote0Color" => ColorScheme.LeftNoteColor,
                    "baseNote1Color" => ColorScheme.RightNoteColor,
                    "baseEnvironmentColor0" => ColorScheme.EnvironmentLeftColor,
                    "baseEnvironmentColor1" => ColorScheme.EnvironmentRightColor,
                    "baseEnvironmentColorW" => ColorScheme.EnvironmentWhiteColor,
                    "baseEnvironmentColor0Boost" => ColorScheme.EnvironmentLeftBoostColor,
                    "baseEnvironmentColor1Boost" => ColorScheme.EnvironmentRightBoostColor,
                    "baseEnvironmentColorWBoost" => ColorScheme.EnvironmentWhiteBoostColor,
                    "baseObstaclesColor" => ColorScheme.ObstacleColor,
                    _ => DefaultColors.White
                };
            }
            else
            {
                i += 4;
                result = new Color(data[0], data[1], data[2], data[3]);
            }

            if (data[i] is JSONArray array)
            {
                i += 1;

                var innerIdx = 0;
                var subColor = ParseColor(array, ref innerIdx);

                var colorOp = array[innerIdx].Value;

                result = colorOp switch
                {
                    "opAdd" => result + subColor,
                    "opSub" => result - subColor,
                    "opMul" => result * subColor,
                    "opDiv" => new Color(
                        result.r / subColor.r,
                        result.g / subColor.g,
                        result.b / subColor.b,
                        result.a / subColor.a),
                    _ => result
                };
            }

            return result;
        }

        public static Vector3 ParseVector3(JSONArray data, ref int i)
        {
            i += 3;
            return new Vector3(data[0], data[1], data[2]);
        }

        public static Quaternion ParseQuaternion(JSONArray data, ref int i)
        {
            i += 3;
            return Quaternion.Euler(data[0], data[1], data[2]);
        }
    }
}

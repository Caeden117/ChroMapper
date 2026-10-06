using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using Beatmap.Base.Customs;

namespace Beatmap.Animations
{
    public interface IPointDefinition
    {
        public struct UntypedParams
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
        }

        // A point can combine literal values with live base providers.
        public interface IValueSegment
        {
            int Dimension { get; }
            void Append(float[] target, ref int offset);
        }
    }

    internal readonly struct StaticValues : IPointDefinition.IValueSegment
    {
        private readonly float[] values;

        internal StaticValues(float[] values) => this.values = values;

        public int Dimension => values.Length;

        public void Append(float[] target, ref int offset)
        {
            for (var i = 0; i < values.Length; ++i)
            {
                target[offset++] = values[i];
            }
        }
    }

    public class PointDefinition<T> : IPointDefinition, IComparable<PointDefinition<T>>
        where T : struct
    {
        public BaseCustomEvent Source;
        public PointData[] Points;

        public float StartTime { get; private set; } = 0;

        // For AnimateTrack
        public float Duration = 0;

        // For AssignPathAnimation
        public float Transition = 0;
        public Func<float, float> Easing;

        public delegate T Parser(JSONArray data, ref int i);

        public delegate T InterpolationHandler(PointData[] points, int prev, int next, float time);

        // Lookup key only, contains no animation points.
        public PointDefinition(float start)
        {
            StartTime = start;
        }

        public PointDefinition(Parser parser, IPointDefinition.UntypedParams p, BaseCustomEvent source)
        {
            Source = source;
            StartTime = p.Time;
            Transition = p.Transition;
            Duration = p.Duration;
            // Use Heck's curve variants for event easing, including Back, Bounce, Elastic, and Expo.
            Easing = global::Easing.HeckNamed(p.Easing ?? "easeLinear");

            // Keep the parser argument for existing callers. T determines how values are decoded.
            var data = ResolvePointsNode(p);

            if (data == null || data.Count == 0)
            {
                Points = Array.Empty<PointData>();
                return;
            }

            // A flat array is shorthand for one point at time zero. Nested modifiers belong to that point.
            if (!data[0].IsArray)
            {
                var single = ParsePoint(data, p.TimeBegin, p.TimeEnd, barePoint: true);
                Points = single != null ? new[] { single } : Array.Empty<PointData>();
                return;
            }

            var parsed = new List<PointData>(data.Count);
            for (var i = 0; i < data.Count; ++i)
            {
                var point = ParsePoint(data[i].AsArray, p.TimeBegin, p.TimeEnd, barePoint: false);
                if (point != null)
                {
                    parsed.Add(point);
                }
            }

            Points = parsed.ToArray();
        }

        public T Interpolate(float time)
        {
            var count = Points.Length;

            if (count == 0)
            {
                return default;
            }

            if (Points[count - 1].Time <= time)
            {
                return Points[count - 1].Value;
            }

            if (Points[0].Time >= time)
            {
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
            next = Points.Length;

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
            return StartTime.CompareTo(other.StartTime);
        }

        private static JSONArray ResolvePointsNode(IPointDefinition.UntypedParams p)
        {
            switch (p.Points)
            {
                case JSONArray arr:
                    return arr;
                case JSONString named:
                    return BeatSaberSongContainer.Instance.Map.PointDefinitions.TryGetValue(named.Value, out var definition)
                        ? definition.AsArray
                        : null;
                default:
                    return null;
            }
        }

        // Read components in order from literals and base providers, then read time.
        // Only the single-point shorthand may omit time.
        private PointData ParsePoint(JSONArray row, float tbegin, float tend, bool barePoint)
        {
            var segments = new List<IPointDefinition.IValueSegment>();
            var staticRun = new List<float>();
            var flags = new List<string>();
            var modifiers = new List<PointModifier<T>>();
            var hasLiveSegment = false;

            for (var i = 0; i < row.Count; ++i)
            {
                var item = row[i];
                if (item is JSONArray modifierNode)
                {
                    var modifier = ParseModifier(modifierNode);
                    if (modifier == null) return null;
                    modifiers.Add(modifier);
                }
                else if (item is JSONString)
                {
                    if (item.Value.StartsWith("base", StringComparison.Ordinal))
                    {
                        FlushStaticRun(segments, staticRun);
                        var segment = BaseProviderManager.Resolve(item.Value);
                        if (segment == null) return null;
                        segments.Add(segment);
                        hasLiveSegment = true;
                    }
                    else
                    {
                        flags.Add(item.Value);
                    }
                }
                else if (item is JSONNumber)
                {
                    staticRun.Add(item.AsFloat);
                }
                else if (item == null || item.IsNull)
                {
                    // Treat null entries as zero, matching Heck's point format. This is very much necessary sadly, as mappers have inadvertently used this in important places...
                    staticRun.Add(0f);
                }
                else
                {
                    Debug.LogError($"Point contains an unsupported entry [{item.Value}] and was skipped.");
                    return null;
                }
            }

            var dimension = PointType<T>.Dimension;

            if (!hasLiveSegment)
            {
                var requiredCount = dimension + 1;
                var minimumCount = barePoint ? dimension : dimension + 1;
                if (staticRun.Count < minimumCount || staticRun.Count > requiredCount)
                {
                    Debug.LogError(
                        $"Point for [{typeof(T).Name}] must have {dimension} numbers plus an optional time; got {staticRun.Count} and it was skipped.");
                    return null;
                }

                var values = new float[dimension];
                for (var i = 0; i < dimension; ++i)
                {
                    values[i] = staticRun[i];
                }

                return new PointData(
                    PointType<T>.Convert(values),
                    null,
                    modifiers.ToArray(),
                    ResolveTime(staticRun.Count == dimension + 1 ? staticRun[dimension] : 0f, tbegin, tend),
                    ResolveEasing(flags),
                    ResolveLerp(flags));
            }

            FlushStaticRun(segments, staticRun);

            var total = 0;
            foreach (var segment in segments)
            {
                total += segment.Dimension;
            }

            float time;
            if (total == dimension && barePoint)
            {
                time = 0f;
            }
            else if (total == dimension + 1)
            {
                // Capture the final value as time when parsing; provider-driven point values stay live.
                var scratch = new float[segments[^1].Dimension];
                var offset = 0;
                segments[^1].Append(scratch, ref offset);
                time = scratch[^1];
            }
            else
            {
                Debug.LogError(
                    $"Point for [{typeof(T).Name}] must have {dimension} numbers plus a time, but got {total} and it was skipped.");
                return null;
            }

            return new PointData(
                null,
                segments.ToArray(),
                modifiers.ToArray(),
                ResolveTime(time, tbegin, tend),
                ResolveEasing(flags),
                ResolveLerp(flags));
        }

        private PointModifier<T> ParseModifier(JSONArray row)
        {
            var segments = new List<IPointDefinition.IValueSegment>();
            var staticRun = new List<float>();
            string operation = null;
            var nested = new List<PointModifier<T>>();
            var hasLiveSegment = false;

            for (var i = 0; i < row.Count; ++i)
            {
                var item = row[i];
                if (item is JSONArray innerNode)
                {
                    // Nested modifiers do not break a run of literal operands.
                    var inner = ParseModifier(innerNode);
                    if (inner == null) return null;
                    nested.Add(inner);
                }
                else if (item is JSONString)
                {
                    if (item.Value.StartsWith("base", StringComparison.Ordinal))
                    {
                        FlushStaticRun(segments, staticRun);
                        var segment = BaseProviderManager.Resolve(item.Value);
                        if (segment == null) return null;
                        segments.Add(segment);
                        hasLiveSegment = true;
                    }
                    else if (operation == null)
                    {
                        operation = item.Value;
                    }
                    else
                    {
                        Debug.LogError("Modifier must have exactly one operation. The point was skipped.");
                        return null;
                    }
                }
                else if (item is JSONNumber)
                {
                    staticRun.Add(item.AsFloat);
                }
                else
                {
                    Debug.LogError($"Modifier contains an unsupported entry [{item.Value}] and was skipped.");
                    return null;
                }
            }

            if (operation == null)
            {
                Debug.LogError("Modifier must have one operation. The point was skipped.");
                return null;
            }

            if (!hasLiveSegment)
            {
                if (staticRun.Count != PointType<T>.Dimension)
                {
                    Debug.LogError(
                        $"Modifier for [{typeof(T).Name}] must have {PointType<T>.Dimension} numbers but got {staticRun.Count} and the point was skipped.");
                    return null;
                }

                var values = new float[PointType<T>.Dimension];
                for (var i = 0; i < PointType<T>.Dimension; ++i)
                {
                    values[i] = staticRun[i];
                }

                return new PointModifier<T>(PointType<T>.Convert(values), null, nested.ToArray(), operation);
            }

            FlushStaticRun(segments, staticRun);

            var total = 0;
            foreach (var segment in segments)
            {
                total += segment.Dimension;
            }

            if (total != PointType<T>.Dimension)
            {
                Debug.LogError(
                    $"Modifier for [{typeof(T).Name}] must have {PointType<T>.Dimension} numbers but got {total} and the point was skipped.");
                return null;
            }

            return new PointModifier<T>(null, segments.ToArray(), nested.ToArray(), operation);
        }

        private static void FlushStaticRun(
            List<IPointDefinition.IValueSegment> segments,
            List<float> staticRun)
        {
            if (staticRun.Count == 0) return;
            segments.Add(new StaticValues(staticRun.ToArray()));
            staticRun.Clear();
        }

        private static float ResolveTime(float rawTime, float tbegin, float tend) =>
            (tend == 0) ? rawTime : Mathf.LerpUnclamped(tbegin, tend, rawTime);

        // The first easing flag wins; unknown names use linear easing.
        private static Func<float, float> ResolveEasing(List<string> flags)
        {
            foreach (var flag in flags)
            {
                if (flag.StartsWith("ease", StringComparison.Ordinal))
                {
                    return global::Easing.HeckNamed(flag);
                }
            }

            return global::Easing.Linear;
        }

        private static InterpolationHandler ResolveLerp(List<string> flags)
        {
            var lerp = PointDataInterpolators.LinearLerp<T>();
            foreach (var flag in flags)
            {
                // Now catmull is in my google search history forever
                if (flag == "splineCatmullRom")
                {
                    lerp = PointDataInterpolators.CatmullRomLerp<T>();
                }

                if (flag == "lerpHSV")
                {
                    lerp = PointDataInterpolators.HSVLerp<T>();
                }
            }

            return lerp;
        }

        public class PointData : IComparable<PointData>
        {
            private readonly T? staticBase;
            private readonly IPointDefinition.IValueSegment[] segments;
            private readonly PointModifier<T>[] modifiers;
            private readonly float[] buffer;

            internal PointData(
                T? staticBase,
                IPointDefinition.IValueSegment[] segments,
                PointModifier<T>[] modifiers,
                float time,
                Func<float, float> easing,
                InterpolationHandler lerp)
            {
                this.staticBase = staticBase;
                this.segments = segments;
                this.modifiers = modifiers;
                Time = time;
                Easing = easing;
                Lerp = lerp;
                buffer = segments != null ? new float[PointType<T>.Dimension] : Array.Empty<float>();
            }

            // Read providers on each evaluation so camera, color, and time-dependent values stay current.
            public T Value
            {
                get
                {
                    if (modifiers.Length == 0 && segments == null)
                    {
                        return staticBase!.Value;
                    }

                    // Advance smoothing before reading a provider's held value.
                    BaseProviderManager.Tick();
                    var current = segments != null ? PointType<T>.Convert(FillBuffer()) : staticBase!.Value;
                    for (var i = 0; i < modifiers.Length; ++i)
                    {
                        current = PointType<T>.ApplyOperation(current, modifiers[i].Point, modifiers[i].Operation);
                    }

                    return current;
                }
            }

            public float Time { get; }
            public Func<float, float> Easing { get; }
            public InterpolationHandler Lerp { get; }

            private float[] FillBuffer()
            {
                var offset = 0;
                foreach (var segment in segments)
                {
                    // Stop once the property's components are filled; the remaining value is the timestamp.
                    if (offset >= buffer.Length) break;
                    segment.Append(buffer, ref offset);
                }

                return buffer;
            }

            public int CompareTo(PointData other) => Time.CompareTo(other.Time);
        }
    }

    internal sealed class PointModifier<T>
        where T : struct
    {
        private readonly T? raw;
        private readonly IPointDefinition.IValueSegment[] segments;
        private readonly PointModifier<T>[] nested;
        private readonly float[] buffer;

        internal PointModifier(
            T? raw,
            IPointDefinition.IValueSegment[] segments,
            PointModifier<T>[] nested,
            string operation)
        {
            this.raw = raw;
            this.segments = segments;
            this.nested = nested;
            Operation = operation;
            buffer = segments != null ? new float[PointType<T>.Dimension] : Array.Empty<float>();
        }

        public string Operation { get; }

        // Evaluate nested modifiers first, then combine their results using each one's operation.
        public T Point
        {
            get
            {
                var current = raw ?? PointType<T>.Convert(FillBuffer());
                for (var i = 0; i < nested.Length; ++i)
                {
                    current = PointType<T>.ApplyOperation(current, nested[i].Point, nested[i].Operation);
                }

                return current;
            }
        }

        private float[] FillBuffer()
        {
            var offset = 0;
            foreach (var segment in segments)
            {
                if (offset >= buffer.Length) break;
                segment.Append(buffer, ref offset);
            }

            return buffer;
        }
    }

    internal static class PointType<T>
        where T : struct
    {
        internal static readonly int Dimension = ResolveDimension();
        internal static readonly Func<float[], T> Convert = CreateConverter();
        internal static readonly Func<T, T, string, T> ApplyOperation = CreateOperations();

        private static int ResolveDimension() =>
            typeof(T) == typeof(float) ? 1
            : typeof(T) == typeof(Vector3) || typeof(T) == typeof(Quaternion) ? 3
            : typeof(T) == typeof(Color) ? 4
            : throw new Exception($"Unhandled point definition type {typeof(T).Name}");

        private static Func<float[], T> CreateConverter() =>
            typeof(T) == typeof(float) ? values => (T)(object)values[0]
            : typeof(T) == typeof(Vector3) ? values => (T)(object)new Vector3(values[0], values[1], values[2])
            : typeof(T) == typeof(Quaternion) ? values => (T)(object)Quaternion.Euler(values[0], values[1], values[2])
            : values => (T)(object)new Color(values[0], values[1], values[2], values[3]);

        private static Func<T, T, string, T> CreateOperations() =>
            typeof(T) == typeof(float)
                ? (current, modifier, operation) =>
                {
                    var a = (float)(object)current;
                    var b = (float)(object)modifier;
                    return (T)(object)(operation switch
                    {
                        "opAdd" => a + b,
                        "opSub" => a - b,
                        "opMul" => a * b,
                        "opDiv" => a / b,
                        "opNone" => a,
                        _ => throw new Exception($"[{operation}] cannot be performed on type float.")
                    });
                }
            : typeof(T) == typeof(Vector3)
                ? (current, modifier, operation) =>
                {
                    var a = (Vector3)(object)current;
                    var b = (Vector3)(object)modifier;
                    return (T)(object)(operation switch
                    {
                        "opAdd" => a + b,
                        "opSub" => a - b,
                        "opMul" => Vector3.Scale(a, b),
                        "opDiv" => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z),
                        "opNone" => a,
                        _ => throw new Exception($"[{operation}] cannot be performed on type Vector3.")
                    });
                }
            : typeof(T) == typeof(Quaternion)
                ? (current, modifier, operation) =>
                {
                    // Quaternion modifiers use Euler-angle arithmetic to match Heck.
                    var a = ((Quaternion)(object)current).eulerAngles;
                    var b = ((Quaternion)(object)modifier).eulerAngles;
                    var result = operation switch
                    {
                        "opAdd" => a + b,
                        "opSub" => a - b,
                        "opMul" => Vector3.Scale(a, b),
                        "opDiv" => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z),
                        "opNone" => a,
                        _ => throw new Exception($"[{operation}] cannot be performed on type Quaternion.")
                    };
                    return (T)(object)Quaternion.Euler(result);
                }
            : (current, modifier, operation) =>
            {
                var a = (Color)(object)current;
                var b = (Color)(object)modifier;
                return (T)(object)(operation switch
                {
                    "opAdd" => a + b,
                    "opSub" => a - b,
                    "opMul" => a * b,
                    "opDiv" => new Color(a.r / b.r, a.g / b.g, a.b / b.b, a.a / b.a),
                    "opNone" => a,
                    _ => throw new Exception($"[{operation}] cannot be performed on type Color.")
                });
            };
    }

    // Keep legacy parser entry points for callers; PointType<T> handles point evaluation.
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

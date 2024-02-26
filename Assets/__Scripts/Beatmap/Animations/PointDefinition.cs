// Mostly just copied from Heck
using System;
using System.Collections.Generic;
using UnityEngine;

using SimpleJSON;

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
    }

    public class PointDefinition<T> : IPointDefinition, IComparable<PointDefinition<T>>
        where T : struct
    {
        public PointData[] Points;
        public float StartTime { get; private set; } = 0;
        // For AnimateTrack
        public float Duration = 0;
        // For AssignPathAnimation
        public float Transition = 0;
        public Func<float, float> Easing;

        public delegate T Parser(JSONArray data, ref int i);
        public delegate T InterpolationHandler(PointData[] points, int prev, int next, float time);

        // Used for searching ONLY
        public PointDefinition(float start)
        {
            StartTime = start;
        }

        public PointDefinition(Parser parser, IPointDefinition.UntypedParams p)
        {
            StartTime = p.Time;
            Transition = p.Transition;
            Duration = p.Duration;
            Easing = global::Easing.Named(p.Easing ?? "easeLinear");

            var _points = new List<PointData>();
            var data = p.Points switch {
                JSONArray arr => arr,
                JSONString pd => (BeatSaberSongContainer.Instance.Map.PointDefinitions.ContainsKey(pd) ? BeatSaberSongContainer.Instance.Map.PointDefinitions[pd] : throw new Exception($"Missing point definition {pd}")),
                _ => new JSONArray(), // TODO: Does this unset properly?
            };

            foreach (var row in data) {
                // WTF, Jevk
                if (row.Value.AsArray == null) {
                    _points.Add(new PointData(parser, data, p.TimeBegin, p.TimeEnd));
                    break;
                }
                _points.Add(new PointData(parser, row.Value.AsArray, p.TimeBegin, p.TimeEnd));
            }

            Points = _points.ToArray();
        }

        public T Interpolate(float time)
        {
            var count = Points.Length;

            if (count == 0) {
                return default;
            }

            if (Points[count - 1].Time <= time) {
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
            // TODO: might be able to cheese previous/next with this
            return StartTime.CompareTo(other.StartTime);
        }

        public class PointData : IComparable<PointData>
        {
            public PointData(Parser parser, JSONArray data, float tbegin = 0, float tend = 0)
            {
                var i = 0;
                Value = parser(data, ref i);
                
                var len = data.Count;
                if (len > i)
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
                Easing = global::Easing.Linear;
                Lerp = PointDataInterpolators.LinearLerp<T>();

                for (; i < len; ++i)
                {
                    string str = data[i];
                    
                    if (str[0] == 'e')
                    {
                        Easing = global::Easing.Named(str);
                    }

                    if (str == "splineCatmullRom")
                    {
                        Lerp = PointDataInterpolators.CatmullRomLerp<T>();
                    }

                    if (str == "lerpHSV")
                    {
                        Lerp = PointDataInterpolators.HSVLerp<T>();
                    }
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

    public class PointDataParsers
    {
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
                    "baseNote0Color" => LoadInitialMap.Platform != null
                        ? LoadInitialMap.Platform.Colors.RedColor
                        : BeatSaberSong.DefaultLeftColor,
                    "baseNote1Color" => LoadInitialMap.Platform != null
                        ? LoadInitialMap.Platform.Colors.BlueColor
                        : BeatSaberSong.DefaultRightColor,
                    _ => BeatSaberSong.DefaultWhiteColor
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
                    "opDiv" => new Color(result.r / subColor.r, result.g / subColor.g, result.b / subColor.b,
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

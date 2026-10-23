using System;
using System.Collections.Generic;
using UnityEngine;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    public interface IAnimateProperty
    {
        float StartTime { get; }
        bool IsEmpty();
        bool UpdateProperty(float time);
        bool ResetEvaluatedValue();
        void Sort();
        void CompletePreparedOrder();
        void RemoveEvent(BaseCustomEvent ev);
        void RefreshBpmTiming();
        float GetTransformAnchorTime(float time);
    }

    public class AnimateProperty<T> : IAnimateProperty where T : struct
    {
        public List<PointDefinition<T>> PointDefinitions;
        public Action<T> Setter;
        public T Default;
        public float StartTime { get; private set; } = Mathf.Infinity;
        internal bool UsePathClock;

        private long nextSequence;
        private PointDefinition<T>[] preparedDefinitions = Array.Empty<PointDefinition<T>>();
        private bool hasUnpreparedDefinitions;
        private bool hasEvaluatedValue;
        private T evaluatedValue;
        private readonly List<(float Time, BaseCustomEvent Source, long Sequence)> nullDefinitions = new();
        private (float Time, int FileOrder, long Sequence, bool HasValue)[] presenceRequests =
            Array.Empty<(float, int, long, bool)>();
        private readonly HashSet<PointDefinition<T>> pathsAfterNull = new();
        private float[] lastPresenceWriteTimes = Array.Empty<float>();
        private float[] constantTransformWriteTimes = Array.Empty<float>();

        public AnimateProperty(List<PointDefinition<T>> points, Action<T> setter, T defaultValue)
        {
            PointDefinitions = points;
            Setter = setter;
            Default = defaultValue;
            foreach (var point in points) point.Sequence = nextSequence++;
        }

        public bool IsEmpty() => PointDefinitions.Count == 0 && nullDefinitions.Count == 0;

        public static bool SkipsMissingPointDefinition(IPointDefinition.UntypedParams p, BaseCustomEvent source = null)
        {
            if (p.Points is not JSONString named) return false;
            var map = BeatSaberSongContainer.Instance != null ? BeatSaberSongContainer.Instance.Map : null;
            if (map != null && map.PointDefinitions.ContainsKey(named.Value)) return false;
            Debug.LogError($"Could not find point definition [{named.Value}]. {new AnimationLogContext(p, source).Describe()}");
            return true;
        }

        public void AddPointDef(PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p,
            BaseCustomEvent source)
        {
            if (SkipsMissingPointDefinition(p, source)) return;
            hasUnpreparedDefinitions = true;
            var definition = new PointDefinition<T>(parser, p, source) { Sequence = nextSequence++ };
            if (definition.HasCurve) PointDefinitions.Add(definition);
        }

        // Parsed curves remain shared; repeats are evaluated arithmetically rather than expanded per object.
        public void AddPreparedPointDef(PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p,
            BaseCustomEvent source)
        {
            if (SkipsMissingPointDefinition(p, source)) return;
            var definition = new PointDefinition<T>(parser, p, source) { Sequence = nextSequence++ };
            if (!definition.HasCurve) return;
            PointDefinitions.Add(definition);
            if (p.PreparedIndex < 0 || p.PreparedLength == 0)
            {
                hasUnpreparedDefinitions = true;
                return;
            }
            if (preparedDefinitions.Length == 0) preparedDefinitions = new PointDefinition<T>[p.PreparedLength];
            preparedDefinitions[p.PreparedIndex] = definition;
        }

        public void CompletePreparedOrder() => Sort();

        internal void AddNullDefinition(float time, BaseCustomEvent source) =>
            nullDefinitions.Add((time, source, nextSequence++));

        internal bool HasValueAt(float time)
        {
            if (nullDefinitions.Count == 0) return time >= StartTime && PointDefinitions.Count > 0;
            var index = FindPresence(time);
            return index >= 0 && presenceRequests[index].HasValue;
        }

        // A cancelled property stops writing. Reconstruct its last write for objects born before cancellation.
        internal bool TryGetLastWriteTime(float time, out float lastWrite)
        {
            lastWrite = time;
            if (HasValueAt(time)) return true;
            if (nullDefinitions.Count == 0) return false;
            var index = FindPresence(time);
            if (index < 0 || float.IsNegativeInfinity(lastPresenceWriteTimes[index])) return false;
            lastWrite = lastPresenceWriteTimes[index];
            return true;
        }

        public T GetLerpedValue(float time) => Evaluate(time, time, false);
        public T GetPathValue(float time, float normalizedTime) => Evaluate(time, normalizedTime, true);

        private T Evaluate(float time, float pointTime, bool normalizedPoints)
        {
            GetIndexes(time, out var current, out _);
            if (current < 0) return Default;
            var definition = PointDefinitions[current];
            var pathClock = normalizedPoints || UsePathClock;
            if (!pathClock && definition.Duration > 0)
            {
                var elapsed = time - definition.StartTime;
                var normalized = elapsed >= definition.Duration * (definition.Repeat + 1d)
                    ? 1f : (elapsed % definition.Duration) / definition.Duration;
                return definition.Interpolate(definition.StartTime + definition.Easing(normalized) * definition.Duration);
            }

            if (pathClock && (current == 0 || pathsAfterNull.Contains(definition)))
                return Sample(definition, pointTime, normalizedPoints);
            if (definition.Transition <= 0 || time >= definition.StartTime + definition.Transition)
                return Sample(definition, pointTime, normalizedPoints);

            var blend = definition.Easing(Mathf.Min((time - definition.StartTime) / definition.Transition, 1f));
            if (!normalizedPoints)
                return PointDefinitionInterpolation.Lerp(current == 0 ? null : PointDefinitions[current - 1],
                    definition, blend, pointTime, Default);
            var start = current == 0 ? Default : PointDefinitions[current - 1].InterpolateNormalized(pointTime);
            var end = definition.InterpolateNormalized(pointTime);
            return (start, end) switch
            {
                (float a, float b) => (T)(object)Mathf.LerpUnclamped(a, b, blend),
                (Color a, Color b) => (T)(object)Color.LerpUnclamped(a, b, blend),
                (Vector3 a, Vector3 b) => (T)(object)Vector3.LerpUnclamped(a, b, blend),
                (Quaternion a, Quaternion b) => (T)(object)Quaternion.SlerpUnclamped(a, b, blend),
                _ => throw new ArgumentException($"Unsupported path type {typeof(T).Name}.")
            };
        }

        private static T Sample(PointDefinition<T> definition, float time, bool normalized) =>
            normalized ? definition.InterpolateNormalized(time) : definition.Interpolate(time);

        /// <summary>Returns the final transform write beat, preserving the parent's pose at that write.</summary>
        public float GetTransformAnchorTime(float time)
        {
            GetIndexes(time, out var current, out _);
            if (current < 0) return time;
            var definition = PointDefinitions[current];
            if (definition.IsConstant) return constantTransformWriteTimes[current];
            var end = definition.StartTime + definition.Duration * (definition.Repeat + 1f);
            if (time < end) return time;
            return definition.Duration <= 0 ? end : PreviousTime(end);
        }

        private static float PreviousTime(float time)
        {
            if (time == 0) return -float.Epsilon;
            var bits = BitConverter.SingleToInt32Bits(time);
            return BitConverter.Int32BitsToSingle(time > 0 ? bits - 1 : bits + 1);
        }

        public bool UpdateProperty(float time)
        {
            if (nullDefinitions.Count > 0 && time >= StartTime && !HasValueAt(time))
                return ResetEvaluatedValue();
            var value = GetLerpedValue(time);
            var changed = !hasEvaluatedValue || !EqualityComparer<T>.Default.Equals(evaluatedValue, value);
            Setter(value);
            evaluatedValue = value;
            hasEvaluatedValue = true;
            return changed;
        }

        public bool ResetEvaluatedValue()
        {
            var changed = hasEvaluatedValue;
            hasEvaluatedValue = false;
            return changed;
        }

        public void Sort()
        {
            if (!hasUnpreparedDefinitions && preparedDefinitions.Length > 0)
            {
                PointDefinitions.Clear();
                foreach (var point in preparedDefinitions)
                    if (point != null) PointDefinitions.Add(point);
            }
            else PointDefinitions.Sort();
            StartTime = PointDefinitions.Count > 0 ? PointDefinitions[0].StartTime : Mathf.Infinity;
            constantTransformWriteTimes = new float[PointDefinitions.Count];
            for (var i = 0; i < PointDefinitions.Count; i++)
            {
                var point = PointDefinitions[i];
                constantTransformWriteTimes[i] = point.StartTime;
                if (i > 0 && point.IsConstant && PointDefinitions[i - 1].IsConstant
                    && EqualityComparer<T>.Default.Equals(point.Points[0].Value, PointDefinitions[i - 1].Points[0].Value))
                    constantTransformWriteTimes[i] = constantTransformWriteTimes[i - 1];
            }

            pathsAfterNull.Clear();
            if (nullDefinitions.Count == 0)
            {
                presenceRequests = Array.Empty<(float, int, long, bool)>();
                lastPresenceWriteTimes = Array.Empty<float>();
                return;
            }
            var requests = new List<(float Time, int FileOrder, long Sequence, bool HasValue)>(
                PointDefinitions.Count + nullDefinitions.Count);
            foreach (var point in PointDefinitions)
                requests.Add((point.StartTime, point.Source?.FileOrder ?? 0, point.Sequence, true));
            foreach (var item in nullDefinitions)
                requests.Add((item.Time, item.Source?.FileOrder ?? 0, item.Sequence, false));
            requests.Sort(static (a, b) =>
            {
                var order = a.Time.CompareTo(b.Time);
                if (order == 0) order = a.FileOrder.CompareTo(b.FileOrder);
                return order != 0 ? order : a.Sequence.CompareTo(b.Sequence);
            });
            presenceRequests = requests.ToArray();
            StartTime = Mathf.Min(StartTime, presenceRequests[0].Time);
            lastPresenceWriteTimes = new float[presenceRequests.Length];
            var lastWrite = float.NegativeInfinity;
            var afterNull = new HashSet<long>();
            for (var i = 0; i < presenceRequests.Length; i++)
            {
                var request = presenceRequests[i];
                if (!request.HasValue && i > 0 && presenceRequests[i - 1].HasValue)
                {
                    var end = PreviousTime(request.Time);
                    if (end >= presenceRequests[i - 1].Time) lastWrite = end;
                }
                if (request.HasValue && i > 0 && !presenceRequests[i - 1].HasValue)
                    afterNull.Add(request.Sequence);
                lastPresenceWriteTimes[i] = lastWrite;
            }
            foreach (var point in PointDefinitions)
                if (afterNull.Contains(point.Sequence)) pathsAfterNull.Add(point);
        }

        public void RemoveEvent(BaseCustomEvent ev)
        {
            PointDefinitions.RemoveAll(point => point.Source == ev);
            for (var i = 0; i < preparedDefinitions.Length; i++)
                if (preparedDefinitions[i]?.Source == ev) preparedDefinitions[i] = null;
            nullDefinitions.RemoveAll(item => item.Source == ev);
        }

        // Arithmetic repeats are not clipped to the loaded song's end; a BPM edit needs no re-expansion.
        public void RefreshBpmTiming() { }

        private int FindPresence(float time)
        {
            var left = 0;
            var right = presenceRequests.Length;
            while (left < right)
            {
                var middle = left + (right - left) / 2;
                if (presenceRequests[middle].Time <= time) left = middle + 1;
                else right = middle;
            }
            return left - 1;
        }

        private void GetIndexes(float time, out int prev, out int next)
        {
            var left = 0;
            var right = PointDefinitions.Count;
            while (left < right)
            {
                var middle = left + (right - left) / 2;
                if (PointDefinitions[middle].StartTime <= time) left = middle + 1;
                else right = middle;
            }
            prev = left - 1;
            next = left;
        }
    }
}

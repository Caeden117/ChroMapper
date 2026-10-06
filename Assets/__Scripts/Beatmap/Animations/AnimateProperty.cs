using System;
using System.Collections.Generic;
using UnityEngine;

using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    public interface IAnimateProperty
    {
        public float StartTime { get; }
        public bool IsEmpty();
        public bool UpdateProperty(float time);
        public bool ResetEvaluatedValue();
        public void Sort();
        public void RemoveEvent(BaseCustomEvent ev);
    }

    public class AnimateProperty<T> : IAnimateProperty
        where T : struct
    {
        public List<PointDefinition<T>> PointDefinitions;
        public Action<T> Setter;
        public T Default;

        public float StartTime { get; private set; } = Mathf.Infinity;
        private PointDefinition<T>[] evaluated = Array.Empty<PointDefinition<T>>();
        private int count;
        private bool hasEvaluatedValue;
        private T evaluatedValue;

        public AnimateProperty(List<PointDefinition<T>> points, Action<T> setter, T _default)
        {
            PointDefinitions = points;
            Setter = setter;
            Default = _default;
            count = 0;
        }

        public bool IsEmpty()
        {
            return PointDefinitions.Count == 0;
        }

        public static bool SkipsMissingPointDefinition(IPointDefinition.UntypedParams p)
        {
            if (p.Points is not JSONString named) return false;
            if (BeatSaberSongContainer.Instance.Map.PointDefinitions.ContainsKey(named.Value)) return false;

            Debug.LogError($"Could not find point definition [{named.Value}]");
            return true;
        }

        public void AddPointDef(PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, BaseCustomEvent source)
        {
            // Repeats past song end cannot be played. Cap them here to avoid allocating unused definitions.
            var repeat = p.Repeat;
            if (p.Duration <= 0)
            {
                // Events without a duration apply once.
                repeat = 0;
            }
            else if (TryGetSongEndJsonTime(out var songEnd))
            {
                var reachable = (songEnd - p.TimeBegin) / p.Duration;
                if (reachable < repeat)
                {
                    repeat = Mathf.Max((int)reachable, 0);
                }
            }

            for (var i = 0; i <= repeat; ++i)
            {
                var pp = p;
                pp.TimeBegin = p.TimeBegin + (i * p.Duration);
                pp.TimeEnd = p.TimeEnd + (i * p.Duration);
                if (i > 0)
                {
                    pp.Time = pp.TimeBegin;
                }

                PointDefinitions.Add(new PointDefinition<T>(parser, pp, source));
            }
        }

        private static bool TryGetSongEndJsonTime(out float jsonTime)
        {
            jsonTime = 0f;
            var songContainer = BeatSaberSongContainer.Instance;
            if (songContainer == null
                || songContainer.LoadedSong == null
                || songContainer.Info == null
                || songContainer.Map == null)
            {
                return false;
            }

            var songBpmTime = songContainer.LoadedSong.length * (songContainer.Info.BeatsPerMinute / 60f);
            jsonTime = songContainer.Map.SongBpmTimeToJsonTime(songBpmTime) ?? songBpmTime;
            return true;
        }

        public T GetLerpedValue(float time)
        {
            GetIndexes(time, out var current, out var _);

            if (evaluated[current].StartTime > time) {
                return Default;
            }

            var cpd = evaluated[current];

            // AnimateTrack eases the animation's clock before sampling its points.
            if (cpd.StartTime < time && time < (cpd.StartTime + cpd.Duration))
            {
                var elapsedTime = time - cpd.StartTime;
                float normalizedTime = cpd.Easing(Mathf.Min(elapsedTime / cpd.Duration, 1));
                float learpedTime = cpd.StartTime + (normalizedTime * cpd.Duration);
                return cpd.Interpolate(learpedTime);
            }

            if (time > (cpd.StartTime + cpd.Transition)) {
                return cpd.Interpolate(time);
            }
            else
            {
                var elapsedTime = time - cpd.StartTime;
                // Blend from the previous definition, or Default for the first event.
                // AnimateTrack has no Transition, so use Duration when a previous definition exists.
                // Zero-duration events switch immediately.
                var transitionDuration = current == 0
                    ? cpd.Transition
                    : (cpd.Transition > 0 ? cpd.Transition : cpd.Duration);
                float normalizedTime = cpd.Easing(Mathf.Min(elapsedTime / transitionDuration, 1));
                return PointDefinitionInterpolation.Lerp<T>(current == 0 ? null : evaluated[current - 1], evaluated[current], normalizedTime, time, Default);
            }
        }

        // A completed world-position animation stays in the parent's frame at its final write.
        // Sampling the previous representable beat keeps a parent cut at that endpoint after the write.
        public float GetTransformAnchorTime(float time)
        {
            GetIndexes(time, out var current, out _);
            var end = evaluated[current].StartTime + evaluated[current].Duration;
            if (time < end)
            {
                return time;
            }

            if (evaluated[current].Duration == 0f)
            {
                return end;
            }

            if (end == 0f)
            {
                return -float.Epsilon;
            }

            var bits = BitConverter.SingleToInt32Bits(end);
            return BitConverter.Int32BitsToSingle(end > 0f ? bits - 1 : bits + 1);
        }

        public bool UpdateProperty(float time)
        {
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
            // Stable sort :upsidedownface: turns out that's important lmoa
            var indexed = new (PointDefinition<T> definition, int originalIndex)[PointDefinitions.Count];
            for (var i = 0; i < indexed.Length; i++)
            {
                indexed[i] = (PointDefinitions[i], i);
            }

            Array.Sort(indexed, (a, b) =>
            {
                var byStartTime = a.definition.StartTime.CompareTo(b.definition.StartTime);
                return byStartTime != 0 ? byStartTime : a.originalIndex.CompareTo(b.originalIndex);
            });
            for (var i = 0; i < indexed.Length; i++)
            {
                PointDefinitions[i] = indexed[i].definition;
            }

            evaluated = GetAllPointDefinitionsThatArentCutOff();
            StartTime = evaluated[0].StartTime;
            count = evaluated.Length;
        }

        // A new event stops the previous event's repeats, even at the same beat.
        private PointDefinition<T>[] GetAllPointDefinitionsThatArentCutOff()
        {
            var sourceOriginalIndex = new Dictionary<object, int>(PointDefinitions.Count);
            var originalStarts = new List<float>(PointDefinitions.Count);
            var hasRepeats = false;
            for (var i = 0; i < PointDefinitions.Count; i++)
            {
                var key = (object)PointDefinitions[i].Source ?? PointDefinitions[i];
                if (sourceOriginalIndex.ContainsKey(key))
                {
                    hasRepeats = true;
                    continue;
                }

                sourceOriginalIndex[key] = originalStarts.Count;
                originalStarts.Add(PointDefinitions[i].StartTime);
            }

            if (!hasRepeats)
                return PointDefinitions.ToArray();

            var kept = new List<PointDefinition<T>>(PointDefinitions.Count);
            var yielded = new HashSet<object>(sourceOriginalIndex.Count);
            for (var i = 0; i < PointDefinitions.Count; i++)
            {
                var pd = PointDefinitions[i];
                var key = (object)pd.Source ?? pd;
                if (yielded.Add(key))
                {
                    kept.Add(pd);
                    continue;
                }

                var next = sourceOriginalIndex[key] + 1;
                var boundary = next < originalStarts.Count ? originalStarts[next] : float.PositiveInfinity;
                if (pd.StartTime < boundary)
                    kept.Add(pd);
            }

            return kept.ToArray();
        }

        public void RemoveEvent(BaseCustomEvent ev)
        {
            PointDefinitions.RemoveAll((pd) => pd.Source == ev);
        }

        private void GetIndexes(float time, out int prev, out int next)
        {
            prev = 0;
            next = count;

            while (prev < next - 1)
            {
                int m = (prev + next) / 2;
                float pointTime = evaluated[m].StartTime;

                if (pointTime <= time)
                {
                    prev = m;
                }
                else
                {
                    next = m;
                }
            }
        }
    }
}

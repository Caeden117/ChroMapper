using System;
using System.Collections.Generic;
using UnityEngine;

using Beatmap.Base.Customs;

namespace Beatmap.Animations
{
    public interface IAnimateProperty
    {
        public float StartTime { get; }
        public bool IsEmpty();
        public void UpdateProperty(float time);
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
        private int count;

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

        public void AddPointDef(PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, BaseCustomEvent source)
        {
            for (var i = 0; i <= p.Repeat; ++i)
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

        public T GetLerpedValue(float time)
        {
            GetIndexes(time, out var current, out var _);

            if (current < 0) {
                return Default;
            }

            var cpd = PointDefinitions[current];

            // AnimateTrack
            if (cpd.StartTime < time && time < (cpd.StartTime + cpd.Duration))
            {
                var elapsedTime = time - cpd.StartTime;
                float normalizedTime = cpd.Easing(Mathf.Min(elapsedTime / cpd.Duration, 1));
                float learpedTime = cpd.StartTime + (normalizedTime * cpd.Duration);
                return cpd.Interpolate(learpedTime);
            }

            // AssignPathAnimation
            // Only one active definition, no interpolate
            if (time > (cpd.StartTime + cpd.Transition)) {
                return cpd.Interpolate(time);
            }
            else
            {
                var elapsedTime = time - cpd.StartTime;
                float normalizedTime = cpd.Easing(Mathf.Min(elapsedTime / cpd.Transition, 1));
                return PointDefinitionInterpolation.Lerp<T>(current == 0 ? null : PointDefinitions[current - 1], PointDefinitions[current], normalizedTime, time, Default);
            }
        }

        public void UpdateProperty(float time)
        {
            Setter(GetLerpedValue(time));
        }

        public void Sort()
        {
            PointDefinitions.Sort();
            StartTime = PointDefinitions[0].StartTime;
            count = PointDefinitions.Count;
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
                float pointTime = PointDefinitions[m].StartTime;

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
    }
}

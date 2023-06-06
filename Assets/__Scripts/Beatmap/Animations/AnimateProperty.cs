using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beatmap.Animations
{
    public interface IAnimateProperty
    {
        public float StartTime { get; }
        public void UpdateProperty(float time);
        public void Sort();
    }

    public class AnimateProperty<T> : IAnimateProperty
        where T : struct
    {
        public List<PointDefinition<T>> PointDefinitions;
        public Action<T> Setter;
        public T Default;

        public float StartTime { get; private set; } = Mathf.Infinity;
        private int Count;

        public AnimateProperty(List<PointDefinition<T>> points, Action<T> setter, T _default)
        {
            PointDefinitions = points;
            Setter = setter;
            Default = _default;
            Count = 0;
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
            Count = PointDefinitions.Count;
        }

        private void GetIndexes(float time, out int prev, out int next)
        {
            prev = 0;
            next = Count;

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

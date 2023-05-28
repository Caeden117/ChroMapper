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

        public float StartTime { get { return PointDefinitions[0].StartTime; } }

        public AnimateProperty(List<PointDefinition<T>> points, Action<T> setter, T _default)
        {
            PointDefinitions = points;
            Setter = setter;
            Default = _default;
        }

        public T GetLerpedValue(float time)
        {
            // 1 after last point, inverted (probably)
            var later = PointDefinitions.BinarySearch(new PointDefinition<T>(time));

            var current = (later < 0)
                ? (~later) - 1
                : later;

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
                return cpd.Interpolate(learpedTime, out var _);
            }

            // AssignPathAnimation
            // Only one active definition, no interpolate
            if (time > (cpd.StartTime + cpd.Transition)) {
                return cpd.Interpolate(time, out var _);
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
        }
    }
}

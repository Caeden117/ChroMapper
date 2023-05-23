using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beatmap.Animations
{
    public interface IAnimateProperty
    {
        public void UpdateProperty(float time);
    }

    public class AnimateProperty<T> : IAnimateProperty
        where T : struct
    {
        public List<PointDefinition<T>> PointDefinitions;
        public Action<T> Setter;
        public T Default;

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

            // TODO: optimizations using out var last?

            // Only one active definition, no interpolate
            if (time > (PointDefinitions[current].StartTime + PointDefinitions[current].Duration)) {
                return PointDefinitions[current].Interpolate(time, out var _);
            }

            var elapsedTime = time - PointDefinitions[current].StartTime;
            float normalizedTime = Mathf.Min(elapsedTime / PointDefinitions[current].Duration, 1);
            return PointDefinitionInterpolation.Lerp<T>(current == 0 ? null : PointDefinitions[current - 1], PointDefinitions[current], normalizedTime, time, Default);
        }

        public void UpdateProperty(float time) {
            Setter(GetLerpedValue(time));
        }
    }
}

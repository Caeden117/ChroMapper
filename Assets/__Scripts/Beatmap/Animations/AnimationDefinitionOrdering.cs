using System;
using System.Collections.Generic;

namespace Beatmap.Animations
{
    internal static class AnimationDefinitionOrdering
    {
        // A new source stops the previous source's repeats, including repeats at the same beat.
        internal static T[] FilterRepeatedSources<T>(IReadOnlyList<T> sorted,
            Func<T, float> startTime, Func<T, object> source) where T : class
        {
            var sourceOriginalIndex = new Dictionary<object, int>(sorted.Count);
            var originalStarts = new List<float>(sorted.Count);
            var hasRepeats = false;
            for (var i = 0; i < sorted.Count; i++)
            {
                var key = source(sorted[i]) ?? sorted[i];
                if (sourceOriginalIndex.ContainsKey(key))
                {
                    hasRepeats = true;
                    continue;
                }

                sourceOriginalIndex[key] = originalStarts.Count;
                originalStarts.Add(startTime(sorted[i]));
            }

            if (!hasRepeats)
            {
                var result = new T[sorted.Count];
                for (var i = 0; i < result.Length; i++)
                    result[i] = sorted[i];
                return result;
            }

            var kept = new List<T>(sorted.Count);
            var yielded = new HashSet<object>(sourceOriginalIndex.Count);
            for (var i = 0; i < sorted.Count; i++)
            {
                var definition = sorted[i];
                var key = source(definition) ?? definition;
                if (yielded.Add(key))
                {
                    kept.Add(definition);
                    continue;
                }

                var next = sourceOriginalIndex[key] + 1;
                var boundary = next < originalStarts.Count ? originalStarts[next] : float.PositiveInfinity;
                if (startTime(definition) < boundary)
                    kept.Add(definition);
            }

            return kept.ToArray();
        }
    }
}

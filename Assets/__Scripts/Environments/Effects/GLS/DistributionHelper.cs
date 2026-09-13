using Beatmap.Enums;
using UnityEngine;

public static class DistributionHelper
{
    public static int GetDurationCount(IndexFilterHelper.IndexFilter indexFilter) =>
        indexFilter.LimitsDuration ? indexFilter.VisibleCount : indexFilter.Count;

    public static int GetDistributionCount(IndexFilterHelper.IndexFilter indexFilter) =>
        indexFilter.LimitsDistribution ? indexFilter.VisibleCount : indexFilter.Count;

    public static float GetBeatStep(
        int count,
        DistributionType type,
        float beatDistribution,
        float lastRelativeBeat)
    {
        beatDistribution = type == DistributionType.Wave
            ? Mathf.Max(beatDistribution - lastRelativeBeat, 0f)
            : beatDistribution;
        return type == DistributionType.Wave
            ? beatDistribution / Mathf.Max(count - 1, 1)
            : beatDistribution;
    }

    public static float GetValueStep(
        int index,
        int count,
        DistributionType type,
        float valueDistribution,
        EaseType ease)
    {
        var easing = Easing.FromID((int)ease);
        return type == DistributionType.Wave
            ? valueDistribution * easing(index / (float)Mathf.Max(count - 1, 1))
            : valueDistribution * easing(index / (float)count) * count;
    }
}

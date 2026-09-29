using Beatmap.Base;
using Beatmap.Helper;

public static class ArcCommand
{
    public static BaseArc SetHeadControlPointLengthMultiplier(BaseArc baseArc, float value)
    {
        var newArc = BeatmapFactory.Clone(baseArc);
        newArc.HeadControlPointLengthMultiplier = value;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newArc,
                baseArc,
                "Update head multiplier",
                mergeType: ActionMergeType.ArcHeadMultTweak,
                preserveSelection: true),
            true);
        return newArc;
    }

    public static BaseArc SetTailControlPointLengthMultiplier(BaseArc baseArc, float value)
    {
        var newArc = BeatmapFactory.Clone(baseArc);
        newArc.TailControlPointLengthMultiplier = value;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newArc,
                baseArc,
                "Update tail multiplier",
                mergeType: ActionMergeType.ArcTailMultTweak,
                preserveSelection: true),
            true);
        return newArc;
    }
}

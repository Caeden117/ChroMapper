using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;

public static class VNJSEventCommand
{
    public static BaseNJSEvent SetEasing(NJSEventContainer container, int easing)
    {
        if (container.NJSData.Easing == easing)
            return null;
        var original = BeatmapFactory.Clone(container.ObjectData);
        container.NJSData.Easing = easing;
        container.UpdateNJSText();
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                container.ObjectData,
                container.ObjectData,
                original,
                "Modified VNJS Event Easing",
                mergeType: ActionMergeType.ModifyNJSEventEase));
        return container.NJSData;
    }

    public static BaseNJSEvent SetExtension(NJSEventContainer container, int extension)
    {
        if (container.NJSData.UsePrevious == extension)
            return null;
        var original = BeatmapFactory.Clone(container.ObjectData);
        container.NJSData.UsePrevious = extension;
        container.UpdateNJSText();
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                container.ObjectData,
                container.ObjectData,
                original,
                "Modified VNJS Event Extension",
                mergeType: ActionMergeType.ModifyNJSEventExtension));
        return container.NJSData;
    }
}

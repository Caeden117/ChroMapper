using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;

public static class SliderCommand
{
    public static BaseSlider InvertColor(BaseSlider baseSlider)
    {
        var newSlider = BeatmapFactory.Clone(baseSlider);
        newSlider.Color = baseSlider.Color == (int)NoteColor.Red
            ? (int)NoteColor.Blue
            : (int)NoteColor.Red;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(newSlider, baseSlider, "invert arc color", preserveSelection: true),
            true);
        return newSlider;
    }
}

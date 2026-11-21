using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Info;
using Beatmap.V3;
using ZLinq;

/// <summary>
/// Suggests ChromaGLS for extended GLS easings, custom GLS colours, and The Second environment ring zoom.
/// These features are implemented by ChromaGLS rather than the regular Chroma plugin.
/// </summary>
public class ChromaGLSReq : RequirementCheck
{
    public override string Name => "ChromaGLS";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map) =>
        HasExtendedGLSEasings(map) || HasChromaGLSEvents(map) || HasSmoothStepRingZoomOverride(map)
            ? RequirementType.Suggestion
            : RequirementType.None;

    private static bool HasExtendedGLSEasings(BaseDifficulty map)
    {
        var version = Settings.Instance.MapVersion;
        if (version != 3 && version != 4)
            return false;

        return HasExtendedGLSEasings(map.LightColorEventBoxGroups, version == 3)
            || HasExtendedGLSEasings(map.LightRotationEventBoxGroups, false)
            || HasExtendedGLSEasings(map.LightTranslationEventBoxGroups, false)
            || HasExtendedGLSEasings(map.VfxEventBoxGroups, false);
    }

    private static bool HasExtendedGLSEasings(IReadOnlyList<BaseEventBoxGroup> groups, bool usesV3ColorExtension)
    {
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            var boxes = groups[groupIndex].ReadOnlyBoxes;
            for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
            {
                var box = boxes[boxIndex];
                if (IsExtendedGameEasing(box.Easing))
                    return true;

                var events = box.ReadOnlyEvents;
                for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
                {
                    var easing = events[eventIndex].Easing;

                    if (usesV3ColorExtension
                        ? V3LightColorBase.RequiresCustomEasing(easing)
                        : IsExtendedGameEasing(easing))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsExtendedGameEasing(int easing) =>
        easing >= (int)EaseType.InSinusoidal && easing <= (int)EaseType.InOutExponential;

    private static bool HasChromaGLSEvents(BaseDifficulty map) =>
        map.LightColorEventBoxGroups
            .AsValueEnumerable()
            .SelectMany(group => group.Boxes)
            .Any(box => box.ColorDistributions.Length > 0
                || box.StrobeColorDistributions.Length > 0
                || box.Events.AsValueEnumerable().Any(lightEvent => lightEvent.IsChroma()));

    private static bool HasSmoothStepRingZoomOverride(BaseDifficulty map)
    {
        if (map.RuntimeTrackDefinitions == null)
            return false;

        // SmoothStepRingZoom only applies to The Second's legacy ring right now.
        return map.Events.AsValueEnumerable().Any(
            basicEvent => basicEvent.CustomStep.HasValue
                && map.RuntimeTrackDefinitions.GetBasicOrDefault(basicEvent.Type).Components
                    .HasFlag(BasicEventComponent.SmoothStepRingZoom));
    }
}

using Beatmap.Base;
using Beatmap.Info;
using ZLinq;

/// <summary>
/// Detects custom RGB data on Group Lighting System colour events.
/// GLS colours are implemented by ChromaGLS rather than the regular Chroma plugin.
/// </summary>
public class ChromaGLSReq : RequirementCheck
{
    public override string Name => "ChromaGLS";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map) =>
        HasChromaGLSEvents(map) || HasSmoothStepRingZoomOverride(map)
            ? RequirementType.Suggestion
            : RequirementType.None;

    private static bool HasChromaGLSEvents(BaseDifficulty map) =>
        map.LightColorEventBoxGroups
            .AsValueEnumerable()
            .SelectMany(group => group.Boxes)
            .SelectMany(box => box.Events)
            .Any(lightEvent => lightEvent.IsChroma());

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

using Beatmap.Base;
using Beatmap.Info;

// BeatToTheFuture owns the flat V3 VNJS runtime path, so preserved events require that capability.
public class BeatToTheFutureReq : RequirementCheck
{
    public override string Name => "BeatToTheFuture";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map)
    {
        // Saved V3 VNJS and raw y=3/4 walls need the backport, so they remain stronger than the ring-speed suggestion.
        if (Settings.Instance.MapVersion == 3
            && ((map.SaveVNJSEventsInV3 && map.NJSEvents.Count > 0) || map.HasV4UpperWalls()))
        {
            return RequirementType.Requirement;
        }

        if (RingPropagationCompatibility.HasOldPropagationDeclaration(infoDifficulty.CustomData)
            || RingPropagationCompatibility.HasOldPropagationDeclaration(map.RuntimeLevelCustomData)
            || RingPropagationCompatibility.HasOldPropagationDeclaration(map.CustomData))
        {
            return infoDifficulty.CustomRequirements.Contains(Name)
                ? RequirementType.Requirement
                : RequirementType.Suggestion;
        }

        return RequirementType.None;
    }
}

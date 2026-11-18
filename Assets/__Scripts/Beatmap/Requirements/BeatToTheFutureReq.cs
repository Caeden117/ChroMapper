using Beatmap.Base;
using Beatmap.Info;
using Beatmap.Shared;

// V3 variable note jump speed and the V4 wall backport require BeatToTheFuture.
public class BeatToTheFutureReq : RequirementCheck
{
    public override string Name => "BeatToTheFuture";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map)
    {
        if (Settings.Instance.MapVersion == 3
            && ((map.SaveVNJSEventsInV3 && map.NJSEvents.Count > 0) || map.HasV4UpperWalls()))
        {
            return RequirementType.Requirement;
        }

        if (RingPropagationCompatibility.HasOldPropagationDeclaration(infoDifficulty.CustomData)
            || RingPropagationCompatibility.HasOldPropagationDeclaration(map.RuntimeLevelCustomData)
            || RingPropagationCompatibility.HasOldPropagationDeclaration(map.CustomData)
            || HasTrueHsvEvents(map))
        {
            return RequirementType.Suggestion;
        }

        return RequirementType.None;
    }

    private static bool HasTrueHsvEvents(BaseDifficulty map)
    {
        foreach (var evt in map.Events)
        {
            if (BasicEventColorLerp.FromSerializedName(evt.CustomLerpType) == BasicEventColorLerpType.TrueHSV)
                return true;
        }

        return false;
    }
}

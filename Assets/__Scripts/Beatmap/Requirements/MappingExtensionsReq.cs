using System.Linq;
using Beatmap.Base;
using Beatmap.Info;

public class MappingExtensionsReq : RequirementCheck
{
    public override string Name => "Mapping Extensions";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map)
    {
        if (infoDifficulty is null) return RequirementType.None;
        // BeatToTheFuture directly supports V4 y=3/4 walls saved in V3, so exempt only those coordinates while preserving
        // Mapping Extensions detection for all other objects and out-of-range wall properties.
        var allowV4UpperWallsInV3 = Settings.Instance.MapVersion == 3 && map.HasV4UpperWalls();
        return map.IsMappingExtensions(allowV4UpperWallsInV3)
            ? RequirementType.Requirement
            : RequirementType.None;
    }
}

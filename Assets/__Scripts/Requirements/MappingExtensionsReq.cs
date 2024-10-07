using System.Linq;
using Beatmap.Base;
using Beatmap.Info;

public class MappingExtensionsReq : RequirementCheck
{
    public override string Name => "Mapping Extensions";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map)
    {
        if (infoDifficulty is null) return RequirementType.None;
        return map.IsMappingExtensions()
            ? RequirementType.Requirement
            : RequirementType.None;
    }
}

using System.Linq;
using Beatmap.Base;

public class MappingExtensionsReq : RequirementCheck
{
    public override string Name => "Mapping Extensions";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BaseDifficulty map)
    {
        if (mapInfo is null) return RequirementType.None;
        return map.IsMappingExtensions()
            ? RequirementType.Requirement
            : RequirementType.None;
    }
}

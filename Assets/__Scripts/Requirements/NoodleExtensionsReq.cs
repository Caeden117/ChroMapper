using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

public class NoodleExtensionsReq : RequirementCheck
{
    private static readonly HashSet<string> customEventsToModRequirements = new HashSet<string>
    {
        "AnimateTrack", "AssignPathAnimation", "AssignTrackParent", "AssignPlayerToTrack"
    };

    public override string Name => "Noodle Extensions";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, IDifficulty map)
    {
        if (mapInfo is null) return RequirementType.None;
        return map.IsNoodleExtensions()
            ? RequirementType.Requirement
            : RequirementType.None;
    }
}

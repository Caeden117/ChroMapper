using System.Collections.Generic;
using System.Linq;

public class NoodleExtensionsReq : RequirementCheck
{
    private static readonly HashSet<string> CustomEventsToModRequirements = new HashSet<string>
    {
        "AnimateTrack", "AssignPathAnimation", "AssignTrackParent", "AssignPlayerToTrack"
    };

    public override string Name => "Noodle Extensions";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        if (mapInfo is null) return RequirementType.None;

        // idk why the customdata checks should be necessary, but they are.
        return map.Obstacles.Any(ob => ob.CustomData?["_position"] != null || ob.CustomData?["_scale"] != null ||
                                        ob.CustomData?["_rotation"] != null ||
                                        ob.CustomData?["_localRotation"] != null ||
                                        ob.CustomData?["_animation"] != null) ||
               map.Notes.Any(ob => ob.CustomData?["_position"] != null || ob.CustomData?["_cutDirection"] != null ||
                                    ob.CustomData?["_fake"] != null || ob.CustomData?["_interactable"] != null ||
                                    ob.CustomData?["_animation"] != null) ||
               map.CustomEvents.Any(ob => CustomEventsToModRequirements.Contains(ob.Type))
            ? RequirementType.Requirement
            : RequirementType.None;
    }
}

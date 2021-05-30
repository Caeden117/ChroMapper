using System.Collections.Generic;
using System.Linq;

public class NoodleExtensionsReq : RequirementCheck
{
    private static readonly HashSet<string> CustomEventsToModRequirements = new HashSet<string>()
    {
         "AnimateTrack", "AssignPathAnimation", "AssignTrackParent", "AssignPlayerToTrack"
    };
    
    public override string Name => "Noodle Extensions";
    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        if (mapInfo is null) return RequirementType.None;

        // idk why the customdata checks should be necessary, but they are.
        return map._obstacles.Any(ob => ob._customData?["_position"] != null || ob._customData?["_scale"] != null ||
                                        ob._customData?["_rotation"] != null || ob._customData?["_localRotation"] != null ||
                                        ob._customData?["_animation"] != null) || 
               map._notes.Any(ob => ob._customData?["_position"] != null || ob._customData?["_cutDirection"] != null ||
                                    ob._customData?["_fake"] != null || ob._customData?["_interactable"] != null ||
                                    ob._customData?["_animation"] != null) ||
               map._customEvents.Any(ob => CustomEventsToModRequirements.Contains(ob._type)) ? RequirementType.Requirement : RequirementType.None;
    }
}
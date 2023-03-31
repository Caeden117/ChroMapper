using System.Collections.Generic;
using System.Linq;

public class NoodleExtensionsReq : RequirementCheck
{
    private static readonly HashSet<string> customEventsToModRequirements = new HashSet<string>
    {
        "AnimateTrack", "AssignPathAnimation", "AssignTrackParent", "AssignPlayerToTrack"
    };

    public override string Name => "Noodle Extensions";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        if (mapInfo is null) return RequirementType.None;

        // idk why the customdata checks should be necessary, but they are.
        return map.Obstacles.Any(ob => ob.CustomData?[MapLoader.heckPosition] != null || ob.CustomData?[MapLoader.heckScale] != null ||
                                        ob.CustomData?[MapLoader.heckRotation] != null ||
                                        ob.CustomData?[MapLoader.heckUnderscore + "localRotation"] != null ||
                                        ob.CustomData?["_animation"] != null) ||
               map.Notes.Any(ob => ob.CustomData?[MapLoader.heckPosition] != null || ob.CustomData?["_cutDirection"] != null ||
                                    ob.CustomData?["_fake"] != null || ob.CustomData?[MapLoader.heckInteractable] != null ||
                                    ob.CustomData?["_animation"] != null) ||
               map.CustomEvents.Any(ob => customEventsToModRequirements.Contains(ob.Type))
            ? RequirementType.Requirement
            : RequirementType.None;
    }
}

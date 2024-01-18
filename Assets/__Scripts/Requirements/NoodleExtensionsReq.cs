using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.V3;

public class NoodleExtensionsReq : HeckRequirementCheck
{
    public override string Name => "Noodle Extensions";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BaseDifficulty map)
    {
        if (mapInfo is null) return RequirementType.None;
        return (map.IsNoodleExtensions() || HasNoodleTracks(map))
            ? RequirementType.Requirement
            : RequirementType.None;
    }

    private static readonly List<string> noodleSpecificTrackTypes =
        new List<string> { "AssignTrackParent", "AssignPlayerToTrack" };

    private static readonly List<string> v3NoodleAnimationKeys = new List<string>
    {
        "offsetPosition",
        "offsetWorldRotation",
        "localRotation",
        "scale",
        "dissolve",
        "dissolveArrow",
        "interactable",
        "definitePosition",
        "time",
        "position",
        "localPosition",
        "rotation",
        "localRotation"
    };

    private static readonly List<string> v2NoodleAnimationKeys = new List<string>
    {
        "_position",
        "_rotation",
        "_localRotation",
        "_scale",
        "_dissolve",
        "_dissolveArrow",
        "_interactable",
        "_definitePosition",
        "_time",
        "_position",
        "_localPosition",
        "_rotation",
        "_localRotation"
    };

    private bool HasNoodleTracks(BaseDifficulty map)
    {
        var noodleAnimationKeys = map is V3Difficulty
            ? v3NoodleAnimationKeys
            : v2NoodleAnimationKeys;
        return HasAnimationsFromMod(map, noodleSpecificTrackTypes, noodleAnimationKeys);
    }
}

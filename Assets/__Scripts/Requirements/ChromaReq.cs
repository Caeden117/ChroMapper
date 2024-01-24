using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.V3;

public class ChromaReq : HeckRequirementCheck
{
    public override string Name => "Chroma";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BaseDifficulty map)
    {
        if (mapInfo != null && (HasEnvironmentRemoval(mapInfo, map) || HasChromaEvents(map) || HasChromaTracks(map)))
            return RequiresChroma(mapInfo, map) ? RequirementType.Requirement : RequirementType.Suggestion;

        return RequirementType.None;
    }

    private bool HasChromaEvents(BaseDifficulty map) =>
        map.IsChroma();

    private bool RequiresChroma(BeatSaberSong.DifficultyBeatmap mapInfo, BaseDifficulty map) =>
        mapInfo.CustomData != null && mapInfo.CustomData.HasKey("_requirements") &&
        mapInfo.CustomData["_requirements"].Linq.Any(x => x.Value == "Chroma");

    private bool HasEnvironmentRemoval(BeatSaberSong.DifficultyBeatmap mapInfo, BaseDifficulty map) =>
        (mapInfo.CustomData != null && mapInfo.CustomData.HasKey("_environmentRemoval") &&
         mapInfo.CustomData["_environmentRemoval"].AsArray.Count > 0) ||
        (map.MainNode.HasKey("_customData") && map.MainNode["_customData"] != null &&
         map.MainNode["_customData"].HasKey("_environment") &&
         map.MainNode["_customData"]["_environment"].AsArray.Count > 0);
    
    private static readonly List<string> chromaSpecificTrackTypes = new List<string> { "AnimateComponent" };

    private static readonly List<string> v3ChromaAnimationKeys = new List<string> { "color" };

    private static readonly List<string> v2ChromaAnimationKeys = new List<string> { "_color" };

    private bool HasChromaTracks(BaseDifficulty map)
    {
        var chromaAnimationKeys = map is V3Difficulty
            ? v3ChromaAnimationKeys
            : v2ChromaAnimationKeys;
        return HasAnimationsFromMod(map, chromaSpecificTrackTypes, chromaAnimationKeys);
    }
}

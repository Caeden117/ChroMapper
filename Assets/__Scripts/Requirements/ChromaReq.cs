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
        (map.CustomData != null &&
         map.CustomData.HasKey("_environment") &&
         map.CustomData["_environment"].AsArray.Count > 0);
    
    private static readonly List<string> chromaSpecificTrackTypes = new List<string> { "AnimateComponent" };

    private static readonly List<string> v3ChromaAnimationKeys = new List<string> { "color" };

    private static readonly List<string> v2ChromaAnimationKeys = new List<string> { "_color" };

    private bool HasChromaTracks(BaseDifficulty map)
    {
        var chromaAnimationKeys = Settings.Instance.MapVersion switch
        {
            3 => v3ChromaAnimationKeys,
            2 => v2ChromaAnimationKeys,
            _ => new List<string>(),
        };
        return HasAnimationsFromMod(map, chromaSpecificTrackTypes, chromaAnimationKeys);
    }
}

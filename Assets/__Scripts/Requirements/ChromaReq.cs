using System.Linq;

public class ChromaReq : RequirementCheck
{
    public override string Name => "Chroma";
    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        if (mapInfo != null && (HasEnvironmentRemoval(mapInfo, map) || HasChromaEvents(map)))
        {
            return RequiresChroma(mapInfo, map) ? RequirementType.Requirement : RequirementType.Suggestion;
        }

        return RequirementType.None;
    }

    //Bold assumption for events, but so far Chroma is the only mod that uses Custom Data in vanilla events.
    private bool HasChromaEvents(BeatSaberMap map) =>
        map._notes.Any(note => note._customData?["_color"] != null) ||
        map._obstacles.Any(ob => ob._customData?["_color"] != null) ||
        map._events.Any(ob => ob._customData != null);

    private bool RequiresChroma(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map) =>
        mapInfo.customData != null && mapInfo.customData.HasKey("_requirements") && mapInfo.customData["_requirements"].Linq.Any(x => x.Value == "Chroma");

    private bool HasEnvironmentRemoval(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map) =>
        (mapInfo.customData != null && mapInfo.customData.HasKey("_environmentRemoval") && mapInfo.customData["_environmentRemoval"].AsArray.Count > 0) ||
        (map.mainNode.HasKey("_customData") && map.mainNode["_customData"] != null &&
            map.mainNode["_customData"].HasKey("_environment") && map.mainNode["_customData"]["_environment"].AsArray.Count > 0);
}
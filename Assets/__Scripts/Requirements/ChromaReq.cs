using System.Linq;
using Beatmap.Base;

public class ChromaReq : RequirementCheck
{
    public override string Name => "Chroma";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, IDifficulty map)
    {
        if (mapInfo != null && (HasEnvironmentRemoval(mapInfo, map) || HasChromaEvents(map)))
            return RequiresChroma(mapInfo, map) ? RequirementType.Requirement : RequirementType.Suggestion;

        return RequirementType.None;
    }

    //Bold assumption for events, but so far Chroma is the only mod that uses Custom Data in vanilla events.
    private bool HasChromaEvents(IDifficulty map) =>
        map.Notes.Any(note => note.CustomData?["_color"] != null) ||
        map.Obstacles.Any(ob => ob.CustomData?["_color"] != null) ||
        map.Events.Any(ob => ob.CustomData != null);

    private bool RequiresChroma(BeatSaberSong.DifficultyBeatmap mapInfo, IDifficulty map) =>
        mapInfo.CustomData != null && mapInfo.CustomData.HasKey("_requirements") &&
        mapInfo.CustomData["_requirements"].Linq.Any(x => x.Value == "Chroma");

    private bool HasEnvironmentRemoval(BeatSaberSong.DifficultyBeatmap mapInfo, IDifficulty map) =>
        (mapInfo.CustomData != null && mapInfo.CustomData.HasKey("_environmentRemoval") &&
         mapInfo.CustomData["_environmentRemoval"].AsArray.Count > 0) ||
        (map.MainNode.HasKey("_customData") && map.MainNode["_customData"] != null &&
         map.MainNode["_customData"].HasKey("_environment") &&
         map.MainNode["_customData"]["_environment"].AsArray.Count > 0);
}

using System.IO;
using Beatmap.Base;

public class CinemaReq : RequirementCheck
{
    public override string Name => "Cinema";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BaseDifficulty map)
    {
        var path = Path.Combine(BeatSaberSongContainer.Instance.Info.Directory, "cinema-video.json");
        return File.Exists(path) ? RequirementType.Suggestion : RequirementType.None;
    }
}

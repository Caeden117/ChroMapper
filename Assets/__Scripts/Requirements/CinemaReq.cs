using System.IO;

public class CinemaReq : RequirementCheck
{
    public override string Name => "Cinema";
    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        var path = Path.Combine(BeatSaberSongContainer.Instance.song.directory, "cinema-video.json");
        return File.Exists(path) ? RequirementType.Suggestion : RequirementType.None;
    }
}
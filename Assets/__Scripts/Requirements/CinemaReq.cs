using System.IO;
using Beatmap.Base;
using Beatmap.Info;

public class CinemaReq : RequirementCheck
{
    public override string Name => "Cinema";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty _, BaseDifficulty __)
    {
        var path = Path.Combine(BeatSaberSongContainer.Instance.Info.Directory, "cinema-video.json");
        return File.Exists(path) ? RequirementType.Suggestion : RequirementType.None;
    }
}

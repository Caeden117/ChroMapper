public static class EnvironmentInfoHelper
{
    public static string GetName() =>
        BeatSaberSongContainer.Instance.MapDifficultyInfo.Characteristic == "90Degree" ||
        BeatSaberSongContainer.Instance.MapDifficultyInfo.Characteristic == "360Degree"
            ? BeatSaberSongContainer.Instance.Info.AllDirectionsEnvironmentName
            : BeatSaberSongContainer.Instance.Info.EnvironmentName;
}

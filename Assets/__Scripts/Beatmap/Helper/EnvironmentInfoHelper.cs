public static class EnvironmentInfoHelper
{
    public static string GetName()
    {
        return
            BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet.BeatmapCharacteristicName == "90Degree" ||
            BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet.BeatmapCharacteristicName == "360Degree"
                ? BeatSaberSongContainer.Instance.Song.AllDirectionsEnvironmentName
                : BeatSaberSongContainer.Instance.Song.EnvironmentName;
    }
}
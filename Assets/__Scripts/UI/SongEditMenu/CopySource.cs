using static BeatSaberSong;

public class CopySource
{
    public CopySource(DifficultySettings difficultySettings, DifficultyBeatmapSet characteristic, DifficultyRow obj)
    {
        DifficultySettings = difficultySettings;
        Characteristic = characteristic;
        Obj = obj;
    }

    public DifficultySettings DifficultySettings { get; }
    public DifficultyBeatmapSet Characteristic { get; }
    public DifficultyRow Obj { get; }
}

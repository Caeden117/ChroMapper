using static BeatSaberSong;

public class CopySource
{
    public DifficultySettings DifficultySettings { get; private set; }
    public DifficultyBeatmapSet Characteristic { get; private set; }
    public DifficultyRow Obj { get; private set; }

    public CopySource(DifficultySettings difficultySettings, DifficultyBeatmapSet characteristic, DifficultyRow obj)
    {
        DifficultySettings = difficultySettings;
        Characteristic = characteristic;
        Obj = obj;
    }
}
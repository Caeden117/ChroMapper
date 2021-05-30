using System.IO;
using System.Linq;

public class SoundExtensionsReq : RequirementCheck
{
    public override string Name => "Sound Extensions";
    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map)
    {
        return HasSounds(mapInfo) || HasNotesWithCustomSounds(map) ? RequirementType.Suggestion : RequirementType.None;
    }

    private bool HasSounds(BeatSaberSong.DifficultyBeatmap mapInfo) => mapInfo.customData != null && mapInfo.customData.HasKey("_sounds");

    private bool HasNotesWithCustomSounds(BeatSaberMap map) =>
        map._notes.Any(note => note._customData != null && (note._customData.HasKey("_soundID") || note._customData.HasKey("_soundHitID") ||
                                                            note._customData.HasKey("_soundHitVolume") || note._customData.HasKey("_soundMissVolume")));
}
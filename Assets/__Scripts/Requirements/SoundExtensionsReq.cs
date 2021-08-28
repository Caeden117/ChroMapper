﻿using System.Linq;

public class SoundExtensionsReq : RequirementCheck
{
    public override string Name => "Sound Extensions";

    public override RequirementType IsRequiredOrSuggested(BeatSaberSong.DifficultyBeatmap mapInfo, BeatSaberMap map) =>
        HasSounds(mapInfo) || HasNotesWithCustomSounds(map) ? RequirementType.Suggestion : RequirementType.None;

    private bool HasSounds(BeatSaberSong.DifficultyBeatmap mapInfo) =>
        mapInfo.CustomData != null && mapInfo.CustomData.HasKey("_sounds");

    private bool HasNotesWithCustomSounds(BeatSaberMap map) =>
        map.Notes.Any(note => note.CustomData != null &&
                               (note.CustomData.HasKey("_soundID") || note.CustomData.HasKey("_soundHitID") ||
                                note.CustomData.HasKey("_soundHitVolume") ||
                                note.CustomData.HasKey("_soundMissVolume")));
}

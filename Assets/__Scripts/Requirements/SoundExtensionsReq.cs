using System.Linq;
using Beatmap.Base;
using Beatmap.Info;

public class SoundExtensionsReq : RequirementCheck
{
    public override string Name => "Sound Extensions";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map) =>
        HasSounds(infoDifficulty) || HasNotesWithCustomSounds(map) ? RequirementType.Suggestion : RequirementType.None;

    // Mod currently doesn't support v4 so this fine as is
    private bool HasSounds(InfoDifficulty infoDifficulty) =>
        infoDifficulty.CustomData != null && infoDifficulty.CustomData.HasKey("_sounds");

    private bool HasNotesWithCustomSounds(BaseDifficulty map) =>
        map.Notes.Any(note => note.CustomData != null &&
                               (note.CustomData.HasKey("_soundID") || note.CustomData.HasKey("_soundHitID") ||
                                note.CustomData.HasKey("_soundHitVolume") ||
                                note.CustomData.HasKey("_soundMissVolume")));
}

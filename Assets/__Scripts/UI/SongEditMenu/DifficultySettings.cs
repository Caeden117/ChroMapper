using SimpleJSON;
using static BeatSaberSong;

public class DifficultySettings
{
    public float NoteJumpMovementSpeed = 16;
    public float NoteJumpStartBeatOffset = 0;
    public string CustomName = "";
    public bool ForceDirty = false;

    public DifficultyBeatmap DifficultyBeatmap { get; private set; }

    public DifficultySettings(DifficultyBeatmap difficultyBeatmap)
    {
        if (difficultyBeatmap.customData == null)
            difficultyBeatmap.customData = new JSONObject();

        this.DifficultyBeatmap = difficultyBeatmap;
        Revert();
    }

    public DifficultySettings(DifficultyBeatmap difficultyBeatmap, bool ForceDirty) : this(difficultyBeatmap)
    {
        this.ForceDirty = ForceDirty;
    }

    public bool IsDirty()
    {
        return ForceDirty || NoteJumpMovementSpeed != DifficultyBeatmap.noteJumpMovementSpeed ||
            NoteJumpStartBeatOffset != DifficultyBeatmap.noteJumpStartBeatOffset ||
            !(CustomName ?? "").Equals(DifficultyBeatmap.customData["_difficultyLabel"].Value);
    }

    public void Commit()
    {
        ForceDirty = false;

        DifficultyBeatmap.noteJumpMovementSpeed = NoteJumpMovementSpeed;
        DifficultyBeatmap.noteJumpStartBeatOffset = NoteJumpStartBeatOffset;

        if (CustomName == null || CustomName.Length == 0)
        {
            DifficultyBeatmap.customData.Remove("_difficultyLabel");
        }
        else
        {
            DifficultyBeatmap.customData["_difficultyLabel"] = CustomName;
        }
    }

    public void Revert()
    {
        NoteJumpMovementSpeed = DifficultyBeatmap.noteJumpMovementSpeed;
        NoteJumpStartBeatOffset = DifficultyBeatmap.noteJumpStartBeatOffset;
        CustomName = DifficultyBeatmap.customData["_difficultyLabel"].Value;
    }
}
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using static BeatSaberSong;

/// <summary>
/// Holds users changes to a difficulty until they ask to save them
/// </summary>
public class DifficultySettings
{
    public float NoteJumpMovementSpeed = 16;
    public float NoteJumpStartBeatOffset = 0;
    public string CustomName = "";
    public List<string> envRemoval = new List<string>();
    public bool ForceDirty = false;

    public DifficultyBeatmap DifficultyBeatmap { get; private set; }

    public DifficultySettings(DifficultyBeatmap difficultyBeatmap)
    {
        difficultyBeatmap.GetOrCreateCustomData();
        DifficultyBeatmap = difficultyBeatmap;
        Revert();
    }

    public DifficultySettings(DifficultyBeatmap difficultyBeatmap, bool ForceDirty) : this(difficultyBeatmap)
    {
        this.ForceDirty = ForceDirty;
    }

    /// <summary>
    /// Check if the user has made changes
    /// </summary>
    /// <returns>True if changes have been made, false otherwise</returns>
    public bool IsDirty()
    {
        return ForceDirty || NoteJumpMovementSpeed != DifficultyBeatmap.noteJumpMovementSpeed ||
            NoteJumpStartBeatOffset != DifficultyBeatmap.noteJumpStartBeatOffset ||
            !(CustomName ?? "").Equals(DifficultyBeatmap.customData == null ? "" : DifficultyBeatmap.customData["_difficultyLabel"].Value) ||
            EnvRemovalChanged();
    }

    private bool EnvRemovalChanged()
    {
        if (DifficultyBeatmap.customData == null || !DifficultyBeatmap.customData["_environmentRemoval"].IsArray)
        {
            return envRemoval.Count > 0;
        }

        var envLocal = new List<string>();
        foreach (var ent in DifficultyBeatmap.customData["_environmentRemoval"].AsArray)
        {
            if (ent.Value.IsString)
                envLocal.Add(ent.Value.Value);
        }

        var distinctEnvLocal = envLocal.Distinct().ToArray();
        return distinctEnvLocal.Length != envRemoval.Count() || !distinctEnvLocal.All(it => envRemoval.Contains(it));
    }

    /// <summary>
    /// Save the users changes to the backing DifficultyBeatmap object
    /// </summary>
    public void Commit()
    {
        ForceDirty = false;

        DifficultyBeatmap.noteJumpMovementSpeed = NoteJumpMovementSpeed;
        DifficultyBeatmap.noteJumpStartBeatOffset = NoteJumpStartBeatOffset;

        if (string.IsNullOrEmpty(CustomName))
        {
            DifficultyBeatmap.customData?.Remove("_difficultyLabel");
        }
        else
        {
            DifficultyBeatmap.GetOrCreateCustomData()["_difficultyLabel"] = CustomName;
        }

        if (envRemoval.Count == 0)
        {
            DifficultyBeatmap.customData?.Remove("_environmentRemoval");
        }
        else
        {
            var envArr = new JSONArray();
            foreach (var ent in envRemoval)
            {
                envArr.Add(ent);
            }

            DifficultyBeatmap.GetOrCreateCustomData()["_environmentRemoval"] = envArr;
        }
    }

    /// <summary>
    /// Discard any changes from the user
    /// </summary>
    public void Revert()
    {
        NoteJumpMovementSpeed = DifficultyBeatmap.noteJumpMovementSpeed;
        NoteJumpStartBeatOffset = DifficultyBeatmap.noteJumpStartBeatOffset;
        CustomName = DifficultyBeatmap.customData["_difficultyLabel"].Value;

        envRemoval.Clear();
        foreach (var ent in DifficultyBeatmap.customData["_environmentRemoval"])
        {
            if (!envRemoval.Contains(ent.Value.Value))
                envRemoval.Add(ent.Value.Value);
        }
    }
}
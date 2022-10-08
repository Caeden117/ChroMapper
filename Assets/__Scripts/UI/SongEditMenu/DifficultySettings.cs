using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.V2.Customs;
using static BeatSaberSong;

/// <summary>
///     Holds users changes to a difficulty until they ask to save them
/// </summary>
public class DifficultySettings
{
    private List<BaseEnvironmentEnhancement> envEnhancements;
    private BaseDifficulty map;
    public string CustomName = "";
    public bool ForceDirty;
    public float NoteJumpMovementSpeed = 16;
    public float NoteJumpStartBeatOffset;

    public DifficultySettings(DifficultyBeatmap difficultyBeatmap)
    {
        difficultyBeatmap.GetOrCreateCustomData();
        DifficultyBeatmap = difficultyBeatmap;

        Revert();
    }

    public DifficultySettings(DifficultyBeatmap difficultyBeatmap, bool forceDirty) : this(difficultyBeatmap) =>
        ForceDirty = forceDirty;

    public BaseDifficulty Map
    {
        get
        {
            map ??= BeatSaberSongContainer.Instance.Song.GetMapFromDifficultyBeatmap(DifficultyBeatmap);
            return map;
        }
    }

    public List<BaseEnvironmentEnhancement> EnvEnhancements
    {
        get
        {
            envEnhancements ??= GetEnvEnhancementsFromMap();
            return envEnhancements;
        }
        set => envEnhancements = value;
    }

    public DifficultyBeatmap DifficultyBeatmap { get; }

    /// <summary>
    ///     Check if the user has made changes
    /// </summary>
    /// <returns>True if changes have been made, false otherwise</returns>
    public bool IsDirty() =>
        ForceDirty || NoteJumpMovementSpeed != DifficultyBeatmap.NoteJumpMovementSpeed ||
        NoteJumpStartBeatOffset != DifficultyBeatmap.NoteJumpStartBeatOffset ||
        !(CustomName ?? "").Equals(DifficultyBeatmap.CustomData == null
            ? ""
            : DifficultyBeatmap.CustomData["_difficultyLabel"].Value) ||
        EnvRemovalChanged();

    private bool EnvRemovalChanged() =>
        envEnhancements != null && Map != null &&
        !(Map.EnvironmentEnhancements.All(envEnhancements.Contains) && Map.EnvironmentEnhancements.Count == envEnhancements.Count);

    /// <summary>
    ///     Save the users changes to the backing DifficultyBeatmap object
    /// </summary>
    public void Commit()
    {
        ForceDirty = false;

        DifficultyBeatmap.NoteJumpMovementSpeed = NoteJumpMovementSpeed;
        DifficultyBeatmap.NoteJumpStartBeatOffset = NoteJumpStartBeatOffset;

        if (string.IsNullOrEmpty(CustomName))
            DifficultyBeatmap.CustomData?.Remove("_difficultyLabel");
        else
            DifficultyBeatmap.GetOrCreateCustomData()["_difficultyLabel"] = CustomName;

        DifficultyBeatmap.CustomData?.Remove("_environmentRemoval");

        // Map save is sloooow so only do it if we need to
        if (EnvRemovalChanged())
        {
            Map.EnvironmentEnhancements = envEnhancements;
            Map.Save();
        }
    }

    private List<BaseEnvironmentEnhancement> GetEnvEnhancementsFromMap()
    {
        var enhancements = new List<BaseEnvironmentEnhancement>();
        if (DifficultyBeatmap.CustomData != null)
        {
            // TODO: check for v3 env enhancement
            foreach (var ent in DifficultyBeatmap.CustomData["_environmentRemoval"])
                enhancements.Add(new V2EnvironmentEnhancement(ent.Value.Value));
        }

        if (Map != null) enhancements.AddRange(Map.EnvironmentEnhancements.Select(it => it.Clone() as BaseEnvironmentEnhancement));

        return enhancements;
    }

    /// <summary>
    ///     Discard any changes from the user
    /// </summary>
    public void Revert()
    {
        NoteJumpMovementSpeed = DifficultyBeatmap.NoteJumpMovementSpeed;
        NoteJumpStartBeatOffset = DifficultyBeatmap.NoteJumpStartBeatOffset;
        CustomName = DifficultyBeatmap.CustomData["_difficultyLabel"].Value;

        envEnhancements = null;
    }
}

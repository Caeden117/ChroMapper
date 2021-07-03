using System;
using System.Collections.Generic;
using System.Linq;
using static BeatSaberSong;

/// <summary>
/// Holds users changes to a difficulty until they ask to save them
/// </summary>
public class DifficultySettings
{
    public float NoteJumpMovementSpeed = 16;
    public float NoteJumpStartBeatOffset = 0;
    public string CustomName = "";
    private List<EnvEnhancement> _envEnhancements = null;
    public bool ForceDirty = false;
    private readonly Lazy<BeatSaberMap> _map;
    public BeatSaberMap Map => _map.Value;

    public List<EnvEnhancement> EnvEnhancements
    {
        get
        {
            _envEnhancements = _envEnhancements ?? GetEnvEnhancementsFromMap();
            return _envEnhancements;
        }
        set => _envEnhancements = value;
    }

    public DifficultyBeatmap DifficultyBeatmap { get; private set; }

    public DifficultySettings(DifficultyBeatmap difficultyBeatmap)
    {
        difficultyBeatmap.GetOrCreateCustomData();
        DifficultyBeatmap = difficultyBeatmap;

        _map = new Lazy<BeatSaberMap>(() => BeatSaberSongContainer.Instance.song.GetMapFromDifficultyBeatmap(DifficultyBeatmap));

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
        return _envEnhancements != null && _map.Value != null &&
               !(_map.Value._envEnhancements.All(_envEnhancements.Contains) && _map.Value._envEnhancements.Count == _envEnhancements.Count);
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

        DifficultyBeatmap.customData?.Remove("_environmentRemoval");

        // Map save is sloooow so only do it if we need to
        if (EnvRemovalChanged())
        {
            _map.Value._envEnhancements = _envEnhancements;
            _map.Value.Save();
        }
    }

    private List<EnvEnhancement> GetEnvEnhancementsFromMap()
    {
        var enhancements = new List<EnvEnhancement>();
        foreach (var ent in DifficultyBeatmap.customData["_environmentRemoval"])
        {
            enhancements.Add(new EnvEnhancement(ent.Value.Value));
        }

        if (_map.Value != null)
        {
            enhancements.AddRange(_map.Value._envEnhancements);
        }

        return enhancements;
    }

    /// <summary>
    /// Discard any changes from the user
    /// </summary>
    public void Revert()
    {
        NoteJumpMovementSpeed = DifficultyBeatmap.noteJumpMovementSpeed;
        NoteJumpStartBeatOffset = DifficultyBeatmap.noteJumpStartBeatOffset;
        CustomName = DifficultyBeatmap.customData["_difficultyLabel"].Value;

        _envEnhancements = null;
    }
}
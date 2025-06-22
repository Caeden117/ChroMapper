using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Info;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using Beatmap.V4;

/// <summary>
///     Holds users changes to a difficulty until they ask to save them
/// </summary>
public class DifficultySettings
{
    private List<string> songCoreInfos;
    private List<string> songCoreWarnings;
    private List<BaseEnvironmentEnhancement> envEnhancements;
    private BaseDifficulty map;

    public InfoDifficulty InfoDifficulty { get; }
    public string CustomName = "";
    public bool ForceDirty;
    public float NoteJumpMovementSpeed = 16;
    public float NoteJumpStartBeatOffset;
    
    public int EnvironmentNameIndex;
    public string EnvironmentName;
    
    // v4 fields
    public string Mappers;
    public string Lighters;
    public string LightshowFilePath;

    public DifficultySettings(InfoDifficulty infoDifficulty)
    {
        InfoDifficulty = infoDifficulty;

        Revert();
    }

    public DifficultySettings(InfoDifficulty infoDifficulty, bool forceDirty) : this(infoDifficulty) =>
        ForceDirty = forceDirty;

    public BaseDifficulty Map
    {
        get
        {
            map ??= BeatSaberSongUtils.GetMapFromInfoFiles(BeatSaberSongContainer.Instance.Info, InfoDifficulty);
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

    public List<string> SongCoreInfos
    {
        get
        {
            songCoreInfos ??= InfoDifficulty.CustomInformation.ToList();
            return songCoreInfos;
        }
        set => songCoreInfos = value;
    }
    
    public List<string> SongCoreWarnings
    {
        get
        {
            songCoreWarnings ??= InfoDifficulty.CustomWarnings.ToList();
            return songCoreWarnings;
        }
        set => songCoreWarnings = value;
    }

    /// <summary>
    ///     Check if the user has made changes
    /// </summary>
    /// <returns>True if changes have been made, false otherwise</returns>
    public bool IsDirty() =>
        ForceDirty ||
        NoteJumpMovementSpeed != InfoDifficulty.NoteJumpSpeed ||
        NoteJumpStartBeatOffset != InfoDifficulty.NoteStartBeatOffset ||
        EnvironmentName != EnvironmentNameFromIndex ||
        LightshowFilePath != InfoDifficulty.LightshowFileName ||
        Mappers != string.Join(',', InfoDifficulty.Mappers) ||
        Lighters != string.Join(',', InfoDifficulty.Lighters) ||
        (CustomName ?? "") != (InfoDifficulty.CustomLabel ?? "") ||
        EnvRemovalChanged() || SongCoreInfoWarningsChanged();

    private bool EnvRemovalChanged() =>
        envEnhancements != null && Map != null &&
        !(Map.EnvironmentEnhancements.All(envEnhancements.Contains) && Map.EnvironmentEnhancements.Count == envEnhancements.Count);

    private bool SongCoreInfoWarningsChanged() =>
        songCoreInfos != null && songCoreWarnings != null && InfoDifficulty != null &&
        !(InfoDifficulty.CustomInformation.SequenceEqual(songCoreInfos) &&
          InfoDifficulty.CustomWarnings.SequenceEqual(songCoreWarnings));

    /// <summary>
    ///     Save the users changes to the backing DifficultyBeatmap object
    /// </summary>
    public void Commit()
    {
        ForceDirty = false;

        InfoDifficulty.NoteJumpSpeed = NoteJumpMovementSpeed;
        InfoDifficulty.NoteStartBeatOffset = NoteJumpStartBeatOffset;

        InfoDifficulty.Mappers = Mappers.Split(',').Select(x => x.Trim()).ToList();
        InfoDifficulty.Lighters = Lighters.Split(',').Select(x => x.Trim()).ToList();
        
        var previousLightshowFileName = InfoDifficulty.LightshowFileName;
        InfoDifficulty.LightshowFileName = LightshowFilePath;
        
        var environmentNameIndex = BeatSaberSongContainer.Instance.Info.EnvironmentNames.IndexOf(EnvironmentName);
        if (environmentNameIndex >= 0)
        {
            InfoDifficulty.EnvironmentNameIndex = EnvironmentNameIndex = environmentNameIndex;
        }
        else
        {
            BeatSaberSongContainer.Instance.Info.EnvironmentNames.Add(EnvironmentName);
            InfoDifficulty.EnvironmentNameIndex = EnvironmentNameIndex = BeatSaberSongContainer.Instance.Info.EnvironmentNames.Count;
        }
        

        // Map lightshow diff has changed and requires reloading the lights for this difficulty
        if (Map is { MajorVersion: 4 } && previousLightshowFileName != LightshowFilePath)
        {
            V4Difficulty.LoadLightsFromLightshowFile(Map, BeatSaberSongContainer.Instance.Info, InfoDifficulty);
        }

        InfoDifficulty.CustomLabel = CustomName;
        InfoDifficulty.CustomInformation = songCoreInfos;
        InfoDifficulty.CustomWarnings = songCoreWarnings;

        InfoDifficulty.CustomData?.Remove("_environmentRemoval");

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
        if (InfoDifficulty.CustomData != null)
        {
            foreach (var ent in InfoDifficulty.CustomData["_environmentRemoval"])
                enhancements.Add(Settings.Instance.MapVersion == 3 ? V3EnvironmentEnhancement.GetFromJson(ent.Value.Value) : V2EnvironmentEnhancement.GetFromJson(ent.Value.Value));
        }

        if (Map != null) enhancements.AddRange(Map.EnvironmentEnhancements.Select(it => it.Clone() as BaseEnvironmentEnhancement));

        return enhancements;
    }

    private string EnvironmentNameFromIndex => BeatSaberSongContainer.Instance.Info.EnvironmentNames
                                                   .ElementAtOrDefault(InfoDifficulty.EnvironmentNameIndex)
                                               ?? "DefaultEnvironment";
    
    /// <summary>
    ///     Discard any changes from the user
    /// </summary>
    public void Revert()
    {
        NoteJumpMovementSpeed = InfoDifficulty.NoteJumpSpeed;
        NoteJumpStartBeatOffset = InfoDifficulty.NoteStartBeatOffset;
        Mappers = string.Join(',', InfoDifficulty.Mappers);
        Lighters = string.Join(',', InfoDifficulty.Lighters);
        EnvironmentNameIndex = InfoDifficulty.EnvironmentNameIndex;
        EnvironmentName = EnvironmentNameFromIndex;
        LightshowFilePath = InfoDifficulty.LightshowFileName;
        CustomName = InfoDifficulty.CustomLabel;

        envEnhancements = null;
        songCoreInfos = null;
        songCoreWarnings = null;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Info;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;

/// <summary>
///     TODO: WIP
///     Holds users changes to a characteristic custom properties until they ask to save them
/// </summary>
public class CharacteristicSettings
{
    private InfoDifficultySet infoDifficultySet;
    
    public string CustomCharacteristicLabel { get; set; }
    public string CustomCharacteristicIconImageFileName { get; set; }

    public bool ForceDirty;
    

    public CharacteristicSettings(InfoDifficultySet infoDifficultySet)
    {
        this.infoDifficultySet = infoDifficultySet;

        Revert();
    }

    public CharacteristicSettings(InfoDifficultySet infoDifficultySet, bool forceDirty) : this(infoDifficultySet) =>
        ForceDirty = forceDirty;


    /// <summary>
    ///     Check if the user has made changes
    /// </summary>
    /// <returns>True if changes have been made, false otherwise</returns>
    public bool IsDirty() =>
        ForceDirty ||
        CustomCharacteristicLabel != infoDifficultySet.CustomCharacteristicLabel ||
        CustomCharacteristicIconImageFileName != infoDifficultySet.CustomCharacteristicIconImageFileName;
    
    /// <summary>
    ///     Save the users changes to the backing DifficultyBeatmap object
    /// </summary>
    public void Commit()
    {
        ForceDirty = false;
        
        infoDifficultySet.CustomCharacteristicLabel = CustomCharacteristicLabel;
        infoDifficultySet.CustomCharacteristicIconImageFileName = CustomCharacteristicIconImageFileName;
    }

    /// <summary>
    ///     Discard any changes from the user
    /// </summary>
    public void Revert()
    {
        CustomCharacteristicLabel = infoDifficultySet.CustomCharacteristicLabel;
        CustomCharacteristicIconImageFileName = infoDifficultySet.CustomCharacteristicIconImageFileName;
    }
}

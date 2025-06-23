using TMPro;
using UnityEngine;

public class SongCoreFlagController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown forceOneSaberDropdown;
    [SerializeField] private TMP_Dropdown showRotationNoteSpawnLineDropdown;

    [SerializeField] private DifficultySelect difficultySelect;
    
    public void UpdateFromDiff(bool? forceOneSaber, bool? showRotationNoteSpawnLine)
    {
        forceOneSaberDropdown.value = forceOneSaber switch
        {
            null => 0,
            true => 1,
            false => 2
        };
        
        showRotationNoteSpawnLineDropdown.value = showRotationNoteSpawnLine switch
        {
            null => 0,
            true => 1,
            false => 2
        };
    }

    public bool? ForceOneSaber => forceOneSaberDropdown.value switch
    {
        0 => null,
        1 => true,
        2 => false
    };
    
    public bool? ShowRotationNoteSpawnLine => showRotationNoteSpawnLineDropdown.value switch
    {
        0 => null,
        1 => true,
        2 => false
    };

    public void UpdateSongCoreFlags() => difficultySelect.UpdateSongCoreFlags();
}

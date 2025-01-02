using System.Collections.Generic;
using System.Linq;
using Beatmap.Info;
using TMPro;
using UnityEngine;

public class LoadedDifficultySelectController : MonoBehaviour
{
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private TMP_Dropdown dropdown;
    
    private List<InfoDifficulty> setDifficulties;

    private void Start()
    {
        // ToList() to keep the ordering the same in case the ordering changes on save (which it currently does)
        setDifficulties = BeatSaberSongContainer.Instance.MapDifficultyInfo.ParentSet.Difficulties.ToList();
        
        var options = setDifficulties.Select(x => Settings.Instance.DisplayDiffDetailsInEditor 
            ? new TMP_Dropdown.OptionData(!string.IsNullOrWhiteSpace(x.CustomLabel) ? x.CustomLabel : x.Difficulty) 
            : new TMP_Dropdown.OptionData(x.Difficulty)); 
        
        dropdown.options = new List<TMP_Dropdown.OptionData>(options);
        dropdown.value = setDifficulties.IndexOf(BeatSaberSongContainer.Instance.MapDifficultyInfo);

        if (BeatSaberSongContainer.Instance.MultiMapperConnection != null)
        {
            // Disable in MultiMapper
            gameObject.SetActive(false);
            return;
        }
        
        if (setDifficulties.Count == 1)
        {
            // No other diffs to switch so disable the dropdown
            dropdown.interactable = false;
        }
        else
        {
            dropdown.onValueChanged.AddListener(OnDropdownChange);
        }
    }

    private void OnDropdownChange(int value)
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo = setDifficulties[value];
        
        var newMap = BeatSaberSongUtils.GetMapFromInfoFiles(
            BeatSaberSongContainer.Instance.Info,
            BeatSaberSongContainer.Instance.MapDifficultyInfo);
        mapLoader.UpdateMapData(newMap);
        mapLoader.HardRefresh();
        
        BeatSaberSongContainer.Instance.Map = newMap;
    }

    public void Disable() => gameObject.SetActive(false);
}


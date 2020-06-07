using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BeatSaberSong;

public class DifficultySelect : MonoBehaviour
{

    [SerializeField] private TMP_InputField njsField;
    [SerializeField] private TMP_InputField songBeatOffsetField;
    [SerializeField] private CharacteristicSelect characteristicSelect;
    [SerializeField] private Color copyColor;

    private DifficultyBeatmapSet currentCharacteristic;
    private Dictionary<string, DifficultySettings> diffs;
    private Dictionary<string, Dictionary<string, DifficultySettings>> characteristics;

    private Dictionary<string, int> diffRankLookup = new Dictionary<string, int>()
    {
        { "Easy", 1 },
        { "Normal", 3 },
        { "Hard", 5 },
        { "Expert", 7 },
        { "ExpertPlus", 9 }
    };

    private Transform copySource;
    private Transform selected;

    BeatSaberSong Song
    {
        get { return BeatSaberSongContainer.Instance?.song; }
    }

    public void Start()
    {
        if (Song?.difficultyBeatmapSets != null)
        {
            characteristics = Song.difficultyBeatmapSets.ToDictionary(
                it => it.beatmapCharacteristicName,
                it => it.difficultyBeatmaps.GroupBy(map => map.difficulty).ToDictionary(
                    grouped => grouped.Key,
                    grouped => new DifficultySettings(grouped.First())
                )
            );
        }
        else
        {
            characteristics = new Dictionary<string, Dictionary<string, DifficultySettings>>();
        }

        foreach (Transform child in transform)
        {
            var toggle = child.Find("Button/Toggle").GetComponent<Toggle>();
            toggle.onValueChanged.AddListener((val) => OnChange(child, val));

            var button = child.Find("Button").GetComponent<Button>();
            button.onClick.AddListener(() => OnClick(child));

            var nameInput = child.Find("Button/Name").GetComponent<TMP_InputField>();
            nameInput.onValueChanged.AddListener((name) => OnValueChanged(child, name));
        }

        songBeatOffsetField.onValueChanged.AddListener((v) => UpdateOffset());
        njsField.onValueChanged.AddListener((v) => UpdateNJS());

        if (Song?.difficultyBeatmapSets != null && Song.difficultyBeatmapSets.Count > 0)
        {
            SetCharacteristic(Song.difficultyBeatmapSets.First().beatmapCharacteristicName);
        }
    }

    private void UpdateOffset()
    {
        if (selected == null || !diffs.ContainsKey(selected.name)) return;

        var diff = diffs[selected.name];

        if (float.TryParse(songBeatOffsetField.text, out float temp2))
        {
            diff.NoteJumpStartBeatOffset = temp2;
        }

        ShowDirtyObjects(selected, diff);
    }

    private void UpdateNJS()
    {
        if (selected == null || !diffs.ContainsKey(selected.name)) return;

        var diff = diffs[selected.name];

        if (float.TryParse(njsField.text, out float temp))
        {
            diff.NoteJumpMovementSpeed = temp;
        }

        ShowDirtyObjects(selected, diff);
    }

    public void Revertdiff(Button button)
    {
        var obj = button.transform.parent;
        var localDiff = diffs[obj.name];
        localDiff.Revert();

        var nameInput = obj.Find("Button/Name").GetComponent<TMP_InputField>();
        nameInput.text = localDiff.CustomName;

        if (obj == selected)
        {
            njsField.text = localDiff.NoteJumpMovementSpeed.ToString();
            songBeatOffsetField.text = localDiff.NoteJumpStartBeatOffset.ToString();
        }

        ShowDirtyObjects(obj, localDiff);
    }

    public void SaveDiff(Button button)
    {
        var obj = button.transform.parent;
        var localDiff = diffs[obj.name];
        localDiff.Commit();
        ShowDirtyObjects(obj, false, true);

        var Song = BeatSaberSongContainer.Instance.song;
        var diff = localDiff.DifficultyBeatmap;

        if (!Song.difficultyBeatmapSets.Contains(currentCharacteristic))
        {
            Song.difficultyBeatmapSets.Add(currentCharacteristic);
        }
        currentCharacteristic.difficultyBeatmaps.Add(diff);

        BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(diff);
        string oldPath = map?.directoryAndFile;

        if (map is null)
        {
            map = new BeatSaberMap();
            map.mainNode = new JSONObject();
        }

        diff.UpdateName();
        map.directoryAndFile = Path.Combine(Song.directory, diff.beatmapFilename);
        if (File.Exists(oldPath) && oldPath != map.directoryAndFile && !File.Exists(map.directoryAndFile))
        {
            File.Move(oldPath, map.directoryAndFile); //This should properly "convert" difficulties just fine
        }
        else
        {
            map.Save();
        }

        diff.RefreshRequirementsAndWarnings(map);

        Song.SaveSong();
        characteristicSelect.Recalculate();

        Debug.Log("Saved " + button.transform.parent.name);
    }

    private void OnValueChanged(Transform obj, string difficultyLabel)
    {
        if (!diffs.ContainsKey(obj.name)) return;

        var diff = diffs[obj.name];

        // Expert+ is special as the only difficulty that is different in JSON
        string defaultName = obj.name == "ExpertPlus" ? "Expert+" : obj.name;
        if (difficultyLabel != "" && difficultyLabel != defaultName)
        {
            diff.CustomName = difficultyLabel;
        }
        else
        {
            diff.CustomName = null;
        }

        ShowDirtyObjects(obj, diff);
    }

    private void DeselectDiff()
    {
        if (selected != null)
        {
            var selImage = selected.GetComponent<Image>();
            selImage.color = new Color(selImage.color.r, selImage.color.g, selImage.color.b, 0.0f);

            // Clean the UI, if we're selecting a new item they'll be repopulated
            BeatSaberSongContainer.Instance.difficultyData = null;
            njsField.text = "";
            songBeatOffsetField.text = "";
        }

        selected = null;
    }

    public void OnClick(Transform obj)
    {
        if (!diffs.ContainsKey(obj.name)) return;

        DeselectDiff();

        // Select a difficulty
        selected = obj;
        var selImage = selected.GetComponent<Image>();
        selImage.color = new Color(selImage.color.r, selImage.color.g, selImage.color.b, 1.0f);

        var diff = diffs[obj.name];
        BeatSaberSongContainer.Instance.difficultyData = diff.DifficultyBeatmap;

        njsField.text = diff.NoteJumpMovementSpeed.ToString();
        songBeatOffsetField.text = diff.NoteJumpStartBeatOffset.ToString();
    }

    private void OnChange(Transform obj, bool val)
    {
        // Create or delete difficulties
        if (!val && diffs.ContainsKey(obj.name))
        {
            // ForceDirty = has never been saved, don't ask for permission
            if (diffs[obj.name].ForceDirty)
            {
                if (obj == selected)
                {
                    DeselectDiff();
                }

                diffs.Remove(obj.name);
                SetState(obj, false);
                obj.Find("Button/Name").GetComponent<TMP_InputField>().text = "";
                ShowDirtyObjects(obj, false, false);
                return;
            }

            PersistentUI.Instance.ShowDialogBox("Are you sure you want to delete " +
            $"{diffs[obj.name].DifficultyBeatmap.difficulty}?\n\nThe song info will be deleted as well, so this will be gone forever!",
            (r) => HandleDeleteDifficulty(obj, r), PersistentUI.DialogBoxPresetType.YesNo);
        }
        else if (val && !diffs.ContainsKey(obj.name))
        {
            DifficultyBeatmap map = new DifficultyBeatmap(currentCharacteristic)
            {
                difficulty = obj.name,
                difficultyRank = diffRankLookup[obj.name]
            };

            map.UpdateName();

            if (copySource != null)
            {
                var fromDiff = diffs[copySource.name];

                copySource.Find("Copy").GetComponent<Image>().color = Color.white;
                copySource = null;

                if (fromDiff != null)
                {
                    map.noteJumpMovementSpeed = fromDiff.DifficultyBeatmap.noteJumpMovementSpeed;
                    map.noteJumpStartBeatOffset = fromDiff.DifficultyBeatmap.noteJumpStartBeatOffset;
                    // This sets the current filename as the filename for another diff and will trigger the copy on save
                    map.UpdateName(fromDiff.DifficultyBeatmap.beatmapFilename);
                }
            }

            diffs[obj.name] = new DifficultySettings(map, true);

            ShowDirtyObjects(obj, diffs[obj.name]);
            SetState(obj, true);
            OnClick(obj);
        }
        else if (val)
        {
            // I don't know how this would happen anymore
            ShowDirtyObjects(obj, diffs[obj.name]);
            SetState(obj, true);
            OnClick(obj);
        }
    }

    private void HandleDeleteDifficulty(Transform obj, int r)
    {
        if (r == 1)
        {
            var toggle = obj.Find("Button/Toggle").GetComponent<Toggle>();
            toggle.isOn = true;
            return;
        }

        var diff = diffs[obj.name].DifficultyBeatmap;

        string fileToDelete = Song.GetMapFromDifficultyBeatmap(diff)?.directoryAndFile;
        if (File.Exists(fileToDelete))
        {
            FileOperationAPIWrapper.MoveToRecycleBin(fileToDelete);
        }

        if (obj == copySource)
        {
            copySource.Find("Copy").GetComponent<Image>().color = Color.white;
            copySource = null;
        }

        if (obj == selected)
        {
            DeselectDiff();
        }
        currentCharacteristic.difficultyBeatmaps.Remove(diffs[obj.name].DifficultyBeatmap);

        if (currentCharacteristic.difficultyBeatmaps.Count == 0)
        {
            Song.difficultyBeatmapSets.Remove(currentCharacteristic);
        }

        diffs.Remove(obj.name);
        Song.SaveSong();

        SetState(obj, false);
        obj.Find("Button/Name").GetComponent<TMP_InputField>().text = "";
        ShowDirtyObjects(obj, false, false);
        characteristicSelect.Recalculate();
    }

    public void SetCopySource(Button button)
    {
        var obj = button.transform.parent;

        if (copySource != null)
        {
            copySource.Find("Copy").GetComponent<Image>().color = Color.white;
        }

        if (copySource == obj)
        {
            copySource = null;
            return;
        }

        copySource = obj;
        copySource.Find("Copy").GetComponent<Image>().color = copyColor;
    }

    public void SetCharacteristic(string name)
    {
        DeselectDiff();

        currentCharacteristic = Song?.difficultyBeatmapSets?.Find(it => it.beatmapCharacteristicName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (currentCharacteristic == null)
        {
            currentCharacteristic = new DifficultyBeatmapSet(name);
        }

        if (!characteristics.ContainsKey(name))
        {
            characteristics.Add(name, new Dictionary<string, DifficultySettings>());
        }
        diffs = characteristics[name];

        foreach (Transform child in transform)
        {
            bool hasDiff = diffs.ContainsKey(child.name);
            SetState(child, diffs.ContainsKey(child.name));
            
            var nameInput = child.Find("Button/Name").GetComponent<TMP_InputField>();

            nameInput.text = hasDiff ? diffs[child.name].CustomName : "";

            if (hasDiff)
            {
                ShowDirtyObjects(child, diffs[child.name]);
                if (selected == null)
                {
                    OnClick(child);
                }
            }
        }

        if (selected == null)
        {
            njsField.text = "";
            songBeatOffsetField.text = "";
        }
    }

    private void SetState(Transform child, bool val)
    {
        var button = child.Find("Button").GetComponent<Button>();
        var toggle = child.Find("Button/Toggle").GetComponent<Toggle>();
        var nameInput = child.Find("Button/Name").GetComponent<TMP_InputField>();
        nameInput.interactable = button.interactable = toggle.isOn = val;
    }

    private void ShowDirtyObjects(Transform obj, DifficultySettings difficultySettings)
    {
        ShowDirtyObjects(obj, difficultySettings.IsDirty(), !difficultySettings.IsDirty());
    }

    private void ShowDirtyObjects(Transform obj, bool show, bool copy)
    {
        obj.Find("Copy").gameObject.SetActive(copy);
        obj.Find("Warning").gameObject.SetActive(show);
        obj.Find("Revert").gameObject.SetActive(show);
    }
}
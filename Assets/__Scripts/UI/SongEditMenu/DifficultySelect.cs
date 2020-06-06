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
    private class DifficultySettings
    {
        public float NoteJumpMovementSpeed = 16;
        public float NoteJumpStartBeatOffset = 0;
        public string CustomName = "";
        public bool ForceDirty = false;

        public DifficultyBeatmap difficultyBeatmap { get; private set; }

        public DifficultySettings(DifficultyBeatmap difficultyBeatmap)
        {
            if (difficultyBeatmap.customData == null)
                difficultyBeatmap.customData = new JSONObject();

            this.difficultyBeatmap = difficultyBeatmap;
            Revert();
        }

        public DifficultySettings(DifficultyBeatmap difficultyBeatmap, bool ForceDirty) : this(difficultyBeatmap)
        {
            this.ForceDirty = ForceDirty;
        }

        public bool IsDirty()
        {
            return ForceDirty || NoteJumpMovementSpeed != difficultyBeatmap.noteJumpMovementSpeed ||
                NoteJumpStartBeatOffset != difficultyBeatmap.noteJumpStartBeatOffset ||
                !(CustomName ?? "").Equals(difficultyBeatmap.customData["_difficultyLabel"].Value);
        }

        public void Commit()
        {
            // TODO: hmm?
            ForceDirty = false;

            difficultyBeatmap.noteJumpMovementSpeed = NoteJumpMovementSpeed;
            difficultyBeatmap.noteJumpStartBeatOffset = NoteJumpStartBeatOffset;

            if (CustomName == null || CustomName.Length == 0)
            {
                difficultyBeatmap.customData.Remove("_difficultyLabel");
            }
            else
            {
                difficultyBeatmap.customData["_difficultyLabel"] = CustomName;
            }
        }

        public void Revert()
        {
            NoteJumpMovementSpeed = difficultyBeatmap.noteJumpMovementSpeed;
            NoteJumpStartBeatOffset = difficultyBeatmap.noteJumpStartBeatOffset;
            CustomName = difficultyBeatmap.customData["_difficultyLabel"].Value;
        }
    }

    [SerializeField] private TMP_InputField njsField;
    [SerializeField] private TMP_InputField songBeatOffsetField;
    [SerializeField] private CharacteristicSelect characteristicSelect;

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
        ShowDirtyObjects(obj, false);

        var Song = BeatSaberSongContainer.Instance.song;
        var diff = localDiff.difficultyBeatmap;

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

        map.directoryAndFile = $"{Song.directory}\\{diff.beatmapFilename}";
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
        }

        selected = null;
    }

    public void OnClick(Transform obj)
    {
        if (!diffs.ContainsKey(obj.name)) return;

        DeselectDiff();

        selected = obj;
        var selImage = selected.GetComponent<Image>();
        selImage.color = new Color(selImage.color.r, selImage.color.g, selImage.color.b, 1.0f);

        var diff = diffs[obj.name];
        BeatSaberSongContainer.Instance.difficultyData = diff.difficultyBeatmap;

        njsField.text = diff.NoteJumpMovementSpeed.ToString();
        songBeatOffsetField.text = diff.NoteJumpStartBeatOffset.ToString();
    }

    private void OnChange(Transform obj, bool val)
    {
        if (!val && diffs.ContainsKey(obj.name))
        {
            if (diffs[obj.name].ForceDirty)
            {
                ShowDirtyObjects(obj, false);
                return;
            }
            PersistentUI.Instance.ShowDialogBox("Are you sure you want to delete " +
            $"{diffs[obj.name].difficultyBeatmap.difficulty}?\n\nThe song info will be deleted as well, so this will be gone forever!",
            (r) => HandleDeleteDifficulty(obj, r), PersistentUI.DialogBoxPresetType.YesNo);
        }
        else if (val && !diffs.ContainsKey(obj.name))
        {
            DifficultyBeatmap map = new DifficultyBeatmap(currentCharacteristic);
            diffs[obj.name] = new DifficultySettings(map, true);

            map.difficulty = obj.name;
            map.difficultyRank = diffRankLookup[obj.name];
            map.UpdateName();

            ShowDirtyObjects(obj, diffs[obj.name]);
            SetState(obj, true);
            OnClick(obj);
        }
        else if (val)
        {
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

        var diff = diffs[obj.name].difficultyBeatmap;

        string fileToDelete = Song.GetMapFromDifficultyBeatmap(diff)?.directoryAndFile;
        if (File.Exists(fileToDelete))
        {
            FileOperationAPIWrapper.MoveToRecycleBin(fileToDelete);
        }

        if (obj == selected)
        {
            BeatSaberSongContainer.Instance.difficultyData = null;
            // Remove the current item
            DeselectDiff();
            njsField.text = "";
            songBeatOffsetField.text = "";
        }
        currentCharacteristic.difficultyBeatmaps.Remove(diffs[obj.name].difficultyBeatmap);

        if (currentCharacteristic.difficultyBeatmaps.Count == 0)
        {
            Song.difficultyBeatmapSets.Remove(currentCharacteristic);
        }

        diffs.Remove(obj.name);
        Song.SaveSong();

        SetState(obj, false);
        obj.Find("Button/Name").GetComponent<TMP_InputField>().text = "";
        ShowDirtyObjects(obj, false);
        characteristicSelect.Recalculate();
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

            if (hasDiff && selected == null)
            {
                OnClick(child);
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
        ShowDirtyObjects(obj, difficultySettings.IsDirty(), difficultySettings.ForceDirty);
    }

    private void ShowDirtyObjects(Transform obj, bool show, bool forceDirty = false)
    {
        obj.Find("Warning").gameObject.SetActive(show);
        obj.Find("Revert").gameObject.SetActive(show);
    }
}
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using SimpleJSON;
using UnityEngine.Networking;
using System.Text;
using System.Globalization;

public class SongInfoEditUI : MonoBehaviour {

    private static List<string> VanillaEnvironments = new List<string>()
    {
        "DefaultEnvironment",
        "BigMirrorEnvironment",
        "TriangleEnvironment",
        "NiceEnvironment",
        "KDAEnvironment",
        "MonstercatEnvironment",
        "DragonsEnvironment",
        "Origins",
        "CrabRaveEnvironment",
        "PanicEnvironment",
        "RocketEnvironment"
    };

    private static List<string> VanillaDirectionalEnvironments = new List<string>()
    {
        "GlassDesertEnvironment"
    };

    public static List<string> CharacteristicDropdownToBeatmapName = new List<string>()
    {
        "Standard",
        "NoArrows",
        "OneSaber",
        "360Degree",
        "90Degree",
        "Lightshow",
        "Lawless"
    };

    public static int GetDirectionalEnvironmentIDFromString(string platforms)
    {
        return VanillaDirectionalEnvironments.IndexOf(platforms);
    }

    public static int GetEnvironmentIDFromString(string environment) {
        return VanillaEnvironments.IndexOf(environment);
    }

    public static string GetEnvironmentNameFromID(int id) {
        return VanillaEnvironments[id];
    }

    BeatSaberSong Song {
        get { return BeatSaberSongContainer.Instance.song; }
    }

    BeatSaberSong.DifficultyBeatmapSet SelectedSet
    {
        get => songDifficultySets.FirstOrDefault(x => x.beatmapCharacteristicName == selectedBeatmapSet);
    }

    [SerializeField] InputField nameField;
    [SerializeField] InputField subNameField;
    [SerializeField] InputField songAuthorField;
    [SerializeField] InputField authorField;
    [SerializeField] InputField coverImageField;

    [SerializeField] InputField bpmField;
    [SerializeField] InputField prevStartField;
    [SerializeField] InputField prevDurField;
    
    [SerializeField] TMP_Dropdown environmentDropdown;
    [SerializeField] TMP_Dropdown customPlatformsDropdown;
    [SerializeField] TMP_Dropdown difficultyLevelSelectDropdown;
    [SerializeField] TMP_Dropdown characteristicDropdown;

    [SerializeField] List<BeatSaberSong.DifficultyBeatmap> songDifficultyData = new List<BeatSaberSong.DifficultyBeatmap>();
    [SerializeField] List<BeatSaberSong.DifficultyBeatmapSet> songDifficultySets = new List<BeatSaberSong.DifficultyBeatmapSet>();
    [SerializeField] int selectedDifficultyIndex = -1;
    [SerializeField] string selectedBeatmapSet = "Standard";
    [SerializeField] Toggle WillChromaBeRequired;
    [SerializeField] TextMeshProUGUI HalfJumpDurationText;
    [SerializeField] TextMeshProUGUI JumpDistanceText;

    [SerializeField] GameObject difficultyExistsPanel;
    [SerializeField] GameObject difficultyNoExistPanel;
    [SerializeField] TMP_Dropdown difficultyDifficultyDropdown;

    [SerializeField] InputField audioPath;
    [SerializeField] InputField offset;
    [SerializeField] InputField difficultyLabel;
    [SerializeField] InputField noteJumpSpeed;
    [SerializeField] InputField startBeatOffset;

    [SerializeField] Button difficultyRevertButton;
    [SerializeField] Button difficultySaveButton;

    [SerializeField] Button editMapButton;

    void Start () {
		if (BeatSaberSongContainer.Instance == null) {
            SceneManager.LoadScene(0);
            return;
        }

        LoadFromSong(true);

    }

    public void SaveToSong() {
        Song.songName = nameField.text;
        Song.songSubName = subNameField.text;
        Song.songAuthorName = songAuthorField.text;
        Song.levelAuthorName = authorField.text;
        Song.coverImageFilename = coverImageField.text;
        Song.songFilename = audioPath.text;

        Song.beatsPerMinute = float.Parse(bpmField.text);
        Song.previewStartTime = float.Parse(prevStartField.text);
        Song.previewDuration = float.Parse(prevDurField.text);
        Song.songTimeOffset = float.Parse(offset.text);

        if (Song.songTimeOffset > 0)
        {
            PersistentUI.Instance.ShowDialogBox("Using Song Time Offset can result in desynced cut noises in game.\n\n" +
                "It is recommended that you apply your offsets using a audio manipulator such as Audacity.", null,
                PersistentUI.DialogBoxPresetType.Ok);
        }

        Song.environmentName = GetEnvironmentNameFromID(environmentDropdown.value);

        if (Song.customData == null) Song.customData = new JSONObject();

        if (customPlatformsDropdown.value > 0)
        {
            string hash;
            Song.customData["_customEnvironment"] = customPlatformsDropdown.captionText.text;
            if (CustomPlatformsLoader.Instance.GetEnvironmentsWithHash().TryGetValue(customPlatformsDropdown.captionText.text, out hash))
                Song.customData["_customEnvironmentHash"] = hash;
        }
        else
        {
            Song.customData.Remove("_customEnvironment");
            Song.customData.Remove("_customEnvironmentHash");
        }

        Song.SaveSong();
        PersistentUI.Instance.DisplayMessage("Song Info Saved!", PersistentUI.DisplayMessageType.BOTTOM);
    }

    public void LoadFromSong(bool initial) {
        
        nameField.text = Song.songName;
        subNameField.text = Song.songSubName;
        songAuthorField.text = Song.songAuthorName;
        authorField.text = Song.levelAuthorName;
        coverImageField.text = Song.coverImageFilename;
        audioPath.text = Song.songFilename;
        offset.text = Song.songTimeOffset.ToString(CultureInfo.InvariantCulture);
        if (Song.songTimeOffset > 0)
        {
            PersistentUI.Instance.ShowDialogBox("Using Song Time Offset can result in desynced cut noises in game.\n\n" +
                "It is recommended that you apply your offsets using a audio manipulator such as Audacity.", null,
                PersistentUI.DialogBoxPresetType.Ok);
        }

        bpmField.text = Song.beatsPerMinute.ToString(CultureInfo.InvariantCulture);
        prevStartField.text = Song.previewStartTime.ToString(CultureInfo.InvariantCulture);
        prevDurField.text = Song.previewDuration.ToString(CultureInfo.InvariantCulture);

        environmentDropdown.ClearOptions();
        environmentDropdown.AddOptions(VanillaEnvironments);
        environmentDropdown.value = GetEnvironmentIDFromString(Song.environmentName);

        customPlatformsDropdown.ClearOptions();
        customPlatformsDropdown.AddOptions(new List<String> { "None" });
        customPlatformsDropdown.AddOptions(CustomPlatformsLoader.Instance.GetAllEnvironmentIds());

        if (Song.customData != null)
        {
            if (Song.customData["_customEnvironment"] != null && Song.customData["_customEnvironment"] != "")
                customPlatformsDropdown.value = CustomPlatformsLoader.Instance.GetAllEnvironmentIds().IndexOf(Song.customData["_customEnvironment"]) + 1;
            else
            { //For some reason the text defaults to "Dueling Dragons", not what we want.
                customPlatformsDropdown.value = 0;
                customPlatformsDropdown.captionText.text = "None";
            }
        }
        else
        {
            customPlatformsDropdown.value = 0;
            customPlatformsDropdown.captionText.text = "None";
        }

        if (Song.difficultyBeatmapSets.Any())
        {
            songDifficultySets = Song.difficultyBeatmapSets;
            songDifficultyData = songDifficultySets.First().difficultyBeatmaps;
            selectedBeatmapSet = songDifficultySets.First().beatmapCharacteristicName;
            characteristicDropdown.value = CharacteristicDropdownToBeatmapName.IndexOf(selectedBeatmapSet);
        }

        if (initial) {
            InitializeDifficultyPanel();
        }

    }

    public void SelectDifficulty(int index) {

        if (index >= songDifficultyData.Count || index < 0) {
            ShowDifficultyEditPanel(false);
            return;
        }
        difficultyDifficultyDropdown.value = difficultyDifficultyDropdown.options.IndexOf(
            difficultyDifficultyDropdown.options.Where(x => x.text == songDifficultyData[index].difficulty).FirstOrDefault());
        difficultyDifficultyDropdown.captionText.text = songDifficultyData[index].difficulty;
        selectedDifficultyIndex = index;
        LoadDifficulty();
        ShowDifficultyEditPanel(true);
    }

    public void SaveDifficulty() {
        if (songDifficultyData[selectedDifficultyIndex].customData == null)
            songDifficultyData[selectedDifficultyIndex].customData = new JSONObject();

        BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(songDifficultyData[selectedDifficultyIndex]);
        string oldPath = map?.directoryAndFile;
        switch (difficultyDifficultyDropdown.value)
        {
            case 0:
                songDifficultyData[selectedDifficultyIndex].difficulty = "Easy";
                songDifficultyData[selectedDifficultyIndex].difficultyRank = 1;
                break;
            case 1:
                songDifficultyData[selectedDifficultyIndex].difficulty = "Normal";
                songDifficultyData[selectedDifficultyIndex].difficultyRank = 3;
                break;
            case 2:
                songDifficultyData[selectedDifficultyIndex].difficulty = "Hard";
                songDifficultyData[selectedDifficultyIndex].difficultyRank = 5;
                break;
            case 3:
                songDifficultyData[selectedDifficultyIndex].difficulty = "Expert";
                songDifficultyData[selectedDifficultyIndex].difficultyRank = 7;
                break;
            case 4:
                songDifficultyData[selectedDifficultyIndex].difficulty = "ExpertPlus";
                songDifficultyData[selectedDifficultyIndex].difficultyRank = 9;
                break;
            default:
                Debug.Log("Difficulty doesnt seem to exist! Default to Easy...");
                songDifficultyData[selectedDifficultyIndex].difficulty = "Easy";
                songDifficultyData[selectedDifficultyIndex].difficultyRank = 1;
                break;
        }
        songDifficultyData[selectedDifficultyIndex].UpdateName();

        if (map is null)
        {
            map = new BeatSaberMap();
            map.mainNode = new JSONObject();
        }

        map.directoryAndFile = $"{Song.directory}\\{songDifficultyData[selectedDifficultyIndex].beatmapFilename}";
        if (File.Exists(oldPath) && oldPath != map.directoryAndFile && !File.Exists(map.directoryAndFile))
            File.Move(oldPath, map.directoryAndFile); //This should properly "convert" difficulties just fine
        else map.Save();
        songDifficultyData[selectedDifficultyIndex].noteJumpMovementSpeed = float.Parse(noteJumpSpeed.text);
        songDifficultyData[selectedDifficultyIndex].noteJumpStartBeatOffset = float.Parse(startBeatOffset.text);
        if (difficultyLabel.text != "")
            songDifficultyData[selectedDifficultyIndex].customData["_difficultyLabel"] = difficultyLabel.text;
        else songDifficultyData[selectedDifficultyIndex].customData.Remove("_difficultyLabel");

        JSONArray requiredArray = new JSONArray();
        JSONArray suggestedArray = new JSONArray();
        if (WillChromaBeRequired.isOn && HasChromaEvents()) requiredArray.Add(new JSONString("Chroma Lighting Events"));
        else if (HasChromaEvents()) suggestedArray.Add(new JSONString("Chroma Lighting Events"));
        if (HasMappingExtensionsObjects()) requiredArray.Add(new JSONString("Mapping Extensions"));
        //if () requiredArray.Add(new JSONString("ChromaToggle")); //TODO: ChromaToggle

        if (suggestedArray.Linq.Any())
            songDifficultyData[selectedDifficultyIndex].customData["_suggestions"] = suggestedArray;
        if (requiredArray.Linq.Any())
            songDifficultyData[selectedDifficultyIndex].customData["_requirements"] = requiredArray;

        SelectedSet.difficultyBeatmaps = songDifficultyData;
        songDifficultySets.Add(SelectedSet);
        Song.difficultyBeatmapSets = songDifficultySets.Distinct().Where(x => x.difficultyBeatmaps.Any()).ToList();
        Song.SaveSong();
        InitializeDifficultyPanel(selectedDifficultyIndex);
    }

    public void LoadDifficulty() {
        difficultyLabel.text = "";
        if (songDifficultyData[selectedDifficultyIndex].customData != null)
        {
            if (songDifficultyData[selectedDifficultyIndex].customData["_difficultyLabel"] != null)
                difficultyLabel.text = songDifficultyData[selectedDifficultyIndex].customData["_difficultyLabel"].Value;
        }
        noteJumpSpeed.text = songDifficultyData[selectedDifficultyIndex].noteJumpMovementSpeed.ToString();
        startBeatOffset.text = songDifficultyData[selectedDifficultyIndex].noteJumpStartBeatOffset.ToString();

        switch (songDifficultyData[selectedDifficultyIndex].difficulty) {
            case "Easy":
                difficultyDifficultyDropdown.value = 0;
                break;
            case "Normal":
                difficultyDifficultyDropdown.value = 1;
                break;
            case "Hard":
                difficultyDifficultyDropdown.value = 2;
                break;
            case "Expert":
                difficultyDifficultyDropdown.value = 3;
                break;
            case "ExpertPlus":
                difficultyDifficultyDropdown.value = 4;
                break;
            default:
                difficultyDifficultyDropdown.value = 0;
                break;
        }
        CalculateHalfJump();
    }

    public void CalculateHalfJump()
    {
        float num = 60f / Song.beatsPerMinute;
        float halfJumpDuration = 4;
        float songNoteJumpSpeed = songDifficultyData[selectedDifficultyIndex].noteJumpMovementSpeed;
        float songStartBeatOffset = songDifficultyData[selectedDifficultyIndex].noteJumpStartBeatOffset;

        while (songNoteJumpSpeed * num * halfJumpDuration > 18)
            halfJumpDuration /= 2;

        halfJumpDuration += songStartBeatOffset;

        if (halfJumpDuration < 1) halfJumpDuration = 1;
        float jumpDistance = songNoteJumpSpeed * num * halfJumpDuration * 2;

        HalfJumpDurationText.text = halfJumpDuration.ToString();
        JumpDistanceText.text = jumpDistance.ToString();
    }

    private bool HasMappingExtensionsObjects()
    {
        if (songDifficultyData[selectedDifficultyIndex] == null) return false;
        BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(songDifficultyData[selectedDifficultyIndex]);
        if (map == null) return false;
        foreach (BeatmapNote note in map?._notes)
            if (note._lineIndex < 0 || note._lineIndex > 3) return true;
        foreach (BeatmapObstacle ob in map?._obstacles)
            if (ob._lineIndex < 0 || ob._lineIndex > 3 || ob._type >= 2 || ob._width >= 1000) return true;
        return false;
    }

    private bool HasChromaEvents()
    {
        BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(songDifficultyData[selectedDifficultyIndex]);
        if (map is null) return false;
        foreach (MapEvent mapevent in map._events)
            if (mapevent._value > ColourManager.RGB_INT_OFFSET) return true;
        return false;
    }

    public void InitializeDifficultyPanel(int index = 0) {
        difficultyLevelSelectDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < songDifficultyData.Count; i++) {
            options.Add(new TMP_Dropdown.OptionData(songDifficultyData[i].difficulty));
        }
        difficultyLevelSelectDropdown.AddOptions(options);
        SelectDifficulty(index);
    }

    public void UpdateCharacteristicSet()
    {
        selectedBeatmapSet = CharacteristicDropdownToBeatmapName[characteristicDropdown.value];
        if (SelectedSet != null)
        {;
            songDifficultyData = SelectedSet.difficultyBeatmaps;
            selectedDifficultyIndex = songDifficultyData.Any() ? 0 : -1;
        }
        else
        {
            BeatSaberSong.DifficultyBeatmapSet set = new BeatSaberSong.DifficultyBeatmapSet(selectedBeatmapSet);
            songDifficultySets.Add(set);
            songDifficultyData = SelectedSet.difficultyBeatmaps;
            selectedDifficultyIndex = -1;
        }
        InitializeDifficultyPanel(selectedDifficultyIndex);
    }

    public void CreateNewDifficultyData()
    {
        BeatSaberSong.DifficultyBeatmap data = new BeatSaberSong.DifficultyBeatmap(SelectedSet);
        songDifficultyData.Add(data);
        selectedDifficultyIndex = songDifficultyData.IndexOf(data);
        InitializeDifficultyPanel(selectedDifficultyIndex);
        PersistentUI.Instance.ShowDialogBox("Be sure to save the difficulty before editing!", null, PersistentUI.DialogBoxPresetType.Ok);
    }

    public void UpdateDifficultyPanel() {
        SelectDifficulty(difficultyLevelSelectDropdown.value);
    }

    public void ShowDifficultyEditPanel(bool b) {
        difficultyExistsPanel.SetActive(b);
        difficultyNoExistPanel.SetActive(!b);
    }


    public void DeleteMap()
    {
        PersistentUI.Instance.ShowDialogBox($"Are you sure you want to delete {Song.songName}?", HandleDeleteMap,
            PersistentUI.DialogBoxPresetType.YesNo);
    }

    private void HandleDeleteMap(int result)
    {
        if (result == 0) //Left button (ID 0) pressed; the user wants to delete the map.
        {
            Directory.Delete(Song.directory, true);
            ReturnToSongList();
        } //Middle button (ID 1) would be pressed; the user doesn't want to delete the map, so we do nothing.
    }



    public void DeleteDifficulty()
    {
        PersistentUI.Instance.ShowDialogBox("Are you sure you want to delete " +
            $"{songDifficultyData[selectedDifficultyIndex].difficulty}?\n\nThe song info will be saved as well, so this will be gone forever!",
            HandleDeleteDifficulty, PersistentUI.DialogBoxPresetType.YesNo);
    }

    private void HandleDeleteDifficulty(int res)
    {
        if (res == 0) //Left button (ID 0) pressed; the user wants to delete the map.
        {
            if (File.Exists(Song.GetMapFromDifficultyBeatmap(songDifficultyData[selectedDifficultyIndex])?.directoryAndFile))
                File.Delete(Song.GetMapFromDifficultyBeatmap(songDifficultyData[selectedDifficultyIndex])?.directoryAndFile);
            songDifficultyData.RemoveAt(selectedDifficultyIndex);
            selectedDifficultyIndex--;
            if (songDifficultyData.Count < 0 && songDifficultyData.Any()) selectedDifficultyIndex = 0;
            SelectedSet.difficultyBeatmaps = songDifficultyData;
            Song.difficultyBeatmapSets = songDifficultySets;
            Song.SaveSong();
            InitializeDifficultyPanel(selectedDifficultyIndex);
        } //Middle button (ID 1) would be pressed; the user doesn't want to delete the map, so we do nothing.
    }

    public void OpenSelectedMapInFileBrowser()
    {
        try
        {
            string winPath = Song.directory.Replace("/", "\\").Replace("\\\\", "\\");
            Debug.Log($"Opening song directory ({winPath}) with Windows...");
            System.Diagnostics.Process.Start("explorer.exe", winPath);
        }catch
        {
            if (Song.directory == null)
            {
                PersistentUI.Instance.ShowDialogBox("Save your song info before opening up song files!", null,
                    PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
            Debug.Log("Windows opening failed, attempting Mac...");
            try
            {
                string macPath = Song.directory.Replace("\\", "/").Replace("//", "/");
                if (!macPath.StartsWith("\"")) macPath = "\"" + macPath;
                if (!macPath.EndsWith("\"")) macPath = macPath + "\"";
                System.Diagnostics.Process.Start("open", macPath);
            }
            catch
            {
                Debug.Log("What is this, some UNIX bullshit?");
                PersistentUI.Instance.ShowDialogBox("Unrecognized OS!\n\nIf you happen to know Linux and would like to contribute," +
                    " please contact me on Discord: Caeden117#0117", null, PersistentUI.DialogBoxPresetType.Ok);
            }
        }
    }

    public void ReturnToSongList() {
        SceneTransitionManager.Instance.LoadScene(1);
    }

    public void EditMapButtonPressed() {
        if (selectedDifficultyIndex >= songDifficultyData.Count || selectedDifficultyIndex < 0) {
            return;
        }

        bool a = Settings.Instance.Load_Notes;
        bool b = Settings.Instance.Load_Obstacles;
        bool c = Settings.Instance.Load_Events;
        bool d = Settings.Instance.Load_Others;

        if (!(a || b || c || d))
        {
            PersistentUI.Instance.ShowDialogBox(
                "ChroMapper is currently set to not load anything enabled.\n" +
                "To set something to load, visit Options and scroll to the bottom of mapper settings.", 
                null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }
        else if (!(a && b && c && d))
        {
            PersistentUI.Instance.ShowDialogBox(
                "ChroMapper is currently set to not load everything.\n" +
                "To re-enable items, visit Options and scroll to the bottom of mapper settings.", 
                null, PersistentUI.DialogBoxPresetType.Ok);
            
        }

        BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(songDifficultyData[selectedDifficultyIndex]);
        PersistentUI.UpdateBackground(Song);
        Debug.Log("Loading Song...");
        TransitionToEditor(map);
        //StartCoroutine(GetSongFromDifficultyData(map));
    }

    IEnumerator GetSongFromDifficultyData(BeatSaberMap map)
    {
        BeatSaberSong.DifficultyBeatmap data = songDifficultyData[selectedDifficultyIndex];
        string directory = Song.directory;
        if (File.Exists(directory + "/" + Song.songFilename))
        {
            if (Song.songFilename.ToLower().EndsWith("ogg") || Song.songFilename.ToLower().EndsWith("egg"))
            {
                UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{directory}/{Song.songFilename}")}", AudioType.OGGVORBIS);
                //Escaping should fix the issue where half the people can't open ChroMapper's editor (I believe this is caused by spaces in the directory, hence escaping)
                yield return www.SendWebRequest();
                Debug.Log("Song loaded!");
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip == null)
                {
                    Debug.Log("Error getting Audio data!");
                    SceneTransitionManager.Instance.CancelLoading("Error getting Audio data!");
                }
                clip.name = "Song";
                BeatSaberSongContainer.Instance.loadedSong = clip;
                BeatSaberSongContainer.Instance.difficultyData = data;
                //TransitionToEditor(map, clip, data);
            }
            else
            {
                Debug.Log("Incompatible file type! WTF!?");
                SceneTransitionManager.Instance.CancelLoading("Incompatible audio type!");
            }
        }
        else
        {
            SceneTransitionManager.Instance.CancelLoading("Audio file does not exist!");
            Debug.Log("Song does not exist! WTF!?");
            Debug.Log(directory + "/" + Song.songFilename);
        }
    }

    void TransitionToEditor(BeatSaberMap map)
    {
        Debug.Log("Transitioning...");
        if (map != null)
        {
            BeatSaberSongContainer.Instance.map = map;
            SceneTransitionManager.Instance.LoadScene(3, GetSongFromDifficultyData(map));
        }
    }

}

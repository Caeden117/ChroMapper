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

public class SongInfoEditUI : MonoBehaviour {

    private static List<string> VanillaEnvironments = new List<string>()
    {
        "DefaultEnvironment",
        "BigMirrorEnvironment",
        "TriangleEnvironment",
        "NiceEnvironment",
        "KDAEnvironment",
        "MonstercatEnvironment",
        "CrabRaveEnvironment",
        "DragonsEnvironment",
        "Origins",
        "PanicEnvironment",
    };

    private static Dictionary<string, string> CustomPlatformNameToModelSaberHash = new Dictionary<string, string>()
    {
        { "Vapor Frame", "3b1f37e53a15b70a24943d325e3801b0" },
        { "Big Mirror V2", "0811b77d81ae58f61e37962126b63c68" },
        { "Dueling Dragons", "" },
        { "Collider", "" },
    };
    
    public static int GetCustomPlatformsIndexFromString(string platforms)
    {
        return CustomPlatformNameToModelSaberHash.Keys.ToList().IndexOf(platforms);
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
        get
        {
            string name = string.Join("", characteristicDropdown.captionText.text.Split(' '));
            return songDifficultySets.Where(x => x.beatmapCharacteristicName == name).FirstOrDefault();
        }
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
    [SerializeField] int selectedBeatmapSet = 0;
    [SerializeField] Toggle WillChromaBeRequired;
    [SerializeField] Toggle ChromaRequirement;
    [SerializeField] Toggle ChromaToggleRequirement;
    [SerializeField] Toggle MappingExtensionsRequirement;

    [SerializeField] GameObject difficultyExistsPanel;
    [SerializeField] GameObject difficultyNoExistPanel;
    [SerializeField] TMP_Dropdown difficultyDifficultyDropdown;

    [SerializeField] InputField audioPath;
    [SerializeField] InputField offset;
    [SerializeField] InputField difficultyLabel;
    [SerializeField] InputField noteJumpSpeed;

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

        Song.environmentName = GetEnvironmentNameFromID(environmentDropdown.value);

        if (Song.customData == null) Song.customData = new JSONObject();

        if (customPlatformsDropdown.value == 0)
        {
            Song.customData["_customEnvironment"] = "";
            Song.customData["_customEnvironmentHash"] = "";
        }
        else
        {
            string hash;
            Song.customData["_customEnvironment"] = customPlatformsDropdown.captionText.text;
            if (CustomPlatformNameToModelSaberHash.TryGetValue(customPlatformsDropdown.captionText.text, out hash))
                Song.customData["_customEnvironmentHash"] = hash;
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
        offset.text = Song.songTimeOffset.ToString();

        bpmField.text = Song.beatsPerMinute.ToString();
        prevStartField.text = Song.previewStartTime.ToString();
        prevDurField.text = Song.previewDuration.ToString();
        environmentDropdown.value = GetEnvironmentIDFromString(Song.environmentName);

        if (Song.customData != null)
        {
            if (Song.customData["_customEnvironment"] != null && Song.customData["_customEnvironment"] != "")
                customPlatformsDropdown.value = GetCustomPlatformsIndexFromString(Song.customData["_customEnvironment"]) + 1;
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
            characteristicDropdown.value = characteristicDropdown.options.IndexOf(characteristicDropdown.options.Where(x => x.text ==
            AddSpacesToSentence(songDifficultySets[selectedBeatmapSet].beatmapCharacteristicName)).FirstOrDefault());
        }

        if (initial) {
            InitializeDifficultyPanel();
        }

    }

    private string AddSpacesToSentence(string text, bool preserveAcronyms = false) //@Binary Worrier | StackOverflow
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        StringBuilder newText = new StringBuilder(text.Length * 2);
        newText.Append(text[0]);
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
                if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                    (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                     i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                    newText.Append(' ');
            newText.Append(text[i]);
        }
        return newText.ToString();
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
        if (oldPath != null) File.Delete(oldPath); //This should properly "convert" difficulties just fine
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
                Debug.Log("E+");
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
        map.Save();
        songDifficultyData[selectedDifficultyIndex].noteJumpMovementSpeed = float.Parse(noteJumpSpeed.text);
        if (difficultyLabel.text != "")
            songDifficultyData[selectedDifficultyIndex].customData["_difficultyLabel"] = difficultyLabel.text;
        else songDifficultyData[selectedDifficultyIndex].customData.Remove("_difficultyLabel");

        JSONArray requiredArray = new JSONArray();
        JSONArray suggestedArray = new JSONArray();
        if (WillChromaBeRequired.isOn && HasChromaEvents()) requiredArray.Add(new JSONString("Chroma"));
        else if (HasChromaEvents()) suggestedArray.Add(new JSONString("Chroma Lighting Events"));
        if (MappingExtensionsRequirement.isOn) requiredArray.Add(new JSONString("Mapping Extensions"));
        if (ChromaToggleRequirement.isOn) requiredArray.Add(new JSONString("ChromaToggle"));
        songDifficultyData[selectedDifficultyIndex].customData["_suggestions"] = suggestedArray;
        songDifficultyData[selectedDifficultyIndex].customData["_requirements"] = requiredArray;

        SelectedSet.difficultyBeatmaps = songDifficultyData;
        Song.difficultyBeatmapSets = songDifficultySets;
        Song.SaveSong();
        InitializeDifficultyPanel(selectedDifficultyIndex);
    }

    public void LoadDifficulty() {
        if (songDifficultyData[selectedDifficultyIndex].customData != null)
        {
            if (songDifficultyData[selectedDifficultyIndex].customData["_difficultyLabel"] != null)
                difficultyLabel.text = songDifficultyData[selectedDifficultyIndex].customData["_difficultyLabel"].Value;
        }
        noteJumpSpeed.text = songDifficultyData[selectedDifficultyIndex].noteJumpMovementSpeed.ToString();

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

        try
        {
            BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(songDifficultyData[selectedDifficultyIndex]);
            foreach(BeatmapNote note in map?._notes)
            {
                if (note._lineIndex < 0 || note._lineIndex > 3)
                {
                    MappingExtensionsRequirement.isOn = true;
                    break;
                }
            }
            foreach (BeatmapObstacle ob in map?._obstacles)
            {
                if (ob._lineIndex < 0 || ob._lineIndex > 3 || ob._type >= 2 || ob._width >= 1000)
                {
                    MappingExtensionsRequirement.isOn = true;
                    break;
                }
            }
            if (HasChromaEvents() && WillChromaBeRequired.isOn) ChromaRequirement.isOn = true;
            //TODO ChromaToggle. Don't know how to detect legacy ChromaToggle w/o potentially freezing ChroMapper
        }
        catch { }

    }

    public void ToggleChromaRequirement(bool Required)
    {
        if (Required && HasChromaEvents()) ChromaRequirement.isOn = true;
        else ChromaRequirement.isOn = false;
    }

    private bool HasChromaEvents()
    {
        try
        {
            BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(songDifficultyData[selectedDifficultyIndex]);
            foreach (MapEvent mapevent in map._events)
                if (mapevent._value > ColourManager.RGB_INT_OFFSET) return true;
            return false;
        }
        catch { return false; }
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
        selectedBeatmapSet = characteristicDropdown.value;
        if (SelectedSet != null)
        {
            selectedBeatmapSet = songDifficultySets.IndexOf(SelectedSet);
            songDifficultyData = songDifficultySets[selectedBeatmapSet].difficultyBeatmaps;
            selectedDifficultyIndex = songDifficultyData.Any() ? 0 : -1;
        }
        else selectedDifficultyIndex = -1;
        InitializeDifficultyPanel(selectedDifficultyIndex);
    }

    public void CreateNewDifficultyData()
    {
        if (!songDifficultySets.Any() || SelectedSet == null)
        {
            BeatSaberSong.DifficultyBeatmapSet set = new BeatSaberSong.DifficultyBeatmapSet(
                string.Join("", characteristicDropdown.captionText.text.Split(' ')));
            songDifficultySets.Add(set);
        }
        BeatSaberSong.DifficultyBeatmap data = new BeatSaberSong.DifficultyBeatmap(SelectedSet);
        songDifficultyData.Add(data);
        InitializeDifficultyPanel();
        PersistentUI.Instance.DisplayMessage("Be sure to save before editing the map!", PersistentUI.DisplayMessageType.BOTTOM);
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

    public void OpenSelectedMapInFileBrowser()
    {
        try
        {
            Debug.Log("Opening song directory with Windows...");
            string winPath = Song.directory.Replace("/", "\\");
            System.Diagnostics.Process.Start("explorer.exe", "/root," + winPath);
        }catch
        {
            Debug.Log("Windows opening failed, attempting Mac...");
            try
            {
                string macPath = Song.directory.Replace("\\", "/");
                if (!macPath.StartsWith("\"")) macPath = "\"" + macPath;
                if (!macPath.EndsWith("\"")) macPath = macPath + "\"";
                System.Diagnostics.Process.Start("open", macPath);
            }
            catch
            {
                Debug.Log("What is this, some UNIX bullshit?");
                PersistentUI.Instance.DisplayMessage("Unrecognized OS!", PersistentUI.DisplayMessageType.BOTTOM);
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

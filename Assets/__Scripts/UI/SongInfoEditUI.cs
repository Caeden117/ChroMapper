using System.IO;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using SimpleJSON;
using UnityEngine.Networking;
using System.Globalization;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using static UnityEngine.InputSystem.InputAction;

public class SongInfoEditUI : MenuBase
{
    [SerializeField] private AudioSource previewAudio;
    private string loadedSong = null;

    private class Environment
    {
        public readonly string humanName;
        public readonly string jsonName;

        public Environment(string humanName, string jsonName)
        {
            this.humanName = humanName;
            this.jsonName = jsonName;
        }
    }

    private static List<Environment> VanillaEnvironments = new List<Environment>()
    {
        new Environment("Default", "DefaultEnvironment"),
        new Environment("Big Mirror", "BigMirrorEnvironment"),
        new Environment("Triangle", "TriangleEnvironment"),
        new Environment("Nice", "NiceEnvironment"),
        new Environment("K/DA", "KDAEnvironment"),
        new Environment("Monstercat", "MonstercatEnvironment"),
        new Environment("Dragons", "DragonsEnvironment"),
        new Environment("Origins", "Origins"),
        new Environment("Crab Rave", "CrabRaveEnvironment"),
        new Environment("Panic! At The Disco", "PanicEnvironment"),
        new Environment("Rocket League", "RocketEnvironment"),
        //"GreenDayEnvironment",
        //"GreenDayGrenadeEnvironment",
        new Environment("Timbaland", "TimbalandEnvironment"),
        new Environment("FitBeat", "FitBeatEnvironment")
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
        return VanillaEnvironments.TakeWhile(i => i.jsonName != environment).Count();
    }

    public static string GetEnvironmentNameFromID(int id) {
        return VanillaEnvironments[id].jsonName;
    }

    BeatSaberSong Song {
        get { return BeatSaberSongContainer.Instance.song; }
    }

    [SerializeField] TMP_InputField nameField;
    [SerializeField] TMP_InputField subNameField;
    [SerializeField] TMP_InputField songAuthorField;
    [SerializeField] TMP_InputField authorField;
    [SerializeField] TMP_InputField coverImageField;

    [SerializeField] TMP_InputField bpmField;
    [SerializeField] TMP_InputField prevStartField;
    [SerializeField] TMP_InputField prevDurField;
    
    [SerializeField] TMP_Dropdown environmentDropdown;
    [SerializeField] TMP_Dropdown customPlatformsDropdown;

    [SerializeField] TMP_InputField audioPath;
    [SerializeField] TMP_InputField offset;

    [SerializeField] Image revertInfoButtonImage;

    void Start() {
        if (BeatSaberSongContainer.Instance == null) {
            SceneManager.LoadScene(0);
            return;
        }

        LoadFromSong();
    }

    protected override GameObject GetDefault()
    {
        return nameField.gameObject;
    }

    public override void OnLeaveMenu(CallbackContext context)
    {
        ReturnToSongList();
    }

    public void SaveToSong() {
        Song.songName = nameField.text;
        Song.songSubName = subNameField.text;
        Song.songAuthorName = songAuthorField.text;
        Song.levelAuthorName = authorField.text;
        Song.coverImageFilename = coverImageField.text;
        Song.songFilename = audioPath.text;

        Song.beatsPerMinute = float.TryParse(bpmField.text, out float bpm) ? bpm : 100;
        Song.previewStartTime = float.TryParse(prevStartField.text, out float previewStart) ? previewStart : 12;
        Song.previewDuration = float.TryParse(prevDurField.text, out float previewDuration) ? previewDuration : 10;
        Song.songTimeOffset = float.TryParse(offset.text, out float offsetFloat) ? offsetFloat : 0;

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
            Song.customData["_customEnvironment"] = customPlatformsDropdown.captionText.text;
            if (CustomPlatformsLoader.Instance.GetAllEnvironments().TryGetValue(customPlatformsDropdown.captionText.text, out PlatformInfo info))
                Song.customData["_customEnvironmentHash"] = info.Md5Hash;
        }
        else
        {
            Song.customData.Remove("_customEnvironment");
            Song.customData.Remove("_customEnvironmentHash");
        }

        Song.SaveSong();

        coverImageField.GetComponent<InputBoxFileValidator>().OnUpdate();
        audioPath.GetComponent<InputBoxFileValidator>().OnUpdate();
        ReloadAudio();

        PersistentUI.Instance.DisplayMessage("Song Info Saved!", PersistentUI.DisplayMessageType.BOTTOM);
    }

    public void LoadFromSong()
    {
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
        environmentDropdown.AddOptions(VanillaEnvironments.Select(it => it.humanName).ToList());
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

        ReloadAudio();
    }

    public void ReloadAudio()
    {
        StartCoroutine(LoadAudio());
    }

    private IEnumerator LoadAudio(bool useTemp = true)
    {
        Debug.Log("Loading audio");
        string fullPath = Path.Combine(Song.directory, useTemp ? audioPath.text : Song.songFilename);

        if (fullPath == loadedSong)
        {
            yield break;
        }

        if (File.Exists(fullPath))
        {
            if (audioPath.text.ToLower().EndsWith("ogg") || audioPath.text.ToLower().EndsWith("egg"))
            {
                Debug.Log("Lets go");
                UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{fullPath}")}", AudioType.OGGVORBIS);
                //((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;
                //Escaping should fix the issue where half the people can't open ChroMapper's editor (I believe this is caused by spaces in the directory, hence escaping)
                yield return www.SendWebRequest();
                Debug.Log("Song loaded!");
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip == null)
                {
                    Debug.Log("Error getting Audio data!");
                    SceneTransitionManager.Instance.CancelLoading("Error getting Audio data!");
                }
                loadedSong = fullPath;
                clip.name = "Song";
                previewAudio.clip = clip;
                BeatSaberSongContainer.Instance.loadedSong = clip;
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
            Debug.Log(fullPath);
        }
    }

    public void SaveDifficulty()
    {
        Debug.Log("Nothing happens");

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
            FileOperationAPIWrapper.MoveToRecycleBin(Song.directory);
            ReturnToSongList();
        } //Middle button (ID 1) would be pressed; the user doesn't want to delete the map, so we do nothing.
    }

    public void PackageZip()
    {
        string zipPath = Path.Combine(Song.directory, Song.songName + ".zip");
        File.Delete(zipPath);

        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(Path.Combine(Song.directory, "info.dat"), "info.dat");
            archive.CreateEntryFromFile(Path.Combine(Song.directory, Song.coverImageFilename), Song.coverImageFilename);
            archive.CreateEntryFromFile(Path.Combine(Song.directory, Song.songFilename), Song.songFilename);

            foreach (var contributor in Song.contributors)
            {
                string imageLocation = Path.Combine(Song.directory, contributor.LocalImageLocation);
                if (File.Exists(imageLocation) && !File.GetAttributes(imageLocation).HasFlag(FileAttributes.Directory))
                {
                    archive.CreateEntryFromFile(imageLocation, contributor.LocalImageLocation);
                }
            }

            foreach (var set in Song.difficultyBeatmapSets)
            {
                foreach (var map in set.difficultyBeatmaps)
                {
                    archive.CreateEntryFromFile(Path.Combine(Song.directory, map.beatmapFilename), map.beatmapFilename);
                }
            }
        }
        OpenSelectedMapInFileBrowser();
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
        if (BeatSaberSongContainer.Instance.difficultyData == null)
        {
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

        BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(BeatSaberSongContainer.Instance.difficultyData);
        PersistentUI.UpdateBackground(Song);
        Debug.Log("Loading Song...");
        TransitionToEditor(map);
    }

    void TransitionToEditor(BeatSaberMap map)
    {
        Debug.Log("Transitioning...");
        if (map != null)
        {
            BeatSaberSongContainer.Instance.map = map;
            SceneTransitionManager.Instance.LoadScene(3, LoadAudio(false));
        }
    }

    public void EditContributors()
    {
        SceneTransitionManager.Instance.LoadScene(5);
    }

    public void SpinIt()
    {
        _reloadSongDataCoroutine = StartCoroutine(SpinReloadSongDataButton());
        LoadFromSong();
    }

    private Coroutine _reloadSongDataCoroutine;
    private IEnumerator SpinReloadSongDataButton()
    {
        if (_reloadSongDataCoroutine != null) StopCoroutine(_reloadSongDataCoroutine);

        float startTime = Time.time;
        var transform1 = revertInfoButtonImage.transform;
        Quaternion rotationQ = transform1.rotation;
        Vector3 rotation = rotationQ.eulerAngles;
        rotation.z = -330;
        transform1.rotation = Quaternion.Euler(rotation);

        while (true)
        {
            float rot = rotation.z;
            float timing = (Time.time / startTime) * 0.075f;
            rot = Mathf.Lerp(rot, 30f, timing);
            rotation.z = rot;
            transform1.rotation = Quaternion.Euler(rotation);

            if (rot >= 25f)
            {
                rotation.z = 30;
                transform1.rotation = Quaternion.Euler(rotation);
                yield break;
            }
            yield return new WaitForFixedUpdate();
        }
    }

}

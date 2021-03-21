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
    public Action TempSongLoadedEvent;

    [SerializeField] private AudioSource previewAudio;
    private string loadedSong = null;

    public class Environment
    {
        public readonly string humanName;
        public readonly string jsonName;

        public Environment(string humanName, string jsonName)
        {
            this.humanName = humanName;
            this.jsonName = jsonName;
        }
    }

    private static Dictionary<string, AudioType> ExtensionToAudio = new Dictionary<string, AudioType>()
    {
        {"ogg", AudioType.OGGVORBIS},
        {"egg", AudioType.OGGVORBIS},
        {"wav", AudioType.WAV}
    };

    public static List<Environment> VanillaEnvironments = new List<Environment>()
    {
        new Environment("Default", "DefaultEnvironment"),
        new Environment("Big Mirror", "BigMirrorEnvironment"),
        new Environment("Triangle", "TriangleEnvironment"),
        new Environment("Nice", "NiceEnvironment"),
        new Environment("K/DA", "KDAEnvironment"),
        new Environment("Monstercat", "MonstercatEnvironment"),
        new Environment("Dragons", "DragonsEnvironment"),
        new Environment("Origins", "OriginsEnvironment"), //i swear to god if beat games reverts this back i am going to lose my shit
        new Environment("Crab Rave", "CrabRaveEnvironment"),
        new Environment("Panic! At The Disco", "PanicEnvironment"),
        new Environment("Rocket League", "RocketEnvironment"),
        new Environment("Green Day", "GreenDayEnvironment"),
        new Environment("Green Day Grenade", "GreenDayGrenadeEnvironment"),
        new Environment("Timbaland", "TimbalandEnvironment"),
        new Environment("FitBeat", "FitBeatEnvironment"),
        new Environment("Linkin Park", "LinkinParkEnvironment"),
        new Environment("BTS", "BTSEnvironment"),
        new Environment("Kaleidoscope", "KaleidoscopeEnvironment")
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
        int result = VanillaEnvironments.TakeWhile(i => i.jsonName != environment).Count();
        return result == VanillaEnvironments.Count ? 0 : result;
    }

    public static string GetEnvironmentNameFromID(int id) {
        return VanillaEnvironments[id].jsonName;
    }

    BeatSaberSong Song {
        get { return BeatSaberSongContainer.Instance.song; }
    }

    [SerializeField] DifficultySelect difficultySelect;
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

    [SerializeField] ContributorsController contributorController;

    void Start() {
        if (BeatSaberSongContainer.Instance == null) {
            SceneManager.LoadScene(0);
            return;
        }

        LoadFromSong();
    }

    /// <summary>
    /// Default object to select when pressing Tab and nothing is selected
    /// </summary>
    /// <returns>A GUI object</returns>
    protected override GameObject GetDefault()
    {
        return nameField.gameObject;
    }

    /// <summary>
    /// Callback for when escape is pressed, user wants out of here
    /// </summary>
    /// <param name="context">Information about the event</param>
    public override void OnLeaveMenu(CallbackContext context)
    {
        if (context.performed) ReturnToSongList();
    }

    /// <summary>
    /// Save the changes the user has made in the song info panel
    /// </summary>
    public void SaveToSong() {
        Song.songName = nameField.text;
        Song.songSubName = subNameField.text;
        Song.songAuthorName = songAuthorField.text;
        Song.levelAuthorName = authorField.text;
        Song.coverImageFilename = coverImageField.text;
        Song.songFilename = audioPath.text;

        Song.beatsPerMinute = GetTextValue(bpmField);
        Song.previewStartTime = GetTextValue(prevStartField);
        Song.previewDuration = GetTextValue(prevDurField);
        Song.songTimeOffset = GetTextValue(offset);

        if (Song.songTimeOffset > 0)
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "songtimeoffset.warning", null,
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

        contributorController.Commit();
        Song.contributors = contributorController.contributors;

        Song.SaveSong();

        // Trigger validation checks, if this is the first save they will not have been done yet
        coverImageField.GetComponent<InputBoxFileValidator>().OnUpdate();
        audioPath.GetComponent<InputBoxFileValidator>().OnUpdate();
        ReloadAudio();

        PersistentUI.Instance.DisplayMessage("SongEditMenu", "saved", PersistentUI.DisplayMessageType.BOTTOM);
    }

    /// <summary>
    /// Populate UI from song data
    /// </summary>
    public void LoadFromSong()
    {
        nameField.text = Song.songName;
        subNameField.text = Song.songSubName;
        songAuthorField.text = Song.songAuthorName;
        authorField.text = Song.levelAuthorName;

        BroadcastMessage("OnValidate"); // god unity why are you so dumb

        coverImageField.text = Song.coverImageFilename;
        audioPath.text = Song.songFilename;

        offset.text = Song.songTimeOffset.ToString(CultureInfo.InvariantCulture);
        if (Song.songTimeOffset > 0)
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "songtimeoffset.warning", null,
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

        customPlatformsDropdown.value = CustomPlatformFromSong();
        if (customPlatformsDropdown.value == 0)
        {
            customPlatformsDropdown.captionText.text = "None";
        }

        contributorController.UndoChanges();

        ReloadAudio();
    }

    /// <summary>
    /// Get the id for the custom platform specified in the song data
    /// </summary>
    /// <returns>Custom platform index</returns>
    private int CustomPlatformFromSong()
    {
        if (Song.customData != null)
        {
            if (Song.customData["_customEnvironment"] != null && Song.customData["_customEnvironment"] != "")
            {
                return CustomPlatformsLoader.Instance.GetAllEnvironmentIds().IndexOf(Song.customData["_customEnvironment"]) + 1;
            }
            else
            { //For some reason the text defaults to "Dueling Dragons", not what we want.
                return 0;
            }
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Start the LoadAudio Coroutine
    /// </summary>
    public void ReloadAudio()
    {
        StartCoroutine(LoadAudio());
    }

    /// <summary>
    /// Try and load the song, this is used for the song preview as well as later
    /// passed to the mapping scene
    /// </summary>
    /// <param name="useTemp">Should we load the song the user has updated in the UI or from the saved song data</param>
    /// <returns>Coroutine IEnumerator</returns>
    private IEnumerator LoadAudio(bool useTemp = true)
    {
        if (Song.directory == null)
        {
            yield break;
        }

        string fullPath = Path.Combine(Song.directory, useTemp ? audioPath.text : Song.songFilename);

        if (fullPath == loadedSong)
        {
            yield break;
        }

        Debug.Log("Loading audio");
        if (File.Exists(fullPath))
        {

            var extension = audioPath.text.Contains(".") ? Path.GetExtension(audioPath.text.ToLower()).Replace(".", "") : "";


            if (!string.IsNullOrEmpty(extension) && ExtensionToAudio.ContainsKey(extension))
            {
                Debug.Log("Lets go");
                var audioType = ExtensionToAudio[extension];
                UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{fullPath}")}", audioType);
                //Escaping should fix the issue where half the people can't open ChroMapper's editor (I believe this is caused by spaces in the directory, hence escaping)
                yield return www.SendWebRequest();
                Debug.Log("Song loaded!");
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip == null)
                {
                    Debug.Log("Error getting Audio data!");
                    SceneTransitionManager.Instance.CancelLoading("load.error.audio");
                }
                loadedSong = fullPath;
                clip.name = "Song";
                previewAudio.clip = clip;
                BeatSaberSongContainer.Instance.loadedSong = clip;

                if (useTemp)
                {
                    TempSongLoadedEvent?.Invoke();
                }
            }
            else
            {
                Debug.Log("Incompatible file type! WTF!?");
                SceneTransitionManager.Instance.CancelLoading("load.error.audio2");
            }
        }
        else
        {
            SceneTransitionManager.Instance.CancelLoading("load.error.audio3");
            Debug.Log("Song does not exist! WTF!?");
            Debug.Log(fullPath);
        }
    }

    /// <summary>
    /// Check the user wants to delete the map
    /// </summary>
    public void DeleteMap()
    {
        PersistentUI.Instance.ShowDialogBox("SongEditMenu", "delete.dialog", HandleDeleteMap,
            PersistentUI.DialogBoxPresetType.YesNo, new object[] { Song.songName });
    }

    /// <summary>
    /// Delete the map, it's still recoverable externally
    /// </summary>
    /// <param name="result">Confirmation from the user</param>
    private void HandleDeleteMap(int result)
    {
        if (result == 0) //Left button (ID 0) pressed; the user wants to delete the map.
        {
            FileOperationAPIWrapper.MoveToRecycleBin(Song.directory);
            ReturnToSongList();
        } //Middle button (ID 1) would be pressed; the user doesn't want to delete the map, so we do nothing.
    }

    private void AddToZip(ZipArchive archive, string fileLocation)
    {
        string fullPath = Path.Combine(Song.directory, fileLocation);
        if (File.Exists(fullPath))
        {
            archive.CreateEntryFromFile(fullPath, fileLocation);
        }
    }

    /// <summary>
    /// Create a zip for sharing the map
    /// </summary>
    public void PackageZip()
    {
        string infoFileLocation = "";
        string zipPath = "";
        if (Song.directory != null)
        {
            zipPath = Path.Combine(Song.directory, Song.cleanSongName + ".zip");
            // Mac doesn't seem to like overwriting existing zips, so delete the old one first
            File.Delete(zipPath);

            infoFileLocation = Path.Combine(Song.directory, "info.dat");
        }

        if (!File.Exists(infoFileLocation))
        {
            Debug.LogError(":hyperPepega: :mega: WHY TF ARE YOU TRYING TO PACKAGE A MAP WITH NO INFO.DAT FILE");
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "zip.warning", null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(infoFileLocation, "Info.dat"); //oh yeah lolpants is gonna kill me if it isnt packaged as "Info.dat"

            AddToZip(archive, Song.coverImageFilename);
            AddToZip(archive, Song.songFilename);

            foreach (var contributor in Song.contributors.DistinctBy(it => it.LocalImageLocation))
            {
                string imageLocation = Path.Combine(Song.directory, contributor.LocalImageLocation);
                if (contributor.LocalImageLocation != Song.coverImageFilename &&
                    File.Exists(imageLocation) && !File.GetAttributes(imageLocation).HasFlag(FileAttributes.Directory))
                {
                    archive.CreateEntryFromFile(imageLocation, contributor.LocalImageLocation);
                }
            }

            foreach (var set in Song.difficultyBeatmapSets)
            {
                foreach (var map in set.difficultyBeatmaps)
                {
                    AddToZip(archive, map.beatmapFilename);
                }
            }
        }
        OpenSelectedMapInFileBrowser();
    }

    /// <summary>
    /// Open the folder containing the map's files in a native file browser
    /// </summary>
    public void OpenSelectedMapInFileBrowser()
    {
        try
        {
            string winPath = Song.directory.Replace("/", "\\").Replace("\\\\", "\\");
            Debug.Log($"Opening song directory ({winPath}) with Windows...");
            System.Diagnostics.Process.Start("explorer.exe", $"\"{winPath}\"");
        }catch
        {
            if (Song.directory == null)
            {
                PersistentUI.Instance.ShowDialogBox("SongEditMenu", "explorer.warning", null,
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

    /// <summary>
    /// Return the the song list scene, if the user has unsaved changes ask first
    /// </summary>
    public void ReturnToSongList() {
        // Do nothing if a dialog is open
        if (PersistentUI.Instance.DialogBox_IsEnabled) return;

        CheckForChanges(HandleReturnToSongList);
    }

    /// <summary>
    /// Return the the song list scene
    /// </summary>
    /// <param name="r">Confirmation from the user</param>
    public void HandleReturnToSongList(int r)
    {
        if (r == 0)
        {
            SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
        }
    }

    /// <summary>
    /// The user wants to edit the map
    /// Check first that some objects are enabled and that there are no unsaved changes
    /// </summary>
    public void EditMapButtonPressed() {
        // If no difficulty is selected or there is a dialog open do nothing
        if (BeatSaberSongContainer.Instance.difficultyData == null || PersistentUI.Instance.DialogBox_IsEnabled)
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
                "SongEditMenu", "load.warning",
                null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }
        else if (!(a && b && c && d))
        {
            PersistentUI.Instance.ShowDialogBox(
                "SongEditMenu", "load.warning2",
                null, PersistentUI.DialogBoxPresetType.Ok);
            
        }

        CheckForChanges(HandleEditMapButtonPressed);
    }

    /// <summary>
    /// Load the editor scene
    /// </summary>
    /// <param name="r">Confirmation from the user</param>
    private void HandleEditMapButtonPressed(int r)
    {
        if (r == 0)
        {
            BeatSaberMap map = Song.GetMapFromDifficultyBeatmap(BeatSaberSongContainer.Instance.difficultyData);
            PersistentUI.UpdateBackground(Song);

            Debug.Log("Transitioning...");
            if (map != null)
            {
                Settings.Instance.LastLoadedMap = Song.directory;
                Settings.Instance.LastLoadedChar = BeatSaberSongContainer.Instance.difficultyData.parentBeatmapSet.beatmapCharacteristicName;
                Settings.Instance.LastLoadedDiff = BeatSaberSongContainer.Instance.difficultyData.difficulty;
                BeatSaberSongContainer.Instance.map = map;
                SceneTransitionManager.Instance.LoadScene("03_Mapper", LoadAudio(false));
            }
        }
    }

    /// <summary>
    /// Helper methods to prompt the user if there are unsaved changes
    /// Will call the callback immediately if there are none
    /// </summary>
    /// <param name="callback">Method to call when the user has made a decision</param>
    /// <returns>True if a dialog has been opened, false otherwise</returns>
    private bool CheckForChanges(Action<int> callback)
    {
        if (IsDirty())
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "unsaved.warning", callback,
            PersistentUI.DialogBoxPresetType.YesNo);
            return true;
        }
        else if (difficultySelect.IsDirty())
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "unsaveddiff.warning", callback,
            PersistentUI.DialogBoxPresetType.YesNo);
            return true;
        }
        else if (contributorController.IsDirty())
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "unsavedcontributor.warning", callback,
            PersistentUI.DialogBoxPresetType.YesNo);
            return true;
        }
        callback(0);
        return false;
    }

    /// <summary>
    /// Edit contributors button has been pressed
    /// Check there are no unsaved changes
    /// </summary>
    public void EditContributors()
    {
        // Do nothing if a dialog is open
        if (PersistentUI.Instance.DialogBox_IsEnabled) return;

        var wrapper = contributorController.transform.parent.gameObject;
        wrapper.SetActive(!wrapper.activeSelf);
    }

    /// <summary>
    /// Undo button has been pressed, trigger animation and reload the song data
    /// </summary>
    public void UndoChanges()
    {
        _reloadSongDataCoroutine = StartCoroutine(SpinReloadSongDataButton());

        var wrapper = contributorController.transform.parent.gameObject;
        if (wrapper.activeSelf)
        {
            contributorController.UndoChanges();
            return;
        }

        LoadFromSong();
    }

    private Coroutine _reloadSongDataCoroutine;
    /// <summary>
    /// Spins the undo button for extra flare
    /// </summary>
    /// <returns>Coroutine IEnumerator</returns>
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

    /// <summary>
    /// Helper method to get the float value from a UI element
    /// Returns the placeholder value if the field is empty
    /// </summary>
    /// <param name="inputfield">Text field to get the value from</param>
    /// <returns>The value parsed to a float</returns>
    private static float GetTextValue(TMP_InputField inputfield)
    {
        if (!float.TryParse(inputfield.text, out float result))
        {
            if (!float.TryParse(inputfield.placeholder.GetComponent<TMP_Text>().text, out result))
            {
                // How have you changed the placeholder so that it isn't valid?
                result = 0;
            }
        }
        return result;
    }

    /// <summary>
    /// Check if any changes have been made from the original song data
    /// </summary>
    /// <returns>True if user has made changes, false otherwise</returns>
    private bool IsDirty()
    {
        return Song.songName != nameField.text ||
            Song.songSubName != subNameField.text ||
            Song.songAuthorName != songAuthorField.text ||
            Song.levelAuthorName != authorField.text ||
            Song.coverImageFilename != coverImageField.text ||
            Song.songFilename != audioPath.text ||
            !NearlyEqual(Song.beatsPerMinute, GetTextValue(bpmField)) ||
            !NearlyEqual(Song.previewStartTime, GetTextValue(prevStartField)) ||
            !NearlyEqual(Song.previewDuration, GetTextValue(prevDurField)) ||
            !NearlyEqual(Song.songTimeOffset, GetTextValue(offset)) ||
            environmentDropdown.value != GetEnvironmentIDFromString(Song.environmentName) ||
            customPlatformsDropdown.value != CustomPlatformFromSong();
    }

    private static bool NearlyEqual(float a, float b, float epsilon = 0.01f)
    {
        return a.Equals(b) || Math.Abs(a - b) < epsilon;
    }

}

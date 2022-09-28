﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using JetBrains.Annotations;
using QuestDumper;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;
using Debug = UnityEngine.Debug;

public class SongInfoEditUI : MenuBase
{
    public static readonly Dictionary<string, AudioType> ExtensionToAudio = new Dictionary<string, AudioType>
    {
        {"ogg", AudioType.OGGVORBIS}, {"egg", AudioType.OGGVORBIS}, {"wav", AudioType.WAV}
    };

    public static List<Environment> VanillaEnvironments = new List<Environment>
    {
        new Environment("Default", "DefaultEnvironment"),
        new Environment("Big Mirror", "BigMirrorEnvironment"),
        new Environment("Triangle", "TriangleEnvironment"),
        new Environment("Nice", "NiceEnvironment"),
        new Environment("K/DA", "KDAEnvironment"),
        new Environment("Monstercat", "MonstercatEnvironment"),
        new Environment("Dragons", "DragonsEnvironment"),
        new Environment("Origins",
            "OriginsEnvironment"), //i swear to god if beat games reverts this back i am going to lose my shit
        new Environment("Crab Rave", "CrabRaveEnvironment"),
        new Environment("Panic! At The Disco", "PanicEnvironment"),
        new Environment("Rocket League", "RocketEnvironment"),
        new Environment("Green Day", "GreenDayEnvironment"),
        new Environment("Green Day Grenade", "GreenDayGrenadeEnvironment"),
        new Environment("Timbaland", "TimbalandEnvironment"),
        new Environment("FitBeat", "FitBeatEnvironment"),
        new Environment("Linkin Park", "LinkinParkEnvironment"),
        new Environment("BTS", "BTSEnvironment"),
        new Environment("Kaleidoscope", "KaleidoscopeEnvironment"),
        new Environment("Interscope", "InterscopeEnvironment"),
        new Environment("Skrillex", "SkrillexEnvironment"),
        new Environment("Billie", "BillieEnvironment"),
        new Environment("Spooky", "HalloweenEnvironment")
    };

    private static readonly List<string> vanillaDirectionalEnvironments = new List<string> { "GlassDesertEnvironment" };

    public static List<string> CharacteristicDropdownToBeatmapName = new List<string>
    {
        "Standard",
        "NoArrows",
        "OneSaber",
        "360Degree",
        "90Degree",
        "Lightshow",
        "Lawless"
    };

    [SerializeField] private AudioSource previewAudio;

    [SerializeField] private DifficultySelect difficultySelect;
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private TMP_InputField subNameField;
    [SerializeField] private TMP_InputField songAuthorField;
    [SerializeField] private TMP_InputField authorField;
    [SerializeField] private TMP_InputField coverImageField;

    [SerializeField] private TMP_InputField bpmField;
    [SerializeField] private TMP_InputField prevStartField;
    [SerializeField] private TMP_InputField prevDurField;

    [SerializeField] private TMP_Dropdown environmentDropdown;
    [SerializeField] private TMP_Dropdown customPlatformsDropdown;

    [SerializeField] private TMP_InputField audioPath;
    [SerializeField] private TMP_InputField offset;

    [SerializeField] private Image revertInfoButtonImage;

    [SerializeField] private ContributorsController contributorController;

    private Coroutine reloadSongDataCoroutine;
    public Action TempSongLoadedEvent;

    private BeatSaberSong Song => BeatSaberSongContainer.Instance.Song;
    private GameObject ContributorWrapper => contributorController.transform.parent.gameObject;

    [SerializeField] private GameObject questExportButton;
    private List<string> questCandidates = new List<string>();
    
    private void Start()
    {
        if (BeatSaberSongContainer.Instance == null)
        {
            SceneManager.LoadScene(0);
            return;
        }

        questExportButton.SetActive(Adb.IsAdbInstalled(out _));

        // Make sure the contributor panel has been initialised
        ContributorWrapper.SetActive(true);

        LoadFromSong();
    }

    public static int GetDirectionalEnvironmentIDFromString(string platforms) =>
        vanillaDirectionalEnvironments.IndexOf(platforms);

    public static int GetEnvironmentIDFromString(string environment)
    {
        var result = VanillaEnvironments.TakeWhile(i => i.JsonName != environment).Count();
        return result == VanillaEnvironments.Count ? 0 : result;
    }

    public static string GetEnvironmentNameFromID(int id) => VanillaEnvironments[id].JsonName;

    /// <summary>
    ///     Default object to select when pressing Tab and nothing is selected
    /// </summary>
    /// <returns>A GUI object</returns>
    protected override GameObject GetDefault() => nameField.gameObject;

    /// <summary>
    ///     Callback for when escape is pressed, user wants out of here
    /// </summary>
    /// <param name="context">Information about the event</param>
    public override void OnLeaveMenu(CallbackContext context)
    {
        if (context.performed) ReturnToSongList();
    }

    /// <summary>
    ///     Save the changes the user has made in the song info panel
    /// </summary>
    public void SaveToSong()
    {
        Song.SongName = nameField.text;
        Song.SongSubName = subNameField.text;
        Song.SongAuthorName = songAuthorField.text;
        Song.LevelAuthorName = authorField.text;
        Song.CoverImageFilename = coverImageField.text;
        Song.SongFilename = audioPath.text;

        Song.BeatsPerMinute = GetTextValue(bpmField);
        Song.PreviewStartTime = GetTextValue(prevStartField);
        Song.PreviewDuration = GetTextValue(prevDurField);
        Song.SongTimeOffset = GetTextValue(offset);

        Song.EnvironmentName = GetEnvironmentNameFromID(environmentDropdown.value);

        if (Song.CustomData == null) Song.CustomData = new JSONObject();

        if (customPlatformsDropdown.value > 0)
        {
            Song.CustomData["_customEnvironment"] = customPlatformsDropdown.captionText.text;
            if (CustomPlatformsLoader.Instance.GetAllEnvironments()
                .TryGetValue(customPlatformsDropdown.captionText.text, out var info))
            {
                Song.CustomData["_customEnvironmentHash"] = info.Md5Hash;
            }
        }
        else
        {
            Song.CustomData.Remove("_customEnvironment");
            Song.CustomData.Remove("_customEnvironmentHash");
        }

        contributorController.Commit();
        Song.Contributors = contributorController.Contributors;

        Song.SaveSong();

        // Update duration cache (This needs to be beneath SaveSong so that the directory is garaunteed to be created)
        // also dont forget to null check please thanks
        if (previewAudio.clip != null)
            SongListItem.SetDuration(this, Path.GetFullPath(Song.Directory), previewAudio.clip.length);

        // Trigger validation checks, if this is the first save they will not have been done yet
        coverImageField.GetComponent<InputBoxFileValidator>().OnUpdate();
        audioPath.GetComponent<InputBoxFileValidator>().OnUpdate();
        ReloadAudio();

        PersistentUI.Instance.DisplayMessage("SongEditMenu", "saved", PersistentUI.DisplayMessageType.Bottom);
    }

    /// <summary>
    ///     Populate UI from song data
    /// </summary>
    public void LoadFromSong()
    {
        nameField.text = Song.SongName;
        subNameField.text = Song.SongSubName;
        songAuthorField.text = Song.SongAuthorName;
        authorField.text = Song.LevelAuthorName;

        BroadcastMessage("OnValidate"); // god unity why are you so dumb

        coverImageField.text = Song.CoverImageFilename;
        audioPath.text = Song.SongFilename;

        offset.text = Song.SongTimeOffset.ToString(CultureInfo.InvariantCulture);

        bpmField.text = Song.BeatsPerMinute.ToString(CultureInfo.InvariantCulture);
        prevStartField.text = Song.PreviewStartTime.ToString(CultureInfo.InvariantCulture);
        prevDurField.text = Song.PreviewDuration.ToString(CultureInfo.InvariantCulture);

        environmentDropdown.ClearOptions();
        environmentDropdown.AddOptions(VanillaEnvironments.Select(it => it.HumanName).ToList());
        environmentDropdown.value = GetEnvironmentIDFromString(Song.EnvironmentName);

        customPlatformsDropdown.ClearOptions();
        customPlatformsDropdown.AddOptions(new List<string> { "None" });
        customPlatformsDropdown.AddOptions(CustomPlatformsLoader.Instance.GetAllEnvironmentIds());

        customPlatformsDropdown.value = CustomPlatformFromSong();
        if (customPlatformsDropdown.value == 0) customPlatformsDropdown.captionText.text = "None";

        contributorController.UndoChanges();

        ReloadAudio();
    }

    /// <summary>
    ///     Get the id for the custom platform specified in the song data
    /// </summary>
    /// <returns>Custom platform index</returns>
    private int CustomPlatformFromSong()
    {
        if (Song.CustomData != null)
        {
            if (Song.CustomData["_customEnvironment"] != null && Song.CustomData["_customEnvironment"] != "")
            {
                return CustomPlatformsLoader.Instance.GetAllEnvironmentIds()
                    .IndexOf(Song.CustomData["_customEnvironment"]) + 1;
            }

            return 0;
        }

        return 0;
    }

    /// <summary>
    ///     Start the LoadAudio Coroutine
    /// </summary>
    public void ReloadAudio() => StartCoroutine(LoadAudio());

    /// <summary>
    ///     Try and load the song, this is used for the song preview as well as later
    ///     passed to the mapping scene
    /// </summary>
    /// <param name="useTemp">Should we load the song the user has updated in the UI or from the saved song data</param>
    /// <returns>Coroutine IEnumerator</returns>
    private IEnumerator LoadAudio(bool useTemp = true, bool applySongTimeOffset = false)
    {
        if (Song.Directory == null) yield break;

        var fullPath = Path.Combine(Song.Directory, useTemp ? audioPath.text : Song.SongFilename);

        // Commented out since Song Time Offset changes need to reload the song, even if its the same file
        //if (fullPath == loadedSong)
        //{
        //    yield break;
        //}

        Debug.Log("Loading audio");
        if (File.Exists(fullPath))
        {
            var extension = audioPath.text.Contains(".")
                ? Path.GetExtension(audioPath.text.ToLower()).Replace(".", "")
                : "";


            if (!string.IsNullOrEmpty(extension) && ExtensionToAudio.ContainsKey(extension))
            {
                Debug.Log("Lets go");
                var audioType = ExtensionToAudio[extension];
                var www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{fullPath}")}",
                    audioType);
                //Escaping should fix the issue where half the people can't open ChroMapper's editor (I believe this is caused by spaces in the directory, hence escaping)
                yield return www.SendWebRequest();
                Debug.Log("Song loaded!");
                var clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip == null)
                {
                    Debug.Log("Error getting Audio data!");
                    SceneTransitionManager.Instance.CancelLoading("load.error.audio");
                }

                clip.name = "Song";

                if (GetTextValue(offset) != 0 && applySongTimeOffset)
                {
                    // Take songTimeOffset into account by adjusting clip data forward/backward

                    // Guaranteed to always be an integer multiple of the number of channels
                    var songTimeOffsetSamples =
                        Mathf.CeilToInt(float.Parse(offset.text) * clip.frequency) * clip.channels;
                    var samples = new float[clip.samples * clip.channels];

                    clip.GetData(samples, 0);

                    // Negative offset: Shift existing data forward, fill in beginning blank with 0s
                    if (songTimeOffsetSamples < 0)
                    {
                        Array.Resize(ref samples, samples.Length - songTimeOffsetSamples);

                        for (var i = samples.Length - 1; i >= 0; i--)
                        {
                            var shiftIndex = i + songTimeOffsetSamples;

                            samples[i] = shiftIndex < 0 ? 0 : samples[shiftIndex];
                        }
                    }
                    // Positive offset: Shift existing data backward, cut off ending blank
                    else
                    {
                        for (var i = 0; i < samples.Length; i++)
                        {
                            var shiftIndex = i + songTimeOffsetSamples;

                            samples[i] = shiftIndex >= samples.Length ? 0 : samples[shiftIndex];
                        }

                        // Bit of a hacky workaround, since you can't create an AudioClip with 0 length,
                        // and something in the spectrogram code doesn't like too short lengths either
                        // This just sets a minimum of 4096 samples per channel
                        Array.Resize(ref samples, Math.Max(samples.Length - songTimeOffsetSamples, clip.channels * 4096));
                    }

                    // Create a new AudioClip because apparently you can't change the length of an existing one
                    clip = AudioClip.Create(clip.name, samples.Length / clip.channels, clip.channels, clip.frequency, false);
                    clip.SetData(samples, 0);
                }

                previewAudio.clip = clip;
                BeatSaberSongContainer.Instance.LoadedSong = clip;

                if (useTemp) TempSongLoadedEvent?.Invoke();
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
    ///     Check the user wants to delete the map
    /// </summary>
    public void DeleteMap() =>
        PersistentUI.Instance.ShowDialogBox("SongEditMenu", "delete.dialog", HandleDeleteMap,
            PersistentUI.DialogBoxPresetType.YesNo, new object[] { Song.SongName });

    /// <summary>
    ///     Delete the map, it's still recoverable externally
    /// </summary>
    /// <param name="result">Confirmation from the user</param>
    private void HandleDeleteMap(int result)
    {
        if (result == 0) //Left button (ID 0) pressed; the user wants to delete the map.
        {
            FileOperationAPIWrapper.MoveToRecycleBin(Song.Directory);
            ReturnToSongList();
        } //Middle button (ID 1) would be pressed; the user doesn't want to delete the map, so we do nothing.
    }

    // Maps a dictionary as [fullFilePath] -> [location of file relative to the song directory]
    private void AddToFileDictionary(IDictionary<string, string> fileMap, string fileLocation)
    {
        var fullPath = Path.Combine(Song.Directory, fileLocation);
        if (File.Exists(fullPath))
        {
            fileMap.Add(fullPath, fileLocation);
        }
    }

    [CanBeNull]
    private Dictionary<string, string> ExportedFiles()
    {
        // path:entry_name
        var exportedFiles = new Dictionary<string, string>();

        var infoFileLocation = "";
        if (Song.Directory != null)
        {
            infoFileLocation = Path.Combine(Song.Directory, "Info.dat");
        }

        if (!File.Exists(infoFileLocation))
        {
            Debug.LogError(":hyperPepega: :mega: WHY TF ARE YOU TRYING TO PACKAGE A MAP WITH NO INFO.DAT FILE");
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "zip.warning", null, PersistentUI.DialogBoxPresetType.Ok);
            return null;
        }

        exportedFiles.Add(infoFileLocation, "Info.dat");
        AddToFileDictionary(exportedFiles, Song.CoverImageFilename);
        AddToFileDictionary(exportedFiles, Song.SongFilename);
        AddToFileDictionary(exportedFiles, "cinema-video.json");

        foreach (var contributor in Song.Contributors.DistinctBy(it => it.LocalImageLocation))
        {
            var imageLocation = Path.Combine(Song.Directory!, contributor.LocalImageLocation);
            if (contributor.LocalImageLocation != Song.CoverImageFilename &&
                File.Exists(imageLocation) && !File.GetAttributes(imageLocation).HasFlag(FileAttributes.Directory))
            {
                exportedFiles.Add(imageLocation, contributor.LocalImageLocation);
            }
        }
        
        foreach (var map in Song.DifficultyBeatmapSets.SelectMany(set => set.DifficultyBeatmaps))
        {
            AddToFileDictionary(exportedFiles, map.BeatmapFilename);
        }

        return exportedFiles;
    }

    // TODO: Move this to a proper location or make configurable with a default
    public const string QUEST_CUSTOM_SONGS_LOCATION = "sdcard/ModData/com.beatgames.beatsaber/Mods/SongLoader/CustomLevels";
    // I added this so the non-quest maintainers can use it as a reference for adding WIP uploads
    // this does indeed exist and work, please don't refrain from asking me. 
    public const string QUEST_CUSTOM_SONGS_WIP_LOCATION = "sdcard/ModData/com.beatgames.beatsaber/Mods/SongLoader/CustomWIPLevels"; 
    
    /// <summary>
    /// Exports the files to the Quest using adb
    /// </summary>
    public async void ExportToQuest()
    {
        var (devices, output) = await Adb.GetDevices();
        var questCandidates = new List<string>();

        if (devices == null || !string.IsNullOrEmpty(output.ErrorOut))
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "quest.no-devices", null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        foreach (var device in devices)
        {
            var (result, error) = await Adb.IsQuest(device);

            if (result && string.IsNullOrEmpty(error.ErrorOut))
            {
                questCandidates.Add(device);
            }    
        }

        if (questCandidates.Count == 0)
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "quest.no-quest", null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        var dialog = PersistentUI.Instance.CreateNewDialogBox();
        dialog.WithTitle("SongEditMenu", "quest.exporting");

        var progressBar = dialog.AddComponent<ProgressBarComponent>();
        progressBar.WithCustomLabelFormatter(f =>
            LocalizationSettings.StringDatabase
                .GetLocalizedString("SongEditMenu", "quest.exporting_progress",
                new object[] { f }));
        
        dialog.Open();
        
        // We should always be exporting to WIP Levels. CustomLevels are for downloaded BeatSaver songs.
        var songExportPath = Path.Combine(QUEST_CUSTOM_SONGS_WIP_LOCATION, Song.CleanSongName).Replace("\\", @"/");
        var exportedFiles = ExportedFiles();

        if (exportedFiles == null) return;


        Debug.Log($"Creating folder if needed at {songExportPath}");

        var totalFiles = questCandidates.Count * exportedFiles.Count;
        var fCount = 0;
        
        foreach (var questCandidate in questCandidates)
        {
            var createDir = await Adb.Mkdir(songExportPath, questCandidate);
            Debug.Log($"ADB Create dir: {createDir}");


            foreach (var fileNamePair in exportedFiles)
            {
                var locationRelativeToSongDir = fileNamePair.Value;
                
                var questPath = Path.Combine(songExportPath, locationRelativeToSongDir).Replace("\\", @"/");

                Debug.Log($"Pushing {questPath} from {fileNamePair.Key}");

                var log = await Adb.Push(fileNamePair.Key, questPath, questCandidate);
                Debug.Log(log.ToString());

                fCount++;
                progressBar.UpdateProgressBar((float) fCount / totalFiles);
            }
        }
        
        dialog.Clear();
        
        Debug.Log("EXPORTED TO QUEST SUCCESSFULLY YAYAAYAYA");
        
        dialog.WithTitle("Options", "quest.success");
        dialog.AddFooterButton(null, "PersistentUI", "ok");
    }

    /// <summary>
    ///     Create a zip for sharing the map
    /// </summary>
    public void PackageZip()
    {
        var infoFileLocation = "";
        var zipPath = "";
        if (Song.Directory != null)
        {
            zipPath = Path.Combine(Song.Directory, Song.CleanSongName + ".zip");
            // Mac doesn't seem to like overwriting existing zips, so delete the old one first
            File.Delete(zipPath);

            infoFileLocation = Path.Combine(Song.Directory, "Info.dat");
        }

        if (!File.Exists(infoFileLocation))
        {
            Debug.LogError(":hyperPepega: :mega: WHY TF ARE YOU TRYING TO PACKAGE A MAP WITH NO INFO.DAT FILE");
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "zip.warning", null,
                PersistentUI.DialogBoxPresetType.Ok);
            return;
        }
        
        var exportedFiles = ExportedFiles();

        if (exportedFiles == null) return;

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var pathFileEntryPair in exportedFiles)
            {
                archive.CreateEntryFromFile(pathFileEntryPair.Key, pathFileEntryPair.Value);
            }
        }

        OpenSelectedMapInFileBrowser();
    }

    /// <summary>
    ///     Open the folder containing the map's files in a native file browser
    /// </summary>
    public void OpenSelectedMapInFileBrowser()
    {
        if (Song.Directory == null)
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "explorer.warning", null,
                PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        var path = Song.Directory;
#if UNITY_STANDALONE_WIN
        path = path.Replace("/", "\\").Replace("\\\\", "\\");
#else
        path = path.Replace("\\", "/").Replace("//", "/");
#endif
        if (!path.StartsWith("\"")) path = "\"" + path;
        if (!path.EndsWith("\"")) path += "\"";

#if UNITY_STANDALONE_WIN
        Debug.Log($"Opening song directory ({path}) with Windows...");
        Process.Start("explorer.exe", path);
#elif UNITY_STANDALONE_OSX
        Debug.Log($"Opening song directory ({path}) with Mac...");
        Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
        Debug.Log($"Opening song directory ({path}) with Linux...");
        Process.Start("xdg-open", path);
#else
        Debug.Log("What is this, some UNIX bullshit?");
        PersistentUI.Instance.ShowDialogBox(
            "Unrecognized OS!\n\nIf you happen to know this OS and would like to contribute," +
            " please contact me on Discord: Caeden117#0117", null, PersistentUI.DialogBoxPresetType.Ok);
#endif
    }

    /// <summary>
    ///     Return the the song list scene, if the user has unsaved changes ask first
    /// </summary>
    public void ReturnToSongList()
    {
        // Do nothing if a dialog is open
        if (PersistentUI.Instance.DialogBoxIsEnabled) return;

        CheckForChanges(HandleReturnToSongList);
    }

    /// <summary>
    ///     Return the the song list scene
    /// </summary>
    /// <param name="r">Confirmation from the user</param>
    public void HandleReturnToSongList(int r)
    {
        if (r == 0) SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
    }

    /// <summary>
    ///     The user wants to edit the map
    ///     Check first that some objects are enabled and that there are no unsaved changes
    /// </summary>
    public void EditMapButtonPressed()
    {
        // If no difficulty is selected or there is a dialog open do nothing
        if (BeatSaberSongContainer.Instance.DifficultyData == null || PersistentUI.Instance.DialogBoxIsEnabled) return;

        var a = Settings.Instance.Load_Notes;
        var b = Settings.Instance.Load_Obstacles;
        var c = Settings.Instance.Load_Events;
        var d = Settings.Instance.Load_Others;

        if (!(a || b || c || d))
        {
            PersistentUI.Instance.ShowDialogBox(
                "SongEditMenu", "load.warning",
                null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        if (!(a && b && c && d))
        {
            PersistentUI.Instance.ShowDialogBox(
                "SongEditMenu", "load.warning2",
                null, PersistentUI.DialogBoxPresetType.Ok);
        }

        CheckForChanges(HandleEditMapButtonPressed);
    }

    /// <summary>
    ///     Load the editor scene
    /// </summary>
    /// <param name="r">Confirmation from the user</param>
    private void HandleEditMapButtonPressed(int r)
    {
        if (r == 0)
        {
            var map = difficultySelect.CurrentDiff;
            PersistentUI.UpdateBackground(Song);

            Debug.Log("Transitioning...");
            if (map != null)
            {
                Settings.Instance.LastLoadedMap = Song.Directory;
                Settings.Instance.LastLoadedChar = BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet
                    .BeatmapCharacteristicName;
                Settings.Instance.LastLoadedDiff = BeatSaberSongContainer.Instance.DifficultyData.Difficulty;
                BeatSaberSongContainer.Instance.Map = map;
                SceneTransitionManager.Instance.LoadScene("03_Mapper", LoadAudio(false, true));
            }
        }
    }

    /// <summary>
    ///     Helper methods to prompt the user if there are unsaved changes
    ///     Will call the callback immediately if there are none
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

        if (difficultySelect.IsDirty())
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "unsaveddiff.warning", callback,
                PersistentUI.DialogBoxPresetType.YesNo);
            return true;
        }

        if (contributorController.IsDirty())
        {
            PersistentUI.Instance.ShowDialogBox("SongEditMenu", "unsavedcontributor.warning", callback,
                PersistentUI.DialogBoxPresetType.YesNo);
            return true;
        }

        callback(0);
        return false;
    }

    /// <summary>
    ///     Edit contributors button has been pressed
    ///     Check there are no unsaved changes
    /// </summary>
    public void EditContributors()
    {
        // Do nothing if a dialog is open
        if (PersistentUI.Instance.DialogBoxIsEnabled) return;

        var wrapper = ContributorWrapper;
        wrapper.SetActive(!wrapper.activeSelf);
    }

    /// <summary>
    ///     Undo button has been pressed, trigger animation and reload the song data
    /// </summary>
    public void UndoChanges()
    {
        reloadSongDataCoroutine = StartCoroutine(SpinReloadSongDataButton());

        if (ContributorWrapper.activeSelf)
        {
            contributorController.UndoChanges();
            return;
        }

        LoadFromSong();
    }

    /// <summary>
    ///     Spins the undo button for extra flare
    /// </summary>
    /// <returns>Coroutine IEnumerator</returns>
    private IEnumerator SpinReloadSongDataButton()
    {
        if (reloadSongDataCoroutine != null) StopCoroutine(reloadSongDataCoroutine);

        var startTime = Time.time;
        var transform1 = revertInfoButtonImage.transform;
        var rotationQ = transform1.rotation;
        var rotation = rotationQ.eulerAngles;
        rotation.z = -330;
        transform1.rotation = Quaternion.Euler(rotation);

        while (true)
        {
            var rot = rotation.z;
            var timing = Time.time / startTime * 0.075f;
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
    ///     Helper method to get the float value from a UI element
    ///     Returns the placeholder value if the field is empty
    /// </summary>
    /// <param name="inputfield">Text field to get the value from</param>
    /// <returns>The value parsed to a float</returns>
    private static float GetTextValue(TMP_InputField inputfield)
    {
        if (!float.TryParse(inputfield.text, out var result))
        {
            if (!float.TryParse(inputfield.placeholder.GetComponent<TMP_Text>().text, out result))
                // How have you changed the placeholder so that it isn't valid?
                result = 0;
        }

        return result;
    }

    /// <summary>
    ///     Check if any changes have been made from the original song data
    /// </summary>
    /// <returns>True if user has made changes, false otherwise</returns>
    private bool IsDirty() =>
        Song.SongName != nameField.text ||
        Song.SongSubName != subNameField.text ||
        Song.SongAuthorName != songAuthorField.text ||
        Song.LevelAuthorName != authorField.text ||
        Song.CoverImageFilename != coverImageField.text ||
        Song.SongFilename != audioPath.text ||
        !NearlyEqual(Song.BeatsPerMinute, GetTextValue(bpmField)) ||
        !NearlyEqual(Song.PreviewStartTime, GetTextValue(prevStartField)) ||
        !NearlyEqual(Song.PreviewDuration, GetTextValue(prevDurField)) ||
        !NearlyEqual(Song.SongTimeOffset, GetTextValue(offset)) ||
        environmentDropdown.value != GetEnvironmentIDFromString(Song.EnvironmentName) ||
        customPlatformsDropdown.value != CustomPlatformFromSong();

    private static bool NearlyEqual(float a, float b, float epsilon = 0.01f) =>
        a.Equals(b) || Math.Abs(a - b) < epsilon;

    public class Environment
    {
        public readonly string HumanName;
        public readonly string JsonName;

        public Environment(string humanName, string jsonName)
        {
            HumanName = humanName;
            JsonName = jsonName;
        }
    }
}

using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AutoSaveController : MonoBehaviour, CMInput.ISavingActions
{
    private const int MAXIMUM_AUTOSAVE_COUNT = 15;

    private float t;
    [SerializeField] private Toggle autoSaveToggle;

    private Thread savingThread = null;

    private List<DirectoryInfo> currentAutoSaves = new List<DirectoryInfo>();

    public void ToggleAutoSave(bool enabled)
    {
        Settings.Instance.AutoSave = enabled;
    }

	// Use this for initialization
	void Start ()
    {
        autoSaveToggle.isOn = Settings.Instance.AutoSave;
        t = 0;

        var autoSavesDir = Path.Combine(BeatSaberSongContainer.Instance.song.directory, "autosaves");
        if (Directory.Exists(autoSavesDir))
        {
            foreach (var dir in Directory.EnumerateDirectories(autoSavesDir))
            {
                currentAutoSaves.Add(new DirectoryInfo(dir));
            }
        }
        CleanAutosaves();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (!Settings.Instance.AutoSave || !Application.isFocused) return;
        t += Time.deltaTime;
        if (t > (Settings.Instance.AutoSaveInterval * 60))
        {
            t = 0;
            Save(true);
        }
	}

    public void Save(bool auto = false)
    {
        if (savingThread != null && savingThread.IsAlive)
        {
            Debug.LogError(":hyperPepega: :mega: STOP TRYING TO SAVE THE SONG WHILE ITS ALREADY SAVING TO DISK");
            return;
        }

        var notification = PersistentUI.Instance.DisplayMessage("Mapper", $"{(auto ? "auto" : "")}save.message", PersistentUI.DisplayMessageType.BOTTOM);
        notification.skipFade = true;
        notification.waitTime = 5.0f;
        SelectionController.RefreshMap(); //Make sure our map is up to date.
        savingThread = new Thread(() => //I could very well move this to its own function but I need access to the "auto" variable.
        {
            Thread.CurrentThread.IsBackground = true; //Making sure this does not interfere with game thread
            //Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
            //This should be thread-wide, but I have this set throughout just in case it isnt.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            //Saving Map Data
            string originalMap = BeatSaberSongContainer.Instance.map.directoryAndFile;
            string originalSong = BeatSaberSongContainer.Instance.song.directory;
            if (auto)
            {
                string autoSaveDir = Path.Combine(originalSong, "autosaves", $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}");

                Debug.Log($"Auto saved to: {autoSaveDir}");
                //We need to create the autosave directory before we can save the .dat difficulty into it.
                Directory.CreateDirectory(autoSaveDir);
                BeatSaberSongContainer.Instance.map.directoryAndFile = Path.Combine(autoSaveDir, BeatSaberSongContainer.Instance.difficultyData.beatmapFilename);
                BeatSaberSongContainer.Instance.song.directory = autoSaveDir;

                var newDirectoryInfo = new DirectoryInfo(autoSaveDir);
                currentAutoSaves.Add(newDirectoryInfo);
                CleanAutosaves();
            }
            BeatSaberSongContainer.Instance.map.Save();
            BeatSaberSongContainer.Instance.map.directoryAndFile = originalMap;

            BeatSaberSong.DifficultyBeatmapSet set = BeatSaberSongContainer.Instance.difficultyData.parentBeatmapSet; //Grab our set
            BeatSaberSongContainer.Instance.song.difficultyBeatmapSets.Remove(set); //Yeet it out
            BeatSaberSong.DifficultyBeatmap data = BeatSaberSongContainer.Instance.difficultyData; //Grab our diff data
            set.difficultyBeatmaps.Remove(data); //Yeet out our difficulty data
            if (BeatSaberSongContainer.Instance.difficultyData.customData == null) //if for some reason this be null, make new customdata
                BeatSaberSongContainer.Instance.difficultyData.customData = new JSONObject();
            set.difficultyBeatmaps.Add(BeatSaberSongContainer.Instance.difficultyData); //Add back our difficulty data
            BeatSaberSongContainer.Instance.song.difficultyBeatmapSets.Add(set); //Add back our difficulty set
            BeatSaberSongContainer.Instance.song.SaveSong(); //Save
            BeatSaberSongContainer.Instance.song.directory = originalSong; //Revert directory if it was changed by autosave
            notification.skipDisplay = true;
        });

        savingThread.Start();
    }

    public void OnSave(InputAction.CallbackContext context)
    {
        if (context.performed) Save();
    }

    private void CleanAutosaves()
    {
        if (currentAutoSaves.Count <= MAXIMUM_AUTOSAVE_COUNT) return;

        Debug.Log($"Too many autosaves; removing excess... ({currentAutoSaves.Count} > {MAXIMUM_AUTOSAVE_COUNT})");

        var ordered = currentAutoSaves.OrderByDescending(d => d.LastWriteTime).ToArray();
        currentAutoSaves = ordered.Take(MAXIMUM_AUTOSAVE_COUNT).ToList();

        foreach (var directoryInfo in ordered.Skip(MAXIMUM_AUTOSAVE_COUNT))
        {
            Directory.Delete(directoryInfo.FullName, true);
        }
    }
}

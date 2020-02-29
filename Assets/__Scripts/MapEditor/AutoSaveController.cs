using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;
using System;
using UnityEngine.UI;
using SimpleJSON;
using System.Globalization;

public class AutoSaveController : MonoBehaviour {
    private float t;
    [SerializeField] private Toggle autoSaveToggle;

    public void ToggleAutoSave(bool enabled)
    {
        Settings.Instance.AutoSave = enabled;
    }

	// Use this for initialization
	void Start () {
        autoSaveToggle.isOn = Settings.Instance.AutoSave;
        t = 0;
	}
	
	// Update is called once per frame
	void Update () {
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
        PersistentUI.Instance.DisplayMessage($"{(auto ? "Auto " : "")}Saving...", PersistentUI.DisplayMessageType.BOTTOM);
        SelectionController.RefreshMap(); //Make sure our map is up to date.
        if (BeatSaberSongContainer.Instance.map._customEvents.Any())
        {
            if (Settings.Instance.Reminder_SavingCustomEvents)
            { //Example of advanced dialog box options.
                PersistentUI.Instance.ShowDialogBox("ChroMapper has detected you are using custom events in your map.\n\n" +
                  "The current format for Custom Events goes against BeatSaver's enforced schema.\n" +
                  "If you try to upload this map to BeatSaver, it will fail.", HandleCustomEventsDecision, "Ok",
                  "Don't Remind Me");
            }
        }
        new Thread(() => //I could very well move this to its own function but I need access to the "auto" variable.
        {
            Thread.CurrentThread.IsBackground = true; //Making sure this does not interfere with game thread
            //Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
            //This should be thread-wide, but I have this set throughout just in case it isnt.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            //Saving Map Data
            string originalMap = BeatSaberSongContainer.Instance.map.directoryAndFile;
            string originalSong = BeatSaberSongContainer.Instance.song.directory;
            if (auto) {
                Queue<string> directory = new Queue<string>(originalSong.Split('/').ToList());
                directory.Enqueue("autosaves");
                directory.Enqueue($"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}"); //timestamp
                string autoSaveDir = string.Join("/", directory.ToArray());
                Debug.Log($"Auto saved to: {autoSaveDir}");
                //We need to create the autosave directory before we can save the .dat difficulty into it.
                System.IO.Directory.CreateDirectory(autoSaveDir);
                BeatSaberSongContainer.Instance.map.directoryAndFile = $"{autoSaveDir}\\{BeatSaberSongContainer.Instance.difficultyData.beatmapFilename}";
                BeatSaberSongContainer.Instance.song.directory = autoSaveDir;
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
        }).Start();
    }

    private void HandleCustomEventsDecision(int res)
    {
        Settings.Instance.Reminder_SavingCustomEvents = res == 0;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AutoSaveController : MonoBehaviour, CMInput.ISavingActions
{
    private const int maximumAutosaveCount = 15;
    [SerializeField] private Toggle autoSaveToggle;
    [SerializeField] private PauseManager pauseManager;

    private List<DirectoryInfo> currentAutoSaves = new List<DirectoryInfo>();

    private Thread savingThread;

    private float t;

    private float maxBeatTime; // Does not account for official bpm changes. V3 sounds like fun
    private const int FALSE = 0; // Because Interlocked.Exchange(bool) doesn't exist
    private const int TRUE = 1;
    private Thread objectCheckingThread;
    private int objectsOutsideMap = FALSE;
    private int objectsCheckIsComplete = FALSE;
    private int saveFlag = (int)SaveType.None;

    public enum SaveType
    {
        None,
        Menu,
        Quit
    }

    // Use this for initialization
    private void Start()
    {
        autoSaveToggle.isOn = Settings.Instance.AutoSave;
        t = 0;

        var autoSavesDir = Path.Combine(BeatSaberSongContainer.Instance.Song.Directory, "autosaves");
        if (Directory.Exists(autoSavesDir))
        {
            foreach (var dir in Directory.EnumerateDirectories(autoSavesDir))
                currentAutoSaves.Add(new DirectoryInfo(dir));
        }

        maxBeatTime = BeatSaberSongContainer.Instance.LoadedSong.length * BeatSaberSongContainer.Instance.Song.BeatsPerMinute / 60;

        CleanAutosaves();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Interlocked.Exchange(ref objectsCheckIsComplete, FALSE) == TRUE)
        {
            if (Interlocked.Exchange(ref objectsOutsideMap, FALSE) == TRUE)
            {
                if (saveFlag == (int)SaveType.None) PersistentUI.Instance.ShowDialogBox("Mapper", "save.objects.outside", CleanAndSave, PersistentUI.DialogBoxPresetType.YesNo);
                if (saveFlag == (int)SaveType.Menu) PersistentUI.Instance.ShowDialogBox("Mapper", "save.objects.outside", CleanAndMenu, PersistentUI.DialogBoxPresetType.YesNo);
                if (saveFlag == (int)SaveType.Quit) PersistentUI.Instance.ShowDialogBox("Mapper", "save.objects.outside", CleanAndQuit, PersistentUI.DialogBoxPresetType.YesNo);
            }
            else
            {
                if (saveFlag == (int)SaveType.None) Save();
                if (saveFlag == (int)SaveType.Menu) pauseManager.SaveAndExitToMenu();
                if (saveFlag == (int)SaveType.Quit) pauseManager.SaveAndQuitCM();
            }
        }

        if (!Settings.Instance.AutoSave || !Application.isFocused) return;
        t += Time.deltaTime;
        if (t > Settings.Instance.AutoSaveInterval * 60)
        {
            t = 0;
            Save(true);
        }
    }

    public void OnSave(InputAction.CallbackContext context)
    {
        if (context.performed) CheckAndSave();
    }

    public void CheckAndSave(int _) => CheckAndSave(); // So it shows up in Unity
    public void CheckAndSave(SaveType saveType = SaveType.None)
    {
        if (objectCheckingThread != null && objectCheckingThread.IsAlive)
        {
            Debug.LogError(":hyperPepega: :mega: PLEASE BE PATIENT THANKS");
            return;
        }

        SelectionController.RefreshMap();
        objectCheckingThread = new Thread(() =>
        {
            if (ObjectIsOutsideMap())
            {
                Debug.Log("Found object outside of the map.");
                Interlocked.Exchange(ref objectsOutsideMap, TRUE);
            }
            Interlocked.Exchange(ref saveFlag, (int)saveType);
            Interlocked.Exchange(ref objectsCheckIsComplete, TRUE);
        });
        objectCheckingThread.Start();
    }

    private void CleanAndSave(int res)
    {
        if (res == 0) CleanObjectsOutsideMap();
        Save();
    }

    private void CleanAndMenu(int res)
    {
        if (res == 0) CleanObjectsOutsideMap();
        pauseManager.SaveAndExitToMenu();
    }

    private void CleanAndQuit(int res)
    {
        if (res == 0) CleanObjectsOutsideMap();
        pauseManager.SaveAndQuitCM();
    }

    private void CleanObjectsOutsideMap()
    {
        if (Settings.Instance.RemoveNotesOutsideMap)
        {
            var noteCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note);
            foreach (var note in BeatSaberSongContainer.Instance.Map.Notes.Where(note => note.Time >= maxBeatTime))
            {
                noteCollection.DeleteObject(note);
            }
        }
        if (Settings.Instance.RemoveEventsOutsideMap)
        {
            var eventCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            foreach (var evt in BeatSaberSongContainer.Instance.Map.Events.Where(evt => evt.Time >= maxBeatTime))
            {
                eventCollection.DeleteObject(evt);
            }
        }
        if (Settings.Instance.RemoveObstaclesOutsideMap)
        {
            var obstacleCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle);
            foreach (var obst in BeatSaberSongContainer.Instance.Map.Obstacles.Where(obst => obst.Time >= maxBeatTime))
            {
                obstacleCollection.DeleteObject(obst);
            }
        }
    }

    private bool ObjectIsOutsideMap()
    {
        if (Settings.Instance.RemoveNotesOutsideMap)
        {
            if (BeatSaberSongContainer.Instance.Map.Notes.Any(note => note.Time >= maxBeatTime))
                return true;
        }
        if (Settings.Instance.RemoveEventsOutsideMap)
        {
            if (BeatSaberSongContainer.Instance.Map.Events.Any(evt => evt.Time >= maxBeatTime))
                return true;
        }
        if (Settings.Instance.RemoveObstaclesOutsideMap)
        {
            if (BeatSaberSongContainer.Instance.Map.Obstacles.Any(obst => obst.Time >= maxBeatTime))
                return true;
        }
        return false;
    }

    public void ToggleAutoSave(bool enabled) => Settings.Instance.AutoSave = enabled;

    public void Save(bool auto = false)
    {
        if (savingThread != null && savingThread.IsAlive)
        {
            Debug.LogError(":hyperPepega: :mega: STOP TRYING TO SAVE THE SONG WHILE ITS ALREADY SAVING TO DISK");
            return;
        }

        var notification = PersistentUI.Instance.DisplayMessage("Mapper", $"{(auto ? "auto" : "")}save.message",
            PersistentUI.DisplayMessageType.Bottom);
        notification.SkipFade = true;
        notification.WaitTime = 5.0f;
        SelectionController.RefreshMap(); //Make sure our map is up to date.
        savingThread = new Thread(
            () => //I could very well move this to its own function but I need access to the "auto" variable.
            {
                Thread.CurrentThread.IsBackground = true; //Making sure this does not interfere with game thread
                //Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
                //This should be thread-wide, but I have this set throughout just in case it isnt.
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                //Saving Map Data
                var originalMap = BeatSaberSongContainer.Instance.Map.DirectoryAndFile;
                var originalSong = BeatSaberSongContainer.Instance.Song.Directory;

                try
                {
                    if (auto)
                    {
                        var autoSaveDir = Path.Combine(originalSong, "autosaves", $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}");

                        Debug.Log($"Auto saved to: {autoSaveDir}");
                        //We need to create the autosave directory before we can save the .dat difficulty into it.
                        Directory.CreateDirectory(autoSaveDir);
                        BeatSaberSongContainer.Instance.Map.DirectoryAndFile = Path.Combine(autoSaveDir,
                            BeatSaberSongContainer.Instance.DifficultyData.BeatmapFilename);
                        BeatSaberSongContainer.Instance.Song.Directory = autoSaveDir;

                        var newDirectoryInfo = new DirectoryInfo(autoSaveDir);
                        currentAutoSaves.Add(newDirectoryInfo);
                        CleanAutosaves();
                    }
                    else // Only check on manual save
                    {
                        if (Settings.Instance.RemoveNotesOutsideMap) BeatSaberSongContainer.Instance.Map.Notes.RemoveAll(note => note.Time >= maxBeatTime);
                        if (Settings.Instance.RemoveEventsOutsideMap) BeatSaberSongContainer.Instance.Map.Events.RemoveAll(evt => evt.Time >= maxBeatTime);
                        if (Settings.Instance.RemoveObstaclesOutsideMap) BeatSaberSongContainer.Instance.Map.Obstacles.RemoveAll(obst => obst.Time >= maxBeatTime);
                    }

                    BeatSaberSongContainer.Instance.Map.Save();

                    var set = BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet; //Grab our set
                    BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Remove(set); //Yeet it out
                    var data = BeatSaberSongContainer.Instance.DifficultyData; //Grab our diff data
                    set.DifficultyBeatmaps.Remove(data); //Yeet out our difficulty data
                    if (BeatSaberSongContainer.Instance.DifficultyData.CustomData ==
                        null) //if for some reason this be null, make new customdata
                    {
                        BeatSaberSongContainer.Instance.DifficultyData.CustomData = new JSONObject();
                    }

                    set.DifficultyBeatmaps.Add(BeatSaberSongContainer.Instance
                        .DifficultyData); //Add back our difficulty data
                    BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Add(set); //Add back our difficulty set
                    BeatSaberSongContainer.Instance.Song.SaveSong(); //Save

                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to autosave (don't worry, progress wasn't lost)");
                    Debug.LogException(ex);
                }

                // Revert directory if it was changed by autosave
                BeatSaberSongContainer.Instance.Song.Directory = originalSong;
                BeatSaberSongContainer.Instance.Map.DirectoryAndFile = originalMap;

                notification.SkipDisplay = true;
            });

        savingThread.Start();
    }

    private void CleanAutosaves()
    {
        if (currentAutoSaves.Count <= maximumAutosaveCount) return;

        Debug.Log($"Too many autosaves; removing excess... ({currentAutoSaves.Count} > {maximumAutosaveCount})");

        var ordered = currentAutoSaves.OrderByDescending(d => d.LastWriteTime).ToArray();
        currentAutoSaves = ordered.Take(maximumAutosaveCount).ToList();

        foreach (var directoryInfo in ordered.Skip(maximumAutosaveCount))
        {
            try
            {
                Directory.Delete(directoryInfo.FullName, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete an old autosave ({directoryInfo.Name}): {ex.Message}.");
            }
        }
    }
}

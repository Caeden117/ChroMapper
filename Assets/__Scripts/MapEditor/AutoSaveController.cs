using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beatmap.Base;
using Beatmap.Enums;
using QuestDumper;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AutoSaveController : MonoBehaviour, CMInput.ISavingActions
{
    public bool IsSaving => savingThread != null && !savingThread.IsCompleted;

    private const int maximumAutosaveCount = 15;
    [SerializeField] private Toggle autoSaveToggle;
    [SerializeField] private PauseManager pauseManager;

    private List<DirectoryInfo> currentAutoSaves = new List<DirectoryInfo>();

    private Task savingThread;

    private float t;

    private float maxSongBpmTime;
    private const int FALSE = 0; // Because Interlocked.Exchange(bool) doesn't exist
    private const int TRUE = 1;
    private Task objectCheckingThread;
    private int objectsOutsideMap = FALSE;
    private int objectsCheckIsComplete = FALSE;
    private int saveFlag = (int)SaveType.None;

    private string saveWarningMessage; 

    private static MapExporter Exporter => new(BeatSaberSongContainer.Instance.Info);

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

        var autoSavesDir = Path.Combine(BeatSaberSongContainer.Instance.Info.Directory, "autosaves");
        if (Directory.Exists(autoSavesDir))
        {
            foreach (var dir in Directory.EnumerateDirectories(autoSavesDir))
                currentAutoSaves.Add(new DirectoryInfo(dir));
        }

        maxSongBpmTime = BeatSaberSongContainer.Instance.LoadedSong.length * BeatSaberSongContainer.Instance.Info.BeatsPerMinute / 60;

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

        if (!string.IsNullOrEmpty(saveWarningMessage))
        {
            PersistentUI.Instance.ShowDialogBox(saveWarningMessage, null, PersistentUI.DialogBoxPresetType.Ok);
            saveWarningMessage = null;
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

    public void OnSaveQuest(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!Adb.IsAdbInstalled(out _)) return;
        CheckAndSaveQuest();
    }

    public void CheckAndSaveQuest(int _) => CheckAndSaveQuest(); // So it shows up in Unity

    public async void CheckAndSaveQuest(SaveType saveType = SaveType.None)
    {
        CheckAndSave(saveType);
        await objectCheckingThread;
        await savingThread; // wait for files to save

        // now wait for exporter to finish
        await Exporter.ExportToQuest();
    }

    public void CheckAndSave(int _) => CheckAndSave(); // So it shows up in Unity
    public void CheckAndSave(SaveType saveType = SaveType.None)
    {
        if (objectCheckingThread != null && !objectCheckingThread.IsCompleted)
        {
            Debug.LogError(":hyperPepega: :mega: PLEASE BE PATIENT THANKS");
            return;
        }

        objectCheckingThread = Task.Run(() =>
        {
            if (ObjectIsOutsideMap())
            {
                Debug.Log("Found object outside of the map.");
                Interlocked.Exchange(ref objectsOutsideMap, TRUE);
            }

            Interlocked.Exchange(ref saveFlag, (int)saveType);
            Interlocked.Exchange(ref objectsCheckIsComplete, TRUE);
        });
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

    // The ToList is necessary because we can't delete from the same collection we're enumerating
    private void CleanObjectsOutsideMap()
    {
        var removedObjects = new List<BaseObject>();
        if (Settings.Instance.RemoveNotesOutsideMap)
        {
            var noteCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            var notesToRemove = BeatSaberSongContainer.Instance.Map.Notes
                .Where(note => note.SongBpmTime >= maxSongBpmTime).ToList();
            foreach (var note in notesToRemove)
            {
                noteCollection.DeleteObject(note, false, false, inCollectionOfDeletes: true);
            }
            noteCollection.DoPostObjectsDeleteWorkflow();
            removedObjects.AddRange(notesToRemove);
        }

        if (Settings.Instance.RemoveEventsOutsideMap)
        {
            var eventCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            var eventsToRemove = BeatSaberSongContainer.Instance.Map.Events
                .Where(evt => evt.SongBpmTime >= maxSongBpmTime).ToList();
            foreach (var evt in eventsToRemove)
            {
                eventCollection.DeleteObject(evt, false, false, inCollectionOfDeletes: true);
            }
            eventCollection.DoPostObjectsDeleteWorkflow();
            removedObjects.AddRange(eventsToRemove);
            
            var bpmCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.BpmChange);
            var bpmEventsToRemove = BeatSaberSongContainer.Instance.Map.BpmEvents
                .Where(bpmEvt => bpmEvt.SongBpmTime >= maxSongBpmTime).ToList();
            foreach (var bpmEvt in bpmEventsToRemove)
            {
                bpmCollection.DeleteObject(bpmEvt, false, false, inCollectionOfDeletes: true);
            }
            bpmCollection.DoPostObjectsDeleteWorkflow();
            removedObjects.AddRange(bpmEventsToRemove);
        }

        if (Settings.Instance.RemoveObstaclesOutsideMap)
        {
            var obstacleCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            var obstaclesToRemove = BeatSaberSongContainer.Instance.Map.Obstacles
                .Where(obst => obst.SongBpmTime >= maxSongBpmTime).ToList();
            foreach (var obst in obstaclesToRemove)
            {
                obstacleCollection.DeleteObject(obst, false, false, inCollectionOfDeletes: true);
            }
            obstacleCollection.DoPostObjectsDeleteWorkflow();
            removedObjects.AddRange(obstaclesToRemove);
        }

        if (Settings.Instance.RemoveArcsOutsideMap)
        {
            var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            var arcsToRemove = BeatSaberSongContainer.Instance.Map.Arcs.Where(arc =>
                arc.SongBpmTime >= maxSongBpmTime || arc.TailSongBpmTime >= maxSongBpmTime).ToList();
            foreach (var arc in arcsToRemove)
            {
                arcCollection.DeleteObject(arc, false, false, inCollectionOfDeletes: true);
            }
            arcCollection.DoPostObjectsDeleteWorkflow();
            removedObjects.AddRange(arcsToRemove);
        }

        if (Settings.Instance.RemoveChainsOutsideMap)
        {
            var chainCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            var chainsToRemove = BeatSaberSongContainer.Instance.Map.Chains.Where(chain =>
                chain.SongBpmTime >= maxSongBpmTime || chain.TailSongBpmTime >= maxSongBpmTime).ToList();
            foreach (var chain in chainsToRemove)
            {
                chainCollection.DeleteObject(chain, false, false, inCollectionOfDeletes: true);
            }
            chainCollection.DoPostObjectsDeleteWorkflow();
            removedObjects.AddRange(chainsToRemove);
        }
        
        if (removedObjects.Count > 0) BeatmapActionContainer.AddAction(new SelectionDeletedAction(removedObjects));
    }

    private bool ObjectIsOutsideMap()
    {
        if (Settings.Instance.RemoveNotesOutsideMap)
        {
            if (BeatSaberSongContainer.Instance.Map.Notes.Any(note => note.SongBpmTime >= maxSongBpmTime))
                return true;
        }
        if (Settings.Instance.RemoveEventsOutsideMap)
        {
            if (BeatSaberSongContainer.Instance.Map.Events.Any(evt => evt.SongBpmTime >= maxSongBpmTime))
                return true;

            if (BeatSaberSongContainer.Instance.Map.BpmEvents.Any(bpmEvt => bpmEvt.SongBpmTime >= maxSongBpmTime))
                return true;
        }
        if (Settings.Instance.RemoveObstaclesOutsideMap)
        {
            if (BeatSaberSongContainer.Instance.Map.Obstacles.Any(obst => obst.SongBpmTime >= maxSongBpmTime))
                return true;
        }
        if (Settings.Instance.RemoveArcsOutsideMap)
        {
            if (BeatSaberSongContainer.Instance.Map.Arcs.Any(arc =>
                    arc.SongBpmTime >= maxSongBpmTime || arc.TailSongBpmTime >= maxSongBpmTime))
                return true;
        }
        if (Settings.Instance.RemoveChainsOutsideMap)
        {
            if (BeatSaberSongContainer.Instance.Map.Chains.Any(chain =>
                    chain.SongBpmTime >= maxSongBpmTime || chain.TailSongBpmTime >= maxSongBpmTime))
                return true;
        }
        
        return false;
    }

    public void ToggleAutoSave(bool enabled) => Settings.Instance.AutoSave = enabled;

    private void DisplayWarningIfIncompatibleDataIsPresent()
    {
        switch (Settings.Instance.MapVersion)
        {
            case 2:
                var stringBuilder = new StringBuilder();
                var map = BeatSaberSongContainer.Instance.Map; 

                if (map.Notes.Any(note => note.AngleOffset != 0))
                {
                    stringBuilder.AppendLine("* Note Angle Offset");
                }

                if (map.Obstacles.Any(obs => obs.PosY != (int)GridY.Base && obs.PosY != (int)GridY.Top))
                {
                    stringBuilder.AppendLine("* Obstacle Y position");
                }
                
                if (map.Obstacles.Any(obs =>
                        (obs.PosY == (int)GridY.Base && obs.Height < (int)ObstacleHeight.Full) ||
                        (obs.PosY == (int)GridY.Top && obs.Height < (int)ObstacleHeight.Crouch)))
                {
                    stringBuilder.AppendLine("* Obstacle Height");
                }

                if (map.Chains.Any())
                {
                    stringBuilder.AppendLine("* Chains");
                }
                
                if (map.Arcs.Any())
                {
                    stringBuilder.AppendLine("* Arcs");
                }

                if (map.LightColorEventBoxGroups.Any() || map.LightRotationEventBoxGroups.Any() ||
                    map.LightTranslationEventBoxGroups.Any())
                {
                    stringBuilder.AppendLine("* Group Lighting");
                }

                if (stringBuilder.Length != 0)
                {
                    // TODO: Localise this string somehow? Can only access localisation string on main thread :(
                    stringBuilder.Insert(0, "Warning\nThe following properties are not saved in v2 format:\n");
                    stringBuilder.AppendLine("Save in v3 or v4 format to ensure data is not lost.");
                }

                saveWarningMessage = stringBuilder.ToString();

                break;
            
            case 3:
                // No vanilla v2 features are unsupported in v3
                break;
        }
    }

    public void Save(bool auto = false)
    {
        if (IsSaving)
        {
            Debug.LogError(":hyperPepega: :mega: STOP TRYING TO SAVE THE SONG WHILE ITS ALREADY SAVING TO DISK");
            return;
        }

        var notification = PersistentUI.Instance.DisplayMessage("Mapper", $"{(auto ? "auto" : "")}save.message",
            PersistentUI.DisplayMessageType.Bottom);
        notification.SkipFade = true;
        notification.WaitTime = 5.0f;

        savingThread = Task.Run(
            () => //I could very well move this to its own function but I need access to the "auto" variable.
            {
                Thread.CurrentThread.IsBackground = true; //Making sure this does not interfere with game thread
                //Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
                //This should be thread-wide, but I have this set throughout just in case it isnt.
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                //Saving Map Data
                var originalMap = BeatSaberSongContainer.Instance.Map.DirectoryAndFile;
                var originalInfo = BeatSaberSongContainer.Instance.Info.Directory;

                try
                {
                    if (auto)
                    {
                        var autoSaveDir = Path.Combine(originalInfo, "autosaves", $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}");

                        Debug.Log($"Auto saved to: {autoSaveDir}");
                        //We need to create the autosave directory before we can save the .dat difficulty into it.
                        Directory.CreateDirectory(autoSaveDir);
                        BeatSaberSongContainer.Instance.Map.DirectoryAndFile = Path.Combine(autoSaveDir,
                            BeatSaberSongContainer.Instance.MapDifficultyInfo.BeatmapFileName);
                        BeatSaberSongContainer.Instance.Info.Directory = autoSaveDir;

                        var newDirectoryInfo = new DirectoryInfo(autoSaveDir);
                        currentAutoSaves.Add(newDirectoryInfo);
                        CleanAutosaves();
                    }

                    if (!auto)
                    {
                        DisplayWarningIfIncompatibleDataIsPresent();
                    }

                    BeatSaberSongContainer.Instance.Map.Save();

                    var set = BeatSaberSongContainer.Instance.MapDifficultyInfo.ParentSet; //Grab our set
                    BeatSaberSongContainer.Instance.Info.DifficultySets.Remove(set); //Yeet it out
                    var data = BeatSaberSongContainer.Instance.MapDifficultyInfo; //Grab our diff data
                    set.Difficulties.Remove(data); //Yeet out our difficulty data
                    if (BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData ==
                        null) //if for some reason this be null, make new customdata
                    {
                        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData = new JSONObject();
                    }

                    set.Difficulties.Add(BeatSaberSongContainer.Instance
                        .MapDifficultyInfo); //Add back our difficulty data
                    BeatSaberSongContainer.Instance.Info.DifficultySets.Add(set); //Add back our difficulty set
                    BeatSaberSongContainer.Instance.Info.Save(); //Save
                    // TODO: Make sure this works for v4

                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to autosave (don't worry, progress wasn't lost)");
                    Debug.LogException(ex);
                }

                // Revert directory if it was changed by autosave
                BeatSaberSongContainer.Instance.Info.Directory = originalInfo;
                BeatSaberSongContainer.Instance.Map.DirectoryAndFile = originalMap;

                notification.SkipDisplay = true;
            });
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

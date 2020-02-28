using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsSettings : MonoBehaviour
{
    #region General
    [Header("Toggles"), Header("-------- General --------")]
    [SerializeField] private BetterToggle discordToggle;
    [SerializeField] private BetterToggle darkThemeToggle;
    
    [SerializeField] private BetterToggle loadEventsToggle;
    [SerializeField] private BetterToggle loadNotesToggle;
    [SerializeField] private BetterToggle loadObstaclesToggle;
    [SerializeField] private BetterToggle loadOtherToggle;
    
    [Header("Sliders")]
    [SerializeField] private BetterSlider initialBatchSizeSlider;
    
    [Header("Misc")]
    [SerializeField] private TextMeshProUGUI installFieldErrorText;
    
    [SerializeField] private BetterInputField customLevelField;
    [SerializeField] private BetterInputField autoSaveInterval;

    #endregion
    
    #region Mapping
    [Header("Toggles"), Header("-------- Mapping --------")]
    [SerializeField] private BetterToggle oscEnabled;
    [SerializeField] private BetterToggle countersPlus;
    [SerializeField] private BetterToggle chromaOnly;
    [SerializeField] private BetterToggle boxSelect;
    [SerializeField] private BetterToggle perfectWalls; // Dont place 0 walls
    [SerializeField, Tooltip("360/90 Mode")] private BetterToggle rotateTrack; // 360/90 mode
    [SerializeField] private BetterToggle highlightRecentlyPlaced;

    [Header("Sliders")]
    [SerializeField] private BetterSlider songSpeedSlider;

    [Header("Misc")]
    [SerializeField] private BetterInputField noteLanes;
    [SerializeField] private TMP_InputField oscIP;
    [SerializeField] private TMP_InputField oscPort;
    
    #endregion

    #region Audio
    [Header("Toggles"), Header("-------- Audio --------")]
    [SerializeField] private BetterToggle redNoteDing;
    [SerializeField] private BetterToggle blueNoteDing;
    [SerializeField] private BetterToggle bombDing;
    
    [Header("Sliders")]
    [SerializeField] private VolumeSlider volumeSlider;
    [SerializeField] private VolumeSlider metronomeSlider;
    [SerializeField] private VolumeSlider noteHitVolumeSlider;

    [Header("Misc")]
    [SerializeField] private TMP_Dropdown noteHitSoundDropdown;
    
    #endregion
    
    #region Graphics
    [Header("Toggles"), Header("-------- Graphics --------")]
    [SerializeField] private BetterToggle chromaLite;
    [SerializeField] private BetterToggle chroma;
    [SerializeField] private BetterToggle waveformGenerator;
    
    [Header("Sliders")]
    [SerializeField] private BetterSlider pastNotesGridScaleSlider;
    [SerializeField] private BetterSlider cameraFOVSlider;
    [SerializeField] private BetterSlider editorScaleSlider;
    [SerializeField] private BetterSlider chunkDistanceSlider;
    [SerializeField] private BetterSlider spawnOffset;
    [SerializeField] private BetterSlider despawnOffset;
    
    //[Header("Misc")]
    
    #endregion
    
    #region Controls
    [Header("Toggles"), Header("-------- Controls --------")]
    [SerializeField] private BetterToggle invertPrecisionScroll;
    [SerializeField] private BetterToggle waveformWorkflow;
    [SerializeField] private BetterToggle nodeEditor;
    [SerializeField] private BetterToggle nodeEditorKeybind;
    [SerializeField] private BetterToggle invertControls;
    
    [Header("Sliders")]
    [SerializeField] private BetterSlider mouseSensSlider;
    [SerializeField] private BetterSlider cameraSpeedSlider;
    
    //[Header("Misc")]
    
    #endregion
    
    private MetronomeHandler _metronomeHandler;
    private bool _inEditor;

    private void Awake()
    {
        _inEditor = SceneManager.GetActiveScene().name == "03_Mapper";
        /*
        oscIP.text = Settings.Instance.OSC_IP;
        oscPort.text = Settings.Instance.OSC_Port;
        oscEnabled.Set(Settings.Instance.OSC_Enabled);
       */
        
        #region General

        discordToggle.isOn = Settings.Instance.DiscordRPCEnabled;
        darkThemeToggle.isOn = Settings.Instance.DarkTheme;
        loadEventsToggle.isOn = Settings.Instance.Load_Events;
        loadNotesToggle.isOn = Settings.Instance.Load_Notes;
        loadObstaclesToggle.isOn = Settings.Instance.Load_Obstacles;
        loadOtherToggle.isOn = Settings.Instance.Load_Others;
        
        customLevelField.text = Settings.Instance.BeatSaberInstallation;
        initialBatchSizeSlider.value = Settings.Instance.InitialLoadBatchSize / 50;
        #endregion
    
        #region Mapping
        countersPlus.isOn = Settings.Instance.CountersPlus;
        chromaOnly.isOn = Settings.Instance.PlaceOnlyChromaEvents;
        boxSelect.isOn = Settings.Instance.BoxSelect;
        perfectWalls.isOn = Settings.Instance.DontPlacePerfectZeroDurationWalls;
        rotateTrack.isOn = Settings.Instance.RotateTrack;
        
        songSpeedSlider.value = OptionsController.Find<SongSpeedController>()?.source.pitch * 10f ?? 10f;
        editorScaleSlider.value = Settings.Instance.EditorScale;
        
        autoSaveInterval.text =Settings.Instance.AutoSaveInterval.ToString();
        noteLanes.text = OptionsController.Find<NoteLanesController>()?.NoteLanes.ToString() ?? "4";
        #endregion
        
        #region Audio
        _metronomeHandler = OptionsController.Find<MetronomeHandler>();
        volumeSlider.value = Settings.Instance.Volume;
        metronomeSlider.value = Settings.Instance.MetronomeVolume;
        noteHitVolumeSlider.value = Settings.Instance.NoteHitVolume;

        redNoteDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_A];
        blueNoteDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_B];
        bombDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB];
        
        noteHitSoundDropdown.value = Settings.Instance.NoteHitSound;
        #endregion
    
        #region Graphics
        chromaLite.isOn = Settings.Instance.EmulateChromaLite;
        chroma.isOn = Settings.Instance.EmulateChromaAdvanced;
        pastNotesGridScaleSlider.value = Settings.Instance.PastNotesGridScale * 10;
        
        waveformGenerator.isOn = Settings.Instance.WaveformGenerator;
        waveformWorkflow.isOn = Settings.Instance.WaveformWorkflow;
        highlightRecentlyPlaced.isOn = Settings.Instance.HighlightLastPlacedNotes;
        
        cameraFOVSlider.value = Settings.Instance.CameraFOV;
        chunkDistanceSlider.value = Settings.Instance.ChunkDistance;
        spawnOffset.value = Settings.Instance.Offset_Spawning;
        despawnOffset.value = Settings.Instance.Offset_Despawning;
        #endregion
    
        #region Controls
        nodeEditor.isOn = Settings.Instance.NodeEditor_Enabled;
        nodeEditorKeybind.isOn = Settings.Instance.NodeEditor_UseKeybind;
        invertControls.isOn = Settings.Instance.InvertNoteControls;
        invertPrecisionScroll.isOn = Settings.Instance.InvertPrecisionScroll;
        
        mouseSensSlider.value = Mathf.RoundToInt((Settings.Instance.Camera_MouseSensitivity - 0.5f) * 2);
        cameraSpeedSlider.value = Settings.Instance.Camera_MovementSpeed;
        #endregion
        
    }

    private void InEditorWarn()
    {
        if (_inEditor) PersistentUI.Instance.ShowDialogBox(
            "Since you are in the Editor, please close and re-open the song to see changes.", null, PersistentUI.DialogBoxPresetType.Ok);
    }
    
    #region General
    public void UpdateDiscordRPC(bool enable)
    {
        if (Settings.Instance.DiscordRPCEnabled == enable) return;
        Settings.Instance.DiscordRPCEnabled = enable;
        PersistentUI.Instance.ShowDialogBox("A restart is required for changes to apply.", null, PersistentUI.DialogBoxPresetType.Ok);
    }

    public void UpdateBeatSaberInstall(string value)
    {
        string old = Settings.Instance.BeatSaberInstallation;
        Settings.Instance.BeatSaberInstallation = value;
        installFieldErrorText.text = "All good!";
        if (!Settings.ValidateDirectory(ErrorFeedback))
            Settings.Instance.BeatSaberInstallation = old;
    }

    private void ErrorFeedback(string feedback)
    {
        installFieldErrorText.text = feedback;
    }

    public void UpdateInitialBatchSize(float value)
    {
        Settings.Instance.InitialLoadBatchSize = Mathf.RoundToInt(value * 50);
    }

    public void UpdateDarkTheme(bool enable)
    {
        if (enable == Settings.Instance.DarkTheme) return;
        PersistentUI.Instance.ShowDialogBox("A restart may be required for all changes to apply.", null, PersistentUI.DialogBoxPresetType.Ok);
        Settings.Instance.DarkTheme = enable;
    }
    public void UpdateLoadEvents(bool enabled)
    {
        Settings.Instance.Load_Events = enabled;
        InEditorWarn();
    }
    
    public void UpdateLoadNotes(bool enabled)
    {
        Settings.Instance.Load_Notes = enabled;
        InEditorWarn();
    }
    public void UpdateLoadObstacles(bool enabled)
    {
        Settings.Instance.Load_Obstacles = enabled;
        InEditorWarn();
    }
    
    public void UpdateLoadOthers(bool enabled)
    {
        Settings.Instance.Load_Others = enabled;
        InEditorWarn();
    }
    #endregion
    
    
    public void UpdateEditorScale(float scale)
    {
        Settings.Instance.EditorScale = Mathf.RoundToInt(scale);
        OptionsController.Find<EditorScaleController>()?.UpdateEditorScale(scale);
    }

    public void UpdateSongSpeed(float speed)
    {
        OptionsController.Find<SongSpeedController>()?.UpdateSongSpeed(speed);
    }

    public void ToggleAutoSave(bool enabled)
    {
        OptionsController.Find<AutoSaveController>()?.ToggleAutoSave(enabled);
    }

    public void UpdateAutoSaveInterval(string value)
    {
        if (int.TryParse(value, out int interval) && interval > 0)
            Settings.Instance.AutoSaveInterval = interval;
    }

    public void UpdateNoteLanes(string value)
    {
        OptionsController.Find<NoteLanesController>()?.UpdateNoteLanes(value);
    }

    public void UpdateInvertedControls(bool inverted)
    {
        Settings.Instance.InvertNoteControls = inverted;
    }

    public void UpdateChromaOnly(bool param)
    {
        Settings.Instance.PlaceOnlyChromaEvents = param;
    }

    public void UpdateWaveform(bool enabled)
    {
        /*if (Settings.Instance.WaveformGenerator != enabled)
        {
            switch (enabled) {
                case true:
                PersistentUI.Instance.ShowDialogBox(
                    "If you are in the Editor, please exit and re-enter to see the waveform." +
                    "\n\nThe waveform will take a while to generate, depending on the length of the song." +
                    "\n\nYou will still be able to edit, map, and play while the waveform is generating.",
                    null, PersistentUI.DialogBoxPresetType.Ok);
                    break;
                case false:
                    PersistentUI.Instance.ShowDialogBox(
                        "If you are in the Editor, please exit and re-enter to remove the waveform.", null,
                        PersistentUI.DialogBoxPresetType.Ok);
                    break;
            }
        }*/
        InEditorWarn();
        Settings.Instance.WaveformGenerator = enabled;
    }

    public void UpdateCountersPlus(bool enabled)
    {
        Settings.Instance.CountersPlus = enabled;
        OptionsController.Find<CountersPlusController>()?.ToggleCounters(enabled);
    }
    
    public void UpdateChunksLoaded(float value)
    {
        Settings.Instance.ChunkDistance = Mathf.RoundToInt(value);
    }

    public void UpdateOSC()
    {
        Settings.Instance.OSC_IP = oscIP.text;
        Settings.Instance.OSC_Port = oscPort.text;
        Settings.Instance.OSC_Enabled = oscEnabled.isOn;
        OptionsController.Find<OSCMessageSender>()?.ReloadOSCStats();
    }

    public void UpdateBoxSelect(bool v)
    {
        Settings.Instance.BoxSelect = v;
    }

    public void UpdatePerfectWalls(bool v)
    {
        Settings.Instance.DontPlacePerfectZeroDurationWalls = v;
    }
    public void UpdateChromaLite(bool enabled)
    {
        if (!enabled) OptionsController.Find<PlatformDescriptor>()?.KillChromaLights();
        Settings.Instance.EmulateChromaLite = enabled;
    }

    public void UpdateChromaAdvanced(bool enabled)
    {
        Settings.Instance.EmulateChromaAdvanced = enabled;
    }

    public void UpdateRotateTrack(bool enabled)
    {
        Settings.Instance.RotateTrack = enabled;
        OptionsController.Find<TracksManager>()?.RefreshTracks();
        OptionsController.Find<RotationCallbackController>()?.RotationChangedEvent?.Invoke(false, 0);

        //InEditorWarn();
        /*PersistentUI.Instance.ShowDialogBox("If you are in the editor, side effects can happen." +
                                            "\n\nIf the rotation of the track is not aligned, going back to the beginning or reloading the editor should fix it."
            , null, PersistentUI.DialogBoxPresetType.Ok);*/
    }

    public void UpdateRecentlyPlacedNotes(bool enabled)
    {
        Settings.Instance.HighlightLastPlacedNotes = enabled;
    }

    public void UpdateSpawnOffset(float v)
    {
        Settings.Instance.Offset_Spawning = Mathf.RoundToInt(v);
    }

    public void UpdateDespawnOffset(float v)
    {
        Settings.Instance.Offset_Despawning = Mathf.RoundToInt(v);
    }


    public void UpdatePastNotesGridScale(float value)
    {
        Settings.Instance.PastNotesGridScale = (float) Math.Round(value / 10, 3);
    }

    #region Mapping
    
    #endregion
    
    #region Audio
    public void UpdateRedNoteDing(bool ding)
    {
        DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_A] = ding;
    }

    public void UpdateBlueNoteDing(bool ding)
    {
        DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_B] = ding;
    }

    public void UpdateBombDing(bool ding)
    {
        DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB] = ding;
    }
    
    public void UpdateNoteHitSound()
    {
        Settings.Instance.NoteHitSound = noteHitSoundDropdown.value;
    }
    public void UpdateNoteHitVolume(float value)
    {
        Settings.Instance.NoteHitVolume = value;
    }
    public void UpdateGameVolume(float value)
    {
        AudioListener.volume = value;
        Settings.Instance.Volume = value;
    }

    public void UpdateMetronomeVolume(float value)
    {
        Settings.Instance.MetronomeVolume = value;
        if (_metronomeHandler != null) _metronomeHandler.CowBell = Input.GetKey(KeyCode.LeftShift);
    }
    #endregion
    
    #region Graphics

    public void UpdateCameraFOV(float value)
    {
        Settings.Instance.CameraFOV = (float) Math.Round(value, 3);
    }

    #endregion
   
    #region Controls
    public void UpdateWaveformWorkflow(bool enabled)
    {
        Settings.Instance.WaveformWorkflow = enabled;
    }
    public void UpdateNodeEditor(bool enabled)
    {
        Settings.Instance.NodeEditor_Enabled = enabled;
    }
    public void UpdateNodeEditorKeybind(bool v)
    {
        Settings.Instance.NodeEditor_UseKeybind = v; 
    }
    public void UpdateMouseSensitivity(float v)
    {
        Settings.Instance.Camera_MouseSensitivity = (v / 2) + 0.5f;
    }
    public void UpdateCameraSpeed(float v)
    {
        Settings.Instance.Camera_MovementSpeed = v;
    }
    public void UpdateInvertPrecisionScroll(bool enabled)
    {
        Settings.Instance.InvertPrecisionScroll = enabled;
    }
    #endregion
    
}

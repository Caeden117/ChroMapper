using System;
using System.Globalization;
using __Scripts.MapEditor.Hit_Sounds;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsEditorSettings : MonoBehaviour
{
    [SerializeField] private Slider editorScaleSlider;
    [SerializeField] private Slider songSpeedSlider;
    [SerializeField] private Slider chunkDistanceSlider;
    [SerializeField] private Slider cameraSpeedSlider;
    [SerializeField] private Slider mouseSensSlider;
    [SerializeField] private TextMeshProUGUI cameraSpeedDisplay;
    [SerializeField] private TextMeshProUGUI mouseSensDisplay;
    [SerializeField] private TextMeshProUGUI songSpeedDisplay;
    [SerializeField] private TextMeshProUGUI chunkDistanceDisplay;
    [SerializeField] private TMP_InputField autoSaveInterval;
    [SerializeField] private TMP_InputField noteLanes;
    [SerializeField] private Toggle oscEnabled;
    [SerializeField] private TMP_InputField oscIP;
    [SerializeField] private TMP_InputField oscPort;
    [SerializeField] private Toggle invertControls;
    [SerializeField] private Toggle nodeEditor;
    [SerializeField] private Toggle nodeEditorKeybind;
    [SerializeField] private Toggle waveformGenerator;
    [SerializeField] private Toggle countersPlus;
    [SerializeField] private Toggle chromaOnly;
    [SerializeField] private Toggle redNoteDing;
    [SerializeField] private Toggle blueNoteDing;
    [SerializeField] private Toggle bombDing;
    [SerializeField] private Toggle boxSelect;
    [SerializeField] private Toggle perfectWalls;
    [SerializeField] private Toggle chromaLite;
    [SerializeField] private Toggle chroma;
    [SerializeField] private Toggle rotateTrack;
    [SerializeField] private Toggle highlightRecentlyPlaced;
    [SerializeField] private Toggle invertPrecisionScroll;
    [SerializeField] private Slider spawnOffset;
    [SerializeField] private TextMeshProUGUI spawnOffsetText;
    [SerializeField] private Slider despawnOffset;
    [SerializeField] private TextMeshProUGUI despawnOffsetText;
    [SerializeField] private Dropdown noteHitSoundDropdown;
    [SerializeField] private Slider noteHitVolumeSlider;
    [SerializeField] private TextMeshProUGUI noteHitVolumeSliderDisplay;
    [SerializeField] private Slider pastNotesGridScaleSlider;
    [SerializeField] private TextMeshProUGUI pastNotesGridScaleSliderDisplay;
    [SerializeField] private Slider cameraFOVSlider;
    [SerializeField] private TextMeshProUGUI cameraFOVSliderDisplay;
    
    void Start()
    {
        editorScaleSlider.value = Settings.Instance.EditorScale;
        songSpeedSlider.value = OptionsController.Find<SongSpeedController>()?.source.pitch * 10f ?? 10f;
        chunkDistanceSlider.value = Settings.Instance.ChunkDistance;
        autoSaveInterval.text = Settings.Instance.AutoSaveInterval.ToString();
        noteLanes.text = OptionsController.Find<NoteLanesController>()?.NoteLanes.ToString(CultureInfo.InvariantCulture) ?? "4";
        oscIP.text = Settings.Instance.OSC_IP;
        oscPort.text = Settings.Instance.OSC_Port;
        oscEnabled.isOn = Settings.Instance.OSC_Enabled;
        invertControls.isOn = Settings.Instance.InvertNoteControls;
        nodeEditor.isOn = Settings.Instance.NodeEditor_Enabled;
        waveformGenerator.isOn = Settings.Instance.WaveformGenerator;
        countersPlus.isOn = Settings.Instance.CountersPlus;
        chromaOnly.isOn = Settings.Instance.PlaceOnlyChromaEvents;
        redNoteDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_A];
        blueNoteDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_B];
        bombDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB];
        nodeEditorKeybind.isOn = Settings.Instance.NodeEditor_UseKeybind;
        boxSelect.isOn = Settings.Instance.BoxSelect;
        perfectWalls.isOn = Settings.Instance.DontPlacePerfectZeroDurationWalls;
        mouseSensSlider.value = Mathf.RoundToInt((Settings.Instance.Camera_MouseSensitivity - 0.5f) * 2);
        mouseSensDisplay.text = Settings.Instance.Camera_MouseSensitivity.ToString(CultureInfo.InvariantCulture);
        cameraSpeedSlider.value = Settings.Instance.Camera_MovementSpeed;
        cameraSpeedDisplay.text = Settings.Instance.Camera_MovementSpeed.ToString(CultureInfo.InvariantCulture);
        chromaLite.isOn = Settings.Instance.EmulateChromaLite;
        chroma.isOn = Settings.Instance.EmulateChromaAdvanced;
        rotateTrack.isOn = Settings.Instance.RotateTrack;
        highlightRecentlyPlaced.isOn = Settings.Instance.HighlightLastPlacedNotes;
        invertPrecisionScroll.isOn = Settings.Instance.InvertPrecisionScroll;
        spawnOffset.value = Settings.Instance.Offset_Spawning;
        spawnOffsetText.text = Settings.Instance.Offset_Spawning.ToString();
        despawnOffset.value = Settings.Instance.Offset_Despawning;
        despawnOffsetText.text = Settings.Instance.Offset_Despawning.ToString();
        noteHitSoundDropdown.value = Settings.Instance.NoteHitSound;
        noteHitVolumeSlider.value = Settings.Instance.MetronomeVolume * 10;
        noteHitVolumeSliderDisplay.text = $"{noteHitVolumeSlider.value * 10}%";
        pastNotesGridScaleSlider.value = Settings.Instance.PastNotesGridScale * 10;
        pastNotesGridScaleSliderDisplay.text = $"{pastNotesGridScaleSlider.value * 10}%";
        cameraFOVSlider.value = Settings.Instance.CameraFOV;
        cameraFOVSliderDisplay.text = $"{Math.Round(cameraFOVSlider.value, 1)}°";
    }

    #region Update Editor Variables
    public void UpdateEditorScale(float scale)
    {
        Settings.Instance.EditorScale = Mathf.RoundToInt(scale);
        OptionsController.Find<EditorScaleController>()?.UpdateEditorScale(scale);
    }

    public void UpdateSongSpeed(float speed)
    {
        OptionsController.Find<SongSpeedController>()?.UpdateSongSpeed(speed);
        songSpeedDisplay.text = speed * 10 + "%";
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

    public void UpdateNodeEditor(bool enabled)
    {
        Settings.Instance.NodeEditor_Enabled = enabled;
    }

    public void UpdateWaveform(bool enabled)
    {
        if (Settings.Instance.WaveformGenerator != enabled)
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
        }
        Settings.Instance.WaveformGenerator = enabled;
    }

    public void UpdateCountersPlus(bool enabled)
    {
        Settings.Instance.CountersPlus = enabled;
        OptionsController.Find<CountersPlusController>()?.ToggleCounters(enabled);
    }

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

    public void UpdateChunksLoaded(float value)
    {
        int chunks = Mathf.RoundToInt(value);
        chunkDistanceDisplay.text = chunks.ToString();
        Settings.Instance.ChunkDistance = chunks;
    }
    
    public void UpdateOSC()
    {
        Settings.Instance.OSC_IP = oscIP.text;
        Settings.Instance.OSC_Port = oscPort.text;
        Settings.Instance.OSC_Enabled = oscEnabled.isOn;
        OptionsController.Find<OSCMessageSender>()?.ReloadOSCStats();
    }

    public void UpdateNodeEditorKeybind(bool v)
    {
        Settings.Instance.NodeEditor_UseKeybind = v; 
    }

    public void UpdateBoxSelect(bool v)
    {
        Settings.Instance.BoxSelect = v;
    }

    public void UpdatePerfectWalls(bool v)
    {
        Settings.Instance.DontPlacePerfectZeroDurationWalls = v;
    }

    public void UpdateMouseSensitivity(float v)
    {
        Settings.Instance.Camera_MouseSensitivity = (v / 2) + 0.5f;
        mouseSensDisplay.text = Settings.Instance.Camera_MouseSensitivity.ToString(CultureInfo.InvariantCulture);
    }
    public void UpdateCameraSpeed(float v)
    {
        Settings.Instance.Camera_MovementSpeed = v;
        cameraSpeedDisplay.text = Settings.Instance.Camera_MovementSpeed.ToString(CultureInfo.InvariantCulture);
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
        if (Settings.Instance.RotateTrack == enabled) return;
        RotationCallbackController callbackController = OptionsController.Find<RotationCallbackController>();
        callbackController?.RotationChangedEvent?.Invoke(false, Settings.Instance.RotateTrack ? 0 : callbackController.Rotation);

        PersistentUI.Instance.ShowDialogBox("If you are in the editor, side effects can happen." +
                                            "\n\nIf the rotation of the track is not aligned, going back to the beginning or reloading the editor should fix it."
            , null, PersistentUI.DialogBoxPresetType.Ok);
    }

    public void UpdateRecentlyPlacedNotes(bool enabled)
    {
        Settings.Instance.HighlightLastPlacedNotes = enabled;
    }

    public void UpdateInvertPrecisionScroll(bool enabled)
    {
        Settings.Instance.InvertPrecisionScroll = enabled;
    }

    public void UpdateSpawnOffset(float v)
    {
        Settings.Instance.Offset_Spawning = Mathf.RoundToInt(v);
        spawnOffsetText.text = Settings.Instance.Offset_Spawning.ToString();
    }

    public void UpdateDespawnOffset(float v)
    {
        Settings.Instance.Offset_Despawning = Mathf.RoundToInt(v);
        despawnOffsetText.text = Settings.Instance.Offset_Despawning.ToString();
    }

    public void UpdateNoteHitSound()
    {
        Settings.Instance.NoteHitSound = noteHitSoundDropdown.value;
    }
    
    public void UpdateNoteHitVolume(float value)
    {
        Settings.Instance.NoteHitVolume = value / 10;
        noteHitVolumeSliderDisplay.text = $"{value * 10}%";
    }

    public void UpdatePastNotesGridScale(float value)
    {
        Settings.Instance.PastNotesGridScale = value / 10;
        pastNotesGridScaleSliderDisplay.text = $"{value * 10}%";
    }
    
    public void UpdateCameraFOV(float value)
    {
        value = (float)Math.Round(value, 3);
        Settings.Instance.CameraFOV = value;
        cameraFOVSliderDisplay.text = $"{Math.Round(value, 1)}°";
    }
    #endregion
}

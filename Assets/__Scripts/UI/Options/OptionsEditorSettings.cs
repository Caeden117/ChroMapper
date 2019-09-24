using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsEditorSettings : MonoBehaviour
{
    [SerializeField] private Slider editorScaleSlider;
    [SerializeField] private Slider songSpeedSlider;
    [SerializeField] private Slider chunkDistanceSlider;
    [SerializeField] private TextMeshProUGUI songSpeedDisplay;
    [SerializeField] private TextMeshProUGUI chunkDistanceDisplay;
    [SerializeField] private TMP_InputField autoSaveInterval;
    [SerializeField] private TMP_InputField noteLanes;
    [SerializeField] private Toggle oscEnabled;
    [SerializeField] private TMP_InputField oscIP;
    [SerializeField] private TMP_InputField oscPort;
    [SerializeField] private Toggle invertControls;
    [SerializeField] private Toggle nodeEditor;
    [SerializeField] private Toggle waveformGenerator;
    [SerializeField] private Toggle countersPlus;
    [SerializeField] private Toggle chromaOnly;
    [SerializeField] private Toggle redNoteDing;
    [SerializeField] private Toggle blueNoteDing;
    [SerializeField] private Toggle bombDing;
    void Start()
    {
        editorScaleSlider.value = Mathf.Sqrt(EditorScaleController.EditorScale);
        songSpeedSlider.value = OptionsController.Find<SongSpeedController>()?.source.pitch * 10f ?? 10f;
        chunkDistanceSlider.value = BeatmapObjectContainerCollection.ChunkRenderDistance;
        autoSaveInterval.text = OptionsController.Find<AutoSaveController>()?.AutoSaveIntervalMinutes.ToString() ?? "5";
        noteLanes.text = OptionsController.Find<NoteLanesController>()?.NoteLanes.ToString() ?? "4";
        oscIP.text = Settings.OSCIP;
        oscPort.text = Settings.OSCPort;
        oscEnabled.isOn = Settings.OSCEnabled;
        invertControls.isOn = OptionsController.Find<KeybindsController>()?.InvertNoteKeybinds ?? false;
        nodeEditor.isOn = OptionsController.Find<NodeEditorController>()?.AdvancedSetting ?? false;
        waveformGenerator.isOn = NodeEditorController.IsActive;
        countersPlus.isOn = CountersPlusController.IsActive;
        chromaOnly.isOn = OptionsController.Find<EventPlacement>()?.PlaceOnlyChromaEvent ?? false;
        redNoteDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_A];
        blueNoteDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_B];
        bombDing.isOn = DingOnNotePassingGrid.NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB];
    }

    #region Update Editor Variables
    public void UpdateEditorScale(float scale)
    {
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
        OptionsController.Find<AutoSaveController>()?.UpdateAutoSaveInterval(value);
    }

    public void UpdateNoteLanes(string value)
    {
        OptionsController.Find<NoteLanesController>()?.UpdateNoteLanes(value);
    }

    public void UpdateInvertedControls(bool inverted)
    {
        if (OptionsController.Find<KeybindsController>() != null)
            OptionsController.Find<KeybindsController>().InvertNoteKeybinds = inverted;
    }

    public void UpdateChromaOnly(bool param)
    {
        if (OptionsController.Find<EventPlacement>() != null)
        {
            OptionsController.Find<EventPlacement>().PlaceOnlyChromaEvent = param;
            OptionsController.Find<EventPlacement>().UpdateChromaBool(param);
        }
    }

    public void UpdateNodeEditor(bool enabled)
    {
        OptionsController.Find<NodeEditorController>()?.UpdateAdvancedSetting(enabled);
    }

    public void UpdateWaveform(bool enabled)
    {
        OptionsController.Find<WaveformGenerator>()?.UpdateActive(enabled);
    }

    public void UpdateCountersPlus(bool enabled)
    {
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
        BeatmapObjectContainerCollection.ChunkRenderDistance = chunks;
    }
    
    public void UpdateOSC()
    {
        Settings.OSCIP = oscIP.text;
        Settings.OSCPort = oscPort.text;
        Settings.OSCEnabled = oscEnabled.isOn;
        OptionsController.Find<OSCMessageSender>()?.ReloadOSCStats();
    }
    #endregion
}

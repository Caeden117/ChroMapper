using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Localization.Components;

public class CountersPlusController : MonoBehaviour {

    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private BPMChangesContainer bpm;
    [SerializeField] private AudioSource cameraAudioSource;
    [SerializeField] private AudioTimeSyncController atsc;

    [Header("Localized Strings")]
    [SerializeField] private LocalizeStringEvent notesMesh;
    [SerializeField] private LocalizeStringEvent notesPSMesh;
    [SerializeField] private LocalizeStringEvent[] extraNoteStrings;
    [SerializeField] private LocalizeStringEvent obstacleString;
    [SerializeField] private LocalizeStringEvent eventString;
    [SerializeField] private LocalizeStringEvent bpmString;
    [SerializeField] private LocalizeStringEvent currentBPMString;
    [SerializeField] private LocalizeStringEvent selectionString;
    [SerializeField] private LocalizeStringEvent timeMappingString;

    private SwingsPerSecond swingsPerSecond;
    private float lastBPM = 0;

    private void Start()
    {
        Settings.NotifyBySettingName("CountersPlus", UpdateCountersVisibility);
        UpdateCountersVisibility(Settings.Instance.CountersPlus);
        
        swingsPerSecond = new SwingsPerSecond(notes, obstacles);

        LoadInitialMap.LevelLoadedEvent += LevelLoadedEvent;
        SelectionController.SelectionChangedEvent += SelectionChangedEvent;
    }

    public void UpdateStatistic(CountersPlusStatistic stat)
    {
        if (!Settings.Instance.CountersPlus["enabled"])
            return;

        switch (stat)
        {
            case CountersPlusStatistic.Notes:
                UpdateNoteStats();
                break;
            case CountersPlusStatistic.Obstacles:
                obstacleString.StringReference.RefreshString();
                break;
            case CountersPlusStatistic.Events:
                eventString.StringReference.RefreshString();
                break;
            case CountersPlusStatistic.BPM_Changes:
                bpmString.StringReference.RefreshString();
                break;
        }
    }

    private void LevelLoadedEvent()
    {
        // Bit archaic but this allows us to refresh everything once on startup
        foreach (var enumValue in System.Enum.GetValues(typeof(CountersPlusStatistic)))
        {
            UpdateStatistic((CountersPlusStatistic)enumValue);
        }
    }

    private void SelectionChangedEvent()
    {
        UpdateSelectionStats();
    }

    private void UpdateNoteStats()
    {
        if (SelectionController.HasSelectedObjects() && NotesSelected > 0)
        {
            notesMesh.StringReference.TableEntryReference = "countersplus.notes.selected";
            notesPSMesh.StringReference.TableEntryReference = "countersplus.nps.selected";
        }
        else
        {
            notesMesh.StringReference.TableEntryReference = "countersplus.notes";
            notesPSMesh.StringReference.TableEntryReference = "countersplus.nps";
        }

        notesMesh.StringReference.RefreshString();
        notesPSMesh.StringReference.RefreshString();

        swingsPerSecond.Update();

        foreach (var str in extraNoteStrings) str.StringReference.RefreshString();
    }

    private void UpdateSelectionStats()
    {
        selectionString.gameObject.SetActive(SelectionController.HasSelectedObjects());

        if (SelectionController.HasSelectedObjects() && NotesSelected > 0)
        {
            notesMesh.StringReference.TableEntryReference = "countersplus.notes.selected";
            notesPSMesh.StringReference.TableEntryReference = "countersplus.nps.selected";
        }
        else
        {
            notesMesh.StringReference.TableEntryReference = "countersplus.notes";
            notesPSMesh.StringReference.TableEntryReference = "countersplus.nps";
        }

        notesMesh.StringReference.RefreshString();
        notesPSMesh.StringReference.RefreshString();
        selectionString.StringReference.RefreshString();
    }

    private void Update() // i do want to update this every single frame
    {
        if (Application.isFocused)
        {
            BeatSaberSongContainer.Instance.map._time += Time.deltaTime / 60; // only tick while application is focused

            var timeMapping = BeatSaberSongContainer.Instance.map._time;
            seconds = Mathf.Abs(Mathf.FloorToInt(timeMapping * 60 % 60));
            minutes = Mathf.FloorToInt(timeMapping % 60);
            hours = Mathf.FloorToInt(timeMapping / 60);

            timeMappingString.StringReference.RefreshString();
        }

        var currentBPM = CurrentBPM;
        if (lastBPM != currentBPM)
        {
            currentBPMString.StringReference.RefreshString();
            lastBPM = currentBPM;
        }
    }

    public void UpdateCountersVisibility(object obj)
    {
        CountersPlusSettings settings = (CountersPlusSettings)obj;
        LocalizeStringEvent[] strings = GetComponentsInChildren<LocalizeStringEvent>(true);
        foreach (LocalizeStringEvent s in strings)
        {
            string key = s.ToString().Replace(" (UnityEngine.Localization.Components.LocalizeStringEvent)", ""); // yep
            bool settingExists = settings.TryGetValue(key, out var counterEnabled);
            if (settingExists) s.gameObject.SetActive(settings["enabled"] && counterEnabled);
        }
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("CountersPlus");
        SelectionController.SelectionChangedEvent -= SelectionChangedEvent;
        LoadInitialMap.LevelLoadedEvent -= LevelLoadedEvent;
    }

    ///// Localization /////

    public int NotesCount
        => notes.LoadedObjects.Where(note => ((BeatmapNote) note)._type != BeatmapNote.NOTE_TYPE_BOMB).Count();

    public float NPSCount => NotesCount / cameraAudioSource.clip.length;

    public int NotesSelected
        => SelectionController.SelectedObjects.Where(x => x is BeatmapNote note && note._type != BeatmapNote.NOTE_TYPE_BOMB).Count();

    public float NPSselected
    {
        get
        {
            List<BeatmapObject> sel = SelectionController.SelectedObjects.OrderBy(it => it._time).ToList();
            float beatTimeDiff = sel.Last()._time - sel.First()._time;
            float secDiff = atsc.GetSecondsFromBeat(beatTimeDiff);

            return NotesSelected / secDiff;
        }
    }

    public int BombCount
        => notes.LoadedObjects.Where(note => ((BeatmapNote)note)._type == BeatmapNote.NOTE_TYPE_BOMB).Count();

    public int ObstacleCount => obstacles.LoadedObjects.Count;

    public int EventCount => events.LoadedObjects.Count;

    public int BPMCount => bpm.LoadedObjects.Count;

    public int SelectedCount => SelectionController.SelectedObjects.Count;

    public float OverallSPS => swingsPerSecond.Total.Overall;

    public float CurrentBPM
        => bpm.FindLastBPM(atsc.CurrentBeat, true)?._BPM ?? BeatSaberSongContainer.Instance.song.beatsPerMinute;

    public float RedBlueRatio
    {
        get
        {
            int redCount = notes.LoadedObjects.Where(note => ((BeatmapNote)note)._type == BeatmapNote.NOTE_TYPE_A).Count();
            int blueCount = notes.LoadedObjects.Where(note => ((BeatmapNote)note)._type == BeatmapNote.NOTE_TYPE_B).Count();
            return blueCount == 0 ? 0f : redCount / (float)blueCount;
        }
    }

    [HideInInspector]
    public int hours, minutes, seconds = 0;
}

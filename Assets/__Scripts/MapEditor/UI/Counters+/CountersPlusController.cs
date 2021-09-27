using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Serialization;

public class CountersPlusController : MonoBehaviour
{
    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private BPMChangesContainer bpm;
    [SerializeField] private AudioSource cameraAudioSource;
    [SerializeField] private AudioTimeSyncController atsc;

    [Header("Localized Strings")]
    [SerializeField]
    private LocalizeStringEvent notesMesh;

    [SerializeField] private LocalizeStringEvent notesPSMesh;
    [SerializeField] private LocalizeStringEvent[] extraNoteStrings;
    [SerializeField] private LocalizeStringEvent obstacleString;
    [SerializeField] private LocalizeStringEvent eventString;
    [SerializeField] private LocalizeStringEvent bpmString;
    [FormerlySerializedAs("currentBPMString")] [SerializeField] private LocalizeStringEvent currentBpmString;
    [SerializeField] private LocalizeStringEvent selectionString;
    [SerializeField] private LocalizeStringEvent timeMappingString;

    private float lastBpm;

    private SwingsPerSecond swingsPerSecond;

#pragma warning disable IDE1006 // Naming Styles
    ///// Localization /////

    // Unfortunately the way localization is set up, we need this to be public AND with the current naming
    // We *COULD* rename every localization entry to use PascalCase versions but it's more effort to do that.
    [FormerlySerializedAs("hours")] [HideInInspector] public int hours;
    [FormerlySerializedAs("minutes")] [HideInInspector] public int minutes;
    [FormerlySerializedAs("seconds")] [HideInInspector] public int seconds;

    public int NotesCount
        => notes.LoadedObjects.Where(note => ((BeatmapNote)note).Type != BeatmapNote.NoteTypeBomb).Count();

    public float NPSCount => NotesCount / cameraAudioSource.clip.length;

    public int NotesSelected
        => SelectionController.SelectedObjects
            .Where(x => x is BeatmapNote note && note.Type != BeatmapNote.NoteTypeBomb).Count();

    public float NPSselected
    {
        get
        {
            var sel = SelectionController.SelectedObjects.OrderBy(it => it.Time).ToList();
            var beatTimeDiff = sel.Last().Time - sel.First().Time;
            var secDiff = atsc.GetSecondsFromBeat(beatTimeDiff);

            return NotesSelected / secDiff;
        }
    }

    public int BombCount
        => notes.LoadedObjects.Where(note => ((BeatmapNote)note).Type == BeatmapNote.NoteTypeBomb).Count();

    public int ObstacleCount => obstacles.LoadedObjects.Count;

    public int EventCount => events.LoadedObjects.Count;

    public int BPMCount => bpm.LoadedObjects.Count;

    public int SelectedCount => SelectionController.SelectedObjects.Count;

    public float OverallSPS => swingsPerSecond.Total.Overall;

    public float CurrentBPM
        => bpm.FindLastBpm(atsc.CurrentBeat)?.Bpm ?? BeatSaberSongContainer.Instance.Song.BeatsPerMinute;

    public float RedBlueRatio
    {
        get
        {
            var redCount = notes.LoadedObjects.Where(note => ((BeatmapNote)note).Type == BeatmapNote.NoteTypeA)
                .Count();
            var blueCount = notes.LoadedObjects.Where(note => ((BeatmapNote)note).Type == BeatmapNote.NoteTypeB)
                .Count();
            return blueCount == 0 ? 0f : redCount / (float)blueCount;
        }
    }
#pragma warning restore IDE1006 // Naming Styles

    private void Start()
    {
        Settings.NotifyBySettingName("CountersPlus", UpdateCountersVisibility);
        UpdateCountersVisibility(Settings.Instance.CountersPlus);

        swingsPerSecond = new SwingsPerSecond(notes, obstacles);

        LoadInitialMap.LevelLoadedEvent += LevelLoadedEvent;
        SelectionController.SelectionChangedEvent += SelectionChangedEvent;
    }

    private void Update() // i do want to update this every single frame
    {
        if (Application.isFocused)
        {
            BeatSaberSongContainer.Instance.Map.Time += Time.deltaTime / 60; // only tick while application is focused

            var timeMapping = BeatSaberSongContainer.Instance.Map.Time;
            seconds = Mathf.Abs(Mathf.FloorToInt(timeMapping * 60 % 60));
            minutes = Mathf.FloorToInt(timeMapping % 60);
            hours = Mathf.FloorToInt(timeMapping / 60);

            timeMappingString.StringReference.RefreshString();
        }

        var currentBpm = CurrentBPM;
        if (lastBpm != currentBpm)
        {
            currentBpmString.StringReference.RefreshString();
            lastBpm = currentBpm;
        }
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("CountersPlus");
        SelectionController.SelectionChangedEvent -= SelectionChangedEvent;
        LoadInitialMap.LevelLoadedEvent -= LevelLoadedEvent;
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
            case CountersPlusStatistic.BpmChanges:
                bpmString.StringReference.RefreshString();
                break;
        }
    }

    private void LevelLoadedEvent()
    {
        // Bit archaic but this allows us to refresh everything once on startup
        foreach (var enumValue in Enum.GetValues(typeof(CountersPlusStatistic)))
            UpdateStatistic((CountersPlusStatistic)enumValue);
    }

    private void SelectionChangedEvent() => UpdateSelectionStats();

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

    public void UpdateCountersVisibility(object obj)
    {
        var settings = (CountersPlusSettings)obj;
        var strings = GetComponentsInChildren<LocalizeStringEvent>(true);
        foreach (var s in strings)
        {
            var key = s.ToString().Replace(" (UnityEngine.Localization.Components.LocalizeStringEvent)", ""); // yep
            var settingExists = settings.TryGetValue(key, out var counterEnabled);
            if (settingExists) s.gameObject.SetActive(settings["enabled"] && counterEnabled);
        }
    }
}

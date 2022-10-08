using System;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Serialization;

public class CountersPlusController : MonoBehaviour
{
    [FormerlySerializedAs("notes")] [SerializeField] private NoteGridContainer noteGrid;
    [FormerlySerializedAs("obstacles")] [SerializeField] private ObstacleGridContainer obstacleGrid;
    [FormerlySerializedAs("chains")] [SerializeField] private ChainGridContainer chainGrid;
    [FormerlySerializedAs("events")] [SerializeField] private EventGridContainer eventGrid;
    [SerializeField] private BPMChangeGridContainer bpm;
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


    public int NotesCount =>
       noteGrid.LoadedObjects.Where(note => ((INote)note).Type != (int)NoteType.Bomb).Count();


    public float NPSCount => NotesCount / cameraAudioSource.clip.length;

    public int NotesSelected
        => SelectionController.SelectedObjects
            .Where(x => (x is INote note && note.Type != (int)NoteType.Bomb) || x is IChain).Count();

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
        => noteGrid.LoadedObjects.Where(note => ((INote)note).Type == (int)NoteType.Bomb).Count();

    public int ObstacleCount => obstacleGrid.LoadedObjects.Count;
    
    public int ChainCount => chainGrid.LoadedObjects.Count;

    public int EventCount => eventGrid.LoadedObjects.Count;

    public int BPMCount => bpm.LoadedObjects.Count;

    public int SelectedCount => SelectionController.SelectedObjects.Count;

    public float OverallSPS => swingsPerSecond.Total.Overall;

    public float CurrentBPM
        => bpm.FindLastBpm(atsc.CurrentBeat)?.Bpm ?? BeatSaberSongContainer.Instance.Song.BeatsPerMinute;

    public float RedBlueRatio
    {
        get
        {
            var redCount = noteGrid.LoadedObjects.Where(note => ((INote)note).Type == (int)NoteType.Red)
                .Count();
            var blueCount = noteGrid.LoadedObjects.Where(note => ((INote)note).Type == (int)NoteType.Blue)
                .Count();
            return blueCount == 0 ? 0f : redCount / (float)blueCount;
        }
    }
#pragma warning restore IDE1006 // Naming Styles

    // I'm going to be doing some bit shift gaming, don't mind me
    private CountersPlusStatistic stringRefreshQueue = CountersPlusStatistic.Invalid;

    private void Start()
    {
        Settings.NotifyBySettingName("CountersPlus", UpdateCountersVisibility);
        UpdateCountersVisibility(Settings.Instance.CountersPlus);

        swingsPerSecond = new SwingsPerSecond(noteGrid, obstacleGrid, chainGrid);

        LoadInitialMap.LevelLoadedEvent += LevelLoadedEvent;
        SelectionController.SelectionChangedEvent += SelectionChangedEvent;
    }

    private void Update() // i do want to update this every single frame
    {
        if (Application.isFocused)
        {
            BeatSaberSongContainer.Instance.Map.Time += Time.deltaTime / 60; // only tick while application is focused

            var timeMapping = BeatSaberSongContainer.Instance.Map.Time;
            var newSeconds = Mathf.Abs(Mathf.FloorToInt(timeMapping * 60 % 60));

            if (newSeconds != seconds)
            {
                seconds = newSeconds;
                minutes = Mathf.FloorToInt(timeMapping % 60);
                hours = Mathf.FloorToInt(timeMapping / 60);

                timeMappingString.StringReference.RefreshString();
            }
        }

        var currentBpm = CurrentBPM;
        if (lastBpm != currentBpm)
        {
            currentBpmString.StringReference.RefreshString();
            lastBpm = currentBpm;
        }

        // Might be a bit unreadable but I'm essentially checking if a bit is set corresponding to a specific statistic.
        // If the bit is set (non-zero), it would update that particular statistic.
        // This essentially ensures that each statistic can only be refreshed once per frame.
        if (stringRefreshQueue > 0)
        {
            if ((stringRefreshQueue & CountersPlusStatistic.Notes) != 0)
                UpdateNoteStats();

            if ((stringRefreshQueue & CountersPlusStatistic.Obstacles) != 0)
                obstacleString.StringReference.RefreshString();

            if ((stringRefreshQueue & CountersPlusStatistic.Events) != 0)
                eventString.StringReference.RefreshString();

            if ((stringRefreshQueue & CountersPlusStatistic.BpmChanges) != 0)
                bpmString.StringReference.RefreshString();

            if ((stringRefreshQueue & CountersPlusStatistic.Selection) != 0)
                UpdateSelectionStats();

            stringRefreshQueue = 0;
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

        // Bit shift stat into queue
        stringRefreshQueue |= stat;
    }

    private void LevelLoadedEvent()
    {
        // Bit archaic but this allows us to refresh everything once on startup
        foreach (var enumValue in Enum.GetValues(typeof(CountersPlusStatistic)))
            UpdateStatistic((CountersPlusStatistic)enumValue);
    }

    private void SelectionChangedEvent() => UpdateStatistic(CountersPlusStatistic.Selection);

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

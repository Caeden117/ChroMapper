using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Name and idea totally not stolen directly from Beat Saber
public class BeatmapObjectCallbackController : MonoBehaviour {

    [SerializeField] NotesContainer notesContainer;
    [SerializeField] EventsContainer eventsContainer;
    [SerializeField] BPMChangesContainer bpmContainer;

    [SerializeField] AudioTimeSyncController timeSyncController;

    [SerializeField] private bool useOffsetFromConfig = true;
    [Tooltip("Whether or not to use the Despawn or Spawn offset from settings.")]
    [SerializeField] private bool useDespawnOffset = false;
    [SerializeField] public float offset = 0;

    [SerializeField] int nextNoteIndex = 0;
    [SerializeField] int nextEventIndex = 0;

    public int NextNoteIndex => nextNoteIndex;

    public int NextEventIndex => nextEventIndex;

    float curNoteTime;

    public Action<bool, int, BeatmapObject> NotePassedThreshold;
    public Action<bool, int, BeatmapObject> EventPassedThreshold;
    public Action<bool, int> RecursiveNoteCheckFinished;
    public Action<bool, int> RecursiveEventCheckFinished;
    
    private List<BeatmapObjectContainer> nextEvents = new List<BeatmapObjectContainer>();
    private Queue<BeatmapObjectContainer> allEvents = new Queue<BeatmapObjectContainer>();
    private List<BeatmapObjectContainer> nextNotes = new List<BeatmapObjectContainer>();
    private Queue<BeatmapObjectContainer> allNotes = new Queue<BeatmapObjectContainer>();
    private static int eventsToLookAhead = 75;
    private static int notesToLookAhead = 25;

    private void OnEnable() {
        timeSyncController.OnPlayToggle += OnPlayToggle;
    }

    private void OnDisable() {
        timeSyncController.OnPlayToggle -= OnPlayToggle;
    }

    private void OnPlayToggle(bool playing) {
        CheckAllNotes(false);
        CheckAllEvents(false);
    }

    private void LateUpdate()
    {
        if (useOffsetFromConfig)
        {
            if (useDespawnOffset) offset = Settings.Instance.Offset_Despawning * -1;
            else offset = Settings.Instance.Offset_Spawning;
        }
        if (timeSyncController.IsPlaying) {
            curNoteTime = timeSyncController.CurrentBeat;
            RecursiveCheckNotes(true, true);
            RecursiveCheckEvents(true, true);
        }
    }

    private void CheckAllNotes(bool natural)
    {
        notesContainer.SortObjects();
        curNoteTime = timeSyncController.CurrentBeat;
        allNotes.Clear();
        allNotes = new Queue<BeatmapObjectContainer>(notesContainer.LoadedContainers.Where(x => x.objectData._time >= curNoteTime + offset));
        nextNoteIndex = notesContainer.LoadedContainers.Count - allNotes.Count;
        RecursiveNoteCheckFinished?.Invoke(natural, nextNoteIndex - 1);
        allNotes.OrderBy(x => x.objectData._time);
        nextNotes.Clear();
        for (int i = 0; i < notesToLookAhead; i++)
            if (allNotes.Any()) nextNotes.Add(allNotes.Dequeue());
    }

    private void CheckAllEvents(bool natural)
    {
        allEvents.Clear();
        allEvents = new Queue<BeatmapObjectContainer>(eventsContainer.LoadedContainers.Where(x => x.objectData._time >= curNoteTime + offset));
        nextEventIndex = eventsContainer.LoadedContainers.Count - allEvents.Count;
        RecursiveEventCheckFinished?.Invoke(natural, nextEventIndex - 1);
        allEvents.OrderBy(x => x.objectData._time);
        nextEvents.Clear();
        for (int i = 0; i < eventsToLookAhead; i++)
            if (allEvents.Any()) nextEvents.Add(allEvents.Dequeue());
    }

    private void RecursiveCheckNotes(bool init, bool natural)
    {
        List<BeatmapObjectContainer> passed = new List<BeatmapObjectContainer>(nextNotes.Where(x => x.objectData._time <= curNoteTime + offset));
        foreach (BeatmapObjectContainer newlyAdded in passed)
        {
            if (natural) NotePassedThreshold?.Invoke(init, nextNoteIndex, newlyAdded.objectData);
            nextNotes.Remove(newlyAdded);
            if (allNotes.Any() && natural) nextNotes.Add(allNotes.Dequeue());
            nextNoteIndex++;
        }
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        List<BeatmapObjectContainer> passed = new List<BeatmapObjectContainer>(nextEvents.Where(x => x.objectData._time <= curNoteTime + offset));
        foreach (BeatmapObjectContainer newlyAdded in passed)
        {
            if (natural) EventPassedThreshold?.Invoke(init, nextEventIndex, newlyAdded.objectData);
            nextEvents.Remove(newlyAdded);
            if (allEvents.Any() && natural) nextEvents.Add(allEvents.Dequeue());
            nextEventIndex++;
        }
    }
}

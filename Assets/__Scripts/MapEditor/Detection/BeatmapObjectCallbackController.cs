using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Name and idea totally not stolen directly from Beat Saber
public class BeatmapObjectCallbackController : MonoBehaviour {

    [SerializeField] NotesContainer notesContainer;
    [SerializeField] EventsContainer eventsContainer;

    [SerializeField] AudioTimeSyncController timeSyncController;

    [SerializeField] private bool useOffsetFromConfig = true;
    [Tooltip("Whether or not to use the Despawn or Spawn offset from settings.")]
    [SerializeField] private bool useDespawnOffset = false;
    [SerializeField] public float offset = 0;

    [SerializeField] int nextNoteIndex = 0;
    [SerializeField] int nextEventIndex = 0;

    float curTime;

    public Action<bool, int, BeatmapObject> NotePassedThreshold;
    public Action<bool, int, BeatmapObject> EventPassedThreshold;
    public Action<bool, int> RecursiveNoteCheckFinished;
    public Action<bool, int> RecursiveEventCheckFinished;
    
    private List<BeatmapObject> nextEvents = new List<BeatmapObject>();
    private Queue<BeatmapObject> allEvents = new Queue<BeatmapObject>();
    private List<BeatmapObject> nextNotes = new List<BeatmapObject>();
    private Queue<BeatmapObject> allNotes = new Queue<BeatmapObject>();
    private static int eventsToLookAhead = 75;
    private static int notesToLookAhead = 25;

    private void OnEnable() {
        timeSyncController.OnPlayToggle += OnPlayToggle;
    }

    private void OnDisable() {
        timeSyncController.OnPlayToggle -= OnPlayToggle;
    }

    private void OnPlayToggle(bool playing) {
        if (playing)
        {
            CheckAllNotes(false);
            CheckAllEvents(false);
        }
    }

    private void LateUpdate()
    {
        if (useOffsetFromConfig)
        {
            if (useDespawnOffset) offset = Settings.Instance.Offset_Despawning * -1;
            else offset = Settings.Instance.Offset_Spawning;
        }
        if (timeSyncController.IsPlaying) {
            curTime = timeSyncController.CurrentBeat;
            RecursiveCheckNotes(true, true);
            RecursiveCheckEvents(true, true);
        }
    }

    private void CheckAllNotes(bool natural)
    {
        //notesContainer.SortObjects();
        curTime = timeSyncController.CurrentBeat;
        allNotes.Clear();
        allNotes = new Queue<BeatmapObject>(notesContainer.LoadedObjects);
        while (allNotes.Count > 0 && allNotes.Peek()._time < curTime + offset)
        {
            allNotes.Dequeue();
        }
        nextNoteIndex = notesContainer.LoadedObjects.Count - allNotes.Count;
        RecursiveNoteCheckFinished?.Invoke(natural, nextNoteIndex - 1);
        nextNotes.Clear();
        for (int i = 0; i < notesToLookAhead; i++)
            if (allNotes.Any()) nextNotes.Add(allNotes.Dequeue());
    }

    private void CheckAllEvents(bool natural)
    {
        allEvents.Clear();
        allEvents = new Queue<BeatmapObject>(eventsContainer.LoadedObjects);
        while (allEvents.Count > 0 && allEvents.Peek()._time < curTime + offset)
        {
            allEvents.Dequeue();
        }
        nextEventIndex = eventsContainer.LoadedObjects.Count - allEvents.Count;
        RecursiveEventCheckFinished?.Invoke(natural, nextEventIndex - 1);
        nextEvents.Clear();
        for (int i = 0; i < eventsToLookAhead; i++)
            if (allEvents.Any()) nextEvents.Add(allEvents.Dequeue());
    }

    private void RecursiveCheckNotes(bool init, bool natural)
    {
        var passed = nextNotes.FindAll(x => x._time <= curTime + offset);
        foreach (BeatmapObject newlyAdded in passed)
        {
            if (natural) NotePassedThreshold?.Invoke(init, nextNoteIndex, newlyAdded);
            nextNotes.Remove(newlyAdded);
            if (allNotes.Any() && natural) nextNotes.Add(allNotes.Dequeue());
            nextNoteIndex++;
        }
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        var passed = nextEvents.FindAll(x => x._time <= curTime + offset);
        foreach (BeatmapObject newlyAdded in passed)
        {
            if (natural) EventPassedThreshold?.Invoke(init, nextEventIndex, newlyAdded);
            nextEvents.Remove(newlyAdded);
            if (allEvents.Any() && natural) nextEvents.Add(allEvents.Dequeue());
            nextEventIndex++;
        }
    }
}

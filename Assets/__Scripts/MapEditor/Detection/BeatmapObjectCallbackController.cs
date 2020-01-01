using System;
using System.Collections;
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

    public int NextNoteIndex {
        get { return nextNoteIndex; }
    }

    public int NextEventIndex
    {
        get { return nextEventIndex; }
    }

    float curNoteTime;

    public Action<bool, int, BeatmapObject> NotePassedThreshold;
    public Action<bool, int, BeatmapObject> EventPassedThreshold;
    public Action<bool, int> RecursiveNoteCheckFinished;
    public Action<bool, int> RecursiveEventCheckFinished;
    
    private List<BeatmapObjectContainer> nextEvents = new List<BeatmapObjectContainer>();
    private Queue<BeatmapObjectContainer> allEvents = new Queue<BeatmapObjectContainer>();
    private static int eventsToLookAhead = 75;

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
        nextNoteIndex = 0;
        RecursiveCheckNotes(true, natural);
        if (RecursiveNoteCheckFinished != null) RecursiveNoteCheckFinished(natural, nextNoteIndex - 1);
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

    private void RecursiveCheckNotes(bool initial, bool natural) {
        if (nextNoteIndex >= notesContainer.LoadedContainers.Count) return;
        if ((curNoteTime + offset) > notesContainer.LoadedContainers[nextNoteIndex].objectData._time) {
            if (natural && NotePassedThreshold != null) NotePassedThreshold.Invoke(initial, nextNoteIndex, notesContainer.LoadedContainers[nextNoteIndex].objectData);
            nextNoteIndex++;
            RecursiveCheckNotes(false, natural);
        }
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        List<BeatmapObjectContainer> passed = new List<BeatmapObjectContainer>(nextEvents.Where(x => x.objectData._time <= curNoteTime + offset));
        foreach (BeatmapObjectContainer newlyAdded in passed)
        {
            if (natural && EventPassedThreshold != null) EventPassedThreshold.Invoke(init, nextEventIndex, newlyAdded.objectData);
            nextEvents.Remove(newlyAdded);
            if (allEvents.Any() && natural) nextEvents.Add(allEvents.Dequeue());
            nextEventIndex++;
        }
    }
}

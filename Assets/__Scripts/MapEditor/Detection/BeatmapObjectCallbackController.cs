using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

//Name and idea totally not stolen directly from Beat Saber
public class BeatmapObjectCallbackController : MonoBehaviour
{
    private static readonly int eventsToLookAhead = 75;
    private static readonly int notesToLookAhead = 25;

    [SerializeField] private NotesContainer notesContainer;
    [SerializeField] private EventsContainer eventsContainer;

    [SerializeField] private AudioTimeSyncController timeSyncController;
    [SerializeField] private UIMode uiMode;

    [SerializeField] private bool useOffsetFromConfig = true;

    [Tooltip("Whether or not to use the Despawn or Spawn offset from settings.")]
    [SerializeField]
    private bool useDespawnOffset;

    [FormerlySerializedAs("offset")] public float Offset;

    [SerializeField] private int nextNoteIndex;
    [SerializeField] private int nextEventIndex;

    [FormerlySerializedAs("useAudioTime")] public bool UseAudioTime;

    private readonly List<BeatmapObject> nextEvents = new List<BeatmapObject>();
    private readonly List<BeatmapObject> nextNotes = new List<BeatmapObject>();
    private Queue<BeatmapObject> allEvents = new Queue<BeatmapObject>();
    private Queue<BeatmapObject> allNotes = new Queue<BeatmapObject>();
    private float curTime;
    public Action<bool, int, BeatmapObject> EventPassedThreshold;

    public Action<bool, int, BeatmapObject> NotePassedThreshold;
    public Action<bool, int> RecursiveEventCheckFinished;
    public Action<bool, int> RecursiveNoteCheckFinished;

    private void Start()
    {
        notesContainer.ObjectSpawnedEvent += NotesContainer_ObjectSpawnedEvent;
        notesContainer.ObjectDeletedEvent += NotesContainer_ObjectDeletedEvent;
        eventsContainer.ObjectSpawnedEvent += EventsContainer_ObjectSpawnedEvent;
        eventsContainer.ObjectDeletedEvent += EventsContainer_ObjectDeletedEvent;
    }

    private void LateUpdate()
    {
        if (useOffsetFromConfig)
        {
            if (UIMode.SelectedMode == UIModeType.Playing || UIMode.SelectedMode == UIModeType.Preview)
            {
                if (useDespawnOffset)
                {
                    Offset = 0;
                }
                else
                {

                    var songNoteJumpSpeed = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
                    var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                    var num = 60f / bpm;
                    var halfJumpDuration = 4;

                    while (songNoteJumpSpeed * num * halfJumpDuration > 18)
                        halfJumpDuration /= 2;

                    Offset = num * halfJumpDuration * 2;
                }
            }
            else
            {
                Offset = useDespawnOffset
                    ? Settings.Instance.Offset_Despawning * -1
                    : Settings.Instance.Offset_Spawning;
            }
        }

        if (timeSyncController.IsPlaying)
        {
            curTime = UseAudioTime ? timeSyncController.CurrentSongBeats : timeSyncController.CurrentBeat;
            RecursiveCheckNotes(true, true);
            RecursiveCheckEvents(true, true);
        }
    }

    private void OnEnable() => timeSyncController.PlayToggle += OnPlayToggle;

    private void OnDisable() => timeSyncController.PlayToggle -= OnPlayToggle;

    private void OnPlayToggle(bool playing)
    {
        if (playing)
        {
            CheckAllNotes(false);
            CheckAllEvents(false);
        }
    }

    private void CheckAllNotes(bool natural)
    {
        //notesContainer.SortObjects();
        curTime = UseAudioTime ? timeSyncController.CurrentSongBeats : timeSyncController.CurrentBeat;
        allNotes.Clear();
        allNotes = new Queue<BeatmapObject>(notesContainer.LoadedObjects);
        while (allNotes.Count > 0 && allNotes.Peek().Time < curTime + Offset) allNotes.Dequeue();
        nextNoteIndex = notesContainer.LoadedObjects.Count - allNotes.Count;
        RecursiveNoteCheckFinished?.Invoke(natural, nextNoteIndex - 1);
        nextNotes.Clear();
        for (var i = 0; i < notesToLookAhead; i++)
        {
            if (allNotes.Any())
                nextNotes.Add(allNotes.Dequeue());
        }
    }

    private void CheckAllEvents(bool natural)
    {
        allEvents.Clear();
        allEvents = new Queue<BeatmapObject>(eventsContainer.LoadedObjects);
        while (allEvents.Count > 0 && allEvents.Peek().Time < curTime + Offset) allEvents.Dequeue();
        nextEventIndex = eventsContainer.LoadedObjects.Count - allEvents.Count;
        RecursiveEventCheckFinished?.Invoke(natural, nextEventIndex - 1);
        nextEvents.Clear();
        for (var i = 0; i < eventsToLookAhead; i++)
        {
            if (allEvents.Any())
                nextEvents.Add(allEvents.Dequeue());
        }
    }

    private void RecursiveCheckNotes(bool init, bool natural)
    {
        var passed = nextNotes.FindAll(x => x.Time <= curTime + Offset);
        foreach (var newlyAdded in passed)
        {
            if (natural) NotePassedThreshold?.Invoke(init, nextNoteIndex, newlyAdded);
            nextNotes.Remove(newlyAdded);
            if (allNotes.Any() && natural) nextNotes.Add(allNotes.Dequeue());
            nextNoteIndex++;
        }
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        var passed = nextEvents.FindAll(x => x.Time <= curTime + Offset);
        foreach (var newlyAdded in passed)
        {
            if (natural) EventPassedThreshold?.Invoke(init, nextEventIndex, newlyAdded);
            nextEvents.Remove(newlyAdded);
            if (allEvents.Any() && natural) nextEvents.Add(allEvents.Dequeue());
            nextEventIndex++;
        }
    }

    private void NotesContainer_ObjectSpawnedEvent(BeatmapObject obj) => OnObjSpawn(obj, nextNotes);

    private void NotesContainer_ObjectDeletedEvent(BeatmapObject obj) => OnObjDeleted(obj, nextNotes, allNotes);

    private void EventsContainer_ObjectSpawnedEvent(BeatmapObject obj) => OnObjSpawn(obj, nextEvents);

    private void EventsContainer_ObjectDeletedEvent(BeatmapObject obj) => OnObjDeleted(obj, nextEvents, allEvents);

    private void OnObjSpawn(BeatmapObject obj, List<BeatmapObject> nextObjects)
    {
        if (!timeSyncController.IsPlaying) return;

        if (obj.Time >= timeSyncController.CurrentBeat)
        {
            nextObjects.Add(obj);
        }
    }

    private void OnObjDeleted(BeatmapObject obj, List<BeatmapObject> nextObjects, Queue<BeatmapObject> allObjects)
    {
        if (!timeSyncController.IsPlaying) return;

        if (obj.Time >= timeSyncController.CurrentBeat)
        {
            nextObjects.Remove(obj);

            if (allObjects.Count > 0)
            {
                // BS way of removing one singular object from a queue but I guess it's the best we've got
                // (without allowcating a new queue from a LINQ statement)
                var firstObj = allObjects.Peek();

                do
                {
                    var curObj = allObjects.Dequeue();

                    if (curObj != obj)
                    {
                        allObjects.Enqueue(curObj);
                    }
                } while (allObjects.Peek() != firstObj);
            }
        }
    }

    private void OnDestroy()
    {
        notesContainer.ObjectSpawnedEvent -= NotesContainer_ObjectSpawnedEvent;
        notesContainer.ObjectDeletedEvent -= NotesContainer_ObjectDeletedEvent;
        eventsContainer.ObjectSpawnedEvent -= EventsContainer_ObjectSpawnedEvent;
        eventsContainer.ObjectDeletedEvent -= EventsContainer_ObjectDeletedEvent;
    }
}

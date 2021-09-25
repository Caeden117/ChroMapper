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
}

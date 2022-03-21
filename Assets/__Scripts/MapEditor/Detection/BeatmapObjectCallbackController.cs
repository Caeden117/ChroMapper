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

    private readonly HashSet<BeatmapObject> nextEvents = new HashSet<BeatmapObject>();
    private readonly HashSet<BeatmapObject> nextNotes = new HashSet<BeatmapObject>();
    private HashSet<BeatmapObject> allEvents = new HashSet<BeatmapObject>();
    private HashSet<BeatmapObject> allNotes = new HashSet<BeatmapObject>();
    private HashSet<BeatmapObject> queuedToClear = new HashSet<BeatmapObject>();

    private float curTime;
    public Action<bool, int, BeatmapObject> EventPassedThreshold;

    public Action<bool, int, BeatmapObject> NotePassedThreshold;
    public Action<bool, int> RecursiveEventCheckFinished;
    public Action<bool, int> RecursiveNoteCheckFinished;

    /// v3 version fields
    [SerializeField] private ChainsContainer chainsContainer; // Now it should be useless. Does chain have sound?
    [SerializeField] private int nextChainIndex;
    private readonly HashSet<BeatmapObject> nextChains = new HashSet<BeatmapObject>();
    private HashSet<BeatmapObject> allChains = new HashSet<BeatmapObject>();
    public Action<bool, int, BeatmapObject> ChainPassedThreshold;
    public Action<bool, int> RecursiveChainCheckFinished;

    private void Start()
    {
        notesContainer.ObjectSpawnedEvent += NotesContainer_ObjectSpawnedEvent;
        notesContainer.ObjectDeletedEvent += NotesContainer_ObjectDeletedEvent;
        eventsContainer.ObjectSpawnedEvent += EventsContainer_ObjectSpawnedEvent;
        eventsContainer.ObjectDeletedEvent += EventsContainer_ObjectDeletedEvent;
        if (Settings.Instance.Load_MapV3 && chainsContainer != null)
        {
            chainsContainer.ObjectSpawnedEvent += ChainsContainer_ObjectSpawnedEvent;
            chainsContainer.ObjectDeletedEvent += ChainsContainer_ObjectDeletedEvent;
        }
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
                    var songStartBeatOffset = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;
                    var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                    Offset = SpawnParameterHelper.CalculateHalfJumpDuration(songNoteJumpSpeed, songStartBeatOffset, bpm);
                }
            }
            else
            {
                Offset = useDespawnOffset
                    ? Settings.Instance.Offset_Despawning * -1
                    : Settings.Instance.Offset_Spawning;
            }

            if (!useDespawnOffset) Shader.SetGlobalFloat("_ObstacleFadeRadius", Offset * EditorScaleController.EditorScale);
        }

        if (queuedToClear.Count > 0)
        {
            foreach (var toClear in queuedToClear)
            {
                if (toClear is BeatmapNote)
                {
                    allNotes.Remove(toClear);
                    nextNotes.Remove(toClear);
                }
                else if (toClear is MapEvent)
                {
                    allEvents.Remove(toClear);
                    nextEvents.Remove(toClear);
                }
                else if (Settings.Instance.Load_MapV3)
                {
                    if (toClear is BeatmapChain)
                    {
                        allChains.Remove(toClear);
                        nextChains.Remove(toClear);
                    }
                }
            }

            queuedToClear.Clear();
        }

        if (timeSyncController.IsPlaying)
        {
            curTime = UseAudioTime ? timeSyncController.CurrentSongBeats : timeSyncController.CurrentBeat;
            RecursiveCheckNotes(true, true);
            RecursiveCheckEvents(true, true);
            if (Settings.Instance.Load_MapV3 && chainsContainer != null)
            {
                RecursiveCheckChains(true, true);
            }
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
            if (Settings.Instance.Load_MapV3 && chainsContainer != null) CheckAllChains(false);
        }
    }

    private void CheckAllNotes(bool natural)
    {
        //notesContainer.SortObjects();
        curTime = UseAudioTime ? timeSyncController.CurrentSongBeats : timeSyncController.CurrentBeat;
        allNotes.Clear();
        allNotes = new HashSet<BeatmapObject>(notesContainer.LoadedObjects.Where(x => x.Time >= curTime + Offset));
        nextNoteIndex = notesContainer.LoadedObjects.Count - allNotes.Count;
        RecursiveNoteCheckFinished?.Invoke(natural, nextNoteIndex - 1);
        nextNotes.Clear();

        for (var i = 0; i < notesToLookAhead; i++)
        {
            if (allNotes.Count > 0) QueueNextObject(allNotes, nextNotes);
        }
    }

    private void CheckAllEvents(bool natural)
    {
        allEvents.Clear();
        allEvents = new HashSet<BeatmapObject>(eventsContainer.LoadedObjects.Where(x => x.Time >= curTime + Offset));

        nextEventIndex = eventsContainer.LoadedObjects.Count - allEvents.Count;
        RecursiveEventCheckFinished?.Invoke(natural, nextEventIndex - 1);
        nextEvents.Clear();

        for (var i = 0; i < eventsToLookAhead; i++)
        {
            if (allEvents.Count > 0) QueueNextObject(allEvents, nextEvents);
        }
    }

    private void CheckAllChains(bool natural)
    {
        if (chainsContainer == null) return; // currently only use for sound effect
        curTime = UseAudioTime ? timeSyncController.CurrentSongBeats : timeSyncController.CurrentBeat;
        allChains.Clear();
        allChains = new HashSet<BeatmapObject>(chainsContainer.LoadedObjects.Where(x => x.Time >= curTime + Offset));
        nextChainIndex = chainsContainer.LoadedObjects.Count - allChains.Count;
        RecursiveChainCheckFinished?.Invoke(natural, nextChainIndex - 1);
        nextChains.Clear();

        for (var i = 0; i < notesToLookAhead; i++)
        {
            if (allChains.Count > 0) QueueNextObject(allChains, nextChains);
        }
    }

    private void RecursiveCheckNotes(bool init, bool natural)
    {
        var passed = nextNotes.Where(x => x.Time <= curTime + Offset).ToArray();
        foreach (var newlyAdded in passed)
        {
            if (natural) NotePassedThreshold?.Invoke(init, nextNoteIndex, newlyAdded);
            nextNotes.Remove(newlyAdded);
            if (allNotes.Count > 0 && natural) QueueNextObject(allNotes, nextNotes);
            nextNoteIndex++;
        }
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        var passed = nextEvents.Where(x => x.Time <= curTime + Offset).ToArray();
        foreach (var newlyAdded in passed)
        {
            if (natural) EventPassedThreshold?.Invoke(init, nextEventIndex, newlyAdded);
            nextEvents.Remove(newlyAdded);
            if (allEvents.Count > 0 && natural) QueueNextObject(allEvents, nextEvents);
            nextEventIndex++;
        }
    }

    private void RecursiveCheckChains(bool init, bool natural)
    {
        if (chainsContainer == null) return; // currently only use for sound effect
        var passed = nextChains.Where(x => x.Time <= curTime + Offset).ToArray();
        foreach (var newlyAdded in passed)
        {
            if (natural) ChainPassedThreshold?.Invoke(init, nextChainIndex, newlyAdded);
            nextChains.Remove(newlyAdded);
            if (allChains.Count > 0 && natural) QueueNextObject(allChains, nextChains);
            nextChainIndex++;
        }
    }

    private void NotesContainer_ObjectSpawnedEvent(BeatmapObject obj) => OnObjSpawn(obj, nextNotes);

    private void NotesContainer_ObjectDeletedEvent(BeatmapObject obj) => OnObjDeleted(obj);

    private void EventsContainer_ObjectSpawnedEvent(BeatmapObject obj) => OnObjSpawn(obj, nextEvents);

    private void EventsContainer_ObjectDeletedEvent(BeatmapObject obj) => OnObjDeleted(obj);

    private void ChainsContainer_ObjectSpawnedEvent(BeatmapObject obj) => OnObjSpawn(obj, nextChains);

    private void ChainsContainer_ObjectDeletedEvent(BeatmapObject obj) => OnObjDeleted(obj);

    private void OnObjSpawn(BeatmapObject obj, HashSet<BeatmapObject> nextObjects)
    {
        if (!timeSyncController.IsPlaying) return;

        if (obj.Time >= timeSyncController.CurrentBeat)
        {
            nextObjects.Add(obj);
        }
    }

    private void OnObjDeleted(BeatmapObject obj)
    {
        if (!timeSyncController.IsPlaying) return;

        if (obj.Time >= timeSyncController.CurrentBeat)
        {
            queuedToClear.Add(obj);
        }
    }

    private void QueueNextObject(HashSet<BeatmapObject> allObjs, HashSet<BeatmapObject> nextObjs)
    {
        // Assumes that the "Count > 0" check happens before this is called
        var first = allObjs.First();
        nextObjs.Add(first);
        allObjs.Remove(first);
    }

    private void OnDestroy()
    {
        notesContainer.ObjectSpawnedEvent -= NotesContainer_ObjectSpawnedEvent;
        notesContainer.ObjectDeletedEvent -= NotesContainer_ObjectDeletedEvent;
        eventsContainer.ObjectSpawnedEvent -= EventsContainer_ObjectSpawnedEvent;
        eventsContainer.ObjectDeletedEvent -= EventsContainer_ObjectDeletedEvent;
        if (Settings.Instance.Load_MapV3 && chainsContainer != null)
        {
            chainsContainer.ObjectSpawnedEvent -= ChainsContainer_ObjectSpawnedEvent;
            chainsContainer.ObjectDeletedEvent -= ChainsContainer_ObjectDeletedEvent;

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

//Name and idea totally not stolen directly from Beat Saber
public class BeatmapObjectCallbackController : MonoBehaviour
{
    private static readonly int eventsToLookAhead = 75;
    private static readonly int notesToLookAhead = 25;

    [FormerlySerializedAs("notesContainer")] [SerializeField] private NoteGridContainer noteGridContainer;
    [FormerlySerializedAs("eventsContainer")] [SerializeField] private EventGridContainer eventGridContainer;

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

    private readonly HashSet<IObject> nextEvents = new HashSet<IObject>();
    private readonly HashSet<IObject> nextNotes = new HashSet<IObject>();
    private HashSet<IObject> allEvents = new HashSet<IObject>();
    private HashSet<IObject> allNotes = new HashSet<IObject>();
    private HashSet<IObject> queuedToClear = new HashSet<IObject>();

    private float curTime;
    public Action<bool, int, IObject> EventPassedThreshold;

    public Action<bool, int, IObject> NotePassedThreshold;
    public Action<bool, int> RecursiveEventCheckFinished;
    public Action<bool, int> RecursiveNoteCheckFinished;

    /// v3 version fields
    [FormerlySerializedAs("chainsContainer")] [SerializeField] private ChainGridContainer chainGridContainer; // Now it should be useless. Does chain have sound?
    [SerializeField] private int nextChainIndex;
    private readonly HashSet<IObject> nextChains = new HashSet<IObject>();
    private HashSet<IObject> allChains = new HashSet<IObject>();
    public Action<bool, int, IObject> ChainPassedThreshold;
    public Action<bool, int> RecursiveChainCheckFinished;

    private void Start()
    {
        noteGridContainer.ObjectSpawnedEvent += NoteGridContainerObjectSpawnedEvent;
        noteGridContainer.ObjectDeletedEvent += NoteGridContainerObjectDeletedEvent;
        eventGridContainer.ObjectSpawnedEvent += EventGridContainerObjectSpawnedEventGrid;
        eventGridContainer.ObjectDeletedEvent += EventGridContainerObjectDeletedEventGrid;
        if (Settings.Instance.Load_MapV3 && chainGridContainer != null)
        {
            chainGridContainer.ObjectSpawnedEvent += ChainGridContainerObjectSpawnedEvent;
            chainGridContainer.ObjectDeletedEvent += ChainGridContainerObjectDeletedEvent;
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
                if (toClear is INote)
                {
                    allNotes.Remove(toClear);
                    nextNotes.Remove(toClear);
                }
                else if (toClear is IEvent)
                {
                    allEvents.Remove(toClear);
                    nextEvents.Remove(toClear);
                }
                else if (Settings.Instance.Load_MapV3)
                {
                    if (toClear is IChain)
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
            if (Settings.Instance.Load_MapV3 && chainGridContainer != null)
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
            if (Settings.Instance.Load_MapV3 && chainGridContainer != null) CheckAllChains(false);
        }
    }

    private void CheckAllNotes(bool natural)
    {
        //notesContainer.SortObjects();
        curTime = UseAudioTime ? timeSyncController.CurrentSongBeats : timeSyncController.CurrentBeat;
        allNotes.Clear();
        allNotes = new HashSet<IObject>(noteGridContainer.LoadedObjects.Where(x => x.Time >= curTime + Offset));
        nextNoteIndex = noteGridContainer.LoadedObjects.Count - allNotes.Count;
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
        allEvents = new HashSet<IObject>(eventGridContainer.LoadedObjects.Where(x => x.Time >= curTime + Offset));

        nextEventIndex = eventGridContainer.LoadedObjects.Count - allEvents.Count;
        RecursiveEventCheckFinished?.Invoke(natural, nextEventIndex - 1);
        nextEvents.Clear();

        for (var i = 0; i < eventsToLookAhead; i++)
        {
            if (allEvents.Count > 0) QueueNextObject(allEvents, nextEvents);
        }
    }

    private void CheckAllChains(bool natural)
    {
        if (chainGridContainer == null) return; // currently only use for sound effect
        curTime = UseAudioTime ? timeSyncController.CurrentSongBeats : timeSyncController.CurrentBeat;
        allChains.Clear();
        allChains = new HashSet<IObject>(chainGridContainer.LoadedObjects.Where(x => x.Time >= curTime + Offset));
        nextChainIndex = chainGridContainer.LoadedObjects.Count - allChains.Count;
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
        if (chainGridContainer == null) return; // currently only use for sound effect
        var passed = nextChains.Where(x => x.Time <= curTime + Offset).ToArray();
        foreach (var newlyAdded in passed)
        {
            if (natural) ChainPassedThreshold?.Invoke(init, nextChainIndex, newlyAdded);
            nextChains.Remove(newlyAdded);
            if (allChains.Count > 0 && natural) QueueNextObject(allChains, nextChains);
            nextChainIndex++;
        }
    }

    private void NoteGridContainerObjectSpawnedEvent(IObject obj) => OnObjSpawn(obj, nextNotes);

    private void NoteGridContainerObjectDeletedEvent(IObject obj) => OnObjDeleted(obj);

    private void EventGridContainerObjectSpawnedEventGrid(IObject obj) => OnObjSpawn(obj, nextEvents);

    private void EventGridContainerObjectDeletedEventGrid(IObject obj) => OnObjDeleted(obj);

    private void ChainGridContainerObjectSpawnedEvent(IObject obj) => OnObjSpawn(obj, nextChains);

    private void ChainGridContainerObjectDeletedEvent(IObject obj) => OnObjDeleted(obj);

    private void OnObjSpawn(IObject obj, HashSet<IObject> nextObjects)
    {
        if (!timeSyncController.IsPlaying) return;

        if (obj.Time >= timeSyncController.CurrentBeat)
        {
            nextObjects.Add(obj);
        }
    }

    private void OnObjDeleted(IObject obj)
    {
        if (!timeSyncController.IsPlaying) return;

        if (obj.Time >= timeSyncController.CurrentBeat)
        {
            queuedToClear.Add(obj);
        }
    }

    private void QueueNextObject(HashSet<IObject> allObjs, HashSet<IObject> nextObjs)
    {
        // Assumes that the "Count > 0" check happens before this is called
        var first = allObjs.First();
        nextObjs.Add(first);
        allObjs.Remove(first);
    }

    private void OnDestroy()
    {
        noteGridContainer.ObjectSpawnedEvent -= NoteGridContainerObjectSpawnedEvent;
        noteGridContainer.ObjectDeletedEvent -= NoteGridContainerObjectDeletedEvent;
        eventGridContainer.ObjectSpawnedEvent -= EventGridContainerObjectSpawnedEventGrid;
        eventGridContainer.ObjectDeletedEvent -= EventGridContainerObjectDeletedEventGrid;
        if (Settings.Instance.Load_MapV3 && chainGridContainer != null)
        {
            chainGridContainer.ObjectSpawnedEvent -= ChainGridContainerObjectSpawnedEvent;
            chainGridContainer.ObjectDeletedEvent -= ChainGridContainerObjectDeletedEvent;

        }
    }
}

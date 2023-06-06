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

    [FormerlySerializedAs("notesContainer")][SerializeField] private NoteGridContainer noteGridContainer;
    [FormerlySerializedAs("eventsContainer")][SerializeField] private EventGridContainer eventGridContainer;

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

    private readonly HashSet<BaseObject> nextEvents = new HashSet<BaseObject>();
    private readonly HashSet<BaseObject> nextNotes = new HashSet<BaseObject>();
    private HashSet<BaseObject> allEvents = new HashSet<BaseObject>();
    private HashSet<BaseObject> allNotes = new HashSet<BaseObject>();
    private HashSet<BaseObject> queuedToClear = new HashSet<BaseObject>();

    private float curTime;
    public Action<bool, int, BaseObject> EventPassedThreshold;

    public Action<bool, int, BaseObject> NotePassedThreshold;
    public Action<bool, int> RecursiveEventCheckFinished;
    public Action<bool, int> RecursiveNoteCheckFinished;

    /// v3 version fields
    [FormerlySerializedAs("chainsContainer")][SerializeField] private ChainGridContainer chainGridContainer;
    [SerializeField] private int nextChainIndex;
    private readonly HashSet<BaseObject> nextChains = new HashSet<BaseObject>();
    private HashSet<BaseObject> allChains = new HashSet<BaseObject>();
    public Action<bool, int, BaseObject> ChainPassedThreshold;
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
                if (toClear is BaseNote)
                {
                    allNotes.Remove(toClear);
                    nextNotes.Remove(toClear);
                }
                else if (toClear is BaseEvent)
                {
                    allEvents.Remove(toClear);
                    nextEvents.Remove(toClear);
                }
                else if (Settings.Instance.Load_MapV3)
                {
                    if (toClear is BaseChain)
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
            curTime = UseAudioTime ? timeSyncController.CurrentAudioBeats : timeSyncController.CurrentSongBpmTime;
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
        curTime = UseAudioTime ? timeSyncController.CurrentAudioBeats : timeSyncController.CurrentSongBpmTime;
        allNotes.Clear();
        allNotes = new HashSet<BaseObject>(noteGridContainer.LoadedObjects.Where(x => x.SongBpmTime >= curTime + Offset));
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
        allEvents = new HashSet<BaseObject>(eventGridContainer.LoadedObjects.Where(x => x.SongBpmTime >= curTime + Offset));

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
        curTime = UseAudioTime ? timeSyncController.CurrentAudioBeats : timeSyncController.CurrentSongBpmTime;
        allChains.Clear();
        allChains = new HashSet<BaseObject>(chainGridContainer.LoadedObjects.Where(x => x.SongBpmTime >= curTime + Offset));
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
        var realOffsets = useOffsetFromConfig && !useDespawnOffset && (UIMode.SelectedMode == UIModeType.Playing || UIMode.SelectedMode == UIModeType.Preview);
        var passed = nextNotes.Where(x => x.SongBpmTime <= curTime + (realOffsets ? (x as BaseGrid).Hjd : Offset)).ToArray();
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
        var passed = nextEvents.Where(x => x.SongBpmTime <= curTime + Offset).ToArray();
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
        var passed = nextChains.Where(x => (x as BaseChain).TailSongBpmTime <= curTime + Offset).ToArray();
        foreach (var newlyAdded in passed)
        {
            if (natural) ChainPassedThreshold?.Invoke(init, nextChainIndex, newlyAdded);
            nextChains.Remove(newlyAdded);
            if (allChains.Count > 0 && natural) QueueNextObject(allChains, nextChains);
            nextChainIndex++;
        }
    }

    private void NoteGridContainerObjectSpawnedEvent(BaseObject obj) => OnObjSpawn(obj, nextNotes);

    private void NoteGridContainerObjectDeletedEvent(BaseObject obj) => OnObjDeleted(obj);

    private void EventGridContainerObjectSpawnedEventGrid(BaseObject obj) => OnObjSpawn(obj, nextEvents);

    private void EventGridContainerObjectDeletedEventGrid(BaseObject obj) => OnObjDeleted(obj);

    private void ChainGridContainerObjectSpawnedEvent(BaseObject obj) => OnObjSpawn(obj, nextChains);

    private void ChainGridContainerObjectDeletedEvent(BaseObject obj) => OnChainObjDeleted(obj);

    private void OnObjSpawn(BaseObject obj, HashSet<BaseObject> nextObjects)
    {
        if (!timeSyncController.IsPlaying) return;

        if (obj.SongBpmTime >= timeSyncController.CurrentSongBpmTime)
        {
            nextObjects.Add(obj);
        }
    }

    private void OnObjDeleted(BaseObject obj)
    {
        if (!timeSyncController.IsPlaying) return;

        if (obj.SongBpmTime >= timeSyncController.CurrentSongBpmTime)
        {
            queuedToClear.Add(obj);
        }
    }

    private void OnChainObjDeleted(BaseObject obj)
    {
        if (!timeSyncController.IsPlaying) return;

        if ((obj as BaseChain).TailSongBpmTime >= timeSyncController.CurrentSongBpmTime)
        {
            queuedToClear.Add(obj);
        }
    }

    private void QueueNextObject(HashSet<BaseObject> allObjs, HashSet<BaseObject> nextObjs)
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

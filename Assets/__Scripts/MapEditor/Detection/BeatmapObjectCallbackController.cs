using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.V2;
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
    [SerializeField] private int nextChainIndex;

    [FormerlySerializedAs("useAudioTime")] public bool UseAudioTime;

    private float curTime;

    public Action<bool, int, BaseObject> NotePassedThreshold;
    public Action<bool, int> RecursiveNoteCheckFinished;
    public Action<bool, int, BaseObject> EventPassedThreshold;
    public Action<bool, int> RecursiveEventCheckFinished;
    public Action<bool, int, BaseObject> ChainPassedThreshold;
    public Action<bool, int> RecursiveChainCheckFinished;

    /// v3 version fields
    [FormerlySerializedAs("chainsContainer")][SerializeField] private ChainGridContainer chainGridContainer;

    private static readonly int obstacleFadeRadius = Shader.PropertyToID("_ObstacleFadeRadius");

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
            if (UIMode.SelectedMode is UIModeType.Playing or UIModeType.Preview)
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

            if (!useDespawnOffset) Shader.SetGlobalFloat(obstacleFadeRadius, Offset * EditorScaleController.EditorScale);
        }

        if (timeSyncController.IsPlaying)
        {
            curTime = UseAudioTime ? timeSyncController.CurrentAudioBeats : timeSyncController.CurrentSongBpmTime;
            RecursiveCheckNotes(true, true);
            RecursiveCheckEvents(true, true);

            if (chainGridContainer != null)
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

            if (chainGridContainer != null)
            {
                CheckAllChains(false);
            }
        }
    }

    private void CheckAllNotes(bool natural)
    {
        nextNoteIndex = noteGridContainer.MapObjects.BinarySearchBy(timeSyncController.CurrentJsonTime, obj => obj.JsonTime);
        if (nextNoteIndex < 0) nextNoteIndex = ~nextNoteIndex;

        RecursiveNoteCheckFinished?.Invoke(natural, nextNoteIndex - 1);
    }

    private void CheckAllEvents(bool natural)
    {
        nextEventIndex = eventGridContainer.MapObjects.BinarySearchBy(timeSyncController.CurrentJsonTime, obj => obj.JsonTime);
        if (nextEventIndex < 0) nextEventIndex = ~nextEventIndex;

        RecursiveEventCheckFinished?.Invoke(natural, nextEventIndex - 1);
    }

    private void CheckAllChains(bool natural)
    {
        nextChainIndex = chainGridContainer.MapObjects.BinarySearchBy(timeSyncController.CurrentJsonTime, obj => obj.JsonTime);
        if (nextChainIndex < 0) nextChainIndex = ~nextChainIndex;

        RecursiveChainCheckFinished?.Invoke(natural, nextChainIndex - 1);
    }

    private void RecursiveCheckNotes(bool init, bool natural)
    {
        var objects = noteGridContainer.MapObjects;
        var useAnimationsOffset = useOffsetFromConfig && !useDespawnOffset && UIMode.AnimationMode;
        while (nextNoteIndex < objects.Count)
        {
            var obj = objects[nextNoteIndex];
            var offset = useAnimationsOffset ? obj.Hjd + Track.JUMP_TIME : Offset;

            if (obj.SongBpmTime > curTime + offset) return;

            if (BeatmapObjectContainerCollection.TrackFilterID == null || BeatmapObjectContainerCollection.TrackFilterID == ((obj.CustomTrack as SimpleJSON.JSONString)?.Value ?? ""))
                NotePassedThreshold?.Invoke(natural, nextNoteIndex, obj);

            nextNoteIndex++;
        }
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        var objects = eventGridContainer.MapObjects;
        while (nextEventIndex < objects.Count)
        {
            var obj = objects[nextEventIndex];

            if (obj.SongBpmTime > curTime + Offset) return;

            EventPassedThreshold?.Invoke(natural, nextEventIndex, obj);
            nextEventIndex++;
        }
    }

    private void RecursiveCheckChains(bool init, bool natural)
    {
        var objects = chainGridContainer.MapObjects;
        var useAnimationsOffset = useOffsetFromConfig && !useDespawnOffset && UIMode.AnimationMode;
        while (nextChainIndex < objects.Count)
        {
            var obj = objects[nextChainIndex];
            var offset = useAnimationsOffset ? obj.Hjd + Track.JUMP_TIME : Offset;

            if (obj.SongBpmTime > curTime + offset) return;

            ChainPassedThreshold?.Invoke(natural, nextChainIndex, obj);
            nextChainIndex++;
        }
    }

    private void NoteGridContainerObjectSpawnedEvent(BaseObject obj) => OnObjSpawn(obj, ref nextNoteIndex);

    private void NoteGridContainerObjectDeletedEvent(BaseObject obj) => OnObjDeleted(obj, ref nextNoteIndex);

    private void EventGridContainerObjectSpawnedEventGrid(BaseObject obj) => OnObjSpawn(obj, ref nextEventIndex);

    private void EventGridContainerObjectDeletedEventGrid(BaseObject obj) => OnObjDeleted(obj, ref nextEventIndex);

    private void ChainGridContainerObjectSpawnedEvent(BaseObject obj) => OnObjSpawn(obj, ref nextChainIndex);

    private void ChainGridContainerObjectDeletedEvent(BaseObject obj) => OnObjDeleted(obj, ref nextChainIndex);

    private void OnObjSpawn(BaseObject obj, ref int idx)
    {
        if (!timeSyncController.IsPlaying || obj.SongBpmTime >= curTime + Offset) return;

        idx++;
    }

    private void OnObjDeleted(BaseObject obj, ref int idx)
    {
        if (!timeSyncController.IsPlaying || obj.SongBpmTime >= curTime + Offset) return;

        idx--;
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

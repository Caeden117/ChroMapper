using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// This is a template for callback controller, to avoid filling various BeatmapObjects into one class
/// </summary>
public class MetaBeatmapObjectCallbackController<TBo, TBocc> : MonoBehaviour
    where TBo: BeatmapObject
    where TBocc: BeatmapObjectContainerCollection
{
    [SerializeField] protected int ObjectsLookAhead = 75;
    [SerializeField] protected TBocc ObjectContainers;

    [SerializeField] protected AudioTimeSyncController Atsc;

    [SerializeField] protected bool UseOffsetFromConfig = true;

    [Tooltip("Whether or not to use the Despawn or Spawn offset from settings.")]
    [SerializeField]
    protected bool UseDespawnOffset;

    [FormerlySerializedAs("offset")] public float Offset;

    [SerializeField] protected int NextObjectIndex;

    [FormerlySerializedAs("useAudioTime")] public bool UseAudioTime;

    protected readonly HashSet<TBo> NextObjects = new HashSet<TBo>();
    protected HashSet<TBo> AllObjects = new HashSet<TBo>();
    protected HashSet<TBo> QueuedToClear = new HashSet<TBo>();

    private float curTime;
    public Action<bool, int, TBo> ObjectPassedThreshold;
    public Action<bool, int> RecursiveObjectCheckFinished;

    protected void Awake()
    {
        if (!CheckLoadingPrerequisite()) Destroy(this);
    }

    /// <summary>
    /// overide this function to decide whether to load this script or not.
    /// </summary>
    /// <returns>if false, Destroy this script on Awake</returns>
    protected virtual bool CheckLoadingPrerequisite()
    {
        return true;
    }

    // Start is called before the first frame update
    protected void Start()
    {
        ObjectContainers.ObjectSpawnedEvent += OnObjectSpawned;
        ObjectContainers.ObjectDeletedEvent += OnObjectDeleted;
    }

    protected void OnDestroy()
    {
        ObjectContainers.ObjectSpawnedEvent -= OnObjectSpawned;
        ObjectContainers.ObjectDeletedEvent -= OnObjectDeleted;
    }

    protected void LateUpdate()
    {
        if (UseOffsetFromConfig)
        {
            if (UIMode.SelectedMode == UIModeType.Playing || UIMode.SelectedMode == UIModeType.Preview)
            {
                if (UseDespawnOffset)
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
                Offset = UseDespawnOffset
                    ? Settings.Instance.Offset_Despawning * -1
                    : Settings.Instance.Offset_Spawning;
            }

            if (!UseDespawnOffset) Shader.SetGlobalFloat("_ObstacleFadeRadius", Offset * EditorScaleController.EditorScale);
        }

        if (QueuedToClear.Count > 0)
        {
            foreach (var toClear in QueuedToClear)
            {
                AllObjects.Remove(toClear);
                NextObjects.Remove(toClear);
            }
            QueuedToClear.Clear();
        }

        if (Atsc.IsPlaying)
        {
            curTime = UseAudioTime ? Atsc.CurrentSongBeats : Atsc.CurrentBeat;
            RecursiveCheckObjects(true, true);
        }
    }

    private void OnObjectSpawned(BeatmapObject obj)
    {
        if (!Atsc.IsPlaying) return;

        if (obj.Time >= Atsc.CurrentBeat)
        {
            NextObjects.Add(obj as TBo);
        }
    }

    private void OnObjectDeleted(BeatmapObject obj)
    {
        if (!Atsc.IsPlaying) return;

        if (obj.Time >= Atsc.CurrentBeat)
        {
            QueuedToClear.Add(obj as TBo);
        }
    }

    protected virtual void RecursiveCheckObjects(bool init, bool natural)
    {
        var passed = NextObjects.Where(x => x.Time <= curTime + Offset).ToArray();
        foreach (var newlyAdded in passed)
        {
            if (natural) ObjectPassedThreshold?.Invoke(init, NextObjectIndex, newlyAdded);
            NextObjects.Remove(newlyAdded);
            if (AllObjects.Count > 0 && natural) QueueNextObject(AllObjects, NextObjects);
            NextObjectIndex++;
        }
    }

    private void QueueNextObject(HashSet<TBo> allObjs, HashSet<TBo> nextObjs)
    {
        // Assumes that the "Count > 0" check happens before this is called
        var first = allObjs.First();
        nextObjs.Add(first);
        allObjs.Remove(first);
    }

    protected void OnEnable() => Atsc.PlayToggle += OnPlayToggle;

    protected void OnDisable() => Atsc.PlayToggle -= OnPlayToggle;
    private void OnPlayToggle(bool playing)
    {
        if (playing)
        {
            CheckAllObjects(false);
        }
    }

    protected virtual void CheckAllObjects(bool natural)
    {
        curTime = UseAudioTime ? Atsc.CurrentSongBeats : Atsc.CurrentBeat;
        AllObjects.Clear();
        AllObjects = new HashSet<TBo>(ObjectContainers.LoadedObjects.Where(x => x.Time >= curTime + Offset).Cast<TBo>());
        NextObjectIndex = ObjectContainers.LoadedObjects.Count - AllObjects.Count;
        RecursiveObjectCheckFinished?.Invoke(natural, NextObjectIndex - 1);
        NextObjects.Clear();

        for (var i = 0; i < ObjectsLookAhead; i++)
        {
            if (AllObjects.Count > 0) QueueNextObject(AllObjects, NextObjects);
        }
    }
}

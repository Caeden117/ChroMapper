using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BPMChangesContainer : BeatmapObjectContainerCollection
{

    //This is a shader-level restriction and nothing I can fix.
    public static readonly int ShaderArrayMaxSize = 1023; //Unity hard caps it here.

    private static readonly int Times = Shader.PropertyToID("_BPMChange_Times");
    private static readonly int BPMs = Shader.PropertyToID("_BPMChange_BPMs");
    private static readonly int BPMCount = Shader.PropertyToID("_BPMChange_Count");

    private static readonly float FirstVisibleBeatTime = 2;

    private static readonly float[] bpmShaderTimes = new float[ShaderArrayMaxSize];
    private static readonly float[] bpmShaderBPMs = new float[ShaderArrayMaxSize];

    [SerializeField] private Transform gridRendererParent;
    [SerializeField] private GameObject bpmPrefab;
    [SerializeField] private MeasureLinesController measureLinesController;
    [SerializeField] private CountersPlusController countersPlus;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.BPM_CHANGE;

    private IEnumerator Start()
    {
        if (BeatSaberSongContainer.Instance.difficultyData.customData == null) yield break;

        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);

        // TODO: Localize the big chunk of text
        if (BeatSaberSongContainer.Instance.difficultyData.customData?.HasKey("_editorOffset") == true &&
            BeatSaberSongContainer.Instance.difficultyData.customData["_editorOffset"] > 0f)
        {
            if (Settings.Instance.Reminder_UnsupportedEditorOffset)
            {
                PersistentUI.Instance.ShowDialogBox("ChroMapper has detected editor offset originating from MediocreMap Assistant 2.\n" +
                    "This is unsupported by ChroMapper. It is recommended to set up your audio to eliminate the need for any offset.\n" +
                    "However, ChroMapper can replace this offset with a BPM Change to keep the grid aligned.\n\n" +
                    "Would you like ChroMapper to do this?", CreateAutogeneratedBPMChange, "Yes", "Do This Automatically", "No");
            }
            else
            {
                CreateAutogeneratedBPMChange(1);
            }
        }
    }

    private void CreateAutogeneratedBPMChange(int res)
    {
        if (res == 2) return;
        Settings.Instance.Reminder_UnsupportedEditorOffset = res == 0;

        float offset = BeatSaberSongContainer.Instance.difficultyData.customData["_editorOffset"];

        BeatSaberSongContainer.Instance.difficultyData.customData.Remove("_editorOffset");
        BeatSaberSongContainer.Instance.difficultyData.customData.Remove("_editorOldOffset");

        BeatmapBPMChange autoGenerated = new BeatmapBPMChange(
            BeatSaberSongContainer.Instance.song.beatsPerMinute,
            AudioTimeSyncController.GetBeatFromSeconds(offset / 1000f));

        autoGenerated._customData = new SimpleJSON.JSONObject();
        autoGenerated._customData.Add("__note", "Autogenerated by ChroMapper");

        SpawnObject(autoGenerated, true, false);
        RefreshGridShaders();
        RefreshPool(true);
    }

    internal override void SubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent += EditorScaleChanged;
        LoadInitialMap.LevelLoadedEvent += RefreshGridShaders;
        AudioTimeSyncController.OnTimeChanged += OnTimeChanged;
    }

    private void EditorScaleChanged(float obj)
    {
        Shader.SetGlobalFloat("_EditorScale", EditorScaleController.EditorScale);
    }

    private void OnTimeChanged()
    {
        if (AudioTimeSyncController.IsPlaying) return;
        RefreshGridProperties();
    }

    internal override void UnsubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleChanged;
        LoadInitialMap.LevelLoadedEvent -= RefreshGridShaders;
        AudioTimeSyncController.OnTimeChanged -= OnTimeChanged;
    }

    protected override void OnObjectDelete(BeatmapObject obj)
    {
        RefreshGridShaders();
        countersPlus.UpdateStatistic(CountersPlusStatistic.BPM_Changes);
    }

    protected override void OnObjectSpawned(BeatmapObject obj)
    {
        countersPlus.UpdateStatistic(CountersPlusStatistic.BPM_Changes);
    }

    public void RefreshGridShaders()
    {
        for (int i = 0; i < LoadedObjects.Count; i++)
        {
            if (i >= ShaderArrayMaxSize - 1)
            {
                Debug.LogError($":hyperPepega: :mega: THE CAP FOR BPM CHANGES IS {ShaderArrayMaxSize - 1}, WHY TF DO YOU HAVE THIS MANY BPM CHANGES!?!?");
                break;
            }

            var bpmChange = LoadedObjects.ElementAt(i) as BeatmapBPMChange;

            if (i == 0)
            {
                bpmChange._Beat = Mathf.CeilToInt(bpmChange._time);
            }
            else
            {
                float songBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;
                BeatmapBPMChange lastChange = LoadedObjects.ElementAt(i - 1) as BeatmapBPMChange;
                float passedBeats = (bpmChange._time - lastChange._time - 0.01f) / songBPM * lastChange._BPM;
                bpmChange._Beat = lastChange._Beat + Mathf.CeilToInt(passedBeats);
            }
        }

        RefreshGridProperties();

        measureLinesController.RefreshMeasureLines();
    }

    public void RefreshGridProperties()
    {
        // Could probably save a tiny bit of performance since this should always be constant (0, Song BPM) but whatever
        var bpmChangeCount = 1;
        bpmShaderTimes[0] = 0;
        bpmShaderBPMs[0] = BeatSaberSongContainer.Instance.song.beatsPerMinute;
        
        // Grab the last object before grid ends
        var lastBPMChange = FindLastBPM(AudioTimeSyncController.CurrentBeat - FirstVisibleBeatTime, false);

        // Plug this last bpm change in, only if it does not have a visible container
        // If it does, then we'll be going over this BPM Change anyways so don't bother
        if (lastBPMChange != null && !lastBPMChange.HasAttachedContainer)
        {
            bpmChangeCount = 2;
            bpmShaderTimes[1] = lastBPMChange._time;
            bpmShaderBPMs[1] = lastBPMChange._BPM;
        }

        // Let's include all active, visible containers
        if (LoadedContainers.Count > 0)
        {
            // Ensure ordered by time (im not changing the entire collection to SortedSet just for this stfu)
            var activeBPMChanges = LoadedContainers.OrderBy(x => x.Key._time);

            // Iterate over and copy time/bpm values to arrays, and increase count
            foreach (var bpmChangeKVP in activeBPMChanges)
            {
                var bpmChange = bpmChangeKVP.Key as BeatmapBPMChange;
                bpmShaderTimes[bpmChangeCount] = bpmChange._time;
                bpmShaderBPMs[bpmChangeCount] = bpmChange._BPM;
                bpmChangeCount++;
            }
        }

        //Debug.Log(string.Join("|", bpmShaderTimes.Select(x => x.ToString())));
        //Debug.Log(string.Join("|", bpmShaderBPMs.Select(x => x.ToString())));
        //Debug.Log(bpmChangeCount);

        Shader.SetGlobalFloatArray(Times, bpmShaderTimes);
        Shader.SetGlobalFloatArray(BPMs, bpmShaderBPMs);
        Shader.SetGlobalInt(BPMCount, bpmChangeCount);
    }

    protected override void OnContainerSpawn(BeatmapObjectContainer container, BeatmapObject obj)
        => RefreshGridProperties();

    protected override void OnContainerDespawn(BeatmapObjectContainer container, BeatmapObject obj)
        => RefreshGridProperties();

    public float FindRoundedBPMTime(float beatTimeInSongBPM, float snap = -1)
    {
        if (snap == -1)
        {
            snap = 1f / AudioTimeSyncController.gridMeasureSnapping;
        }
        BeatmapBPMChange lastBPM = FindLastBPM(beatTimeInSongBPM); //Find the last BPM Change before our beat time
        if (lastBPM is null) return (float)Math.Round(beatTimeInSongBPM / snap, MidpointRounding.AwayFromZero) * snap; //If its null, return rounded song bpm
        float difference = beatTimeInSongBPM - lastBPM._time;
        float differenceInBPMBeat = difference / BeatSaberSongContainer.Instance.song.beatsPerMinute * lastBPM._BPM;
        float roundedDifference = (float)Math.Round(differenceInBPMBeat / snap, MidpointRounding.AwayFromZero) * snap;
        float roundedDifferenceInSongBPM = roundedDifference / lastBPM._BPM * BeatSaberSongContainer.Instance.song.beatsPerMinute;
        return roundedDifferenceInSongBPM + lastBPM._time;
    }

    /// <summary>
    /// Find the last <see cref="BeatmapBPMChange"/> before a given beat time.
    /// </summary>
    /// <param name="beatTimeInSongBPM">Time in raw beats (Unmodified by any BPM Changes)</param>
    /// <param name="inclusive">Whether or not to include <see cref="BeatmapBPMChange"/>s with the same time value.</param>
    /// <returns>The last <see cref="BeatmapBPMChange"/> before the given beat (or <see cref="null"/> if there is none).</returns>
    public BeatmapBPMChange FindLastBPM(float beatTimeInSongBPM, bool inclusive = true)
    {
        if (inclusive) return LoadedObjects.LastOrDefault(x => x._time <= beatTimeInSongBPM + 0.01f) as BeatmapBPMChange;
        return LoadedObjects.LastOrDefault(x => x._time + 0.01f < beatTimeInSongBPM) as BeatmapBPMChange;
    }

    /// <summary>
    /// Find the next <see cref="BeatmapBPMChange"/> after a given beat time.
    /// </summary>
    /// <param name="beatTimeInSongBPM">Time in raw beats (Unmodified by any BPM Changes)</param>
    /// <param name="inclusive">Whether or not to include <see cref="BeatmapBPMChange"/>s with the same time value.</param>
    /// <returns>The next <see cref="BeatmapBPMChange"/> after the given beat (or <see cref="null"/> if there is none).</returns>
    public BeatmapBPMChange FindNextBPM(float beatTimeInSongBPM, bool inclusive = false)
    {
        if (inclusive) return LoadedObjects.FirstOrDefault(x => x._time >= beatTimeInSongBPM - 0.01f) as BeatmapBPMChange;
        return LoadedObjects.FirstOrDefault(x => x._time - 0.01f > beatTimeInSongBPM) as BeatmapBPMChange;
    }

    /// <summary>
    /// Calculates the number of beats in song BPM for a given number of beats in local BPM, accounting for all BPM changes, relative to a starting position
    /// </summary>
    /// <param name="localBeats">Number of beats in local BPM</param>
    /// <param name="startBeat">The starting position from which to calculate. Number is in song BPM</param>
    /// <returns>The number of beats in song BPM equivalent to the number of beats in local bpm around a starting position</returns>
    public float LocalBeatsToSongBeats(float localBeats, float startBeat)
    {
        float totalSongBeats = 0;
        float localBeatsLeft = localBeats;
        float currentBeat = startBeat;
        float songBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;
        float currentBPM = FindLastBPM(startBeat, true)?._BPM ?? songBPM;

        if (localBeats > 0)
        {
            BeatmapBPMChange nextBPMChange = FindNextBPM(startBeat, false);
            while (localBeatsLeft > 0)
            {
                if (nextBPMChange is null)
                {
                    totalSongBeats += localBeatsLeft * songBPM / currentBPM;
                    break;
                }

                float distance = Math.Min(localBeatsLeft * songBPM / currentBPM, nextBPMChange._time - currentBeat);
                totalSongBeats += distance;
                localBeatsLeft -= distance * currentBPM / songBPM;

                currentBeat = nextBPMChange._time;
                currentBPM = nextBPMChange._BPM;
                nextBPMChange = FindNextBPM(currentBeat, false);
            }
        }
        else
        {
            BeatmapBPMChange lastBPMChange = FindLastBPM(startBeat, false);
            while (localBeatsLeft < 0)
            {
                if (lastBPMChange is null)
                {
                    totalSongBeats += localBeatsLeft;
                    break;
                }

                currentBPM = lastBPMChange._BPM;

                float distance = Math.Max(localBeatsLeft * songBPM / currentBPM, lastBPMChange._time - currentBeat);
                totalSongBeats += distance;
                localBeatsLeft -= distance * currentBPM / songBPM;

                currentBeat = lastBPMChange._time;
                lastBPMChange = FindLastBPM(currentBeat, false);
            }
        }

        return totalSongBeats;
    }

    public override BeatmapObjectContainer CreateContainer() => BeatmapBPMChangeContainer.SpawnBPMChange(null, ref bpmPrefab);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        BeatmapBPMChangeContainer container = con as BeatmapBPMChangeContainer;
        container.UpdateBPMText();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;

public class BPMChangeGridContainer : BeatmapObjectContainerCollection
{
    // We cap the amount of BPM Changes in the shader to reduce memory and have it work on OpenGL/Vulkan/Metal.
    // Unless you have over 170 BPM Changes within a section of a song, this SHOULD be fine.
    public static readonly int MaxBpmChangesInShader = 170;

    private static readonly int times = Shader.PropertyToID("_BPMChange_Times");
    private static readonly int jsonTimes = Shader.PropertyToID("_BPMChange_Json_Times");
    private static readonly int bpMs = Shader.PropertyToID("_BPMChange_BPMs");
    private static readonly int bpmCount = Shader.PropertyToID("_BPMChange_Count");
    private static readonly int songBpm = Shader.PropertyToID("_SongBPM");

    private static readonly float firstVisibleBeatTime = 2;

    private static readonly float[] bpmShaderTimes = new float[MaxBpmChangesInShader];
    private static readonly float[] bpmShaderJsonTimes = new float[MaxBpmChangesInShader];
    private static readonly float[] bpmShaderBpMs = new float[MaxBpmChangesInShader];

    [SerializeField] private Transform gridRendererParent;
    [SerializeField] private GameObject bpmPrefab;
    [SerializeField] private MeasureLinesController measureLinesController;
    [SerializeField] private CountersPlusController countersPlus;

    public override ObjectType ContainerType => ObjectType.BpmChange;

    private IEnumerator Start()
    {
        if (BeatSaberSongContainer.Instance.DifficultyData.CustomData == null) yield break;

        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);

        Shader.SetGlobalFloat(songBpm, BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
    }

    internal override void SubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent += EditorScaleChanged;
        LoadInitialMap.LevelLoadedEvent += RefreshModifiedBeat;
        AudioTimeSyncController.TimeChanged += OnTimeChanged;
    }

    private void EditorScaleChanged(float obj) =>
        Shader.SetGlobalFloat("_EditorScale", EditorScaleController.EditorScale);

    private void OnTimeChanged()
    {
        if (AudioTimeSyncController.IsPlaying) return;
        RefreshGridProperties();
    }

    internal override void UnsubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleChanged;
        LoadInitialMap.LevelLoadedEvent -= RefreshModifiedBeat;
        AudioTimeSyncController.TimeChanged -= OnTimeChanged;
    }

    protected override void OnObjectDelete(BaseObject obj) => OnObjectDeleteOrSpawn(obj);

    protected override void OnObjectSpawned(BaseObject obj) => OnObjectDeleteOrSpawn(obj);

    private void OnObjectDeleteOrSpawn(BaseObject obj)
    {
        RefreshModifiedBeat();
        countersPlus.UpdateStatistic(CountersPlusStatistic.BpmChanges);
        BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(obj.JsonTime);
    }

    public void RefreshModifiedBeat()
    {
        BaseBpmEvent lastChange = null;

        foreach (var obj in LoadedObjects)
        {
            var bpmChange = obj as BaseBpmEvent;

            if (lastChange == null)
            {
                bpmChange.Beat = Mathf.CeilToInt(bpmChange.JsonTime);
            }
            else
            {
                var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                var passedBeats = (bpmChange.JsonTime - lastChange.JsonTime - 0.01f) / songBpm * lastChange.Bpm;
                bpmChange.Beat = lastChange.Beat + Mathf.CeilToInt(passedBeats);
            }

            lastChange = bpmChange;
        }

        RefreshGridProperties();

        measureLinesController.RefreshMeasureLines();
    }

    public void RefreshGridProperties()
    {
        // Could probably save a tiny bit of performance since this should always be constant (0, Song BPM) but whatever
        var bpmChangeCount = 1;
        bpmShaderTimes[0] = 0;
        bpmShaderJsonTimes[0] = 0;
        bpmShaderBpMs[0] = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;

        // Grab the last object before grid ends
        var lastBpmChange = FindLastBpm(AudioTimeSyncController.CurrentBeat - firstVisibleBeatTime, false);

        // Plug this last bpm change in
        // Believe it or not, I cannot actually skip this BPM change if it exists
        if (lastBpmChange != null)
        {
            bpmChangeCount = 2;
            bpmShaderTimes[1] = lastBpmChange.SongBpmTime;
            bpmShaderJsonTimes[1] = lastBpmChange.JsonTime;
            bpmShaderBpMs[1] = lastBpmChange.Bpm;
        }

        if (LoadedContainers.Count > 0)
        {
            // Ensure ordered by time (im not changing the entire collection to SortedSet just for this stfu)
            var activeBpmChanges = LoadedContainers.OrderBy(x => x.Key.JsonTime);

            // Iterate over and copy time/bpm values to arrays, and increase count
            foreach (var bpmChangeKvp in activeBpmChanges)
            {
                if (bpmChangeCount >= MaxBpmChangesInShader)
                {
                    Debug.LogError(
                        @$":hyperPepega: :mega: THE CAP FOR BPM CHANGES IN THE SHADER IS {MaxBpmChangesInShader - 1}");
                    break;
                }

                var bpmChange = bpmChangeKvp.Key as BaseBpmEvent;
                bpmShaderTimes[bpmChangeCount] = bpmChange.SongBpmTime;
                bpmShaderJsonTimes[bpmChangeCount] = bpmChange.JsonTime;
                bpmShaderBpMs[bpmChangeCount] = bpmChange.Bpm;
                bpmChangeCount++;
            }
        }

        // Pass all of this into our shader
        Shader.SetGlobalFloatArray(times, bpmShaderTimes);
        Shader.SetGlobalFloatArray(jsonTimes, bpmShaderJsonTimes);
        Shader.SetGlobalFloatArray(bpMs, bpmShaderBpMs);
        Shader.SetGlobalInt(bpmCount, bpmChangeCount);
    }

    protected override void OnContainerSpawn(ObjectContainer container, BaseObject obj)
        => RefreshGridProperties();

    protected override void OnContainerDespawn(ObjectContainer container, BaseObject obj)
        => RefreshGridProperties();

    public float FindRoundedBpmTime(float beatTimeInSongBpm, float snap = -1)
    {
        if (snap == -1) snap = 1f / AudioTimeSyncController.GridMeasureSnapping;
        var lastBpm = FindLastBpm(beatTimeInSongBpm); //Find the last BPM Change before our beat time
        if (lastBpm is null)
        {
            return (float)Math.Round(beatTimeInSongBpm / snap, MidpointRounding.AwayFromZero) *
                   snap; //If its null, return rounded song bpm
        }

        var difference = beatTimeInSongBpm - lastBpm.SongBpmTime;
        var differenceInBpmBeat = difference / BeatSaberSongContainer.Instance.Song.BeatsPerMinute * lastBpm.Bpm;
        var roundedDifference = (float)Math.Round(differenceInBpmBeat / snap, MidpointRounding.AwayFromZero) * snap;
        var roundedDifferenceInSongBpm =
            roundedDifference / lastBpm.Bpm * BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        return roundedDifferenceInSongBpm + lastBpm.SongBpmTime;
    }

    /// <summary>
    ///     Find the last <see cref="BaseBpmEvent" /> before a given beat time.
    /// </summary>
    /// <param name="beatTimeInSongBpm">Time in raw beats (Unmodified by any BPM Changes)</param>
    /// <param name="inclusive">Whether or not to include <see cref="BaseBpmEvent" />s with the same time value.</param>
    /// <returns>The last <see cref="BaseBpmEvent" /> before the given beat (or <see cref="null" /> if there is none).</returns>
    public BaseBpmEvent FindLastBpm(float beatTimeInSongBpm, bool inclusive = true)
    {
        if (inclusive)
            return LoadedObjects.LastOrDefault(x => x.SongBpmTime <= beatTimeInSongBpm + 0.01f) as BaseBpmEvent;
        return LoadedObjects.LastOrDefault(x => x.SongBpmTime + 0.01f < beatTimeInSongBpm) as BaseBpmEvent;
    }

    /// <summary>
    ///     Find the next <see cref="BaseBpmEvent" /> after a given beat time.
    /// </summary>
    /// <param name="beatTimeInSongBpm">Time in raw beats (Unmodified by any BPM Changes)</param>
    /// <param name="inclusive">Whether or not to include <see cref="BaseBpmEvent" />s with the same time value.</param>
    /// <returns>The next <see cref="BaseBpmEvent" /> after the given beat (or <see cref="null" /> if there is none).</returns>
    public BaseBpmEvent FindNextBpm(float beatTimeInSongBpm, bool inclusive = false)
    {
        if (inclusive)
            return LoadedObjects.FirstOrDefault(x => x.JsonTime >= beatTimeInSongBpm - 0.01f) as BaseBpmEvent;
        return LoadedObjects.FirstOrDefault(x => x.JsonTime - 0.01f > beatTimeInSongBpm) as BaseBpmEvent;
    }

    private BaseBpmEvent DefaultEvent()
    {
        var defaultEvent = BeatmapFactory.BpmEvent();
        defaultEvent.Bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        return defaultEvent;
    }

    public float JsonTimeToSongBpmTime(float jsonTime)
    {
        var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        var bpms = new List<BaseBpmEvent> { DefaultEvent() };
        bpms.AddRange(LoadedObjects.Where(x => x.JsonTime <= jsonTime).Cast<BaseBpmEvent>());

        var currentSongBeats = 0f;
        for (int i = 0; i < bpms.Count() - 1; i++)
        {
            var bpmChange = bpms[i];
            var nextBpmChange = bpms[i + 1];

            var timeDiff = nextBpmChange.JsonTime - bpmChange.JsonTime;

            currentSongBeats += timeDiff * (songBpm / bpmChange.Bpm);
        }

        currentSongBeats += (jsonTime - bpms.Last().JsonTime) * (songBpm / bpms.Last().Bpm);
        return currentSongBeats;
    }

    public float SongBpmTimeToJsonTime(float songBpmTime)
    {
        var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        var bpms = new List<BaseBpmEvent> { DefaultEvent() };
        bpms.AddRange(LoadedObjects.Cast<BaseBpmEvent>());

        var seconds = songBpmTime * (60f / songBpm);

        var currentSeconds = 0f;
        var nextSeconds = 0f;
        for (int i = 0; i < bpms.Count - 1; i++)
        {
            var bpmChange = bpms[i];
            var nextBpmChange = bpms[i + 1];

            var timeDiff = nextBpmChange.JsonTime - bpmChange.JsonTime;
            var scale = bpmChange.Bpm / 60;
            nextSeconds += timeDiff / scale;

            if (nextSeconds > seconds)
            {
                return bpmChange.JsonTime + scale * (seconds - currentSeconds);
            }

            currentSeconds = nextSeconds;
        }

        var lastBpm = bpms.Last();
        return lastBpm.JsonTime + lastBpm.Bpm / 60 * (seconds - currentSeconds);
    }

    public override ObjectContainer CreateContainer() =>
        BpmEventContainer.SpawnBpmChange(null, ref bpmPrefab);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var container = con as BpmEventContainer;
        container.UpdateBpmText();
    }
}

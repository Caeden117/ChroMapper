using System;
using System.Collections;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public class BPMChangesContainer : BeatmapObjectContainerCollection
{
    // We cap the amount of BPM Changes in the shader to reduce memory and have it work on OpenGL/Vulkan/Metal.
    // Unless you have over 256 BPM Changes within a section of a song, this SHOULD be fine.
    public static readonly int MaxBpmChangesInShader = 256;

    private static readonly int Times = Shader.PropertyToID("_BPMChange_Times");
    private static readonly int BpMs = Shader.PropertyToID("_BPMChange_BPMs");
    private static readonly int BpmCount = Shader.PropertyToID("_BPMChange_Count");

    private static readonly float FirstVisibleBeatTime = 2;

    private static readonly float[] BpmShaderTimes = new float[MaxBpmChangesInShader];
    private static readonly float[] BpmShaderBpMs = new float[MaxBpmChangesInShader];

    [SerializeField] private Transform gridRendererParent;
    [SerializeField] private GameObject bpmPrefab;
    [SerializeField] private MeasureLinesController measureLinesController;
    [SerializeField] private CountersPlusController countersPlus;

    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.BpmChange;

    private IEnumerator Start()
    {
        if (BeatSaberSongContainer.Instance.DifficultyData.CustomData == null) yield break;

        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);

        // TODO: Localize the big chunk of text
        if (BeatSaberSongContainer.Instance.DifficultyData.CustomData?.HasKey("_editorOffset") == true &&
            BeatSaberSongContainer.Instance.DifficultyData.CustomData["_editorOffset"] > 0f)
        {
            if (Settings.Instance.Reminder_UnsupportedEditorOffset)
            {
                PersistentUI.Instance.ShowDialogBox(
                    "ChroMapper has detected editor offset originating from MediocreMap Assistant 2.\n" +
                    "This is unsupported by ChroMapper. It is recommended to set up your audio to eliminate the need for any offset.\n" +
                    "However, ChroMapper can replace this offset with a BPM Change to keep the grid aligned.\n\n" +
                    "Would you like ChroMapper to do this?", CreateAutogeneratedBpmChange, "Yes",
                    "Do This Automatically", "No");
            }
            else
                CreateAutogeneratedBpmChange(1);
        }
    }

    private void CreateAutogeneratedBpmChange(int res)
    {
        if (res == 2) return;
        Settings.Instance.Reminder_UnsupportedEditorOffset = res == 0;

        float offset = BeatSaberSongContainer.Instance.DifficultyData.CustomData["_editorOffset"];

        BeatSaberSongContainer.Instance.DifficultyData.CustomData.Remove("_editorOffset");
        BeatSaberSongContainer.Instance.DifficultyData.CustomData.Remove("_editorOldOffset");

        var autoGenerated = new BeatmapBPMChange(
            BeatSaberSongContainer.Instance.Song.BeatsPerMinute,
            AudioTimeSyncController.GetBeatFromSeconds(offset / 1000f)) {CustomData = new JSONObject()};
        autoGenerated.CustomData.Add("__note", "Autogenerated by ChroMapper");

        SpawnObject(autoGenerated, true, false);
        RefreshModifiedBeat();
        RefreshPool(true);
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

    protected override void OnObjectDelete(BeatmapObject obj)
    {
        RefreshModifiedBeat();
        countersPlus.UpdateStatistic(CountersPlusStatistic.BpmChanges);
    }

    protected override void OnObjectSpawned(BeatmapObject obj) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.BpmChanges);

    public void RefreshModifiedBeat()
    {
        BeatmapBPMChange lastChange = null;

        foreach (var obj in LoadedObjects)
        {
            var bpmChange = obj as BeatmapBPMChange;

            if (lastChange == null)
            {
                bpmChange.Beat = Mathf.CeilToInt(bpmChange.Time);
            }
            else
            {
                var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                var passedBeats = (bpmChange.Time - lastChange.Time - 0.01f) / songBpm * lastChange.Bpm;
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
        BpmShaderTimes[0] = 0;
        BpmShaderBpMs[0] = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;

        // Grab the last object before grid ends
        var lastBpmChange = FindLastBpm(AudioTimeSyncController.CurrentBeat - FirstVisibleBeatTime, false);

        // Plug this last bpm change in
        // Believe it or not, I cannot actually skip this BPM change if it exists
        if (lastBpmChange != null)
        {
            bpmChangeCount = 2;
            BpmShaderTimes[1] = lastBpmChange.Time;
            BpmShaderBpMs[1] = lastBpmChange.Bpm;
        }

        // Let's include all active, visible containers
        if (LoadedContainers.Count > 0)
        {
            // Ensure ordered by time (im not changing the entire collection to SortedSet just for this stfu)
            var activeBpmChanges = LoadedContainers.OrderBy(x => x.Key.Time);

            // Iterate over and copy time/bpm values to arrays, and increase count
            foreach (var bpmChangeKvp in activeBpmChanges)
            {
                if (bpmChangeCount >= MaxBpmChangesInShader)
                {
                    Debug.LogError(
                        @$":hyperPepega: :mega: THE CAP FOR BPM CHANGES IN THE SHADER IS {MaxBpmChangesInShader - 1}");
                    break;
                }

                var bpmChange = bpmChangeKvp.Key as BeatmapBPMChange;
                BpmShaderTimes[bpmChangeCount] = bpmChange.Time;
                BpmShaderBpMs[bpmChangeCount] = bpmChange.Bpm;
                bpmChangeCount++;
            }
        }

        // Pass all of this into our shader
        Shader.SetGlobalFloatArray(Times, BpmShaderTimes);
        Shader.SetGlobalFloatArray(BpMs, BpmShaderBpMs);
        Shader.SetGlobalInt(BpmCount, bpmChangeCount);
    }

    protected override void OnContainerSpawn(BeatmapObjectContainer container, BeatmapObject obj)
        => RefreshGridProperties();

    protected override void OnContainerDespawn(BeatmapObjectContainer container, BeatmapObject obj)
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

        var difference = beatTimeInSongBpm - lastBpm.Time;
        var differenceInBpmBeat = difference / BeatSaberSongContainer.Instance.Song.BeatsPerMinute * lastBpm.Bpm;
        var roundedDifference = (float)Math.Round(differenceInBpmBeat / snap, MidpointRounding.AwayFromZero) * snap;
        var roundedDifferenceInSongBpm =
            roundedDifference / lastBpm.Bpm * BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        return roundedDifferenceInSongBpm + lastBpm.Time;
    }

    /// <summary>
    ///     Find the last <see cref="BeatmapBPMChange" /> before a given beat time.
    /// </summary>
    /// <param name="beatTimeInSongBpm">Time in raw beats (Unmodified by any BPM Changes)</param>
    /// <param name="inclusive">Whether or not to include <see cref="BeatmapBPMChange" />s with the same time value.</param>
    /// <returns>The last <see cref="BeatmapBPMChange" /> before the given beat (or <see cref="null" /> if there is none).</returns>
    public BeatmapBPMChange FindLastBpm(float beatTimeInSongBpm, bool inclusive = true)
    {
        if (inclusive)
            return LoadedObjects.LastOrDefault(x => x.Time <= beatTimeInSongBpm + 0.01f) as BeatmapBPMChange;
        return LoadedObjects.LastOrDefault(x => x.Time + 0.01f < beatTimeInSongBpm) as BeatmapBPMChange;
    }

    /// <summary>
    ///     Find the next <see cref="BeatmapBPMChange" /> after a given beat time.
    /// </summary>
    /// <param name="beatTimeInSongBpm">Time in raw beats (Unmodified by any BPM Changes)</param>
    /// <param name="inclusive">Whether or not to include <see cref="BeatmapBPMChange" />s with the same time value.</param>
    /// <returns>The next <see cref="BeatmapBPMChange" /> after the given beat (or <see cref="null" /> if there is none).</returns>
    public BeatmapBPMChange FindNextBpm(float beatTimeInSongBpm, bool inclusive = false)
    {
        if (inclusive)
            return LoadedObjects.FirstOrDefault(x => x.Time >= beatTimeInSongBpm - 0.01f) as BeatmapBPMChange;
        return LoadedObjects.FirstOrDefault(x => x.Time - 0.01f > beatTimeInSongBpm) as BeatmapBPMChange;
    }

    /// <summary>
    ///     Calculates the number of beats in song BPM for a given number of beats in local BPM, accounting for all BPM
    ///     changes, relative to a starting position
    /// </summary>
    /// <param name="localBeats">Number of beats in local BPM</param>
    /// <param name="startBeat">The starting position from which to calculate. Number is in song BPM</param>
    /// <returns>The number of beats in song BPM equivalent to the number of beats in local bpm around a starting position</returns>
    public float LocalBeatsToSongBeats(float localBeats, float startBeat)
    {
        float totalSongBeats = 0;
        var localBeatsLeft = localBeats;
        var currentBeat = startBeat;
        var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        var currentBpm = FindLastBpm(startBeat)?.Bpm ?? songBpm;

        if (localBeats > 0)
        {
            var nextBpmChange = FindNextBpm(startBeat);
            while (localBeatsLeft > 0)
            {
                if (nextBpmChange is null)
                {
                    totalSongBeats += localBeatsLeft * songBpm / currentBpm;
                    break;
                }

                var distance = Math.Min(localBeatsLeft * songBpm / currentBpm, nextBpmChange.Time - currentBeat);
                totalSongBeats += distance;
                localBeatsLeft -= distance * currentBpm / songBpm;

                currentBeat = nextBpmChange.Time;
                currentBpm = nextBpmChange.Bpm;
                nextBpmChange = FindNextBpm(currentBeat);
            }
        }
        else
        {
            var lastBpmChange = FindLastBpm(startBeat, false);
            while (localBeatsLeft < 0)
            {
                if (lastBpmChange is null)
                {
                    totalSongBeats += localBeatsLeft;
                    break;
                }

                currentBpm = lastBpmChange.Bpm;

                var distance = Math.Max(localBeatsLeft * songBpm / currentBpm, lastBpmChange.Time - currentBeat);
                totalSongBeats += distance;
                localBeatsLeft -= distance * currentBpm / songBpm;

                currentBeat = lastBpmChange.Time;
                lastBpmChange = FindLastBpm(currentBeat, false);
            }
        }

        return totalSongBeats;
    }

    public override BeatmapObjectContainer CreateContainer() =>
        BeatmapBPMChangeContainer.SpawnBpmChange(null, ref bpmPrefab);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var container = con as BeatmapBPMChangeContainer;
        container.UpdateBpmText();
    }
}

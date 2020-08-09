using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BPMChangesContainer : BeatmapObjectContainerCollection
{

    public float lastBPM;
    public int lastCheckedBPMIndex = 0;

    private bool firstSeen = false;
    private IEnumerable<Renderer> allGridRenderers;
    [SerializeField] private Transform gridRendererParent;
    [SerializeField] private GameObject bpmPrefab;
    [SerializeField] private MeasureLinesController measureLinesController;

    //This is a shader-level restriction and nothing I can fix.
    public static readonly int ShaderArrayMaxSize = 1023; //Unity hard caps it here.

    private static readonly int Times = Shader.PropertyToID("_BPMChange_Times");
    private static readonly int BPMs = Shader.PropertyToID("_BPMChange_BPMs");
    private static readonly int BPMCount = Shader.PropertyToID("_BPMChange_Count");

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.BPM_CHANGE;

    private void Start()
    {
        allGridRenderers = gridRendererParent.GetComponentsInChildren<Renderer>().Where(x => x.material.shader.name == "Grid ZDir");
        lastBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;
    }

    internal override void SubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent += EditorScaleChanged;
        LoadInitialMap.LevelLoadedEvent += RefreshGridShaders;
    }

    private void EditorScaleChanged(float obj)
    {
        foreach (Renderer renderer in allGridRenderers)
        {
            renderer.material.SetFloat("_EditorScale", EditorScaleController.EditorScale);
        }
    }

    internal override void UnsubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleChanged;
        LoadInitialMap.LevelLoadedEvent -= RefreshGridShaders;
    }

    protected override void OnObjectDelete(BeatmapObject obj)
    {
        RefreshGridShaders();
    }

    public void RefreshGridShaders()
    {
        float[] bpmChangeTimes = new float[ShaderArrayMaxSize];
        float[] bpmChangeBPMS = new float[ShaderArrayMaxSize];
        bpmChangeTimes[0] = 0;
        bpmChangeBPMS[0] = BeatSaberSongContainer.Instance.song.beatsPerMinute;
        for (int i = 0; i < LoadedObjects.Count; i++)
        {
            if (i >= ShaderArrayMaxSize - 1)
            {
                Debug.LogError($":hyperPepega: :mega: THE CAP FOR BPM CHANGES IS {ShaderArrayMaxSize - 1}, WHY TF DO YOU HAVE THIS MANY BPM CHANGES!?!?");
                break;
            }
            BeatmapBPMChange bpmChange = LoadedObjects.ElementAt(i) as BeatmapBPMChange;
            bpmChangeTimes[i + 1] = bpmChange._time;
            bpmChangeBPMS[i + 1] = bpmChange._BPM;
        }
        foreach (Renderer renderer in allGridRenderers)
        {
            renderer.material.SetFloatArray(Times, bpmChangeTimes);
            renderer.material.SetFloatArray(BPMs, bpmChangeBPMS);
            renderer.material.SetInt(BPMCount, LoadedObjects.Count + 1);
        }
        measureLinesController.RefreshMeasureLines();
    }

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
        if (inclusive) return LoadedObjects.LastOrDefault(x => x._time <= beatTimeInSongBPM + 0.001f) as BeatmapBPMChange;
        return LoadedObjects.LastOrDefault(x => x._time + 0.001f < beatTimeInSongBPM) as BeatmapBPMChange;
    }

    public override BeatmapObjectContainer CreateContainer() => BeatmapBPMChangeContainer.SpawnBPMChange(null, ref bpmPrefab);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        BeatmapBPMChangeContainer container = con as BeatmapBPMChangeContainer;
        container.UpdateBPMText();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BPMChangesContainer : BeatmapObjectContainerCollection {

    public float lastBPM;
    public int lastCheckedBPMIndex = 0;

    private bool firstSeen = false;
    private IEnumerable<Renderer> allGridRenderers;
    [SerializeField] private Transform gridRendererParent;
    [SerializeField] private GameObject bpmPrefab;

    //This is a shader-level restriction and nothing I can fix.
    public static readonly int ShaderArrayMaxSize = 2046;

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
    }

    private void EditorScaleChanged(int obj)
    {
        foreach (Renderer renderer in allGridRenderers)
        {
            renderer.material.SetFloat("_EditorScale", EditorScaleController.EditorScale);
        }
    }

    internal override void UnsubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleChanged;
    }

    public override void SortObjects()
    {
        float[] bpmChangeTimes = new float[ShaderArrayMaxSize];
        float[] bpmChangeBPMS = new float[ShaderArrayMaxSize];
        bpmChangeTimes[0] = 0;
        bpmChangeBPMS[0] = BeatSaberSongContainer.Instance.song.beatsPerMinute;
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        for (int i = 0; i < LoadedContainers.Count; i++)
        {
            if (i >= ShaderArrayMaxSize - 1)
            {
                Debug.LogError($":hyperPepega: :mega: THE CAP FOR BPM CHANGES IS {ShaderArrayMaxSize - 1}, WHY TF DO YOU HAVE THIS MANY BPM CHANGES!?!?");
                break;
            }
            BeatmapBPMChangeContainer con = LoadedContainers[i] as BeatmapBPMChangeContainer;
            con.UpdateGridPosition();
            BeatmapBPMChange bpmChange = con.objectData as BeatmapBPMChange;
            bpmChangeTimes[i + 1] = bpmChange._time;
            bpmChangeBPMS[i + 1] = bpmChange._BPM;
        }
        foreach (Renderer renderer in allGridRenderers)
        {
            renderer.material.SetFloatArray(Times, bpmChangeTimes);
            renderer.material.SetFloatArray(BPMs, bpmChangeBPMS);
            renderer.material.SetInt(BPMCount, LoadedContainers.Count + 1);
        }
    }

    public float FindRoundedBPMTime(float beatTimeInSongBPM)
    {
        float snap = 1f / AudioTimeSyncController.gridMeasureSnapping; //For rounding to nearest 1/1th beat, 1/2th beat, 1/4th, etc.
        BeatmapBPMChange lastBPM = FindLastBPM(beatTimeInSongBPM); //Find the last BPM Change before our beat time
        if (lastBPM is null) return (float)Math.Round(beatTimeInSongBPM / snap) * snap; //If its null, return rounded song bpm
        float difference = beatTimeInSongBPM - lastBPM._time;
        float differenceInBPMBeat = difference / BeatSaberSongContainer.Instance.song.beatsPerMinute * lastBPM._BPM;
        float roundedDifference = (float)Math.Round(differenceInBPMBeat / snap) * snap;
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
        if (inclusive) return LoadedContainers.LastOrDefault(x => x.objectData._time <= beatTimeInSongBPM)?.objectData as BeatmapBPMChange ?? null;
        return LoadedContainers.LastOrDefault(x => x.objectData._time < beatTimeInSongBPM &&
            beatTimeInSongBPM - x.objectData._time > 0.01f)?.objectData as BeatmapBPMChange ?? null;
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting, bool removeConflicting = true, bool refreshMap = true)
    {
        if (LoadedContainers.Count >= ShaderArrayMaxSize)
        {
            Debug.LogError("Something's wrong, I can feel it!\nMore BPM Changes are present then what Unity shaders allow!");
        }
        conflicting = null;
        BeatmapBPMChangeContainer beatmapBPMChange = BeatmapBPMChangeContainer.SpawnBPMChange(obj as BeatmapBPMChange, ref bpmPrefab);
        beatmapBPMChange.transform.SetParent(GridTransform);
        beatmapBPMChange.UpdateGridPosition();
        LoadedContainers.Add(beatmapBPMChange);
        if (refreshMap) SelectionController.RefreshMap();
        return beatmapBPMChange;
    }
}

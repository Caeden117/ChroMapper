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

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.BPM_CHANGE;

    private void Start()
    {
        allGridRenderers = gridRendererParent.GetComponentsInChildren<Renderer>().Where(x => x.material.shader.name == "Grid ZDir");
        lastBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;
    }

    internal override void SubscribeToCallbacks() { }
    internal override void UnsubscribeToCallbacks() { }

    public override void SortObjects()
    {
        List<float> bpmChangeTimes = new List<float>();
        List<float> bpmChangeBPMS = new List<float>();
        bpmChangeTimes.Add(0);
        bpmChangeBPMS.Add(BeatSaberSongContainer.Instance.song.beatsPerMinute);
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        foreach (BeatmapBPMChangeContainer con in LoadedContainers)
        {
            con.UpdateGridPosition();
            BeatmapBPMChange bpmChange = con.objectData as BeatmapBPMChange;
            bpmChangeTimes.Add(bpmChange._time);
            bpmChangeBPMS.Add(bpmChange._BPM);
        }
        foreach (Renderer renderer in allGridRenderers)
        {
            renderer.material.SetFloatArray("_BPMChange_Times", bpmChangeTimes.ToArray());
            renderer.material.SetFloatArray("_BPMChange_BPMs", bpmChangeBPMS.ToArray());
            renderer.material.SetInt("_BPMChange_Count", bpmChangeBPMS.Count);
            renderer.material.SetFloat("_EditorScale", EditorScaleController.EditorScale);
        }
    }

    public float FindLastBPM(float beatTimeInSongBPM, out int lastBPMChangeIndex)
    {
        lastBPMChangeIndex = -1;
        float last = BeatSaberSongContainer.Instance.song.beatsPerMinute;
        for (int i = 0; i < LoadedContainers.Count; i++)
        {
            if (i + 1 >= LoadedContainers.Count) break;
            if (LoadedContainers[i].objectData._time <= beatTimeInSongBPM && LoadedContainers[i + 1].objectData._time >= beatTimeInSongBPM)
            {
                last = ((BeatmapBPMChangeContainer) LoadedContainers[i]).bpmData._BPM;
                lastCheckedBPMIndex = i;
                break;
            }
        }
        lastBPM = last;
        return last;
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting, bool removeConflicting = true, bool refreshMap = true)
    {
        conflicting = null;
        BeatmapBPMChangeContainer beatmapBPMChange = BeatmapBPMChangeContainer.SpawnBPMChange(obj as BeatmapBPMChange, ref bpmPrefab);
        beatmapBPMChange.transform.SetParent(GridTransform);
        beatmapBPMChange.UpdateGridPosition();
        LoadedContainers.Add(beatmapBPMChange);
        if (refreshMap) SelectionController.RefreshMap();
        return beatmapBPMChange;
    }
}

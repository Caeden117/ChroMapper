using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class BPMChangesContainer : BeatmapObjectContainerCollection {

    [SerializeField] private GameObject bpmPrefab;
    public float lastBPM = 0;
    public int lastCheckedBPMIndex = 0;

    private void Start()
    {
        lastBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;
    }

    internal override void SubscribeToCallbacks() { }
    internal override void UnsubscribeToCallbacks() { }

    public override void SortObjects()
    {
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        foreach (BeatmapBPMChangeContainer con in LoadedContainers) con.UpdateGridPosition();
    }

    public float FindLastBPM(float beatTimeInSongBPM, out int lastBPMChangeIndex)
    {
        lastBPMChangeIndex = 0;
        float last = BeatSaberSongContainer.Instance.song.beatsPerMinute;
        for (int i = 0; i < LoadedContainers.Count; i++)
        {
            if (i + 1 >= LoadedContainers.Count) break;
            if (LoadedContainers[i].objectData._time <= beatTimeInSongBPM && LoadedContainers[i + 1].objectData._time >= beatTimeInSongBPM)
            {
                last = (LoadedContainers[i] as BeatmapBPMChangeContainer).bpmData._BPM;
                lastCheckedBPMIndex = i;
                break;
            }
        }
        lastBPM = last;
        return last;
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj)
    {
        BeatmapBPMChangeContainer beatmapBPMChange = BeatmapBPMChangeContainer.SpawnBPMChange(obj as BeatmapBPMChange, ref bpmPrefab);
        beatmapBPMChange.transform.SetParent(GridTransform);
        beatmapBPMChange.UpdateGridPosition();
        LoadedContainers.Add(beatmapBPMChange);
        return beatmapBPMChange;
    }
}

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

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        AudioTimeSyncController.OnPlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        AudioTimeSyncController.OnPlayToggle -= OnPlayToggle;
    }

    void OnPlayToggle(bool playing)
    {
        if (playing)
            foreach (BeatmapObjectContainer container in LoadedContainers)
            {
                BeatmapBPMChangeContainer e = container as BeatmapBPMChangeContainer;
                container.gameObject.SetActive(e.objectData._time < AudioTimeSyncController.CurrentBeat + SpawnCallbackController.offset
                    && e.objectData._time >= AudioTimeSyncController.CurrentBeat - DespawnCallbackController.offset);
            }
        else
            for (int i = 0; i < LoadedContainers.Count; i++)
                LoadedContainers[i].gameObject.SetActive(true);
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        OnPlayToggle(AudioTimeSyncController.IsPlaying);
    }

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

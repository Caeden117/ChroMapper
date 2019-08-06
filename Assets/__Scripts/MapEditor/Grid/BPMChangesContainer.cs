using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class BPMChangesContainer : MonoBehaviour {

    [SerializeField] AudioTimeSyncController atsc;

    [SerializeField] public List<BeatmapBPMChangeContainer> loadedBPMChanges = new List<BeatmapBPMChangeContainer>();

    [SerializeField] BeatmapObjectCallbackController spawn;
    [SerializeField] BeatmapObjectCallbackController despawn;

    public float lastBPM = 0;
    public int lastCheckedBPMIndex = 0;

    private void Start()
    {
        lastBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;
    }

    public void SortEvents()
    {
        loadedBPMChanges = loadedBPMChanges.OrderBy(x => x.objectData._time).ToList();
        foreach (BeatmapBPMChangeContainer con in loadedBPMChanges) con.UpdateGridPosition();
    }

    public float FindLastBPM(float beatTimeInSongBPM, out int lastBPMChangeIndex)
    {
        lastBPMChangeIndex = 0;
        float last = BeatSaberSongContainer.Instance.song.beatsPerMinute;
        for (int i = 0; i < loadedBPMChanges.Count; i++)
        {
            if (i + 1 >= loadedBPMChanges.Count) break;
            if (loadedBPMChanges[i].objectData._time <= beatTimeInSongBPM && loadedBPMChanges[i + 1].objectData._time >= beatTimeInSongBPM)
            {
                last = loadedBPMChanges[i].bpmData._BPM;
                lastCheckedBPMIndex = i;
                break;
            }
        }
        lastBPM = last;
        return last;
    }
}

using System.Linq;
using UnityEngine;

public class BPMChangesContainer : BeatmapObjectContainerCollection {

    [SerializeField] private GameObject bpmPrefab;
    public float lastBPM;
    public int lastCheckedBPMIndex = 0;
    
    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.BPM_CHANGE;

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
        if (LoadedContainers.Any())
        {
            PersistentUI.Instance.ShowDialogBox("ChroMapper has detected BPM changes in your map.\n\n" +
                "Not gonna lie to you, BPM changes have been a major pain in the ass for all of CM " +
                "development. It will be a huge refactor to get BPM changes to MMA2 standards.\n\n" +
                "I will probably be deprecating/removing BPM changes from future versions just " +
                "because they were/are such a pain in the ass to deal with. If you wish to keep them, " +
                "you'd probably be better of making this map using MM/MMA2.\n\n" + 
                "Also, expect weird bugs and inconsistencies if you do use BPM changes with ChroMapper.\n\n" +
                "Sorry! I'm not a perfect developer. If you are infuriated because of my inability to add an actually " +
                "kinda useful feature, please consider learning Unity, C# and Git, fork ChroMapper, and fix/add it yourself. " +
                "I'd be impressed if you can fix all the issues with the visual editor grids, placing objects with BPM changes, " +
                "snapping in time with BPM changes, etc.", null, PersistentUI.DialogBoxPresetType.Ok);
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

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting, bool removeConflicting = true)
    {
        conflicting = null;
        BeatmapBPMChangeContainer beatmapBPMChange = BeatmapBPMChangeContainer.SpawnBPMChange(obj as BeatmapBPMChange, ref bpmPrefab);
        beatmapBPMChange.transform.SetParent(GridTransform);
        beatmapBPMChange.UpdateGridPosition();
        LoadedContainers.Add(beatmapBPMChange);
        SelectionController.RefreshMap();
        return beatmapBPMChange;
    }
}

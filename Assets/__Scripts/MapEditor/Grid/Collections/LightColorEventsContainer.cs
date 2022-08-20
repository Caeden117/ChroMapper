using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LightColorEventsContainer : BeatmapObjectContainerCollection
{

    [SerializeField] private GameObject colorPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    [SerializeField] private LightColorEventPlacement lightColorEventPlacement;
    internal PlatformDescriptorV3 platformDescriptor;
    [SerializeField] private GameObject label;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightColorEvent;

    // (groupId, laneIdx) => ordered list
    private Dictionary<(int, int), List<BeatmapLightColorEventData>> nextEventDict = new Dictionary<(int, int), List<BeatmapLightColorEventData>>();

    public override BeatmapObjectContainer CreateContainer()
    {
        return BeatmapLightColorEventContainer.SpawnLightColorEvent(this, null, ref colorPrefab, ref eventAppearanceSo);
    }
    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
    }
    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
    }

    private void Start() => LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        eventAppearanceSo.SetLightColorEventAppearance(con as BeatmapLightColorEventContainer, false);
    }

    private void PlatformLoaded(PlatformDescriptor descriptor)
    {
        platformDescriptor = descriptor as PlatformDescriptorV3;
        StartCoroutine(AfterPlatformLoaded());
    }
    
    private IEnumerator AfterPlatformLoaded()
    {
        yield return null;
        UpdateGrids();
    }

    public void UpdateGrids()
    {
        lightColorEventPlacement.SetGridSize(platformDescriptor.LightsManagersV3.Length);
        for (int i = 0; i < platformDescriptor.LightsManagersV3.Length; ++i)
        {
            var instantiate = Instantiate(label, label.transform.parent);
            instantiate.SetActive(true);
            instantiate.transform.localPosition = new Vector3(i, 0, 0);
            var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
            textMesh.text = platformDescriptor.LightsManagersV3[i].name;
        }
    }

    public void OnPlayToggle(bool isPlaying)
    {
        if (isPlaying)
        {
            LinkAllLightColorEventDatas();
        }
    }

    /// <summary>
    /// Group all events based on group. Then for each group, group them based on lightIndex
    /// </summary>
    public void LinkAllLightColorEventDatas()
    {
        var groupLights = LoadedObjects.Cast<BeatmapLightColorEvent>().GroupBy(x => x.Group);
        foreach (var group in groupLights) // 5 nested for loops!!!!
        {
            var groupId = group.Key;
            var laneIdx = platformDescriptor.GroupIdToLaneIndex(groupId);
            if (laneIdx == -1) continue;
            var lights = platformDescriptor.LightsManagersV3[laneIdx].ControllingLights;

            var lists = new List<BeatmapLightColorEventData>[lights.Count];
            for (int i = 0; i < lists.Length; ++i) lists[i] = new List<BeatmapLightColorEventData>();

            foreach (var colorEvent in group)
            {
                foreach (var colorEventBox in colorEvent.EventBoxes)
                {
                    var filteredLights = colorEventBox.Filter.FilterType == 1
                        ? PlatformDescriptorV3.Partition(lights, colorEventBox.Filter.Section, colorEventBox.Filter.Partition)
                        : PlatformDescriptorV3.Range(lights, colorEventBox.Filter.Section, colorEventBox.Filter.Partition);
                    foreach (var colorEventData in colorEventBox.EventDatas)
                    {
                        foreach (var singleLight in filteredLights)
                        {
                            int lightIdx = singleLight.LightIdx;
                            while (lists[lightIdx].Count > 0 && lists[lightIdx].Last().Time > colorEventData.Time + 1e-3)
                            {
                                lists[lightIdx].RemoveAt(lists[lightIdx].Count - 1);
                            }
                            lists[lightIdx].Add(colorEventData);
                        }
                    }
                }
            }

            for (int lightIdx = 0; lightIdx < lists.Length; ++lightIdx)
            {
                nextEventDict[(groupId, lightIdx)] = lists[lightIdx];
            }
        }
    }

    /// <summary>
    /// Giving group and light index, return next data that has effect on this light
    /// </summary>
    /// <param name="group"></param>
    /// <param name="idx"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool TryGetNextLightColorEventData(int group, int idx, in BeatmapLightColorEventData inData, out BeatmapLightColorEventData data)
    {
        data = null;
        return false;
    }
}

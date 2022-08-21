using System;
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
    /// Group all events based on group. Then for each group, group them based on lightIndex.
    /// Actually the whole lights are precomputed and saved, so that we could get the next event with right brightness and time
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
                float baseTime = colorEvent.Time;
                foreach (var colorEventBox in colorEvent.EventBoxes)
                {
                    var filteredLights = colorEventBox.Filter.FilterType == 1
                        ? PlatformDescriptorV3.Partition(lights, colorEventBox.Filter.Section, colorEventBox.Filter.Partition)
                        : PlatformDescriptorV3.Range(lights, colorEventBox.Filter.Partition, colorEventBox.Filter.Section);

                    float deltaAlpha = colorEventBox.BrightnessDistribution;
                    if (colorEventBox.BrightnessDistributionType == 1) deltaAlpha /= filteredLights.Count();
                    float deltaTime = colorEventBox.Distribution;
                    if (colorEventBox.DistributionType == 1) deltaTime /= filteredLights.Count();

                    foreach (var colorEventData in colorEventBox.EventDatas)
                    {
                        var brightness = colorEventData.Brightness;
                        float extraTime = 0.0f;
                        foreach (var singleLight in filteredLights)
                        {
                            int lightIdx = singleLight.LightIdx;
                            var thisData = new BeatmapLightColorEventData(baseTime + extraTime + colorEventData.Time, 
                                colorEventData.TransitionType, colorEventData.Color, brightness, colorEventData.FlickerFrequency);
                            while (lists[lightIdx].Count > 0 && lists[lightIdx].Last().Time > thisData.Time + 1e-3)
                            {
                                lists[lightIdx].RemoveAt(lists[lightIdx].Count - 1);
                            }
                            lists[lightIdx].Add(thisData);
                            brightness += deltaAlpha;
                        }
                        extraTime += deltaTime;
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
    public bool TryGetNextLightColorEventData(int group, int idx, float time, out BeatmapLightColorEventData data)
    {
        data = null;
        if (nextEventDict.TryGetValue((group, idx), out var list))
        {
            var fakeData = new BeatmapLightColorEventData(time, 0, 0, 0, 0);
            int i = list.BinarySearch(fakeData, new BeatmapObjectComparer());
            if (i < 0)
            {
                i = ~i;
                if (i < list.Count - 1)
                {
                    data = list[i + 1];
                    return true;
                }
                return false;
            }
            if (i != list.Count - 1)
            {
                data = list[i + 1];
                return true;
            }
        }
        return false;
    }
}

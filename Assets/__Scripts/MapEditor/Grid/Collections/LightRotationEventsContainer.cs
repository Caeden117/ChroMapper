using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightRotationEventsContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject rotationPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    internal PlatformDescriptorV3 platformDescriptor;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightRotationEvent;

    private Dictionary<(int, int), List<BeatmapLightRotationEventData>> nextEventDict = new Dictionary<(int, int), List<BeatmapLightRotationEventData>>();

    public override BeatmapObjectContainer CreateContainer()
    {
        return BeatmapLightRotationEventContainer.SpawnLightRotationEvent(this, null, ref rotationPrefab, ref eventAppearanceSo);
    }
    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
    }
    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
    }

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        eventAppearanceSo.SetLightRotationEventAppearance(con as BeatmapLightRotationEventContainer);
    }

    private void Start() => LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;

    private void PlatformLoaded(PlatformDescriptor descriptor)
    {
        platformDescriptor = descriptor as PlatformDescriptorV3;
    }

    public void OnPlayToggle(bool isPlaying)
    {
        if (isPlaying)
        {
            LinkAllLightRotationEventDatas();
        }
    }

    /// <summary>
    /// Group all rotations based on group. Then for each group, group them based on rotationIndex.
    /// Actually the whole rotations are precomputed and saved, so that we could get the next event with right degress and time
    /// </summary>
    public void LinkAllLightRotationEventDatas()
    {
        var groupLights = LoadedObjects.Cast<BeatmapLightRotationEvent>().GroupBy(x => x.Group);
        foreach (var group in groupLights) // 5 nested for loops!!!!
        {
            var groupId = group.Key;
            var laneIdx = platformDescriptor.GroupIdToLaneIndex(groupId);
            if (laneIdx == -1) continue;
            var rotations = platformDescriptor.LightsManagersV3[laneIdx].ControllingRotations;

            var lists = new List<BeatmapLightRotationEventData>[rotations.Count];
            for (int i = 0; i < lists.Length; ++i) lists[i] = new List<BeatmapLightRotationEventData>();

            foreach (var rotationEvent in group)
            {
                float baseTime = rotationEvent.Time;
                foreach (var rotationEventBox in rotationEvent.EventBoxes)
                {
                    var filteredRotations = rotationEventBox.Filter.FilterType == 1
                        ? PlatformDescriptorV3.Partition(rotations, rotationEventBox.Filter.Section, rotationEventBox.Filter.Partition)
                        : PlatformDescriptorV3.Range(rotations, rotationEventBox.Filter.Partition, rotationEventBox.Filter.Section);

                    float deltaDegree = rotationEventBox.RotationDistribution;
                    if (rotationEventBox.RotationDistributionType == 1) deltaDegree /= filteredRotations.Count();
                    float deltaTime = rotationEventBox.Distribution;
                    if (rotationEventBox.DistributionType == 1) deltaTime /= filteredRotations.Count();

                    foreach (var rotationEventData in rotationEventBox.EventDatas)
                    {
                        float degree = rotationEventData.RotationValue;
                        float extraTime = 0.0f;
                        foreach (var singleRotation in filteredRotations)
                        {
                            int rotationIdx = singleRotation.RotationIdx;
                            var thisData = new BeatmapLightRotationEventData(baseTime + extraTime + rotationEventData.Time,
                                rotationEventData.Transition, rotationEventData.EaseType, rotationEventData.AdditionalLoop,
                                degree, rotationEventData.RotationDirection);
                            while (lists[rotationIdx].Count > 0 && lists[rotationIdx].Last().Time > thisData.Time + 1e-3)
                            {
                                lists[rotationIdx].RemoveAt(lists[rotationIdx].Count - 1);
                            }
                            lists[rotationIdx].Add(thisData);
                            degree += deltaDegree;
                            extraTime += deltaTime;
                        }
                    }
                }
            }

            for (int rotationIdx = 0; rotationIdx < lists.Length; ++rotationIdx)
            {
                nextEventDict[(groupId, rotationIdx)] = lists[rotationIdx];
            }
        }
    }

    /// <summary>
    /// Giving group and rotation index, return next data that has effect on this object
    /// </summary>
    /// <param name="group"></param>
    /// <param name="idx"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool TryGetNextLightRotationEventData(int group, int idx, float time, out BeatmapLightRotationEventData data)
    {
        data = null;
        if (nextEventDict.TryGetValue((group, idx), out var list))
        {
            var fakeData = new BeatmapLightRotationEventData(time, 0, 0, 0, 0, 0);
            int i = list.BinarySearch(fakeData, new BeatmapObjectComparer());
            if (i < 0)
            {
                i = ~i;
                if (i < list.Count)
                {
                    if (Mathf.Approximately(list[i].Time, time)) ++i;
                    if (i < list.Count)
                    {
                        data = list[i];
                        return true;
                    }
                }
                return false;
            }
            else if (i < list.Count - 1)
            {
                data = list[i + 1];
                return true;
            }
        }
        return false;
    }
}

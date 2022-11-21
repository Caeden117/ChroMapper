using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public abstract class LightEventsContainerCollectionBase<TBo, TEb, TEbd, TBoc, TBocc, TLightEvent> : BeatmapObjectContainerCollection
    where TBo: BeatmapLightEventBase<TEb, TEbd>
    where TEb: BeatmapLightEventBoxBase<TEbd>, new()
    where TEbd: BeatmapLightEventBoxDataBase, new()
    where TBoc: BeatmapLightEventContainerBase<TBo, TEb, TEbd, TBoc, TBocc, TLightEvent>
    where TBocc: LightEventsContainerCollectionBase<TBo, TEb, TEbd, TBoc, TBocc, TLightEvent>
    where TLightEvent: ILightEventV3
{
    [SerializeField] private GameObject lightPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    internal PlatformDescriptorV3 platformDescriptor;
    [SerializeField] internal EventsContainer eventsContainer;
    internal bool containersUP;

    protected Dictionary<(int, int, int), List<TEbd>> NextEventDict = new Dictionary<(int, int, int), List<TEbd>>();

    protected abstract class StaticGraphEnumerator
    {
        protected TEb EventBox;
        protected TEbd EventData;
        protected int EventDataIdx;
        public abstract IEnumerable<int> AdditonalField();
        public abstract bool AdditonalFieldMatched(int additional, TEb LightEventBox);
        public abstract void DeltaScaleByFilterLimit(
            IEnumerable<TLightEvent> all, IEnumerable<IEnumerable<TLightEvent>> filtered, BeatmapLightEventFilter filter, ref float deltaTime);
        public void InitDelta(TEb lightEventBox, IEnumerable<IEnumerable<TLightEvent>> filteredLightChunks)
        {
            EventBox = lightEventBox;
            InitDeltaImpl(lightEventBox, filteredLightChunks);
        }
        protected abstract void InitDeltaImpl(TEb lightEventBox, IEnumerable<IEnumerable<TLightEvent>> filteredLightChunks);
        public TEbd InitValue(TEbd lightEventData, int eventDataIdx)
        {
            EventData = BeatmapObject.GenerateCopy(lightEventData);
            EventDataIdx = eventDataIdx;
            InitValueImpl(lightEventData, eventDataIdx);
            return BeatmapObject.GenerateCopy(EventData);
        }

        protected abstract void InitValueImpl(TEbd lightEventData, int eventDataIdx);
        public abstract TEbd Next();
    }
    protected abstract StaticGraphEnumerator GraphEnumerator { get; }

    protected abstract List<TLightEvent> GetAllLights(int laneIdx);

    protected virtual void Start() => LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    protected virtual void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
    private void PlatformLoaded(PlatformDescriptor descriptor)
    {
        platformDescriptor = descriptor as PlatformDescriptorV3;
    }

    public override BeatmapObjectContainer CreateContainer()
    {
        return BeatmapLightEventContainerBase<TBo, TEb, TEbd, TBoc, TBocc, TLightEvent>
            .SpawnLightEvent(this as TBocc,  null, ref lightPrefab, ref eventAppearanceSO);
    }
    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var lightContainer = con as TBoc;
        lightContainer.LightEventData = obj as TBo;
        lightContainer.SetLightEventAppearance(eventAppearanceSO, lightContainer, obj.Time, 0);
        lightContainer.SpawnEventDatas(eventAppearanceSO);
    }

    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle += OnPlayToggle;

    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;

    }
    public void OnPlayToggle(bool isPlaying)
    {
        if (isPlaying)
        {
            LinkAllLightEventDatas();
        }
    }
    public void LinkAllLightEventDatas()
    {
        var groupLights = LoadedObjects.Cast<TBo>().GroupBy(x => x.Group);
        foreach (var group in groupLights)
        {
            LinkGroupLightEventDatas(group.Key, group.AsEnumerable());
        }
    }

    public void LinkGroupLightEventDatas(int groupId, IEnumerable<TBo> group)
    {
        var laneIdx = platformDescriptor.GroupIdToLaneIndex(groupId);
        if (laneIdx == -1) return;
        var lights = GetAllLights(laneIdx);

        foreach (var additonalField in GraphEnumerator.AdditonalField())
        {
            foreach (var lightEvent in group)
            {
                float baseTime = lightEvent.Time;
                foreach (var lightEventBox in lightEvent.EventBoxes)
                {
                    if (!GraphEnumerator.AdditonalFieldMatched(additonalField, lightEventBox)) continue;
                    var filteredLightChunks = lightEventBox.Filter.Filter(lights);
                    GraphEnumerator.InitDelta(lightEventBox, filteredLightChunks);
                    // float deltaAlpha = lightEventBox.BrightnessDistribution;
                    // if (lightEventBox.BrightnessDistributionType == 1) deltaAlpha /= BeatmapLightEventFilter.Intervals(filteredLightChunks);
                    float deltaTime = lightEventBox.Distribution;
                    if (lightEventBox.DistributionType == 1) deltaTime /= BeatmapLightEventFilter.Intervals(filteredLightChunks);

                    GraphEnumerator.DeltaScaleByFilterLimit(lights, filteredLightChunks, lightEventBox.Filter, ref deltaTime);

                    for (int i = 0; i < lightEventBox.EventDatas.Count; ++i)
                    {
                        var lightEventData = lightEventBox.EventDatas[i];
                        float extraTime = 0.0f;
                        var thisData = GraphEnumerator.InitValue(lightEventData, i);
                        foreach (var lightChunk in filteredLightChunks)
                        {
                            foreach (var singleLight in lightChunk)
                            {
                                int lightIdx = singleLight.GetIndex();
                                var dictKey = (groupId, lightIdx, additonalField);
                                if (!NextEventDict.ContainsKey(dictKey))
                                    NextEventDict[dictKey] = new List<TEbd>();

                                thisData.Time = baseTime + extraTime + lightEventData.Time;

                                while (NextEventDict[dictKey].Count > 0 && NextEventDict[dictKey].Last().Time > thisData.Time + 1e-3)
                                {
                                    NextEventDict[dictKey].RemoveAt(NextEventDict[dictKey].Count - 1);
                                }
                                NextEventDict[dictKey].Add(thisData);
                            }
                            thisData = GraphEnumerator.Next();
                            // brightness += (i == 0 && lightEventBox.BrightnessAffectFirst == 0) ? 0 : deltaAlpha;
                            extraTime += deltaTime;
                        }
                    }
                }
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
    public bool TryGetNextLightEventData(int group, int idx, int additonal, float time, out TEbd data)
    {
        data = null;
        if (NextEventDict.TryGetValue((group, idx, additonal), out var list))
        {
            var fakeData = new TEbd();
            fakeData.Time = time;
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
            }
            else if (i < list.Count - 1)
            {
                data = list[i + 1];
                return true;
            }
        }
        return false;
    }

    public bool TryGetPreviousLightEventData(int group, int idx, int additional, float time, out TEbd data)
    {
        data = null;
        if (NextEventDict.TryGetValue((group, idx, additional), out var list))
        {
            if (list.Count == 0) return false;
            var fakeData = new TEbd();
            fakeData.Time = time;
            int i = list.BinarySearch(fakeData, new BeatmapObjectComparer());
            if (i < 0)
            {
                i = ~i;
                if (i > 0)
                {
                    i--;
                    data = list[i];
                    return true;
                }
                return false;
            }
            else
            {
                if (i == list.Count) i--;
                data = list[i];
                return true;
            }
        }
        return false;
    }
}

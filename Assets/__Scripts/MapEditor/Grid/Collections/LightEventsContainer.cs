using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightEventsContainer : BeatmapObjectContainerCollection
{

    [SerializeField] private GameObject colorPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightEvent;

    public override BeatmapObjectContainer CreateContainer()
    {
        return BeatmapLightColorEventContainer.SpawnLightColorEvent(this, null, ref colorPrefab, ref eventAppearanceSo);
    }
    internal override void SubscribeToCallbacks()
    {

    }
    internal override void UnsubscribeToCallbacks()
    {

    }

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
    }
}

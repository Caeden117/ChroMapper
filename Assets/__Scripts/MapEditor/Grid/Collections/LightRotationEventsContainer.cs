using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRotationEventsContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject rotationPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightRotationEvent;

    public override BeatmapObjectContainer CreateContainer()
    {
        return BeatmapLightRotationEventContainer.SpawnLightRotationEvent(this, null, ref rotationPrefab, ref eventAppearanceSo);
    }
    internal override void SubscribeToCallbacks()
    {

    }
    internal override void UnsubscribeToCallbacks()
    {

    }

}

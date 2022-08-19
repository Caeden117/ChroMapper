using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRotationEventsContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject rotationPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    internal PlatformDescriptorV3 platformDescriptor;
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
}

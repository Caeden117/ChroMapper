using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightColorEventsContainer : BeatmapObjectContainerCollection
{

    [SerializeField] private GameObject colorPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightColorEvent;

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

    private void Start() => LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        eventAppearanceSo.SetLightColorEventAppearance(con as BeatmapLightColorEventContainer, false);
    }

    private void PlatformLoaded(PlatformDescriptor descriptor)
    {
        StartCoroutine(AfterPlatformLoaded());
    }
    
    private IEnumerator AfterPlatformLoaded()
    {
        yield return null;
        UpdateGrids();
    }

    public void UpdateGrids()
    {

    }
}

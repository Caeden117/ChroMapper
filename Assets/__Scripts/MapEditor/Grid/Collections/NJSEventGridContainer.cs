using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class NJSEventGridContainer : BeatmapObjectContainerCollection<BaseNJSEvent>
{
    [SerializeField] private GameObject njsEventPrefab;
    // [SerializeField] private NJSEventAppearanceSO njsEventAppearanceSo;

    [SerializeField] private CountersPlusController countersPlus;
    
    public override ObjectType ContainerType => ObjectType.NJSEvent;

    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
        UIMode.PreviewModeSwitched += OnUIPreviewModeSwitch;
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
        UIMode.PreviewModeSwitched -= OnUIPreviewModeSwitch;
    }

    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    public override ObjectContainer CreateContainer() => NJSEventContainer.SpawnNJSEvent(null, ref njsEventPrefab);

    // protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    // {
    //     var njsEvent = con as NJSEventContainer;
    //     var njsEventData = obj as BaseNJSEvent;
    //     NJSEventAppearanceSo.SetNJSEventAppearance(njsEvent);
    //     njsEvent.Setup();
    // }

    protected override void OnObjectSpawned(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.NJSEvents);

    protected override void OnObjectDelete(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.NJSEvents);
}

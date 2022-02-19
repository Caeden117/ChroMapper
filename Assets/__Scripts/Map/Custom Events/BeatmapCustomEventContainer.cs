using UnityEngine;

public class BeatmapCustomEventContainer : BeatmapObjectContainer
{
    private CustomEventsContainer collection;
    public BeatmapCustomEvent CustomEventData;

    public override BeatmapObject ObjectData
    {
        get => CustomEventData;
        set => CustomEventData = (BeatmapCustomEvent)value;
    }

    public static BeatmapCustomEventContainer SpawnCustomEvent(BeatmapCustomEvent data,
        CustomEventsContainer collection, ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapCustomEventContainer>();
        container.CustomEventData = data;
        container.collection = collection;
        return container;
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(
            collection.CustomEventTypes.IndexOf(CustomEventData.Type), 0.5f,
            CustomEventData.Time * EditorScaleController.EditorScale);
        UpdateCollisionGroups();
    }
}

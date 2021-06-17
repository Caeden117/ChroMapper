using System;
using UnityEngine;

public class BeatmapCustomEventContainer : BeatmapObjectContainer
{

    public override BeatmapObject objectData { get => customEventData; set => customEventData = (BeatmapCustomEvent)value; }
    public BeatmapCustomEvent customEventData;
    private CustomEventsContainer collection;

    public static BeatmapCustomEventContainer SpawnCustomEvent(BeatmapCustomEvent data, CustomEventsContainer collection, ref GameObject prefab)
    {
        BeatmapCustomEventContainer container = Instantiate(prefab).GetComponent<BeatmapCustomEventContainer>();
        container.customEventData = data;
        container.collection = collection;
        return container;
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(
            collection.CustomEventTypes.IndexOf(customEventData._type), 0.5f, customEventData._time * EditorScaleController.EditorScale);
        UpdateCollisionGroups();
    }
}

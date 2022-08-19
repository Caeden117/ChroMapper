using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapLightRotationEventContainer : BeatmapEventContainer
{
    public BeatmapLightRotationEvent RotationEventData;
    public LightRotationEventsContainer RotationEventsContainer;

    public override BeatmapObject ObjectData { get => RotationEventData; set => RotationEventData = (BeatmapLightRotationEvent)value; }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(
            RotationEventsContainer.platformDescriptor.GroupIdToLaneIndex(RotationEventData.Group) + 0.5f,
            -0.5f,
            RotationEventData.Time * EditorScaleController.EditorScale
            );
    }

    public static BeatmapLightRotationEventContainer SpawnLightRotationEvent(LightRotationEventsContainer rotationEventsContainer, BeatmapLightRotationEvent data,
        ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapLightRotationEventContainer>();
        container.RotationEventData = data;
        container.RotationEventsContainer = rotationEventsContainer;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }
}

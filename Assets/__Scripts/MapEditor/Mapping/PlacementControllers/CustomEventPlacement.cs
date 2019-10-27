using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomEventPlacement : PlacementController<BeatmapCustomEvent, BeatmapCustomEventContainer, CustomEventsContainer>
{
    public override BeatmapAction GenerateAction(BeatmapCustomEventContainer spawned, BeatmapObjectContainer conflicting)
    {
        return new BeatmapObjectPlacementAction(spawned, objectContainerCollection, conflicting);
    }

    public override BeatmapCustomEvent GenerateOriginalData()
    {
        return new BeatmapCustomEvent(0, "", null);
    }

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        int customEventTypeId = Mathf.RoundToInt(instantiatedContainer.transform.position.x - transform.position.x);
        if (customEventTypeId < objectContainerCollection.CustomEventTypes.Count && customEventTypeId >= 0)
            queuedData._type = objectContainerCollection.CustomEventTypes[customEventTypeId];
    }
}

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
        queuedData._type = objectContainerCollection.CustomEventTypes[Mathf.RoundToInt(instantiatedContainer.transform.position.x - transform.position.x)];
    }
}

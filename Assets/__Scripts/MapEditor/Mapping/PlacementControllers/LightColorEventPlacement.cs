using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightColorEventPlacement : PlacementController<BeatmapLightColorEvent, BeatmapLightColorEventContainer, LightColorEventsContainer>
{
    internal PlatformDescriptorV3 platformDescriptor;
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    private int objectGroup = -1;
    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a LightColorEvent.");
    public override BeatmapLightColorEvent GenerateOriginalData() => new BeatmapLightColorEvent();
    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        instantiatedContainer.transform.localPosition = new Vector3(
            instantiatedContainer.transform.localPosition.x,
            0.5f,
            instantiatedContainer.transform.localPosition.z);
        objectGroup = platformDescriptor.LaneIndexToGroupId(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x));
    }
    public override void TransferQueuedToDraggedObject(ref BeatmapLightColorEvent dragged, BeatmapLightColorEvent queued) => throw new System.NotImplementedException();

    /// <summary>
    /// exactly same as <see cref="EventPlacement.SetGridSize(int)"/>
    /// </summary>
    /// <param name="gridSize"></param>
    public void SetGridSize(int gridSize = 16)
    {
        foreach (Transform eachChild in transform)
        {
            switch (eachChild.name)
            {
                case "Event Grid Front Scaling Offset":
                    var newFrontScale = eachChild.transform.localScale;
                    newFrontScale.x = gridSize / 10f;
                    eachChild.transform.localScale = newFrontScale;
                    break;
                case "Event Interface Scaling Offset":
                    var newInterfaceScale = eachChild.transform.localScale;
                    newInterfaceScale.x = gridSize / 10f;
                    eachChild.transform.localScale = newInterfaceScale;
                    break;
            }
        }

        GridChild.Size = gridSize;
    }

    public void UpdateAppearance()
    {
        if (instantiatedContainer is null) RefreshVisuals();
        instantiatedContainer.ColorEventData = queuedData;
        eventAppearanceSO.SetLightColorEventAppearance(instantiatedContainer, false, 0, false);
    }

    public void UpdateData(BeatmapLightColorEvent e)
    {
        queuedData = e;
        UpdateAppearance();
    }

    internal override void ApplyToMap()
    {
        queuedData.Group = objectGroup;
        Debug.Log($"Apply group {objectGroup} to map");
        base.ApplyToMap();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightColorEventPlacement : PlacementController<BeatmapLightColorEvent, BeatmapLightColorEventContainer, LightColorEventsContainer>
{
    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting) => throw new System.NotImplementedException();
    public override BeatmapLightColorEvent GenerateOriginalData() => throw new System.NotImplementedException();
    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint) => throw new System.NotImplementedException();
    public override void TransferQueuedToDraggedObject(ref BeatmapLightColorEvent dragged, BeatmapLightColorEvent queued) => throw new System.NotImplementedException();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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
}

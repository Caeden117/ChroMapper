using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcPlacement : PlacementController<BaseArc, ArcContainer, ArcGridContainer>,
    CMInput.IArcPlacementActions
{
    private static HashSet<BaseObject> SelectedObjects => SelectionController.SelectedObjects;
    

    public void OnSpawnArc(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) return;
        if (!Settings.Instance.Load_MapV3) return;
        var objects = SelectedObjects.ToList();
        if (objects.Count != 2) { return; }
        if(!IsColorNote(objects[0]) || !IsColorNote(objects[1]))
        {
            return;
        }
        var n1 = objects[0] as BaseNote;
        var n2 = objects[1] as BaseNote;
        if (n1.Time > n2.Time) { var t = n1; n1 = n2; n2 = t; }

        var arcData = new V3Arc(n1, n2);
        SpawnArc(arcData);
    }

    public static bool IsColorNote(BaseObject o)
    {
        return o is BaseNote && !(o is BaseBombNote);
    }

    public override BaseArc GenerateOriginalData() => new V3Arc();
    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed an arc.");

    public void SpawnArc(BaseArc arcData)
    {
        var arcContainer = objectContainerCollection;
        arcContainer.SpawnObject(arcData, false);
        BeatmapActionContainer.AddAction(GenerateAction(arcData, new List<BaseObject>()));
    }




    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        return;
    }
    public override void TransferQueuedToDraggedObject(ref BaseArc dragged, BaseArc queued) { }
}

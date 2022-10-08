using System.Collections.Generic;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Base;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcPlacement : PlacementController<IArc, ArcContainer, ArcGridContainer>,
    CMInput.IArcPlacementActions
{
    private static HashSet<IObject> SelectedObjects => SelectionController.SelectedObjects;
    

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
        var n1 = objects[0] as INote;
        var n2 = objects[1] as INote;
        if (n1.Time > n2.Time) { var t = n1; n1 = n2; n2 = t; }
        var arcData = new V3Arc(n1, n2);
        SpawnArc(arcData);
    }

    public static bool IsColorNote(IObject o)
    {
        return o is INote && !(o is V3BombNote);
    }

    public override IArc GenerateOriginalData() => new V3Arc();
    public override BeatmapAction GenerateAction(IObject spawned, IEnumerable<IObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed an arc.");

    public void SpawnArc(IArc arcData)
    {
        var arcContainer = objectContainerCollection;
        arcContainer.SpawnObject(arcData, false);
        BeatmapActionContainer.AddAction(GenerateAction(arcData, new List<IObject>()));
    }




    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        return;
    }
    public override void TransferQueuedToDraggedObject(ref IArc dragged, IArc queued) { }
}

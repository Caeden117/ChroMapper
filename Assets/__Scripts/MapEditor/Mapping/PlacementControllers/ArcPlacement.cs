using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcPlacement : PlacementController<BeatmapArc, BeatmapArcContainer, ArcsContainer>,
    CMInput.IArcPlacementActions
{
    private static HashSet<BeatmapObject> SelectedObjects => SelectionController.SelectedObjects;
    

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
        var n1 = objects[0] as BeatmapNote;
        var n2 = objects[1] as BeatmapNote;
        if (n1.Time > n2.Time) { var t = n1; n1 = n2; n2 = t; }
        if (n1.CutDirection == BeatmapNote.NoteCutDirectionAny || n2.CutDirection == BeatmapNote.NoteCutDirectionAny) { return; }
        var arcData = new BeatmapArc
        {
            Time = n1.Time,
            Color = n1.Type,
            X = n1.LineIndex,
            Y = n1.LineLayer,
            Direction = n1.CutDirection,
            TailTime = n2.Time,
            TailX = n2.LineIndex,
            TailY = n2.LineLayer,
            HeadControlPointLengthMultiplier = 1.0f,
            TailControlPointLengthMultiplier = 1.0f,
            TailCutDirection = n2.CutDirection,
            ArcMidAnchorMode = 0
        };
        SpawnArc(arcData);
    }

    public static bool IsColorNote(BeatmapObject o)
    {
        return o is BeatmapNote && !(o is BeatmapBombNote);
    }

    public override BeatmapArc GenerateOriginalData() => new BeatmapArc();
    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed an arc.");

    public void SpawnArc(BeatmapArc arcData)
    {
        var arcContainer = objectContainerCollection;
        arcContainer.SpawnObject(arcData, false);
        BeatmapActionContainer.AddAction(GenerateAction(arcData, new List<BeatmapObject>()));
    }




    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        return;
    }
    public override void TransferQueuedToDraggedObject(ref BeatmapArc dragged, BeatmapArc queued) { }
}

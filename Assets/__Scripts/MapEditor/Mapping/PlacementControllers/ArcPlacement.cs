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

        var notes = SelectedObjects.Where(obj => IsColorNote(obj)).Cast<BaseNote>().ToList();
        notes.Sort((a, b) => a.Time.CompareTo(b.Time));

        if (!Settings.Instance.Load_MapV3 && notes.Count > 1)
        {
            PersistentUI.Instance.ShowDialogBox("Arc placement is not supported in v2 format.\nConvert map to v3 to place arcs.",
                null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        for (int i = 1; i < notes.Count; i++)
        {
            SpawnArc(notes[i - 1], notes[i]);
        }
    }

    public static bool IsColorNote(BaseObject o)
    {
        return o is BaseNote && !(o is BaseBombNote);
    }

    public override BaseArc GenerateOriginalData() => new V3Arc();
    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed an arc.");

    public void SpawnArc(BaseNote head, BaseNote tail)
    {
        if (head.Time > tail.Time)
        {
            (head, tail) = (tail, head);
        }

        SpawnArc(new V3Arc(head, tail));
    }

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

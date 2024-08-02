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

        var notes = SelectedObjects.Where(IsColorNote).Cast<BaseNote>().ToList();
        notes.Sort((a, b) => a.JsonTime.CompareTo(b.JsonTime));

        if (Settings.Instance.MapVersion == 2 && notes.Count > 1)
        {
            PersistentUI.Instance.ShowDialogBox("Arc placement is not supported in v2 format.\nConvert map to v3 to place arcs.",
                null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        // is there better way than this?
        var generatedObjects = new List<BaseArc>();
        var red = notes.Where(n => n.Color == 0).ToList();
        var blue = notes.Where(n => n.Color == 1).ToList();

        for (var i = 1; i < red.Count; i++)
        {
            generatedObjects.Add(CreateArcData(red[i - 1], red[i]));
        }
        for (var i = 1; i < blue.Count; i++)
        {
            generatedObjects.Add(CreateArcData(blue[i - 1], blue[i]));
        }

        if (generatedObjects.Count > 0)
        {
            foreach (BaseArc arcData in generatedObjects)
            {
                objectContainerCollection.SpawnObject(arcData, false);
            }

            SelectionController.DeselectAll();
            SelectionController.SelectedObjects = new HashSet<BaseObject>(generatedObjects);
            SelectionController.SelectionChangedEvent?.Invoke();
            SelectionController.RefreshSelectionMaterial(false);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(generatedObjects.ToArray(), new List<BaseObject>(), $"Placed {generatedObjects.Count} arcs"));
        }
    }

    public static bool IsColorNote(BaseObject o)
    {
        return o is BaseNote && !(o is BaseBombNote);
    }

    public override BaseArc GenerateOriginalData() => new V3Arc();
    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed an arc.");

    public BaseArc CreateArcData(BaseNote head, BaseNote tail)
    {
        if (head.JsonTime > tail.JsonTime)
        {
            (head, tail) = (tail, head);
        }

        return new V3Arc(head, tail);
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        return;
    }
    public override void TransferQueuedToDraggedObject(ref BaseArc dragged, BaseArc queued) { }
}

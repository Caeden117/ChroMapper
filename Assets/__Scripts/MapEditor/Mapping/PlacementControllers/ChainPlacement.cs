using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ChainPlacement : PlacementController<BaseChain, ChainContainer, ChainGridContainer>, CMInput.IChainPlacementActions
{
    public const int ChainDefaultSpawnCount = 3;
    private static HashSet<BaseObject> SelectedObjects => SelectionController.SelectedObjects;
    [SerializeField] private SelectionController selectionController;
    [FormerlySerializedAs("notesContainer")] [SerializeField] private NoteGridContainer noteGridContainer;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) => 
        new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a chain.");
    public override BaseChain GenerateOriginalData() => new V3Chain();
    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint) => throw new System.NotImplementedException();

    /// <summary>
    /// Perform all check for spawning a chain. Maybe should swap `n1` and `n2` when `n2` is actually pointing to `n1`
    /// </summary>
    /// <param name="context"></param>
    public void OnSpawnChain(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) return;
        if (!Settings.Instance.Load_MapV3) return;
        var objects = SelectedObjects.ToList();
        if (objects.Count != 2) { return; }
        if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
        {
            return;
        }
        var n1 = objects[0] as BaseNote;
        var n2 = objects[1] as BaseNote;
        if (n1.Time > n2.Time) (n1, n2) = (n2, n1);
        if (n1.CutDirection == (int)NoteCutDirection.Any) { return; }
        var chainData = new V3Chain(n1, n2);
        SpawnChain(chainData, n1, n2);
    }

    public void SpawnChain(BaseChain chainData, BaseNote headNote, BaseNote toDeleteNote)
    {
        var chainContainer = objectContainerCollection;
        chainContainer.SpawnObject(chainData, false);
        SelectionController.Deselect(headNote);
        var conflict = new List<BaseObject>(SelectedObjects);
        selectionController.Delete(false);
        BeatmapActionContainer.AddAction(GenerateAction(chainData, conflict));
    }

    public override void TransferQueuedToDraggedObject(ref BaseChain dragged, BaseChain queued) { }
}

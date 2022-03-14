using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChainPlacement : PlacementController<BeatmapChain, BeatmapChainContainer, ChainsContainer>, CMInput.IChainPlacementActions
{
    public const int ChainDefaultSpawnCount = 3;
    private static HashSet<BeatmapObject> SelectedObjects => SelectionController.SelectedObjects;
    [SerializeField] private SelectionController selectionController;

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting) => 
        new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a chain.");
    public override BeatmapChain GenerateOriginalData() => throw new System.NotImplementedException();
    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint) => throw new System.NotImplementedException();

    public void OnSpawnChain(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) return;
        if (!Settings.Instance.Load_MapV3) return;
        var objects = SelectedObjects.ToList();
        if (objects.Count != 2) { return; }
        if (!SliderPlacement.IsColorNote(objects[0]) || !SliderPlacement.IsColorNote(objects[1]))
        {
            return;
        }
        var n1 = objects[0] as BeatmapNote;
        var n2 = objects[1] as BeatmapNote;
        if (n1.Time > n2.Time) { var t = n1; n1 = n2; n2 = t; }
        if (n1.CutDirection == BeatmapNote.NoteCutDirectionAny) { return; }
        var chainData = new BeatmapChain
        {
            B = n1.Time,
            C = n1.Type,
            X = n1.LineIndex,
            Y = n1.LineLayer,
            D = n1.CutDirection,
            Tb = n2.Time,
            Tx = n2.LineIndex,
            Ty = n2.LineLayer,
            Sc = ChainDefaultSpawnCount,
            S = 1.0f
        };
        SpawnChain(chainData);
    }

    public void SpawnChain(BeatmapChain chainData)
    {
        var chainContainer = objectContainerCollection;
        chainContainer.SpawnObject(chainData, false);
        BeatmapActionContainer.AddAction(GenerateAction(chainData, new List<BeatmapObject>(SelectedObjects)));
        selectionController.Delete(false); 
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapChain dragged, BeatmapChain queued) => throw new System.NotImplementedException();


    private void Start()
    {
        
    }
    private void Update()
    {

    }
}

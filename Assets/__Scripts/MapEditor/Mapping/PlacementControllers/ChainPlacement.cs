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
    [SerializeField] private NotesContainer notesContainer;

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
        if (n1.Time > n2.Time) (n1, n2) = (n2, n1);
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
        SpawnChain(chainData, n1, n2);
    }

    public void SpawnChain(BeatmapChain chainData, BeatmapNote headNote, BeatmapNote toDeleteNote)
    {
        var chainContainer = objectContainerCollection;
        chainContainer.SpawnObject(chainData, false);
        SelectionController.Deselect(headNote);
        var conflict = new List<BeatmapObject>(SelectedObjects);
        selectionController.Delete(false);
        BeatmapActionContainer.AddAction(GenerateAction(chainData, conflict));
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapChain dragged, BeatmapChain queued) => throw new System.NotImplementedException();


    private void Start()
    {
        
    }
    private void Update()
    {

    }

}

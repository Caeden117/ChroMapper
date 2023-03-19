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
    [FormerlySerializedAs("notesContainer")][SerializeField] private NoteGridContainer noteGridContainer;

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

        var notes = SelectedObjects.Where(obj => IsColorNote(obj)).Cast<BaseNote>().ToList();
        notes.Sort((a, b) => a.Time.CompareTo(b.Time));

        if (!Settings.Instance.Load_MapV3 && notes.Count > 1)
        {
            PersistentUI.Instance.ShowDialogBox("Chain placement is not supported in v2 format.\nConvert map to v3 to place chains.",
                null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        var generatedObjects = new List<BaseChain>();
        var tailNotes = new List<BaseNote>();
        for (int i = 1; i < notes.Count; i++)
        {
            if (TryCreateChainData(notes[i - 1], notes[i], out var chain, out var tailNote))
            {
                tailNotes.Add(tailNote);
                generatedObjects.Add(chain);
            }
        }

        if (generatedObjects.Count > 0)
        {
            SelectionController.DeselectAll();
            SelectionController.SelectedObjects = new HashSet<BaseObject>(tailNotes);
            selectionController.Delete(false);

            foreach (BaseChain chainData in generatedObjects)
            {
                objectContainerCollection.SpawnObject(chainData, false); ;
            }

            SelectionController.SelectedObjects = new HashSet<BaseObject>(generatedObjects);
            SelectionController.SelectionChangedEvent?.Invoke();
            SelectionController.RefreshSelectionMaterial(false);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(generatedObjects.ToArray(), tailNotes, $"Placed {generatedObjects.Count} chains"));
        }
    }

    private bool IsColorNote(BaseObject o) => o is BaseNote && !(o is BaseBombNote);

    public bool TryCreateChainData(BaseNote head, BaseNote tail, out BaseChain chain, out BaseNote tailNote)
    {
        if (head.Time > tail.Time)
        {
            (head, tail) = (tail, head);
        }

        tailNote = tail;

        if (head.CutDirection == (int)NoteCutDirection.Any)
        {
            chain = null;
            return false;
        }
        else
        {
            chain = new V3Chain(head, tail);
            return true;
        }
    }

    public override void TransferQueuedToDraggedObject(ref BaseChain dragged, BaseChain queued) { }
}

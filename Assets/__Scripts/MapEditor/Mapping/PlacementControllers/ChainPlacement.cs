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
    private static HashSet<BaseObject> SelectedObjects => SelectionController.SelectedObjects;
    [SerializeField] private SelectionController selectionController;
    [FormerlySerializedAs("notesContainer")][SerializeField] private NoteGridContainer noteGridContainer;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a chain.");
    public override BaseChain GenerateOriginalData() => new BaseChain();
    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint) => throw new System.NotImplementedException();

    /// <summary>
    /// Perform all check for spawning a chain. Maybe should swap `n1` and `n2` when `n2` is actually pointing to `n1`
    /// </summary>
    /// <param name="context"></param>
    public void OnSpawnChain(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) return;

        var notes = SelectedObjects.Where(IsColorNote).Cast<BaseNote>().ToList();
        notes.Sort((a, b) => a.JsonTime.CompareTo(b.JsonTime));

        if (Settings.Instance.MapVersion == 2 && notes.Count > 1)
        {
            PersistentUI.Instance.ShowDialogBox("Chain placement is not supported in v2 format.\nConvert map to v3 to place chains.",
                null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        var removedTailNotes = new List<BaseNote>();
        var generatedObjects = new List<BaseChain>();

        // is there better way than this?
        var redNotes = notes.Where(n => n.Color == (int)NoteColor.Red).ToList();
        var blueNotes = notes.Where(n => n.Color == (int)NoteColor.Blue).ToList();

        for (var i = 1; i < redNotes.Count; i++)
        {
            if (TryCreateChainData(redNotes[i - 1], redNotes[i], out var chain, out var tailNote))
            {
                removedTailNotes.Add(tailNote);
                generatedObjects.Add(chain);
            }
        }
        
        for (var i = 1; i < blueNotes.Count; i++)
        {
            if (TryCreateChainData(blueNotes[i - 1], blueNotes[i], out var chain, out var tailNote))
            {
                removedTailNotes.Add(tailNote);
                generatedObjects.Add(chain);
            }
        }

        if (generatedObjects.Count > 0)
        {
            SelectionController.DeselectAll();
            SelectionController.SelectedObjects = new HashSet<BaseObject>(removedTailNotes);
            selectionController.Delete(false);

            foreach (var chainData in generatedObjects)
            {
                objectContainerCollection.SpawnObject(chainData, false);
            }

            SelectionController.SelectedObjects = new HashSet<BaseObject>(generatedObjects);
            SelectionController.SelectionChangedEvent?.Invoke();
            SelectionController.RefreshSelectionMaterial(false);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(generatedObjects.ToArray(), removedTailNotes, $"Placed {generatedObjects.Count} chains"));
        }
    }

    private static bool IsColorNote(BaseObject o) => ArcPlacement.IsColorNote(o);

    public bool TryCreateChainData(BaseNote head, BaseNote tail, out BaseChain chain, out BaseNote tailNote)
    {
        if (head.JsonTime > tail.JsonTime)
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
            chain = new BaseChain(head, tail);
            return true;
        }
    }

    public override void TransferQueuedToDraggedObject(ref BaseChain dragged, BaseChain queued) { }
}

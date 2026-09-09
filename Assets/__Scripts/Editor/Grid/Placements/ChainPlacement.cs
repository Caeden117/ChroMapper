using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ChainPlacement : BasePlacement<BaseChain, ChainContainer, ChainGridContainer>,
                              CMInput.IChainPlacementActions
{
    [SerializeField] private SelectionController selectionController;

    [FormerlySerializedAs("notesContainer")] [SerializeField]
    private NoteGridContainer noteGridContainer;

    [NonSerialized] public float Squish = Settings.Instance.DefaultChainSquish;
    [NonSerialized] public int SliceCount = Settings.Instance.DefaultChainSliceCount;

    /// <summary>
    ///     Perform all check for spawning a chain. Maybe should swap `n1` and `n2` when `n2` is actually pointing to `n1`
    /// </summary>
    /// <param name="context"></param>
    public void OnSpawnChain(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) return;

        SpawnChainFromSelection();
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed a chain.");

    protected override BaseChain GenerateOriginalData() => new();

    public int SpawnChainFromSelection()
    {
        var allNotes = SelectionController.SelectedObjects.Where(IsColorNote).Cast<BaseNote>().ToList();
        allNotes.Sort((a, b) => a.JsonTime.CompareTo(b.JsonTime));

        if (Settings.Instance.MapVersion == 2 && allNotes.Count > 1)
        {
            PersistentUI.Instance.ShowDialogBox(
                "Chain placement is not supported in v2 format.\nConvert map to v3 to place chains.",
                null,
                PersistentUI.DialogBoxPresetType.Ok);
            return 0;
        }

        var removedTailNotes = new List<BaseNote>();
        var generatedChains = new List<BaseChain>();
        var modifiedHeadOld = new List<BaseNote>();
        var modifiedHeadNew = new List<BaseNote>();

        var notesByHand = allNotes.GroupBy(n => n.Type);

        foreach (var handGroup in notesByHand)
        {
            var notes = handGroup.ToList();
            BaseNote head = null;

            foreach (var note in notes)
            {
                if (head == null)
                {
                    head = note;
                    continue;
                }

                var tail = note;

                if (head.CutDirection == (int)NoteCutDirection.Any &&
                    tail.CutDirection == (int)NoteCutDirection.Any)
                    continue;

                if (head.JsonTime > tail.JsonTime + 0.001f) (head, tail) = (tail, head);

                var misaligned = !CommonBeatmapUtils.HeadPointsTowardTail(head, tail);

                if (Mathf.Abs(head.JsonTime - tail.JsonTime) < 0.001f && misaligned)
                    (head, tail) = (tail, head);

                if (!TryCreateChainDataInternal(head, tail, out var chain, out var dotHeadAngle))
                    continue;

                if (head.CutDirection == (int)NoteCutDirection.Any)
                {
                    var modified = BeatmapFactory.Clone(head);
                    modified.CutDirection = (int)NoteCutDirection.Any;
                    modified.AngleOffset = (int)dotHeadAngle;
                    modifiedHeadOld.Add(head);
                    modifiedHeadNew.Add(modified);
                }

                chain.SliceCount = SliceCount;
                chain.Squish = Squish;

                generatedChains.Add(chain);
                removedTailNotes.Add(tail);

                head = null;
            }
        }

        if (generatedChains.Count > 0)
        {
            var spawned = new List<BaseObject>(generatedChains);
            spawned.AddRange(modifiedHeadNew);
            var deleted = new List<BaseObject>(removedTailNotes);
            deleted.AddRange(modifiedHeadOld);

            var selectedBefore = new HashSet<BaseObject>(SelectionController.SelectedObjects);
            var selectedAfter = new HashSet<BaseObject>(selectedBefore);
            selectedAfter.UnionWith(spawned);
            foreach (var obj in deleted) selectedAfter.Remove(obj);

            var finalSelection = new HashSet<BaseObject>(generatedChains);

            SelectionController.DeselectAll();

            foreach (var obj in deleted)
                BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType)
                    .DeleteObject(obj, false, false);

            foreach (var obj in spawned)
                BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType)
                    .SpawnObject(obj, false, false);

            BeatmapObjectContainerCollection.RefreshAllPools();

            SelectionController.SelectedObjects = finalSelection;
            SelectionController.OnSelectionChanged?.Invoke();
            SelectionController.RefreshSelectionMaterial(false);

            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    spawned.ToArray(),
                    deleted.ToArray(),
                    $"Placed {generatedChains.Count} chain(s)"));
        }

        return generatedChains.Count;
    }

    private static bool IsColorNote(BaseObject o) => ArcPlacement.IsColorNote(o);

    public bool TryCreateChainData(BaseNote head, BaseNote tail, out BaseChain chain, out BaseNote tailNote)
    {
        if (head.JsonTime > tail.JsonTime) (head, tail) = (tail, head);

        tailNote = tail;

        if (TryCreateChainDataInternal(head, tail, out chain, out _))
        {
            chain.SliceCount = SliceCount;
            chain.Squish = Squish;
            return true;
        }

        chain = null;
        return false;
    }

    private bool TryCreateChainDataInternal(BaseNote head, BaseNote tail, out BaseChain chain, out float dotHeadAngle)
    {
        dotHeadAngle = 0f;

        if (head.CutDirection == (int)NoteCutDirection.Any)
        {
            // work backwards from the tail (which has an arrow) to get a reasonable
            // estimate of a good cut direction for the head
            var delta = head.GetPosition() - tail.GetPosition();
            if (tail.CutDirection == (int)NoteCutDirection.Any || delta.sqrMagnitude < 0.1f)
            {
                chain = null;
                return false;
            }

            delta.Normalize();
            var tailCutVector = CommonBeatmapUtils.AngleToVector(CommonBeatmapUtils.GetAngle(tail.CutDirection));

            const float ReflectDotCoeff = 2.5f;
            var headCutVector = -(tailCutVector - ReflectDotCoeff * Vector2.Dot(tailCutVector, delta) * delta).normalized;
            var headAngle = CommonBeatmapUtils.VectorToAngle(headCutVector);
            var headCutDirection = CommonBeatmapUtils.AngleToCutDirection(headAngle, out _);

            chain = new BaseChain(head, tail)
            {
                CutDirection = (int)headCutDirection
            };
            dotHeadAngle = CommonBeatmapUtils.GetAngle((int)headCutDirection);
            return true;
        }

        chain = new BaseChain(head, tail);
        return true;
    }
}


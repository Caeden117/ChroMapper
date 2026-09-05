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

    private static HashSet<BaseObject> SelectedObjects => SelectionController.SelectedObjects;

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
        var allNotes = SelectedObjects.Where(IsColorNote).Cast<BaseNote>().ToList();
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

                var misaligned = !HeadPointsTowardTail(head, tail);

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
            var tailCutVector = AngleToVector(GetAngle(tail.CutDirection));

            const float ReflectDotCoeff = 2.5f;
            var headCutVector = -(tailCutVector - ReflectDotCoeff * Vector2.Dot(tailCutVector, delta) * delta).normalized;
            var headAngle = VectorToAngle(headCutVector);
            var headCutDirection = AngleToCutDirection(headAngle, out _);

            chain = new BaseChain(head, tail)
            {
                CutDirection = (int)headCutDirection
            };
            dotHeadAngle = GetAngle((int)headCutDirection);
            return true;
        }

        chain = new BaseChain(head, tail);
        return true;
    }

    private static readonly float[] CutDirectionAngles =
    {
        180f, 0f, 270f, 90f, 225f, 135f, 315f, 45f, 0f
    };

    private static float GetAngle(int cutDirection)
    {
        if (cutDirection < 0 || cutDirection >= CutDirectionAngles.Length)
            throw new ArgumentOutOfRangeException(nameof(cutDirection));
        return CutDirectionAngles[cutDirection];
    }

    private static Vector2 AngleToVector(float angle)
    {
        angle *= Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
    }

    private static float VectorToAngle(Vector2 vector)
    {
        vector.Normalize();
        return Mathf.Atan2(vector.x, -vector.y) * Mathf.Rad2Deg;
    }

    private static NoteCutDirection AngleToCutDirection(float angle, out float angleOffset, bool useAny = false)
    {
        if (useAny)
        {
            angleOffset = angle;
            return NoteCutDirection.Any;
        }

        angle = Mathf.Repeat(angle, 360f);

        var bestDir = 0;
        var bestDelta = float.MaxValue;
        for (var i = 0; i < CutDirectionAngles.Length; i++)
        {
            var delta = Mathf.DeltaAngle(angle, CutDirectionAngles[i]);
            if (Mathf.Abs(delta) < Mathf.Abs(bestDelta))
            {
                bestDelta = delta;
                bestDir = i;
            }
        }

        angleOffset = angle - CutDirectionAngles[bestDir];
        return (NoteCutDirection)bestDir;
    }

    private static float GetOverallCutAngle(BaseNote note)
    {
        var angle = GetAngle(note.CutDirection);
        if (note.AngleOffset != 0) angle += note.AngleOffset;
        return angle;
    }

    private static Vector2 GetOverallCutVector(BaseNote note)
    {
        var angle = GetOverallCutAngle(note);
        return AngleToVector(angle);
    }

    private static bool HeadPointsTowardTail(BaseNote head, BaseNote tail)
    {
        var headDir = head.CutDirection == (int)NoteCutDirection.Any ? Vector2.zero : GetOverallCutVector(head);
        var tailDir = tail.CutDirection == (int)NoteCutDirection.Any ? Vector2.zero : GetOverallCutVector(tail);
        if (Vector2.Dot(headDir, tailDir) < -0.9f)
            return false;

        var averageDir = (headDir + tailDir).normalized;
        // if both are dots, averageDir is zero; treat as not misaligned so no swap
        if (averageDir.sqrMagnitude < 0.001f) return true;

        var headDot = Vector2.Dot(head.GetPosition(), averageDir);
        var tailDot = Vector2.Dot(tail.GetPosition(), averageDir);

        return headDot < tailDot;
    }
}

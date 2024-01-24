using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V2;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class BoxSelectionPlacementController : PlacementController<BaseEvent, EventContainer, EventGridContainer>,
    CMInput.IBoxSelectActions
{
    [SerializeField] public CustomEventGridContainer CustomCollection;
    [SerializeField] public EventGridContainer EventGridContainer;
    [SerializeField] public CreateEventTypeLabels Labels;

    private readonly HashSet<BaseObject> selected = new HashSet<BaseObject>();

    private readonly List<ObjectType> selectedTypes = new List<ObjectType>();
    private HashSet<BaseObject> alreadySelected = new HashSet<BaseObject>();

    private bool keybindPressed;
    private Vector3 originPos;
    private Intersections.IntersectionHit previousHit;
    private Vector3 transformed;
    public static bool IsSelecting { get; private set; }

    [HideInInspector] protected override bool CanClickAndDrag { get; set; } = false;

    public override bool IsValid => Settings.Instance.BoxSelect && (keybindPressed || IsSelecting);

    public override int PlacementXMin => int.MinValue;

    public override int PlacementXMax => int.MaxValue;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || instantiatedContainer is null) return;
        Gizmos.color = Color.red;
        var boxyBoy = instantiatedContainer.GetComponent<BoxCollider>();
        if (boxyBoy == null) return;
        var bounds = new Bounds
        {
            center = boxyBoy.bounds.center,
            size = instantiatedContainer.transform.lossyScale / 2f
        };
        Gizmos.DrawMesh(instantiatedContainer.GetComponentInChildren<MeshFilter>().mesh, bounds.center,
            instantiatedContainer.transform.rotation, bounds.size);
    }

    public void OnActivateBoxSelect(InputAction.CallbackContext context) => keybindPressed = context.performed;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) => null;

    // TODO: v3 check?
    public override BaseEvent GenerateOriginalData() => new V2Event(float.MaxValue, 69, 420);

    protected override bool TestForType<T>(Intersections.IntersectionHit hit, ObjectType type)
    {
        if (base.TestForType<T>(hit, type))
        {
            selectedTypes.Add(type);
            return true;
        }

        return false;
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        previousHit = hit;
        transformed = transformedPoint;

        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);
        roundedHit = new Vector3(
            Mathf.Ceil(Math.Min(Math.Max(roundedHit.x, Bounds.min.x + 0.01f), Bounds.max.x)),
            Mathf.Ceil(Math.Min(Math.Max(roundedHit.y, 0.01f), 3f)),
            roundedHit.z
        );
        instantiatedContainer.transform.localPosition = roundedHit - new Vector3(0.5f, 1, 0);
        if (!IsSelecting)
        {
            Bounds = default;
            selectedTypes.Clear();
            TestForType<EventPlacement>(hit, ObjectType.Event);
            TestForType<NotePlacement>(hit, ObjectType.Note);
            TestForType<ObstaclePlacement>(hit, ObjectType.Obstacle);
            TestForType<CustomEventPlacement>(hit, ObjectType.CustomEvent);
            TestForType<BPMChangePlacement>(hit, ObjectType.BpmChange);
            if (Settings.Instance.Load_MapV3)
            {
                TestForType<ArcPlacement>(hit, ObjectType.Arc);
                TestForType<ChainPlacement>(hit, ObjectType.Chain);
            }

            instantiatedContainer.transform.localScale = Vector3.right + Vector3.up;
            var localScale = instantiatedContainer.transform.localScale;
            var localpos = instantiatedContainer.transform.localPosition;
            instantiatedContainer.transform.localPosition -= new Vector3(localScale.x / 2, 0, 0);
        }
        else
        {
            var originShove = originPos;
            float xOffset = 0;
            float yOffset = 0;

            // When moving from right to left, move the origin to the right and make
            // the selection larger as the origin points are on the left
            if (roundedHit.x <= originPos.x + 1)
            {
                xOffset = -1;
                originShove.x += 1;
            }

            if (roundedHit.y <= originPos.y)
            {
                yOffset = -1;
                originShove.y += 1;
            }

            instantiatedContainer.transform.localPosition = originShove;
            var newLocalScale = roundedHit + new Vector3(xOffset, yOffset, 0.5f) - originShove;

            var newLocalScaleY = Mathf.Max(newLocalScale.y, 1);
            if (yOffset < 0) newLocalScaleY = Mathf.Min(-1, newLocalScale.y);

            newLocalScale = new Vector3(newLocalScale.x, newLocalScaleY, newLocalScale.z);
            instantiatedContainer.transform.localScale = newLocalScale;

            var startSongBpmBeat = instantiatedContainer.transform.localPosition.z / EditorScaleController.EditorScale;
            var endSongBpmBeat = (instantiatedContainer.transform.localPosition.z + newLocalScale.z) /
                          EditorScaleController.EditorScale;
            if (startSongBpmBeat > endSongBpmBeat) (startSongBpmBeat, endSongBpmBeat) = (endSongBpmBeat, startSongBpmBeat);

            SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(startSongBpmBeat, endSongBpmBeat, true, true, true, (bocc, bo) =>
            {
                if (!selectedTypes.Contains(bo.ObjectType)) return; // Must be a type we can select

                var left = instantiatedContainer.transform.localPosition.x +
                           instantiatedContainer.transform.localScale.x;
                var right = instantiatedContainer.transform.localPosition.x;
                if (right < left) (left, right) = (right, left);

                var top = instantiatedContainer.transform.localPosition.y +
                          instantiatedContainer.transform.localScale.y;
                var bottom = instantiatedContainer.transform.localPosition.y;
                if (top < bottom) (top, bottom) = (bottom, top);

                var p = new Vector2(left, bottom);

                if (bo is IObjectBounds obj)
                {
                    p = obj.GetCenter();
                }
                else if (bo is BaseBpmEvent)
                {
                    // Bpm events are in a separate single lane so we don't need to get position
                }
                else if (bo is BaseEvent evt)
                {
                    var position = evt.GetPosition(Labels, EventGridContainer.PropagationEditing,
                        EventGridContainer.EventTypeToPropagate);

                    // Not visible = notselectable
                    if (position == null) return;

                    p = new Vector2(position?.x + Bounds.min.x ?? 0, position?.y ?? 0);
                }
                else if (bo is BaseCustomEvent custom)
                {
                    p = new Vector2(CustomCollection.CustomEventTypes.IndexOf(custom.Type) + Bounds.min.x + 0.5f,
                        0.5f);
                }

                // Check if calculated position is outside bounds
                if (p.x < left || p.x > right || p.y < bottom || p.y >= top) return;

                if (!alreadySelected.Contains(bo) && selected.Add(bo))
                    SelectionController.Select(bo, true, false, false);
            });

            foreach (var combinedObj in SelectionController.SelectedObjects.ToArray())
            {
                if (!selected.Contains(combinedObj) && !alreadySelected.Contains(combinedObj))
                    SelectionController.Deselect(combinedObj, false);
            }

            selected.Clear();
        }
    }

    public override void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        if (!IsValid && IsSelecting)
            StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
        base.OnMousePositionUpdate(context);
    }

    internal override void ApplyToMap()
    {
        if (!IsSelecting)
        {
            IsSelecting = true;
            originPos = instantiatedContainer.transform.localPosition;
            alreadySelected = new HashSet<BaseObject>(SelectionController.SelectedObjects);
        }
        else
        {
            StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
            SelectionController.RefreshSelectionMaterial(selected.Any());
            SelectionController.SelectionChangedEvent?.Invoke();
            OnPhysicsRaycast(previousHit, transformed);
        }
    }

    private IEnumerator WaitABitFuckOffOtherPlacementControllers()
    {
        yield return new WaitForSeconds(0.1f);
        IsSelecting = false;
        selected.Clear(); // oh shit turned out i didnt need to rewrite the whole thing, just move it over here
        OnPhysicsRaycast(previousHit, transformed);
    }

    public override void CancelPlacement()
    {
        IsSelecting = false;
        foreach (var selectedObject in selected) SelectionController.Deselect(selectedObject);
    }

    public override void TransferQueuedToDraggedObject(ref BaseEvent dragged, BaseEvent queued) { }
}

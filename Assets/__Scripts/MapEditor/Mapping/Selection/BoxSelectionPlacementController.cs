using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class BoxSelectionPlacementController : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>,
    CMInput.IBoxSelectActions
{
    [FormerlySerializedAs("customCollection")] public CustomEventsContainer CustomCollection;
    [FormerlySerializedAs("eventsContainer")] public EventsContainer EventsContainer;
    [FormerlySerializedAs("labels")] public CreateEventTypeLabels Labels;

    private readonly HashSet<BeatmapObject> selected = new HashSet<BeatmapObject>();

    private readonly List<BeatmapObject.ObjectType> selectedTypes = new List<BeatmapObject.ObjectType>();
    private HashSet<BeatmapObject> alreadySelected = new HashSet<BeatmapObject>();

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
        var bounds = new Bounds
        {
            center = boxyBoy.bounds.center,
            size = instantiatedContainer.transform.lossyScale / 2f
        };
        Gizmos.DrawMesh(instantiatedContainer.GetComponentInChildren<MeshFilter>().mesh, bounds.center,
            instantiatedContainer.transform.rotation, bounds.size);
    }

    public void OnActivateBoxSelect(InputAction.CallbackContext context) => keybindPressed = context.performed;

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting) => null;

    public override MapEvent GenerateOriginalData() => new MapEvent(float.MaxValue, 69, 420);

    protected override bool TestForType<T>(Intersections.IntersectionHit hit, BeatmapObject.ObjectType type)
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
            TestForType<EventPlacement>(hit, BeatmapObject.ObjectType.Event);
            TestForType<NotePlacement>(hit, BeatmapObject.ObjectType.Note);
            TestForType<ObstaclePlacement>(hit, BeatmapObject.ObjectType.Obstacle);
            TestForType<CustomEventPlacement>(hit, BeatmapObject.ObjectType.CustomEvent);
            TestForType<BPMChangePlacement>(hit, BeatmapObject.ObjectType.BpmChange);
            if (Settings.Instance.Load_MapV3)
            {
                TestForType<ArcPlacement>(hit, BeatmapObject.ObjectType.Arc);
                TestForType<ChainPlacement>(hit, BeatmapObject.ObjectType.Chain);
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

            var startBeat = instantiatedContainer.transform.localPosition.z / EditorScaleController.EditorScale;
            var endBeat = (instantiatedContainer.transform.localPosition.z + newLocalScale.z) /
                          EditorScaleController.EditorScale;
            if (startBeat > endBeat) (startBeat, endBeat) = (endBeat, startBeat);

            SelectionController.ForEachObjectBetweenTimeByGroup(startBeat, endBeat, true, true, true, (bocc, bo) =>
            {
                if (!selectedTypes.Contains(bo.BeatmapType)) return; // Must be a type we can select

                var left = instantiatedContainer.transform.localPosition.x +
                           instantiatedContainer.transform.localScale.x;
                var right = instantiatedContainer.transform.localPosition.x;
                if (right < left) (left, right) = (right, left);

                var top = instantiatedContainer.transform.localPosition.y +
                          instantiatedContainer.transform.localScale.y;
                var bottom = instantiatedContainer.transform.localPosition.y;
                if (top < bottom) (top, bottom) = (bottom, top);

                var p = new Vector2(left, bottom);

                if (bo is IBeatmapObjectBounds obj)
                {
                    p = obj.GetCenter();
                }
                else if (bo is MapEvent evt)
                {
                    var position = evt.GetPosition(Labels, EventsContainer.PropagationEditing,
                        EventsContainer.EventTypeToPropagate);

                    // Not visible = notselectable
                    if (position == null) return;

                    p = new Vector2(position?.x + Bounds.min.x ?? 0, position?.y ?? 0);
                }
                else if (bo is BeatmapCustomEvent custom)
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
            alreadySelected = new HashSet<BeatmapObject>(SelectionController.SelectedObjects);
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

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued) { }
}

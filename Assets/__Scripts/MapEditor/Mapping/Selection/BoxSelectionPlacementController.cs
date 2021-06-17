using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoxSelectionPlacementController : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>, CMInput.IBoxSelectActions
{
    public static bool IsSelecting { get; private set; } = false;
    private Vector3 originPos;
    private Intersections.IntersectionHit previousHit;
    private Vector3 transformed;

    private bool keybindPressed = false;

    private HashSet<BeatmapObject> selected = new HashSet<BeatmapObject>();
    private HashSet<BeatmapObject> alreadySelected = new HashSet<BeatmapObject>();

    private List<BeatmapObject.Type> SelectedTypes = new List<BeatmapObject.Type>();

    [HideInInspector] protected override bool CanClickAndDrag { get; set; } = false;

    public override bool IsValid => Settings.Instance.BoxSelect && (keybindPressed || IsSelecting);

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting) => null;

    public override MapEvent GenerateOriginalData() => new MapEvent(float.MaxValue, 69, 420);

    public override int PlacementXMin => int.MinValue;

    public override int PlacementXMax => int.MaxValue;

    public CustomEventsContainer customCollection;
    public EventsContainer eventsContainer;
    public CreateEventTypeLabels labels;

    protected override bool TestForType<T>(Intersections.IntersectionHit hit, BeatmapObject.Type type)
    {
        if (base.TestForType<T>(hit, type))
        {
            SelectedTypes.Add(type);
            return true;
        }
        return false;
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        previousHit = hit;
        transformed = transformedPoint;

        Vector3 roundedHit = parentTrack.InverseTransformPoint(hit.Point);
        roundedHit = new Vector3(
            Mathf.Ceil(Math.Min(Math.Max(roundedHit.x, bounds.min.x + 0.01f), bounds.max.x)),
            Mathf.Ceil(Math.Min(Math.Max(roundedHit.y, 0.01f), 3f)),
            roundedHit.z
        );
        instantiatedContainer.transform.localPosition = roundedHit - new Vector3(0.5f, 1, 0);
        if (!IsSelecting)
        {
            bounds = default;
            SelectedTypes.Clear();
            TestForType<EventPlacement>(hit, BeatmapObject.Type.EVENT);
            TestForType<NotePlacement>(hit, BeatmapObject.Type.NOTE);
            TestForType<ObstaclePlacement>(hit, BeatmapObject.Type.OBSTACLE);
            TestForType<CustomEventPlacement>(hit, BeatmapObject.Type.CUSTOM_EVENT);
            TestForType<BPMChangePlacement>(hit, BeatmapObject.Type.BPM_CHANGE);

            instantiatedContainer.transform.localScale = Vector3.right + Vector3.up;
            Vector3 localScale = instantiatedContainer.transform.localScale;
            Vector3 localpos = instantiatedContainer.transform.localPosition;
            instantiatedContainer.transform.localPosition -= new Vector3(localScale.x / 2, 0, 0);
        }
        else
        {
            Vector3 originShove = originPos;
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
            Vector3 newLocalScale = roundedHit + new Vector3(xOffset, yOffset, 0.5f) - originShove;

            float newLocalScaleY = Mathf.Max(newLocalScale.y, 1);
            if (yOffset < 0)
            {
                newLocalScaleY = Mathf.Min(-1, newLocalScale.y);
            }

            newLocalScale = new Vector3(newLocalScale.x, newLocalScaleY, newLocalScale.z);
            instantiatedContainer.transform.localScale = newLocalScale;

            float startBeat = instantiatedContainer.transform.localPosition.z / EditorScaleController.EditorScale;
            float endBeat = (instantiatedContainer.transform.localPosition.z + newLocalScale.z) / EditorScaleController.EditorScale;
            if (startBeat > endBeat) (startBeat, endBeat) = (endBeat, startBeat);

            SelectionController.ForEachObjectBetweenTimeByGroup(startBeat, endBeat, true, true, true, (bocc, bo) =>
            {
                if (!SelectedTypes.Contains(bo.beatmapType)) return; // Must be a type we can select

                var left = instantiatedContainer.transform.localPosition.x + instantiatedContainer.transform.localScale.x;
                var right = instantiatedContainer.transform.localPosition.x;
                if (right < left) (left, right) = (right, left);

                var top = instantiatedContainer.transform.localPosition.y + instantiatedContainer.transform.localScale.y;
                var bottom = instantiatedContainer.transform.localPosition.y;
                if (top < bottom) (top, bottom) = (bottom, top);

                var p = new Vector2(left, bottom);

                if (bo is IBeatmapObjectBounds obj)
                {
                    p = obj.GetCenter();
                }
                else if (bo is MapEvent evt)
                {
                    var position = evt.GetPosition(labels, eventsContainer.PropagationEditing, eventsContainer.EventTypeToPropagate);

                    // Not visible = notselectable
                    if (position == null) return;

                    p = new Vector2(position?.x + bounds.min.x ?? 0, position?.y ?? 0);
                }
                else if (bo is BeatmapCustomEvent custom)
                {
                    p = new Vector2(customCollection.CustomEventTypes.IndexOf(custom._type) + bounds.min.x + 0.5f, 0.5f);
                }

                // Check if calculated position is outside bounds
                if (p.x < left || p.x > right || p.y < bottom || p.y >= top) return;

                if (!alreadySelected.Contains(bo) && selected.Add(bo))
                {
                    SelectionController.Select(bo, true, false, false);
                }
            });

            foreach (BeatmapObject combinedObj in SelectionController.SelectedObjects.ToArray())
            {
                if (!selected.Contains(combinedObj) && !alreadySelected.Contains(combinedObj))
                {
                    SelectionController.Deselect(combinedObj, false);
                }
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

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || instantiatedContainer is null) return;
        Gizmos.color = Color.red;
        BoxCollider boxyBoy = instantiatedContainer.GetComponent<BoxCollider>();
        Bounds bounds = new Bounds();
        bounds.center = boxyBoy.bounds.center;
        bounds.size = instantiatedContainer.transform.lossyScale / 2f;
        Gizmos.DrawMesh(instantiatedContainer.GetComponentInChildren<MeshFilter>().mesh, bounds.center, instantiatedContainer.transform.rotation, bounds.size);
    }

    public override void CancelPlacement()
    {
        IsSelecting = false;
        foreach(BeatmapObject selectedObject in selected)
        {
            SelectionController.Deselect(selectedObject);
        }
    }

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued) { }

    public void OnActivateBoxSelect(InputAction.CallbackContext context)
    {
        keybindPressed = context.performed;
    }
}

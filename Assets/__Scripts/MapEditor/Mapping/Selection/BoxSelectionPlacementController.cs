using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoxSelectionPlacementController : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>
{
    public static bool IsSelecting { get; private set; } = false;
    private Vector3 originPos;
    private float startTime;
    private RaycastHit previousHit;
    private Vector3 transformed;

    private HashSet<BeatmapObject> selected = new HashSet<BeatmapObject>();
    private HashSet<BeatmapObject> alreadySelected = new HashSet<BeatmapObject>();

    private List<BeatmapObject.Type> SelectedTypes = new List<BeatmapObject.Type>();

    [HideInInspector] protected override bool DestroyBoxCollider { get; set; } = false;
    [HideInInspector] protected override bool CanClickAndDrag { get; set; } = false;

    public override bool IsValid => (KeybindsController.CtrlHeld || IsSelecting) && Settings.Instance.BoxSelect;

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting) => null;

    public override MapEvent GenerateOriginalData() => new MapEvent(float.MaxValue, 69, 420);

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 transformedPoint)
    {
        //this mess of localposition and position assignments are to align the shits up with the grid
        //and to hopefully not cause IndexOutOfRangeExceptions
        instantiatedContainer.transform.localPosition = parentTrack.InverseTransformPoint(hit.point); //fuck transformedpoint we're doing it ourselves

        Vector3 localMax = parentTrack.InverseTransformPoint(hit.collider.bounds.max);
        Vector3 localMin = parentTrack.InverseTransformPoint(hit.collider.bounds.min);
        float farRightPoint = localMax.x;
        float farLeftPoint = localMin.x;
        float farTopPoint = localMax.y;
        float farBottomPoint = localMin.y;

        Vector3 roundedHit = parentTrack.InverseTransformPoint(hit.point);
        roundedHit = new Vector3(Mathf.Ceil(roundedHit.x), Mathf.Ceil(roundedHit.y), roundedHit.z);
        instantiatedContainer.transform.localPosition = roundedHit - new Vector3(0.5f, 1, 0);
        float x = instantiatedContainer.transform.localPosition.x; //Clamp values to prevent exceptions
        float y = instantiatedContainer.transform.localPosition.y;
        CalculateTimes(hit, out _, out float realTime, out _, out _, out float offset);
        instantiatedContainer.transform.localPosition = new Vector3(
            Mathf.Clamp(x, farLeftPoint + 0.5f, farRightPoint - 0.5f),
            Mathf.Clamp(y, 0, farTopPoint),
            instantiatedContainer.transform.localPosition.z);
        previousHit = hit;
        transformed = transformedPoint;
        if (!IsSelecting)
        {
            SelectedTypes.Clear();
            if (hit.transform.GetComponentInParent<EventPlacement>()) SelectedTypes.Add(BeatmapObject.Type.EVENT);
            if (hit.transform.GetComponentInParent<NotePlacement>()) SelectedTypes.Add(BeatmapObject.Type.NOTE);
            if (hit.transform.GetComponentInParent<ObstaclePlacement>()) SelectedTypes.Add(BeatmapObject.Type.OBSTACLE);
            if (hit.transform.GetComponentInParent<CustomEventPlacement>()) SelectedTypes.Add(BeatmapObject.Type.CUSTOM_EVENT);
            instantiatedContainer.transform.localScale = Vector3.one;
            Vector3 localScale = instantiatedContainer.transform.localScale;
            startTime = realTime;
            Vector3 localpos = instantiatedContainer.transform.localPosition;
            instantiatedContainer.transform.localPosition -= new Vector3(localScale.x / 2, 0, localScale.z / 2);
        }
        else
        {
            instantiatedContainer.transform.localPosition = originPos;
            Vector3 newLocalScale = roundedHit + new Vector3(0, 0, 0.5f) - originPos;
            newLocalScale = new Vector3(newLocalScale.x, Mathf.Max(newLocalScale.y, 1), newLocalScale.z);
            instantiatedContainer.transform.localScale = newLocalScale;

            selected.Clear();
            OverlapBox((containerBoye) =>
            {
                if (!alreadySelected.Contains(containerBoye.objectData) && selected.Add(containerBoye.objectData))
                {
                    SelectionController.Select(containerBoye.objectData, true, false, false);
                }
            });
            foreach (BeatmapObject combinedObj in SelectionController.SelectedObjects.ToArray())
            {
                if (!selected.Contains(combinedObj) && !alreadySelected.Contains(combinedObj))
                {
                    //Id imagine if you select an object, and that object is unloaded, then it should stay selected...?
                    if (BeatmapObjectContainerCollection.GetCollectionForType(combinedObj.beatmapType).LoadedContainers.ContainsKey(combinedObj))
                    {
                        SelectionController.Deselect(combinedObj);
                    }
                }
            }
            selected.Clear();
        }
    }

    public override void OnMousePositionUpdate(UnityEngine.InputSystem.InputAction.CallbackContext context)
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
            IsSelecting = false;
            OnPhysicsRaycast(previousHit, transformed);
        }
    }

    private void OverlapBox(Action<BeatmapObjectContainer> action)
    {
        //Big brain boye does big brain things with big brain box
        BoxCollider boxyBoy = instantiatedContainer.GetComponent<BoxCollider>();
        Bounds bounds = new Bounds();
        bounds.center = boxyBoy.bounds.center;
        Vector3 absoluteLossyScale = new Vector3(
            Mathf.Abs(instantiatedContainer.transform.lossyScale.x),
            Mathf.Abs(instantiatedContainer.transform.lossyScale.y),
            Mathf.Abs(instantiatedContainer.transform.lossyScale.z)
            );
        bounds.size = absoluteLossyScale / 2f;
        Collider[] boxyBoyHitsStuffTM = Physics.OverlapBox(bounds.center, bounds.size, instantiatedContainer.transform.rotation, 1 << 9);
        foreach (Collider collider in boxyBoyHitsStuffTM)
        {
            BeatmapObjectContainer containerBoye = collider.gameObject.GetComponent<BeatmapObjectContainer>();
            if (!SelectedTypes.Contains(containerBoye.objectData.beatmapType)) continue;
            action?.Invoke(containerBoye);
        }
    }

    private IEnumerator WaitABitFuckOffOtherPlacementControllers()
    {
        yield return new WaitForSeconds(0.1f);
        IsSelecting = false;
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
}

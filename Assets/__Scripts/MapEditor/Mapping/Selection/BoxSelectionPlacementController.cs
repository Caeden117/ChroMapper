using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoxSelectionPlacementController : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>
{
    public static bool IsSelecting { get; private set; } = false;
    private Vector3 originPos;
    private int boxWidth = 1;
    private int boxHeight = 1;
    private float startTime;
    private float endTime;

    private List<BeatmapObjectContainer> selected;

    private List<BeatmapObject.Type> SelectedTypes = new List<BeatmapObject.Type>();

    protected override bool DestroyBoxCollider { get; set; } = false;
    protected override bool CanClickAndDrag { get; set; } = false;

    public override bool IsValid => (KeybindsController.CtrlHeld || IsSelecting) &&
        !(SongTimelineController.IsHovering || !IsActive) && Settings.Instance.BoxSelect;

    public override BeatmapAction GenerateAction(BeatmapEventContainer spawned, BeatmapObjectContainer conflicting) => null;

    public override MapEvent GenerateOriginalData() => null;

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 transformedPoint)
    {
        if (!IsSelecting)
        {
            SelectedTypes.Clear();
            if (hit.transform.GetComponentInParent<EventPlacement>()) SelectedTypes.Add(BeatmapObject.Type.EVENT);
            if (hit.transform.GetComponentInParent<NotePlacement>()) SelectedTypes.Add(BeatmapObject.Type.NOTE);
            if (hit.transform.GetComponentInParent<ObstaclePlacement>()) SelectedTypes.Add(BeatmapObject.Type.OBSTACLE);
            if (hit.transform.GetComponentInParent<CustomEventPlacement>()) SelectedTypes.Add(BeatmapObject.Type.CUSTOM_EVENT);
            startTime = RoundedTime;
            instantiatedContainer.transform.localScale = Vector3.one;
            instantiatedContainer.transform.localPosition = parentTrack.InverseTransformPoint(hit.point - new Vector3(0.5f, 0.5f, 0));
        }
        else
        {
            instantiatedContainer.transform.localPosition = originPos;
            Vector3 newLocalScale = parentTrack.InverseTransformPoint(hit.point) - originPos;
            instantiatedContainer.transform.localScale = new Vector3(newLocalScale.x + 1, newLocalScale.y + 1, newLocalScale.z + 1);
            endTime = (transformedPoint.z / EditorScaleController.EditorScale) + atsc.CurrentBeat;
        }
    }

    internal override void Update()
    {
        if (!IsValid && IsSelecting)
            StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
        base.Update();
        if (IsSelecting)
        {
            //if (Input.GetMouseButtonDown(1)) //Cancel selection with a right click.
            //    IsSelecting = false;
            instantiatedContainer.transform.localPosition = new Vector3(instantiatedContainer.transform.localPosition.x,
                instantiatedContainer.transform.localPosition.y,
                instantiatedContainer.transform.localPosition.z
                );
            instantiatedContainer.transform.localScale = new Vector3(instantiatedContainer.transform.localScale.x,
                instantiatedContainer.transform.localScale.y, ((endTime - startTime) * EditorScaleController.EditorScale) + 1f);
        }
    }

    internal override void ApplyToMap()
    {
        if (!IsSelecting)
        {
            IsSelecting = true;
            startTime = (instantiatedContainer.transform.localPosition.z / EditorScaleController.EditorScale) + atsc.offsetBeat;
            originPos = instantiatedContainer.transform.localPosition;
        }
        else
        {
            StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
            List<BeatmapObjectContainer> toSelect = new List<BeatmapObjectContainer>();

            //Big brain boye does big brain things with big brain box
            BoxCollider boxyBoy = instantiatedContainer.GetComponent<BoxCollider>();
            Bounds bounds = new Bounds();
            bounds.center = boxyBoy.bounds.center;
            bounds.size = instantiatedContainer.transform.lossyScale / 2f;
            Collider[] boxyBoyHitsStuffTM = Physics.OverlapBox(bounds.center, bounds.extents, instantiatedContainer.transform.rotation, 1 << 9);
            foreach(Collider collider in boxyBoyHitsStuffTM){
                BeatmapObjectContainer containerBoye = collider.gameObject.GetComponent<BeatmapObjectContainer>();
                if (containerBoye != null && SelectedTypes.Contains(containerBoye.objectData.beatmapType)) toSelect.Add(containerBoye);
            }

            foreach (BeatmapObjectContainer obj in toSelect) SelectionController.Select(obj, true, false);
            SelectionController.RefreshSelectionMaterial(toSelect.Any());
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

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued) { }
}

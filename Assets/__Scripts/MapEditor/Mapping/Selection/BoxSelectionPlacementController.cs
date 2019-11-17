using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoxSelectionPlacementController : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>
{
    public static bool IsSelecting { get; private set; } = false;
    private int originWidth = 0;
    private int originHeight = 0;
    private int boxWidth = 1;
    private int boxHeight = 1;
    private float startTime = 0;
    private float endTime = 0;

    private List<BeatmapObjectContainer> selected;

    protected override bool DestroyBoxCollider { get; set; } = false;
    protected override bool CanClickAndDrag { get; set; } = false;

    public override bool IsValid => (KeybindsController.CtrlHeld || IsSelecting) &&
        !(SongTimelineController.IsHovering || !IsActive) && Settings.Instance.BoxSelect;

    public override BeatmapAction GenerateAction(BeatmapEventContainer spawned, BeatmapObjectContainer conflicting) => null;

    public override MapEvent GenerateOriginalData() => null;

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        float snapping = 1f / atsc.gridMeasureSnapping;
        float time = (hit.point.z / EditorScaleController.EditorScale) + atsc.CurrentBeat;
        float roundedTime = (Mathf.Round((time - atsc.offsetBeat) / snapping) * snapping) + atsc.offsetBeat;
        if (!IsSelecting)
        {
            startTime = roundedTime;
            instantiatedContainer.transform.localScale = Vector3.one;
            instantiatedContainer.transform.localPosition = new Vector3(
                 Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                     Mathf.Ceil(hit.collider.bounds.min.x),
                     Mathf.Floor(hit.collider.bounds.max.x)
                 ) - 1,
                 Mathf.Clamp(Mathf.Floor(hit.point.y - 0.1f), 0f,
                     Mathf.Floor(hit.collider.bounds.max.y)),
                 (roundedTime * EditorScaleController.EditorScale) - 0.5f
                 );
            originWidth = Mathf.RoundToInt(instantiatedContainer.transform.position.x + 2);
            originHeight = Mathf.RoundToInt(instantiatedContainer.transform.position.y);
        }
        else
        {
            instantiatedContainer.transform.position = new Vector3(originWidth - 2, originHeight, instantiatedContainer.transform.position.z);
            boxWidth = Mathf.CeilToInt(Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                                    Mathf.Ceil(hit.collider.bounds.min.x),
                                    Mathf.Floor(hit.collider.bounds.max.x)) + 2) - originWidth;
            if (boxWidth < originWidth) boxWidth--;
            boxHeight = Mathf.RoundToInt(Mathf.Clamp(Mathf.Floor(hit.point.y - 0.1f), 0, Mathf.Floor(hit.collider.bounds.max.y))) - originHeight;
            if (boxHeight < originHeight) boxHeight--;
            instantiatedContainer.transform.localScale = new Vector3(boxWidth, boxHeight + 1, instantiatedContainer.transform.localScale.z);
            endTime = roundedTime;
        }
    }

    internal override void Update()
    {
        if (!IsValid && IsSelecting)
            StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
        base.Update();
        if (IsSelecting)
        {
            if (Input.GetMouseButtonDown(1)) //Cancel selection with a right click.
                IsSelecting = false;
            instantiatedContainer.transform.position = new Vector3(instantiatedContainer.transform.position.x,
                instantiatedContainer.transform.position.y,
                ((startTime - atsc.CurrentBeat) * EditorScaleController.EditorScale) - 0.5f
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
            float snapping = 1f / atsc.gridMeasureSnapping;
            float time = ((instantiatedContainer.transform.position.z + 0.5f) / EditorScaleController.EditorScale) + atsc.CurrentBeat;
            float roundedTime = Mathf.Round((time - atsc.offsetBeat) / snapping) * snapping;
            startTime = roundedTime + atsc.offsetBeat;
        }
        else
        {
            StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
            List<BeatmapObjectContainer> toSelect = new List<BeatmapObjectContainer>();

            //Big brain boye does big brain things with big brain box
            BoxCollider boxyBoy = instantiatedContainer.GetComponent<BoxCollider>();
            Collider[] boxyBoyHitsStuffTM = Physics.OverlapBox(boxyBoy.bounds.center, boxyBoy.bounds.extents, boxyBoy.transform.rotation, 1 << 9);
            foreach(Collider collider in boxyBoyHitsStuffTM){
                BeatmapObjectContainer containerBoye = collider.gameObject.GetComponent<BeatmapObjectContainer>();
                if (containerBoye != null) toSelect.Add(containerBoye);
            }

            if (!KeybindsController.ShiftHeld) SelectionController.DeselectAll();
            foreach (BeatmapObjectContainer obj in toSelect) SelectionController.Select(obj, true, false);
            SelectionController.RefreshSelectionMaterial(toSelect.Any());
        }
    }

    private IEnumerator WaitABitFuckOffOtherPlacementControllers()
    {
        yield return new WaitForSeconds(0.1f);
        IsSelecting = false;
    }

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued) { }
}

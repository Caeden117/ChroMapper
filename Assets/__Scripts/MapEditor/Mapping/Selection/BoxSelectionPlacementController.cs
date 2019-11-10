using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoxSelectionPlacementController : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>
{
    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;

    private bool isSelecting = false;
    private int originWidth = 0;
    private int originHeight = 0;
    private int boxWidth = 1;
    private int boxHeight = 1;
    private float startTime = 0;
    private float endTime = 0;

    private List<BeatmapObjectContainer> selected;

    public override bool IsValid => (KeybindsController.CtrlHeld || isSelecting) &&
        !(Input.GetMouseButton(1) || SongTimelineController.IsHovering || !IsActive) && Settings.Instance.BoxSelect;

    public override BeatmapAction GenerateAction(BeatmapEventContainer spawned, BeatmapObjectContainer conflicting) => null;

    public override MapEvent GenerateOriginalData() => null;

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        float snapping = 1f / atsc.gridMeasureSnapping;
        float time = (hit.point.z / EditorScaleController.EditorScale) + atsc.CurrentBeat;
        float roundedTime = Mathf.Round((time - atsc.offsetBeat) / snapping) * snapping;
        if (!isSelecting)
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
        if (!IsValid) isSelecting = false;
        base.Update();
        if (isSelecting)
        {
            if (Input.GetMouseButtonDown(1)) //Cancel selection with a right click.
                isSelecting = false;
            instantiatedContainer.transform.position = new Vector3(instantiatedContainer.transform.position.x,
                instantiatedContainer.transform.position.y,
                ((startTime - atsc.CurrentBeat - atsc.offsetBeat) * EditorScaleController.EditorScale) - 0.5f
                );
            instantiatedContainer.transform.localScale = new Vector3(instantiatedContainer.transform.localScale.x,
                instantiatedContainer.transform.localScale.y, ((endTime - startTime) * EditorScaleController.EditorScale) + 1f);
        }
    }

    internal override void ApplyToMap()
    {
        if (!isSelecting)
        {
            isSelecting = true;
            float snapping = 1f / atsc.gridMeasureSnapping;
            float time = ((instantiatedContainer.transform.position.z + 0.5f) / EditorScaleController.EditorScale) + atsc.CurrentBeat;
            float roundedTime = Mathf.Round((time - atsc.offsetBeat) / snapping) * snapping;
            startTime = roundedTime;
        }
        else
        {
            Debug.Log($"{startTime}|{endTime}|{originWidth}|{boxWidth}|{originHeight}|{boxHeight}");
            isSelecting = false;
            List<BeatmapObjectContainer> toSelect = new List<BeatmapObjectContainer>();
            if (originWidth >= 17)
            {
                toSelect = objectContainerCollection.LoadedContainers.Where(x =>
                    x.objectData._time >= startTime && x.objectData._time <= endTime &&
                    BeatmapEventContainer.EventTypeToModifiedType((x.objectData as MapEvent)._type) >= originWidth - 17 &&
                    BeatmapEventContainer.EventTypeToModifiedType((x.objectData as MapEvent)._type) <= originWidth + boxWidth - 17).ToList();
            }
            else
            {
                toSelect = notes.LoadedContainers.Where(x =>
                    x.objectData._time >= startTime && x.objectData._time <= endTime &&
                    (x.objectData as BeatmapNote)._lineIndex >= originWidth && (x.objectData as BeatmapNote)._lineIndex <= originWidth + boxWidth &&
                    (x.objectData as BeatmapNote)._lineLayer >= originHeight && (x.objectData as BeatmapNote)._lineLayer <= originHeight + boxHeight
                    ).ToList();
                toSelect.AddRange(obstacles.LoadedContainers.Where(x =>
                    x.objectData._time >= startTime && x.objectData._time <= endTime &&
                    (x.objectData as BeatmapObstacle)._lineIndex >= originWidth && (x.objectData as BeatmapObstacle)._lineIndex <= originWidth + boxWidth &&
                    (x.objectData as BeatmapObstacle)._type * 1.5f >= originHeight && (x.objectData as BeatmapObstacle)._type * 1.5f <= originHeight + boxHeight
                    ));
            }
            if (!KeybindsController.ShiftHeld) SelectionController.DeselectAll();
            foreach (BeatmapObjectContainer obj in toSelect) SelectionController.Select(obj, true, false);
            SelectionController.RefreshSelectionMaterial(toSelect.Any());
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public abstract class PlacementController<BO, BOC, BOCC> : MonoBehaviour where BO : BeatmapObject where BOC : BeatmapObjectContainer where BOCC : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject objectContainerPrefab;
    [SerializeField] private BO objectData;
    [SerializeField] internal BOCC objectContainerCollection;
    [SerializeField] private BeatmapObject.Type objectDataType;
    [SerializeField] private bool startingActiveState;
    [SerializeField] internal AudioTimeSyncController atsc;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;

    protected virtual bool DestroyBoxCollider { get; set; } = true;

    protected virtual bool CanClickAndDrag { get; set; } = true;
    private bool isDraggingObject = false;
    private BOC draggedObjectContainer = null;
    private BO draggedObjectData = null;
    private BO originalQueued = null;

    public virtual bool IsValid { get
        {
            return !(KeybindsController.AnyCriticalKeys || Input.GetMouseButton(1) || SongTimelineController.IsHovering || !IsActive || 
                BoxSelectionPlacementController.IsSelecting);
        } }

    public bool IsActive = false;

    internal BO queuedData; //Data that is not yet applied to the BeatmapObjectContainer.
    internal BOC instantiatedContainer;

    internal virtual void Start()
    {
        Physics.autoSyncTransforms = false; //Causes performance degradation, do not want.
        queuedData = GenerateOriginalData();
        IsActive = startingActiveState;
    }

    internal virtual void Update()
    {
        if (KeybindsController.AltHeld && Input.GetMouseButtonDown(0) && CanClickAndDrag)
        {
            Ray dragRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(dragRay, out RaycastHit dragHit, 999f, 1 << 9))
            {
                BeatmapObjectContainer con = dragHit.transform.gameObject.GetComponent<BeatmapObjectContainer>();
                if (con is null || !(con is BOC)) return; //Filter out null objects and objects that aren't what we're targetting.
                isDraggingObject = true;
                draggedObjectData = BeatmapObject.GenerateCopy(con.objectData as BO);
                originalQueued = BeatmapObject.GenerateCopy(queuedData);
                queuedData = BeatmapObject.GenerateCopy(draggedObjectData);
                draggedObjectContainer = con as BOC;
                return;
            }
        }
        else if ((!KeybindsController.AltHeld || Input.GetMouseButtonUp(0)) && isDraggingObject)
        {
            isDraggingObject = false;
            queuedData = BeatmapObject.GenerateCopy(originalQueued);
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] BeatmapObjectsHit = Physics.RaycastAll(ray, 999f);
        bool isOnPlacement = false;
        foreach (RaycastHit objectHit in BeatmapObjectsHit)
        {
            if (objectHit.transform.GetComponentsInParent(GetType()).Any() && !isOnPlacement)
                isOnPlacement = true;
            BeatmapObjectContainer con = objectHit.transform.gameObject.GetComponent<BeatmapObjectContainer>();
            if (con == null || con == draggedObjectContainer) continue;
            con.SafeSetBoxCollider(KeybindsController.AnyCriticalKeys || Input.GetMouseButtonDown(2));
        }
        if (PauseManager.IsPaused) return;
        if ((!IsValid && (!isDraggingObject || !IsActive)) || !isOnPlacement)
        {
            ColliderExit();
            return;
        }
        if (instantiatedContainer == null) RefreshVisuals();
        if (!instantiatedContainer.gameObject.activeSelf) instantiatedContainer.gameObject.SetActive(true);
        objectData = queuedData;
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, 1 << 11))
        {
            if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
            if (BeatmapObjectContainerCollection.TrackFilterID != null && !objectContainerCollection.IgnoreTrackFilter)
            {
                if (queuedData._customData == null) queuedData._customData = new SimpleJSON.JSONObject();
                queuedData._customData["track"] = BeatmapObjectContainerCollection.TrackFilterID;
            }
            else queuedData?._customData?.Remove("track");
            float snapping = 1f / atsc.gridMeasureSnapping;
            float time = (hit.point.z / EditorScaleController.EditorScale) + atsc.CurrentBeat;
            float roundedTime = Mathf.Round((time - atsc.offsetBeat) / snapping) * snapping * EditorScaleController.EditorScale;
            instantiatedContainer.transform.localPosition = new Vector3(
                Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                    Mathf.Ceil(hit.collider.bounds.min.x),
                    Mathf.Floor(hit.collider.bounds.max.x)
                ) - 0.5f,
                Mathf.Clamp(Mathf.Floor(hit.point.y - 0.1f), 0f,
                    Mathf.Floor(hit.collider.bounds.max.y)) + 0.5f,
                roundedTime
                );
            OnPhysicsRaycast(hit);
            if (isDraggingObject && queuedData != null)
            {
                TransferQueuedToDraggedObject(ref draggedObjectData, BeatmapObject.GenerateCopy(queuedData));
                draggedObjectContainer.objectData = draggedObjectData;
                draggedObjectContainer.objectData._time = roundedTime / EditorScaleController.EditorScale;
                draggedObjectContainer.UpdateGridPosition();
            }
        }
        if (Input.GetMouseButtonDown(0) && !isDraggingObject) ApplyToMap();
    }

    void ColliderExit()
    {
        if (instantiatedContainer != null) instantiatedContainer.gameObject.SetActive(false);
    }

    private void RefreshVisuals()
    {
        instantiatedContainer = Instantiate(objectContainerPrefab,
            objectContainerCollection.transform).GetComponent(typeof(BOC)) as BOC;
        if (instantiatedContainer.GetComponent<BoxCollider>() != null && DestroyBoxCollider)
            Destroy(instantiatedContainer.GetComponent<BoxCollider>());
        instantiatedContainer.name = $"Hover {objectDataType}";
    }

    internal virtual void ApplyToMap()
    {
        objectData = BeatmapObject.GenerateCopy(queuedData);
        objectData._time = (instantiatedContainer.transform.position.z / EditorScaleController.EditorScale)
            + atsc.CurrentBeat - atsc.offsetBeat;
        BOC spawned = objectContainerCollection.SpawnObject(objectData, out BeatmapObjectContainer conflicting) as BOC;
        BeatmapActionContainer.AddAction(GenerateAction(spawned, conflicting));
        SelectionController.RefreshMap();
        queuedData = BeatmapObject.GenerateCopy(queuedData);
    }

    public abstract BO GenerateOriginalData();
    public abstract BeatmapAction GenerateAction(BOC spawned, BeatmapObjectContainer conflicting);
    public abstract void OnPhysicsRaycast(RaycastHit hit);

    public abstract void TransferQueuedToDraggedObject(ref BO dragged, BO queued);
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    public bool IsValid { get
        {
            return !(NodeEditorController.IsActive || !IsActive || KeybindsController.ShiftHeld ||
                KeybindsController.CtrlHeld || Input.GetMouseButton(1) || SongTimelineController.IsHovering);
        } }

    public bool IsActive = false;

    internal BO queuedData; //Data that is not yet applied to the BeatmapObjectContainer.
    internal BOC instantiatedContainer;

    void Start()
    {
        queuedData = GenerateOriginalData();
        IsActive = startingActiveState;
    }

    void ColliderHit()
    {
        if (PauseManager.IsPaused) return;
        if (!IsValid)
        {
            ColliderExit();
            return;
        }
        if (instantiatedContainer == null) RefreshVisuals();
        if (!instantiatedContainer.gameObject.activeSelf) instantiatedContainer.gameObject.SetActive(true);
        objectData = queuedData;
        IsActive = true;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, 1 << 11))
        {
            if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
            float roundedToPrecision = Mathf.Round((hit.point.z / EditorScaleController.EditorScale) /
                (1 / (float)atsc.gridMeasureSnapping)) * (1 / (float)atsc.gridMeasureSnapping)
                * EditorScaleController.EditorScale;
            instantiatedContainer.transform.localPosition = new Vector3(
                Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                    Mathf.Ceil(hit.collider.bounds.min.x),
                    Mathf.Floor(hit.collider.bounds.max.x)
                ) - 0.5f,
                Mathf.Clamp(Mathf.Floor(hit.point.y - 0.1f), 0f,
                    Mathf.Floor(hit.collider.bounds.max.y)) + 0.5f,
                roundedToPrecision + (objectContainerCollection.AudioTimeSyncController.CurrentBeat * EditorScaleController.EditorScale)
                );
            OnPhysicsRaycast(hit);
        }
        if (Input.GetMouseButtonDown(0)) ApplyToMap();
    }

    void ColliderExit()
    {
        if (instantiatedContainer != null) instantiatedContainer.gameObject.SetActive(false);
    }

    private void RefreshVisuals()
    {
        instantiatedContainer = Instantiate(objectContainerPrefab,
            objectContainerCollection.transform).GetComponent(typeof(BOC)) as BOC;
        Destroy(instantiatedContainer.GetComponent<BoxCollider>());
        instantiatedContainer.name = $"Hover {objectDataType}";
    }

    internal virtual void ApplyToMap()
    {
        objectData = BeatmapObject.GenerateCopy(queuedData);
        objectData._time = (instantiatedContainer.transform.position.z / EditorScaleController.EditorScale)
            + atsc.CurrentBeat;
        BOC spawned = objectContainerCollection.SpawnObject(objectData, out BeatmapObjectContainer conflicting) as BOC;
        BeatmapActionContainer.AddAction(GenerateAction(spawned, conflicting));
        SelectionController.RefreshMap();
        queuedData = BeatmapObject.GenerateCopy(queuedData);
    }

    public abstract BO GenerateOriginalData();
    public abstract BeatmapAction GenerateAction(BOC spawned, BeatmapObjectContainer conflicting);
    public abstract void OnPhysicsRaycast(RaycastHit hit);
}

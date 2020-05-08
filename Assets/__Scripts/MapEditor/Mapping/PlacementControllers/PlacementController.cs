using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public abstract class PlacementController<BO, BOC, BOCC> : MonoBehaviour, CMInput.IPlacementControllersActions, CMInput.ICancelPlacementActions where BO : BeatmapObject where BOC : BeatmapObjectContainer where BOCC : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject objectContainerPrefab;
    [SerializeField] private BO objectData;
    [SerializeField] internal BOCC objectContainerCollection;
    [SerializeField] protected Transform parentTrack;
    [SerializeField] protected Transform interfaceGridParent;
    [SerializeField] protected bool AssignTo360Tracks;
    [SerializeField] private BeatmapObject.Type objectDataType;
    [SerializeField] private bool startingActiveState;
    [SerializeField] protected AudioTimeSyncController atsc;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [SerializeField] protected TracksManager tracksManager;
    [SerializeField] protected RotationCallbackController gridRotation;

    [HideInInspector] protected virtual bool DestroyBoxCollider { get; set; } = true;

    [HideInInspector] protected virtual bool CanClickAndDrag { get; set; } = true;

    [HideInInspector] protected virtual float RoundedTime { get; private set; } = 0;

    protected bool isDraggingObject = false;
    protected Vector2 mousePosition;
    protected bool isOnPlacement = false;
    protected Camera mainCamera = null;

    protected BOC draggedObjectContainer = null;
    private BO draggedObjectData = null;
    private BO originalQueued = null;

    public virtual bool IsValid { get
        {
            return !(KeybindsController.AnyCriticalKeys || Input.GetMouseButton(1) || SongTimelineController.IsHovering || !IsActive || 
                BoxSelectionPlacementController.IsSelecting) && Application.isFocused;
        } }

    public bool IsActive = false;

    internal BO queuedData; //Data that is not yet applied to the BeatmapObjectContainer.
    internal BOC instantiatedContainer;

    internal virtual void Start()
    {
        Physics.autoSyncTransforms = false; //Causes performance degradation, do not want.
        queuedData = GenerateOriginalData();
        IsActive = startingActiveState;
        mainCamera = Camera.main;
    }

    protected void CalculateTimes(RaycastHit hit, out Vector3 transformedPoint, out float realTime, out float roundedTime, out float roundedCurrent, out float offsetTime)
    {
        Transform hitTransform = hit.transform;
        transformedPoint = interfaceGridParent.InverseTransformPoint(hit.point);
        transformedPoint = new Vector3(transformedPoint.x * hitTransform.lossyScale.x,
            transformedPoint.y, transformedPoint.z * hitTransform.lossyScale.z);
        realTime = (transformedPoint.z / (EditorScaleController.EditorScale * (hitTransform.parent.localScale.z / 10f))) + atsc.CurrentBeat;
        roundedTime = atsc.FindRoundedBeatTime(realTime) + atsc.offsetBeat;
        roundedCurrent = atsc.FindRoundedBeatTime(atsc.CurrentBeat);
        offsetTime = hit.collider.gameObject.name.Contains("Interface") ? 0 : atsc.CurrentBeat - roundedCurrent;
        if (!atsc.IsPlaying) roundedTime += offsetTime;
    }

    void ColliderExit()
    {
        if (instantiatedContainer != null) instantiatedContainer.gameObject.SetActive(false);
    }

    protected void RefreshVisuals()
    {
        instantiatedContainer = Instantiate(objectContainerPrefab,
            parentTrack).GetComponent(typeof(BOC)) as BOC;
        if (instantiatedContainer.GetComponent<BoxCollider>() != null && DestroyBoxCollider)
            Destroy(instantiatedContainer.GetComponent<BoxCollider>());
        instantiatedContainer.name = $"Hover {objectDataType}";
    }

    private void Update360Tracks()
    {
        if (!AssignTo360Tracks) return;
        TracksManager manager = objectContainerCollection.GetComponent<TracksManager>();
        if (manager == null)
            Debug.LogWarning("Could not find an attached TracksManager.");
        else
        {
            Track track = manager.GetTrackForRotationValue(Mathf.RoundToInt(transform.localEulerAngles.y));
            if (track != null)
            {
                Vector3 localPos = instantiatedContainer.transform.localPosition;
                parentTrack = track.ObjectParentTransform;
                instantiatedContainer.transform.SetParent(track.ObjectParentTransform, false);
                instantiatedContainer.transform.localPosition = localPos;
                instantiatedContainer.transform.localEulerAngles = new Vector3(instantiatedContainer.transform.localEulerAngles.x,
                    0, instantiatedContainer.transform.localEulerAngles.z);
            }
        }
    }

    internal virtual void ApplyToMap()
    {
        objectData = BeatmapObject.GenerateCopy(queuedData);
        objectData._time = RoundedTime;
        BOC spawned = objectContainerCollection.SpawnObject(objectData, out BeatmapObjectContainer conflicting) as BOC;
        BeatmapActionContainer.AddAction(GenerateAction(spawned, conflicting));
        queuedData = BeatmapObject.GenerateCopy(queuedData);
        if (AssignTo360Tracks)
        {
            Vector3 localRotation = spawned.transform.localEulerAngles;
            Track track = tracksManager.GetTrackForRotationValue(gridRotation.Rotation);
            track?.AttachContainer(spawned);
            spawned.UpdateGridPosition();
            spawned.transform.localEulerAngles = localRotation;
            tracksManager.RefreshTracks();
        }
    }

    public abstract BO GenerateOriginalData();
    public abstract BeatmapAction GenerateAction(BOC spawned, BeatmapObjectContainer conflicting);
    public abstract void OnPhysicsRaycast(RaycastHit hit, Vector3 transformedPoint);

    public virtual void AfterDraggedObjectDataChanged() { }

    public virtual bool IsObjectOverlapping(BO draggedData, BO overlappingData) => true;

    public virtual void ClickAndDragFinished() { }

    public virtual void CancelPlacement() { }

    public abstract void TransferQueuedToDraggedObject(ref BO dragged, BO queued);

    public void OnPlaceObject(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        bool isBoxSelect = this is BoxSelectionPlacementController;
        if (context.performed && !isDraggingObject && isOnPlacement && instantiatedContainer != null && IsValid
            && !PersistentUI.Instance.DialogBox_IsEnabled && (isBoxSelect ? true : !KeybindsController.CtrlHeld) &&
            queuedData?._time >= 0) ApplyToMap();
    }

    public void OnInitiateClickandDrag(InputAction.CallbackContext context)
    {
        if (context.performed && CanClickAndDrag)
        {
            Ray dragRay = mainCamera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(dragRay, out RaycastHit dragHit, 999f, 1 << 9))
            {
                BeatmapObjectContainer con = dragHit.transform.gameObject.GetComponent<BeatmapObjectContainer>();
                if (con is null || !(con is BOC)) return; //Filter out null objects and objects that aren't what we're targetting.
                isDraggingObject = true;
                draggedObjectData = BeatmapObject.GenerateCopy(con.objectData as BO);
                originalQueued = BeatmapObject.GenerateCopy(queuedData);
                queuedData = BeatmapObject.GenerateCopy(draggedObjectData);
                draggedObjectContainer = con as BOC;
            }
        }
        else if (context.canceled && isDraggingObject && instantiatedContainer != null)
        {
            //First, find and delete anything that's overlapping our dragged object.
            Ray dragRay = mainCamera.ScreenPointToRay(mousePosition);
            float distance = Vector3.Distance(mainCamera.transform.position, instantiatedContainer.transform.position);
            RaycastHit[] allRaycasts = Physics.RaycastAll(dragRay, distance, 1 << 9);
            foreach (RaycastHit dragHit in allRaycasts)
            {
                BeatmapObjectContainer con = dragHit.transform.GetComponent<BeatmapObjectContainer>();
                if (con != instantiatedContainer && con != draggedObjectContainer &&
                    con.objectData.beatmapType == queuedData.beatmapType && con.objectData._time == queuedData._time)
                { //Test these guys against a potentially overridden function to make sure little accidents happen.
                    if (IsObjectOverlapping(queuedData, con.objectData as BO))
                        objectContainerCollection.DeleteObject(con);
                }
            }
            isDraggingObject = false;
            queuedData = BeatmapObject.GenerateCopy(originalQueued);
            ClickAndDragFinished();
        }
    }

    protected virtual void Update()
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit[] BeatmapObjectsHit = Physics.RaycastAll(ray, 999f);
        isOnPlacement = false;
        foreach (RaycastHit objectHit in BeatmapObjectsHit)
        {
            if (!isOnPlacement && objectHit.transform.GetComponentsInParent(GetType()).Any())
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
            Transform hitTransform = hit.transform; //Make a reference to the transform instead of calling hit.transform a lot
            if (!hitTransform.IsChildOf(transform) || hitTransform.GetComponent<PlacementMessageSender>() == null ||
                PersistentUI.Instance.DialogBox_IsEnabled)
            {
                ColliderExit();
                return;
            }
            if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
            if (BeatmapObjectContainerCollection.TrackFilterID != null && !objectContainerCollection.IgnoreTrackFilter)
            {
                if (queuedData._customData == null) queuedData._customData = new SimpleJSON.JSONObject();
                queuedData._customData["track"] = BeatmapObjectContainerCollection.TrackFilterID;
            }
            else queuedData?._customData?.Remove("track");
            CalculateTimes(hit, out Vector3 transformedPoint, out _, out float roundedTime, out _, out _);
            RoundedTime = roundedTime;
            float placementZ = RoundedTime * EditorScaleController.EditorScale;
            Update360Tracks();
            instantiatedContainer.transform.localPosition = new Vector3(
                Mathf.Ceil(transformedPoint.x) - 0.5f,
                Mathf.Floor(transformedPoint.y) + 0.5f,
                placementZ);
            OnPhysicsRaycast(hit, transformedPoint);
            if (isDraggingObject && queuedData != null)
            {
                TransferQueuedToDraggedObject(ref draggedObjectData, BeatmapObject.GenerateCopy(queuedData));
                draggedObjectContainer.objectData = draggedObjectData;
                draggedObjectContainer.objectData._time = placementZ / EditorScaleController.EditorScale;
                if (draggedObjectContainer != null)
                {
                    draggedObjectContainer?.UpdateGridPosition();
                }
                AfterDraggedObjectDataChanged();
            }
        }
        else
        {
            ColliderExit();
            return;
        }
    }

    public virtual void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        mousePosition = Mouse.current.position.ReadValue();
    }

    public void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (context.performed)
            CancelPlacement();
    }
}

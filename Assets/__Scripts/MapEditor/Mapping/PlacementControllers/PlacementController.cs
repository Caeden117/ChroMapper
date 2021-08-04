using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    [SerializeField] protected GridChild gridChild;
    [SerializeField] Transform noteGridTransform;

    [HideInInspector] protected virtual bool CanClickAndDrag { get; set; } = true;

    [HideInInspector] internal virtual float RoundedTime { get; set; } = 0;

    protected bool isDraggingObject = false;
    protected bool isDraggingObjectAtTime = false;
    protected bool usePrecisionPlacement = false;
    protected Vector2 mousePosition;
    protected bool isOnPlacement = false;
    protected Camera mainCamera = null;

    protected BOC draggedObjectContainer = null;
    private BO draggedObjectData = null;
    private BO originalQueued = null;
    private BO originalDraggedObjectData = null;

    private bool applicationFocus = false;
    private bool applicationFocusChanged = false;

    public virtual bool IsValid { get
        {
            return !Input.GetMouseButton(1) && !SongTimelineController.IsHovering && IsActive && 
                !BoxSelectionPlacementController.IsSelecting && applicationFocus && !SceneTransitionManager.IsLoading && KeybindsController.IsMouseInWindow &&
                !DeleteToolController.IsActive && !NodeEditorController.IsActive;
        } }

    public Bounds bounds = default;
    public bool IsActive = false;

    internal BO queuedData; //Data that is not yet applied to the BeatmapObjectContainer.
    internal BOC instantiatedContainer;

    internal virtual void Start()
    {
        queuedData = GenerateOriginalData();
        IsActive = startingActiveState;
        mainCamera = Camera.main;
    }

    protected virtual bool TestForType<T>(Intersections.IntersectionHit hit, BeatmapObject.Type type) where T : MonoBehaviour
    {
        var placementObj = hit.GameObject.GetComponentInParent<T>();
        if (placementObj != null)
        {
            var boundLocal = placementObj.GetComponentsInChildren<Renderer>().FirstOrDefault(it => it.name == "Grid X").bounds;

            // Transform the bounds into the pseudo-world space we use for selection
            var localTransform = placementObj.transform;
            var localScale = localTransform.localScale;
            var boundsNew = localTransform.InverseTransformBounds(boundLocal);
            boundsNew.center += localTransform.localPosition;
            boundsNew.extents = new Vector3(
                boundsNew.extents.x * localScale.x,
                boundsNew.extents.y * localScale.y,
                boundsNew.extents.z * localScale.z
            );

            if (bounds == default)
            {
                bounds = boundsNew;
            }
            else
            {
                // Probably a bad idea but why not drag between lanes
                bounds.Encapsulate(boundsNew);
            }
            return true;
        }
        return false;
    }
    
    protected void CalculateTimes(Intersections.IntersectionHit hit, out Vector3 roundedHit, out float roundedTime)
    {
        float currentBeat = isDraggingObjectAtTime ? draggedObjectData._time : atsc.CurrentBeat;

        roundedHit = parentTrack.InverseTransformPoint(hit.Point);
        float realTime = roundedHit.z / EditorScaleController.EditorScale;

        if (hit.GameObject.transform.parent.name.Contains("Interface"))
        {
            realTime = parentTrack.InverseTransformPoint(hit.GameObject.transform.parent.position).z / EditorScaleController.EditorScale;
        }

        float roundedCurrent = atsc.FindRoundedBeatTime(currentBeat);
        float offsetTime = currentBeat - roundedCurrent;

        roundedTime = atsc.FindRoundedBeatTime(realTime - offsetTime) + atsc.offsetBeat;

        if (!atsc.IsPlaying) roundedTime += offsetTime;
    }

    void ColliderExit()
    {
        if (instantiatedContainer != null) instantiatedContainer.SafeSetActive(false);
    }

    internal virtual void RefreshVisuals()
    {
        instantiatedContainer = Instantiate(objectContainerPrefab,
            parentTrack).GetComponent(typeof(BOC)) as BOC;
        instantiatedContainer.Setup();
        instantiatedContainer.OutlineVisible = false;

        foreach (var collider in instantiatedContainer.GetComponentsInChildren<IntersectionCollider>(true))
            Destroy(collider);

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
            Track track = manager.GetTrackAtTime(RoundedTime);
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
        objectData = queuedData;
        objectData._time = RoundedTime;
        //objectContainerCollection.RemoveConflictingObjects(new[] { objectData }, out List<BeatmapObject> conflicting);
        objectContainerCollection.SpawnObject(objectData, out List<BeatmapObject> conflicting);
        BeatmapActionContainer.AddAction(GenerateAction(objectData, conflicting));
        queuedData = BeatmapObject.GenerateCopy(queuedData);
    }

    public abstract BO GenerateOriginalData();
    public abstract BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting);
    public abstract void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint);

    public virtual void ClickAndDragFinished() { }

    public virtual void CancelPlacement() { }

    public virtual int PlacementXMin => 0;

    public virtual int PlacementXMax => GridOrderController.GetSizeForOrder(gridChild.Order);

    public abstract void TransferQueuedToDraggedObject(ref BO dragged, BO queued);

    public void OnPlaceObject(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) || !KeybindsController.IsMouseInWindow || !context.performed) return;
        if (!isDraggingObject && !isDraggingObjectAtTime && isOnPlacement && instantiatedContainer != null && IsValid
            && !PersistentUI.Instance.DialogBox_IsEnabled &&
            queuedData?._time >= 0 && !applicationFocusChanged && instantiatedContainer.gameObject.activeSelf) ApplyToMap();
    }

    public void OnInitiateClickandDrag(InputAction.CallbackContext context)
    {
        if (usePrecisionPlacement) return;
        if (context.performed && CanClickAndDrag)
        {
            Ray dragRay = mainCamera.ScreenPointToRay(mousePosition);
            instantiatedContainer?.gameObject?.SetActive(false);
            if (Intersections.Raycast(dragRay, 9, out var dragHit))
            {
                BeatmapObjectContainer con = dragHit.GameObject.GetComponentInParent<BeatmapObjectContainer>();
                if (StartDrag(con))
                {
                    isDraggingObject = true;
                }
            }
        }
        else if (context.canceled && isDraggingObject && instantiatedContainer != null)
        {
            FinishDrag();
        }
    }

    public void OnInitiateClickandDragatTime(InputAction.CallbackContext context)
    {
        if (usePrecisionPlacement) return;
        if (context.performed && CanClickAndDrag)
        {
            Ray dragRay = mainCamera.ScreenPointToRay(mousePosition);
            if (Intersections.Raycast(dragRay, 9, out var dragHit))
            {
                BeatmapObjectContainer con = dragHit.GameObject.GetComponentInParent<BeatmapObjectContainer>();
                if (StartDrag(con))
                {
                    isDraggingObjectAtTime = true;
                    float newZ = (con.objectData._time - atsc.CurrentBeat) * EditorScaleController.EditorScale;
                    noteGridTransform.localPosition = new Vector3(noteGridTransform.localPosition.x, noteGridTransform.localPosition.y, newZ);
                }

            }
        }
        else if (context.canceled && isDraggingObjectAtTime && instantiatedContainer != null)
        {
            noteGridTransform.localPosition = new Vector3(noteGridTransform.localPosition.x, noteGridTransform.localPosition.y, 0);
            FinishDrag();
        }
    }

    private bool StartDrag(BeatmapObjectContainer con)
    {
        if (con is null || !(con is BOC) || con.objectData.beatmapType != objectDataType || !IsActive) return false; //Filter out null objects and objects that aren't what we're targetting.
        draggedObjectData = con.objectData as BO;
        originalQueued = BeatmapObject.GenerateCopy(queuedData);
        originalDraggedObjectData = BeatmapObject.GenerateCopy(con.objectData as BO);
        queuedData = BeatmapObject.GenerateCopy(draggedObjectData);
        draggedObjectContainer = con as BOC;
        draggedObjectContainer.dragging = true;
        return true;
    }

    private void FinishDrag()
    {
        if (!(isDraggingObject || isDraggingObjectAtTime)) return;
        //First, find and delete anything that's overlapping our dragged object.
        var selected = SelectionController.IsObjectSelected(draggedObjectData);

        // To delete properly we need to set the original time
        float _time = draggedObjectData._time;
        draggedObjectData._time = originalDraggedObjectData._time;
        objectContainerCollection.DeleteObject(draggedObjectData, false, false);
        draggedObjectData._time = _time;

        objectContainerCollection.SpawnObject(draggedObjectData, out List<BeatmapObject> conflicting, true, true);
        if (conflicting.Contains(draggedObjectData))
        {
            conflicting.Remove(draggedObjectData);

            if (selected)
            {
                SelectionController.Select(draggedObjectData);
            }
        }

        queuedData = BeatmapObject.GenerateCopy(originalQueued);
        BeatmapAction action;
        // Don't queue an action if we didn't actually change anything
        if (draggedObjectData.ToString() != originalDraggedObjectData.ToString())
        {
            if (conflicting.Any())
            {
                action = new BeatmapObjectModifiedWithConflictingAction(draggedObjectData, draggedObjectData, originalDraggedObjectData, conflicting.First(), "Modified via alt-click and drag.");
            }
            else
            {
                action = new BeatmapObjectModifiedAction(draggedObjectData, draggedObjectData, originalDraggedObjectData, "Modified via alt-click and drag.");
            }
            BeatmapActionContainer.AddAction(action);
        }

        draggedObjectContainer.dragging = false;
        draggedObjectContainer = null;
        ClickAndDragFinished();
        isDraggingObject = isDraggingObjectAtTime = false;
    }

    protected virtual void Update()
    {
        if ((isDraggingObject && !Input.GetMouseButton(0)) || (isDraggingObjectAtTime && !Input.GetMouseButton(1)))
        {
            noteGridTransform.localPosition = new Vector3(noteGridTransform.localPosition.x, noteGridTransform.localPosition.y, 0);
            FinishDrag();
        }
        if (Application.isFocused != applicationFocus)
        {
            applicationFocus = Application.isFocused;
            applicationFocusChanged = true;
            return;
        }
        else if (applicationFocusChanged)
        {
            applicationFocusChanged = false;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        var gridsHit = Intersections.RaycastAll(ray, 11);
        isOnPlacement = false;

        foreach (var objectHit in gridsHit)
        {
            if (!isOnPlacement && objectHit.GameObject.GetComponentInParent(GetType()) != null)
            {
                isOnPlacement = true;
                break;
            }
        }

        if (PauseManager.IsPaused) return;

        if ((!IsValid && ((!isDraggingObject && !isDraggingObjectAtTime) || !IsActive)) || !isOnPlacement)
        {
            ColliderExit();
            return;
        }

        if (instantiatedContainer == null) RefreshVisuals();

        if (!instantiatedContainer.gameObject.activeSelf) instantiatedContainer.gameObject.SetActive(true);

        objectData = queuedData;

        if (gridsHit.Any())
        {
            var hit = gridsHit.OrderBy(i => i.Distance).First();

            Transform hitTransform = hit.GameObject.transform; //Make a reference to the transform instead of calling hit.transform a lot
            if (!hitTransform.IsChildOf(transform) || PersistentUI.Instance.DialogBox_IsEnabled)
            {
                ColliderExit();
                return;
            }
            if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
            if (BeatmapObjectContainerCollection.TrackFilterID != null && !objectContainerCollection.IgnoreTrackFilter)
            {
                queuedData.GetOrCreateCustomData()["track"] = BeatmapObjectContainerCollection.TrackFilterID;
            }
            else queuedData?._customData?.Remove("track");
            CalculateTimes(hit, out Vector3 roundedHit, out float roundedTime);
            RoundedTime = roundedTime;
            float placementZ = RoundedTime * EditorScaleController.EditorScale;
            Update360Tracks();

            //this mess of localposition and position assignments are to align the shits up with the grid
            //and to hopefully not cause IndexOutOfRangeExceptions
            instantiatedContainer.transform.localPosition = parentTrack.InverseTransformPoint(hit.Point); //fuck transformedpoint we're doing it ourselves

            Vector3 localMax = parentTrack.InverseTransformPoint(hit.Bounds.max);
            Vector3 localMin = parentTrack.InverseTransformPoint(hit.Bounds.min);
            float farRightPoint = PlacementXMax;
            float farLeftPoint = PlacementXMin;
            float farTopPoint = localMax.y;
            float farBottomPoint = localMin.y;

            roundedHit = new Vector3(Mathf.Ceil(roundedHit.x), Mathf.Ceil(roundedHit.y), placementZ);
            instantiatedContainer.transform.localPosition = roundedHit - new Vector3(0.5f, 1f, 0);
            float x = instantiatedContainer.transform.localPosition.x; //Clamp values to prevent exceptions
            float y = instantiatedContainer.transform.localPosition.y;
            instantiatedContainer.transform.localPosition = new Vector3(
                Mathf.Clamp(x, farLeftPoint + 0.5f, farRightPoint - 0.5f),
                Mathf.Round(Mathf.Clamp(y, farBottomPoint, farTopPoint - 1)) + 0.5f,
                instantiatedContainer.transform.localPosition.z);

            OnPhysicsRaycast(hit, roundedHit);
            queuedData._time = RoundedTime;
            if ((isDraggingObject || isDraggingObjectAtTime) && queuedData != null)
            {
                TransferQueuedToDraggedObject(ref draggedObjectData, BeatmapObject.GenerateCopy(queuedData));
                draggedObjectContainer.objectData._time = placementZ / EditorScaleController.EditorScale;
                if (draggedObjectContainer != null)
                {
                    draggedObjectContainer.UpdateGridPosition();
                }
            }
        }
        else
        {
            ColliderExit();
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

    public void OnPrecisionPlacementToggle(InputAction.CallbackContext context)
    {
        usePrecisionPlacement = context.performed && Settings.Instance.PrecisionPlacementGrid;
    }

    protected BOC ObjectUnderCursor() {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return null;

        var ray = mainCamera.ScreenPointToRay(mousePosition);
        return !Intersections.Raycast(ray, 9, out var hit) ? null : hit.GameObject.GetComponentInParent<BOC>();
    }

    private void OnDestroy() => Intersections.Clear();
}

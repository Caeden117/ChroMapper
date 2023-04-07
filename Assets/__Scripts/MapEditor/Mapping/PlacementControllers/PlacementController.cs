using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public abstract class PlacementController<TBo, TBoc, TBocc> : MonoBehaviour, CMInput.IPlacementControllersActions,
    CMInput.ICancelPlacementActions where TBo : BaseObject
    where TBoc : ObjectContainer
    where TBocc : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject objectContainerPrefab;
    [SerializeField] private TBo objectData;
    [FormerlySerializedAs("ObjectContainerCollection")][SerializeField] internal TBocc objectContainerCollection;
    [FormerlySerializedAs("parentTrack")][SerializeField] protected Transform ParentTrack;
    [FormerlySerializedAs("interfaceGridParent")][SerializeField] protected Transform InterfaceGridParent;
    [SerializeField] protected bool AssignTo360Tracks;
    [SerializeField] private ObjectType objectDataType;
    [SerializeField] private bool startingActiveState;
    [FormerlySerializedAs("atsc")][SerializeField] protected AudioTimeSyncController Atsc;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [FormerlySerializedAs("tracksManager")][SerializeField] protected TracksManager TracksManager;
    [FormerlySerializedAs("gridRotation")][SerializeField] protected RotationCallbackController GridRotation;
    [FormerlySerializedAs("gridChild")][SerializeField] protected GridChild GridChild;
    [SerializeField] private Transform noteGridTransform;

    [FormerlySerializedAs("bounds")] public Bounds Bounds;
    public bool IsActive;

    private bool applicationFocus;
    private bool applicationFocusChanged;

    protected TBoc DraggedObjectContainer;
    private TBo draggedObjectData;
    internal TBoc instantiatedContainer;

    protected bool IsDraggingObject;
    protected bool IsDraggingObjectAtTime;
    protected bool IsOnPlacement;
    protected Camera MainCamera;
    protected Vector2 MousePosition;
    private TBo originalDraggedObjectData;
    private TBo originalQueued;

    internal TBo queuedData; //Data that is not yet applied to the ObjectContainer.
    protected bool UsePrecisionPlacement;

    [HideInInspector] protected virtual bool CanClickAndDrag { get; set; } = true;

    [HideInInspector] internal virtual float RoundedTime { get; set; }

    public virtual bool IsValid => !Input.GetMouseButton(1) && !SongTimelineController.IsHovering && IsActive &&
                                   !BoxSelectionPlacementController.IsSelecting && applicationFocus &&
                                   !SceneTransitionManager.IsLoading && KeybindsController.IsMouseInWindow &&
                                   !DeleteToolController.IsActive && !NodeEditorController.IsActive;

    public virtual int PlacementXMin => 0;

    public virtual int PlacementXMax => GridOrderController.GetSizeForOrder(GridChild.Order);

    internal virtual void Start()
    {
        queuedData = GenerateOriginalData();
        IsActive = startingActiveState;
        MainCamera = Camera.main;
    }

    protected virtual void Update()
    {
        if ((IsDraggingObject && !Input.GetMouseButton(0)) || (IsDraggingObjectAtTime && !Input.GetMouseButton(1)))
        {
            noteGridTransform.localPosition =
                new Vector3(noteGridTransform.localPosition.x, noteGridTransform.localPosition.y, 0);
            FinishDrag();
        }

        if (Application.isFocused != applicationFocus)
        {
            applicationFocus = Application.isFocused;
            applicationFocusChanged = true;
            return;
        }

        if (applicationFocusChanged) applicationFocusChanged = false;

        var ray = MainCamera.ScreenPointToRay(MousePosition);
        var gridsHit = Intersections.RaycastAll(ray, 11);
        IsOnPlacement = false;

        foreach (var objectHit in gridsHit)
        {
            if (!IsOnPlacement && objectHit.GameObject.GetComponentInParent(GetType()) != null)
            {
                IsOnPlacement = true;
                break;
            }
        }

        if (PauseManager.IsPaused) return;

        if ((!IsValid && ((!IsDraggingObject && !IsDraggingObjectAtTime) || !IsActive)) || !IsOnPlacement)
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

            var hitTransform =
                hit.GameObject.transform; //Make a reference to the transform instead of calling hit.transform a lot
            if (!hitTransform.IsChildOf(transform) || PersistentUI.Instance.DialogBoxIsEnabled)
            {
                ColliderExit();
                return;
            }

            if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
            if (BeatmapObjectContainerCollection.TrackFilterID != null && !objectContainerCollection.IgnoreTrackFilter)
                queuedData.CustomTrack = BeatmapObjectContainerCollection.TrackFilterID;
            else
                queuedData.CustomTrack = null;

            CalculateTimes(hit, out var roundedHit, out var roundedTime);
            RoundedTime = roundedTime;
            var placementZ = RoundedTime * EditorScaleController.EditorScale;
            Update360Tracks();

            //this mess of localposition and position assignments are to align the shits up with the grid
            //and to hopefully not cause IndexOutOfRangeExceptions
            instantiatedContainer.transform.localPosition =
                ParentTrack.InverseTransformPoint(hit.Point); //fuck transformedpoint we're doing it ourselves

            var localMax = ParentTrack.InverseTransformPoint(hit.Bounds.max);
            var localMin = ParentTrack.InverseTransformPoint(hit.Bounds.min);
            float farRightPoint = PlacementXMax;
            float farLeftPoint = PlacementXMin;
            var farTopPoint = localMax.y;
            var farBottomPoint = localMin.y;

            roundedHit = new Vector3(Mathf.Ceil(roundedHit.x), Mathf.Ceil(roundedHit.y), placementZ);
            instantiatedContainer.transform.localPosition = roundedHit - new Vector3(0.5f, 1f, 0);
            var x = instantiatedContainer.transform.localPosition.x; //Clamp values to prevent exceptions
            var y = instantiatedContainer.transform.localPosition.y;
            instantiatedContainer.transform.localPosition = new Vector3(
                Mathf.Clamp(x, farLeftPoint + 0.5f, farRightPoint - 0.5f),
                Mathf.Round(Mathf.Clamp(y, farBottomPoint, farTopPoint - 1)) + 0.5f,
                instantiatedContainer.transform.localPosition.z);

            OnPhysicsRaycast(hit, roundedHit);
            queuedData.JsonTime = RoundedTime;
            if ((IsDraggingObject || IsDraggingObjectAtTime) && queuedData != null)
            {
                TransferQueuedToDraggedObject(ref draggedObjectData, BeatmapFactory.Clone(queuedData));
                if (DraggedObjectContainer != null) DraggedObjectContainer.UpdateGridPosition();
            }
        }
        else
        {
            ColliderExit();
        }
    }

    private void OnDestroy() => Intersections.Clear();

    public void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (context.performed)
            CancelPlacement();
    }

    public virtual void OnPlaceObject(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) ||
            !KeybindsController.IsMouseInWindow || !context.performed)
        {
            return;
        }

        if (!IsDraggingObject && !IsDraggingObjectAtTime && IsOnPlacement && instantiatedContainer != null && IsValid
            && !PersistentUI.Instance.DialogBoxIsEnabled &&
            queuedData?.JsonTime >= 0 && !applicationFocusChanged && instantiatedContainer.gameObject.activeSelf)
        {
            ApplyToMap();
        }
    }

    public void OnInitiateClickandDrag(InputAction.CallbackContext context)
    {
        if (context.performed && CanClickAndDrag)
        {
            var dragRay = MainCamera.ScreenPointToRay(MousePosition);

            if (instantiatedContainer != null)
            {
                instantiatedContainer.gameObject.SetActive(false);
            }

            if (Intersections.Raycast(dragRay, 9, out var dragHit))
            {
                var con = dragHit.GameObject.GetComponentInParent<ObjectContainer>();
                if (StartDrag(con)) IsDraggingObject = true;
            }
        }
        else if (context.canceled && IsDraggingObject && instantiatedContainer != null)
        {
            FinishDrag();
        }
    }

    protected virtual float GetContainerPosZ(ObjectContainer con)
    {
        return (con.ObjectData.JsonTime - Atsc.CurrentBeat) * EditorScaleController.EditorScale;
    }

    public void OnInitiateClickandDragatTime(InputAction.CallbackContext context)
    {
        if (context.performed && CanClickAndDrag)
        {
            var dragRay = MainCamera.ScreenPointToRay(MousePosition);
            if (Intersections.Raycast(dragRay, 9, out var dragHit))
            {
                var con = dragHit.GameObject.GetComponentInParent<ObjectContainer>();
                if (StartDrag(con))
                {
                    IsDraggingObjectAtTime = true;
                    var newZ = GetContainerPosZ(con);
                    noteGridTransform.localPosition = new Vector3(noteGridTransform.localPosition.x,
                        noteGridTransform.localPosition.y, newZ);
                }
            }
        }
        else if (context.canceled && IsDraggingObjectAtTime && instantiatedContainer != null)
        {
            noteGridTransform.localPosition =
                new Vector3(noteGridTransform.localPosition.x, noteGridTransform.localPosition.y, 0);
            FinishDrag();
        }
    }

    public virtual void OnMousePositionUpdate(InputAction.CallbackContext context) =>
        MousePosition = Mouse.current.position.ReadValue();

    public void OnPrecisionPlacementToggle(InputAction.CallbackContext context) =>
        UsePrecisionPlacement = context.performed && Settings.Instance.PrecisionPlacementGrid;

    protected virtual bool TestForType<T>(Intersections.IntersectionHit hit, ObjectType type)
        where T : MonoBehaviour
    {
        var placementObj = hit.GameObject.GetComponentInParent<T>();
        if (placementObj != null)
        {
            var boundLocal = placementObj.GetComponentsInChildren<Renderer>().FirstOrDefault(it => it.name == "Grid X")
                .bounds;

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

            if (Bounds == default)
                Bounds = boundsNew;
            else
                // Probably a bad idea but why not drag between lanes
                Bounds.Encapsulate(boundsNew);
            return true;
        }

        return false;
    }

    protected void CalculateTimes(Intersections.IntersectionHit hit, out Vector3 roundedHit, out float roundedTime)
    {
        var currentBeat = IsDraggingObjectAtTime ? draggedObjectData.JsonTime : Atsc.CurrentBeat;

        roundedHit = ParentTrack.InverseTransformPoint(hit.Point);
        var realTime = roundedHit.z / EditorScaleController.EditorScale;

        if (hit.GameObject.transform.parent.name.Contains("Interface"))
        {
            realTime = ParentTrack.InverseTransformPoint(hit.GameObject.transform.parent.position).z /
                       EditorScaleController.EditorScale;
        }

        var roundedCurrent = Atsc.FindRoundedBeatTime(currentBeat);
        var offsetTime = currentBeat - roundedCurrent;

        roundedTime = Atsc.FindRoundedBeatTime(realTime - offsetTime);

        if (!Atsc.IsPlaying) roundedTime += offsetTime;
    }

    private void ColliderExit()
    {
        if (instantiatedContainer != null) instantiatedContainer.SafeSetActive(false);
    }

    internal virtual void RefreshVisuals()
    {
        instantiatedContainer = Instantiate(objectContainerPrefab,
            ParentTrack).GetComponent(typeof(TBoc)) as TBoc;
        instantiatedContainer.Setup();
        instantiatedContainer.OutlineVisible = false;

        foreach (var collider in instantiatedContainer.GetComponentsInChildren<IntersectionCollider>(true))
            Destroy(collider);

        instantiatedContainer.name = $"Hover {objectDataType}";
    }

    private void Update360Tracks()
    {
        if (!AssignTo360Tracks) return;
        var manager = objectContainerCollection.GetComponent<TracksManager>();
        if (manager == null)
        {
            Debug.LogWarning("Could not find an attached TracksManager.");
        }
        else
        {
            var track = manager.GetTrackAtTime(RoundedTime);
            if (track != null)
            {
                var localPos = instantiatedContainer.transform.localPosition;
                ParentTrack = track.ObjectParentTransform;
                instantiatedContainer.transform.SetParent(track.ObjectParentTransform, false);
                instantiatedContainer.transform.localPosition = localPos;
                instantiatedContainer.transform.localEulerAngles = new Vector3(
                    instantiatedContainer.transform.localEulerAngles.x,
                    0, instantiatedContainer.transform.localEulerAngles.z);
            }
        }
    }

    internal virtual void ApplyToMap()
    {
        objectData = queuedData;
        objectData.JsonTime = RoundedTime;
        //objectContainerCollection.RemoveConflictingObjects(new[] { objectData }, out List<BaseObject> conflicting);
        objectContainerCollection.SpawnObject(objectData, out var conflicting);
        BeatmapActionContainer.AddAction(GenerateAction(objectData, conflicting));
        queuedData = BeatmapFactory.Clone(queuedData);
        queuedData.CustomData = null;
    }

    public abstract TBo GenerateOriginalData();
    public abstract BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting);
    public abstract void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint);

    public virtual void ClickAndDragFinished() { }

    public virtual void CancelPlacement() { }

    public abstract void TransferQueuedToDraggedObject(ref TBo dragged, TBo queued);

    private bool StartDrag(ObjectContainer con)
    {
        if (con is null || !(con is TBoc) || con.ObjectData.ObjectType != objectDataType || !IsActive)
            return false; //Filter out null objects and objects that aren't what we're targetting.
        draggedObjectData = con.ObjectData as TBo;
        originalQueued = BeatmapFactory.Clone(queuedData);
        originalDraggedObjectData = BeatmapFactory.Clone(con.ObjectData as TBo);
        queuedData = BeatmapFactory.Clone(draggedObjectData);
        DraggedObjectContainer = con as TBoc;
        DraggedObjectContainer.Dragging = true;
        return true;
    }

    private void FinishDrag()
    {
        if (!(IsDraggingObject || IsDraggingObjectAtTime)) return;
        //First, find and delete anything that's overlapping our dragged object.
        var selected = SelectionController.IsObjectSelected(draggedObjectData);

        // To delete properly we need to set the original time
        var time = draggedObjectData.JsonTime;
        draggedObjectData.JsonTime = originalDraggedObjectData.JsonTime;
        objectContainerCollection.DeleteObject(draggedObjectData, false, false);
        draggedObjectData.JsonTime = time;

        objectContainerCollection.SpawnObject(draggedObjectData, out var conflicting);
        if (conflicting.Contains(draggedObjectData))
        {
            conflicting.Remove(draggedObjectData);

            if (selected) SelectionController.Select(draggedObjectData);
        }

        queuedData = BeatmapFactory.Clone(originalQueued);
        BeatmapAction action;
        // Don't queue an action if we didn't actually change anything
        if (draggedObjectData.ToString() != originalDraggedObjectData.ToString())
        {
            if (conflicting.Any())
            {
                action = new BeatmapObjectModifiedWithConflictingAction(draggedObjectData, draggedObjectData,
                    originalDraggedObjectData, conflicting.First(), "Modified via alt-click and drag.");
            }
            else
            {
                action = new BeatmapObjectModifiedAction(draggedObjectData, draggedObjectData,
                    originalDraggedObjectData, "Modified via alt-click and drag.");
            }

            BeatmapActionContainer.AddAction(action);
        }

        DraggedObjectContainer.Dragging = false;
        DraggedObjectContainer = null;
        ClickAndDragFinished();
        IsDraggingObject = IsDraggingObjectAtTime = false;
    }

    protected TBoc ObjectUnderCursor()
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return null;

        var ray = MainCamera.ScreenPointToRay(MousePosition);
        return !Intersections.Raycast(ray, 9, out var hit) ? null : hit.GameObject.GetComponentInParent<TBoc>();
    }
}

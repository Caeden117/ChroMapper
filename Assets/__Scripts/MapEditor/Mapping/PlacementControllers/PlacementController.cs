using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Animations;
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
    [SerializeField] protected BPMChangeGridContainer BpmChangeGridContainer; // This is stinky. Maybe separate song/json time to another class?
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
    protected TBo draggedObjectData;
    internal TBoc instantiatedContainer;

    [SerializeField] protected CameraManager CameraManager;
    
    protected bool IsDraggingObject;
    protected bool IsDraggingObjectAtTime;
    protected bool IsOnPlacement;
    protected Vector2 MousePosition;
    private TBo originalDraggedObjectData;
    private TBo originalQueued;

    protected List<ObjectContainer> DraggedAttachedSliderContainers = new();
    protected Dictionary<IndicatorType, List<BaseSlider>> DraggedAttachedSliderDatas = new()
    {
        {IndicatorType.Head, new List<BaseSlider>()},
        {IndicatorType.Tail, new List<BaseSlider>()}
    };
    private Dictionary<IndicatorType, List<BaseSlider>> originalDraggedAttachedSliderDatas = new()
    {
        {IndicatorType.Head, new List<BaseSlider>()},
        {IndicatorType.Tail, new List<BaseSlider>()}
    };

    internal TBo queuedData; //Data that is not yet applied to the ObjectContainer.
    protected bool UsePrecisionPlacement;

    protected virtual Vector2 precisionOffset { get; } = new(-0.5f, -1.1f);
    protected virtual Vector2 vanillaOffset { get; } = new(1.5f, -1.1f);

    protected virtual bool CanClickAndDrag { get; set; } = true;

    private float roundedJsonTime;
    internal float RoundedJsonTime
    {
        get => roundedJsonTime;
        set
        {
            SongBpmTime = BpmChangeGridContainer.JsonTimeToSongBpmTime(value);
            roundedJsonTime = value;
        }
    }

    internal float SongBpmTime { get; set; } // No point rounding this

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

        var ray = CameraManager.SelectedCameraController.Camera.ScreenPointToRay(MousePosition);
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

            if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
            if (BeatmapObjectContainerCollection.TrackFilterID != null && !objectContainerCollection.IgnoreTrackFilter)
                queuedData.CustomTrack = BeatmapObjectContainerCollection.TrackFilterID;
            else
                queuedData.CustomTrack = null;

            CalculateTimes(hit, out var roundedHit, out var roundedJsonTime);
            roundedHit += (Vector3)vanillaOffset;
            RoundedJsonTime = roundedJsonTime;
            var placementZ = SongBpmTime * EditorScaleController.EditorScale;
            Update360Tracks();

            roundedHit = new Vector3(Mathf.Round(roundedHit.x), Mathf.Round(roundedHit.y), placementZ);

            var localMax = ParentTrack.InverseTransformPoint(hit.Bounds.max);
            var localMin = ParentTrack.InverseTransformPoint(hit.Bounds.min);
            float farRightPoint = PlacementXMax;
            float farLeftPoint = PlacementXMin;
            var farTopPoint = localMax.y;
            var farBottomPoint = localMin.y;

            var x = roundedHit.x; //Clamp values to prevent exceptions
            var y = roundedHit.y;
            instantiatedContainer.transform.localPosition = new Vector3(
                Mathf.Clamp(x, farLeftPoint, farRightPoint),
                Mathf.Round(Mathf.Clamp(y, farBottomPoint, farTopPoint - 1)),
                roundedHit.z);

            queuedData.SetTimes(roundedJsonTime, SongBpmTime);
            OnPhysicsRaycast(hit, roundedHit);
            if ((IsDraggingObject || IsDraggingObjectAtTime) && queuedData != null)
            {
                TransferQueuedToDraggedObject(ref draggedObjectData, queuedData);
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
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true) ||
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
            var dragRay = CameraManager.SelectedCameraController.Camera.ScreenPointToRay(MousePosition);

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
        return (con.ObjectData.SongBpmTime - Atsc.CurrentSongBpmTime) * EditorScaleController.EditorScale;
    }

    public void OnInitiateClickandDragatTime(InputAction.CallbackContext context)
    {
        if (context.performed && CanClickAndDrag)
        {
            var dragRay = CameraManager.SelectedCameraController.Camera.ScreenPointToRay(MousePosition);
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

    public void OnPrecisionPlacementToggle(InputAction.CallbackContext context)
    {
        switch (Settings.Instance.PrecisionPlacementMode)
        {
            case PrecisionPlacementMode.Off:
                UsePrecisionPlacement = false;
                break;
            case PrecisionPlacementMode.Hold:
                UsePrecisionPlacement = context.performed;
                break;
            case PrecisionPlacementMode.Toggle:
                if (context.started && !context.performed)
                {
                    UsePrecisionPlacement = !UsePrecisionPlacement;
                }
                break;
        }
    }

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

    protected virtual float GetDraggedObjectJsonTime()
    {
        return draggedObjectData.JsonTime;
    }

    protected void CalculateTimes(Intersections.IntersectionHit hit, out Vector3 roundedHit, out float roundedJsonTime)
    {
        var currentJsonTime = IsDraggingObjectAtTime ? GetDraggedObjectJsonTime() : Atsc.CurrentJsonTime;
        var snap = 1f / Atsc.GridMeasureSnapping;
        var offsetJsonTime = currentJsonTime - (float)Math.Round((currentJsonTime) / snap, MidpointRounding.AwayFromZero) * snap;

        roundedHit = ParentTrack.InverseTransformPoint(hit.Point);
        var realTime = roundedHit.z / EditorScaleController.EditorScale;

        if (hit.GameObject.transform.parent.name.Contains("Interface"))
        {
            realTime = ParentTrack.InverseTransformPoint(hit.GameObject.transform.parent.position).z /
                       EditorScaleController.EditorScale;
        }

        var hitPointJsonTime = BpmChangeGridContainer.SongBpmTimeToJsonTime(realTime);
        roundedJsonTime = (float)Math.Round((hitPointJsonTime - offsetJsonTime) / snap, MidpointRounding.AwayFromZero) * snap;

        if (!Atsc.IsPlaying) roundedJsonTime += offsetJsonTime;
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
        if (instantiatedContainer.GetComponent<ObjectAnimator>() is ObjectAnimator animator)
            animator.enabled = false;

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
            var track = manager.GetTrackAtTime(SongBpmTime);
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

    // TODO(Bullet): Clean up implementations.
    public abstract void TransferQueuedToDraggedObject(ref TBo dragged, TBo queued);

    private bool StartDrag(ObjectContainer con)
    {
        if (con is null || !(con is TBoc) || con.ObjectData.ObjectType != objectDataType || !IsActive)
            return false; //Filter out null objects and objects that aren't what we're targeting.

        objectContainerCollection.SilentRemoveObject(con.ObjectData);
        
        draggedObjectData = con.ObjectData as TBo;
        originalQueued = BeatmapFactory.Clone(queuedData);
        originalDraggedObjectData = BeatmapFactory.Clone(con.ObjectData as TBo);
        queuedData = BeatmapFactory.Clone(draggedObjectData);
        DraggedObjectContainer = con as TBoc;
        DraggedObjectContainer.Dragging = true;

        if (con is NoteContainer noteContainer)
        {
            var noteCollection = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            noteCollection.ClearSpecialAngles(con.ObjectData);

            StartDragSliders(noteContainer);
        }

        return true;
    }

    private void FinishDrag()
    {
        if (!(IsDraggingObject || IsDraggingObjectAtTime)) return;

        // Spawn our dragged object and delete anything that's overlapping.
        objectContainerCollection.SpawnObject(draggedObjectData, out var conflicting);

        queuedData = BeatmapFactory.Clone(originalQueued);
        var actions = new List<BeatmapAction>();
        // Don't queue an action if we didn't actually change anything
        if (draggedObjectData.ToString() != originalDraggedObjectData.ToString())
        {
            if (conflicting.Any())
            {
                actions.Add(new BeatmapObjectModifiedWithConflictingAction(draggedObjectData, draggedObjectData,
                    originalDraggedObjectData, conflicting, "Modified via alt-click and drag."));
            }
            else
            {
                actions.Add(new BeatmapObjectModifiedAction(draggedObjectData, draggedObjectData,
                    originalDraggedObjectData, "Modified via alt-click and drag."));
            }

            SelectionController.SelectionChangedEvent?.Invoke();
        }

        if (DraggedObjectContainer is NoteContainer)
        {
            var noteCollection = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            noteCollection.RefreshSpecialAngles(draggedObjectData, false, false);

            FinishSliderDrag(actions);
            ClearDraggedAttachedSliders();
        }

        if (actions.Count == 1)
            BeatmapActionContainer.AddAction(actions[0]);
        else if (actions.Count > 1)
            BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, true, "Modified via alt-click and drag"));

        DraggedObjectContainer.Dragging = false;
        DraggedObjectContainer = null;
        ClickAndDragFinished();
        IsDraggingObject = IsDraggingObjectAtTime = false;
    }

    protected TBoc ObjectUnderCursor()
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return null;

        var ray = CameraManager.SelectedCameraController.Camera.ScreenPointToRay(MousePosition);
        return !Intersections.Raycast(ray, 9, out var hit) ? null : hit.GameObject.GetComponentInParent<TBoc>();
    }

    # region Attached Slider Functions

    private void StartDragSliders(NoteContainer noteContainer)
    {
        var noteData = noteContainer.NoteData;
        var epsilon = BeatmapObjectContainerCollection.Epsilon;

        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        foreach (var arcContainer in arcCollection.LoadedContainers)
        {
            var arcData = arcContainer.Key as BaseArc;
            var isConnectedToHead = Mathf.Abs(arcData.JsonTime - noteData.JsonTime) < epsilon && arcData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(arcData.TailJsonTime - noteData.JsonTime) < epsilon && arcData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                originalDraggedAttachedSliderDatas[IndicatorType.Head].Add(BeatmapFactory.Clone(arcData));
                DraggedAttachedSliderDatas[IndicatorType.Head].Add(arcData);
                DraggedAttachedSliderContainers.Add(arcContainer.Value);
                arcCollection.SilentRemoveObject(arcData);
            }
            else if (isConnectedToTail)
            {
                originalDraggedAttachedSliderDatas[IndicatorType.Tail].Add(BeatmapFactory.Clone(arcData));
                DraggedAttachedSliderDatas[IndicatorType.Tail].Add(arcData);
                DraggedAttachedSliderContainers.Add(arcContainer.Value);
                arcCollection.SilentRemoveObject(arcData);
            }
        }

        var chainCollection = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        foreach (var chainContainer in chainCollection.LoadedContainers)
        {
            var chainData = chainContainer.Key as BaseChain;
            var isConnectedToHead = Mathf.Abs(chainData.JsonTime - noteData.JsonTime) < epsilon && chainData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(chainData.TailJsonTime - noteData.JsonTime) < epsilon && chainData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                originalDraggedAttachedSliderDatas[IndicatorType.Head].Add(BeatmapFactory.Clone(chainData));
                DraggedAttachedSliderDatas[IndicatorType.Head].Add(chainData);
                DraggedAttachedSliderContainers.Add(chainContainer.Value);
                chainCollection.SilentRemoveObject(chainData);
            }
            else if (isConnectedToTail)
            {
                originalDraggedAttachedSliderDatas[IndicatorType.Tail].Add(BeatmapFactory.Clone(chainData));
                DraggedAttachedSliderDatas[IndicatorType.Tail].Add(chainData);
                DraggedAttachedSliderContainers.Add(chainContainer.Value);
                chainCollection.SilentRemoveObject(chainData);
            }
        }
    }

    private void FinishSliderDrag(List<BeatmapAction> actions)
    {
        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        var chainCollection = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

        for (int i = 0; i < DraggedAttachedSliderDatas[IndicatorType.Head].Count; i++)
        {
            var draggedSlider = DraggedAttachedSliderDatas[IndicatorType.Head][i];
            var originalDraggedSlider = originalDraggedAttachedSliderDatas[IndicatorType.Head][i];

            if (draggedSlider is BaseArc draggedArc)
            {
                SpawnDraggedSlider(arcCollection, draggedArc, originalDraggedSlider, actions);
            }
            else if (draggedSlider is BaseChain draggedChain)
            {
                SpawnDraggedSlider(chainCollection, draggedChain, originalDraggedSlider, actions);
            }
        }

        for (int i = 0; i < DraggedAttachedSliderDatas[IndicatorType.Tail].Count; i++)
        {
            var draggedSlider = DraggedAttachedSliderDatas[IndicatorType.Tail][i];
            var originalDraggedSlider = originalDraggedAttachedSliderDatas[IndicatorType.Tail][i];

            if (draggedSlider is BaseArc draggedArc)
            {
                SpawnDraggedSlider(arcCollection, draggedArc, originalDraggedSlider, actions);
            }
            else if (draggedSlider is BaseChain draggedChain)
            {
                SpawnDraggedSlider(chainCollection, draggedChain, originalDraggedSlider, actions);
            }
        }
    }

    private void SpawnDraggedSlider(BeatmapObjectContainerCollection sliderCollection, BaseSlider draggedSlider,
        BaseObject originalSlider, List<BeatmapAction> actions)
    {
        sliderCollection.SpawnObject(draggedSlider, out var conflictingArcs);

        // Don't queue an action if we didn't actually change anything
        if (draggedSlider.ToString() != originalSlider.ToString())
        {
            if (conflictingArcs.Any())
            {
                actions.Add(new BeatmapObjectModifiedWithConflictingAction(draggedSlider, draggedSlider,
                    originalSlider, conflictingArcs, "Modified via alt-click and drag."));
            }
            else
            {
                actions.Add(new BeatmapObjectModifiedAction(draggedSlider, draggedSlider,
                    originalSlider, "Modified via alt-click and drag."));
            }
        }
    }

    private void ClearDraggedAttachedSliders()
    {
        DraggedAttachedSliderContainers.Clear();
        DraggedAttachedSliderDatas[IndicatorType.Head].Clear();
        DraggedAttachedSliderDatas[IndicatorType.Tail].Clear();
        originalDraggedAttachedSliderDatas[IndicatorType.Head].Clear();
        originalDraggedAttachedSliderDatas[IndicatorType.Tail].Clear();
    }

    # endregion
}

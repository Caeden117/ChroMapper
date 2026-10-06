using System;
using System.Collections.Generic;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

/// <summary>
/// Base class for all placements. Handles the basic logic for placing objects on the grid.
/// ShowVisual / HideVisual help with stuff.
/// </summary>
public abstract class BasePlacement : MonoBehaviour
{
    // Every gameplay placement shares this setting, so own it above note, bomb, and wall implementations.
    public static readonly string ChromaColorKey = "PlaceChromaObjects";

    // Notify all placement-adjacent controls when the shared gameplay-object Chroma setting changes.
    public static event Action<bool> OnPlaceChromaObjectsChanged;

    [SerializeField] public ObjectType ObjectDataType;
    [SerializeField] public GameObject ObjectContainerPrefab;

    [Tooltip("This is required to be on game object with track as it is used to track and compare time")]
    [SerializeField]
    public Transform PlacementTrack;

    public bool CanPrecisionPlacement;
    public bool AdjustZScale;

    [Header("Dependencies")] [SerializeField]
    public CustomStandaloneInputModule CustomStandaloneInputModule;

    [SerializeField] public AudioTimeSyncController Atsc;
    [SerializeField] public BoxSelectionPlacement boxSelectionPlacement;

    [Header("360/90")] [SerializeField] public bool AssignTo360Tracks;
    [SerializeField] public TracksManager TracksManager;
    [SerializeField] public LaneRotationProvider LaneRotationProvider;

    [Header("State")]
    [Tooltip("If you have multiple placement in a single grid, consider making control flow and toggle this condition")]
    public bool AllowPlacement = true;

    public PlacementState State;
    public bool IsDragging;
    public float JsonTimeRounded;
    public Bounds Bounds;
    public Vector3 BoundsPosition;
    protected Vector3 LanePosition;

    public virtual bool CanClickAndDrag => true;
    public virtual bool CanPlace => boxSelectionPlacement.State == PlacementState.Idle;

    // Two-click placement types can retain their first endpoint while the cursor temporarily leaves a valid grid surface.
    public virtual bool RetainsPendingPlacementOnInvalidHit => false;

    public bool IsIdle => State == PlacementState.Idle;
    public bool IsActive => State == PlacementState.Active;
    public bool IsPlacing => State == PlacementState.Placing;

    // Keep the Chroma object state separate from lighting-event placement while making it available to every placement type.
    public static bool CanPlaceChromaObjects => (bool)Settings.NonPersistentSettings.GetValueOrDefault(ChromaColorKey, false);

    // Route all controls through one notification path so the color tile and picker cannot diverge.
    public static void SetPlaceChromaObjects(bool enabled)
    {
        Settings.NonPersistentSettings[ChromaColorKey] = enabled;
        OnPlaceChromaObjectsChanged?.Invoke(enabled);
    }

    public float RoundedJsonTime
    {
        get => JsonTimeRounded;
        set
        {
            SongBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(value);
            JsonTimeRounded = value;
        }
    }

    protected float SongBpmTime { get; private set; } // No point rounding this

    protected static Vector2 GridOffset => Vector2.one * 0.5f;

    public abstract void Initialize(PlacementProvider provider);
    public abstract void UpdateState(Intersections.IntersectionHit hit, PlacementInputState inputState);
    public abstract void ShowVisual();
    public abstract void HideVisual();

    protected abstract void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint);
    protected virtual void HandlePlacementToData(PlacementInputState inputState) { }

    public abstract void Exit();
    public abstract void Apply();
    public virtual void Cancel() { }

    public abstract ObjectContainer StartDrag(GameObject draggedObject);
    protected abstract List<BeatmapAction> PerformPreFinishDragActions();
    public abstract void FinishDrag();
    protected virtual void HandleDragged() { }

    public virtual float GetContainerPosZ(ObjectContainer con) =>
        (con.ObjectData.SongBpmTime - Atsc.CurrentSongBpmTime) * EditorScaleController.EditorScale;
}

public abstract class BasePlacement<TObject, TContainer, TCollection> : BasePlacement
    where TObject : BaseObject
    where TContainer : ObjectContainer
    where TCollection : BeatmapObjectContainerCollection
{
    [Header("Data")] public TCollection ObjectContainerCollection;

    public TContainer PlacementVisualContainer;

    public TContainer DraggedObjectContainer;
    public TObject DraggedObjectData;

    public TObject OriginalDraggedObjectData;
    public TObject OriginalQueued;

    public TObject QueuedData; //Data that is not yet applied to the ObjectContainer.

    [Header("Implementation")] public bool ForceHeaderPlsIgnore;
    private bool hasPreviousSnappedState;

    private Vector2 previousSnappedState;

    public virtual void Start()
    {
        CreateVisual();
        HideVisual();
        LaneRotationProvider.OnEditChanged += HandleRotationChanged;
        QueuedData ??= GenerateOriginalData();
    }

    public virtual void OnDestroy() => LaneRotationProvider.OnEditChanged -= HandleRotationChanged;

    public event Action OnApplied; // this is an odd name

    protected abstract TObject GenerateOriginalData();
    protected abstract BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts);

    public override void Initialize(PlacementProvider provider)
    {
        ResetHysteresis();
        CreateVisual();
        HideVisual();
        QueuedData ??= GenerateOriginalData();
    }

    public override void UpdateState(
        Intersections.IntersectionHit hit,
        PlacementInputState inputState)
    {
        if (!AllowPlacement && !IsDragging)
        {
            if (!IsActive) return;
            HideVisual();
            State = PlacementState.Idle;
            return;
        }

        if (IsIdle)
            State = PlacementState.Active;

        if (inputState == PlacementInputState.Hover && !PlacementVisualContainer.gameObject.activeSelf)
            ShowVisual();

        if (BeatmapObjectContainerCollection.TrackFilterID != null && !ObjectContainerCollection.IgnoreTrackFilter)
            QueuedData.CustomTrack = BeatmapObjectContainerCollection.TrackFilterID;
        else
            QueuedData.CustomTrack = null;

        var (localPoint, jsonTime) = GetPositionAndTime(hit, inputState);
        RoundedJsonTime = jsonTime;
        QueuedData.JsonTime = jsonTime;

        SetTo360Tracks();
        HandleHitToPlacement(hit, localPoint);
        HandlePlacementToData(inputState);

        // Alt keeps the queued preview visible on empty grid space but hides it after a hovered node or ribbon updates it.
        if (ShouldHideVisualForHoveredTarget() || IsDragging)
        {
            HideVisual();
        }

        if (inputState == PlacementInputState.Hover || !IsDragging) return;
        TransferQueuedToDraggedObject(ref DraggedObjectData, QueuedData);
        if (DraggedObjectContainer != null) DraggedObjectContainer.UpdateGridPosition();
    }

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var placementZ = SongBpmTime * EditorScaleController.EditorScale;

        if (PrecisionPlacementController.IsEnabled && CanPrecisionPlacement)
        {
            ResetHysteresis();
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            Vector3 roundedPoint = (Vector2)Vector2Int.FloorToInt((Vector2)localPoint * precision) / precision;
            roundedPoint.z = placementZ;
            PlacementVisualContainer.transform.localPosition = roundedPoint + (Vector3)GridOffset;
        }
        else
        {
            var snappedPosition = SnapWithHysteresis(localPoint.x, localPoint.y);

            var minX = Bounds.min.x;
            var maxX = Bounds.max.x;
            var minY = Bounds.min.y;
            var maxY = Bounds.max.y;

            PlacementVisualContainer.transform.localPosition = new Vector3(
                    Mathf.Clamp(snappedPosition.x, 0, maxX - minX - 1),
                    Mathf.Clamp(snappedPosition.y, 0, maxY - minY - 1),
                    placementZ)
                + (Vector3)GridOffset;
        }
    }

    protected Vector2 SnapWithHysteresis(float rawX, float rawY)
    {
        var raw = new Vector2(rawX, rawY);
        if (!hasPreviousSnappedState)
        {
            previousSnappedState = new Vector2(Mathf.Floor(raw.x), Mathf.Floor(raw.y));
            hasPreviousSnappedState = true;
        }
        else
            previousSnappedState = BeatmapPositionHelper.SnapWithHysteresis(raw, previousSnappedState);

        return previousSnappedState;
    }

    protected virtual void ResetHysteresis()
    {
        hasPreviousSnappedState = false;
        previousSnappedState = Vector2.zero;
    }

    // Resolve only authored node and ribbon hits so Alt alone does not suppress the queued placement preview.
    private bool ShouldHideVisualForHoveredTarget()
    {
        if (!KeybindsController.IsHoverKeyHeld)
        {
            return false;
        }

        // Basic Event colliders resolve through their dedicated controller after the shared cache can be invalidated.
        var eventInput = BeatmapEventInputController.ActiveInstance;
        if (eventInput != null && eventInput.IsHovering)
        {
            return true;
        }

        if (!BeatmapRaycastCache.HasHit)
        {
            return false;
        }

        var hit = BeatmapRaycastCache.FirstHit;
        if (hit == null)
        {
            return false;
        }

        if (hit.GetComponentInParent<ObjectContainer>() != null)
        {
            return true;
        }

        var ribbon = hit.GetComponentInParent<LightGradientController>();
        return ribbon != null && ribbon.IsInteractiveBasicEventRibbon;
    }

    public override void ShowVisual() => PlacementVisualContainer.SafeSetActive(true);

    public override void HideVisual() => PlacementVisualContainer.SafeSetActive(false);

    protected virtual float GetDraggedObjectJsonTime() => DraggedObjectData.JsonTime;

    private (Vector3 localPoint, float jsonTime) GetPositionAndTime(
        Intersections.IntersectionHit hit,
        PlacementInputState inputState)
    {
        var currentJsonTime = inputState == PlacementInputState.DragAtTime && IsDragging
            ? GetDraggedObjectJsonTime()
            : Atsc.CurrentJsonTime;
        currentJsonTime -= Atsc.VisualBeatOriginJsonTime;

        var snap = 1f / Atsc.GridMeasureSnapping;
        var offsetJsonTime = currentJsonTime
            - ((float)Math.Round(currentJsonTime / snap, MidpointRounding.AwayFromZero) * snap);

        var localPoint = PlacementTrack.InverseTransformPoint(hit.Point);

        localPoint.z = AdjustZScale
            ? (localPoint.z - BeatmapConstant.ZOffset) / BeatmapConstant.LaneSize
            : localPoint.z;
        var realTime = localPoint.z / EditorScaleController.EditorScale;
        if (hit.GameObject.transform.parent.name.Contains("Interface"))
        {
            realTime = PlacementTrack.InverseTransformPoint(hit.GameObject.transform.parent.position).z
                / EditorScaleController.EditorScale;
        }

        var hitPointJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(realTime);
        var jsonTime = (float)Math.Round((hitPointJsonTime - offsetJsonTime) / snap, MidpointRounding.AwayFromZero)
            * snap;
        if (!Atsc.IsPlaying) jsonTime += offsetJsonTime;

        return (localPoint, jsonTime);
    }

    public virtual void CreateVisual()
    {
        if (PlacementVisualContainer != null)
        {
            PlacementVisualContainer.IsPlacementVisual = true;
            return;
        }

        PlacementVisualContainer = Instantiate(
                ObjectContainerPrefab,
                PlacementTrack)
            .GetComponent(typeof(TContainer)) as TContainer;
        PlacementVisualContainer.IsPlacementVisual = true;
        PlacementVisualContainer.Setup();
        PlacementVisualContainer.Selected = false;

        foreach (var coll in PlacementVisualContainer.GetComponentsInChildren<IntersectionCollider>(true))
            Destroy(coll);
        if (PlacementVisualContainer.GetComponent<ObjectAnimator>() is ObjectAnimator animator)
            animator.enabled = false;

        PlacementVisualContainer.name = $"Hover {ObjectDataType}";
    }

    private void SetTo360Tracks()
    {
        if (!AssignTo360Tracks) return;
        var track = TracksManager.GetTrackAtTime(
            SongBpmTime,
            QueuedData is BaseGrid grid ? grid.Rotation : 0);
        if (track == null) return;

        var localPos = PlacementVisualContainer.transform.localPosition;
        PlacementTrack = track.ObjectParentTransform;
        PlacementVisualContainer.transform.SetParent(track.ObjectParentTransform, false);
        PlacementVisualContainer.transform.localPosition = localPos;
        PlacementVisualContainer.transform.localEulerAngles = new Vector3(
            PlacementVisualContainer.transform.localEulerAngles.x,
            0,
            PlacementVisualContainer.transform.localEulerAngles.z);
    }

    protected virtual void HandleRotationChanged(float rotation) { }

    public override void Apply()
    {
        if (QueuedData?.JsonTime >= 0
            && PlacementVisualContainer.gameObject.activeSelf)
        {
            HandleApply();
            OnApplied?.Invoke();
        }
    }

    public virtual void HandleApply()
    {
        ObjectContainerCollection.SpawnObject(QueuedData, out var conflicting);
        BeatmapActionContainer.AddAction(GenerateAction(QueuedData, conflicting));
        QueuedData = BeatmapFactory.Clone(QueuedData);
        QueuedData.CustomData = null;
    }

    public override void Exit()
    {
        ResetHysteresis();
        HideVisual();
        State = PlacementState.Idle;
    }

    public override void Cancel() => ResetHysteresis();

    // TODO(Bullet): Clean up implementations.
    protected virtual void TransferQueuedToDraggedObject(ref TObject dragged, TObject queued) { }

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        var con = draggedObject.GetComponentInParent<TContainer>();
        // this does not need the last check
        if (con == null || con.ObjectData.ObjectType != ObjectDataType) return null;

        ObjectContainerCollection.SilentRemoveObject(con.ObjectData);

        DraggedObjectData = con.ObjectData as TObject;
        OriginalQueued = BeatmapFactory.Clone(QueuedData);
        OriginalDraggedObjectData = BeatmapFactory.Clone(con.ObjectData as TObject);
        QueuedData = BeatmapFactory.Clone(DraggedObjectData);
        DraggedObjectContainer = con;
        DraggedObjectContainer.Dragged = true;

        IsDragging = true;
        return con;
    }

    protected override List<BeatmapAction> PerformPreFinishDragActions() => new();

    public override void FinishDrag()
    {
        var actions = PerformPreFinishDragActions();

        // Spawn our dragged object and delete anything that's overlapping.
        ObjectContainerCollection.SpawnObject(DraggedObjectData, out var conflicting);

        QueuedData = BeatmapFactory.Clone(OriginalQueued);
        // Don't queue an action if we didn't actually change anything
        if (DraggedObjectData.ToString() != OriginalDraggedObjectData.ToString())
        {
            if (conflicting.Count > 0)
            {
                actions.Add(
                    new BeatmapObjectModifiedWithConflictingAction(
                        DraggedObjectData,
                        DraggedObjectData,
                        OriginalDraggedObjectData,
                        conflicting,
                        "Modified via alt-click and drag."));
            }
            else
            {
                actions.Add(
                    new BeatmapObjectModifiedAction(
                        DraggedObjectData,
                        DraggedObjectData,
                        OriginalDraggedObjectData,
                        "Modified via alt-click and drag."));
            }

            SelectionController.OnSelectionChanged?.Invoke();
        }

        if (actions.Count == 1)
            BeatmapActionContainer.AddAction(actions[0]);
        else if (actions.Count > 1)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(actions, true, true, "Modified via alt-click and drag"));
        }

        DraggedObjectContainer.Dragged = false;
        DraggedObjectContainer = null;
        HandleDragged();
        IsDragging = false;
    }
}

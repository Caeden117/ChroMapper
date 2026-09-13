using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NotePlacement : BasePlacement<BaseNote, NoteContainer, NoteGridContainer>
{
    private static readonly int alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private BeatmapSharedNoteInputController beatmapSharedNoteInputController;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;

    [SerializeField] private CameraManager cameraManager;

    private readonly List<ObjectContainer> draggedAttachedSliderContainers = new();

    private readonly Dictionary<IndicatorType, List<BaseSlider>> draggedAttachedSliderDatas = new()
    {
        { IndicatorType.Head, new List<BaseSlider>() }, { IndicatorType.Tail, new List<BaseSlider>() }
    };

    private readonly Dictionary<IndicatorType, List<BaseSlider>> originalDraggedAttachedSliderDatas = new()
    {
        { IndicatorType.Head, new List<BaseSlider>() }, { IndicatorType.Tail, new List<BaseSlider>() }
    };

    private bool updateAttachedSliderDirection;

    public override void Start()
    {
        base.Start();
        beatmapSharedNoteInputController.OnCutDirectionChanged += HandleOnCutDirectionChanged;
    }

    public void OnDestroy() => beatmapSharedNoteInputController.OnCutDirectionChanged -= HandleOnCutDirectionChanged;

    // Preserve the scene callback while routing it through BasePlacement's shared gameplay-object Chroma state.
    public void PlaceChromaObjects(bool enabled) => BasePlacement.SetPlaceChromaObjects(enabled);

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed a note.");

    protected override BaseNote GenerateOriginalData() =>
        new() { Color = (int)NoteColor.Red, CutDirection = (int)NoteCutDirection.Down };

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        var con = base.StartDrag(draggedObject);
        if (IsDragging) StartDragSliders(DraggedObjectContainer);

        return con;
    }

    private void StartDragSliders(NoteContainer noteContainer)
    {
        var noteData = noteContainer.NoteData;
        var epsilon = BeatmapObjectContainerCollection.Epsilon;

        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        foreach (var arcContainer in arcCollection.LoadedContainers)
        {
            var arcData = arcContainer.Key as BaseArc;
            var isConnectedToHead = Mathf.Abs(arcData.JsonTime - noteData.JsonTime) < epsilon
                && arcData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(arcData.TailJsonTime - noteData.JsonTime) < epsilon
                && arcData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                originalDraggedAttachedSliderDatas[IndicatorType.Head].Add(BeatmapFactory.Clone(arcData));
                draggedAttachedSliderDatas[IndicatorType.Head].Add(arcData);
                draggedAttachedSliderContainers.Add(arcContainer.Value);
                arcCollection.SilentRemoveObject(arcData);
            }
            else if (isConnectedToTail)
            {
                originalDraggedAttachedSliderDatas[IndicatorType.Tail].Add(BeatmapFactory.Clone(arcData));
                draggedAttachedSliderDatas[IndicatorType.Tail].Add(arcData);
                draggedAttachedSliderContainers.Add(arcContainer.Value);
                arcCollection.SilentRemoveObject(arcData);
            }
        }

        var chainCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        foreach (var chainContainer in chainCollection.LoadedContainers)
        {
            var chainData = chainContainer.Key as BaseChain;
            var isConnectedToHead = Mathf.Abs(chainData.JsonTime - noteData.JsonTime) < epsilon
                && chainData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(chainData.TailJsonTime - noteData.JsonTime) < epsilon
                && chainData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                originalDraggedAttachedSliderDatas[IndicatorType.Head].Add(BeatmapFactory.Clone(chainData));
                draggedAttachedSliderDatas[IndicatorType.Head].Add(chainData);
                draggedAttachedSliderContainers.Add(chainContainer.Value);
                chainCollection.SilentRemoveObject(chainData);
            }
            else if (isConnectedToTail)
            {
                originalDraggedAttachedSliderDatas[IndicatorType.Tail].Add(BeatmapFactory.Clone(chainData));
                draggedAttachedSliderDatas[IndicatorType.Tail].Add(chainData);
                draggedAttachedSliderContainers.Add(chainContainer.Value);
                chainCollection.SilentRemoveObject(chainData);
            }
        }

        foreach (var container in draggedAttachedSliderContainers) container.Dragged = true;
    }

    protected override List<BeatmapAction> PerformPreFinishDragActions()
    {
        var noteCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
        noteCollection.RefreshSpecialAngles(DraggedObjectData, false, false);

        var actions = new List<BeatmapAction>();
        FinishSliderDrag(actions);
        ClearDraggedAttachedSliders();

        return actions;
    }

    private void FinishSliderDrag(List<BeatmapAction> actions)
    {
        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        var chainCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

        for (var i = 0; i < draggedAttachedSliderDatas[IndicatorType.Head].Count; i++)
        {
            var draggedSlider = draggedAttachedSliderDatas[IndicatorType.Head][i];
            var originalDraggedSlider = originalDraggedAttachedSliderDatas[IndicatorType.Head][i];

            if (draggedSlider is BaseArc draggedArc)
                SpawnDraggedSlider(arcCollection, draggedArc, originalDraggedSlider, actions);
            else if (draggedSlider is BaseChain draggedChain)
                SpawnDraggedSlider(chainCollection, draggedChain, originalDraggedSlider, actions);
        }

        for (var i = 0; i < draggedAttachedSliderDatas[IndicatorType.Tail].Count; i++)
        {
            var draggedSlider = draggedAttachedSliderDatas[IndicatorType.Tail][i];
            var originalDraggedSlider = originalDraggedAttachedSliderDatas[IndicatorType.Tail][i];

            if (draggedSlider is BaseArc draggedArc)
                SpawnDraggedSlider(arcCollection, draggedArc, originalDraggedSlider, actions);
            else if (draggedSlider is BaseChain draggedChain)
                SpawnDraggedSlider(chainCollection, draggedChain, originalDraggedSlider, actions);
        }
    }

    private void SpawnDraggedSlider(
        BeatmapObjectContainerCollection sliderCollection,
        BaseSlider draggedSlider,
        BaseObject originalSlider,
        List<BeatmapAction> actions)
    {
        sliderCollection.SpawnObject(draggedSlider, out var conflictingArcs);

        // Don't queue an action if we didn't actually change anything
        if (draggedSlider.ToString() != originalSlider.ToString())
        {
            if (conflictingArcs.Count > 0)
            {
                actions.Add(
                    new BeatmapObjectModifiedWithConflictingAction(
                        draggedSlider,
                        draggedSlider,
                        originalSlider,
                        conflictingArcs,
                        "Modified via alt-click and drag."));
            }
            else
            {
                actions.Add(
                    new BeatmapObjectModifiedAction(
                        draggedSlider,
                        draggedSlider,
                        originalSlider,
                        "Modified via alt-click and drag."));
            }
        }
    }

    private void ClearDraggedAttachedSliders()
    {
        foreach (var container in draggedAttachedSliderContainers) container.Dragged = false;
        draggedAttachedSliderContainers.Clear();
        draggedAttachedSliderDatas[IndicatorType.Head].Clear();
        draggedAttachedSliderDatas[IndicatorType.Tail].Clear();
        originalDraggedAttachedSliderDatas[IndicatorType.Head].Clear();
        originalDraggedAttachedSliderDatas[IndicatorType.Tail].Clear();
    }

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        UpdateAppearance();
    }

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var zPlacement = BeatmapPositionHelper.SongTimeToLanePositionZ(SongBpmTime);

        if (PrecisionPlacementController.IsEnabled)
        {
            ResetHysteresis();
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            LanePosition = BeatmapPositionHelper.LocalPositionToLanePositionRound(
                localPoint,
                precision,
                BeatmapConstant.PlayerYOffset / 2f);
            LanePosition.z = zPlacement;
            PlacementVisualContainer.transform.localPosition =
                BeatmapPositionHelper.LanePositionToLocalPosition(LanePosition, BeatmapConstant.PlayerYOffset / 2f);
        }
        else
        {
            var rawX = (localPoint.x / BeatmapConstant.LaneSize) - (gridViewController.IsOdd ? 0.5f : 0f);
            var rawY = (localPoint.y - BeatmapConstant.YOffset - (BeatmapConstant.PlayerYOffset / 2f))
                / BeatmapConstant.LaneSize;
            var snappedPosition = SnapWithHysteresis(rawX, rawY);

            LanePosition = new Vector3(
                snappedPosition.x + (gridViewController.IsOdd ? 0.5f : 0f),
                snappedPosition.y,
                zPlacement);
            PlacementVisualContainer.transform.localPosition =
                BeatmapPositionHelper.LanePositionToLocalPosition(
                    LanePosition,
                    Bounds,
                    BeatmapConstant.PlayerYOffset / 2f);
        }
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        // Apply the active gameplay-object Chroma color even when its picker flyout is not visible.
        QueuedData.CustomColor = CanPlaceChromaObjects
            ? colorPicker.CurrentColor
            : null;

        var pos = LanePosition;
        pos.x += 2f;

        var vanillaX = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0f, 3f));
        var vanillaY = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0f, 2f));

        QueuedData.PosX = vanillaX;
        QueuedData.PosY = vanillaY;

        if (PrecisionPlacementController.IsEnabled)
            QueuedData.CustomCoordinate = new Vector2(pos.x - 2f, pos.y);
        else
        {
            QueuedData.CustomCoordinate =
                !(Mathf.Approximately(vanillaX, pos.x)
                    && Mathf.Approximately(vanillaY, pos.y))
                    ? new Vector2(pos.x - 2f, pos.y)
                    : null;
        }

        // Refresh the queued ghost immediately so toggling object Chroma never leaves it on a stale primary color.
        QueuedData.WriteCustom();
        UpdateAppearance();
    }

    protected override void HandleRotationChanged(float rotation)
    {
        if (QueuedData == null) return;
        QueuedData.Rotation = (int)rotation;
        noteAppearance.SetNoteAppearance(PlacementVisualContainer);
    }

    // Do we need this anymore?
    public NoteContainer ObjectUnderCursor()
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return null;

        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        return !Intersections.Raycast(ray, 9, out var hit)
            ? null
            : hit.GameObject.GetComponentInParent<NoteContainer>();
    }

    private void HandleOnCutDirectionChanged(int cutDirection)
    {
        ToggleDiagonalAngleOffset(QueuedData, cutDirection);
        QueuedData.CutDirection = cutDirection;
        if (DraggedObjectContainer != null && DraggedObjectContainer.NoteData != null)
        {
            ToggleDiagonalAngleOffset(DraggedObjectContainer.NoteData, cutDirection);
            DraggedObjectContainer.NoteData.CutDirection = cutDirection;
            noteAppearance.SetNoteAppearance(DraggedObjectContainer);
            updateAttachedSliderDirection = true;
        }

        UpdateAppearance();
    }

    private void ToggleDiagonalAngleOffset(BaseNote note, int newCutDirection)
    {
        if (note.CutDirection == (int)NoteCutDirection.Any
            && newCutDirection == (int)NoteCutDirection.Any
            && note.AngleOffset != 45)
            note.AngleOffset = 45;
        else
            note.AngleOffset = 0;
    }

    public void UpdateType(int type)
    {
        QueuedData.Type = type;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (PlacementVisualContainer is null) return;
        PlacementVisualContainer.NoteData = QueuedData;
        noteAppearance.SetNoteAppearance(PlacementVisualContainer);
        PlacementVisualContainer.ModelController.MpbController.Mpb.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.ArrowMpbController.Mpb.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.UpdateMaterials();
        PlacementVisualContainer.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(QueuedData);
    }

    protected override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CutDirection = queued.CutDirection;
        dragged.CustomCoordinate = queued.CustomCoordinate;
        if (DraggedObjectContainer != null)
        {
            DraggedObjectContainer.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(dragged);
            DraggedObjectContainer.DirectionTargetEuler = NoteContainer.Directionalize(dragged);
        }

        if (dragged.Rotation != queued.Rotation)
        {
            dragged.Rotation = queued.Rotation;
            TracksManager.RefreshTracks();
        }

        noteAppearance.SetNoteAppearance(DraggedObjectContainer);

        TransferQueuedToAttachedDraggedSliders(queued);
    }

    private void TransferQueuedToAttachedDraggedSliders(BaseNote queued)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        foreach (var baseSlider in draggedAttachedSliderDatas[IndicatorType.Head])
        {
            baseSlider.JsonTime = queued.JsonTime;
            baseSlider.PosX = queued.PosX;
            baseSlider.PosY = queued.PosY;
            if (updateAttachedSliderDirection) baseSlider.CutDirection = queued.CutDirection;
            if (baseSlider.Rotation != queued.Rotation)
            {
                baseSlider.Rotation = queued.Rotation;
                TracksManager.RefreshTracks();
            }

            baseSlider.CustomCoordinate = queued.CustomCoordinate;
        }

        foreach (var baseSlider in draggedAttachedSliderDatas[IndicatorType.Tail])
        {
            baseSlider.TailJsonTime = queued.JsonTime;
            baseSlider.TailPosX = queued.PosX;
            baseSlider.TailPosY = queued.PosY;
            baseSlider.CustomTailCoordinate = queued.CustomCoordinate;

            if (baseSlider is BaseArc baseArc && updateAttachedSliderDirection)
                baseArc.TailCutDirection = queued.CutDirection;
            if (baseSlider.TailRotation != queued.Rotation) baseSlider.TailRotation = queued.Rotation;
        }

        foreach (var baseSliderContainer in draggedAttachedSliderContainers)
        {
            switch (baseSliderContainer)
            {
                case ArcContainer arcContainer:
                    arcContainer.NotifySplineChanged();
                    break;
                case ChainContainer chainContainer:
                    chainContainer.AdjustTimePlacement();
                    chainContainer.GenerateChain();
                    break;
            }
        }

        updateAttachedSliderDirection = false;
    }

    public override void FinishDrag()
    {
        base.FinishDrag();
        QueuedData.Rotation = (int)LaneRotationProvider.EditRotation;
    }

    public override void CreateVisual()
    {
        base.CreateVisual();
        PlacementVisualContainer.SetArcVisible(false);
    }
}

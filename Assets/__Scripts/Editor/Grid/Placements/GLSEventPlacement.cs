using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public abstract class
    GLSEventPlacement<TGroup, TEvent> : BasePlacement<TEvent, GLSEventContainer, GLSEventGridContainer>
    where TEvent : BaseGLSEvent
{
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] protected GLSEventAppearanceSO GlsEventAppearance;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] protected BeatmapEasingsSelectionInputController EasingInputController;
    // The authored parent retains the dragged child reference, so undo needs a deep group snapshot captured before movement mutates it.
    private BaseEventBoxGroup originalDraggedGroup;

    public override bool CanPlace =>
        base.CanPlace
        && glsEventGridProvider.GroupContext != null
        && glsEventGridProvider.GroupContext.GetType() == typeof(TGroup)
        // Transform groups expose display-only missing-axis lanes even when no corresponding box is serialized yet.
        && glsEventGridProvider.DisplayedLaneCount > 0
        // Non-grid-aligned outer groups can produce a sub-epsilon negative residue at their visual zero offset.
        // Fixes the weird float rounding CatKid issue
        && QueuedData.RelativeJsonTime >= -BeatmapObjectContainerCollection.Epsilon;

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        throw new ArgumentException("If you triggered this, you tried to use add object where it couldn't");

    public override void Start()
    {
        base.Start();
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
    }

    public void OnDestroy() => glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;

    private void HandleGroupChanged(BaseEventBoxGroup group) => QueuedData.EventBoxGroupData = group;

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        PlacementVisualContainer.EventData = QueuedData;
        PlacementVisualContainer.SafeSetActive(CanPlace);
        RefreshAppearance();
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        // Every inner GLS placement receives hover updates, so suppress inactive type contexts before dereferencing its group.
        var group = glsEventGridProvider.GroupContext;
        if (group == null || group.GetType() != typeof(TGroup))
        {
            // Inactive GLS modes must stay idle so paste resolves the hovered type's queued group-relative offset.
            State = PlacementState.Idle;
            PlacementVisualContainer.SafeSetActive(false);
            return;
        }

        QueuedData.EventBoxGroupData = group;
        var i = (int)(PlacementVisualContainer.transform.localPosition.x - 0.5f);
        QueuedData.RelativeJsonTime = RoundedJsonTime - group.JsonTime;
        // Preserve the zero-offset lane when absolute grid rounding lands a few float units before its outer group.
        if (QueuedData.RelativeJsonTime < 0f
            && QueuedData.RelativeJsonTime >= -BeatmapObjectContainerCollection.Epsilon)
        {
            QueuedData.RelativeJsonTime = 0f;
        }

        QueuedData.RecomputeSongBpmTime();
        // Re-evaluate after updating the offset so the hover node immediately hides before the group.
        PlacementVisualContainer.SafeSetActive(CanPlace);
        var boxIndex = Math.Clamp(i, 0, glsEventGridProvider.DisplayedLaneCount - 1);
        if (!glsEventGridProvider.TryGetDisplayedBox(boxIndex, out var displayedBox)) return;
        QueuedData.EventBoxData = displayedBox;
        // Hover X uses the merged display lane while authored placement retains the group's real box index.
        QueuedData.BoxIndex = glsEventGridProvider.GetAuthoredBoxIndex(boxIndex);
        PlacementVisualContainer.DisplayLaneIndex = boxIndex;
        if (DraggedObjectContainer != null)
        {
            DraggedObjectContainer.DisplayLaneIndex = boxIndex;
        }
        // The hover preview bypasses collection positioning, so ground it with the finalized inner GLS node position.
        PlacementVisualContainer.UpdateGridPosition();
        RefreshAppearance();
    }

    public override void HandleApply()
    {
        // Auto-XYZ lanes have no authored box index, so use the cached O(1) lane mapping instead of scanning every box.
        if (QueuedData.BoxIndex < 0)
        {
            ObjectContainerCollection.PlaceInDisplayOnlyLane(QueuedData, QueuedData.EventBoxData);
        }
        else
        {
            ObjectContainerCollection.SpawnObject(QueuedData, out _);
        }
        QueuedData = BeatmapFactory.Clone(QueuedData);
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        PlacementVisualContainer.EventData = QueuedData;
    }

    public override void Apply()
    {
        // Guard direct placement calls as well as the normal input-system CanPlace filter.
        if (CanPlace)
            base.Apply();
    }

    // Match queued inner GLS node colors to the boost state used by finalized child-node containers.
    protected void RefreshAppearance() => GlsEventAppearance.SetAppearance(
        PlacementVisualContainer,
        false,
        ObjectContainerCollection.IsBoostAt(QueuedData.JsonTime));

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        // GLS event placements share one ObjectType, so reject other GLS subtypes before the base path removes their node.
        var eventContainer = draggedObject.GetComponentInParent<GLSEventContainer>();
        if (eventContainer == null || eventContainer.EventData is not TEvent)
        {
            return null;
        }

        var con = base.StartDrag(draggedObject);
        if (con == null) return null;

        // Clone before any drag hover update changes the child still owned by the authoritative parent group.
        originalDraggedGroup = BeatmapFactory.Clone(glsEventGridProvider.GroupContext);

        // imagine having to assign this bullshit again and agian
        DraggedObjectData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        OriginalQueued.EventBoxGroupData = glsEventGridProvider.GroupContext;
        OriginalDraggedObjectData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;

        return con;
    }

    public override void FinishDrag()
    {
        if (!ReferenceEquals(DraggedObjectData.EventBoxGroupData, glsEventGridProvider.GroupContext))
        {
            // Undo or another group action retired this drag's parent, so cancel instead of publishing its stale child into the new context.
            ResetDragState();
            return;
        }

        // Restore the original node when a drag would move it before its group's beat.
        if (DraggedObjectData.RelativeJsonTime < 0f)
        {
            // Normal spawning creates a group action whose original side still contains the transient negative child.
            ObjectContainerCollection.RestoreRejectedDrag(originalDraggedGroup);
            ResetDragState();
            return;
        }

        // slightly different, just no action
        // Publish the destination group against the untouched pre-drag parent so undo restores the original source location.
        try
        {
            if (DraggedObjectData.BoxIndex < 0)
            {
                ObjectContainerCollection.MoveToDisplayOnlyLane(
                    DraggedObjectData,
                    OriginalDraggedObjectData,
                    DraggedObjectData.EventBoxData,
                    originalDraggedGroup);
            }
            else
            {
                ObjectContainerCollection.UseOriginalGroupForNextReplacement(originalDraggedGroup);
                ObjectContainerCollection.SpawnObject(DraggedObjectData, out _);
            }
        }
        finally
        {
            // Input release remains active after exceptions, so always retire drag state to prevent per-frame duplicate insertion.
            ResetDragState();
        }
    }

    private void ResetDragState(bool restoreQueuedData = true)
    {
        originalDraggedGroup = null;
        if (restoreQueuedData)
        {
            QueuedData = BeatmapFactory.Clone(OriginalQueued);
            QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;
            // Group restoration and replacement clone every lane, so never leave the queued node pointing at a retired box instance.
            if (QueuedData.EventBoxGroupData != null
                && QueuedData.BoxIndex >= 0
                && QueuedData.BoxIndex < QueuedData.EventBoxGroupData.ReadOnlyBoxes.Count)
            {
                QueuedData.EventBoxData = QueuedData.EventBoxGroupData.ReadOnlyBoxes[QueuedData.BoxIndex];
            }
        }

        if (DraggedObjectContainer != null)
        {
            DraggedObjectContainer.Dragged = false;
        }

        DraggedObjectContainer = null;
        HandleDragged();
        IsDragging = false;
        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void TransferQueuedToDraggedObject(ref TEvent dragged, TEvent queued)
    {
        dragged.RelativeJsonTime = queued.RelativeJsonTime;
        dragged.JsonTime = queued.JsonTime;
        dragged.EventBoxData = queued.EventBoxData;
        dragged.BoxIndex = queued.BoxIndex;
    }
}

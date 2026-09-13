using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public abstract class GLSGroupPlacement<TGroup, TCollection> : BasePlacement<TGroup, GLSGroupContainer, TCollection>
    where TGroup : BaseEventBoxGroup where TCollection : GLSGroupGridContainer<TGroup>
{
    [SerializeField] public GLSGroupTrack GlsGroupTrack;

    [SerializeField] protected GLSGroupAppearanceSO GlsGroupAppearance;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] protected BeatmapEasingsSelectionInputController EasingInputController;

    public override bool CanPlace => base.CanPlace && IsInPosition() && !BeatmapRaycastCache.HasHit;

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed a GLS Group.");

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        GlsGroupTrack = provider.GetComponent<GLSGroupTrack>();
        PlacementTrack = GlsGroupTrack.Track.ObjectParentTransform;
        QueuedData.ID = GlsGroupTrack.TrackDefinition.ID;
        PlacementVisualContainer.EventBoxGroupData = QueuedData;
        PlacementVisualContainer.transform.SetParent(PlacementTrack, false);
        PlacementVisualContainer.SafeSetActive(CanPlace);
        RefreshAppearance();
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        // GLS group alt-drags may cross the map origin, but serialized group beats may not become negative.
        if (IsDragging)
        {
            QueuedData.JsonTime = Mathf.Max(0f, QueuedData.JsonTime);
        }

        PlacementVisualContainer.SafeSetActive(CanPlace);
        // The outer hover preview bypasses collection positioning, so align its smaller model base with finalized GLS nodes.
        var position = PlacementVisualContainer.transform.localPosition;
        position.y = EventAppearanceSO.GetGroundedNodeCenterY(false);
        PlacementVisualContainer.transform.localPosition = position;
        foreach (var evt in QueuedData.ReadOnlyBoxes.SelectMany(box => box.ReadOnlyEvents))
            evt.JsonTime = QueuedData.JsonTime + evt.RelativeJsonTime;
    }

    protected bool IsInPosition() =>
        Mathf.Approximately(
            Mathf.Floor(PlacementVisualContainer.transform.localPosition.x),
            GLSGroupContainer.GetPositionFromTrackDefinition(beatmapRuntimeContext.TrackDefinitions, QueuedData));

    // Use the event grid's indexed boost state so outer queued GLS groups match their finalized preview node color.
    protected void RefreshAppearance() => GlsGroupAppearance.SetAppearance(
        PlacementVisualContainer,
        false,
        ObjectContainerCollection.IsBoostAt(QueuedData.JsonTime));

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        // Ghost hits must drag their collection-owned group, avoiding a second lane offset for the inner preview beat.
        var hitContainer = draggedObject.GetComponentInParent<GLSGroupContainer>();
        if (hitContainer == null)
            return base.StartDrag(draggedObject);
        var dragTarget = hitContainer.DragTarget;
        var container = base.StartDrag(dragTarget.gameObject);
        // Unity objects require their overloaded null comparison before applying the group-wide drag state.
        if (container is GLSGroupContainer glsContainer)
            glsContainer.SetGroupDragged(true);
        return container;
    }

    public override void HandleApply()
    {
        base.HandleApply();
        PlacementVisualContainer.EventBoxGroupData = QueuedData;
    }

    public override void FinishDrag()
    {
        var draggedGroup = DraggedObjectContainer;
        base.FinishDrag();
        PlacementVisualContainer.EventBoxGroupData = QueuedData;
        // Clear the group-wide drag highlight after the owner has been respawned at its final beat.
        // Unity drag targets need explicit null checks before clearing the shared drag highlight.
        if (draggedGroup != null)
        {
            draggedGroup.SetGroupDragged(false);
        }
    }

    protected override void TransferQueuedToDraggedObject(ref TGroup dragged, TGroup queued) =>
        dragged.JsonTime = queued.JsonTime;
}

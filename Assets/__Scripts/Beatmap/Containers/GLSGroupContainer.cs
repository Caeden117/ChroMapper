using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GLSGroupContainer : ObjectContainer
    {
        // Match the dithered transparency property used by passed note models.
        private static readonly int alwaysTranslucentId = Shader.PropertyToID("_AlwaysTranslucent");
        private static readonly int translucentAlphaId = Shader.PropertyToID("_TranslucentAlpha");
        // Keep the shared preview pool with the other static state.
        private static readonly Stack<GLSGroupContainer> previewGhostPool = new();

        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private GLSGroupAppearanceSO glsGroupAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro[] valueDisplays;
        [SerializeField] private GLSNodeLabel[] valueLabels;
        [SerializeField] private GLSEventIconView iconView;
        [SerializeField] public LightGradientController lightGradientController;
        [SerializeField] private LightGradientController incomingLightGradientController;
        public LightGradientController IncomingLightGradientController => incomingLightGradientController;
        [SerializeField] public TrackDefinitionsSO TrackDefinitions;

        public BaseEventBoxGroup EventBoxGroupData;

        // Retain the represented inner node so outer-track ghost interactions can target it later.
        public BaseGLSEvent PreviewEventData;

        // Track dynamically-created previews so a recycled group container can rebuild them safely.
        private readonly List<GLSGroupContainer> previewGhosts = new();

        private readonly Dictionary<BaseGLSEvent, GLSGroupContainer> previewGhostByEvent = new();
        private readonly HashSet<GLSGroupContainer> retainedPreviewGhosts = new();
        private readonly List<GLSGroupContainer> configuredPreviewGhosts = new();
        private readonly HashSet<BaseGLSEvent> desiredPreviewEventSet = new();
        private readonly List<BaseGLSEvent> desiredPreviewEvents = new();

        // Index existing ghost slots by offset once per configuration so replacing nodes does not rescan the
        // whole group. Sorted offsets also support Mathf.Approximately matches.
        private readonly Dictionary<float, Queue<int>> preservedPreviewSlotsByOffset = new();
        private readonly List<float> preservedPreviewOffsets = new();
        // Only reuse slots present at the start of this refresh; newly added ghosts are already assigned.
        private int previewOldGhostCount;
        private int previewCapacityReuseCursor;

        private PreviewConfigurationStage previewConfigurationStage;
        private int previewConfigurationEventIndex;
        private int previewConfigurationReleaseIndex;
        private float previewConfigurationPreviousOffset;
        private bool previewConfigurationForceAppearanceRefresh;
        private bool previewConfigurationUsesGroupAppearance;
        private BaseGLSEvent configuredPrimaryPreviewEvent;

        // Bind visible nodes during the refresh so they appear immediately. Color ribbon uploads can wait for
        // spare frame time.
        private bool deferPreviewRibbons;
        private bool previewRibbonDirty;
        private int pendingPreviewRibbonCount;
        private int nextPreviewRibbonIndex;

        private Transform previewGhostRoot;
        // Scale ghost geometry under its own parent so shrinking the preview does not shorten its timeline
        // ribbons.
        private Transform previewVisualRoot;
        private GridLane ribbonGridLane;

        // Retain the boost lookup so existing source nodes can refresh ribbons after a later target changes easing.
        private Func<float, bool> previewBoostResolver;
        // The shared boost index is retained for later ribbon uploads.
        private ColorBoostEventIndex previewBoostEvents;

        // Distinguish the collection-owned node from its translucent, dynamically-created previews.
        private bool isPreviewGhost;

        private bool preservePreviewSlotsOnNextConfigure;
        private bool reusePreviewCapacityOnNextConfigure;
        private bool previewSlotsConfigured;
        internal bool HasActivePooledPreviewRoot { get; private set; }

        private static float cachedInnerPreviewShrink = float.NaN;
        private static Vector3 cachedInnerPreviewVisualScale;

        // Let a hovered pooled preview update the visual outline of its owning logical group.
        private GLSGroupContainer previewOwner;

        private enum PreviewConfigurationStage
        {
            None,
            HideReboundRoot,
            ReparentReboundRoot,
            ConfigurePrimary,
            ConfigureGhosts,
            ReleaseGhosts,
            ActivateReboundRoot,
            SuspendRoot,
        }

        internal float CalculatePreviewConfigurationPrioritySongBpmTime()
        {
            // Resume at the first dirty ribbon, which may belong to an earlier node.
            if (previewConfigurationStage == PreviewConfigurationStage.None && pendingPreviewRibbonCount > 0)
            {
                for (var index = nextPreviewRibbonIndex; index <= previewGhosts.Count; index++)
                {
                    var preview = index == 0 ? this : previewGhosts[index - 1];
                    if (preview.previewRibbonDirty && preview.PreviewEventData != null)
                        return preview.PreviewEventData.SongBpmTime;
                }
            }

            switch (previewConfigurationStage)
            {
                case PreviewConfigurationStage.HideReboundRoot:
                case PreviewConfigurationStage.ReparentReboundRoot:
                case PreviewConfigurationStage.ConfigurePrimary:
                    return PreviewEventData != null
                        ? PreviewEventData.SongBpmTime
                        : EventBoxGroupData != null
                            ? EventBoxGroupData.SongBpmTime
                            : float.PositiveInfinity;
                case PreviewConfigurationStage.ConfigureGhosts:
                    for (var index = previewConfigurationEventIndex; index < desiredPreviewEvents.Count; index++)
                    {
                        var previewEvent = desiredPreviewEvents[index];
                        if (!Mathf.Approximately(
                            previewEvent.RelativeJsonTime,
                            previewConfigurationPreviousOffset))
                        {
                            return previewEvent.SongBpmTime;
                        }
                    }
                    break;
                case PreviewConfigurationStage.SuspendRoot:
                    return float.PositiveInfinity;
            }

            if (desiredPreviewEvents.Count > 0)
            {
                return desiredPreviewEvents[desiredPreviewEvents.Count - 1].SongBpmTime;
            }
            return PreviewEventData != null
                ? PreviewEventData.SongBpmTime
                : EventBoxGroupData != null
                    ? EventBoxGroupData.SongBpmTime
                    : float.PositiveInfinity;
        }

        // Resolve ghost-node drags to the collection-owned group so Alt-drag moves every node together.
        public GLSGroupContainer DragTarget => previewOwner != null ? previewOwner : this;

        // Ghosts use the collection owner's scrolling track so their ribbons share the grid coordinates of
        // the primary node.
        public void BindRibbonLane(GridLane lane)
        {
            ribbonGridLane = lane;
            BindOwnRibbonLane(DragTarget.transform.parent);
            foreach (var ghost in previewGhosts)
            {
                ghost.ribbonGridLane = lane;
                ghost.BindOwnRibbonLane(transform.parent);
            }
        }

        private void BindOwnRibbonLane(Transform scrollingTrack)
        {
            lightGradientController.BindRibbonLane(ribbonGridLane, transform, scrollingTrack);
            if (incomingLightGradientController != null)
                incomingLightGradientController.BindRibbonLane(ribbonGridLane, transform, scrollingTrack);
        }

        private bool groupDragActive;
        private bool groupWasSelectedBeforeDrag;

        // Keep outer-track GLS previews visually selected whenever their one logical group is selected.
        public override bool Selected
        {
            get => base.Selected;
            set
            {
                // Propagate even when the primary value is unchanged because previews may have just been rebuilt.
                base.Selected = value;
                if (isPreviewGhost) return;

                foreach (var previewGhost in previewGhosts) previewGhost.Selected = value;
            }
        }

        public override BaseObject ObjectData
        {
            get => EventBoxGroupData;
            set
            {
                if (!ReferenceEquals(EventBoxGroupData, value) && value == null)
                {
                    reusePreviewCapacityOnNextConfigure = true;
                    previewSlotsConfigured = false;
                }

                EventBoxGroupData = (BaseEventBoxGroup)value;
                PreviewEventData = null;
            }
        }

        internal void RebindEventBoxGroup(BaseEventBoxGroup eventBoxGroup)
        {
            EventBoxGroupData = eventBoxGroup;
            PreviewEventData = null;
            preservePreviewSlotsOnNextConfigure = true;
            foreach (var previewGhost in previewGhosts)
            {
                previewGhost.EventBoxGroupData = eventBoxGroup;
            }
        }

        protected override void RegisterCallback()
        {
            if (isPreviewGhost) return;

            VisualSettings.OnBlockModelChanged += HandleModelChanged;
            VisualSettings.OnEventModelChanged += HandleModelChanged;
            SelectionController.SelectionChangedEvent += SyncPreviewSelection;
        }

        protected override void UnregisterCallback()
        {
            if (isPreviewGhost)
                return;

            if (previewGhostRoot != null)
            {
                previewConfigurationStage = PreviewConfigurationStage.None;
                previewGhosts.Clear();
                previewGhostByEvent.Clear();
                retainedPreviewGhosts.Clear();
                configuredPreviewGhosts.Clear();
                desiredPreviewEventSet.Clear();
                desiredPreviewEvents.Clear();
                preservedPreviewSlotsByOffset.Clear();
                preservedPreviewOffsets.Clear();
                previewOldGhostCount = 0;
                previewCapacityReuseCursor = 0;
                Destroy(previewGhostRoot.gameObject);
                previewGhostRoot = null;
            }

            VisualSettings.OnBlockModelChanged -= HandleModelChanged;
            VisualSettings.OnEventModelChanged -= HandleModelChanged;
            SelectionController.SelectionChangedEvent -= SyncPreviewSelection;
        }

        // Update preview outlines only when selection changes, keeping ghost nodes free of per-frame work.
        private void SyncPreviewSelection()
        {
            var selected = EventBoxGroupData != null && SelectionController.IsObjectSelected(EventBoxGroupData);
            Selected = selected;
        }

        // Highlight every preview outline while keeping the group as the sole logical selection.
        public void SetGroupHighlighted(bool highlighted)
        {
            var owner = previewOwner != null ? previewOwner : this;
            owner.Highlighted = highlighted;
            foreach (var previewGhost in owner.previewGhosts) previewGhost.Highlighted = highlighted;
        }

        // Keep the whole logical GLS group blue while any one of its rendered nodes is being dragged.
        public void SetGroupDragged(bool dragged)
        {
            var owner = previewOwner != null ? previewOwner : this;
            if (dragged)
            {
                if (!owner.groupDragActive)
                {
                    owner.groupWasSelectedBeforeDrag = owner.Selected;
                    owner.groupDragActive = true;
                }

                // Use the normal selected outline color (blue in the editor) instead of the generic white drag outline.
                owner.Selected = true;
            }
            else if (owner.groupDragActive)
            {
                owner.Selected = owner.groupWasSelectedBeforeDrag;
                owner.groupDragActive = false;
            }

            owner.Dragged = dragged;
            foreach (var previewGhost in owner.previewGhosts)
                previewGhost.Dragged = dragged;
        }

        private void HandleModelChanged() => VModelController.Set(VisualSettings.GetBlockModel());

        public override void Setup() => DisablePassedObjectDither();

        public static GLSGroupContainer SpawnGLSGroup(
            BaseEventBoxGroup data,
            TrackDefinitionsSO trackDefinitions,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GLSGroupContainer>();
            container.EventBoxGroupData = data;
            container.TrackDefinitions = trackDefinitions;
            return container;
        }

        public override void UpdateGridPosition()
        {
            var pos = transform.localPosition;
            // Keep every inner GLS node grounded after its shared 75%-scale appearance is applied. Fixes GLS nodes hovering too high above grid and being hard to tell where they are visually.
            pos.y = BeatmapConstant.EventNodeGroundedCenterY
                - ((EventAppearanceSO.FinalNodeScale - transform.localScale.y) / 2f);
            // Unity preview events need explicit null checks before choosing the rendered beat position.
            var previewSongBpmTime = PreviewEventData != null
                ? PreviewEventData.SongBpmTime
                : EventBoxGroupData.SongBpmTime;
            pos.z = previewSongBpmTime * EditorScaleController.EditorScale;
            transform.localPosition = pos;
            // Outer ghosts reach their final position after appearance binding. Update the ribbon plane from
            // that final transform before rendering.
            lightGradientController.RefreshRibbonPlane();
            if (incomingLightGradientController != null)
                incomingLightGradientController.RefreshRibbonPlane();
            UpdateCollisionGroups();

            if (!isPreviewGhost && previewSlotsConfigured)
                foreach (var previewGhost in previewGhosts)
                    previewGhost.UpdateGridPosition();
        }

        // Render one selectable outer-track node per distinct inner-event offset.
        public void ConfigurePreviewNodes(Func<float, bool> isBoostAt)
            => ConfigurePreviewNodes(
                isBoostAt,
                float.NegativeInfinity,
                float.PositiveInfinity,
                null,
                true);

        public bool HasSamePreviewNodeWindow(float lowerBound, float upperBound, ISet<BaseGLSEvent> retainedEvents)
        {
            if (EventBoxGroupData == null
                || !EventBoxGroupData.OrderedEventsInitialized
                || !previewSlotsConfigured
                || previewConfigurationStage != PreviewConfigurationStage.None)
            {
                return false;
            }

            var orderedEvents = EventBoxGroupData.OrderedEvents;
            var span = orderedEvents.AsSpan();
            var startIndex = span.LowerBoundBy(lowerBound, previewEvent => previewEvent.SongBpmTime);
            var endIndex = span.UpperBoundBy(upperBound, previewEvent => previewEvent.SongBpmTime);
            var desiredCount = endIndex - startIndex;
            for (var index = startIndex; index < endIndex; index++)
            {
                if (!desiredPreviewEventSet.Contains(orderedEvents[index]))
                    return false;
            }

            if (retainedEvents != null)
            {
                foreach (var retainedEvent in retainedEvents)
                {
                    if (!ReferenceEquals(retainedEvent.EventBoxGroupData, EventBoxGroupData))
                        continue;
                    if (!desiredPreviewEventSet.Contains(retainedEvent))
                        return false;
                    if (retainedEvent.SongBpmTime < lowerBound || retainedEvent.SongBpmTime > upperBound)
                        desiredCount++;
                }
            }

            return desiredCount == desiredPreviewEventSet.Count;
        }

        public void ConfigurePreviewNodes(
            Func<float, bool> isBoostAt,
            float lowerBound,
            float upperBound,
            ISet<BaseGLSEvent> retainedEvents,
            bool forceAppearanceRefresh = false,
            ColorBoostEventIndex boostEvents = null)
        {
            BeginPreviewNodeConfiguration(
                isBoostAt,
                lowerBound,
                upperBound,
                retainedEvents,
                forceAppearanceRefresh,
                false,
                boostEvents: boostEvents);
            while (!ProcessPreviewNodeConfigurationStep())
            {
            }
            // A direct refresh must complete ribbons left pending by a previous scrolling frame.
            while (!ProcessNextPreviewRibbon())
            {
            }
        }

        public void BeginPreviewNodeConfiguration(
            Func<float, bool> isBoostAt,
            float lowerBound,
            float upperBound,
            ISet<BaseGLSEvent> retainedEvents,
            bool forceAppearanceRefresh,
            bool hideUntilConfigured,
            bool deferRibbonUpdate = false,
            ColorBoostEventIndex boostEvents = null)
        {
            HasActivePooledPreviewRoot = false;
            // Preserve the collection's boost resolver for targeted ribbon-only refreshes that do not rebuild hover objects.
            previewBoostResolver = isBoostAt;
            previewBoostEvents = boostEvents;
            deferPreviewRibbons = deferRibbonUpdate;
            nextPreviewRibbonIndex = 0;
            retainedPreviewGhosts.Clear();
            configuredPreviewGhosts.Clear();
            previewGhostByEvent.Clear();
            desiredPreviewEventSet.Clear();
            desiredPreviewEvents.Clear();
            preservedPreviewSlotsByOffset.Clear();
            preservedPreviewOffsets.Clear();
            previewOldGhostCount = previewGhosts.Count;
            previewCapacityReuseCursor = 0;
            for (var index = 0; index < previewGhosts.Count; index++)
            {
                var previewEvent = previewGhosts[index].PreviewEventData;
                if (previewEvent == null)
                    continue;
                previewGhostByEvent[previewEvent] = previewGhosts[index];
                if (!preservePreviewSlotsOnNextConfigure)
                    continue;
                var offset = previewEvent.RelativeJsonTime;
                if (!preservedPreviewSlotsByOffset.TryGetValue(offset, out var slots))
                {
                    preservedPreviewSlotsByOffset[offset] = slots = new Queue<int>();
                    preservedPreviewOffsets.Add(offset);
                }
                slots.Enqueue(index);
            }
            if (preservePreviewSlotsOnNextConfigure)
                preservedPreviewOffsets.Sort();

            previewConfigurationEventIndex = 0;
            previewConfigurationReleaseIndex = -1;
            previewConfigurationForceAppearanceRefresh = forceAppearanceRefresh;
            previewConfigurationUsesGroupAppearance = false;

            if (hideUntilConfigured)
            {
                gameObject.SetActive(false);
            }

            if (EventBoxGroupData == null)
            {
                previewConfigurationStage = PreviewConfigurationStage.ReleaseGhosts;
                previewConfigurationReleaseIndex = previewGhosts.Count - 1;
                return;
            }

            // Zero opacity restores the original single-node rendering path without creating ghost objects.
            if (Mathf.Approximately(Settings.Instance.GLSOuterTrackGhostNodeOpacity, 0f))
            {
                PreviewEventData = null;
                previewConfigurationUsesGroupAppearance = true;
                previewConfigurationStage = hideUntilConfigured && previewGhostRoot != null
                    ? PreviewConfigurationStage.HideReboundRoot
                    : PreviewConfigurationStage.ConfigurePrimary;
                return;
            }

            // Preview rebuilds can happen repeatedly while scrolling, so consume the maintained ordering unless it is uninitialized.
            if (!EventBoxGroupData.OrderedEventsInitialized)
                EventBoxGroupData.ResortOrderedEvents();

            if (EventBoxGroupData is not (BaseLightColorEventBoxGroup or ILightTransformEventBoxGroup or BaseVfxEventEventBoxGroup))
            {
                previewConfigurationStage = PreviewConfigurationStage.None;
                if (hideUntilConfigured)
                    gameObject.SetActive(true);
                return;
            }

            var orderedEvents = EventBoxGroupData.OrderedEvents;
            if (orderedEvents.Count == 0)
            {
                PreviewEventData = null;
                previewConfigurationReleaseIndex = previewGhosts.Count - 1;
                previewConfigurationStage = hideUntilConfigured && previewGhostRoot != null
                    ? PreviewConfigurationStage.HideReboundRoot
                    : PreviewConfigurationStage.ReleaseGhosts;
                return;
            }

            // Only visible nodes and offscreen nodes whose ribbons cross the viewport need preview
            // containers.
            var span = orderedEvents.AsSpan();
            var startIndex = span.LowerBoundBy(lowerBound, previewEvent => previewEvent.SongBpmTime);
            var endIndex = span.UpperBoundBy(upperBound, previewEvent => previewEvent.SongBpmTime);
            for (var eventIndex = startIndex; eventIndex < endIndex; eventIndex++)
            {
                var previewEvent = orderedEvents[eventIndex];
                if (desiredPreviewEventSet.Add(previewEvent))
                    desiredPreviewEvents.Add(previewEvent);
            }
            if (retainedEvents != null)
            {
                foreach (var retainedEvent in retainedEvents)
                {
                    if (ReferenceEquals(retainedEvent.EventBoxGroupData, EventBoxGroupData)
                        && desiredPreviewEventSet.Add(retainedEvent))
                    {
                        desiredPreviewEvents.Add(retainedEvent);
                    }
                }
            }
            desiredPreviewEvents.Sort();

            var primaryPreviewEvent = orderedEvents[0];
            previewConfigurationForceAppearanceRefresh = forceAppearanceRefresh
                || hideUntilConfigured
                || !ReferenceEquals(configuredPrimaryPreviewEvent, primaryPreviewEvent);
            PreviewEventData = primaryPreviewEvent;
            previewConfigurationPreviousOffset = primaryPreviewEvent.RelativeJsonTime;
            previewConfigurationStage = hideUntilConfigured && previewGhostRoot != null
                ? PreviewConfigurationStage.HideReboundRoot
                : PreviewConfigurationStage.ConfigurePrimary;
        }

        public bool ProcessPreviewNodeConfigurationStep()
        {
            while (previewConfigurationStage != PreviewConfigurationStage.None)
            {
                switch (previewConfigurationStage)
                {
                    case PreviewConfigurationStage.HideReboundRoot:
                        previewGhostRoot.gameObject.SetActive(false);
                        previewConfigurationStage = PreviewConfigurationStage.ReparentReboundRoot;
                        return false;
                    case PreviewConfigurationStage.ReparentReboundRoot:
                        SetPreviewParent(transform.parent);
                        previewConfigurationStage = PreviewConfigurationStage.ConfigurePrimary;
                        return false;
                    case PreviewConfigurationStage.ConfigurePrimary:
                        previewConfigurationStage = PreviewConfigurationStage.ConfigureGhosts;
                        // Restore the main node's solid appearance before applying its new event.
                        isPreviewGhost = false;
                        if (previewConfigurationUsesGroupAppearance)
                        {
                            ConfigureAsPreviewGhost(
                                previewBoostResolver(EventBoxGroupData.JsonTime),
                                previewBoostResolver,
                                deferPreviewRibbons,
                                previewBoostEvents);
                            configuredPrimaryPreviewEvent = null;
                            return false;
                        }
                        if (PreviewEventData != null && previewConfigurationForceAppearanceRefresh)
                        {
                            ConfigureAsPreviewGhost(
                                previewBoostResolver(PreviewEventData.JsonTime),
                                previewBoostResolver,
                                deferPreviewRibbons,
                                previewBoostEvents);
                            configuredPrimaryPreviewEvent = PreviewEventData;
                            return false;
                        }
                        break;
                    case PreviewConfigurationStage.ConfigureGhosts:
                        if (ConfigureNextPreviewGhost())
                            return false;
                        previewConfigurationReleaseIndex = previewGhosts.Count - 1;
                        previewConfigurationStage = PreviewConfigurationStage.ReleaseGhosts;
                        break;
                    case PreviewConfigurationStage.ReleaseGhosts:
                        while (previewConfigurationReleaseIndex >= 0)
                        {
                            var previewGhost = previewGhosts[previewConfigurationReleaseIndex--];
                            if (retainedPreviewGhosts.Contains(previewGhost))
                                continue;
                            // Release one ghost per step without shifting the list tail. Compact the retained
                            // ghosts once after all releases.
                            ReleasePreviewGhost(previewGhost);
                            return false;
                        }
                        previewGhosts.Clear();
                        previewGhosts.AddRange(configuredPreviewGhosts);
                        previewSlotsConfigured = true;
                        preservePreviewSlotsOnNextConfigure = false;
                        reusePreviewCapacityOnNextConfigure = false;
                        previewConfigurationStage = PreviewConfigurationStage.ActivateReboundRoot;
                        break;
                    case PreviewConfigurationStage.ActivateReboundRoot:
                        if (previewGhostRoot != null && !previewGhostRoot.gameObject.activeSelf)
                        {
                            previewGhostRoot.gameObject.SetActive(true);
                            previewConfigurationStage = PreviewConfigurationStage.None;
                            gameObject.SetActive(true);
                            SyncPreviewSelection();
                            return true;
                        }
                        previewConfigurationStage = PreviewConfigurationStage.None;
                        gameObject.SetActive(true);
                        SyncPreviewSelection();
                        break;
                    case PreviewConfigurationStage.SuspendRoot:
                        SuspendPreviewGhosts();
                        return true;
                }
            }

            return true;
        }

        private bool ConfigureNextPreviewGhost()
        {
            while (previewConfigurationEventIndex < desiredPreviewEvents.Count)
            {
                var previewEvent = desiredPreviewEvents[previewConfigurationEventIndex++];
                if (previewEvent.RelativeJsonTime == previewConfigurationPreviousOffset)
                    continue;
                previewConfigurationPreviousOffset = previewEvent.RelativeJsonTime;

                var eventChanged = !previewGhostByEvent.TryGetValue(previewEvent, out var ghost);
                var recycledSlot = false;
                if (eventChanged && preservePreviewSlotsOnNextConfigure)
                {
                    // Reuse the ghost at this beat so its collider stays under the cursor during hover edits.
                    ghost = ClaimPreservedPreviewSlot(previewEvent.RelativeJsonTime);
                }
                if (ghost == null && reusePreviewCapacityOnNextConfigure)
                {
                    // A pooled group can reuse unused ghost slots. Skip assigned slots so each is considered
                    // once.
                    ghost = ClaimReusablePreviewSlot();
                    recycledSlot = ghost != null;
                }
                if (ghost == null)
                {
                    ghost = GetPreviewGhost();
                    previewGhosts.Add(ghost);
                }

                retainedPreviewGhosts.Add(ghost);
                configuredPreviewGhosts.Add(ghost);
                previewGhostByEvent[previewEvent] = ghost;
                ghost.BindPreviewState(this, EventBoxGroupData, previewEvent, GlsLightCount, recycledSlot);
                if (eventChanged || previewConfigurationForceAppearanceRefresh)
                {
                    // Evaluate boost at this inner event's absolute time, not at the group's start time.
                    ghost.ConfigureAsPreviewGhost(
                        previewBoostResolver(previewEvent.JsonTime),
                        previewBoostResolver,
                        deferPreviewRibbons,
                        previewBoostEvents);
                    return true;
                }
            }

            return false;
        }

        // Match offsets approximately and prefer the lowest unretained slot index to preserve first-match
        // order for stacked previews.
        private GLSGroupContainer ClaimPreservedPreviewSlot(float relativeJsonTime)
        {
            var matchIndex = preservedPreviewOffsets.BinarySearch(relativeJsonTime);
            if (matchIndex < 0)
                matchIndex = ~matchIndex;

            var bestIndex = int.MaxValue;
            Queue<int> bestQueue = null;
            for (var index = matchIndex - 1;
                 index >= 0 && Mathf.Approximately(preservedPreviewOffsets[index], relativeJsonTime);
                 index--)
            {
                SelectEarliestAvailablePreservedPreviewSlot(preservedPreviewOffsets[index], ref bestIndex, ref bestQueue);
            }
            for (var index = matchIndex;
                 index < preservedPreviewOffsets.Count && Mathf.Approximately(preservedPreviewOffsets[index], relativeJsonTime);
                 index++)
            {
                SelectEarliestAvailablePreservedPreviewSlot(preservedPreviewOffsets[index], ref bestIndex, ref bestQueue);
            }

            if (bestQueue == null)
                return null;
            bestQueue.Dequeue();
            return previewGhosts[bestIndex];
        }

        // Skip slots already reused elsewhere, then take the earliest remaining slot to keep stacked ghosts
        // in order.
        private void SelectEarliestAvailablePreservedPreviewSlot(float offset, ref int bestIndex, ref Queue<int> bestQueue)
        {
            var slots = preservedPreviewSlotsByOffset[offset];
            while (slots.Count > 0 && retainedPreviewGhosts.Contains(previewGhosts[slots.Peek()]))
            {
                slots.Dequeue();
            }
            if (slots.Count > 0 && slots.Peek() < bestIndex)
            {
                bestIndex = slots.Peek();
                bestQueue = slots;
            }
        }

        private GLSGroupContainer ClaimReusablePreviewSlot()
        {
            while (previewCapacityReuseCursor < previewOldGhostCount
                && retainedPreviewGhosts.Contains(previewGhosts[previewCapacityReuseCursor]))
            {
                previewCapacityReuseCursor++;
            }

            return previewCapacityReuseCursor < previewOldGhostCount
                ? previewGhosts[previewCapacityReuseCursor]
                : null;
        }

        // Refresh forward-owned ribbons on this group and its ghosts without recycling the nodes under the cursor.
        public void RefreshTransitionRibbons()
        {
            if (previewBoostResolver == null)
                return;
            glsGroupAppearance.UpdateTransitionRibbon(this, previewBoostResolver, previewBoostEvents);
            ClearPreviewRibbonDirty();
            foreach (var previewGhost in previewGhosts)
            {
                glsGroupAppearance.UpdateTransitionRibbon(
                    previewGhost, previewBoostResolver, previewBoostEvents);
                previewGhost.ClearPreviewRibbonDirty();
            }
        }

        // Refresh affected ribbons immediately and clear their pending scrub jobs so the scheduler does not
        // upload them twice.
        public void RefreshTransitionRibbons(
            HashSet<BaseLightColorBase> changedNodes,
            Dictionary<BaseEventBoxGroup, HashSet<float>> changedAggregates)
        {
            if (previewBoostResolver == null)
                return;
            if (TransitionRibbonPreviewChanged(PreviewEventData, changedNodes, changedAggregates))
            {
                glsGroupAppearance.UpdateTransitionRibbon(this, previewBoostResolver, previewBoostEvents);
                ClearPreviewRibbonDirty();
            }
            foreach (var previewGhost in previewGhosts)
            {
                if (TransitionRibbonPreviewChanged(previewGhost.PreviewEventData, changedNodes, changedAggregates))
                {
                    glsGroupAppearance.UpdateTransitionRibbon(
                        previewGhost, previewBoostResolver, previewBoostEvents);
                    previewGhost.ClearPreviewRibbonDirty();
                }
            }
        }

        private static bool TransitionRibbonPreviewChanged(
            BaseGLSEvent previewEvent,
            HashSet<BaseLightColorBase> changedNodes,
            Dictionary<BaseEventBoxGroup, HashSet<float>> changedAggregates) =>
            previewEvent is BaseLightColorBase colorEvent
                && (changedNodes.Contains(colorEvent)
                    || (colorEvent.EventBoxGroupData != null
                        && changedAggregates.TryGetValue(colorEvent.EventBoxGroupData, out var changedTimes)
                        && changedTimes.Contains(colorEvent.RelativeJsonTime)));

        private void ConfigureAsPreviewGhost(
            bool boost,
            Func<float, bool> isBoostAt,
            bool deferRibbonUpdate,
            ColorBoostEventIndex boostEvents)
        {
            // Set the ghost shader flags before SetAppearance uploads the property block.
            PreparePreviewOpacity();
            glsGroupAppearance.SetAppearance(this, true, boost);
            ApplyInnerPreviewShrink();
            if (deferRibbonUpdate && PreviewEventData is BaseLightColorBase)
            {
                lightGradientController.SetVisible(false);
                if (incomingLightGradientController != null)
                {
                    incomingLightGradientController.SetVisible(false);
                }
                MarkPreviewRibbonDirty();
            }
            else
            {
                // Ghosts hold no boost index of their own; the owning container passes its shared one in.
                glsGroupAppearance.UpdateTransitionRibbon(this, isBoostAt, boostEvents);
                ClearPreviewRibbonDirty();
            }
            // Give unmanaged previews the same selection outline color as their collection-owned group.
            SetOutlineColor(SelectionController.SelectedColor);
            UpdateGridPosition();
        }

        // Store pending ribbon work on the group container so a later scrub can replace the queued events.
        private void MarkPreviewRibbonDirty()
        {
            if (previewRibbonDirty)
                return;
            previewRibbonDirty = true;
            var owner = previewOwner != null ? previewOwner : this;
            owner.pendingPreviewRibbonCount++;
        }

        private void ClearPreviewRibbonDirty()
        {
            if (!previewRibbonDirty)
                return;
            previewRibbonDirty = false;
            var owner = previewOwner != null ? previewOwner : this;
            owner.pendingPreviewRibbonCount--;
        }

        // Upload at most one ribbon per call so the scheduler can stop between uploads when its frame budget
        // runs out.
        public bool ProcessNextPreviewRibbon()
        {
            if (pendingPreviewRibbonCount == 0)
                return true;

            for (var index = nextPreviewRibbonIndex; index <= previewGhosts.Count; index++)
            {
                var preview = index == 0 ? this : previewGhosts[index - 1];
                if (!preview.previewRibbonDirty)
                    continue;

                glsGroupAppearance.UpdateTransitionRibbon(
                    preview, previewBoostResolver, previewBoostEvents);
                preview.ClearPreviewRibbonDirty();
                nextPreviewRibbonIndex = index + 1;
                return pendingPreviewRibbonCount == 0;
            }

            nextPreviewRibbonIndex = 0;
            return pendingPreviewRibbonCount == 0;
        }

        public bool HasPendingPreviewRibbons => pendingPreviewRibbonCount > 0;

        private void ApplyInnerPreviewShrink()
        {
            if (!isPreviewGhost)
                return;

            var shrinkSetting = Settings.Instance.GLSInnerEventPreviewShrink;
            if (shrinkSetting != cachedInnerPreviewShrink)
            {
                cachedInnerPreviewShrink = shrinkSetting;
                cachedInnerPreviewVisualScale = Vector3.one * (1f - Mathf.Clamp01(shrinkSetting));
            }

            previewVisualRoot.localScale = cachedInnerPreviewVisualScale;
            previewVisualRoot.localPosition = new Vector3(
                0f,
                (cachedInnerPreviewVisualScale.y - 1f) / 2f,
                0f);
        }

        private void PreparePreviewOpacity()
        {
            // A pooled renderer can switch between the main node and a ghost, so reset both shader values.
            MpbController.Mpb.SetFloat(alwaysTranslucentId, isPreviewGhost ? 1f : 0f);
            MpbController.Mpb.SetFloat(
                translucentAlphaId,
                isPreviewGhost
                    ? Mathf.Clamp01(Settings.Instance.GLSOuterTrackGhostNodeOpacity)
                    : 1f);
        }

        private void ClearPreviewGhosts()
        {
            SetColorHover(false);
            // A recycled hovered ghost loses its owner reference, so clear the owner now to prevent stale primary highlights.
            Highlighted = false;

            foreach (var previewGhost in previewGhosts)
                ReleasePreviewGhost(previewGhost);

            previewGhosts.Clear();
            previewGhostByEvent.Clear();
            retainedPreviewGhosts.Clear();
            configuredPreviewGhosts.Clear();
            desiredPreviewEventSet.Clear();
            desiredPreviewEvents.Clear();
            preservedPreviewSlotsByOffset.Clear();
            preservedPreviewOffsets.Clear();
            previewOldGhostCount = 0;
            previewCapacityReuseCursor = 0;
            reusePreviewCapacityOnNextConfigure = false;
        }

        public void SuspendPreviewGhosts()
        {
            previewConfigurationStage = PreviewConfigurationStage.None;
            SetColorHover(false);
            Highlighted = false;
            if (previewGhostRoot != null && previewGhostRoot.gameObject.activeSelf)
            {
                previewGhostRoot.gameObject.SetActive(false);
            }
            reusePreviewCapacityOnNextConfigure = true;
        }

        public void BeginPreviewGhostSuspension() => previewConfigurationStage = PreviewConfigurationStage.SuspendRoot;

        public void CancelPreviewNodeConfiguration()
        {
            previewConfigurationStage = PreviewConfigurationStage.None;
            if (EventBoxGroupData == null)
                SuspendPreviewGhosts();
        }

        // Keep the ghost root active until this refresh finishes in case another group reuses the container.
        public void ResetForPool(bool keepActiveForRefresh = false)
        {
            previewConfigurationStage = PreviewConfigurationStage.None;
            // A recycled owner keeps its ghost slots but must discard every ribbon job tied to the old group.
            previewRibbonDirty = false;
            pendingPreviewRibbonCount = 0;
            nextPreviewRibbonIndex = 0;
            deferPreviewRibbons = false;
            foreach (var previewGhost in previewGhosts)
            {
                previewGhost.previewRibbonDirty = false;
            }
            previewConfigurationEventIndex = 0;
            previewConfigurationReleaseIndex = -1;
            previewConfigurationPreviousOffset = 0f;
            previewOldGhostCount = 0;
            previewCapacityReuseCursor = 0;
            previewConfigurationForceAppearanceRefresh = false;
            previewConfigurationUsesGroupAppearance = false;
            previewBoostResolver = null;
            previewBoostEvents = null;
            configuredPrimaryPreviewEvent = null;
            preservePreviewSlotsOnNextConfigure = false;
            previewSlotsConfigured = false;
            HasActivePooledPreviewRoot = keepActiveForRefresh;
            groupDragActive = false;
            groupWasSelectedBeforeDrag = false;
            isPreviewGhost = false;
            ResetInteractionState();
            GlsLightCount = 0;
            MpbController.Mpb.SetFloat(alwaysTranslucentId, 0f);
            MpbController.Mpb.SetFloat(translucentAlphaId, 1f);
            if (lightGradientController.gameObject.activeSelf)
            {
                lightGradientController.SetVisible(false);
            }
            if (incomingLightGradientController != null
                && incomingLightGradientController.gameObject.activeSelf)
            {
                incomingLightGradientController.SetVisible(false);
            }
            if (!keepActiveForRefresh && previewGhostRoot != null && previewGhostRoot.gameObject.activeSelf)
            {
                previewGhostRoot.gameObject.SetActive(false);
            }
            EventBoxGroupData = null;
            PreviewEventData = null;
            previewOwner = null;
        }

        public void DeactivateUnusedPooledPreviewRoot()
        {
            HasActivePooledPreviewRoot = false;
            SuspendPreviewGhosts();
        }

        private void BindPreviewState(
            GLSGroupContainer owner,
            BaseEventBoxGroup group,
            BaseGLSEvent previewEvent,
            int lightCount,
            bool resetInteractionState)
        {
            if (resetInteractionState)
            {
                ResetInteractionState();
            }
            // Reapply ghost shading after a pool reset, which restores the main-node appearance.
            isPreviewGhost = true;
            EventBoxGroupData = group;
            PreviewEventData = previewEvent;
            previewOwner = owner;
            GlsLightCount = lightCount;
            var ghostPosition = transform.localPosition;
            ghostPosition.x = owner.transform.localPosition.x;
            transform.localPosition = ghostPosition;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            ApplyGhostPreviewVisibility();
        }

        // The ghost-preview toggle hides visuals and hit-test colliders while keeping both ribbon directions
        // bound. Re-enabling it can then restore the previews without rebuilding the group.
        private void ApplyGhostPreviewVisibility()
        {
            var previewVisible = Settings.Instance.EnableGLSGhostPreview;
            if (previewVisualRoot != null)
            {
                previewVisualRoot.gameObject.SetActive(previewVisible);
            }
            foreach (var collider in Colliders)
            {
                collider.enabled = previewVisible;
            }
        }

        private void ResetInteractionState()
        {
            SetColorHover(false);
            Highlighted = false;
            Selected = false;
            Dragged = false;
        }

        private void ReleasePreviewGhost(GLSGroupContainer previewGhost)
        {
            previewGhost.ClearPreviewRibbonDirty();
            // Disable before pooling so ghost renderers and hit-test colliders stop participating this frame.
            previewGhost.gameObject.SetActive(false);
            previewGhost.ResetForPool();
            if (previewGhostRoot != null)
                previewGhost.transform.SetParent(previewGhostRoot.parent, false);
            previewGhostPool.Push(previewGhost);
        }

        private GLSGroupContainer GetPreviewGhost()
        {
            GLSGroupContainer ghost;
            // Discard Unity-destroyed entries retained by the static pool across map reloads.
            while (previewGhostPool.TryPop(out ghost) && ghost == null) { }

            if (ghost == null)
            {
                ghost = Instantiate(this, transform.parent);
                ghost.isPreviewGhost = true;
                ghost.CreatePreviewVisualRoot();
            }

            ghost.transform.SetParent(GetPreviewGhostRoot(), false);
            // Reused ghosts may come from another track. Bind before enabling or rendering.
            ghost.ribbonGridLane = ribbonGridLane;
            ghost.BindOwnRibbonLane(transform.parent);
            // A clone or reused ghost may retain hover highlighting. Clear it before activation.
            ghost.ResetInteractionState();
            // Restore the owner lane before UpdateGridPosition updates the event-specific Z coordinate.
            var position = transform.localPosition;
            ghost.transform.localPosition = new Vector3(position.x, position.y, ghost.transform.localPosition.z);
            ghost.gameObject.SetActive(true);
            return ghost;
        }

        private void CreatePreviewVisualRoot()
        {
            previewVisualRoot = new GameObject("GLS Preview Node Visuals").transform;
            previewVisualRoot.SetParent(transform, false);
            var outgoingRibbon = lightGradientController.transform;
            var incomingRibbon = incomingLightGradientController != null
                ? incomingLightGradientController.transform
                : null;
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (child == previewVisualRoot || child == outgoingRibbon || child == incomingRibbon)
                    continue;
                child.SetParent(previewVisualRoot, false);
            }

            iconView.SetPreviewVisualParent(previewVisualRoot);
        }

        private Transform GetPreviewGhostRoot()
        {
            if (previewGhostRoot == null)
            {
                var root = new GameObject("GLS Preview Ghost Root");
                previewGhostRoot = root.transform;
                SetPreviewParent(transform.parent);
            }

            return previewGhostRoot;
        }

        public void SetPreviewParent(Transform parent)
        {
            if (previewGhostRoot == null)
                return;
            if (previewGhostRoot.parent == parent)
                return;
            previewGhostRoot.SetParent(parent, false);
            previewGhostRoot.localPosition = Vector3.zero;
            previewGhostRoot.localRotation = Quaternion.identity;
            previewGhostRoot.localScale = Vector3.one;
        }

        public void SetText(bool enable)
        {
            foreach (var textMeshPro in valueDisplays)
                textMeshPro.enabled = enable;
        }

        public void SetText(BaseGLSEvent evt)
        {
            foreach (var label in valueLabels)
                label.SetEvent(evt);
        }

        public void SetIcons(BaseGLSEvent previewEvent)
        {
            iconView.SetIcons(GLSEventIconResolver.Resolve(previewEvent), previewEvent, valueDisplays);
        }

        public void SetColorHover(bool visible) => iconView.SetColorHover(visible, valueDisplays);

        public static float GetPositionFromTrackDefinition(TrackDefinitionsSO trackDefinitions, BaseEventBoxGroup data)
        {
            var track = trackDefinitions.GetGlsOrDefault(data.ID);

            var offset = 0f;
            if (track.ColorTrack)
            {
                if (data is BaseLightColorEventBoxGroup) return offset;
                offset++;
            }

            if (track.RotationTracks.Any(x => x))
            {
                if (data is BaseLightRotationEventBoxGroup) return offset;
                offset++;
            }

            if (track.TranslationTracks.Any(x => x))
            {
                if (data is BaseLightTranslationEventBoxGroup) return offset;
                offset++;
            }

            if (track.FloatFXTrack && data is BaseVfxEventEventBoxGroup) return offset;

            return -1f;
        }
    }
}

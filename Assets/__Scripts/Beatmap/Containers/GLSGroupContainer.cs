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
        [SerializeField] public LightGradientController lightGradientController;
        // Keep the serialized field compatible with dev's TracksDefinitionSO asset type.
        [SerializeField] public TracksDefinitionSO TracksDefinition;

        public BaseEventBoxGroup EventBoxGroupData;

        // Retain the represented inner node so outer-track ghost interactions can target it later.
        public BaseGLSEvent PreviewEventData;

        // Track dynamically-created previews so a recycled group container can rebuild them safely.
        private readonly List<GLSGroupContainer> previewGhosts = new();

        // Retain the boost lookup so existing source nodes can refresh ribbons after a later target changes easing.
        private Func<float, bool> previewBoostResolver;

        // Distinguish the collection-owned node from its translucent, dynamically-created previews.
        private bool isPreviewGhost;

        // Let a hovered pooled preview update the visual outline of its owning logical group.
        private GLSGroupContainer previewOwner;

        // Resolve ghost-node drags to the collection-owned group so Alt-drag moves every node together.
        public GLSGroupContainer DragTarget => previewOwner != null ? previewOwner : this;

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
                // Return previews whenever this container changes logical owner, including direct rebinds that skip null.
                if (!ReferenceEquals(EventBoxGroupData, value)) ClearPreviewGhosts();

                EventBoxGroupData = (BaseEventBoxGroup)value;
                PreviewEventData = null;
            }
        }

        protected override void RegisterCallback()
        {
            if (isPreviewGhost) return;

            VisualSettings.OnBlockModelChanged += HandleModelChanged;
            VisualSettings.OnEventModelChanged += HandleModelChanged;
            SelectionController.OnSelectionChanged += SyncPreviewSelection;
        }

        protected override void UnregisterCallback()
        {
            if (isPreviewGhost)
                return;

            // Preview objects are siblings, so release them before a destroyed owner can orphan visible nodes.
            if (previewGhosts.Count > 0)
            {
                ClearPreviewGhosts();
            }

            VisualSettings.OnBlockModelChanged -= HandleModelChanged;
            VisualSettings.OnEventModelChanged -= HandleModelChanged;
            SelectionController.OnSelectionChanged -= SyncPreviewSelection;
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

        public static GLSGroupContainer SpawnGLSGroup(
            BaseEventBoxGroup data,
            TracksDefinitionSO tracksDefinition,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GLSGroupContainer>();
            container.EventBoxGroupData = data;
            container.TracksDefinition = tracksDefinition;
            return container;
        }

        public override void UpdateGridPosition()
        {
            var pos = transform.localPosition;
            // Keep every inner GLS node grounded after its shared 75%-scale appearance is applied. Fixes GLS nodes hovering too high above grid and being hard to tell where they are visually.
            pos.y = BeatmapConstant.EventNodeGroundedCenterY;
            // Unity preview events need explicit null checks before choosing the rendered beat position.
            var previewSongBpmTime = PreviewEventData != null
                ? PreviewEventData.SongBpmTime
                : EventBoxGroupData.SongBpmTime;
            pos.z = previewSongBpmTime * EditorScaleController.EditorScale;
            transform.localPosition = pos;
            UpdateCollisionGroups();

            // Pooled outer-track previews are not collection-owned, so forward global editor-scale position refreshes to them.
            if (!isPreviewGhost)
                foreach (var previewGhost in previewGhosts)
                    previewGhost.UpdateGridPosition();
        }

        // Render one selectable outer-track node per distinct inner-event offset.
        public void ConfigurePreviewNodes(Func<float, bool> isBoostAt)
        {
            // Preserve the collection's boost resolver for targeted ribbon-only refreshes that do not rebuild hover objects.
            previewBoostResolver = isBoostAt;
            ClearPreviewGhosts();
            if (EventBoxGroupData == null) return;

            // Zero opacity restores the original single-node rendering path without creating ghost objects.
            if (Mathf.Approximately(Settings.Instance.GLSOuterTrackGhostNodeOpacity, 0f))
            {
                PreviewEventData = null;
                ConfigureAsPreviewGhost(isBoostAt(EventBoxGroupData.JsonTime), isBoostAt);
                return;
            }

            // Preview rebuilds can happen repeatedly while scrolling, so consume the maintained ordering unless it is uninitialized.
            if (!EventBoxGroupData.OrderedEventsInitialized)
                EventBoxGroupData.ResortOrderedEvents();

            // Preserve the supported preview families while sharing rotation and translation through the transform contract.
            if (EventBoxGroupData is BaseLightColorEventBoxGroup or ILightTransformEventBoxGroup or BaseVfxEventEventBoxGroup)
            {
                ConfigurePreviewNodes(EventBoxGroupData.OrderedEvents, isBoostAt);
            }
        }

        private void ConfigurePreviewNodes(IReadOnlyList<BaseGLSEvent> orderedEvents, Func<float, bool> isBoostAt)
        {
            var previousOffset = float.NaN;
            var isFirstPreview = true;
            var ghostSlot = 0;

            foreach (var previewEvent in orderedEvents)
            {
                // Only the first sorted event represents a shared beat offset in the outer track.
                if (previewEvent.RelativeJsonTime == previousOffset) continue;

                previousOffset = previewEvent.RelativeJsonTime;
                if (isFirstPreview)
                {
                    PreviewEventData = previewEvent;
                    isFirstPreview = false;
                    continue;
                }

                // Reuse the ghost already occupying this slot (by index) instead of round-tripping through the
                // shared static pool every rebuild. A same-shape mutation (easing/axis change) does not add or
                // remove slots, so this keeps ghost identity/collider stable across rapid hover-driven scrolls;
                // otherwise a cached HoveredObject reference could end up representing a different slot mid-frame.
                var ghost = ghostSlot < previewGhosts.Count
                    ? previewGhosts[ghostSlot]
                    : AddPreviewGhost();
                ghostSlot++;
                ghost.EventBoxGroupData = EventBoxGroupData;
                ghost.PreviewEventData = previewEvent;
                ghost.previewOwner = this;
                // Evaluate boost at this inner event's absolute time, not at the group's start time.
                ghost.ConfigureAsPreviewGhost(isBoostAt(previewEvent.JsonTime), isBoostAt);
            }

            // Release any ghosts left over from a previously larger group shape.
            ReleaseExcessPreviewGhosts(ghostSlot);

            if (isFirstPreview) PreviewEventData = null;
            // Keep the primary preview's non-Chroma color consistent with the inner event editor.
            // Unity preview events need explicit null checks before choosing the preview's boost time.
            var previewJsonTime = PreviewEventData != null
                ? PreviewEventData.JsonTime
                : EventBoxGroupData.JsonTime;
            ConfigureAsPreviewGhost(isBoostAt(previewJsonTime), isBoostAt);
            SyncPreviewSelection();
        }

        // Refresh forward-owned ribbons on this group and its ghosts without recycling the nodes under the cursor.
        public void RefreshTransitionRibbons()
        {
            if (previewBoostResolver == null)
                return;
            glsGroupAppearance.UpdateTransitionRibbon(this, previewBoostResolver);
            foreach (var previewGhost in previewGhosts)
                glsGroupAppearance.UpdateTransitionRibbon(previewGhost, previewBoostResolver);
        }

        private void ConfigureAsPreviewGhost(bool boost, Func<float, bool> isBoostAt)
        {
            glsGroupAppearance.SetAppearance(this, true, boost);
            // Rebuild this preview's cross-group color ribbon whenever its represented inner node changes.
            glsGroupAppearance.UpdateTransitionRibbon(this, isBoostAt);
            ApplyPreviewOpacity();
            // Give unmanaged previews the same selection outline color as their collection-owned group.
            SetOutlineColor(SelectionController.SelectedColor);
            UpdateGridPosition();
        }

        private void ApplyPreviewOpacity()
        {
            if (!isPreviewGhost) return;

            // Match passed notes by enabling the shader branch that consumes _TranslucentAlpha.
            var opacity = Mathf.Clamp01(Settings.Instance.GLSOuterTrackGhostNodeOpacity);
            MpbController.Mpb.SetFloat(alwaysTranslucentId, 1f);
            MpbController.Mpb.SetFloat(translucentAlphaId, opacity);
            MpbController.ApplyChanges();
        }

        private void ClearPreviewGhosts()
        {
            // A recycled hovered ghost loses its owner reference, so clear the owner now to prevent stale primary highlights.
            Highlighted = false;

            foreach (var previewGhost in previewGhosts)
                ReleasePreviewGhost(previewGhost);

            previewGhosts.Clear();
        }

        // Release ghosts beyond keepCount back to the shared pool, preserving the identity of retained slots.
        private void ReleaseExcessPreviewGhosts(int keepCount)
        {
            for (var i = previewGhosts.Count - 1; i >= keepCount; i--)
            {
                ReleasePreviewGhost(previewGhosts[i]);
                previewGhosts.RemoveAt(i);
            }
        }

        private GLSGroupContainer AddPreviewGhost()
        {
            var ghost = GetPreviewGhost();
            previewGhosts.Add(ghost);
            return ghost;
        }

        private void ReleasePreviewGhost(GLSGroupContainer previewGhost)
        {
            // Warn if ownership changed before release; this is the signature of a ghost escaping its source group.
            if (previewGhost.previewOwner != this)
            {
                Debug.LogWarning(
                    $"[GLS Preview Nodes] Ownership mismatch while releasing preview instance={previewGhost.GetInstanceID()}: " +
                    $"owner={GetInstanceID()}, recordedOwner={(previewGhost.previewOwner != null ? previewGhost.previewOwner.GetInstanceID() : 0)}.");
            }

            // Disable before pooling so ghost renderers and hit-test colliders stop participating this frame.
            previewGhost.gameObject.SetActive(false);
            // Clear transient visual state so a hovered/selected owner cannot leak it into another pooled preview.
            previewGhost.Highlighted = false;
            previewGhost.Selected = false;
            previewGhost.Dragged = false;
            previewGhost.EventBoxGroupData = null;
            previewGhost.PreviewEventData = null;
            previewGhost.previewOwner = null;
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
            }
            else
            {
                // A pooled preview must be inactive and ownerless; retain evidence if it was not fully released.
                if (ghost.gameObject.activeSelf || ghost.previewOwner != null)
                {
                    Debug.LogWarning(
                        $"[GLS Preview Nodes] Reusing unreleased preview instance={ghost.GetInstanceID()}: " +
                        $"active={ghost.gameObject.activeSelf}, recordedOwner={(ghost.previewOwner != null ? ghost.previewOwner.GetInstanceID() : 0)}.");
                }
            }

            ghost.transform.SetParent(transform.parent, false);
            // Instantiating a hovered owner and reusing a hovered ghost both copy visual state unless it is reset here.
            ghost.Highlighted = false;
            ghost.Selected = false;
            ghost.Dragged = false;
            // Restore the owner lane before UpdateGridPosition updates the event-specific Z coordinate.
            var position = transform.localPosition;
            ghost.transform.localPosition = new Vector3(position.x, position.y, ghost.transform.localPosition.z);
            ghost.gameObject.SetActive(true);
            return ghost;
        }

        public void SetText(bool enable)
        {
            foreach (var textMeshPro in valueDisplays) textMeshPro.enabled = enable;
        }

        public void SetText(string text)
        {
            foreach (var textMeshPro in valueDisplays) textMeshPro.SetText(text);
        }

        public static float GetPositionFromTrackDefinition(TracksDefinitionSO tracksDefinition, BaseEventBoxGroup data)
        {
            var track = tracksDefinition.GetGlsOrDefault(data.ID);

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

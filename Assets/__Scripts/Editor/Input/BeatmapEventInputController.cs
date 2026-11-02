using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapEventInputController : BeatmapInputController<EventContainer>, CMInput.IEventObjectsActions
{
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private SelectionController selectionController;
    // Read the authoritative mutable definition directly so startup cannot miss an earlier environment notification.
    private TrackDefinitionsSO TrackDefinition => beatmapRuntimeContext.TrackDefinitions;

    // Convert a temporary in-place edit into the required contract: edited clone plus exact unedited live original.
    private static BeatmapObjectUpdatedAction UpdatedEventAction(
        BaseObject edited,
        BaseObject original,
        ActionMergeType mergeType = ActionMergeType.None)
    {
        var liveEvent = (BaseEvent)edited;
        var editedSnapshot = BeatmapFactory.Clone(liveEvent);
        liveEvent.Apply(original);
        // BaseEvent.Apply copies custom JSON but does not rebuild parsed ring/light custom fields.
        liveEvent.RefreshCustom();
        return new BeatmapObjectUpdatedAction(editedSnapshot, liveEvent, mergeType: mergeType, preserveSelection: true);
    }

    public static bool IsHoveringRingOrZoom { get; private set; }

    // used by IsRingRotationHoveredByPointer
    // could maybe be replaced by taking a serialized dependency on this class in the users of that function, instead of this singleton approach? I dont see much downside to singleton approach though so i'm hesitant to try changing it
    public static BeatmapEventInputController ActiveInstance { get; private set; }

    private void OnEnable() => ActiveInstance = this;

    private void OnDisable()
    {
        // Only clear the singleton if this instance owns it; another enabled controller will set itself next.
        if (ActiveInstance == this)
            ActiveInstance = null;
    }

    private bool isScrolling;
    private Vector3 lastMousePosition;
    private EventContainer lastMetadataFailureContainer;
    private string lastMetadataFailureReason;

    private void Start()
    {
        lastMousePosition = Input.mousePosition;
    }

    private void HidePreviewVisual()
    {
        // Unity selection controllers need explicit null checks before toggling preview visuals.
        if (selectionController != null)
        {
            selectionController.HideEventPlacementVisual();
        }
    }

    private void ShowPreviewVisual()
    {
        // Unity selection controllers need explicit null checks before toggling preview visuals.
        if (selectionController != null)
        {
            selectionController.ShowEventPlacementVisual();
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        // Laser speed uses the same precision-aware hover input ownership as Basic Event ring controls.
        IsHoveringRingOrZoom = IsHovering
                               && HoveredObject
                               && (IsRingRotationEvent(HoveredObject)
                                    || IsRingZoomEvent(HoveredObject)
                                    || IsLaserSpeedEvent(HoveredObject));
        UpdatePreviewVisualOnMouseMove();
    }

    private void UpdatePreviewVisualOnMouseMove()
    {
        if (!isScrolling) return;

        var currentMousePosition = Input.mousePosition;
        var mouseMoved = Vector3.Distance(currentMousePosition, lastMousePosition) > 0.1f;

        if (mouseMoved)
        {
            isScrolling = false;
            ShowPreviewVisual();
        }
        else
        {
            HidePreviewVisual();
        }

        lastMousePosition = currentMousePosition;
    }

    public void OnInvertEventValue(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsMouseInWindow
            || !context.performed)
            return;

        RaycastFirstObject(out var e);
        if (e != null && !e.Dragged) InvertEvent(e);
    }

    public void OnTweakEventMain(InputAction.CallbackContext context)
    {
        // The authored action owns its chord; this callback owns only event-editor context.
        if (!context.performed || !InputChordOwnership.Owns(context))
            return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);

        // Alt+Scroll on a transition ribbon toggles the source node's RGB/HSV interpolation mode.
        if (IsBasicLightTransitionRibbonHit(e))
        {
            TweakLerpType(e, modifier);
            return;
        }

        TweakMain(e, modifier);
    }

    public void OnTweakEventAlternative(InputAction.CallbackContext context)
    {
        if (!context.performed || !InputChordOwnership.Owns(context))
            return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        // Alt+Shift remains available on nodes but does not add a third ribbon binding.
        if (IsBasicLightTransitionRibbonHit(e))
            return;

        TweakAlternative(e, modifier);
    }

    public void InvertEvent(EventContainer e)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);
        if (e.EventData.IsColorBoostEvent())
        {
            e.EventData.Value = e.EventData.Value > 0 ? 0 : 1;
            // Use the shared finalize path so the replacement container/effects refresh after the action.
            FinalizeBasicEventTweak(e, original, ActionMergeType.None);
        }
        else if (IsRingRotationEvent(e) || IsLaserSpeedEvent(e))
        {
            // Ring and laser rotations share Chroma's unspecified -> CW -> CCW direction cycle.
            var direction = e.EventData.CustomDirection;
            e.EventData.CustomDirection = direction switch
            {
                null => 1,
                0 => null,
                1 => 0,
                _ => null
            };
            e.EventData.WriteCustom();
            eventAppearance.SetAppearance(e, TrackDefinition);
            BeatmapActionContainer.AddAction(UpdatedEventAction(e.ObjectData, original), true);
        }
        else if (IsRingZoomEvent(e))
        {
            // Invert step value between positive and negative
            if (e.EventData.CustomStep.HasValue)
            {
                e.EventData.CustomStep = -e.EventData.CustomStep.Value;
                e.EventData.WriteCustom();
                eventAppearance.SetAppearance(e, TrackDefinition);
                BeatmapActionContainer.AddAction(UpdatedEventAction(e.ObjectData, original), true);
            }
        }
        else if (TrackDefinition.GetBasicOrDefault(e.EventData.Type).Kind != BasicEventKind.Lights)
        {
            return;
        }
        else
        {
            switch (e.EventData.Value)
            {
                case > 0 and <= 8:
                    e.EventData.Value += 4;
                    break;
                case > 8 and <= 12:
                    e.EventData.Value -= 8; // white to blue
                    break;
            }

            RefreshPrevEventContainer(e);
            eventAppearance.SetAppearance(e, TrackDefinition);
            BeatmapActionContainer.AddAction(UpdatedEventAction(e.ObjectData, original), true);
        }
    }

    public void OnTweakEventCtrlAlt(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        // The live chord decides ownership so remapped modifier bindings keep working.
        if (!InputChordOwnership.Owns(context))
            return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        // Alt+Scroll now owns transition-ribbon RGB/HSV toggling; Ctrl+Alt on a ribbon is a no-op.
        if (IsBasicLightTransitionRibbonHit(e))
            return;
        if (IsRingZoomEvent(e) && IsSmoothStepRingZoomEvent(e))
        {
            PersistentUI.Instance.ShowDialogBox(
                "This environment does not support ring speed. \nInstead place a duplicate step node at the point you want the step to complete",
                null,
                PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        var definitions = GetTrackDefinitions(e);
        ApplyTweak(e, evt => BasicEventHoverMutation.TweakCtrlAlt(evt, modifier, scrollPrecisionController, definitions));
    }

    public void OnTweakEventCtrlShift(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        // The live chord decides ownership so remapped modifier bindings keep working.
        if (!InputChordOwnership.Owns(context))
            return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var definitions = GetTrackDefinitions(e);
        var easingValues = Easing.DisplayNameToInternalName.Values.ToList();
        ApplyTweak(
            e,
            evt => BasicEventHoverMutation.TweakCtrlShift(evt, modifier, scrollPrecisionController, definitions, easingValues));
    }

    public void OnTweakEventCtrlShiftAlt(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        // The live chord decides ownership so remapped modifier bindings keep working.
        if (!InputChordOwnership.Owns(context))
            return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged || !IsRingRotationEvent(e)) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        ApplyTweak(e, evt => BasicEventHoverMutation.TweakRingStep(evt, modifier, scrollPrecisionController));
        BeatmapRaycastCache.Invalidate();
    }

    protected override bool GetComponentFromTransform(GameObject t, out EventContainer obj) =>
        t.transform.parent.TryGetComponent(out obj);

    // Ribbon input is valid only for the visible Basic Event transition owned by the raycast source node.
    private bool IsBasicLightTransitionRibbonHit(EventContainer e)
    {
        // Both LightIdTransitionRibbon interruption regressions require hover editing to validate the rendered endpoint.
        var transitionTarget = e.GetEffectiveNextLightEvent();
        if (TrackDefinition.GetBasicOrDefault(e.EventData.Type).Kind != BasicEventKind.Lights
            || e.EventData.CustomLightGradient != null  // Keep RGB-only legacy Chroma gradients immutable by this shortcut.
            || e.EventData.IsFade
            || e.EventData.IsFlash
            || transitionTarget == null
            || !transitionTarget.IsTransition)
        {
            return false;
        }

        var firstHit = BeatmapRaycastCache.FirstHit;
        if (firstHit == null)
            return false;

        var ribbon = firstHit.GetComponentInParent<LightGradientController>();
        return ribbon != null
            && ribbon.IsInteractiveTransitionRibbon
            && ribbon.transform.IsChildOf(e.transform);
    }

    // for event that frequently gets changed
    public void TweakMain(EventContainer e, int modifier)
    {
        var definitions = GetTrackDefinitions(e);
        var controlHeld = KeybindsController.IsControlKeyHeld;
        var selectHeld = KeybindsController.IsSelectKeyHeld;
        ApplyTweak(
            e,
            evt => BasicEventHoverMutation.TweakMain(
                evt,
                modifier,
                scrollPrecisionController,
                definitions,
                controlHeld,
                selectHeld));
    }

    // for event that occasionally gets changed
    public void TweakAlternative(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);

        var isRingRot = IsRingRotationEvent(e);
        var isRingZoom = IsRingZoomEvent(e);

        // All modifier combinations are now handled by custom InputActions
        // This method only handles basic scroll behavior if needed
        if (isRingRot || isRingZoom)
        {
            return;
        }

        if (TrackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights
            && !e.EventData.IsColorBoostEvent())
        {
            return;
        }

        FinalizeBasicEventTweak(e, original, ActionMergeType.EventAltTweak);
    }

    private void RefreshPrevEventContainer(EventContainer e)
    {
        var prevEvent = e.EventData.Prev;
        if (prevEvent != null)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (collection.LoadedContainers.TryGetValue(prevEvent, out var container))
                (container as EventContainer).RefreshAppearance();
        }
    }

    // Guard pooled and cloned hover containers while retaining evidence about whichever initialization field is missing.
    private bool TryGetEventComponents(EventContainer e, out BasicEventComponent components)
    {
        components = BasicEventComponent.None;
        if (e == null) return false;

        if (e.EventData == null)
        {
            LogMetadataFailure(e, "EventData is null");
            return false;
        }

        // ScriptableObjects require Unity's overloaded null comparison when selecting the runtime fallback.
        var trackDefinitions = e.TrackDefinitions != null
            ? e.TrackDefinitions
            : TrackDefinition;
        if (trackDefinitions == null)
        {
            LogMetadataFailure(e, "both container and runtime TrackDefinitions are null");
            return false;
        }

        lastMetadataFailureContainer = null;
        lastMetadataFailureReason = null;
        components = trackDefinitions.GetBasicOrDefault(e.EventData.Type).Components;
        return true;
    }

    private void LogMetadataFailure(EventContainer e, string reason)
    {
        // Log only when the failing container or reason changes so LateUpdate cannot flood the console.
        if (lastMetadataFailureContainer == e && lastMetadataFailureReason == reason) return;

        lastMetadataFailureContainer = e;
        lastMetadataFailureReason = reason;
    }

    // Use the hovered container's active definition so input and node appearance classify the same environment track.
    private bool IsRingRotationEvent(EventContainer e) =>
        TryGetEventComponents(e, out var components)
        && components.HasFlag(BasicEventComponent.RingRotation);

    // Shared wheel actions need an immediate hit test instead of relying on last frame's hover state.
    public bool IsPointerOverRingRotation()
    {
        return RaycastFirstObject(out var eventContainer)
               && eventContainer != null
               && !eventContainer.Dragged
               && IsRingRotationEvent(eventContainer);
    }

    // Global wheel handlers use this shared query so ring rotation owns overlapping modifier chords.
    public static bool IsRingRotationHoveredByPointer()
    {
        // Use the most-recently enabled controller so we don't raycast through an inactive instance.
        return ActiveInstance != null && ActiveInstance.IsPointerOverRingRotation();
    }

    // Cursor interval shares Ctrl+Shift+Scroll with every Basic Event node that has a hover-specific edit.
    public static bool IsCursorIntervalOwnedByPointer()
    {
        var controller = FindFirstObjectByType<BeatmapEventInputController>();
        if (controller == null || !controller.RaycastFirstObject(out var eventContainer) || eventContainer == null
            || eventContainer.Dragged)
            return false;

        if (controller.IsRingRotationEvent(eventContainer) || controller.IsRingZoomEvent(eventContainer))
            return true;

        // ScriptableObjects require Unity's overloaded null comparison when selecting the runtime fallback.
        var definitions = eventContainer.TrackDefinitions != null
            ? eventContainer.TrackDefinitions
            : controller.TrackDefinition;
        return definitions != null
               && definitions.GetBasicOrDefault(eventContainer.EventData.Type).Kind == BasicEventKind.Lights;
    }

    // Zoom capability is independent so mixed component tracks remain supported.
    private bool IsRingZoomEvent(EventContainer e) =>
        TryGetEventComponents(e, out var components)
        && (components.HasFlag(BasicEventComponent.RingZoom)
            || IsSmoothStepRingZoomEvent(e));

    // SmoothStepRingZoom only applies to The Second's legacy ring right now.
    private bool IsSmoothStepRingZoomEvent(EventContainer e) =>
        TryGetEventComponents(e, out var components)
        && components.HasFlag(BasicEventComponent.SmoothStepRingZoom);

    // Laser-speed semantics belong to tracks consumed by Basic Event light-rotation components.
    private bool IsLaserSpeedEvent(EventContainer e) =>
        TryGetEventComponents(e, out var components)
        && components.HasFlag(BasicEventComponent.LightRotation);

    private TrackDefinitionsSO GetTrackDefinitions(EventContainer container) =>
        container.TrackDefinitions != null
            ? container.TrackDefinitions
            : TrackDefinition;

    private void ApplyTweak(EventContainer container, Func<BaseEvent, ActionMergeType?> mutation)
    {
        var definitions = GetTrackDefinitions(container);
        if (HoverSelectionMutation.TryApply(
                container.EventData,
                evt => mutation(evt),
                out var edited,
                evt => BasicEventHoverMutation.MatchesType(container.EventData, evt, definitions)))
        {
            if (edited.Count > 0)
            {
                isScrolling = true;
                HidePreviewVisual();
                foreach (var evt in edited)
                    RefreshEditedEventAppearance(evt);

                var collection = (EventGridContainer)BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
                if (collection.PropagationEditing == EventGridContainer.PropMode.Light)
                    StartCoroutine(RefreshEditedEventsNextFrame(edited));

                var descriptor = beatmapRuntimeContext.Descriptor;
                if (descriptor != null)
                    descriptor.BasicEventEffectManager.Refresh();
            }

            return;
        }

        var original = BeatmapFactory.Clone(container.EventData);
        var mergeType = mutation(container.EventData);
        container.EventData.WriteCustom();
        if (mergeType.HasValue)
            FinalizeBasicEventTweak(container, original, mergeType.Value);
    }

    private IEnumerator RefreshEditedEventsNextFrame(List<BaseObject> edited)
    {
        yield return null;
        foreach (var evt in edited)
            RefreshEditedEventAppearance(evt);
    }

    private void FinalizeBasicEventTweak(EventContainer e, BaseObject original, ActionMergeType mergeType)
    {
        if (e.EventData.HasSameContent(original))
            return;

        eventAppearance.SetAppearance(e, TrackDefinition);
        // Record successful scrolling before the replacement action invalidates the hovered container.
        isScrolling = true;
        HidePreviewVisual();
        var updateAction = UpdatedEventAction(e.ObjectData, (BaseEvent)original, mergeType);
        BeatmapActionContainer.AddAction(updateAction, true);
        // Refresh the replacement container after relinking so its text and gradient use the final edited object.
        RefreshEditedEventAppearance(updateAction.EditedObject);
        // Alt+P can rebind its visible lane container after the synchronous action refresh, so refresh that identity next frame.
        var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
        if (collection is EventGridContainer eventCollection
            && eventCollection.PropagationEditing == EventGridContainer.PropMode.Light)
        {
            StartCoroutine(RefreshEditedEventAppearanceNextFrame(updateAction.EditedObject));
        }
        // Unity runtime descriptors need explicit null checks before refreshing effect playback.
        var descriptor = beatmapRuntimeContext.Descriptor;
        if (descriptor != null)
        {
            descriptor.BasicEventEffectManager.Refresh();
        }
    }

    // Refresh only the edited event and its linked predecessor without rebuilding the event pool.
    private bool RefreshEditedEventAppearance(BaseObject editedObject)
    {
        var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
        if (!collection.LoadedContainers.TryGetValue(editedObject, out var replacement)) return false;

        var replacementEvent = replacement as EventContainer;
        if (replacementEvent == null)
            return false;

        replacementEvent.RefreshAppearance();
        RefreshPrevEventContainer(replacementEvent);
        return true;
    }

    // Wait for the Alt+P lane layout to finish rebinding its pooled container before updating its text.
    private IEnumerator RefreshEditedEventAppearanceNextFrame(BaseObject editedObject)
    {
        yield return null;
        var refreshed = RefreshEditedEventAppearance(editedObject);
    }

    private void TweakLerpType(EventContainer e, int modifier)
    {
        ApplyTweak(e, evt => BasicEventHoverMutation.TweakLerpType(evt, modifier));
    }

}

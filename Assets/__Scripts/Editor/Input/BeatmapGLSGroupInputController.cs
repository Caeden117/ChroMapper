using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
// MouseButton is in the LowLevel namespace of the new Input System.
using UnityEngine.InputSystem.LowLevel;

public abstract class BeatmapGLSGroupInputController<TData> : BeatmapInputController<GLSGroupContainer>,
                                                              CMInput.IGLSGroupSelectActions
    where TData : BaseEventBoxGroup
{
    private GLSGroupContainer lastHoveredContainer;
    private bool wasHovering;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private GLSEventGridProvider eventGridProvider;
    [SerializeField] private BoxSelectionPlacement boxSelectionPlacement;

    // Box selection owns its active and finishing-click frames, preventing GLS hover highlight and entry takeover.
    protected override bool ValidObject(GLSGroupContainer container) =>
        !boxSelectionPlacement.IsPlacing
        && boxSelectionPlacement.LastCompletionFrame != Time.frameCount
        && container.EventBoxGroupData is TData;

    // Resolve the current preview event through the shared beatmap raycast cache.
    protected bool TryGetHoveredPreviewEvent<TEvent>(InputAction.CallbackContext context, out TEvent evt)
        where TEvent : BaseGLSEvent
    {
        evt = null;
        if (!context.performed || !IsHovering || !RaycastFirstObject(out var container))
        {
            return false;
        }

        evt = container.PreviewEventData as TEvent;
        if (evt == null || !ReferenceEquals(evt.EventBoxGroupData, container.EventBoxGroupData))
        {
            return false;
        }

        return true;
    }

    // Resolve cached Unity references through Unity's overloaded null check rather than C# null coalescing.
    protected static ScrollPrecisionController ResolvePrecision(ref ScrollPrecisionController precision)
    {
        if (precision == null)
            precision = FindFirstObjectByType<ScrollPrecisionController>();
        return precision;
    }

    protected override void HandleHoverChanged(GLSGroupContainer container)
    {
        // Keep shared precision from claiming wheel chords while a ghost node owns them.
        // Unity hover containers need explicit null checks before toggling group highlights.
        if (lastHoveredContainer != container && lastHoveredContainer != null)
        {
            lastHoveredContainer.SetGroupHighlighted(false);
        }

        if (container != null)
        {
            container.SetGroupHighlighted(true);
        }
        lastHoveredContainer = container;
    }

    protected override void LateUpdate()
    {
        // Claim shared scroll precision only while this type-owned outer controller has a valid hover target.
        if (wasHovering != IsHovering)
        {
            wasHovering = IsHovering;
            GLSEventInputHoverTracker.SetHovering(wasHovering);
        }

        base.LateUpdate();
    }

    protected virtual void OnDisable()
    {
        // Release shared precision when this outer controller is disabled during a mode or scene transition.
        if (!wasHovering) return;
        wasHovering = false;
        GLSEventInputHoverTracker.SetHovering(false);
    }

    private bool CanInteract =>
        !Input.GetMouseButton((int)MouseButton.Right)
        && KeybindsController.IsMouseInWindow
        && !SongTimelineController.IsHovering
        && !SceneTransitionManager.IsLoading
        && !DeleteToolController.IsActive
        && !NodeEditorController.IsActive
        && !UIMode.PreviewMode
        && !MassSelect;

    // TODO: prevent interaction after box selection is complete, race condition or somethin
    public void OnEnterGroup(InputAction.CallbackContext context)
    {
        // Ignore the finishing box-selection click even though box placement has already reset to Idle this frame.
        if (context.performed
            && CanInteract
            && boxSelectionPlacement.LastCompletionFrame != Time.frameCount
            && EditContext.EditingMode.HasFlag(EditingMode.GLS)
            && IsHovering)
        {
            var clickedEvent = HoveredObject.PreviewEventData;
            if (atsc.CurrentSongBpmTime < HoveredObject.EventBoxGroupData.SongBpmTime)
                atsc.MoveToSongBpmTime(HoveredObject.EventBoxGroupData.SongBpmTime);

            // order of operations matter bc Visual Beat Origin is reset on edit mode change, so set group context after changing edit mode
            EditContext.EditingMode = EditingMode.EventBox;
            atsc.VisualBeatOrigin = HoveredObject.EventBoxGroupData.SongBpmTime;
            eventGridProvider.GroupContext = HoveredObject.EventBoxGroupData;
            // Select the represented inner event after the mode transition clears the prior outer-track selection.
            if (clickedEvent != null) SelectionController.Select(clickedEvent);
        }
    }

    public void OnExitGroup(InputAction.CallbackContext context)
    {
        if (context.performed && EditContext.EditingMode.HasFlag(EditingMode.EventBox))
            EditContext.EditingMode = EditingMode.GLS;
    }

    public void OnMirrorHover(InputAction.CallbackContext context)
    {
        // Middle-click is type-owned by the specific outer GLS controller; do not mirror an entire color group too.
    }
}

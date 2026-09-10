using Beatmap.Base;

public class
    BeatmapGLSGroupTranslationInputController : BeatmapGLSGroupInputController<BaseLightTranslationEventBoxGroup>, CMInput.IGLSTranslationObjectsActions
{
    private ScrollPrecisionController precision;

    // Resolve the current hovered preview event for this controller's GLS node type.
    private bool TryGetHoveredEvent(UnityEngine.InputSystem.InputAction.CallbackContext context, out BaseLightTranslationBase evt) =>
        TryGetHoveredPreviewEvent(context, out evt);

    private ScrollPrecisionController Precision => ResolvePrecision(ref precision);

    // Keep hover value mutations under the Tweak prefix in keybind settings.
    public void OnTweakValueHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustTranslation(context, TryGetHoveredEvent(context, out var evt) ? evt : null, Precision);
    }

    public void OnYEETTranslationHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        var resolved = TryGetHoveredEvent(context, out var evt) ? evt : null;
        GLSEventHoverMutation.ToggleTranslationYeet(context, resolved);
    }

    // Use the explicit modifier action because the Alt-only value action is suppressed by more-specific chords.
    public void OnTweakEasingHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        var resolved = TryGetHoveredEvent(context, out var evt) ? evt : null;
        GLSEventHoverMutation.AdjustTranslationEasing(context, resolved);
    }

    // Name the scroll-wheel axis mutation consistently with the concise keybind label.
    public void OnTweakAxisHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // The authored axis action targets this controller's outer preview event.
        var resolved = TryGetHoveredEvent(context, out var evt) ? evt : null;
        GLSCommonCommand.CycleEventAxis(context, resolved);
    }

    // Outer previews expose only hover-specific mutations; fixed value actions remain inner-editor controls.
    public void OnValuen100(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnValuen50(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnValue0(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnValue50(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnValue100(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
}

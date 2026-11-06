using System;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSGroupFloatFXInputController : BeatmapGLSGroupInputController<BaseVfxEventEventBoxGroup>, CMInput.IGLSFloatFXObjectsActions
{
    private ScrollPrecisionController precision;

    // Resolve the current hovered preview event for this controller's GLS node type.
    private bool TryGetHoveredEvent(UnityEngine.InputSystem.InputAction.CallbackContext context, out BaseFxEventFloat evt) =>
        TryGetHoveredPreviewEvent(context, out evt);

    private ScrollPrecisionController Precision => ResolvePrecision(ref precision);

    // Keep hover value mutations under the Tweak prefix in keybind settings.
    public void OnTweakValueHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustFloatFx(context, TryGetHoveredEvent(context, out var evt) ? evt : null, Precision);
    }

    // Use the explicit Ctrl+Alt action because the Alt-only value action is suppressed by more-specific chords.
    public void OnTweakEasingHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        var resolved = TryGetHoveredEvent(context, out var evt) ? evt : null;
        GLSEventHoverMutation.AdjustEasing(context, resolved);
    }

    // Outer previews expose only hover-specific mutations; fixed value actions remain inner-editor controls.
    public void OnValuen100(InputAction.CallbackContext context) { }
    public void OnValuen50(InputAction.CallbackContext context) { }
    public void OnValue0(InputAction.CallbackContext context) { }
    public void OnValue50(InputAction.CallbackContext context) { }
    public void OnValue100(InputAction.CallbackContext context) { }
}

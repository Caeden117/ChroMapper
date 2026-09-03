using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSGroupColorInputController : BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>,
                                                   CMInput.IGLSColorObjectsActions
{
    private ScrollPrecisionController scrollPrecisionController;

    // Resolve the current hovered preview event for this controller's GLS node type.
    private bool TryGetHoveredEvent(InputAction.CallbackContext context, out BaseLightColorBase evt) =>
        TryGetHoveredPreviewEvent(context, out evt);

    private ScrollPrecisionController ScrollPrecisionController =>
        ResolvePrecision(ref scrollPrecisionController);

    // Keep hover value mutations under the Tweak prefix in keybind settings.
    public void OnTweakBrightnessHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustColorBrightness(context, TryGetHoveredEvent(context, out var evt) ? evt : null, ScrollPrecisionController);
    }

    public void OnTweakStrobeFrequencyHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustColorFrequency(context, TryGetHoveredEvent(context, out var evt) ? evt : null, ScrollPrecisionController);
    }

    public void OnTweakStrobeBrightnessHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustColorStrobeBrightness(context, TryGetHoveredEvent(context, out var evt) ? evt : null, ScrollPrecisionController);
    }

    public void OnToggleStrobeFadeHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.ToggleColorStrobeFade(context, TryGetHoveredEvent(context, out var evt) ? evt : null);
    }

    public void OnTweakEasingHover(InputAction.CallbackContext context)
    {
        var resolved = TryGetHoveredEvent(context, out var evt) ? evt : null;
        GLSEventHoverMutation.AdjustColorEasing(context, resolved);
    }

    // Outer previews support only hover-specific mutations; non-hover actions remain owned by the inner editor.
    public void OnPrimaryLightColor(InputAction.CallbackContext context) { }
    public void OnSecondaryLightColor(InputAction.CallbackContext context) { }
    public void OnWhiteLightColor(InputAction.CallbackContext context) { }
    public void OnChromaLightColor(InputAction.CallbackContext context) { }
    public void OnStrobeChromaColor(InputAction.CallbackContext context) { }
    public void OnStatic0Brightness(InputAction.CallbackContext context) { }
    public void OnStatic50Brightness(InputAction.CallbackContext context) { }
    public void OnStatic100Brightness(InputAction.CallbackContext context) { }
    public void OnFade0Brightness(InputAction.CallbackContext context) { }
    public void OnFade50Brightness(InputAction.CallbackContext context) { }
    public void OnFade100Brightness(InputAction.CallbackContext context) { }
    public void OnBrightness0(InputAction.CallbackContext context) { }
    public void OnBrightness10(InputAction.CallbackContext context) { }
    public void OnBrightness20(InputAction.CallbackContext context) { }
    public void OnBrightness30(InputAction.CallbackContext context) { }
    public void OnBrightness40(InputAction.CallbackContext context) { }
    public void OnBrightness50(InputAction.CallbackContext context) { }
    public void OnBrightness60(InputAction.CallbackContext context) { }
    public void OnBrightness70(InputAction.CallbackContext context) { }
    public void OnBrightness80(InputAction.CallbackContext context) { }
    public void OnBrightness90(InputAction.CallbackContext context) { }
    public void OnBrightness100(InputAction.CallbackContext context) { }
    public void OnBrightness120(InputAction.CallbackContext context) { }
    public void OnBrightness150(InputAction.CallbackContext context) { }
    public void OnStrobeOn(InputAction.CallbackContext context) { }
    public void OnStrobeOff(InputAction.CallbackContext context) { }
    public void OnStrobeBrightness(InputAction.CallbackContext context) { }
    public void OnSoftStrobe(InputAction.CallbackContext context) { }
    public void OnMirrorHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.MirrorColor(context, TryGetHoveredEvent(context, out var evt) ? evt : null);
    }
}

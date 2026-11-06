using System;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSGroupColorInputController : BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>,
                                                   CMInput.IGLSColorObjectsActions
{
    private static BeatmapGLSGroupColorInputController activeInstance;
    private ScrollPrecisionController scrollPrecisionController;

    private void OnEnable() => activeInstance = this;

    protected override void OnDisable()
    {
        base.OnDisable();
        if (activeInstance == this)
            activeInstance = null;
    }

    public static bool IsStrobePulseWidthOwnedByPointer()
        => activeInstance != null
            && activeInstance.IsHovering && Settings.Instance.EnableGLSGhostPreview
            && activeInstance.RaycastFirstObject(out var container)
            && container != null && !container.Dragged
            && container.PreviewEventData is BaseLightColorBase color
            && ReferenceEquals(color.EventBoxGroupData, container.EventBoxGroupData)
            && !GLSEventCommon.IsColorTransitionRibbonHit(container);

    // Resolve the current hovered preview event for this controller's GLS node type, keeping the
    // physically hit container so ribbon chords can test against the exact object under the cursor.
    private bool TryGetHoveredEvent(
        InputAction.CallbackContext context,
        out BaseLightColorBase evt,
        out GLSGroupContainer container) =>
        TryGetHoveredPreviewEvent(context, out evt, out container);

    private ScrollPrecisionController ScrollPrecisionController =>
        ResolvePrecision(ref scrollPrecisionController);

    private bool TryGetRibbonTransition(
        GLSGroupContainer container,
        BaseLightColorBase source,
        out BaseLightColorBase transition) =>
        GLSEventCommon.IsColorRibbonHover(container, source, out transition);

    public void OnTweakBrightnessHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        if (TryGetRibbonTransition(container, evt, out var transition))
        {
            GLSEventHoverMutation.CycleColorLerpType(context, transition);
        }
        else
        {
            GLSEventHoverMutation.AdjustColorBrightness(context, evt, ScrollPrecisionController);
        }
    }

    public void OnTweakStrobeFrequencyHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        if (GLSEventCommon.IsColorTransitionRibbonHit(container))
            return;

        GLSEventHoverMutation.AdjustColorFrequency(context, evt, ScrollPrecisionController);
    }

    public void OnTweakStrobeBrightnessHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        if (GLSEventCommon.IsColorTransitionRibbonHit(container))
            return;

        GLSEventHoverMutation.AdjustColorStrobeBrightness(context, evt, ScrollPrecisionController);
    }

    public void OnTweakStrobePulseWidthHover(InputAction.CallbackContext context)
    {
        if (!context.performed || !StrobePulseWidthInput.OwnsInput(context))
            return;

        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        if (GLSEventCommon.IsColorTransitionRibbonHit(container))
            return;

        GLSEventHoverMutation.AdjustColorStrobePulseWidth(context, evt, ScrollPrecisionController);
    }

    public void OnToggleStrobeFadeHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        var target = TryGetRibbonTransition(container, evt, out var transition) ? transition : evt;
        GLSEventHoverMutation.CycleColorStrobeFade(context, target);
    }

    public void OnTweakEasingHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        var target = TryGetRibbonTransition(container, evt, out var transition) ? transition : evt;
        GLSEventHoverMutation.AdjustColorEasing(context, target);
    }

    public void OnTweakStrobeColorEasingHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        var target = TryGetRibbonTransition(container, evt, out var transition) ? transition : evt;
        GLSEventHoverMutation.AdjustStrobeColorEasing(context, target);
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
        GLSEventHoverMutation.MirrorColor(context, TryGetHoveredEvent(context, out var evt, out _) ? evt : null);
    }

    public void OnToggleColorLerpTypeHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        if (GLSEventCommon.IsColorTransitionRibbonHit(container))
            return;

        GLSEventHoverMutation.ToggleColorLerpType(context, evt);
    }
}

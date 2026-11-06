using System;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventFloatFXInputController : BeatmapGLSEventInputController<BaseFxEventFloat>,
                                                     CMInput.IGLSFloatFXObjectsActions
{
    public event Action<float> OnValueChanged;
    private float currentValue;

    private void OnValueChange(float value)
    {
        if (KeybindsController.IsHoverKeyHeld)
        {
            if (IsHovering)
            {
                GLSEventFloatFXCommand.SetValue(HoveredObject.EventData as BaseFxEventFloat, value);
            }
        }
        else
        {
            NotifyValueChanged(value);
        }
    }
    
    public void OnValuen100(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(-1f);
    }

    public void OnValuen50(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(-.5f);
    }

    public void OnValue0(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(0f);
    }

    public void OnValue50(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(.5f);
    }

    public void OnValue100(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(1f);
    }

    // Keep hover value mutations under the Tweak prefix in keybind settings.
    public void OnTweakValueHover(InputAction.CallbackContext context)
    {
        // Re-resolve after every clone-producing wheel tick so pooled containers cannot redirect the next mutation.
        TryGetHoveredEvent(context, out var evt);
        GLSEventHoverMutation.AdjustFloatFx(context, evt, ScrollPrecisionController);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    // Use the explicit Ctrl+Alt action because the Alt-only value action is suppressed by more-specific chords.
    public void OnTweakEasingHover(InputAction.CallbackContext context)
    {
        // Re-resolve after every clone-producing wheel tick so pooled containers cannot redirect the next mutation.
        TryGetHoveredEvent(context, out var evt);
        GLSEventHoverMutation.AdjustEasing(context, evt);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void NotifyValueChanged(float value)
    {
        // Retain placement-restored state until an inactive GLS view subscribes during Start.
        currentValue = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }

    // Replay the last provider notification for a GLS view that initialized after map loading.
    public void RefreshViews() => OnValueChanged?.Invoke(currentValue);
}

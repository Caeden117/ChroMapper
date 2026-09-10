using System;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventTranslationInputController : BeatmapGLSEventInputController<BaseLightTranslationBase>,
                                                         CMInput.IGLSTranslationObjectsActions
{
    public event Action<float> OnValueChanged;
    private float currentValue;

    private void OnValueChange(float value)
    {
        if (KeybindsController.IsHoverKeyHeld)
        {
            if (IsHovering)
            {
                GLSEventTranslationCommand.SetValue(HoveredObject.EventData as BaseLightTranslationBase, value);
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
        GLSEventHoverMutation.AdjustTranslation(context, evt, ScrollPrecisionController);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void OnYEETTranslationHover(InputAction.CallbackContext context)
    {
        TryGetHoveredEvent(context, out var evt);
        GLSEventHoverMutation.ToggleTranslationYeet(context, evt);
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
        GLSEventHoverMutation.AdjustTranslationEasing(context, evt);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    // Name the scroll-wheel axis mutation consistently with the concise keybind label.
    public void OnTweakAxisHover(InputAction.CallbackContext context)
    {
        // Re-resolve after every clone-producing wheel tick so pooled containers cannot redirect the next mutation.
        TryGetHoveredEvent(context, out var evt);
        // Inner event-box mode uses the same group-safe axis mutation as the outer preview.
        GLSCommonCommand.CycleEventAxis(context, evt);
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

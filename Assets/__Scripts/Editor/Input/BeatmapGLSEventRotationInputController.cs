using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventRotationInputController : BeatmapGLSEventInputController<BaseLightRotationBase>,
                                                      CMInput.IGLSRotationObjectsActions
{
    public event Action<float> OnValueChanged;
    public event Action<int> OnDirectionChanged;
    public event Action<int> OnLoopChanged;
    private float currentValue;
    private int currentDirection;
    private int currentLoop;

    // REVIEW: Perhaps partner with Obama to turn this list of bools
    // into some binary shifting goodness
    private readonly bool[] heldKeys = { false, false, false, false };
    private readonly bool[] heldKeysHover = { false, false, false, false };

    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;

    private bool flagDirectionsUpdate;
    private bool flagHoverDirectionsUpdate;

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (KeybindsController.IsHoverKeyHeld)
        {
            HandleKeyHoverUpdate(context, id);
        }
        
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }

    private void HandleKeyHoverUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagHoverDirectionsUpdate = true;
        heldKeysHover[id] = context.performed;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }

        if (flagHoverDirectionsUpdate)
        {
            HandleHoverDirectionValues();
            flagHoverDirectionsUpdate = false;
        }
    }

    private bool diagonal;
    private bool hoverDiagonal;

    private void HandleDirectionValues()
    {
        var up = heldKeys[upKey];
        var down = heldKeys[downKey];
        var left = heldKeys[leftKey];
        var right = heldKeys[rightKey];
        var previousDiagonalState = diagonal;

        var upDown = up ^ down; // XOR: True if the values are different, false if the same
        var leftRight = left ^ right;

        diagonal = upDown && leftRight;

        if (previousDiagonalState && !diagonal)
        {
            StartCoroutine(CheckForDiagonalUpdate());
            return;
        }

        switch (upDown)
        {
            case true when !leftRight:
                NotifyValueChanged(up ? 0f : 180f);
                break;
            case false when leftRight:
                NotifyValueChanged(left ? 270f : 90f);
                break;
            default:
                {
                    if (diagonal)
                    {
                        if (left)
                            NotifyValueChanged(up ? 315f : 225f);
                        else
                            NotifyValueChanged(up ? 45f : 135f);
                    }

                    break;
                }
        }
    }

    private void HandleHoverDirectionValues()
    {
        if (!IsHovering) return;

        var up = heldKeysHover[upKey];
        var down = heldKeysHover[downKey];
        var left = heldKeysHover[leftKey];
        var right = heldKeysHover[rightKey];
        var previousDiagonalState = hoverDiagonal;

        var upDown = up ^ down; // XOR: True if the values are different, false if the same
        var leftRight = left ^ right;

        hoverDiagonal = upDown && leftRight;

        if (previousDiagonalState && !hoverDiagonal)
        {
            StartCoroutine(CheckForDiagonalHoverUpdate());
            return;
        }

        switch (upDown)
        {
            case true when !leftRight:
                GLSEventRotationCommand.SetValue(HoveredObject.EventData as BaseLightRotationBase, up ? 0f : 180f);
                break;
            case false when leftRight:
                GLSEventRotationCommand.SetValue(HoveredObject.EventData as BaseLightRotationBase, left ? 270f : 90f);
                break;
            default:
                {
                    if (hoverDiagonal)
                    {
                        if (left)
                        {
                            GLSEventRotationCommand.SetValue(
                                HoveredObject.EventData as BaseLightRotationBase,
                                up ? 315f : 225f);
                        }
                        else
                        {
                            GLSEventRotationCommand.SetValue(
                                HoveredObject.EventData as BaseLightRotationBase,
                                up ? 45f : 135f);
                        }
                    }

                    break;
                }
        }
    }

    public void NotifyValueChanged(float value)
    {
        // Retain placement-restored state until an inactive GLS view subscribes during Start.
        currentValue = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }

    public void OnAngle0(InputAction.CallbackContext context) => HandleKeyUpdate(context, upKey);

    public void OnAngle90(InputAction.CallbackContext context) => HandleKeyUpdate(context, rightKey);

    public void OnAngle180(InputAction.CallbackContext context) => HandleKeyUpdate(context, downKey);

    public void OnAngle270(InputAction.CallbackContext context) => HandleKeyUpdate(context, leftKey);

    // Keep hover value mutations under the Tweak prefix in keybind settings.
    public void OnTweakAngleHover(InputAction.CallbackContext context)
    {
        // Re-resolve after every clone-producing wheel tick so pooled containers cannot redirect the next mutation.
        TryGetHoveredEvent(context, out var evt);
        // Reuse the outer-track implementation so modifier behavior stays identical in both GLS views.
        GLSEventHoverMutation.AdjustRotation(context, evt, ScrollPrecisionController);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void OnTweakLoopHover(InputAction.CallbackContext context)
    {
        // Re-resolve after every clone-producing wheel tick so pooled containers cannot redirect the next mutation.
        TryGetHoveredEvent(context, out var evt);
        // The three-modifier loop chord is shared with the outer GLS group preview.
        GLSEventHoverMutation.AdjustRotationLoop(context, evt);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void OnTweakEasingHover(InputAction.CallbackContext context)
    {
        // Re-resolve after every clone-producing wheel tick so pooled containers cannot redirect the next mutation.
        TryGetHoveredEvent(context, out var evt);
        // The two-modifier easing chord is shared with the outer GLS group preview.
        GLSEventHoverMutation.AdjustEasing(context, evt);
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

    public void OnTweakDirectionHover(InputAction.CallbackContext context)
    {
        // Re-resolve after every clone-producing wheel tick so pooled containers cannot redirect the next mutation.
        TryGetHoveredEvent(context, out var evt);
        // Keep direction cycling matched with the outer GLS group preview.
        GLSEventHoverMutation.CycleRotationDirection(context, evt);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    private void OnRotationPerformed(LightRotationDirection lightRotationDirection)
    {
        if (KeybindsController.IsHoverKeyHeld)
        {
            if (IsHovering)
            {
                GLSEventRotationCommand.SetDirection(
                    HoveredObject.EventData as BaseLightRotationBase,
                    lightRotationDirection);
            }
        }
        else
        {
            NotifyDirectionChanged((int)lightRotationDirection);
        }
    }
    
    public void OnRotationDirectionLeft(InputAction.CallbackContext context)
    {
        if (context.performed) OnRotationPerformed(LightRotationDirection.CounterClockwise);
    }

    public void OnRotationDirectionAutomatic(InputAction.CallbackContext context)
    {
        if (context.performed) OnRotationPerformed(LightRotationDirection.Automatic);
    }
    public void OnRotationDirectionRight(InputAction.CallbackContext context)
    {
        if (context.performed) OnRotationPerformed(LightRotationDirection.Clockwise);
    }

    public void NotifyDirectionChanged(int value)
    {
        // Retain placement-restored state until an inactive GLS view subscribes during Start.
        currentDirection = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnDirectionChanged?.Invoke(value);
    }

    private void OnChangeLoop(int loopChange)
    {
        if (KeybindsController.IsHoverKeyHeld)
        {
            if (IsHovering)
            {
                if (loopChange == 1)
                {
                    var evt = HoveredObject.EventData as BaseLightRotationBase;
                    GLSEventRotationCommand.SetLoop(evt, (evt.Loop + 1) % 5);
                }
                else
                {
                    GLSEventRotationCommand.SetLoop(HoveredObject.EventData as BaseLightRotationBase, 0);
                }
            }
        }
        else
        {
            NotifyLoopChanged(loopChange);
        }
    }
    
    public void OnChangeLoopCount(InputAction.CallbackContext context)
    {
        if (context.performed) OnChangeLoop(1);
    }

    public void OnResetLoopCount(InputAction.CallbackContext context)
    {
        if (context.performed) OnChangeLoop(0);
    }

    // Extension placement clears the remembered loops without switching the easing selector back to a normal node.
    public void NotifyLoopChanged(int value, bool clearExtension = true)
    {
        // Retain placement-restored state until an inactive GLS view subscribes during Start.
        currentLoop = value;
        if (clearExtension)
            EasingInputController.NotifyExtensionChanged(0);
        OnLoopChanged?.Invoke(value);
    }

    // Replay the last provider notification for a GLS view that initialized after map loading.
    public void RefreshViews()
    {
        OnValueChanged?.Invoke(currentValue);
        OnLoopChanged?.Invoke(currentLoop);
        OnDirectionChanged?.Invoke(currentDirection);
    }

    private IEnumerator CheckForDiagonalUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeys);
        yield return new WaitForSeconds(0.1f);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys
            .Except(heldKeys)
            .Any())
            flagDirectionsUpdate = true;
    }

    private IEnumerator CheckForDiagonalHoverUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeysHover);
        yield return new WaitForSeconds(0.1f);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys
            .Except(heldKeysHover)
            .Any())
            flagHoverDirectionsUpdate = true;
    }
}

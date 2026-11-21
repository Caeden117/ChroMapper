using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Strobe pulse width is the GLS scroll tweak whose ownership is negotiated with the timeline: the
// same wheel tick can mean editing the hovered color node or changing the cursor interval. This
// helper adds the pointer target to the shared chord arbitration so both sides agree without reading
// physical modifier keys.
public static class StrobePulseWidthInput
{
    public static readonly Guid PulseWidthActionId = new Guid("9fc4d3fb-3c94-4acd-82ee-8c7b4c16d1ae");

    public const string InteractionName = "StrobePulseWidth";

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void RegisterInteraction()
    {
        // Fixture runtime resets drop project registrations; isolated test fixtures re-register on Setup.
        InputSystem.RegisterInteraction(typeof(StrobePulseWidthInteraction), InteractionName);
    }

    public static bool OwnsInput(InputAction.CallbackContext context)
    {
        var action = context.action;
        if (action != null && action.id != PulseWidthActionId)
        {
            action = FindPulseWidthAction(action.actionMap?.asset);
        }

        return OwnsInput(action, context.control);
    }

    public static bool OwnsInput(InputAction pulseWidthAction, InputControl triggeringControl)
    {
        if (pulseWidthAction == null
            || pulseWidthAction.id != PulseWidthActionId
            || !pulseWidthAction.enabled
            || triggeringControl == null)
        {
            return false;
        }

        // The wheel only belongs to pulse width while the pointer is on an editable color node.
        if (!BeatmapGLSEventColorInputController.IsStrobePulseWidthOwnedByPointer()
            && !BeatmapGLSGroupColorInputController.IsStrobePulseWidthOwnedByPointer())
        {
            return false;
        }

        return InputChordOwnership.Owns(pulseWidthAction, triggeringControl);
    }

    private static InputAction FindPulseWidthAction(InputActionAsset asset) =>
        asset == null ? null : asset.FindAction(PulseWidthActionId);
}

// Staying in the waiting phase on every unowned tick is what keeps the wheel un-consumed for the
// timeline even when shortcutKeysConsumeInput is enabled.
public class StrobePulseWidthInteraction : IInputInteraction
{
    public void Process(ref InputInteractionContext context)
    {
        if (context.ReadValue<float>() != 0f
            && StrobePulseWidthInput.OwnsInput(context.action, context.control))
        {
            context.Performed();
        }
    }

    public void Reset()
    {
    }
}

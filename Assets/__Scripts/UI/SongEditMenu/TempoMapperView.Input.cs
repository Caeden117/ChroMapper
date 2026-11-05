using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public partial class TempoMapperView
{
    private CMInput input;
    private bool heldPlayback;
    private int heldPlaybackSample;
    private double heldViewStart;
    private double heldViewEnd;
    // Match AudioTimeSyncController: tapping toggles, holding then releasing cancels playback.
    private const double holdPlaybackDuration = 0.3;

    private bool CanHandleMenuInput => initialized && isActiveAndEnabled && Application.isFocused
        && !confirmationOpen && !OptionsController.IsActive;

    private bool CanHandleInput
    {
        get
        {
            if (!CanHandleMenuInput)
                return false;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected == null || selected.GetComponent<TMP_InputField>() is not { isFocused: true };
        }
    }

    private void InstallInput()
    {
        // Block the song menu's installed handlers, while subscribing this modal to the same rebinding-aware actions.
        // AddCallbacks/RemoveCallbacks scopes subscriptions to this instance without cloning or replacing an action map.
        CMInputCallbackInstaller.DisableActionMaps(typeof(TempoMapperView), blockedInputs);
        input = CMInputCallbackInstaller.InputInstance;
        if (input == null) return;
        input.Timeline.AddCallbacks(this);
        input.Actions.AddCallbacks(this);
        input.Playback.AddCallbacks(this);
        input.Saving.AddCallbacks(this);
        input.MenusExtended.AddCallbacks(this);
    }

    private void UninstallInput()
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(TempoMapperView), blockedInputs);
        if (input == null) return;
        input.Timeline.RemoveCallbacks(this);
        input.Actions.RemoveCallbacks(this);
        input.Playback.RemoveCallbacks(this);
        input.Saving.RemoveCallbacks(this);
        input.MenusExtended.RemoveCallbacks(this);
        input = null;
    }

    public void OnChangeTimeandPrecision(InputAction.CallbackContext context)
    {
        if (!context.performed || !CanHandleInput || !PrecisionScrollActive) return;
        SetSubdivision(StepSubdivision(Subdivision, context.ReadValue<float>(), input.Timeline.PreciseSnapModification.IsPressed()));
    }

    public void OnPreciselyChangeTimeandPrecision(InputAction.CallbackContext context)
    {
        if (!context.performed || !CanHandleInput || !PrecisionScrollActive) return;
        SetSubdivision(StepSubdivision(Subdivision, context.ReadValue<float>(), true));
    }

    public void OnChangePrecisionModifier(InputAction.CallbackContext context) { }
    public void OnPreciseSnapModification(InputAction.CallbackContext context) { }
    public void OnUserTrace(InputAction.CallbackContext context) { }
    public void OnSaveQuest(InputAction.CallbackContext context) { }

    public void OnUndoMethod1(InputAction.CallbackContext context)
    {
        if (context.performed && CanHandleInput) Undo();
    }

    public void OnUndoMethod2(InputAction.CallbackContext context) => OnUndoMethod1(context);

    public void OnRedoMethod1(InputAction.CallbackContext context)
    {
        if (context.performed && CanHandleInput) Redo();
    }

    public void OnRedoMethod2(InputAction.CallbackContext context) => OnRedoMethod1(context);

    public void OnTogglePlaying(InputAction.CallbackContext context)
    {
        if (context.performed && CanHandleInput)
        {
            var starting = !music.isPlaying;
            heldPlaybackSample = Sample;
            heldViewStart = ViewStart;
            heldViewEnd = ViewEnd;
            TogglePlaying();
            heldPlayback = starting;
        }
        if (context.canceled)
        {
            if (heldPlayback && context.duration >= holdPlaybackDuration) CancelHeldPlayback();
            heldPlayback = false;
        }
    }

    private void CancelHeldPlayback()
    {
        PausePlayback();
        Seek(heldPlaybackSample);
        ViewStart = heldViewStart;
        ViewEnd = heldViewEnd;
        Refresh();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && heldPlayback) CancelHeldPlayback();
    }

    public void OnResetTime(InputAction.CallbackContext context)
    {
        if (context.performed && CanHandleInput) Seek(0);
    }

    public void OnSave(InputAction.CallbackContext context)
    {
        if (context.performed && CanHandleInput) Save();
    }

    public void OnLeaveMenu(InputAction.CallbackContext context)
    {
        if (context.performed && CanHandleMenuInput)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            RequestClose();
        }
    }

    public void OnTab(InputAction.CallbackContext context)
    {
        if (context.performed && CanHandleInput) bpm.Selectable.Select();
    }
}

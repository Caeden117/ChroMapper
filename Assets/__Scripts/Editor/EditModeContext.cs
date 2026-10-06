using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditModeContext : MonoBehaviour, CMInput.IEditModeActions, IEditorStateProvider
{
    [SerializeField] private EditingMode editingMode = EditingMode.Gameplay;

    public EditingMode EditingMode
    {
        get => editingMode;
        set
        {
            if (editingMode == value) return;
            editingMode = value;
            NotifyChanged();
        }
    }

    public event Action<EditingMode> OnEditModeChanged;
    // Keep the active workspace tab with the context that publishes tab changes.
    public string StateKey => "editingMode";
    public void NotifyChanged() => OnEditModeChanged?.Invoke(EditingMode);

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        NotifyChanged();
    }

    private void Start()
    {
        EditorStateService.Register(this);
    }

    // Remove the workspace context from saves after its scene has been destroyed.
    private void OnDestroy() => EditorStateService.Unregister(this);

    // Persist the parent GLS tab instead of transient event-box lanes, which Escape would leave before saving.
    public void CaptureEditorState(SimpleJSON.JSONObject data)
    {
        var persistedMode = editingMode == EditingMode.EventBox
            ? EditingMode.GLS
            : editingMode;
        data["mode"] = (int)persistedMode;
    }

    // Let the context notify every tab view when its saved workspace mode is restored.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        if (data.HasKey("mode"))
        {
            var savedMode = (EditingMode)data["mode"].AsInt;
            // Accept old EventBox metadata but never reopen a map directly into its node-lane view.
            EditingMode = savedMode == EditingMode.EventBox
                ? EditingMode.GLS
                : savedMode;
        }
    }

    public void OnGameplayEdit(InputAction.CallbackContext context)
    {
        if (context.performed) EditingMode = EditingMode.Gameplay;
    }

    // prevents F2 from disabling the gameplay tracks that own the playing camera while a preview UI mode is active.
    public void OnGLSEdit(InputAction.CallbackContext context)
    {
        if (context.performed && !UIMode.PreviewMode)
            EditingMode = EditingMode.GLS;
    }

    // Ditto but F3
    public void OnBasicEventEdit(InputAction.CallbackContext context)
    {
        if (context.performed && !UIMode.PreviewMode)
            EditingMode = EditingMode.BasicEvent;
    }
}

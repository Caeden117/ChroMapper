using Beatmap.Base;
using UnityEngine;

public class GLSEventFloatFXPlacement : GLSEventPlacement<BaseVfxEventEventBoxGroup, BaseFxEventFloat>, IEditorStateProvider
{
    [SerializeField] private BeatmapGLSEventFloatFXInputController inputController;

    public override void Start()
    {
        base.Start();
        inputController.OnValueChanged += HandleValueChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        // Restore after this placement has connected its input callbacks.
        EditorStateService.Register(this);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        inputController.OnValueChanged -= HandleValueChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    // Keep the inner GLS FloatFX preview state with its placement owner.
    public string StateKey => "floatFxEvent";
    public void CaptureEditorState(SimpleJSON.JSONObject data) => GLSPlacementEditorState.WriteFloatFx(data, QueuedData);

    // Apply only this placement's cached FloatFX-node data after map metadata loads.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        GLSPlacementEditorState.ReadFloatFx(data, QueuedData);
        inputController.NotifyValueChanged(QueuedData.Value);
        GLSPlacementEditorState.RefreshFloatFxViews(QueuedData);
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Value = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Easing = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleExtensionChanged(int value)
    {
        QueuedData.UsePrevious = value;
        GLSPlacementEditorState.ClearExtensionEasing(QueuedData, EasingInputController);
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseFxEventFloat GenerateOriginalData() => new();
}

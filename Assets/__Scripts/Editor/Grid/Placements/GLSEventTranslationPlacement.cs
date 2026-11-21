using Beatmap.Base;
using UnityEngine;

public class
    GLSEventTranslationPlacement : GLSEventPlacement<BaseLightTranslationEventBoxGroup, BaseLightTranslationBase>, IEditorStateProvider
{
    [SerializeField] private BeatmapGLSEventTranslationInputController inputController;

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

    // Keep the inner GLS translation preview state with its placement owner.
    public string StateKey => "translationEvent";
    public void CaptureEditorState(SimpleJSON.JSONObject data) => GLSPlacementEditorState.WriteTranslation(data, QueuedData);

    // Apply only this placement's cached translation-node data after map metadata loads.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        GLSPlacementEditorState.ReadTranslation(data, QueuedData);
        inputController.NotifyValueChanged(QueuedData.Translation);
        GLSPlacementEditorState.RefreshTranslationViews(QueuedData);
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Translation = value;
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

    protected override BaseLightTranslationBase GenerateOriginalData() => new();
}

using Beatmap.Base;
using UnityEngine;

public class
    GLSEventRotationPlacement : GLSEventPlacement<BaseLightRotationEventBoxGroup, BaseLightRotationBase>, IEditorStateProvider
{
    [SerializeField] private BeatmapGLSEventRotationInputController inputController;

    public override void Start()
    {
        base.Start();
        inputController.OnValueChanged += HandleValueChanged;
        inputController.OnLoopChanged += HandleLoopChanged;
        inputController.OnDirectionChanged += HandleDirectionChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        // Restore after this placement has connected its input callbacks.
        EditorStateService.Register(this);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        inputController.OnValueChanged -= HandleValueChanged;
        inputController.OnLoopChanged -= HandleLoopChanged;
        inputController.OnDirectionChanged -= HandleDirectionChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    // Keep the inner GLS rotation preview state with its placement owner.
    public string StateKey => "rotationEvent";
    public void CaptureEditorState(SimpleJSON.JSONObject data) => GLSPlacementEditorState.WriteRotation(data, QueuedData);

    // Apply only this placement's cached rotation-node data after map metadata loads.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        GLSPlacementEditorState.ReadRotation(data, QueuedData);
        inputController.NotifyValueChanged(QueuedData.Rotation);
        inputController.NotifyLoopChanged(QueuedData.Loop);
        inputController.NotifyDirectionChanged(QueuedData.Direction);
        GLSPlacementEditorState.RefreshRotationViews(QueuedData);
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Rotation = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleLoopChanged(int value)
    {
        QueuedData.Loop = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleDirectionChanged(int value)
    {
        QueuedData.Direction = value;
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
        GLSPlacementEditorState.ClearRotationExtensionLoops(QueuedData, inputController);
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    // Copied or restored queues can bypass the Extension callback. Clear their loop count before placing a
    // new extension.
    public override void HandleApply()
    {
        GLSPlacementEditorState.ClearRotationExtensionLoops(QueuedData, inputController);
        base.HandleApply();
    }

    protected override BaseLightRotationBase GenerateOriginalData() => new();
}

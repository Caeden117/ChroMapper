using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

public class
    GLSGroupRotationPlacement : GLSGroupPlacement<BaseLightRotationEventBoxGroup, GLSGroupRotationGridContainer>, IEditorStateProvider
{
    [SerializeField] private BeatmapGLSGroupRotationInputController groupInputController;
    [SerializeField] private BeatmapGLSEventRotationInputController eventInputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.RotationTracks.Any(x => x) && !groupInputController.IsHovering;

    public override void Start()
    {
        base.Start();
        eventInputController.OnValueChanged += HandleValueChanged;
        eventInputController.OnLoopChanged += HandleLoopChanged;
        eventInputController.OnDirectionChanged += HandleDirectionChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        // Restore after this placement has connected its input callbacks.
        EditorStateService.Register(this);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        eventInputController.OnValueChanged -= HandleValueChanged;
        eventInputController.OnLoopChanged -= HandleLoopChanged;
        eventInputController.OnDirectionChanged -= HandleDirectionChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    // Keep the outer GLS rotation preview state with its placement owner.
    public string StateKey => "rotationGroup";
    public void CaptureEditorState(SimpleJSON.JSONObject data) => GLSPlacementEditorState.WriteRotation(data, QueuedData.Boxes[0].Events[0]);

    // Apply only this placement's cached rotation-group data after map metadata loads.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        var queuedEvent = QueuedData.Boxes[0].Events[0];
        GLSPlacementEditorState.ReadRotation(data, queuedEvent);
        eventInputController.NotifyValueChanged(queuedEvent.Rotation);
        eventInputController.NotifyLoopChanged(queuedEvent.Loop);
        eventInputController.NotifyDirectionChanged(queuedEvent.Direction);
        GLSPlacementEditorState.RefreshRotationViews(queuedEvent);
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Rotation = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleLoopChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Loop = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleDirectionChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Direction = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].EaseType = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleExtensionChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].UsePrevious = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightRotationEventBoxGroup GenerateOriginalData() =>
        BeatmapFactory.Clone(
            new BaseLightRotationEventBoxGroup()
            {
                Boxes = new()
                {
                    new BaseLightRotationEventBox
                    {
                        IsAutomaticAxisLane = true,
                        Events = new[] { new BaseLightRotationBase() }
                    }
                }
            });
}

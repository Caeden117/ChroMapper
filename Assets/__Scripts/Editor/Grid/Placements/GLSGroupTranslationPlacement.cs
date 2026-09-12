using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

public class
    GLSGroupTranslationPlacement : GLSGroupPlacement<BaseLightTranslationEventBoxGroup,
    GLSGroupTranslationGridContainer>, IEditorStateProvider
{
    [SerializeField] private BeatmapGLSGroupTranslationInputController groupInputController;
    [SerializeField] private BeatmapGLSEventTranslationInputController eventInputController;

    public override bool CanPlace =>
        base.CanPlace
        && GlsGroupTrack.TrackDefinition.TranslationTracks.Any(x => x)
        && !groupInputController.IsHovering;

    public override void Start()
    {
        base.Start();
        eventInputController.OnValueChanged += HandleValueChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        // Restore after this placement has connected its input callbacks.
        EditorStateService.Register(this);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        eventInputController.OnValueChanged -= HandleValueChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    // Keep the outer GLS translation preview state with its placement owner.
    public string StateKey => "translationGroup";
    public void CaptureEditorState(SimpleJSON.JSONObject data) => GLSPlacementEditorState.WriteTranslation(data, QueuedData.Boxes[0].Events[0]);

    // Apply only this placement's cached translation-group data after map metadata loads.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        var queuedEvent = QueuedData.Boxes[0].Events[0];
        GLSPlacementEditorState.ReadTranslation(data, queuedEvent);
        eventInputController.NotifyValueChanged(queuedEvent.Translation);
        GLSPlacementEditorState.RefreshTranslationViews(queuedEvent);
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Translation = value;
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

    protected override BaseLightTranslationEventBoxGroup GenerateOriginalData() =>
        BeatmapFactory.Clone(
            new BaseLightTranslationEventBoxGroup()
            {
                Boxes = new()
                {
                    new BaseLightTranslationEventBox
                    {
                        IsAutomaticAxisLane = true,
                        Events = new[] { new BaseLightTranslationBase() }
                    }
                }
            });
}

using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

public class GLSEventColorPlacement : GLSEventPlacement<BaseLightColorEventBoxGroup, BaseLightColorBase>, IEditorStateProvider
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;
    [SerializeField] private ColorPicker colorPicker;

    private int selectedColor;

    public override void Start()
    {
        base.Start();
        // Unity objects require their overloaded null comparison when resolving cross-prefab scene references.
        if (colorPicker == null)
            colorPicker = ColourPicker.ActivePicker;
        if (colorPicker == null)
            colorPicker = FindObjectOfType<ColorPicker>();
        selectedColor = QueuedData.Color;
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        inputController.OnFadeChanged += HandleEasingChanged;
        inputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        inputController.OnColorDistributionsChanged += HandleColorDistributionsChanged;
        inputController.OnStrobeColorDistributionsChanged += HandleStrobeColorDistributionsChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        ColorTypeController.OnColorChanged += HandleColorChanged;
        // Restore the event preview only after it has subscribed to its menu controls.
        EditorStateService.Register(this);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        inputController.OnColorChanged -= HandleColorChanged;
        inputController.OnBrightnessChanged -= HandleBrightnessChanged;
        inputController.OnFadeChanged -= HandleEasingChanged;
        inputController.OnStrobeFrequencyChanged -= HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged -= HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged -= HandleSoftStrobeChanged;
        inputController.OnColorDistributionsChanged -= HandleColorDistributionsChanged;
        inputController.OnStrobeColorDistributionsChanged -= HandleStrobeColorDistributionsChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
        ColorTypeController.OnColorChanged -= HandleColorChanged;
    }

    // Keep this inner GLS color-node preview in its own map metadata node.
    public string StateKey => "colorEvent";

    // Let the owner write its queued GLS color node without global object discovery.
    public void CaptureEditorState(JSONObject data) => GLSPlacementEditorState.WriteColor(data, QueuedData);

    // Apply only this placement's cached color-node data after map metadata loads.
    public void LoadEditorState(JSONNode data)
    {
        // Keep late metadata loads identical to Start-time restoration for every GLS color control.
        GLSPlacementEditorState.RestoreColorPlacementState(data, QueuedData, inputController);
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        base.HandlePlacementToData(inputState);
        QueuedData.Color = selectedColor;
        // Extension nodes inherit color from the previous node and must not carry independent Chroma RGB data.
        if (QueuedData.UsePrevious == 0 && EventPlacement.CanPlaceChromaEvents && colorPicker != null)
        {
            QueuedData.CustomColor = colorPicker.CurrentColor;
        }
        else
        {
            QueuedData.CustomColor = null;
        }
        // Apply the independent strobe picker only to non-extension GLS color nodes.
        QueuedData.StrobeColor = QueuedData.UsePrevious == 0 && StrobeColorPickerController.Instance is { IsEnabled: true } strobePicker
            ? strobePicker.CurrentColor
            : null;
        QueuedData.SaveCustom();
        if (PlacementVisualContainer != null)
        {
            RefreshAppearance();
        }
    }

    private void HandleColorChanged(int value)
    {
        selectedColor = value;
        QueuedData.Color = selectedColor;
        // Extension nodes inherit color from the previous node and must not carry independent Chroma RGB data.
        if (QueuedData.UsePrevious == 0 && EventPlacement.CanPlaceChromaEvents && colorPicker != null)
        {
            QueuedData.CustomColor = colorPicker.CurrentColor;
        }
        else
        {
            QueuedData.CustomColor = null;
        }
        // Keep the queued strobe override aligned when the primary GLS color changes.
        QueuedData.StrobeColor = QueuedData.UsePrevious == 0 && StrobeColorPickerController.Instance is { IsEnabled: true } strobePicker
            ? strobePicker.CurrentColor
            : null;
        QueuedData.SaveCustom();
        if (PlacementVisualContainer != null)
        {
            RefreshAppearance();
        }
    }

    private void HandleBrightnessChanged(float value)
    {
        QueuedData.Brightness = Mathf.Max(value, 0f);
        RefreshAppearance();
    }

    private void HandleStrobeFrequencyChanged(int value)
    {
        QueuedData.Frequency = value;
        RefreshAppearance();
    }

    private void HandleStrobeBrightnessChanged(float value)
    {
        QueuedData.StrobeBrightness = Mathf.Max(value, 0f);
        RefreshAppearance();
    }

    private void HandleSoftStrobeChanged(int value)
    {
        QueuedData.StrobeFade = value;
        RefreshAppearance();
    }

    private void HandleColorDistributionsChanged(string[] value)
    {
        QueuedData.ColorDistributions = value;
        QueuedData.WriteCustom();
        RefreshAppearance();
    }

    private void HandleStrobeColorDistributionsChanged(string[] value)
    {
        QueuedData.StrobeColorDistributions = value;
        QueuedData.WriteCustom();
        RefreshAppearance();
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Easing = (int)(value >= 0 ? EaseType.Linear : EaseType.None);
        RefreshAppearance();
    }

    private void HandleExtensionChanged(int value)
    {
        QueuedData.UsePrevious = value;
        GLSPlacementEditorState.ClearExtensionEasing(QueuedData, EasingInputController);
        // Remove both custom color channels immediately when this node becomes an extension node.
        if (value != 0)
        {
            QueuedData.CustomColor = null;
            QueuedData.StrobeColor = null;
            QueuedData.SaveCustom();
        }
        RefreshAppearance();
    }

    protected override BaseLightColorBase GenerateOriginalData() => new();
}

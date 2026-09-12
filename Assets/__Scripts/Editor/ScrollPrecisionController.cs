using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// we use list here simply for the fact that we could possibly extend it
// ScrollPrecisionEditorStateTest requires the map-local precision owner to participate
// in the same EditorData provider lifecycle as the other restored editor controls.
public class ScrollPrecisionController : MonoBehaviour, CMInput.IScrollPrecisionActions, IEditorStateProvider
{
    public event Action<ScrollPrecision> OnPrecisionChanged;

    public const int MaxPrecision = 4;
    [SerializeField] private ScrollPrecision currentPrecision = ScrollPrecision.Medium;

    public ScrollPrecision CurrentPrecision
    {
        get => currentPrecision;
        set
        {
            if (currentPrecision == value) return;
            currentPrecision = value;
            OnPrecisionChanged?.Invoke(currentPrecision);
        }
    }

    public List<float> BrightnessPrecision = new(MaxPrecision) { 1f, 2.5f, 10f, 100f };
    public List<float> RotationPrecision = new(MaxPrecision) { 1f, 2.5f, 15f, 30f };
    public List<float> TranslationPrecision = new(MaxPrecision) { 1f, 2.5f, 10f, 100f };
    public List<float> FloatFXPrecision = new(MaxPrecision) { 1f, 2.5f, 10f, 100f };
    public List<float> AngleOffsetPrecision = new(MaxPrecision) { 1f, 2f, 5f, 15f };
    public List<float> TimePrecision = new(MaxPrecision) { 0.01f, 0.1f, 0.25f, 1f };
    public List<float> PercentPrecision = new(MaxPrecision) { 1f, 5f, 10f, 50f };
    public List<float> MultiplierPrecision = new(MaxPrecision) { 0.01f, 0.025f, 0.1f, 0.5f };

    // Ring and laser basic-event tweak precisions.
    public List<float> RingZoomStepPrecision = new(MaxPrecision) { 0.005f, 0.02f, 0.1f, 0.5f };
    public List<float> RingRotationStepPrecision = new(MaxPrecision) { 0.1f, 1f, 5f, 20f };
    public List<float> RingRotationPropagationPrecision = new(MaxPrecision) { 0.001f, 0.01f, 0.1f, 1f };
    public List<float> LaserSpeedPrecision = new(MaxPrecision) { 0.1f, 0.5f, 1f, 5f };

    // Default starting values for Chroma ring/laser fields when scrolling creates them.
    public const float DefaultRingRotation = 90f;
    public const float DefaultRingZoomStep = 2f;
    public const float DefaultRingRotationStep = 10f;
    public const float DefaultRingPropagation = 2f;
    public const float DefaultRingSpeed = 5f;

    // Keep scroll precision isolated under its own EditorData component key.
    public string StateKey => "scrollPrecision";

    // Register after scene initialization so EditorStateService can either hydrate from
    // its loaded cache immediately or include this controller in the next map save.
    private void Start() => EditorStateService.Register(this);

    // A destroyed map scene must not leave its precision controller in later saves.
    private void OnDestroy() => EditorStateService.Unregister(this);

    // ScrollPrecisionEditorStateTest locks the map-local precision value into EditorData.
    public void CaptureEditorState(SimpleJSON.JSONObject data) => data["value"] = (int)CurrentPrecision;

    // Restore through the property so the slider and every other subscriber receive the
    // same change notification as an interactive precision change.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        if (data.HasKey("value"))
        {
            CurrentPrecision = (ScrollPrecision)Math.Clamp(data["value"].AsInt, 0, MaxPrecision - 1);
        }
    }

    public float GetCurrentBrightnessPrecision() => BrightnessPrecision[(int)CurrentPrecision];
    public float GetCurrentRotationPrecision() => RotationPrecision[(int)CurrentPrecision];
    public float GetCurrentTranslationPrecision() => TranslationPrecision[(int)CurrentPrecision];
    public float GetCurrentFloatFXPrecision() => FloatFXPrecision[(int)CurrentPrecision];
    public float GetCurrentAngleOffsetPrecision() => AngleOffsetPrecision[(int)CurrentPrecision];
    public float GetCurrentTimePrecision() => TimePrecision[(int)CurrentPrecision];
    public float GetCurrentPercentPrecision() => PercentPrecision[(int)CurrentPrecision];
    public float GetCurrentMultiplierPrecision() => MultiplierPrecision[(int)CurrentPrecision];
    public float GetCurrentRingZoomStepPrecision() => RingZoomStepPrecision[(int)CurrentPrecision];
    public float GetCurrentRingRotationStepPrecision() => RingRotationStepPrecision[(int)CurrentPrecision];
    public float GetCurrentRingRotationPropagationPrecision() => RingRotationPropagationPrecision[(int)CurrentPrecision];
    public float GetCurrentLaserSpeedPrecision() => LaserSpeedPrecision[(int)CurrentPrecision];

    public void OnScroll(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var isRing = BeatmapEventInputController.IsRingRotationHoveredByPointer();
        // GLS and Basic Event ring-step hover actions own this chord and must not also change global precision.
        if (GLSEventInputHoverTracker.IsHovering
            || isRing)
        {
            return;
        }
        var delta = context.GetScrollDirection(Settings.Instance.InvertPrecisionScroll);
        CurrentPrecision = (ScrollPrecision)Math.Clamp((byte)CurrentPrecision - delta, 0, MaxPrecision - 1);
    }
}

public enum ScrollPrecision : byte
{
    Ultra,
    High,
    Medium,
    Low
}

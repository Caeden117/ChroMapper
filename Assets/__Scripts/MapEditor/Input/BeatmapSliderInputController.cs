using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapSliderInputController : BeatmapInputController<BeatmapSliderContainer>, CMInput.ISliderObjectsActions
{
    public const float MuChangeSpeed = 0.1f;
    [FormerlySerializedAs("sliderAppearanceSO")] [SerializeField] private SliderAppearanceSO sliderAppearanceSo;
    public void OnChangingMu(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragging || !context.performed) return;

        var modifier = context.ReadValue<float>();
        modifier = modifier > 0 ? MuChangeSpeed : -MuChangeSpeed;
        ChangeMu(e, modifier);
    }

    public void ChangeMu(BeatmapSliderContainer s, float modifier)
    {
        var original = BeatmapObject.GenerateCopy(s.SliderData);
        s.ChangeMu(modifier);
        s.NotifySplineChanged();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(s.ObjectData, s.ObjectData, original));
    }

    public void OnInvertSliderColor(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) ||
            !KeybindsController.IsMouseInWindow || !context.performed)
        {
            return;
        }

        RaycastFirstObject(out var slider);
        if (slider != null && !slider.Dragging) InvertSlider(slider);
    }

    public void InvertSlider(BeatmapSliderContainer slider)
    {
        var original = BeatmapObject.GenerateCopy(slider.SliderData);
        var newType = slider.SliderData.Color == BeatmapNote.NoteTypeA
            ? BeatmapNote.NoteTypeB
            : BeatmapNote.NoteTypeA;
        slider.SliderData.Color = newType;
        sliderAppearanceSo.SetSliderAppearance(slider);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(slider.ObjectData, slider.ObjectData, original, "invert slider color"));
    }

    public void OnChangingTmu(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragging || !context.performed) return;

        var modifier = context.ReadValue<float>();
        modifier = modifier > 0 ? MuChangeSpeed : -MuChangeSpeed;
        ChangeTmu(e, modifier);
    }

    public void ChangeTmu(BeatmapSliderContainer s, float modifier)
    {
        var original = BeatmapObject.GenerateCopy(s.SliderData);
        s.ChangeTmu(modifier);
        s.NotifySplineChanged();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(s.ObjectData, s.ObjectData, original));
    }
}

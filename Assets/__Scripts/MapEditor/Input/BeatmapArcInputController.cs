using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapArcInputController : BeatmapInputController<BeatmapArcContainer>, CMInput.IArcObjectsActions
{
    public const float MuChangeSpeed = 0.1f;
    [FormerlySerializedAs("arcAppearanceSO")] [SerializeField] private ArcAppearanceSO arcAppearanceSo;
    public void OnChangingMu(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragging || !context.performed) return;

        var modifier = context.ReadValue<float>();
        modifier = modifier > 0 ? MuChangeSpeed : -MuChangeSpeed;
        ChangeMu(e, modifier);
    }

    public void ChangeMu(BeatmapArcContainer s, float modifier)
    {
        var original = BeatmapObject.GenerateCopy(s.ArcData);
        s.ChangeMu(modifier);
        s.NotifySplineChanged();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(s.ObjectData, s.ObjectData, original));
    }

    public void OnInvertArcColor(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) ||
            !KeybindsController.IsMouseInWindow || !context.performed)
        {
            return;
        }

        RaycastFirstObject(out var arc);
        if (arc != null && !arc.Dragging) InvertArc(arc);
    }

    public void InvertArc(BeatmapArcContainer arc)
    {
        var original = BeatmapObject.GenerateCopy(arc.ArcData);
        var newType = arc.ArcData.Color == BeatmapNote.NoteTypeA
            ? BeatmapNote.NoteTypeB
            : BeatmapNote.NoteTypeA;
        arc.ArcData.Color = newType;
        arcAppearanceSo.SetArcAppearance(arc);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(arc.ObjectData, arc.ObjectData, original, "invert arc color"));
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

    public void ChangeTmu(BeatmapArcContainer s, float modifier)
    {
        var original = BeatmapObject.GenerateCopy(s.ArcData);
        s.ChangeTmu(modifier);
        s.NotifySplineChanged();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(s.ObjectData, s.ObjectData, original));
    }
}

using System.Collections;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapArcInputController : BeatmapInputController<ArcContainer>, CMInput.IArcObjectsActions
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

    public void ChangeMu(ArcContainer s, float modifier)
    {
        var original = BeatmapFactory.Clone(s.ArcData);
        s.ChangeMultiplier(modifier);
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

    public void InvertArc(ArcContainer arc)
    {
        var original = BeatmapFactory.Clone(arc.ArcData);
        var newType = arc.ArcData.Color == (int)NoteType.Red
            ? (int)NoteType.Blue
            : (int)NoteType.Red;
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

    public void ChangeTmu(ArcContainer s, float modifier)
    {
        var original = BeatmapFactory.Clone(s.ArcData);
        s.ChangeTailMultiplier(modifier);
        s.NotifySplineChanged();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(s.ObjectData, s.ObjectData, original));
    }
}

using System.Collections;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V4;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapArcInputController : BeatmapInputController<ArcContainer>, CMInput.IArcObjectsActions
{
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private ArcAppearanceSO arcAppearance;

    public void OnChangingMu(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollArcMultiplier)
            * scrollPrecisionController.GetCurrentMultiplierPrecision();
        ChangeMu(HoveredObject, modifier);
    }

    public void ChangeMu(ArcContainer s, float modifier)
    {
        float GetMultiplier(BaseArc data) => CMMath.RoundToDecimals(data.HeadControlPointLengthMultiplier + modifier);
        if (HoverSelectionMutation.TryApply(
                s.ArcData,
                data => data.HeadControlPointLengthMultiplier = GetMultiplier(data),
                out _))
        {
            return;
        }

        ArcCommand.SetHeadControlPointLengthMultiplier(s.ArcData, GetMultiplier(s.ArcData));
    }

    public void OnChangingTmu(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollArcMultiplier)
            * scrollPrecisionController.GetCurrentMultiplierPrecision();
        ChangeTmu(HoveredObject, modifier);
    }

    public void ChangeTmu(ArcContainer s, float modifier)
    {
        float GetMultiplier(BaseArc data) => CMMath.RoundToDecimals(data.TailControlPointLengthMultiplier + modifier);
        if (HoverSelectionMutation.TryApply(
                s.ArcData,
                data => data.TailControlPointLengthMultiplier = GetMultiplier(data),
                out _))
        {
            return;
        }

        ArcCommand.SetTailControlPointLengthMultiplier(s.ArcData, GetMultiplier(s.ArcData));
    }
}

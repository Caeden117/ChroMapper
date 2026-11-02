using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapChainInputController : BeatmapInputController<ChainContainer>, CMInput.IChainObjectsActions
{
    private const int minChainCount = 1;
    private const int maxChainCount = 999;
    private const float minChainSquish = 0.1f;
    private const float maxChainSquish = 999;

    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private ChainAppearanceSO chainAppearance;

    public void OnTweakChainCount(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollChainSegmentCount);
        TweakValue(HoveredObject, modifier);
    }

    public void TweakValue(ChainContainer c, int modifier)
    {
        int GetSliceCount(BaseChain data) => Mathf.Clamp(data.SliceCount + modifier, minChainCount, maxChainCount);
        if (HoverSelectionMutation.TryApply(
                c.ChainData,
                data => data.SliceCount = GetSliceCount(data),
                out _))
        {
            return;
        }

        ChainCommand.SetSliceCount(c.ChainData, GetSliceCount(c.ChainData));
    }

    public void OnTweakChainSquish(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;
        
        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollChainSquish)
            * scrollPrecisionController.GetCurrentMultiplierPrecision();
        TweakChainSquish(HoveredObject, modifier);
    }

    public void TweakChainSquish(ChainContainer c, float modifier)
    {
        float GetSquish(BaseChain data) => Mathf.Clamp(
            CMMath.RoundToDecimals(data.Squish + modifier),
            minChainSquish,
            maxChainSquish);

        if (HoverSelectionMutation.TryApply(
                c.ChainData,
                data => data.Squish = GetSquish(data),
                out _))
        {
            return;
        }

        ChainCommand.SetSquish(c.ChainData, GetSquish(c.ChainData));
    }
}

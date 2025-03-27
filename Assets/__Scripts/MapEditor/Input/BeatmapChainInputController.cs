using System.Collections;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapChainInputController : BeatmapInputController<ChainContainer>, CMInput.IChainObjectsActions
{
    private const int minChainCount = 1;
    private const int maxChainCount = 999;
    private const float minChainSquish = 0.1f;
    private const float maxChainSquish = 999;
    private const float squishChangeSpeed = 0.1f;

    [FormerlySerializedAs("chainAppearanceSO")][SerializeField] private ChainAppearanceSO chainAppearanceSo;
    public void OnTweakChainCount(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var c);
        if (c == null || c.Dragging || !context.performed) return;

        var modifier = ((context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollChainSegmentCount)
            ? 1
            : -1;
        TweakValue(c, modifier);
    }

    public void TweakValue(ChainContainer c, int modifier)
    {
        var original = BeatmapFactory.Clone(c.ObjectData);
        c.ChainData.SliceCount += modifier;
        c.ChainData.SliceCount = Mathf.Clamp(c.ChainData.SliceCount, minChainCount, maxChainCount);
        if (c.ChainData.CompareTo(original) == 0) return;
        c.GenerateChain();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(c.ObjectData, c.ObjectData, original, mergeType: ActionMergeType.ChainSliceCountTweak));
    }

    public void OnInvertChainColor(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true) ||
            !KeybindsController.IsMouseInWindow || !context.performed)
        {
            return;
        }

        RaycastFirstObject(out var chain);
        if (chain != null && !chain.Dragging) InvertChain(chain);
    }

    public void InvertChain(ChainContainer chain)
    {
        var original = BeatmapFactory.Clone(chain.ObjectData);
        var newType = chain.ChainData.Color == (int)NoteColor.Red
            ? (int)NoteColor.Blue
            : (int)NoteColor.Red;
        chain.ChainData.Color = newType;
        chainAppearanceSo.SetChainAppearance(chain);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(chain.ObjectData, chain.ObjectData, original));
    }

    public void OnTweakChainSquish(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var c);
        if (c == null || c.Dragging || !context.performed) return;

        var modifier = ((context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollChainSquish)
            ? squishChangeSpeed
            : -squishChangeSpeed;
        TweakChainSquish(c, modifier);
    }

    public void TweakChainSquish(ChainContainer c, float modifier)
    {
        var original = BeatmapFactory.Clone(c.ObjectData);
        c.ChainData.Squish += modifier;
        c.ChainData.Squish = Mathf.Clamp(c.ChainData.Squish, minChainSquish, maxChainSquish);
        if (c.ChainData.CompareTo(original) == 0) return;
        c.GenerateChain();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(c.ObjectData, c.ObjectData, original, mergeType: ActionMergeType.ChainSquishTweak));
    }
}

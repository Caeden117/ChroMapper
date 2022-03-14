using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapChainInputController : BeatmapInputController<BeatmapChainContainer>, CMInput.IChainObjectsActions
{
    [FormerlySerializedAs("chainAppearanceSO")] [SerializeField] private ChainAppearanceSO chainAppearanceSo;
    public void OnTweakChainCount(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var c);
        if (c == null || c.Dragging || !context.performed) return;

        var modifier = context.ReadValue<float>() > 0 ? 1 : -1;
        TweakValue(c, modifier);
    }

    public void TweakValue(BeatmapChainContainer c, int modifier)
    {
        var original = BeatmapObject.GenerateCopy(c.ObjectData);
        c.ChainData.Sc += modifier;
        c.ChainData.Sc = Mathf.Clamp(c.ChainData.Sc, BeatmapChain.MinChainCount, BeatmapChain.MaxChainCount);
        c.GenerateChain();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(c.ObjectData, c.ObjectData, original));
    }

    public void OnInvertChainColor(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) ||
            !KeybindsController.IsMouseInWindow || !context.performed)
        {
            return;
        }

        RaycastFirstObject(out var chain);
        if (chain != null && !chain.Dragging) InvertChain(chain);
    }

    public void InvertChain(BeatmapChainContainer chain)
    {
        var original = BeatmapObject.GenerateCopy(chain.ObjectData);
        var newType = chain.ChainData.C == BeatmapNote.NoteTypeA
            ? BeatmapNote.NoteTypeB
            : BeatmapNote.NoteTypeA;
        chain.ChainData.C = newType;
        chainAppearanceSo.SetChainAppearance(chain);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(chain.ObjectData, chain.ObjectData, original));
    }

    public void OnTweakChainSquish(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var c);
        if (c == null || c.Dragging || !context.performed) return;

        var modifier = context.ReadValue<float>() > 0 ? 0.1f : -0.1f;
        TweakChainSquish(c, modifier);
    }

    public void TweakChainSquish(BeatmapChainContainer c, float modifier)
    {
        var original = BeatmapObject.GenerateCopy(c.ObjectData);
        c.ChainData.S += modifier;
        c.ChainData.S = Mathf.Clamp(c.ChainData.S, BeatmapChain.MinChainSquish, BeatmapChain.MaxChainSquish);
        c.GenerateChain();
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(c.ObjectData, c.ObjectData, original));
    }
}

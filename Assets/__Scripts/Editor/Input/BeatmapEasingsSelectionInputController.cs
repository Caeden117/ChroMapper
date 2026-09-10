using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapEasingsSelectionInputController : BeatmapInputController<ObjectContainer>,
                                                      CMInput.IEasingsSelectionActions
{
    [SerializeField] private PlacementModeController placementModeController;

    public event Action<int> OnEasingChanged;
    public event Action<int> OnExtensionChanged;

    private EaseType currentEase;
    private EaseCurve currentCurve;

    private static readonly List<EaseType> easeStandard = new()
    {
        EaseType.InQuadratic,
        EaseType.OutQuadratic,
        EaseType.InOutQuadratic,
        EaseType.InCircular,
        EaseType.OutCircular,
        EaseType.InOutCircular
    };

    private static readonly List<EaseType> easeAlternative = new()
    {
        EaseType.InBounce,
        EaseType.OutBounce,
        EaseType.InOutBounce,
        EaseType.InBack,
        EaseType.OutBack,
        EaseType.InOutBack,
        EaseType.InElastic,
        EaseType.OutElastic,
        EaseType.InOutElastic
    };

    // you're about to witness bizarre
    public void OnEasingCurve(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var ease = currentEase;
            if (ease is EaseType.None or EaseType.Linear) return;

            var curve = GetEaseCurve(ease);
            ease -= (int)curve;
            curve = (EaseCurve)(((int)curve + 1) % 3);
            ease += (int)curve;
            NotifyEasingChanged(ease);
        }
    }

    public void OnEasingCurveHover(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering) return;
        EaseType ease;
        switch (HoveredObject)
        {
            case GLSEventContainer glsEventContainer:
                ease = (EaseType)(glsEventContainer.EventData switch
                {
                    BaseLightColorBase lcb => lcb.Easing,
                    BaseLightRotationBase lrb => lrb.EaseType,
                    BaseLightTranslationBase ltb => ltb.EaseType,
                    BaseFxEventFloat fx => fx.Easing,
                    _ => 0
                });
                break;
            case NJSEventContainer njsEventContainer:
                ease = (EaseType)njsEventContainer.NJSData.Easing;
                break;
            default:
                return;
        }

        if (ease is EaseType.None or EaseType.Linear) return;

        var easeCurve = GetEaseCurve(ease);
        if (easeCurve != currentCurve)
            ease = ease - (int)easeCurve + (int)currentCurve;
        else
        {
            ease -= (int)currentCurve;
            currentCurve = (EaseCurve)(((int)currentCurve + 1) % 3);
            ease += (int)currentCurve;
        }

        switch (HoveredObject)
        {
            case GLSEventContainer glsEventContainer:
                {
                    GLSEventEasingCommand.SetEasing(glsEventContainer.EventData, (int)ease);
                    NotifyEasingChanged(ease);
                    break;
                }
            case NJSEventContainer njsEventContainer:
                {
                    NJSEventSetEase(njsEventContainer, (int)ease);
                    NotifyEasingChanged(ease);
                    break;
                }
        }
    }

    public void OnEasingNone(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyEasingChanged(EaseType.None);
    }

    public void OnEasingNoneHover(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering) return;
        switch (HoveredObject)
        {
            case GLSEventContainer glsEventContainer:
                {
                    GLSEventEasingCommand.SetEasing(glsEventContainer.EventData, (int)EaseType.None);
                    NotifyEasingChanged(EaseType.None);
                    break;
                }
            case NJSEventContainer njsEventContainer:
                {
                    NJSEventSetEase(njsEventContainer, (int)EaseType.None);
                    NotifyEasingChanged(EaseType.None);
                    break;
                }
        }
    }

    public void OnEasingStandard(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var ease = currentEase;

        var easeCurve = (int)(ease is EaseType.Linear or EaseType.None ? currentCurve : GetEaseCurve(ease));
        if (ease == EaseType.Linear)
            ease = easeStandard.Contains(currentEase) ? currentEase : EaseType.InQuadratic + easeCurve;
        else if (ease == EaseType.None)
            ease = easeStandard.Contains(currentEase) ? currentEase : EaseType.Linear;
        else if (!IsSameEaseType(ease, currentEase) && easeStandard.Contains(currentEase))
            ease = currentEase - (int)GetEaseCurve(currentEase) + easeCurve;
        else if (easeStandard.Contains(ease))
        {
            ease -= easeCurve;
            var idx = easeStandard.IndexOf(ease) + 3;
            if (idx >= easeStandard.Count)
                ease = EaseType.Linear;
            else
            {
                ease = easeStandard[idx];
                ease += easeCurve;
            }
        }
        else
            ease = easeStandard[easeCurve];

        NotifyEasingChanged(ease);
    }

    public void OnEasingStandardHover(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering) return;
        EaseType ease;
        switch (HoveredObject)
        {
            case GLSEventContainer glsEventContainer:
                ease = (EaseType)(glsEventContainer.EventData switch
                {
                    BaseLightColorBase lcb => lcb.Easing,
                    BaseLightRotationBase lrb => lrb.EaseType,
                    BaseLightTranslationBase ltb => ltb.EaseType,
                    BaseFxEventFloat fx => fx.Easing,
                    _ => 0
                });
                break;
            case NJSEventContainer njsEventContainer:
                ease = (EaseType)njsEventContainer.NJSData.Easing;
                break;
            default:
                return;
        }

        var easeCurve = (int)(ease is EaseType.Linear or EaseType.None ? currentCurve : GetEaseCurve(ease));
        if (ease == EaseType.Linear)
            ease = easeStandard.Contains(currentEase) ? currentEase : EaseType.InQuadratic + easeCurve;
        else if (ease == EaseType.None)
            ease = easeStandard.Contains(currentEase) ? currentEase : EaseType.Linear;
        else if (!IsSameEaseType(ease, currentEase) && easeStandard.Contains(currentEase))
            ease = currentEase - (int)GetEaseCurve(currentEase) + easeCurve;
        else if (easeStandard.Contains(ease))
        {
            ease -= easeCurve;
            var idx = easeStandard.IndexOf(ease) + 3;
            if (idx >= easeStandard.Count)
                ease = EaseType.Linear;
            else
            {
                ease = easeStandard[idx];
                ease += easeCurve;
            }
        }
        else
            ease = easeStandard[easeCurve];


        switch (HoveredObject)
        {
            case GLSEventContainer glsEventContainer:
                {
                    GLSEventEasingCommand.SetEasing(glsEventContainer.EventData, (int)ease);
                    NotifyEasingChanged(ease);
                    break;
                }
            case NJSEventContainer njsEventContainer:
                {
                    NJSEventSetEase(njsEventContainer, (int)ease);
                    NotifyEasingChanged(ease);
                    break;
                }
        }
    }

    public void OnEasingAlternative(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var ease = easeAlternative.Contains(currentEase)
                ? easeAlternative[(easeAlternative.IndexOf(currentEase) + 3) % easeAlternative.Count]
                : easeAlternative[(int)currentCurve];
            NotifyEasingChanged(ease);
        }
    }

    public void OnEasingAlternativeHover(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering) return;
        EaseType ease;
        switch (HoveredObject)
        {
            case GLSEventContainer glsEventContainer:
                ease = (EaseType)(glsEventContainer.EventData switch
                {
                    BaseLightColorBase lcb => lcb.Easing,
                    BaseLightRotationBase lrb => lrb.EaseType,
                    BaseLightTranslationBase ltb => ltb.EaseType,
                    BaseFxEventFloat fx => fx.Easing,
                    _ => 0
                });
                break;
            default:
                return;
        }

        var easeCurve = (int)GetEaseCurve(ease);
        if (IsSameEaseType(ease, currentEase) && easeAlternative.Contains(ease))
        {
            ease -= easeCurve;
            ease = easeAlternative[(easeAlternative.IndexOf(ease) + 3) % easeAlternative.Count];
            ease += easeCurve;
        }
        else if (!IsSameEaseType(ease, currentEase) && easeAlternative.Contains(currentEase))
            ease = currentEase - (int)GetEaseCurve(currentEase) + easeCurve;
        else
            ease = easeAlternative[(int)currentCurve];


        switch (HoveredObject)
        {
            case GLSEventContainer glsEventContainer:
                {
                    GLSEventEasingCommand.SetEasing(glsEventContainer.EventData, (int)ease);
                    NotifyEasingChanged(ease);
                    break;
                }
        }
    }

    public void NotifyEasingChanged(EaseType value)
    {
        NotifyExtensionChanged(0);
        if (currentEase == value) return;
        currentEase = value;
        if (value is not EaseType.Linear and not EaseType.None) currentCurve = GetEaseCurve(value);
        OnEasingChanged?.Invoke((int)value);
    }

    private int extension;

    // Expose the menu-owned values so editor metadata can restore checkbox state without selecting a map node.
    public int CurrentExtension => extension;
    public int CurrentEasing => (int)currentEase;

    // Restore the input menu and notify its views without resetting extension state as normal input does.
    public void RestoreMenuState(int easing, int restoredExtension)
    {
        currentEase = (EaseType)easing;
        extension = restoredExtension % 2;
        OnEasingChanged?.Invoke(easing);
        OnExtensionChanged?.Invoke(extension);
    }

    public void OnExtension(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Use the same placement-mode controller path as GLS color shortcuts so the Delete picker and tool both update.
            placementModeController.SetMode(PlacementModeController.PlacementMode.Note);
            NotifyExtensionChanged(extension + 1);
        }
    }

    public void OnExtensionHover(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering) return;
        switch (HoveredObject)
        {
            case GLSEventContainer glsEventContainer:
                {
                    var e = glsEventContainer.EventData switch
                    {
                        BaseLightColorBase lcb => lcb.UsePrevious,
                        BaseLightRotationBase lrb => lrb.UsePrevious,
                        BaseLightTranslationBase ltb => ltb.UsePrevious,
                        BaseFxEventFloat fx => fx.UsePrevious,
                        _ => 0
                    };
                    GLSEventEasingCommand.SetExtension(glsEventContainer.EventData, (e + 1) % 2);
                    break;
                }
            case NJSEventContainer njsEventContainer:
                {
                    var e = njsEventContainer.NJSData.UsePrevious;
                    NJSEventSetExtension(njsEventContainer, (e + 1) % 2);
                    break;
                }
            default:
                return;
        }
    }

    private void NJSEventSetEase(NJSEventContainer njsEventContainer, int ease)
    {
        VNJSEventCommand.SetEasing(njsEventContainer, ease);
    }

    private void NJSEventSetExtension(NJSEventContainer njsEventContainer, int ext)
    {
        VNJSEventCommand.SetExtension(njsEventContainer, ext);
    }

    public void NotifyExtensionChanged(int value)
    {
        value %= 2;
        if (extension == value) return;
        extension = value;
        OnExtensionChanged?.Invoke(extension);
    }

    // TODO: these are gigahorrible, but easy way out
    private static bool IsSameEaseType(EaseType a, EaseType b) => a - (int)GetEaseCurve(a) == b - (int)GetEaseCurve(b);

    private static EaseCurve GetEaseCurve(EaseType ease)
    {
        var easeCurve = ease.ToString();
        if (easeCurve.StartsWith("InOut")) return EaseCurve.InOut;
        if (easeCurve.StartsWith("Out")) return EaseCurve.Out;
        return EaseCurve.In;
    }
}

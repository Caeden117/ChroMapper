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
        EaseType.BeatSaberInOutBounce,
        EaseType.InBack,
        EaseType.OutBack,
        EaseType.InOutBack,
        EaseType.BeatSaberInOutBack,
        EaseType.InElastic,
        EaseType.OutElastic,
        EaseType.InOutElastic,
        EaseType.BeatSaberInOutElastic
    };

    public void OnEasingCurve(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var ease = currentEase;
            if (ease is EaseType.None or EaseType.Linear) return;

            var curve = GetNextEaseCurve(ease);
            ease = SetEaseCurve(ease, curve);
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
                ease = (EaseType)(glsEventContainer.EventData?.Easing ?? 0);
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
            ease = SetEaseCurve(ease, currentCurve);
        else
        {
            currentCurve = GetNextEaseCurve(ease);
            ease = SetEaseCurve(ease, currentCurve);
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

        var easeCurve = (int)ToStandardCurve(ease is EaseType.Linear or EaseType.None
            ? currentCurve
            : GetEaseCurve(ease));
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
                ease = (EaseType)(glsEventContainer.EventData?.Easing ?? 0);
                break;
            case NJSEventContainer njsEventContainer:
                ease = (EaseType)njsEventContainer.NJSData.Easing;
                break;
            default:
                return;
        }

        var easeCurve = (int)ToStandardCurve(ease is EaseType.Linear or EaseType.None
            ? currentCurve
            : GetEaseCurve(ease));
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
                ? easeAlternative[(easeAlternative.IndexOf(currentEase) + 4) % easeAlternative.Count]
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
                ease = (EaseType)(glsEventContainer.EventData?.Easing ?? 0);
                break;
            default:
                return;
        }

        var easeCurve = GetEaseCurve(ease);
        if (IsSameEaseType(ease, currentEase) && easeAlternative.Contains(ease))
        {
            ease = easeAlternative[(easeAlternative.IndexOf(ease) + 4) % easeAlternative.Count];
        }
        else if (!IsSameEaseType(ease, currentEase) && easeAlternative.Contains(currentEase))
            ease = SetEaseCurve(currentEase, easeCurve);
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

    // Clearing a placement extension's curve must preserve the Extension selector. Ordinary easing input still selects a normal node.
    public void NotifyEasingChanged(EaseType value, bool clearExtension = true)
    {
        if (clearExtension)
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
                    var e = glsEventContainer.EventData?.UsePrevious ?? 0;
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

    private static bool IsSameEaseType(EaseType a, EaseType b) => GetEaseFamily(a) == GetEaseFamily(b);

    public static EaseCurve GetEaseCurve(EaseType ease)
    {
        return ease switch
        {
            EaseType.OutQuadratic or
            EaseType.OutSinusoidal or
            EaseType.OutCubic or
            EaseType.OutQuartic or
            EaseType.OutQuintic or
            EaseType.OutExponential or
            EaseType.OutCircular or
            EaseType.OutBack or
            EaseType.OutElastic or
            EaseType.OutBounce => EaseCurve.Out,
            EaseType.InOutQuadratic or
            EaseType.InOutSinusoidal or
            EaseType.InOutCubic or
            EaseType.InOutQuartic or
            EaseType.InOutQuintic or
            EaseType.InOutExponential or
            EaseType.InOutCircular or
            EaseType.InOutBack or
            EaseType.InOutElastic or
            EaseType.InOutBounce => EaseCurve.InOut,
            EaseType.BeatSaberInOutBack or
            EaseType.BeatSaberInOutElastic or
            EaseType.BeatSaberInOutBounce => EaseCurve.BeatSaberInOut,
            _ => EaseCurve.In
        };
    }

    public static EaseType SetEaseCurve(EaseType ease, EaseCurve curve)
    {
        var family = GetEaseFamily(ease);
        if (curve == EaseCurve.BeatSaberInOut)
        {
            return family switch
            {
                EaseType.InBack => EaseType.BeatSaberInOutBack,
                EaseType.InElastic => EaseType.BeatSaberInOutElastic,
                EaseType.InBounce => EaseType.BeatSaberInOutBounce,
                _ => family + (int)EaseCurve.InOut
            };
        }

        return family + (int)curve;
    }

    private static EaseCurve GetNextEaseCurve(EaseType ease)
    {
        var curveCount = IsAlternativeFamily(ease) ? 4 : 3;
        return (EaseCurve)(((int)GetEaseCurve(ease) + 1) % curveCount);
    }

    private static EaseCurve ToStandardCurve(EaseCurve curve)
    {
        return curve == EaseCurve.BeatSaberInOut
            ? EaseCurve.InOut
            : curve;
    }

    private static bool IsAlternativeFamily(EaseType ease)
    {
        var family = GetEaseFamily(ease);
        return family is EaseType.InBack or EaseType.InElastic or EaseType.InBounce;
    }

    private static EaseType GetEaseFamily(EaseType ease)
    {
        return ease switch
        {
            EaseType.InOutBack or EaseType.BeatSaberInOutBack => EaseType.InBack,
            EaseType.InOutElastic or EaseType.BeatSaberInOutElastic => EaseType.InElastic,
            EaseType.InOutBounce or EaseType.BeatSaberInOutBounce => EaseType.InBounce,
            _ => ease - (int)GetEaseCurve(ease)
        };
    }
}

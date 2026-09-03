using Beatmap.Base;
using UnityEngine;

public static class GLSEventTranslationCommand
{
    // GLSTranslationYeetTest defines the precision-safe sentinel boundary and reversible offset used by every YEET entry point.
    public const float YeetOffset = 10_000f;
    public const float YeetCutoff = -5_000f;

    // Values at the cutoff are already YEET and must be restored instead of shifted farther negative.
    public static bool IsYeet(float value) => value <= YeetCutoff;

    // ShiftZRoundsYeetAndUnyeetToTwoDecimalPlaces removes float noise after either sentinel offset direction.
    public static BaseLightTranslationBase ToggleYeet(BaseLightTranslationBase evt) => SetValue(
        evt,
        Mathf.Round((evt.Translation + (IsYeet(evt.Translation) ? YeetOffset : -YeetOffset)) * 100f) / 100f);

    public static BaseLightTranslationBase SetValue(BaseLightTranslationBase evt, float value)
    {
        if (Mathf.Approximately(evt.Translation, value)) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Translation = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSTranslationValue);
    }
}

using Beatmap.Base;
using UnityEngine;

public static class GLSEventTranslationCommand
{
    // How much to subtract from a position to YEET it
    public const float YeetOffset = 10_000f;
    // The cutoff point where we display YEET. Not -1000 so that you can YEET things that are like +1000 to start with etc.
    public const float YeetCutoff = -5_000f;

    public static bool IsYeet(float value) => value <= YeetCutoff;

    public static BaseLightTranslationBase ToggleYeet(BaseLightTranslationBase evt) => SetValue(
        evt,
        Mathf.Round((evt.Translation + (IsYeet(evt.Translation) ? YeetOffset : -YeetOffset)) * 100f) / 100f); // Round to prevent float noise because we're outside the range floats can go to 3 decimal places precisely.

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

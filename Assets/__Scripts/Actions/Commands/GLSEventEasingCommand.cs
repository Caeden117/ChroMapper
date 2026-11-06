using Beatmap.Base;

public static class GLSEventEasingCommand
{
    public static BaseGLSEvent SetEasing(BaseGLSEvent evt, int value, bool inPlace = false)
    {
        if (evt == null || evt is BaseFxEventInt || evt.Easing == value)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.Easing = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSEventEasing,
            inPlace);
    }

    public static BaseGLSEvent SetExtension(BaseGLSEvent evt, int value)
    {
        if (evt == null || evt is BaseFxEventInt || evt.UsePrevious == value)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.UsePrevious = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSEventExtension);
    }
}

using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public static class GLSEventRotationCommand
{
    public static BaseLightRotationBase SetValue(
        BaseLightRotationBase evt,
        float value,
        bool inPlace = false)
    {
        if (Mathf.Approximately(evt.Rotation, value))
            return null;

        // Clone through the shared GLS path so rotation mutations retain the source event identity.
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.Rotation = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationValue,
            inPlace);
    }

    public static BaseLightRotationBase SetDirection(
        BaseLightRotationBase evt,
        LightRotationDirection value,
        bool inPlace = false)
    {
        if (evt.Direction == (int)value)
            return null;

        // Clone through the shared GLS path so rotation mutations retain the source event identity.
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.Direction = (int)value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationDirection,
            inPlace);
    }

    public static void SetEaseType(BaseLightRotationBase evt, int value)
    {
        if (evt.Easing == value)
            return;
        // Clone through the shared GLS path so rotation mutations retain the source event identity.
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Easing = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSRotationEaseType);
    }

    public static BaseLightRotationBase SetLoop(
        BaseLightRotationBase evt,
        int value,
        bool inPlace = false)
    {
        if (evt.Loop == value)
            return null;

        // Clone through the shared GLS path so rotation mutations retain the source event identity.
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.Loop = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationLoop,
            inPlace);
    }

}

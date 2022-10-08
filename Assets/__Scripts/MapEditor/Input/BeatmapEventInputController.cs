using System.Linq;
using Beatmap.Appearances;
using Beatmap.Containers;
using Beatmap.Helper;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapEventInputController : BeatmapInputController<EventContainer>, CMInput.IEventObjectsActions
{
    [FormerlySerializedAs("eventAppearanceSO")] [SerializeField] private EventAppearanceSO eventAppearanceSo;
    [SerializeField] private TracksManager tracksManager;

    public void OnInvertEventValue(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) ||
            !KeybindsController.IsMouseInWindow || !context.performed)
        {
            return;
        }

        RaycastFirstObject(out var e);
        if (e != null && !e.Dragging) InvertEvent(e);
    }

    public void OnTweakEventValue(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragging || !context.performed) return;

        var modifier = context.ReadValue<float>() > 0 ? 1 : -1;
        TweakValue(e, modifier);
    }

    public void OnTweakEventFloatValue(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragging || !context.performed) return;

        var modifier = context.ReadValue<float>() > 0 ? 1 : -1;
        TweakFloatValue(e, modifier);
    }

    public void InvertEvent(EventContainer e)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);
        if (e.EventData.IsLaneRotationEvent())
        {
            var rotation = e.EventData.GetRotationDegreeFromValue();
            if (rotation != null)
            {
                if (e.EventData is BaseRotationEvent)
                {
                    // (e.EventData as IRotationEvent).Rotation *= -1;
                }
                else
                {
                    if (e.EventData.Value >= 0 && e.EventData.Value < BaseEvent.LightValueToRotationDegrees.Length)
                    {
                        e.EventData.Value =
                            BaseEvent.LightValueToRotationDegrees.ToList().IndexOf((rotation ?? 0) * -1);
                    }
                    else if (e.EventData.Value >= 1000 && e.EventData.Value <= 1720) //Invert Mapping Extensions rotation
                    {
                        e.EventData.Value = 1720 - (e.EventData.Value - 1000);
                    }
                }

                tracksManager.RefreshTracks();
            }
        }
        else if (e.EventData.IsColorBoostEvent())
        {
            e.EventData.Value = e.EventData.Value > 0 ? 0 : 1;
        }
        else if (!e.EventData.IsLightEvent(EnvironmentInfoHelper.GetName()))
        {
            return;
        }
        else
        {
            if (e.EventData.Value > 4 && e.EventData.Value <= 8) e.EventData.Value -= 4;
            else if (e.EventData.Value > 0 && e.EventData.Value <= 4) e.EventData.Value += 4;
            else if (e.EventData.Value > 8 && e.EventData.Value <= 12) e.EventData.Value -= 4; // white to red
        }

        eventAppearanceSo.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }

    protected override bool GetComponentFromTransform(GameObject t, out EventContainer obj) =>
        t.transform.parent.TryGetComponent(out obj);

    public void TweakValue(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);

        if (e.EventData is BaseRotationEvent)
        {
            // (e.EventData as IRotationEvent).Rotation += modifier;
        }
        else
        {
            e.EventData.Value += modifier;

            if (e.EventData.Value == 4 && e.EventData.IsLightEvent(EnvironmentInfoHelper.GetName()))
                e.EventData.Value += modifier;

            if (e.EventData.Value < 0) e.EventData.Value = 0;

            if (!e.EventData.IsLaserRotationEvent(EnvironmentInfoHelper.GetName()))
            {
                if (e.EventData.Value > 7)
                    e.EventData.Value = 7;
            }
        }

        if (e.EventData.IsLaneRotationEvent())
            tracksManager.RefreshTracks();
        eventAppearanceSo.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }

    public void TweakFloatValue(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);

        e.EventData.FloatValue += 0.1f * modifier;
        if (e.EventData.FloatValue < 0) e.EventData.FloatValue = 0;

        eventAppearanceSo.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }
}

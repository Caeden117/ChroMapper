using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapEventInputController : BeatmapInputController<BeatmapEventContainer>, CMInput.IEventObjectsActions
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
        if (e != null && !e.Dragging)
        {
            switch (e.ObjectData.BeatmapType)
            {
                case BeatmapObject.ObjectType.LightColorEvent:
                case BeatmapObject.ObjectType.LightRotationEvent:
                case BeatmapObject.ObjectType.LightTranslationEvent:
                    var i = e as IEventV3Action;
                    i.InvertEvent();
                    break;
                default:
                    InvertEvent(e);
                    break;
            }
        }
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

    public void InvertEvent(BeatmapEventContainer e)
    {
        var original = BeatmapObject.GenerateCopy(e.ObjectData);
        if (e.EventData.IsRotationEvent)
        {
            var rotation = e.EventData.GetRotationDegreeFromValue();
            if (rotation != null)
            {
                if (e.EventData is RotationEvent)
                {
                    (e.EventData as RotationEvent).RotationAmount *= -1;
                }
                else
                {
                    if (e.EventData.Value >= 0 && e.EventData.Value < MapEvent.LightValueToRotationDegrees.Length)
                    {
                        e.EventData.Value =
                            MapEvent.LightValueToRotationDegrees.ToList().IndexOf((rotation ?? 0) * -1);
                    }
                    else if (e.EventData.Value >= 1000 && e.EventData.Value <= 1720) //Invert Mapping Extensions rotation
                    {
                        e.EventData.Value = 1720 - (e.EventData.Value - 1000);
                    }
                }

                tracksManager.RefreshTracks();
            }
        }
        else if (e.EventData.Type == MapEvent.EventTypeBoostLights)
        {
            e.EventData.Value = e.EventData.Value > 0 ? 0 : 1;
        }
        else if (e.EventData.IsUtilityEvent)
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

    protected override bool GetComponentFromTransform(GameObject t, out BeatmapEventContainer obj) =>
        t.transform.parent.TryGetComponent(out obj);

    public void TweakValue(BeatmapEventContainer e, int modifier)
    {
        var original = BeatmapObject.GenerateCopy(e.ObjectData);

        if (e.EventData is RotationEvent)
        {
            (e.EventData as RotationEvent).RotationAmount += modifier;
        }
        else
        {
            e.EventData.Value += modifier;

            if (e.EventData.Value == 4 && !e.EventData.IsUtilityEvent)
                e.EventData.Value += modifier;

            if (e.EventData.Value < 0) e.EventData.Value = 0;

            if (!e.EventData.IsLaserSpeedEvent)
            {
                if (e.EventData.Value > 7)
                    e.EventData.Value = 7;
            }
        }

        if (e.EventData.IsRotationEvent)
            tracksManager.RefreshTracks();
        eventAppearanceSo.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }

    public void TweakFloatValue(BeatmapEventContainer e, int modifier)
    {
        var original = BeatmapObject.GenerateCopy(e.ObjectData);

        if (!e.EventData.IsUtilityEvent)
        {
            e.EventData.FloatValue += 0.1f * modifier;
            if (e.EventData.FloatValue < 0) e.EventData.FloatValue = 0;
        }

        eventAppearanceSo.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }
}

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapEventInputController : BeatmapInputController<BeatmapEventContainer>, CMInput.IEventObjectsActions
{
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private TracksManager tracksManager;

    public void OnInvertEventValue(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) || !KeybindsController.IsMouseInWindow || !context.performed) return;
        RaycastFirstObject(out BeatmapEventContainer e);
        if (e != null)
        {
            InvertEvent(e);
        }
    }

    public void InvertEvent(BeatmapEventContainer e)
    {
        BeatmapObject original = BeatmapObject.GenerateCopy(e.objectData);
        if (e.eventData.IsRotationEvent)
        {
            int? rotation = e.eventData.GetRotationDegreeFromValue();
            if (rotation != null)
            {
                if (e.eventData._value >= 0 && e.eventData._value < MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.Length)
                    e.eventData._value = MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf((rotation ?? 0) * -1);
                else if (e.eventData._value >= 1000 && e.eventData._value <= 1720) //Invert Mapping Extensions rotation
                    e.eventData._value = 1720 - (e.eventData._value - 1000);

                tracksManager?.RefreshTracks();
            }
        }
        else if (e.eventData.IsUtilityEvent)
        {
            return;
        }
        else
        {
            if (e.eventData._value > 4 && e.eventData._value < 8) e.eventData._value -= 4;
            else if (e.eventData._value > 0 && e.eventData._value <= 4) e.eventData._value += 4;
        }

        eventAppearanceSO?.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.objectData, e.objectData, original));
    }

    protected override bool GetComponentFromTransform(Transform t, out BeatmapEventContainer obj)
    {
        return t.parent.TryGetComponent(out obj);
    }

    public void OnTweakEventValue(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out BeatmapEventContainer e);
        if (e == null || !context.performed) return;

        var modifier = context.ReadValue<float>() > 0 ? 1 : -1;
        TweakValue(e, modifier);
    }

    public void TweakValue(BeatmapEventContainer e, int modifier)
    {
        BeatmapObject original = BeatmapObject.GenerateCopy(e.objectData);
        e.eventData._value += modifier;

        if (e.eventData._value == 4 && !e.eventData.IsUtilityEvent)
            e.eventData._value += modifier;

        if (e.eventData._value < 0) e.eventData._value = 0;

        if (!e.eventData.IsLaserSpeedEvent)
        {
            if (e.eventData._value > 7) e.eventData._value = 7;
        }

        if (e.eventData.IsRotationEvent)
            tracksManager?.RefreshTracks();
        eventAppearanceSO.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.objectData, e.objectData, original));
    }
}

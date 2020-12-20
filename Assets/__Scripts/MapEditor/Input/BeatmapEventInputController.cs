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
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out BeatmapEventContainer e);
        if (e != null && context.performed)
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
                }
                eventAppearanceSO?.SetEventAppearance(e);
                tracksManager?.RefreshTracks();
                return;
            }
            if (e.eventData.IsUtilityEvent) return;
            if (e.eventData._value > 4 && e.eventData._value < 8) e.eventData._value -= 4;
            else if (e.eventData._value > 0 && e.eventData._value <= 4) e.eventData._value += 4;
            eventAppearanceSO?.SetEventAppearance(e);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(e.objectData), original));
        }
    }

    protected override bool GetComponentFromTransform(Transform t, out BeatmapEventContainer obj)
    {
        return t.parent.TryGetComponent(out obj);
    }

    public void OnTweakEventValue(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out BeatmapEventContainer e);
        if (e != null && context.performed)
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(e.objectData);
            int modifier = context.ReadValue<float>() > 0 ? 1 : -1;
            e.eventData._value += modifier;

            if (e.eventData._value == 4 && (!e.eventData.IsUtilityEvent || e.eventData.IsLaserSpeedEvent))
                e.eventData._value += modifier;

            if (e.eventData._value < 0) e.eventData._value = 0;

            if (!e.eventData.IsLaserSpeedEvent)
            {
                if (e.eventData._value > 7) e.eventData._value = 7;
            }

            if (e.eventData.IsRotationEvent)
                tracksManager?.RefreshTracks();
            eventAppearanceSO.SetEventAppearance(e);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(e.objectData), original));
        }
    }
}

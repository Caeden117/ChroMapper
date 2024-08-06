using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapEventInputController : BeatmapInputController<EventContainer>, CMInput.IEventObjectsActions
{
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    [SerializeField] private TracksManager tracksManager;

    public void OnInvertEventValue(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true) ||
            !KeybindsController.IsMouseInWindow || !context.performed)
        {
            return;
        }

        RaycastFirstObject(out var e);
        if (e != null && !e.Dragging) InvertEvent(e);
    }

    public void OnTweakEventMain(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragging || !context.performed) return;

        var modifier = ((context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollEventValue)
            ? 1
            : -1;
        TweakMain(e, modifier);
    }

    public void OnTweakEventAlternative(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragging || !context.performed) return;

        var modifier = ((context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollEventValue)
            ? 1
            : -1;
        TweakAlternative(e, modifier);
    }

    public void InvertEvent(EventContainer e)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);
        if (e.EventData.IsLaneRotationEvent())
        {
            var rotation = e.EventData.GetRotationDegreeFromValue();
            if (rotation != null)
            {
                if (Settings.Instance.MapVersion >= 3)
                {
                    e.EventData.FloatValue *= -1;
                }
                else
                {
                    if (e.EventData.Value >= 0 && e.EventData.Value < BaseEvent.LightValueToRotationDegrees.Length)
                    {
                        e.EventData.Value =
                            BaseEvent.LightValueToRotationDegrees.ToList().IndexOf((int)rotation * -1);
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
        else if (!e.EventData.IsLightEvent())
        {
            return;
        }
        else
        {
            if (e.EventData.Value > 0 && e.EventData.Value <= 4) e.EventData.Value += 4; // blue to red
            else if (e.EventData.Value > 4 && e.EventData.Value <= 8) e.EventData.Value += 4; // red to white
            else if (e.EventData.Value > 8 && e.EventData.Value <= 12) e.EventData.Value -= 8; // white to blue

            RefreshPrevEventContainer(e);
        }

        eventAppearanceSo.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }

    protected override bool GetComponentFromTransform(GameObject t, out EventContainer obj) =>
        t.transform.parent.TryGetComponent(out obj);

    // for event that frequently gets changed
    public void TweakMain(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);

        if (e.EventData.IsLightEvent())
        {
            e.EventData.FloatValue += 0.1f * modifier;
            if (e.EventData.FloatValue < 0) e.EventData.FloatValue = 0;

            RefreshPrevEventContainer(e);
        }
        else if (e.EventData.IsLaneRotationEvent())
        {
            if (Settings.Instance.MapVersion >= 3)
            {
                e.EventData.FloatValue += 15 * modifier;
            }
            else
            {
                if (e.EventData.Value < 8 || e.EventData.Value >= 1000)
                {
                    e.EventData.Value += modifier;
                    if (e.EventData.Value == 8) e.EventData.Value = 7;
                }

                if (e.EventData.Value < 0) e.EventData.Value = 0;
                if (e.EventData.Value > 1720) e.EventData.Value -= 720;
            }

            tracksManager.RefreshTracks();
        }
        else if (e.EventData.IsColorBoostEvent())
        {
            e.EventData.Value = e.EventData.Value == 0 ? 1 : 0;
        }
        else if (e.EventData.IsBpmEvent())
        {
            e.EventData.FloatValue += modifier;
            if (e.EventData.FloatValue < 1) e.EventData.FloatValue = 1;
        }
        else
        {
            e.EventData.Value += modifier;
            if (e.EventData.Value < 0) e.EventData.Value = 0;
        }

        eventAppearanceSo.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }

    // for event that occasionally gets changed
    public void TweakAlternative(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);

        if (e.EventData.IsLightEvent())
        {
            e.EventData.Value += modifier;

            if (e.EventData.Value < 0) e.EventData.Value = 0;
            if (e.EventData.Value > 12 && e.EventData.IsLightEvent()) e.EventData.Value = 12;

            RefreshPrevEventContainer(e);
        }
        else if (e.EventData.IsLaneRotationEvent())
        {
            if (Settings.Instance.MapVersion >= 3)
                e.EventData.FloatValue += modifier;
        }

        eventAppearanceSo.SetEventAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }

    private void RefreshPrevEventContainer(EventContainer e)
    {
        var prevEvent = e.EventData.Prev;
        if (prevEvent != null)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (collection.LoadedContainers.TryGetValue(prevEvent, out var container))
                (container as EventContainer).RefreshAppearance();
        }
    }
}

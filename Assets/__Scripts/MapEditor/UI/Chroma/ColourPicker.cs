using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColourPicker : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private EventsContainer eventsContainer;
    [SerializeField] private Toggle toggle;

    // Start is called before the first frame update
    private void Start()
    {
        toggle.isOn = Settings.Instance.PickColorFromChromaEvents;
        SelectionController.ObjectWasSelectedEvent += SelectedObject;
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= SelectedObject;
    }

    public void UpdateColourPicker(bool enabled)
    {
        Settings.Instance.PickColorFromChromaEvents = enabled;
    }

    private void SelectedObject(BeatmapObjectContainer obj)
    {
        if (!(obj is BeatmapEventContainer e) || !dropdown.Visible || !Settings.Instance.PickColorFromChromaEvents) return;
        if (e.eventData._value >= ColourManager.RGB_INT_OFFSET)
            picker.CurrentColor = ColourManager.ColourFromInt(e.eventData._value);
        else
        {
            BeatmapEventContainer attached = FindAttachedChromaEvent(e);
            if (attached) picker.CurrentColor = ColourManager.ColourFromInt(attached.eventData._value);
        }
    }

    private BeatmapEventContainer FindAttachedChromaEvent(BeatmapEventContainer container)
    {
        return eventsContainer.LoadedContainers.Where((BeatmapObjectContainer x) =>
        (x.objectData as MapEvent)._type == container.eventData._type && //Ensure same type
        !(x.objectData as MapEvent).IsUtilityEvent() && //And that they are not utility
        x.objectData._time >= container.eventData._time - (1f / 4f) && //They are close enough behind said container
        x.objectData._time <= container.eventData._time && //dont forget to make sure they're actually BEHIND a container.
        (x.objectData as MapEvent)._value >= ColourManager.RGB_INT_OFFSET //And they be a Chroma event.
        ).FirstOrDefault() as BeatmapEventContainer;
    }
}

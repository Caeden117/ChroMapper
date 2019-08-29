using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColourPicker : MonoBehaviour
{
    [SerializeField] private ColourSelector selector;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private EventsContainer eventsContainer;

    public static bool IsActive { get; private set; } = false;

    // Start is called before the first frame update
    private void Start()
    {
        SelectionController.ObjectWasSelectedEvent += SelectedObject;
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= SelectedObject;
    }

    public void UpdateColourPicker(bool enabled)
    {
        IsActive = enabled;
    }

    private void SelectedObject(BeatmapObjectContainer obj)
    {
        if (!(obj is BeatmapEventContainer e) || dropdown.YDestination != -75f || !IsActive) return;
        if (e.eventData._value >= ColourManager.RGB_INT_OFFSET)
            selector.UpdateColour(ColourManager.ColourFromInt(e.eventData._value));
        else
        {
            BeatmapEventContainer attached = FindAttachedChromaEvent(e);
            if (attached) selector.UpdateColour(ColourManager.ColourFromInt(attached.eventData._value));
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

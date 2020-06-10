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

    private void SelectedObject(BeatmapObject obj)
    {
        if (!Settings.Instance.PickColorFromChromaEvents || !dropdown.Visible) return;
        if (obj._customData?.HasKey("_color") ?? false)
        {
            picker.CurrentColor = obj._customData["_color"];
        }
        if (!(obj is MapEvent e)) return;
        if (e._value >= ColourManager.RGB_INT_OFFSET)
        {
            picker.CurrentColor = ColourManager.ColourFromInt(e._value);
        }
    }
}

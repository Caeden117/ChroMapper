using UnityEngine.UI;
using UnityEngine;

public class ColourPicker : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private EventsContainer eventsContainer;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Toggle placeChromaToggle;

    // Start is called before the first frame update
    private void Start()
    {
        SelectionController.ObjectWasSelectedEvent += SelectedObject;
        toggle.isOn = Settings.Instance.PickColorFromChromaEvents;
        placeChromaToggle.isOn = Settings.Instance.PlaceChromaColor;
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
        else if (e._lightGradient != null)
        {
            picker.CurrentColor = e._lightGradient.StartColor;
        }
    }
}

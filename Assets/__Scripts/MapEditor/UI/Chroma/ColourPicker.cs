using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ColourPicker : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;
    [SerializeField] private ToggleColourDropdown dropdown;
    [FormerlySerializedAs("eventsContainer")] [SerializeField] private EventGridContainer eventGridContainer;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Toggle placeChromaToggle;

    // Start is called before the first frame update
    private void Start()
    {
        SelectionController.ObjectWasSelectedEvent += SelectedObject;
        toggle.isOn = Settings.Instance.PickColorFromChromaEvents;
        placeChromaToggle.isOn = Settings.Instance.PlaceChromaColor;
    }

    private void OnDestroy() => SelectionController.ObjectWasSelectedEvent -= SelectedObject;

    public void UpdateColourPicker(bool enabled) => Settings.Instance.PickColorFromChromaEvents = enabled;

    private void SelectedObject(IObject obj)
    {
        if (!Settings.Instance.PickColorFromChromaEvents || !dropdown.Visible) return;
        if (obj.CustomColor != null) picker.CurrentColor = (Color)obj.CustomColor;
        if (!(obj is IEvent e)) return;
        if (e.Value >= ColourManager.RgbintOffset)
            picker.CurrentColor = ColourManager.ColourFromInt(e.Value);
        else if (e.CustomLightGradient != null) picker.CurrentColor = e.CustomLightGradient.StartColor;
    }
}

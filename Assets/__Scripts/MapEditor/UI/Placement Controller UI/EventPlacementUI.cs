using UnityEngine.UI;
using UnityEngine;

public class EventPlacementUI : MonoBehaviour
{
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [SerializeField] private Toggle redColorToggle;
    [SerializeField] private Toggle blueColorToggle;
    [SerializeField] private Toggle offValueToggle;
    [SerializeField] private Toggle onValueToggle;
    [SerializeField] private Toggle fadeValueToggle;
    [SerializeField] private Toggle flashValueToggle;
    private bool red = true;

    public void Off()
    {
        UpdateValue(MapEvent.LIGHT_VALUE_OFF);
    }

    public void On()
    {
        UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_ON : MapEvent.LIGHT_VALUE_BLUE_ON);
    }

    public void Fade()
    {
        UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_FADE : MapEvent.LIGHT_VALUE_BLUE_FADE);
    }

    public void Flash()
    {
        UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_FLASH : MapEvent.LIGHT_VALUE_BLUE_FLASH);
    }

    public void Red()
    {
        red = true;
        eventPlacement.SwapColors(true);
    }

    public void Blue()
    {
        red = false;
        eventPlacement.SwapColors(false);
    }

    public void UpdateUI(MapEvent previewEvent)
    {
        redColorToggle.isOn = IsRedNote();
        blueColorToggle.isOn = !IsRedNote();
        offValueToggle.isOn = previewEvent._value == MapEvent.LIGHT_VALUE_OFF;
        onValueToggle.isOn = previewEvent._value == MapEvent.LIGHT_VALUE_BLUE_ON || previewEvent._value == MapEvent.LIGHT_VALUE_RED_ON;
        fadeValueToggle.isOn = previewEvent._value == MapEvent.LIGHT_VALUE_BLUE_FADE || previewEvent._value == MapEvent.LIGHT_VALUE_RED_FADE;
        flashValueToggle.isOn = previewEvent._value == MapEvent.LIGHT_VALUE_BLUE_FLASH || previewEvent._value == MapEvent.LIGHT_VALUE_RED_FLASH;
    }

    private void UpdateValue(int value)
    {
        if (!customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        eventPlacement.UpdateValue(value);
    }

    private bool IsRedNote()
    {
        switch (eventPlacement.queuedData._value)
        {
            case MapEvent.LIGHT_VALUE_OFF: return eventPlacement.PlaceRedNote;
            case MapEvent.LIGHT_VALUE_RED_ON: return true;
            case MapEvent.LIGHT_VALUE_RED_FLASH: return true;
            case MapEvent.LIGHT_VALUE_RED_FADE: return true;
            default: return false;
        }
    }
}

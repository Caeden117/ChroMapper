using UnityEngine.UI;
using UnityEngine;

public class EventPlacementUI : MonoBehaviour
{
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [SerializeField] private KeybindsController keybindsController;
    [SerializeField] private Toggle redColorToggle; //todo: are these vars useless??
    [SerializeField] private Toggle blueColorToggle;
    [SerializeField] private Toggle offValueToggle;
    [SerializeField] private Toggle onValueToggle;
    [SerializeField] private Toggle fadeValueToggle;
    [SerializeField] private Toggle flashValueToggle;
    [SerializeField] private Toggle deleteToggle;
    [SerializeField] private Toggle redNoteToggle;
    [SerializeField] private Toggle dummyToggle;
    [SerializeField] private Toggle placeChromaToggle;
    private bool red = true;

    public void Off(bool active)
    {
        if (!active) return;
        UpdateValue(MapEvent.LIGHT_VALUE_OFF);
        UpdateUI(false);
    }

    public void On(bool active)
    {
        if (!active) return;
        UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_ON : MapEvent.LIGHT_VALUE_BLUE_ON);
        UpdateUI(false);
    }

    public void Fade(bool active)
    {
        if (!active) return;
        UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_FADE : MapEvent.LIGHT_VALUE_BLUE_FADE);
        UpdateUI(false);
    }

    public void Flash(bool active)
    {
        if (!active) return;
        UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_FLASH : MapEvent.LIGHT_VALUE_BLUE_FLASH);
        UpdateUI(false);
    }

    public void Red(bool active)
    {
        if (!active) return;
        red = true;
        UpdateUI(false, true);
        eventPlacement.SwapColors(true);
    }

    public void Blue(bool active)
    {
        if (!active) return;
        red = false;
        UpdateUI(false, true);
        eventPlacement.SwapColors(false);
    }

    public void Delete(bool active)
    {
        if (!active) return;
        UpdateUI(true);
    }

    public void UpdateUI(bool del, bool on = false) // delete toggle isnt in event toggle group, so lets fake it
    {
        placeChromaToggle.isOn = false;
        if (del)
        {
            eventPlacement.IsActive = false;
            dummyToggle.isOn = true;
        }
        else
        {
            if (NotePlacementUI.delete)
            {
                eventPlacement.IsActive = true;
                keybindsController.wasdCase(); //wtf am i doing; this is for clicking the button
                NotePlacementUI.delete = false;
                if (on) onValueToggle.isOn = true;
            }
        }
    }

    private void UpdateValue(int value)
    {
        //if (!customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return; // again idk what this is
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

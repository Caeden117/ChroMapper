using UnityEngine.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class EventPlacementUI : MonoBehaviour, CMInput.IEventUIActions
{
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private Toggle redColorToggle;
    [SerializeField] private Toggle blueColorToggle;
    [SerializeField] private Toggle offValueToggle;
    [SerializeField] private Toggle onValueToggle;
    [SerializeField] private Toggle fadeValueToggle;
    [SerializeField] private Toggle flashValueToggle;
    [SerializeField] private Toggle dummyToggle;
    [SerializeField] private Toggle placeChromaToggle;
    [SerializeField] private CanvasGroup precisionRotationCanvasGroup;
    private bool red = true;
    private bool wasChromaOn = false;

    public bool IsTypingRotation { get; private set; } = false;

    public void Off(bool active)
    {
        if (!active) return;
        wasChromaOn = placeChromaToggle.isOn;
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
                //keybindsController.wasdCase(); //wtf am i doing; this is for clicking the button
                NotePlacementUI.delete = false;
                if (on) onValueToggle.isOn = true;
            }
            if (eventPlacement.queuedData._value != MapEvent.LIGHT_VALUE_OFF)
            {
                placeChromaToggle.isOn = wasChromaOn;
            }
        }
    }

    private void UpdateValue(int value)
    {
        //if (!customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return; // again idk what this is
        if (!eventPlacement.queuedData.IsUtilityEvent)
        {
            eventPlacement.UpdateValue(value);
        }
    }

    public void UpdatePrecisionRotationValue()
    {
        bool enabled = precisionRotationCanvasGroup.alpha == 0;
        precisionRotationCanvasGroup.alpha = enabled ? 1 : 0;
        eventPlacement.PlacePrecisionRotation = enabled;
    }

    public void UpdatePrecisionRotation(string res)
    {
        if (int.TryParse(res, out int value))
        {
            eventPlacement.PrecisionRotationValue = value;
            IsTypingRotation = false;
        }
    }

    public void UpdateTyping(string res)
    {
        IsTypingRotation = true;
    }

    public void OnTypeOn(InputAction.CallbackContext context)
    {
        onValueToggle.isOn = true;
    }

    public void OnTypeFlash(InputAction.CallbackContext context)
    {
        flashValueToggle.isOn = true;
    }

    public void OnTypeOff(InputAction.CallbackContext context)
    {
        offValueToggle.isOn = true;
    }

    public void OnTypeFade(InputAction.CallbackContext context)
    {
        fadeValueToggle.isOn = true;
    }

    public void OnTogglePrecisionRotation(InputAction.CallbackContext context)
    {
        UpdatePrecisionRotationValue();
    }
}

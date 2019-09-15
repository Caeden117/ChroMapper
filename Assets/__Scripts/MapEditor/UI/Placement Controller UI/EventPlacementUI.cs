using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventPlacementUI : MonoBehaviour
{
    [SerializeField] private EventPlacement eventPlacement;
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

    private void UpdateValue(int value)
    {
        eventPlacement.UpdateValue(value);
    }
}

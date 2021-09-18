using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class LightingModeController : MonoBehaviour
{
    public enum LightingMode
    {
        [PickerChoice("Mapper", "bar.events.on")]
        ON,
        [PickerChoice("Mapper", "bar.events.off")]
        OFF,
        [PickerChoice("Mapper", "bar.events.flash")]
        FLASH,
        [PickerChoice("Mapper", "bar.events.fade")]
        FADE
    }
    [SerializeField] private EnumPicker lightingPicker;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private MaskableGraphic modeLock;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;
    private bool modeLocked;
    private LightingMode currentMode;

    void Start()
    {
        lightingPicker.Initialize(typeof(LightingMode));
        SetLocked(false);
        lightingPicker.OnClick += UpdateMode;
    }

    public void SetMode(Enum lightingMode)
    {
        if (modeLocked)
            return;
        lightingPicker.Select(lightingMode);
        UpdateMode(lightingMode);
    }

    public void SetLocked(bool locked)
    {
        modeLocked = locked;
        lightingPicker.Locked = modeLocked;

        if (modeLock is Image img)
        {
            img.sprite = modeLocked ? lockedSprite : unlockedSprite;
        }
        else if (modeLock is SVGImage svg)
        {
            svg.sprite = modeLocked ? lockedSprite : unlockedSprite;
        }
    }

    public void ToggleLock() => SetLocked(!modeLocked);

    public void UpdateValue()
    {
        bool red = notePlacement.queuedData._type == BeatmapNote.NOTE_TYPE_A;
        switch (currentMode)
        {
            case LightingMode.OFF:
                eventPlacement.UpdateValue(MapEvent.LIGHT_VALUE_OFF);
                break;
            case LightingMode.ON:
                eventPlacement.UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_ON : MapEvent.LIGHT_VALUE_BLUE_ON);
                break;
            case LightingMode.FLASH:
                eventPlacement.UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_FLASH : MapEvent.LIGHT_VALUE_BLUE_FLASH);
                break;
            case LightingMode.FADE:
                eventPlacement.UpdateValue(red ? MapEvent.LIGHT_VALUE_RED_FADE : MapEvent.LIGHT_VALUE_BLUE_FADE);
                break;
        }
    }

    private void UpdateMode(Enum lightingMode)
    {
        currentMode = (LightingMode)lightingMode;
        UpdateValue();
    }
}

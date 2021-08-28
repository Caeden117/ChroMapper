using System;
using UnityEngine;
using UnityEngine.UI;
using static EnumPicker;

public class LightingModeController : MonoBehaviour
{
    public enum LightingMode
    {
        [PickerChoice("Mapper", "bar.events.on")]
        ON,

        [PickerChoice("Mapper", "bar.events.off")]
        Off,

        [PickerChoice("Mapper", "bar.events.flash")]
        Flash,

        [PickerChoice("Mapper", "bar.events.fade")]
        Fade
    }

    [SerializeField] private EnumPicker lightingPicker;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private Image modeLock;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;
    private LightingMode currentMode;
    private bool modeLocked;

    private void Start()
    {
        lightingPicker.Initialize(typeof(LightingMode));
        SetLocked(false);
        lightingPicker.Click += UpdateMode;
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
        modeLock.sprite = modeLocked ? lockedSprite : unlockedSprite;
    }

    public void ToggleLock() => SetLocked(!modeLocked);

    public void UpdateValue()
    {
        var red = notePlacement.queuedData.Type == BeatmapNote.NoteTypeA;
        switch (currentMode)
        {
            case LightingMode.Off:
                eventPlacement.UpdateValue(MapEvent.LightValueOff);
                break;
            case LightingMode.ON:
                eventPlacement.UpdateValue(red ? MapEvent.LightValueRedON : MapEvent.LightValueBlueON);
                break;
            case LightingMode.Flash:
                eventPlacement.UpdateValue(red ? MapEvent.LightValueRedFlash : MapEvent.LightValueBlueFlash);
                break;
            case LightingMode.Fade:
                eventPlacement.UpdateValue(red ? MapEvent.LightValueRedFade : MapEvent.LightValueBlueFade);
                break;
        }
    }

    private void UpdateMode(Enum lightingMode)
    {
        currentMode = (LightingMode)lightingMode;
        UpdateValue();
    }
}

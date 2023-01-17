using System;
using UnityEngine;
using UnityEngine.UI;

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
        Fade,

        [PickerChoice("Mapper", "bar.events.transition")]
        Transition
    }

    [SerializeField] private EnumPicker lightingPicker;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private MaskableGraphic modeLock;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;
    private LightingMode currentMode;
    private bool modeLocked;

    private void Start()
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
        var red = notePlacement.queuedData.Type == BeatmapNote.NoteTypeA;
        var white = notePlacement.queuedData.Type == BeatmapNote.NoteTypeBomb;
        switch (currentMode)
        {
            case LightingMode.Off:
                eventPlacement.UpdateValue(MapEvent.LightValueOff);
                break;
            case LightingMode.ON:
                eventPlacement.UpdateValue(red ? MapEvent.LightValueRedON : white ? MapEventV3.LightValueWhiteON : MapEvent.LightValueBlueON);
                break;
            case LightingMode.Flash:
                eventPlacement.UpdateValue(red ? MapEvent.LightValueRedFlash : white ? MapEventV3.LightValueWhiteFlash : MapEvent.LightValueBlueFlash);
                break;
            case LightingMode.Fade:
                eventPlacement.UpdateValue(red ? MapEvent.LightValueRedFade : white ? MapEventV3.LightValueWhiteFade : MapEvent.LightValueBlueFade);
                break;
            case LightingMode.Transition:
                eventPlacement.UpdateValue(red ? MapEventV3.LightValueRedTransition : white ? MapEventV3.LightValueWhiteTransition : MapEventV3.LightValueBlueTransition);
                break;
        }
    }

    private void UpdateMode(Enum lightingMode)
    {
        currentMode = (LightingMode)lightingMode;
        UpdateValue();
    }
}

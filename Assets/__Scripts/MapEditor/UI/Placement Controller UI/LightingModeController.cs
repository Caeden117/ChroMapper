using System;
using Beatmap.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LightingModeController : MonoBehaviour
{
    public enum LightingMode
    {
        [PickerChoice("Mapper", "bar.events.on")]
        On,

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
        var red = notePlacement.queuedData.Type == (int)NoteType.Red;
        var white = notePlacement.queuedData.Type == (int)NoteType.Bomb;
        switch (currentMode)
        {
            case LightingMode.Off:
                eventPlacement.UpdateValue((int)LightValue.Off);
                break;
            case LightingMode.On:
                eventPlacement.UpdateValue(red ? (int)LightValue.RedOn : white ? (int)LightValue.WhiteOn : (int)LightValue.BlueOn);
                break;
            case LightingMode.Flash:
                eventPlacement.UpdateValue(red ? (int)LightValue.RedFlash : white ? (int)LightValue.WhiteFlash : (int)LightValue.BlueFlash);
                break;
            case LightingMode.Fade:
                eventPlacement.UpdateValue(red ? (int)LightValue.RedFade : white ? (int)LightValue.WhiteFade : (int)LightValue.BlueFade);
                break;
            case LightingMode.Transition:
                eventPlacement.UpdateValue(red ? (int)LightValue.RedTransition : white ? (int)LightValue.WhiteTransition : (int)LightValue.BlueTransition);
                break;
        }
    }

    private void UpdateMode(Enum lightingMode)
    {
        currentMode = (LightingMode)lightingMode;
        UpdateValue();
    }
}

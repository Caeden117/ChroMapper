﻿using UnityEngine;
using UnityEngine.UI;

public class ColorTypeController : MonoBehaviour
{
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private LightingModeController lightMode;
    [SerializeField] private CustomColorsUIController customColors;
    [SerializeField] private Image leftSelected;
    [SerializeField] private Image rightSelected;
    [SerializeField] private Image leftNote;
    [SerializeField] private Image leftLight;
    [SerializeField] private Image rightNote;
    [SerializeField] private Image rightLight;

    private PlatformDescriptor platform;

    private void Start()
    {
        leftSelected.enabled = true;
        rightSelected.enabled = false;
        LoadInitialMap.PlatformLoadedEvent += SetupColors;
        customColors.CustomColorsUpdatedEvent += UpdateColors;
    }

    private void OnDestroy()
    {
        customColors.CustomColorsUpdatedEvent -= UpdateColors;
        LoadInitialMap.PlatformLoadedEvent -= SetupColors;
    }

    private void SetupColors(PlatformDescriptor descriptor)
    {
        platform = descriptor;
        UpdateColors();
    }

    private void UpdateColors()
    {
        leftNote.color = platform.Colors.RedNoteColor;
        leftLight.color = platform.Colors.RedColor;
        rightNote.color = platform.Colors.BlueNoteColor;
        rightLight.color = platform.Colors.BlueColor;
    }

    public void RedNote(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NoteTypeA);
    }

    public void BlueNote(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NoteTypeB);
    }

    public void UpdateValue(int type)
    {
        notePlacement.UpdateType(type);
        lightMode.UpdateValue();
        UpdateUI();
    }

    public void UpdateUI()
    {
        leftSelected.enabled = notePlacement.queuedData.Type == BeatmapNote.NoteTypeA;
        rightSelected.enabled = notePlacement.queuedData.Type == BeatmapNote.NoteTypeB;
    }

    public bool LeftSelectedEnabled() => leftSelected.enabled;
}

using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ColorTypeController : MonoBehaviour
{
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private Image leftSelected;
    [SerializeField] private Image rightSelected;
    [SerializeField] private Image leftNote;
    [SerializeField] private Image leftLight;
    [SerializeField] private Image rightNote;
    [SerializeField] private Image rightLight;

    void Start()
    {
        leftSelected.enabled = true;
        rightSelected.enabled = false;
        LoadInitialMap.PlatformLoadedEvent += SetupColors;
    }

    private void SetupColors(PlatformDescriptor descriptor)
    {
        leftNote.color = descriptor.colors.RedNoteColor;
        leftLight.color = descriptor.colors.RedColor;
        rightNote.color = descriptor.colors.BlueNoteColor;
        rightLight.color = descriptor.colors.BlueColor;

        LoadInitialMap.PlatformLoadedEvent -= SetupColors;
    }

    public void RedNote(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NOTE_TYPE_A);
    }

    public void BlueNote(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NOTE_TYPE_B);
    }

    public void UpdateValue(int type)
    {
        notePlacement.UpdateType(type);
        UpdateUI();
    }

    public void UpdateUI()
    {
        leftSelected.enabled = notePlacement.queuedData._type == BeatmapNote.NOTE_TYPE_A;
        rightSelected.enabled = notePlacement.queuedData._type == BeatmapNote.NOTE_TYPE_B;
    }
}

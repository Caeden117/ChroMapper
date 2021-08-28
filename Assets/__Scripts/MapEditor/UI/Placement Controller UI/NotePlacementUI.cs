﻿using UnityEngine;
using UnityEngine.UI;

public class NotePlacementUI : MonoBehaviour
{
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private BombPlacement bombPlacement;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [SerializeField] private DeleteToolController deleteToolController;

    [SerializeField] private Toggle[] chromaToggles;
    [SerializeField] private Toggle[] singleSaberDisabledToggles;

    private void Start()
    {
        var set = BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet;

        //ChromaToggle notes will be disabled until the mod is revived with some pretty breaking changes I have in mind.
        //The biggest of that is to create a new ChromaToggle characteristic that'll hold maps made for CT.
        foreach (var toggle in chromaToggles)
        {
            if (set.BeatmapCharacteristicName != "ChromaToggle")
            {
                toggle.interactable = false;
                var tooltip = toggle.GetComponent<Tooltip>();
                if (tooltip != null) tooltip.TooltipOverride = "ChromaToggle coming soon!";
            }
        }

        foreach (var toggle in singleSaberDisabledToggles)
        {
            if (set.BeatmapCharacteristicName == "OneSaber")
            {
                toggle.interactable = false;
                var tooltip = toggle.GetComponent<Tooltip>();
                if (tooltip != null) tooltip.TooltipOverride = "Single Saber only allows the right saber!";
            }
        }
    }

    public void RedNote(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NoteTypeA);
    }

    public void BlueNote(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NoteTypeB);
    }

    public void Bomb(bool active)
    {
        if (!active) return;
        notePlacement.IsActive = false;
        bombPlacement.IsActive = true;
        obstaclePlacement.IsActive = false;
        deleteToolController.UpdateDeletion(false);
    }

    public void Wall(bool active)
    {
        if (!active) return;
        notePlacement.IsActive = false;
        bombPlacement.IsActive = false;
        obstaclePlacement.IsActive = true;
        deleteToolController.UpdateDeletion(false);
    }

    public void RedAlt(bool active)
    {
        //if (active) UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.ALTERNATE);
    }

    public void BlueAlt(bool active)
    {
        //if (active) UpdateValue(BeatmapNote.NOTE_TYPE_B, true, BeatmapChromaNote.ALTERNATE);
    }

    public void Mono(bool active)
    {
        //if (active) UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.MONOCHROME);
    }

    public void Duo(bool active)
    {
        //if (active) UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.DUOCHROME);
    }

    public void UpdateValue(int v)
    {
        //if (notePlacement.atsc.IsPlaying) return;
        //if (!customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return; // not sure what these do?
        notePlacement.IsActive = true;
        bombPlacement.IsActive = false;
        obstaclePlacement.IsActive = false;
        notePlacement.UpdateType(v);
        deleteToolController.UpdateDeletion(false);
    }
}

using System;
using System.ComponentModel;
using UnityEngine;
using static EnumPicker;

public class PlacementModeController : MonoBehaviour
{
    public enum PlacementMode
    {
        [PickerChoice("Mapper", "place.note")]
        NOTE,
        [PickerChoice("Mapper", "place.bomb")]
        BOMB,
        [PickerChoice("Mapper", "place.wall")]
        WALL,
        [PickerChoice("Mapper", "place.delete")]
        DELETE
    }

    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private BombPlacement bombPlacement;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private DeleteToolController deleteToolController;

    [SerializeField] private EnumPicker modePicker;
    
    void Start()
    {
        modePicker.Initialize(typeof(PlacementMode));
        modePicker.OnClick += UpdateMode;
        UpdateMode(PlacementMode.NOTE);
    }

    public void SetMode(Enum placementMode)
    {
        modePicker.Select(placementMode);
        UpdateMode(placementMode);
    }

    private void UpdateMode(Enum placementMode)
    {
        PlacementMode mode = (PlacementMode)placementMode;
        notePlacement.IsActive = mode == PlacementMode.NOTE;
        bombPlacement.IsActive = mode == PlacementMode.BOMB;
        obstaclePlacement.IsActive = mode == PlacementMode.WALL;
        deleteToolController.UpdateDeletion(mode == PlacementMode.DELETE);
    }
}

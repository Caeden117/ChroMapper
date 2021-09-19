using System;
using UnityEngine;

public class PlacementModeController : MonoBehaviour
{
    public enum PlacementMode
    {
        [PickerChoice("Mapper", "place.note")] Note,
        [PickerChoice("Mapper", "place.bomb")] Bomb,
        [PickerChoice("Mapper", "place.wall")] Wall,

        [PickerChoice("Mapper", "place.delete")]
        Delete
    }

    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private BombPlacement bombPlacement;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private DeleteToolController deleteToolController;

    [SerializeField] private EnumPicker modePicker;

    private void Start()
    {
        modePicker.Initialize(typeof(PlacementMode));
        modePicker.OnClick += UpdateMode;
        UpdateMode(PlacementMode.Note);
    }

    public void SetMode(Enum placementMode)
    {
        modePicker.Select(placementMode);
        UpdateMode(placementMode);
    }

    private void UpdateMode(Enum placementMode)
    {
        var mode = (PlacementMode)placementMode;
        notePlacement.IsActive = mode == PlacementMode.Note;
        bombPlacement.IsActive = mode == PlacementMode.Bomb;
        obstaclePlacement.IsActive = mode == PlacementMode.Wall;
        deleteToolController.UpdateDeletion(mode == PlacementMode.Delete);
    }
}

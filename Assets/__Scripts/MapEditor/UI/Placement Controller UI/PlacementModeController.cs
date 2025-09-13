using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

public class PlacementModeController : MonoBehaviour
{
    public enum PlacementMode
    {
        [PickerChoice("Mapper", "place.note")] Note,
        [PickerChoice("Mapper", "place.bomb")] Bomb,
        [PickerChoice("Mapper", "place.wall")] Wall,

        [PickerChoice("Mapper", "place.chain")] Chain,
        [PickerChoice("Mapper", "place.arc")] Arc,
        
        [PickerChoice("Mapper", "place.delete")] Delete,
    }

    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private BombPlacement bombPlacement;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private ArcPlacement arcPlacement;
    [SerializeField] private ChainPlacement chainPlacement;
    
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

        if (mode == PlacementMode.Arc)
        {
            if (arcPlacement.SpawnArcsFromSelection() == 0)
            {
                var keybind = GetKeybindFromAction("SpawnArc");
                var localiseFormat = LocalizationSettings.StringDatabase.GetLocalizedString("Mapper", "place.arc.tutorial");
                var message = string.Format(localiseFormat, keybind);
                
                PersistentUI.Instance.ShowDialogBox(message, null, PersistentUI.DialogBoxPresetType.Ok);
            }
        }
        else if (mode == PlacementMode.Chain)
        {
            if (chainPlacement.SpawnChainFromSelection() == 0)
            {
                var keybind = GetKeybindFromAction("SpawnChain");
                var localiseFormat = LocalizationSettings.StringDatabase.GetLocalizedString("Mapper", "place.chain.tutorial");
                var message = string.Format(localiseFormat, keybind);
                
                PersistentUI.Instance.ShowDialogBox(message, null, PersistentUI.DialogBoxPresetType.Ok);
            }
        }

        // These "modes" act as a button click rather than a toggle
        if (mode is PlacementMode.Arc or PlacementMode.Chain)
        {
            mode = PlacementMode.Note;
            modePicker.Select(mode);
        }
            
        notePlacement.IsActive = mode == PlacementMode.Note;
        bombPlacement.IsActive = mode == PlacementMode.Bomb;
        obstaclePlacement.IsActive = mode == PlacementMode.Wall;
        deleteToolController.UpdateDeletion(mode == PlacementMode.Delete);
    }

    // This should probably be moved to somewhere else
    private static string GetKeybindFromAction(string actionName)
    {
        var input = CMInputCallbackInstaller.InputInstance;
        var actionMap = input.asset.actionMaps
            .First(x => x.actions.Any(y => y.name == actionName));
        var bindingNames = actionMap.SelectMany(x => x.controls.Select(c => c.displayName));
        var keybind = string.Join(" + ", bindingNames);

        return keybind;
    }
}

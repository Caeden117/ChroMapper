using Beatmap.V2;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapVersionSwitchInputController : MonoBehaviour, CMInput.ISwitchVersionActions
{
    [SerializeField] private PauseManager pauseManager;
    /// <summary>
    /// Switch version, then exist(for new containers reloading).
    /// </summary>
    /// <param name="context"></param>
    public void OnSwitchingVersion(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) return;
        PromptSwitchVersion();
    }

    public void PromptSwitchVersion()
    {
        var currentVersion = Settings.Instance.MapVersion == 3 ? "3" : "2";
        var convertVersion = Settings.Instance.MapVersion == 3 ? "2" : "3";
        var extraMsg = currentVersion == "3" ? "\n\nNOTE: Beatmap v2 is deprecated as stated legacy in internal game code, it is unlikely to be supported in the near future." : "";
        
        // TODO: Check if any data would be lost from conversion (e.g. AngleOffset from v3 to v2)
        PersistentUI.Instance.ShowDialogBox($"Do you want to change map version from map v{currentVersion} to v{convertVersion}?\nWARNING: Map containing incompatible data (including custom data) may result in loss, please >>manually backup<< your map before conversion." + extraMsg, (res) =>
        {
            if (res != 0) return;

            if (Settings.Instance.MapVersion == 3)
            {
                BeatSaberSongContainer.Instance.Map.ConvertCustomDataVersion(fromVersion: 3, toVersion: 2);
                Settings.Instance.MapVersion = 2;
            }
            else
            {
                BeatSaberSongContainer.Instance.Map.ConvertCustomDataVersion(fromVersion: 2, toVersion: 3);
                Settings.Instance.MapVersion = 3;
            }
        }, PersistentUI.DialogBoxPresetType.YesNoCancel);
    }
}

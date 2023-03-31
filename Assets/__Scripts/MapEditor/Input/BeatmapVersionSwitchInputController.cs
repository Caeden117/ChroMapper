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
        var v1 = Settings.Instance.Load_MapV3 ? "3" : "2";
        var v2 = Settings.Instance.Load_MapV3 ? "2" : "3";
        var extraMsg = v1 == "3" ? "\n\nNOTE: Beatmap v2 is deprecated as stated legacy in internal game code, it is unlikely to be supported in the near future." : "";
        PersistentUI.Instance.ShowDialogBox($"Do you want to change map version from map v{v1} to v{v2}?\nWARNING: Map containing incompatible data (including custom data) may result in loss, please >>manually backup<< your map before conversion." + extraMsg, (res) =>
        {
            if (res != 0) return;

            // what the fuck how does this converter even work?
            if (v1 == "3")
            {
                Settings.Instance.Load_MapV3 = false;
                var map = BeatSaberSongContainer.Instance.Map;
                var mapV2 = Beatmap.Converters.V3ToV2.Difficulty(map as V3Difficulty);
                BeatSaberSongContainer.Instance.Map = mapV2;
                map.MainNode = null;
            }
            else
            {
                Settings.Instance.Load_MapV3 = true;
                var map = BeatSaberSongContainer.Instance.Map;
                var mapV3 = Beatmap.Converters.V2ToV3.Difficulty(map as V2Difficulty);
                BeatSaberSongContainer.Instance.Map = mapV3;
            }
            pauseManager.Quit(true);
        }, PersistentUI.DialogBoxPresetType.YesNoCancel);
    }
}

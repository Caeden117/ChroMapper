using System.Collections;
using System.Collections.Generic;
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
        var v1 = Settings.Instance.Load_MapV3 ? "3" : "2";
        var v2 = Settings.Instance.Load_MapV3 ? "2" : "3";
        PersistentUI.Instance.ShowDialogBox($"Do you want to change map version from map v{v1} to v{v2}?\nPlease manually backup(copy and rename) your map before conversion.", (res) =>
        {
            if (res != 0) return;

            if (v1 == "3")
            {
                Settings.Instance.Load_MapV3 = false;
                var mapV3 = BeatSaberSongContainer.Instance.Map as BeatSaberMapV3;
                mapV3.Version = "2.2.0";
                mapV3.MainNode = null;
            }
            else
            {
                Settings.Instance.Load_MapV3 = true;
                var map = BeatSaberSongContainer.Instance.Map;
                var mapV3 = new BeatSaberMapV3(map);
                mapV3.Version = "3.0.0";
                BeatSaberSongContainer.Instance.Map = mapV3;
            }
            pauseManager.Quit(true);
        }, PersistentUI.DialogBoxPresetType.YesNoCancel);
    }
}

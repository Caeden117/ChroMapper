using System.Collections;
using System.Collections.Generic;
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
        var v1 = Settings.Instance.Load_MapV3 ? "3" : "2";
        var v2 = Settings.Instance.Load_MapV3 ? "2" : "3";
        PersistentUI.Instance.ShowDialogBox($"Do you want to change map version from map v{v1} to v{v2}?\nPlease manually backup(copy and rename) your map before conversion.", (res) =>
        {
            if (res != 0) return;

            if (v1 == "3")
            {
                Settings.Instance.Load_MapV3 = false;
                var mapV3 = BeatSaberSongContainer.Instance.Map;
                mapV3.MainNode = null;
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

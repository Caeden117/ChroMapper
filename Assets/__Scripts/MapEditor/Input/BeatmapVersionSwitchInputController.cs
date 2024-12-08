using System;
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

    private void OnChangeVersion(int version)
    {
        switch (version)
        {
            case 2:
                if (Settings.Instance.MapVersion == 3)
                    BeatSaberSongContainer.Instance.Map.ConvertCustomDataVersion(fromVersion: 3, toVersion: 2);
                Settings.Instance.MapVersion = 2;
                break;
            case 3:
                if (Settings.Instance.MapVersion == 2)
                    BeatSaberSongContainer.Instance.Map.ConvertCustomDataVersion(fromVersion: 2, toVersion: 3);
                Settings.Instance.MapVersion = 3;
                break;
            case 4:
                Settings.Instance.MapVersion = 4;
                break;
        }
    }

    public void PromptSwitchVersion()
    {
        // Don't expect this to be used that often so destroy on close
        var switchVersionDialogueBox = PersistentUI.Instance
            .CreateNewDialogBox()
            .WithTitle("Mapper","change.beatmap.version");

        switchVersionDialogueBox
            .AddComponent<TextComponent>()
            .WithInitialValue("Mapper", "change.beatmap.version.warning");

        // Cancel button
        switchVersionDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");

        switchVersionDialogueBox.AddFooterButton(() => OnChangeVersion(2), "v2");
        switchVersionDialogueBox.AddFooterButton(() => OnChangeVersion(3), "v3");
        
        // v4 difficulty is only supported with v4 info
        if (BeatSaberSongContainer.Instance.Info.MajorVersion == 4)
        {
            switchVersionDialogueBox.AddFooterButton(() => OnChangeVersion(4), "v4");
        }
        
        switchVersionDialogueBox.Open();
    }
}

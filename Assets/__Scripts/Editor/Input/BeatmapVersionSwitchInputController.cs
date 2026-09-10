using System;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapVersionSwitchInputController : MonoBehaviour, CMInput.ISwitchVersionActions
{
    private const string BeatToTheFutureRequirement = "BeatToTheFuture";

    // TODO: Localization — move the migration warning into the Mapper string table before stable.
    private const string MappingExtensionsWallPromptMessage =
        "Mapping Extensions is no longer supported in latest Beat Saber versions, and appears unmaintained " +
        "moving forward (so likely never will be). Do you want to convert your modded walls from " +
        "MappingExtensions to Noodle?";

    public static event Action<int> OnMapVersionChanged;

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
                if (Settings.Instance.MapVersion is 3 or 4)
                    BeatSaberSongContainer.Instance.Map.ConvertCustomDataVersion(
                        fromVersion: Settings.Instance.MapVersion,
                        toVersion: 2);
                Settings.Instance.MapVersion = 2;
                break;
            case 3:
                if (Settings.Instance.MapVersion == 2)
                    BeatSaberSongContainer.Instance.Map.ConvertCustomDataVersion(fromVersion: 2, toVersion: 3);
                Settings.Instance.MapVersion = 3;
                break;
            case 4:
                if (Settings.Instance.MapVersion == 2)
                    BeatSaberSongContainer.Instance.Map.ConvertCustomDataVersion(fromVersion: 2, toVersion: 4);
                Settings.Instance.MapVersion = 4;
                break;
        }

        // Notify format-dependent editor lanes after conversion has made the selected version authoritative.
        OnMapVersionChanged?.Invoke(version);
    }

    private void OnChangeToV3WithVNJS(bool includeVNJSEvents)
    {
        BeatSaberSongContainer.Instance.Map.SaveVNJSEventsInV3 = includeVNJSEvents;
        if (includeVNJSEvents)
        {
            ContinueChangeToV3AfterUpperWalls();
            return;
        }

        ContinueChangeToV3();
    }

    private void OnChangeToV3WithBeatToTheFuture()
    {
        ContinueChangeToV3AfterUpperWalls();
    }

    private void OnChangeToV3WithNoodleExtensions()
    {
        var map = BeatSaberSongContainer.Instance.Map;
        foreach (var wall in map.Obstacles)
        {
            if (!IsV4UpperWall(wall.PosY))
            {
                continue;
            }

            var shape = wall.GetShape();
            wall.CustomCoordinate = new JSONArray
            {
                [0] = shape.Position,
                [1] = shape.StartHeight
            };
            wall.PosY = 0;
            wall.WriteCustom();
        }

        ContinueChangeToV3AfterUpperWalls();
    }

    private void OnChangeToV3WithoutUpperWallMod()
    {
        foreach (var wall in BeatSaberSongContainer.Instance.Map.Obstacles)
        {
            if (IsV4UpperWall(wall.PosY))
            {
                wall.PosY = 0;
            }
        }

        ContinueChangeToV3AfterUpperWalls();
    }

    private void ContinueChangeToV3()
    {
        if (ShouldPromptForV4UpperWalls())
        {
            PromptV4UpperWallConversion();
            return;
        }

        ContinueChangeToV3AfterUpperWalls();
    }

    private void OnConvertMappingExtensionsWallsToNoodleExtensions()
    {
        var songContainer = BeatSaberSongContainer.Instance;
        foreach (var wall in songContainer.Map.Obstacles)
        {
            if (!wall.IsMappingExtensionsWallForMigration())
            {
                continue;
            }

            var shape = wall.GetShape();
            wall.PosX = 0;
            wall.PosY = 0;
            wall.Width = 1;
            wall.Height = 5;
            wall.Type = 0;
            wall.CustomCoordinate = new JSONArray
            {
                [0] = shape.Position,
                [1] = shape.StartHeight
            };
            wall.CustomSize = new JSONArray
            {
                [0] = shape.Width,
                [1] = shape.Height
            };
            wall.WriteCustom();
        }

        OnChangeVersion(3);
    }

    private void OnKeepMappingExtensionsWalls() => OnChangeVersion(3);

    // Prompt only when wall data contains a legacy encoding; stale requirements, vanilla walls,
    // existing Noodle walls, and BeatToTheFuture y=3/4 lanes are intentionally excluded by the shared domain helper.
    private bool ShouldPromptForMappingExtensionsWalls()
    {
        foreach (var wall in BeatSaberSongContainer.Instance.Map.Obstacles)
        {
            if (wall.IsMappingExtensionsWallForMigration())
            {
                return true;
            }
        }

        return false;
    }

    private void ContinueChangeToV3AfterUpperWalls()
    {
        if (ShouldPromptForMappingExtensionsWalls())
        {
            PromptMappingExtensionsWallMigration();
            return;
        }

        OnChangeVersion(3);
    }

    private bool ShouldPromptForV4UpperWalls()
    {
        var songContainer = BeatSaberSongContainer.Instance;
        if (Settings.Instance.MapVersion != 4
            || songContainer.MapDifficultyInfo.CustomRequirements.Contains(BeatToTheFutureRequirement))
        {
            return false;
        }

        return songContainer.Map.HasV4UpperWalls();
    }

    private bool ShouldPromptForV4VNJS() =>
        Settings.Instance.MapVersion == 4
        && BeatSaberSongContainer.Instance.Map.NJSEvents.Count > 0;

    private static bool IsV4UpperWall(int posY) => posY is 3 or 4;

    // V4 VNJS needs an explicit compatibility decision because ordinary V3 players do not understand the OEM arrays.
    private void PromptChangeToV3()
    {
        // TODO: Localization — translate this compatibility prompt's title, body, and footer labels before stable.
        var dialogBox = PersistentUI
            .Instance
            .CreateNewDialogBox()
            .WithTitle("Variable Note Jump Speed");

        dialogBox
            .AddComponent<TextComponent>()
            .WithInitialValue(
                "Do you want to add a BeatToTheFuture requirement and include Variable Note Jump Speed events in your V3 map? " +
                "Otherwise your VNJS events will be omitted from the V3 difficulty file when it is saved.");

        dialogBox.AddFooterButton(() => OnChangeToV3WithVNJS(true), "Yes");
        dialogBox.AddFooterButton(() => OnChangeToV3WithVNJS(false), "No");
        dialogBox.Open();
    }

    private void PromptV4UpperWallConversion()
    {
        // TODO: Localization — translate this conversion prompt's title, body, and fallback label before stable.
        var dialogBox = PersistentUI
            .Instance
            .CreateNewDialogBox()
            .WithTitle("V4 Upper-Lane Walls");

        dialogBox
            .AddComponent<TextComponent>()
            .WithInitialValue(
                "Your map contains V4 walls at y=3 or y=4, which ordinary V3 maps do not support. " +
                "You can add a BeatToTheFuture dependency to preserve them working as-is in V3, or add NoodleExtensions dependency and auto-convert them to Noodle walls, " +
                "otherwise they will move to y=0.");

        // Dialog footer buttons render in insertion order, so keep the two preserving choices before the vanilla fallback.
        dialogBox.AddFooterButton(OnChangeToV3WithBeatToTheFuture, "BeatToTheFuture");
        dialogBox.AddFooterButton(OnChangeToV3WithNoodleExtensions, "NoodleExtensions");
        dialogBox.AddFooterButton(OnChangeToV3WithoutUpperWallMod, "Set y=0");
        dialogBox.Open();
    }

    // The migration warning and explicit Yes/No callbacks replace silent Mapping Extensions tagging with a deliberate
    // choice while MappingExtensionsWallMigrationUsesUnsupportedModWarning protects the exact requested wording.
    private void PromptMappingExtensionsWallMigration()
    {
        // TODO: Localization — translate the title and footer labels along with MappingExtensionsWallPromptMessage.
        var dialogBox = PersistentUI
            .Instance
            .CreateNewDialogBox()
            .WithTitle("Mapping Extensions Walls");

        dialogBox
            .AddComponent<TextComponent>()
            .WithInitialValue(MappingExtensionsWallPromptMessage);

        dialogBox.AddFooterButton(OnConvertMappingExtensionsWallsToNoodleExtensions, "Yes");
        dialogBox.AddFooterButton(OnKeepMappingExtensionsWalls, "No");
        dialogBox.Open();
    }

    public void PromptSwitchVersion()
    {
        // Don't expect this to be used that often so destroy on close
        var switchVersionDialogueBox = PersistentUI
            .Instance
            .CreateNewDialogBox()
            .WithTitle("Mapper", "change.beatmap.version");

        switchVersionDialogueBox
            .AddComponent<TextComponent>()
            .WithInitialValue("Mapper", "change.beatmap.version.warning");

        // Cancel button
        switchVersionDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");

        switchVersionDialogueBox.AddFooterButton(() => OnChangeVersion(2), "v2");
        switchVersionDialogueBox.AddFooterButton(null, "v3").OnClick(
            () =>
            {
                if (ShouldPromptForV4VNJS())
                {
                    switchVersionDialogueBox.Close();
                    PromptChangeToV3();
                    return;
                }

                ContinueChangeToV3();
                switchVersionDialogueBox.Close();
            });

        // v4 difficulty is only supported with v4 info
        if (BeatSaberSongContainer.Instance.Info.MajorVersion == 4)
        {
            switchVersionDialogueBox.AddFooterButton(() => OnChangeVersion(4), "v4");
        }

        switchVersionDialogueBox.Open();
    }
}

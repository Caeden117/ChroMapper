using System;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapVersionSwitchInputController : MonoBehaviour, CMInput.ISwitchVersionActions
{
    // V4 upper-wall choices add supported replacement requirements by these canonical Info.dat names.
    private const string BeatToTheFutureRequirement = "BeatToTheFuture";
    private const string MappingExtensionsRequirement = "Mapping Extensions";
    private const string NoodleExtensionsRequirement = "Noodle Extensions";

    // MappingExtensionsWallMigrationUsesUnsupportedModWarning locks this policy text because the prompt replaces an
    // obsolete automatic requirement with an explicit, geometry-preserving migration decision.
    private const string MappingExtensionsWallPromptMessage =
        "Mapping Extensions is no longer supported in latest Beat Saber versions, and appears unmaintained " +
        "moving forward (so likely never will be). Do you want to convert your modded walls from " +
        "MappingExtensions to Noodle?";

    // VNJS lane availability follows the active save format, including version switches that do not reload the mapper scene.
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

    // V3 previously omitted VNJS on save, so this choice records whether to keep that behavior before continuing
    // to the independently evaluated upper-wall compatibility decision.
    private void OnChangeToV3WithVNJS(bool includeVNJSEvents)
    {
        var songContainer = BeatSaberSongContainer.Instance;
        songContainer.Map.SaveVNJSEventsInV3 = includeVNJSEvents;

        if (includeVNJSEvents
            && !songContainer.MapDifficultyInfo.CustomRequirements.Contains(BeatToTheFutureRequirement))
        {
            songContainer.MapDifficultyInfo.CustomRequirements.Add(BeatToTheFutureRequirement);
        }

        ContinueChangeToV3();
    }

    // BeatToTheFuture supports raw V4 y=3/4 walls in V3, so this choice preserves their geometry and records that runtime.
    private void OnChangeToV3WithBeatToTheFuture()
    {
        AddRequirement(BeatToTheFutureRequirement);
        ContinueChangeToV3AfterUpperWalls();
    }

    // Noodle Extensions represents the unsupported V3 wall height through custom coordinates, so this choice moves
    // only the raw base wall to y=0 while retaining its original visual x/y position in Noodle data.
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

        AddRequirement(NoodleExtensionsRequirement);
        ContinueChangeToV3AfterUpperWalls();
    }

    // Declining mod conversion must not leave unsupported y=3/4 values that the V3 requirement scanner mistakes for
    // Mapping Extensions, so this safe fallback moves only those affected walls to the vanilla floor.
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

    // The VNJS prompt may add or preserve requirements before this point, so reevaluate the wall gate here and skip
    // a redundant prompt whenever BeatToTheFuture already makes raw upper walls valid.
    private void ContinueChangeToV3()
    {
        if (ShouldPromptForV4UpperWalls())
        {
            PromptV4UpperWallConversion();
            return;
        }

        ContinueChangeToV3AfterUpperWalls();
    }

    // MappingExtensionsWallMigrationPreservesShapeAsNoodle verifies that every affected wall is converted from its
    // rendered legacy bounds into Noodle coordinates/size before raw fields are normalized to vanilla-safe values.
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

        var requirements = songContainer.MapDifficultyInfo.CustomRequirements;
        if (!songContainer.Map.IsMappingExtensions(allowV4UpperWallsInV3: true))
        {
            requirements.RemoveAll(x => x == MappingExtensionsRequirement);
        }

        AddRequirement(NoodleExtensionsRequirement);
        OnChangeVersion(3);
    }

    // DecliningMappingExtensionsWallMigrationLeavesMapUnchanged verifies that No retains legacy data and its current
    // requirement, so declining the optional migration never performs a destructive or partial conversion.
    private void OnKeepMappingExtensionsWalls() => OnChangeVersion(3);

    // Prompt only when authoritative wall data contains a legacy encoding; stale requirements, vanilla walls,
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

    // Mapping Extensions migration is evaluated after the independent upper-wall decision so BeatToTheFuture lanes
    // and newly created Noodle walls are not prompted twice during the same V2/V4-to-V3 conversion chain.
    private void ContinueChangeToV3AfterUpperWalls()
    {
        if (ShouldPromptForMappingExtensionsWalls())
        {
            PromptMappingExtensionsWallMigration();
            return;
        }

        OnChangeVersion(3);
    }

    // Only raw V4 wall lanes that V3 cannot represent need a decision, and an existing BeatToTheFuture requirement already
    // supplies the compatible runtime behavior requested by the mapper.
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

    // Keep the VNJS prompt decision independently observable from the upper-wall gate so maps containing both features
    // take both branches in sequence instead of treating either compatibility choice as a substitute for the other.
    private bool ShouldPromptForV4VNJS() =>
        Settings.Instance.MapVersion == 4
        && BeatSaberSongContainer.Instance.Map.NJSEvents.Count > 0;

    // Keep the supported upper lanes centralized so prompt detection and both destructive conversions use identical
    // boundaries and never alter unrelated Mapping Extensions geometry.
    private static bool IsV4UpperWall(int posY) => posY is 3 or 4;

    // Requirement choices share one idempotent update so repeated conversions cannot duplicate Info.dat entries.
    private static void AddRequirement(string requirement)
    {
        var requirements = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomRequirements;
        if (!requirements.Contains(requirement))
        {
            requirements.Add(requirement);
        }
    }

    // V4 VNJS needs an explicit compatibility decision because ordinary V3 players do not understand the OEM arrays.
    private void PromptChangeToV3()
    {
        var dialogBox = PersistentUI
            .Instance
            .CreateNewDialogBox()
            .WithTitle("Variable Note Jump Speed");

        dialogBox
            .AddComponent<TextComponent>()
            .WithInitialValue(
                "Do you want to add a BeatToTheFuture requirement and include Variable Note Jump Speed events in your V3 map? " +
                "Otherwise your VNJS events will be omitted from the V3 difficulty file when it is saved.");

        // Dialog footer buttons render in insertion order, so put the preservation choice on the left and omission on the right.
        dialogBox.AddFooterButton(() => OnChangeToV3WithVNJS(true), "Yes");
        dialogBox.AddFooterButton(() => OnChangeToV3WithVNJS(false), "No");
        dialogBox.Open();
    }

    // Raw V4 upper walls have three deliberate V3 outcomes, so this prompt prevents an implicit Mapping Extensions
    // requirement and makes preservation, Noodle conversion, or vanilla flattening an explicit mapper choice.
    private void PromptV4UpperWallConversion()
    {
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
        // V4 maps resolve VNJS first, then dynamically evaluate upper walls so requirements chosen by the first prompt
        // can suppress a redundant second prompt while maps containing both features still receive both decisions.
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
